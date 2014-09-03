/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Diagnostics;

/**
 * Class to construct DFAs that match a word within some edit distance.
 * <p>
 * Implements the algorithm described in:
 * Schulz and Mihov: Fast String Correction with Levenshtein Automata
 * <p>
 * @lucene.experimental
 */
public class LevenshteinAutomata
{
   /** Maximum edit distance this class can generate an automaton for.
    *  @lucene.internal */
   public static readonly int MAXIMUM_SUPPORTED_DISTANCE = 2;

   /* input word */
   readonly int[] word;
   /* the automata alphabet. */
   readonly int[] alphabet;
   /* the maximum symbol in the alphabet (e.g. 255 for UTF-8 or 10FFFF for UTF-32) */
   readonly int alphaMax;

   /* the ranges outside of alphabet */
   readonly int[] rangeLower;
   readonly int[] rangeUpper;
   int numRanges = 0;

   ParametricDescription[] descriptions;

   /**
    * Create a new LevenshteinAutomata for some input String.
    * Optionally count transpositions as a primitive edit.
    */
   public LevenshteinAutomata(String input, Boolean withTranspositions)
      :
         this(CodePoints.codePoints(input), CodePoints.MAX_CODE_POINT, withTranspositions)
   { }

   /**
    * Expert: specify a custom maximum possible symbol
    * (alphaMax); default is Character.MAX_CODE_POINT.
    */
   public LevenshteinAutomata(int[] word, int alphaMax, Boolean withTranspositions)
   {
      this.word = word;
      this.alphaMax = alphaMax;

      // calculate the alphabet
      //SortedSet<Integer> set = new TreeSet<>();
      var set = new Wintellect.PowerCollections.OrderedSet<int>();
      for (int i = 0; i < word.Length; i++)
      {
         int v = word[i];
         if (v > alphaMax)
         {
            throw new ArgumentException("alphaMax exceeded by symbol " + v + " in word");
         }
         set.Add(v);
      }
      alphabet = new int[set.Count /* size() */];
      //Iterator<Integer> iterator = set.iterator();
      //for (int i = 0; i < alphabet.length; i++)
      using (var iterator = set.GetEnumerator())
         for (var i = 0; iterator.MoveNext(); i++)
            alphabet[i] = iterator.Current; // iterator.next();

      rangeLower = new int[alphabet.Length + 2];
      rangeUpper = new int[alphabet.Length + 2];
      // calculate the unicode range intervals that exclude the alphabet
      // these are the ranges for all unicode characters not in the alphabet
      int lower = 0;
      for (int i = 0; i < alphabet.Length; i++)
      {
         int higher = alphabet[i];
         if (higher > lower)
         {
            rangeLower[numRanges] = lower;
            rangeUpper[numRanges] = higher - 1;
            numRanges++;
         }
         lower = higher + 1;
      }
      /* add the final endpoint */
      if (lower <= alphaMax)
      {
         rangeLower[numRanges] = lower;
         rangeUpper[numRanges] = alphaMax;
         numRanges++;
      }

      descriptions = new ParametricDescription[] {
        null, /* for n=0, we do not need to go through the trouble */
        withTranspositions ? (ParametricDescription)new Lev1TParametricDescription(word.Length) : new Lev1ParametricDescription(word.Length),
        withTranspositions ? (ParametricDescription)new Lev2TParametricDescription(word.Length) : new Lev2ParametricDescription(word.Length),
    };
   }

   /**
    * Compute a DFA that accepts all strings within an edit distance of <code>n</code>.
    * <p>
    * All automata have the following properties:
    * <ul>
    * <li>They are deterministic (DFA).
    * <li>There are no transitions to dead states.
    * <li>They are not minimal (some transitions could be combined).
    * </ul>
    * </p>
    */
   public Automaton toAutomaton(int n)
   {
      return toAutomaton(n, "");
   }

   /**
    * Compute a DFA that accepts all strings within an edit distance of <code>n</code>,
    * matching the specified exact prefix.
    * <p>
    * All automata have the following properties:
    * <ul>
    * <li>They are deterministic (DFA).
    * <li>There are no transitions to dead states.
    * <li>They are not minimal (some transitions could be combined).
    * </ul>
    * </p>
    */
   public Automaton toAutomaton(int n, String prefix)
   {
      Debug.Assert(prefix != null);

      if (n == 0)
      {

         return Automaton.makeString(prefix + CodePoints.newString(word, 0, word.Length));

         //return Automata.makeString(prefix + UnicodeUtil.newString(word, 0, word.Length));
      }

      if (n >= descriptions.Length)
         return null;

      int range = 2 * n + 1;
      ParametricDescription description = descriptions[n];
      // the number of states is based on the length of the word and n
      int numStates = description.size();

      Automaton a = new Automaton();
      int lastState;
      if (prefix != null)
      {
         // Insert prefix
         lastState = a.createState();
         for (int i = 0, cp = 0; i < prefix.Length; i += CodePoints.charCount(cp))
         {
            int state = a.createState();
            cp = CodePoints.codePointAt(prefix, i); // prefix.codePointAt(i);
            a.addTransition(lastState, state, cp, cp);
            lastState = state;
         }
      }
      else
      {
         lastState = a.createState();
      }

      int stateOffset = lastState;
      a.setAccept(lastState, description.isAccept(0));

      // create all states, and mark as accept states if appropriate
      for (int i = 1; i < numStates; i++)
      {
         int state = a.createState();
         a.setAccept(state, description.isAccept(i));
      }

      // TODO: this creates bogus states/transitions (states are final, have self loops, and can't be reached from an init state)

      // create transitions from state to state
      for (int k = 0; k < numStates; k++)
      {
         int xpos = description.getPosition(k);
         if (xpos < 0)
            continue;
         int end = xpos + Math.Min(word.Length - xpos, range);

         for (int x = 0; x < alphabet.Length; x++)
         {
            int ch = alphabet[x];
            // get the characteristic vector at this position wrt ch
            int cvec = getVector(ch, xpos, end);
            int dest = description.transition(k, xpos, cvec);
            if (dest >= 0)
            {
               a.addTransition(stateOffset + k, stateOffset + dest, ch);
            }
         }
         // add transitions for all other chars in unicode
         // by definition, their characteristic vectors are always 0,
         // because they do not exist in the input string.
         int dest2 = description.transition(k, xpos, 0); // by definition
         if (dest2 >= 0)
         {
            for (int r = 0; r < numRanges; r++)
            {
               a.addTransition(stateOffset + k, stateOffset + dest2, rangeLower[r], rangeUpper[r]);
            }
         }
      }

      a.finishState();

      // TODO TOD TOD
      //    Debug.Assert(a.isDeterministic());
      return a;
   }

   /**
    * Get the characteristic vector <code>X(x, V)</code> 
    * where V is <code>substring(pos, end)</code>
    */
   int getVector(int x, int pos, int end)
   {
      int vector = 0;
      for (int i = pos; i < end; i++)
      {
         vector <<= 1;
         if (word[i] == x)
            vector |= 1;
      }
      return vector;
   }
}
