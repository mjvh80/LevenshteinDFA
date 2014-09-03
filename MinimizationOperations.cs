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

/**
 * Operations for minimizing automata.
 * 
 * @lucene.experimental
 */
using System.Collections.Generic;
public class MinimizationOperations
{

   private MinimizationOperations() { }

   /**
    * Minimizes (and determinizes if not already deterministic) the given
    * automaton.
    */
   public static Automaton minimize(Automaton a)
   {
      return minimizeHopcroft(a);
   }

   /**
    * Minimizes the given automaton using Hopcroft's algorithm.
    */
   public static Automaton minimizeHopcroft(Automaton a)
   {
      if (a.getNumStates() == 0 || (a.isAccept(0) == false && a.getNumTransitions(0) == 0))
      {
         // Fastmatch for common case
         return new Automaton();
      }
      a = Operations.determinize(a);
      //a.writeDot("adet");
      if (a.getNumTransitions(0) == 1)
      {
         Transition t = new Transition();
         a.getTransition(0, 0, t);
         if (t.dest == 0 && t.min == CodePoints.MIN_CODE_POINT
             && t.max == CodePoints.MAX_CODE_POINT)
         {
            // Accepts all strings
            return a;
         }
      }
      a = Operations.totalize(a);
      //a.writeDot("atot");

      // initialize data structures
      int[] sigma = a.getStartPoints();
      int sigmaLen = sigma.Length, statesLen = a.getNumStates();

      var reverse = new List<int>[statesLen, sigmaLen];
      //ArrayList<Integer>[][] reverse =
      //  (ArrayList<Integer>[][]) new ArrayList[statesLen][sigmaLen];

      var partition = new HashSet<int>[statesLen];
      //HashSet<Integer>[] partition =
      // (HashSet<Integer>[]) new HashSet[statesLen];

      var splitblock = new List<int>[statesLen];
      //ArrayList<Integer>[] splitblock =
      // (ArrayList<Integer>[]) new ArrayList[statesLen];

      int[] block = new int[statesLen];
      var active = new StateList[statesLen, sigmaLen];
      var active2 = new StateListNode[statesLen, sigmaLen];
      LinkedList<Tuple<int, int>> pending = new LinkedList<Tuple<int, int>>();
      BitSet pending2 = new BitSet(sigmaLen * statesLen);
      BitSet split = new BitSet(statesLen),
        refine = new BitSet(statesLen), refine2 = new BitSet(statesLen);
      for (int q = 0; q < statesLen; q++)
      {
         splitblock[q] = new List<int>(); // new ArrayList<>();
         partition[q] = new HashSet<int>(); // new HashSet<>();
         for (int x = 0; x < sigmaLen; x++)
         {
            active[q, x] = new StateList();
            //active[q][x] = new StateList();
         }
      }
      // find initial partition and reverse edges
      for (int q = 0; q < statesLen; q++)
      {
         int j = a.isAccept(q) ? 0 : 1;
         partition[j].Add(q);
         block[q] = j;
         for (int x = 0; x < sigmaLen; x++)
         {
            var r = reverse[a.step(q, sigma[x]), x];

            if (r == null)
            {
               r = reverse[a.step(q, sigma[x]), x] = new List<int>();
            }
            r.Add(q);

            //if (r[x] == null)
            //{
            //   r[x] = new List<int>(); // new ArrayList<>();
            //}
            //r[x].Add(q);
         }
      }
      // initialize active sets
      for (int j = 0; j <= 1; j++)
      {
         for (int x = 0; x < sigmaLen; x++)
         {
            foreach (int q in partition[j])
            {
               //  for (int q : partition[j]) {
               if (reverse[q, x] != null)
               {
                  active2[q, x] = active[j, x].add(q);
               }
            }
         }
      }

      // initialize pending
      for (int x = 0; x < sigmaLen; x++)
      {
         int j = (active[0, x].size <= active[1, x].size) ? 0 : 1;
         pending.AddLast(Tuple.Create(j, x)); // (new IntPair(j, x));
         pending2.Set(x * statesLen + j);
      }

      // process pending until fixed point
      int k = 2;
      //System.out.println("start min");
      while (pending.Count > 0)
      { // (!pending.isEmpty()) {
         //System.out.println("  cycle pending");
         var ip = pending.First.Value; pending.RemoveFirst(); // pending.removeFirst();
         int p = ip.Item1;
         int x = ip.Item2;
         //System.out.println("    pop n1=" + ip.n1 + " n2=" + ip.n2);
         pending2.Clear(x * statesLen + p);
         // find states that need to be split off their blocks
         for (StateListNode m = active[p, x].first; m != null; m = m.next)
         {
            var r = reverse[m.q, x];
            if (r != null)
            {
               //for (int i : r) {
               foreach (int i in r)
               {
                  if (!split.Get(i))
                  {
                     split.Set(i);
                     int j = block[i];
                     splitblock[j].Add(i);
                     if (!refine2.Get(j))
                     {
                        refine2.Set(j);
                        refine.Set(j);
                     }
                  }
               }
            }
         }

         // refine blocks
         for (int j = refine.NextSetBit(0); j >= 0; j = refine.NextSetBit(j + 1))
         {
            var sb = splitblock[j];
            if (sb.Count < partition[j].Count)
            {
               var b1 = partition[j];
               var b2 = partition[k];
               // for (int s : sb) {
               foreach (int s in sb)
               {
                  b1.Remove(s);
                  b2.Add(s);
                  block[s] = k;
                  for (int c = 0; c < sigmaLen; c++)
                  {
                     StateListNode sn = active2[s, c];
                     if (sn != null && sn.sl == active[j, c])
                     {
                        sn.remove();
                        active2[s, c] = active[k, c].add(s);
                     }
                  }
               }
               // update pending
               for (int c = 0; c < sigmaLen; c++)
               {
                  int aj = active[j, c].size,
                    ak = active[k, c].size,
                    ofs = c * statesLen;
                  if (!pending2.Get(ofs + j) && 0 < aj && aj <= ak)
                  {
                     pending2.Set(ofs + j);
                     pending.AddLast(Tuple.Create(j, c));
                  }
                  else
                  {
                     pending2.Set(ofs + k);
                     pending.AddLast(Tuple.Create(k, c));
                  }
               }
               k++;
            }
            refine2.Clear(j);
            //for (int s : sb) {
            foreach (int s in sb)
            {
               split.Clear(s);
            }
            sb.Clear();
         }
         refine.Clear();
      }

      Automaton result = new Automaton();

      Transition tInner = new Transition();

      //System.out.println("  k=" + k);

      // make a new state for each equivalence class, set initial state
      int[] stateMap = new int[statesLen];
      int[] stateRep = new int[k];

      result.createState();

      //System.out.println("min: k=" + k);
      for (int n = 0; n < k; n++)
      {
         //System.out.println("    n=" + n);

         bool isInitial = false;
         //  for (int q : partition[n]) {
         foreach (int q in partition[n])
         {
            if (q == 0)
            {
               isInitial = true;
               //System.out.println("    isInitial!");
               break;
            }
         }

         int newState;
         if (isInitial)
         {
            newState = 0;
         }
         else
         {
            newState = result.createState();
         }

         //System.out.println("  newState=" + newState);

         //  for (int q : partition[n]) {
         foreach (int q in partition[n])
         {
            stateMap[q] = newState;
            //System.out.println("      q=" + q + " isAccept?=" + a.isAccept(q));
            result.setAccept(newState, a.isAccept(q));
            stateRep[newState] = q;   // select representative
         }
      }

      // build transitions and set acceptance
      for (int n = 0; n < k; n++)
      {
         int numTransitions = a.initTransition(stateRep[n], tInner);
         for (int i = 0; i < numTransitions; i++)
         {
            a.getNextTransition(tInner);
            //System.out.println("  add trans");
            result.addTransition(n, stateMap[tInner.dest], tInner.min, tInner.max);
         }
      }
      result.finishState();
      //System.out.println(result.getNumStates() + " states");

      return Operations.removeDeadStates(result);
   }



   class StateList
   {

      public int size;

      public StateListNode first, last;

      public StateListNode add(int q)
      {
         return new StateListNode(q, this);
      }
   }

   class StateListNode
   {

      public int q;

      public StateListNode next, prev;

      public StateList sl;

      public StateListNode(int q, StateList sl)
      {
         this.q = q;
         this.sl = sl;
         if (sl.size++ == 0) sl.first = sl.last = this;
         else
         {
            sl.last.next = this;
            prev = sl.last;
            sl.last = this;
         }
      }

      public void remove()
      {
         sl.size--;
         if (sl.first == this) sl.first = next;
         else prev.next = next;
         if (sl.last == this) sl.last = prev;
         else next.prev = prev;
      }
   }
}

