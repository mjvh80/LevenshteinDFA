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

/** Represents an automaton and all its states and transitions.  States
 *  are integers and must be created using {@link #createState}.  Mark a
 *  state as an accept state using {@link #setAccept}.  Add transitions
 *  using {@link #addTransition}.  Each state must have all of its
 *  transitions added at once; if this is too restrictive then use
 *  {@link Automaton.Builder} instead.  State 0 is always the
 *  initial state.  Once a state is finished, either
 *  because you've starting adding transitions to another state or you
 *  call {@link #finishState}, then that states transitions are sorted
 *  (first by min, then max, then dest) and reduced (transitions with
 *  adjacent labels going to the same dest are combined).
 *
 * @lucene.experimental */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public class Automaton
{
   /** Where we next write to the int[] states; this increments by 2 for
    *  each added state because we pack a pointer to the transitions
    *  array and a count of how many transitions leave the state.  */
   private int nextState;

   /** Where we next write to in int[] transitions; this
    *  increments by 3 for each added transition because we
    *  pack min, max, dest in sequence. */
   private int nextTransition;

   /** Current state we are adding transitions to; the caller
    *  must add all transitions for this state before moving
    *  onto another state. */
   private int curState = -1;

   /** Index in the transitions array, where this states
    *  leaving transitions are stored, or -1 if this state
    *  has not added any transitions yet, followed by number
    *  of transitions.
    *  
    *  Thus, array looks like [ ...., index_transition, num_transition, ... ]
    *  A state is an index in this array / 2.
    */
   private int[] states = new int[4];

   /** Holds toState, min, max for each transition. */
   private int[] transitions = new int[6];

   private BitSet _isAccept = new BitSet(4);

   /** True if no state has two transitions leaving with the same label. */
   private bool deterministic = true;

   /** Sole constructor; creates an automaton with no states. */
   public Automaton()
   {
   }

   #region Methods Taken From Automata.java

   /**
   * Returns a new (deterministic) automaton that accepts the single given
   * string.
   */
   public static Automaton makeString(String s)
   {
      Automaton a = new Automaton();
      int lastState = a.createState();
      for (int i = 0, cp = 0; i < s.Length; i += CodePoints.charCount(cp))
      {
         int state = a.createState();
         cp = CodePoints.codePointAt(s, i); // s.codePointAt(i);
         a.addTransition(lastState, state, cp, cp);
         lastState = state;
      }

      a.setAccept(lastState, true);
      a.finishState();

      Debug.Assert(a.isDeterministic());
      // Debug.Assert(Operations.hasDeadStates(a) == false);

      return a;
   }

   public static Automaton makeEmpty()
   {
      Automaton a = new Automaton();
      a.finishState();
      return a;
   }

   #endregion

   /** Create a new state. */
   public int createState()
   {
      growStates();
      int state = nextState / 2;
      states[nextState] = -1;
      nextState += 2;
      return state;
   }

   /** Set or clear this state as an accept state. */
   public void setAccept(int state, bool accept)
   {
      if (state >= getNumStates())
      {
         throw new ArgumentException("state=" + state + " is out of bounds (numStates=" + getNumStates() + ")");
      }
      if (accept)
      {
         _isAccept.Set(state, true);
      }
      else
      {
         _isAccept.Set(state, false);
      }
   }

   /** Sugar to get all transitions for all states.  This is
    *  object-heavy; it's better to iterate state by state instead. */
   public Transition[][] getSortedTransitions()
   {
      int numStates = getNumStates();
      Transition[][] transitions = new Transition[numStates][];
      for (int s = 0; s < numStates; s++)
      {
         int numTransitions = getNumTransitions(s);
         transitions[s] = new Transition[numTransitions];
         for (int t = 0; t < numTransitions; t++)
         {
            Transition transition = new Transition();
            getTransition(s, t, transition);
            transitions[s][t] = transition;
         }
      }

      return transitions;
   }

   /** Returns accept states.  If the bit is set then that state is an accept state. */
   public BitSet getAcceptStates()
   {
      return _isAccept;
   }

   /** Returns true if this state is an accept state. */
   public bool isAccept(int state)
   {
      return _isAccept.Get(state);
   }

   /** Add a new transition with min = max = label. */
   public void addTransition(int source, int dest, int label)
   {
      addTransition(source, dest, label, label);
   }

   /** Add a new transition with the specified source, dest, min, max. */
   public void addTransition(int source, int dest, int min, int max)
   {
      Debug.Assert(nextTransition % 3 == 0);

      if (source >= nextState / 2)
      {
         throw new ArgumentException("source=" + source + " is out of bounds (maxState is " + (nextState / 2 - 1) + ")");
      }
      if (dest >= nextState / 2)
      {
         throw new ArgumentException("dest=" + dest + " is out of bounds (max state is " + (nextState / 2 - 1) + ")");
      }

      growTransitions();
      if (curState != source)
      {
         if (curState != -1)
         {
            finishCurrentState();
         }

         // Move to next source:
         curState = source;
         if (states[2 * curState] != -1)
         {
            throw new ArgumentException("from state (" + source + ") already had transitions added");
         }
         Debug.Assert(states[2 * curState + 1] == 0);
         states[2 * curState] = nextTransition;
      }

      transitions[nextTransition++] = dest;
      transitions[nextTransition++] = min;
      transitions[nextTransition++] = max;

      // Increment transition count for this state
      states[2 * curState + 1]++;
   }

   /** Add a [virtual] epsilon transition between source and dest.
    *  Dest state must already have all transitions added because this
    *  method simply copies those same transitions over to source. */
   public void addEpsilon(int source, int dest)
   {
      Transition t = new Transition();
      int count = initTransition(dest, t);
      for (int i = 0; i < count; i++)
      {
         getNextTransition(t);
         addTransition(source, t.dest, t.min, t.max);
      }
      if (isAccept(dest))
      {
         setAccept(source, true);
      }
   }

   /** Copies over all states/transitions from other.  The states numbers
    *  are sequentially assigned (appended). */
   public void copy(Automaton other)
   {

      // Bulk copy and then fixup the state pointers:
      int stateOffset = getNumStates();
      states = ArrayUtil.grow(states, nextState + other.nextState);
      Array.Copy(other.states, 0, states, nextState, other.nextState);
      // System.arraycopy(other.states, 0, states, nextState, other.nextState);
      for (int i = 0; i < other.nextState; i += 2)
      {
         if (states[nextState + i] != -1)
         {
            states[nextState + i] += nextTransition;
         }
      }
      nextState += other.nextState;
      int otherNumStates = other.getNumStates();
      var otherAcceptStates = other.getAcceptStates();
      int state = 0;

      while (state < otherNumStates && (state = otherAcceptStates.NextSetBit(state)) != -1)
      {
         setAccept(stateOffset + state, true);
         state++;
      }

      // Bulk copy and then fixup dest for each transition:
      transitions = ArrayUtil.grow(transitions, nextTransition + other.nextTransition);
      Array.Copy(other.transitions, 0, transitions, nextTransition, other.nextTransition);
      for (int i = 0; i < other.nextTransition; i += 3)
      {
         transitions[nextTransition + i] += stateOffset;
      }
      nextTransition += other.nextTransition;

      if (other.deterministic == false)
      {
         deterministic = false;
      }
   }

   /** Freezes the last state, sorting and reducing the transitions. */
   private void finishCurrentState()
   {
      int numTransitions = states[2 * curState + 1];
      Debug.Assert(numTransitions > 0);


      int offset = states[2 * curState];
      int start = offset / 3;
      destMinMaxSorter.sort(transitions, start, start + numTransitions);

      // Reduce any "adjacent" transitions:
      int upto = 0;
      int min = -1;
      int max = -1;
      int dest = -1;

      for (int i = 0; i < numTransitions; i++)
      {
         int tDest = transitions[offset + 3 * i];
         int tMin = transitions[offset + 3 * i + 1];
         int tMax = transitions[offset + 3 * i + 2];

         if (dest == tDest)
         {
            if (tMin <= max + 1)
            {
               if (tMax > max)
               {
                  max = tMax;
               }
            }
            else
            {
               if (dest != -1)
               {
                  transitions[offset + 3 * upto] = dest;
                  transitions[offset + 3 * upto + 1] = min;
                  transitions[offset + 3 * upto + 2] = max;
                  upto++;
               }
               min = tMin;
               max = tMax;
            }
         }
         else
         {
            if (dest != -1)
            {
               transitions[offset + 3 * upto] = dest;
               transitions[offset + 3 * upto + 1] = min;
               transitions[offset + 3 * upto + 2] = max;
               upto++;
            }
            dest = tDest;
            min = tMin;
            max = tMax;
         }
      }

      if (dest != -1)
      {
         // Last transition
         transitions[offset + 3 * upto] = dest;
         transitions[offset + 3 * upto + 1] = min;
         transitions[offset + 3 * upto + 2] = max;
         upto++;
      }

      nextTransition -= (numTransitions - upto) * 3;
      states[2 * curState + 1] = upto;

      // Sort transitions by min/max/dest:
      minMaxDestSorter.sort(transitions, start, start + upto);

      if (deterministic && upto > 1)
      {
         int lastMax = transitions[offset + 2];
         for (int i = 1; i < upto; i++)
         {
            min = transitions[offset + 3 * i + 1];
            if (min <= lastMax)
            {
               deterministic = false;
               break;
            }
            lastMax = transitions[offset + 3 * i + 2];
         }
      }
   }

   /** Returns true if this automaton is deterministic (for ever state
    *  there is only one transition for each label). */
   public bool isDeterministic()
   {
      return deterministic;
   }

   /** Finishes the current state; call this once you are done adding
    *  transitions for a state.  This is automatically called if you
    *  start adding transitions to a new source state, but for the last
    *  state you add you need to this method yourself. */
   public void finishState()
   {
      if (curState != -1)
      {
         finishCurrentState();
         curState = -1;
      }
   }

   // TODO: add finish() to shrink wrap the arrays?

   /** How many states this automaton has. */
   public int getNumStates()
   {
      return nextState / 2;
   }

   /** How many transitions this state has. */
   public int getNumTransitions(int state)
   {
      int count = states[2 * state + 1];
      if (count == -1)
      {
         return 0;
      }
      else
      {
         return count;
      }
   }

   private void growStates()
   {
      if (nextState + 2 >= states.Length)
      {
         states = ArrayUtil.grow(states, nextState + 2);
      }
   }

   private void growTransitions()
   {
      if (nextTransition + 3 >= transitions.Length)
      {
         transitions = ArrayUtil.grow(transitions, nextTransition + 3);
      }
   }

   private class _DestMinMaxSorter : InPlaceMergeSorter<int>
   {
      private void SwapOne(int i, int j)
      {
         int x = arrayToSort[i];
         arrayToSort[i] = arrayToSort[j];
         arrayToSort[j] = x;
      }

      protected override void swap(int i, int j)
      {
         int iStart = 3 * i;
         int jStart = 3 * j;
         SwapOne(iStart, jStart);
         SwapOne(iStart + 1, jStart + 1);
         SwapOne(iStart + 2, jStart + 2);
      }

      protected override int compare(int i, int j)
      {
         int iStart = 3 * i;
         int jStart = 3 * j;

         // First dest:
         int iDest = arrayToSort[iStart];
         int jDest = arrayToSort[jStart];
         if (iDest < jDest)
         {
            return -1;
         }
         else if (iDest > jDest)
         {
            return 1;
         }

         // Then min:
         int iMin = arrayToSort[iStart + 1];
         int jMin = arrayToSort[jStart + 1];
         if (iMin < jMin)
         {
            return -1;
         }
         else if (iMin > jMin)
         {
            return 1;
         }

         // Then max:
         int iMax = arrayToSort[iStart + 2];
         int jMax = arrayToSort[jStart + 2];
         if (iMax < jMax)
         {
            return -1;
         }
         else if (iMax > jMax)
         {
            return 1;
         }

         return 0;
      }
   }

   /** Sorts transitions by dest, ascending, then min label ascending, then max label ascending */
   private Sorter<int> destMinMaxSorter = new _DestMinMaxSorter(); // target: transitions

   private class _MinMaxDestSorter : InPlaceMergeSorter<int>
   {
      private void swapOne(int i, int j)
      {
         int x = arrayToSort[i];
         arrayToSort[i] = arrayToSort[j];
         arrayToSort[j] = x;
      }

      override
      protected void swap(int i, int j)
      {
         int iStart = 3 * i;
         int jStart = 3 * j;
         swapOne(iStart, jStart);
         swapOne(iStart + 1, jStart + 1);
         swapOne(iStart + 2, jStart + 2);
      }

      override
      protected int compare(int i, int j)
      {
         int iStart = 3 * i;
         int jStart = 3 * j;

         // First min:
         int iMin = arrayToSort[iStart + 1];
         int jMin = arrayToSort[jStart + 1];
         if (iMin < jMin)
         {
            return -1;
         }
         else if (iMin > jMin)
         {
            return 1;
         }

         // Then max:
         int iMax = arrayToSort[iStart + 2];
         int jMax = arrayToSort[jStart + 2];
         if (iMax < jMax)
         {
            return -1;
         }
         else if (iMax > jMax)
         {
            return 1;
         }

         // Then dest:
         int iDest = arrayToSort[iStart];
         int jDest = arrayToSort[jStart];
         if (iDest < jDest)
         {
            return -1;
         }
         else if (iDest > jDest)
         {
            return 1;
         }

         return 0;
      }
   }

   /** Sorts transitions by min label, ascending, then max label ascending, then dest ascending */
   private Sorter<int> minMaxDestSorter = new _MinMaxDestSorter();

   /** Initialize the provided Transition to iterate through all transitions
   *  leaving the specified state.  You must call {@link #getNextTransition} to
   *  get each transition.  Returns the number of transitions
   *  leaving this state. */
   public int initTransition(int state, Transition t)
   {
      Debug.Assert(state < nextState / 2, "state=" + state + " nextState=" + nextState);
      t.source = state;
      t.transitionUpto = states[2 * state];
      return getNumTransitions(state);
   }

   /** Iterate to the next transition after the provided one */
   public void getNextTransition(Transition t)
   {
      // Make sure there is still a transition left:
      Debug.Assert((t.transitionUpto + 3 - states[2 * t.source]) <= 3 * states[2 * t.source + 1]);
      t.dest = transitions[t.transitionUpto++];
      t.min = transitions[t.transitionUpto++];
      t.max = transitions[t.transitionUpto++];
   }

   /** Fill the provided {@link Transition} with the index'th
    *  transition leaving the specified state. */
   public void getTransition(int state, int index, Transition t)
   {
      int i = states[2 * state] + 3 * index;
      t.source = state;
      t.dest = transitions[i++];
      t.min = transitions[i++];
      t.max = transitions[i++];
   }

   static void appendCharString(int c, StringBuilder b)
   {
      if (c >= 0x21 && c <= 0x7e && c != '\\' && c != '"') b.Append((Char)c); // b.AppendCodePoint(c);
      else
      {
         b.Append("\\\\U");
         String s = c.ToString("X"); // Integer.toHexString(c);
         if (c < 0x10) b.Append("0000000").Append(s);
         else if (c < 0x100) b.Append("000000").Append(s);
         else if (c < 0x1000) b.Append("00000").Append(s);
         else if (c < 0x10000) b.Append("0000").Append(s);
         else if (c < 0x100000) b.Append("000").Append(s);
         else if (c < 0x1000000) b.Append("00").Append(s);
         else if (c < 0x10000000) b.Append("0").Append(s);
         else b.Append(s);
      }
   }

   /*
   public void writeDot(String fileName) {
     if (fileName.indexOf('/') == -1) {
       fileName = "/l/la/lucene/core/" + fileName + ".dot";
     }
     try {
       PrintWriter pw = new PrintWriter(fileName);
       pw.println(toDot());
       pw.close();
     } catch (IOException ioe) {
       throw new RuntimeException(ioe);
     }
   }
   */

   /** Returns the dot (graphviz) representation of this automaton.
    *  This is extremely useful for visualizing the automaton. */
   public String toDot()
   {
      // TODO: breadth first search so we can see get layered output...

      StringBuilder b = new StringBuilder();
      b.Append("digraph Automaton {\n");
      b.Append("  rankdir = LR\n");
      int numStates = getNumStates();
      if (numStates > 0)
      {
         b.Append("  initial [shape=plaintext,label=\"0\"]\n");
         b.Append("  initial -> 0\n");
      }

      Transition t = new Transition();

      for (int state = 0; state < numStates; state++)
      {
         b.Append("  ");
         b.Append(state);
         if (isAccept(state))
         {
            b.Append(" [shape=doublecircle,label=\"" + state + "\"]\n");
         }
         else
         {
            b.Append(" [shape=circle,label=\"" + state + "\"]\n");
         }
         int numTransitions = initTransition(state, t);
         //System.out.println("toDot: state " + state + " has " + numTransitions + " transitions; t.nextTrans=" + t.transitionUpto);
         for (int i = 0; i < numTransitions; i++)
         {
            getNextTransition(t);
            //System.out.println("  t.nextTrans=" + t.transitionUpto);
            Debug.Assert(t.max >= t.min);
            b.Append("  ");
            b.Append(state);
            b.Append(" -> ");
            b.Append(t.dest);
            b.Append(" [label=\"");
            appendCharString(t.min, b);
            if (t.max != t.min)
            {
               b.Append('-');
               appendCharString(t.max, b);
            }
            b.Append("\"]\n");
            //System.out.println("  t=" + t);
         }
      }
      b.Append('}');
      return b.ToString();
   }

   /**
    * Returns sorted array of all interval start points.
    */
   public int[] getStartPoints()
   {
      // Set<Integer> pointset = new HashSet<>();
      var pointset = new HashSet<int>();
      pointset.Add(0); // Character.MIN_CODE_POINT);
      //System.out.println("getStartPoints");
      for (int s = 0; s < nextState; s += 2)
      {
         int trans = states[s];
         int limit = trans + 3 * states[s + 1];
         //System.out.println("  state=" + (s/2) + " trans=" + trans + " limit=" + limit);
         while (trans < limit)
         {
            int min = transitions[trans + 1];
            int max = transitions[trans + 2];
            //System.out.println("    min=" + min);
            pointset.Add(min);
            if (max < 0)
            { // Character.MAX_CODE_POINT) {
               pointset.Add(max + 1);
            }
            trans += 3;
         }
      }
      int[] points = new int[pointset.Count];
      int n = 0;
      // for (Integer m : pointset) {
      foreach (var m in pointset)
      {
         points[n++] = m;
      }
      //Arrays.sort(points);
      Array.Sort(points);
      return points;
   }

   /**
    * Performs lookup in transitions, assuming determinism.
    * 
    * @param state starting state
    * @param label codepoint to look up
    * @return destination state, -1 if no matching outgoing transition
    */
   public int step(int state, int label)
   {
      Debug.Assert(state >= 0);
      Debug.Assert(label >= 0);
      int trans = states[2 * state];
      int limit = trans + 3 * states[2 * state + 1];
      // TODO: we could do bin search; transitions are sorted
      while (trans < limit)
      {
         // int dest = transitions[trans];
         int min = transitions[trans + 1];
         //int max = transitions[trans + 2];
         if (min <= label && label <= transitions[trans + 2] /* max */)
         {
            return transitions[trans];
            //return dest;
         }
         trans += 3;
      }

      return -1;
   }

   // MJ: implemented binary search as suggested by above todo.
   // for now i can't see any improvement, makes sense I think because
   // the number of state transitions isn't high and instead of branches
   // we're doing more arithmetic.
   public int step_binsearch(int state, int label, bool binsearch = false)
   {
      state *= 2;
      int trans = states[state];
      int limit = trans + 3 * states[state + 1];

      int mid = trans + 3 * (((limit - trans) / 3) / 2);

      bool stopsearch = false;
      while (mid < limit)
      {
         int min = transitions[mid + 1];

         if (min == label || stopsearch)
         {
            // todo: binary search over max?
            if (label <= transitions[mid + 2])
            {
               return transitions[mid];
            }
            stopsearch = true;
            mid += 3;
            continue;
         }
         else if (min < label)
         {
            if (label <= transitions[mid + 2])
            {
               return transitions[mid];
            }

            trans = mid + 3;
         }
         else
         {
            limit = mid;
         }

         mid = trans + 3 * (((limit - trans) / 3) / 2);
      }

      return -1;
   }

   /** Records new states and transitions and then {@link
    *  #finish} creates the {@link Automaton}.  Use this
    *  when you cannot create the Automaton directly because
    *  it's too restrictive to have to add all transitions
    *  leaving each state at once. */
   public class Builder
   {
      private int[] transitions = new int[4];
      private int nextTransition;
      private readonly Automaton a = new Automaton();

      /** Sole constructor. */
      public Builder()
      {
      }

      /** Add a new transition with min = max = label. */
      public void addTransition(int source, int dest, int label)
      {
         addTransition(source, dest, label, label);
      }

      /** Add a new transition with the specified source, dest, min, max. */
      public void addTransition(int source, int dest, int min, int max)
      {
         if (transitions.Length < nextTransition + 4)
         {
            transitions = ArrayUtil.grow(transitions, nextTransition + 4);
         }
         transitions[nextTransition++] = source;
         transitions[nextTransition++] = dest;
         transitions[nextTransition++] = min;
         transitions[nextTransition++] = max;
      }

      private class _TransitionSorter : InPlaceMergeSorter<int>
      {
         private void swapOne(int i, int j)
         {
            int x = arrayToSort[i];
            arrayToSort[i] = arrayToSort[j];
            arrayToSort[j] = x;
         }

         override
         protected void swap(int i, int j)
         {
            int iStart = 4 * i;
            int jStart = 4 * j;
            swapOne(iStart, jStart);
            swapOne(iStart + 1, jStart + 1);
            swapOne(iStart + 2, jStart + 2);
            swapOne(iStart + 3, jStart + 3);
         }

         override
         protected int compare(int i, int j)
         {
            int iStart = 4 * i;
            int jStart = 4 * j;

            // First src:
            int iSrc = arrayToSort[iStart];
            int jSrc = arrayToSort[jStart];
            if (iSrc < jSrc)
            {
               return -1;
            }
            else if (iSrc > jSrc)
            {
               return 1;
            }

            // Then min:
            int iMin = arrayToSort[iStart + 2];
            int jMin = arrayToSort[jStart + 2];
            if (iMin < jMin)
            {
               return -1;
            }
            else if (iMin > jMin)
            {
               return 1;
            }

            // Then max:
            int iMax = arrayToSort[iStart + 3];
            int jMax = arrayToSort[jStart + 3];
            if (iMax < jMax)
            {
               return -1;
            }
            else if (iMax > jMax)
            {
               return 1;
            }

            // First dest:
            int iDest = arrayToSort[iStart + 1];
            int jDest = arrayToSort[jStart + 1];
            if (iDest < jDest)
            {
               return -1;
            }
            else if (iDest > jDest)
            {
               return 1;
            }

            return 0;
         }
      }


      /** Sorts transitions first then min label ascending, then
       *  max label ascending, then dest ascending */
      private readonly Sorter<int> sorter = new _TransitionSorter();

      /** Compiles all added states and transitions into a new {@code Automaton}
     *  and returns it. */
      public Automaton finish()
      {
         //System.out.println("LA.Builder.finish: count=" + (nextTransition/4));
         // TODO: we could make this more efficient,
         // e.g. somehow xfer the int[] to the automaton, or
         // alloc exactly the right size from the automaton
         //System.out.println("finish pending");
         sorter.sort(transitions, 0, nextTransition / 4);
         int upto = 0;
         while (upto < nextTransition)
         {
            a.addTransition(transitions[upto],
                            transitions[upto + 1],
                            transitions[upto + 2],
                            transitions[upto + 3]);
            upto += 4;
         }

         a.finishState();
         return a;
      }

      /** Create a new state. */
      public int createState()
      {
         return a.createState();
      }

      /** Set or clear this state as an accept state. */
      public void setAccept(int state, bool accept)
      {
         a.setAccept(state, accept);
      }

      /** Returns true if this state is an accept state. */
      public bool isAccept(int state)
      {
         return a.isAccept(state);
      }

      /** How many states this automaton has. */
      public int getNumStates()
      {
         return a.getNumStates();
      }

      /** Copies over all states/transitions from other. */
      public void copy(Automaton other)
      {
         int offset = getNumStates();
         int otherNumStates = other.getNumStates();
         for (int s = 0; s < otherNumStates; s++)
         {
            int newState = createState();
            setAccept(newState, other.isAccept(s));
         }
         Transition t = new Transition();
         for (int s = 0; s < otherNumStates; s++)
         {
            int count = other.initTransition(s, t);
            for (int i = 0; i < count; i++)
            {
               other.getNextTransition(t);
               addTransition(offset + s, offset + t.dest, t.min, t.max);
            }
         }
      }
   }
}
