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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/**
 * Automata operations.
 * 
 * @lucene.experimental
 */
public class Operations
{

   private Operations() { }

   /**
    * Returns an automaton that accepts the concatenation of the languages of the
    * given automata.
    * <p>
    * Complexity: linear in total number of states.
    */
   static public Automaton concatenate(Automaton a1, Automaton a2)
   {
      //return concatenate(Arrays.asList(a1, a2));
      return concatenate(new List<Automaton> { a1, a2 });
   }

   /**
    * Returns an automaton that accepts the concatenation of the languages of the
    * given automata.
    * <p>
    * Complexity: linear in total number of states.
    */
   static public Automaton concatenate(List<Automaton> l)
   {
      Automaton result = new Automaton();

      // First pass: create all states
      // for(Automaton a : l) {
      foreach (Automaton a in l)
      {
         if (a.getNumStates() == 0)
         {
            result.finishState();
            return result;
         }
         int numStates = a.getNumStates();
         for (int s = 0; s < numStates; s++)
         {
            result.createState();
         }
      }

      // Second pass: add transitions, carefully linking accept
      // states of A to init state of next A:
      int stateOffset = 0;
      Transition t = new Transition();
      for (int i = 0; i < l.Count; i++)
      {
         Automaton a = l[i]; //.get(i);
         int numStates = a.getNumStates();

         Automaton nextA = (i == l.Count - 1) ? null : l[i + 1];

         for (int s = 0; s < numStates; s++)
         {
            int numTransitions = a.initTransition(s, t);
            for (int j = 0; j < numTransitions; j++)
            {
               a.getNextTransition(t);
               result.addTransition(stateOffset + s, stateOffset + t.dest, t.min, t.max);
            }

            if (a.isAccept(s))
            {
               Automaton followA = nextA;
               int followOffset = stateOffset;
               int upto = i + 1;
               while (true)
               {
                  if (followA != null)
                  {
                     // Adds a "virtual" epsilon transition:
                     numTransitions = followA.initTransition(0, t);
                     for (int j = 0; j < numTransitions; j++)
                     {
                        followA.getNextTransition(t);
                        result.addTransition(stateOffset + s, followOffset + numStates + t.dest, t.min, t.max);
                     }
                     if (followA.isAccept(0))
                     {
                        // Keep chaining if followA accepts empty string
                        followOffset += followA.getNumStates();
                        followA = (upto == l.Count - 1) ? null : l[upto + 1];
                        upto++;
                     }
                     else
                     {
                        break;
                     }
                  }
                  else
                  {
                     result.setAccept(stateOffset + s, true);
                     break;
                  }
               }
            }
         }

         stateOffset += numStates;
      }

      if (result.getNumStates() == 0)
      {
         result.createState();
      }

      result.finishState();

      return result;
   }

   /**
    * Returns an automaton that accepts the union of the empty string and the
    * language of the given automaton.
    * <p>
    * Complexity: linear in number of states.
    */
   static public Automaton optional(Automaton a)
   {
      Automaton result = new Automaton();
      result.createState();
      result.setAccept(0, true);
      if (a.getNumStates() > 0)
      {
         result.copy(a);
         result.addEpsilon(0, 1);
      }
      result.finishState();
      return result;
   }

   /**
    * Returns an automaton that accepts the Kleene star (zero or more
    * concatenated repetitions) of the language of the given automaton. Never
    * modifies the input automaton language.
    * <p>
    * Complexity: linear in number of states.
    */
   static public Automaton repeat(Automaton a)
   {
      Automaton.Builder builder = new Automaton.Builder();
      builder.createState();
      builder.setAccept(0, true);
      builder.copy(a);

      Transition t = new Transition();
      int count = a.initTransition(0, t);
      for (int i = 0; i < count; i++)
      {
         a.getNextTransition(t);
         builder.addTransition(0, t.dest + 1, t.min, t.max);
      }

      int numStates = a.getNumStates();
      for (int s = 0; s < numStates; s++)
      {
         if (a.isAccept(s))
         {
            count = a.initTransition(0, t);
            for (int i = 0; i < count; i++)
            {
               a.getNextTransition(t);
               builder.addTransition(s + 1, t.dest + 1, t.min, t.max);
            }
         }
      }

      return builder.finish();
   }

   /**
    * Returns an automaton that accepts <code>min</code> or more concatenated
    * repetitions of the language of the given automaton.
    * <p>
    * Complexity: linear in number of states and in <code>min</code>.
    */
   static public Automaton repeat(Automaton a, int min)
   {
      if (min == 0)
      {
         return repeat(a);
      }
      List<Automaton> @as = new List<Automaton>(); // new ArrayList<>();
      while (min-- > 0)
      {
         @as.Add(a);
      }
      @as.Add(repeat(a));
      return concatenate(@as);
   }

#if TODO_PORT
  /**
   * Returns an automaton that accepts between <code>min</code> and
   * <code>max</code> (including both) concatenated repetitions of the language
   * of the given automaton.
   * <p>
   * Complexity: linear in number of states and in <code>min</code> and
   * <code>max</code>.
   */
  static public Automaton repeat(Automaton a, int min, int max) {
    if (min > max) {
      return Automata.makeEmpty();
    }

    Automaton b;
    if (min == 0) {
      b = Automata.makeEmptyString();
    } else if (min == 1) {
      b = new Automaton();
      b.copy(a);
    } else {
      List<Automaton> as = new ArrayList<>();
      for(int i=0;i<min;i++) {
        as.add(a);
      }
      b = concatenate(as);
    }

    Set<Integer> prevAcceptStates = toSet(b, 0);

    for(int i=min;i<max;i++) {
      int numStates = b.getNumStates();
      b.copy(a);
      for(int s : prevAcceptStates) {
        b.addEpsilon(s, numStates);
      }
      prevAcceptStates = toSet(a, numStates);
    }

    b.finishState();

    return b;
  }


  private static HashSet<int> toSet(Automaton a, int offset) {
    int numStates = a.getNumStates();
    var isAccept = a.getAcceptStates();
    var result = new HashSet<int>();
    int upto = 0;
    while (upto < numStates && (upto = isAccept.nextSetBit(upto)) != -1) {
      result.add(offset+upto);
      upto++;
    }

    return result;
  }

  
  /**
   * Returns a (deterministic) automaton that accepts the complement of the
   * language of the given automaton.
   * <p>
   * Complexity: linear in number of states (if already deterministic).
   */
  static public Automaton complement(Automaton a) {
    a = totalize(determinize(a));
    int numStates = a.getNumStates();
    for (int p=0;p<numStates;p++) {
      a.setAccept(p, !a.isAccept(p));
    }
    return removeDeadStates(a);
  }


  /**
   * Returns a (deterministic) automaton that accepts the intersection of the
   * language of <code>a1</code> and the complement of the language of
   * <code>a2</code>. As a side-effect, the automata may be determinized, if not
   * already deterministic.
   * <p>
   * Complexity: quadratic in number of states (if already deterministic).
   */
  static public Automaton minus(Automaton a1, Automaton a2) {
    if (Operations.isEmpty(a1) || a1 == a2) {
      return Automaton.makeEmpty();
    }
    if (Operations.isEmpty(a2)) {
      return a1;
    }
    return intersection(a1, complement(a2));
  }
#endif

   private class StatePair
   {
      public int State;
      public Tuple<int, int> Pair;

      public StatePair(int state, int left, int right)
      {
         State = state;
         Pair = Tuple.Create(left, right);
      }

      public override bool Equals(object obj)
      {
         var other = obj as StatePair;
         if (other == null) return false;

         return this.Pair.Equals(other.Pair);
      }

      public override int GetHashCode()
      {
         return Pair.GetHashCode();
      }

      public int Item1 { get { return Pair.Item1; } }
      public int Item2 { get { return Pair.Item2; } }
      public int s1 { get { return Pair.Item1; } }
      public int s2 { get { return Pair.Item2; } }
   }

   /**
    * Returns an automaton that accepts the intersection of the languages of the
    * given automata. Never modifies the input automata languages.
    * <p>
    * Complexity: quadratic in number of states.
    */
   static public Automaton intersection(Automaton a1, Automaton a2)
   {
      if (a1 == a2)
      {
         return a1;
      }
      if (a1.getNumStates() == 0)
      {
         return a1;
      }
      if (a2.getNumStates() == 0)
      {
         return a2;
      }
      Transition[][] transitions1 = a1.getSortedTransitions();
      Transition[][] transitions2 = a2.getSortedTransitions();
      Automaton c = new Automaton();
      c.createState();
      LinkedList<StatePair> worklist = new LinkedList<StatePair>();
      //  HashMap<StatePair,StatePair> newstates = new HashMap<>();
      Dictionary<StatePair, StatePair> newstates = new Dictionary<StatePair, StatePair>();

      //  StatePair p = new StatePair(0, 0, 0);



      var p = new StatePair(0, 0, 0);

      worklist.AddLast(p);
      newstates.Add(p, p);
      //newstates.put(p, p);
      while (worklist.Count > 0)
      {
         p = worklist.First.Value; worklist.RemoveFirst();
         c.setAccept(p.State, a1.isAccept(p.Item1) && a2.isAccept(p.Item2));
         Transition[] t1 = transitions1[p.Item1];
         Transition[] t2 = transitions2[p.Item2];
         for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
         {
            while (b2 < t2.Length && t2[b2].max < t1[n1].min)
               b2++;
            for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
               if (t2[n2].max >= t1[n1].min)
               {
                  //StatePair q = new StatePair(t1[n1].dest, t2[n2].dest);
                  var q = new StatePair(-1, t1[n1].dest, t2[n2].dest);
                  //   var r = newstates[q];
                  StatePair r = null;
                  if (!newstates.TryGetValue(q, out r))
                  {
                     q.State = c.createState();
                     worklist.AddLast(q);
                     newstates.Add(q, q);
                     r = q;
                  }
                  int min = t1[n1].min > t2[n2].min ? t1[n1].min : t2[n2].min;
                  int max = t1[n1].max < t2[n2].max ? t1[n1].max : t2[n2].max;
                  c.addTransition(p.State, r.State, min, max);
               }
         }
      }
      c.finishState();

      return removeDeadStates(c);
   }

   /** Returns true if these two automata accept exactly the
    *  same language.  This is a costly computation!  Note
    *  also that a1 and a2 will be determinized as a side
    *  effect.  Both automata must be determinized and have
    *  no dead states! */
   public static bool sameLanguage(Automaton a1, Automaton a2)
   {
      if (a1 == a2)
      {
         return true;
      }
      return subsetOf(a2, a1) && subsetOf(a1, a2);
   }

   // TODO: move to test-framework?
   /** Returns true if this automaton has any states that cannot
    *  be reached from the initial state or cannot reach an accept state.
    *  Cost is O(numTransitions+numStates). */
   public static bool hasDeadStates(Automaton a)
   {
      var liveStates = getLiveStates(a);
      int numLive = liveStates.Cardinality; // liveStates.cardinality();
      int numStates = a.getNumStates();
      //   assert numLive <= numStates: "numLive=" + numLive + " numStates=" + numStates + " " + liveStates;
      return numLive < numStates;
   }

   // TODO: move to test-framework?
   /** Returns true if there are dead states reachable from an initial state. */
   public static bool hasDeadStatesFromInitial(Automaton a)
   {
      var reachableFromInitial = getLiveStatesFromInitial(a);
      var reachableFromAccept = getLiveStatesToAccept(a);
      //reachableFromInitial.andNot(reachableFromAccept);

      // reachableFromInitial.And(reachableFromAccept.Not());
      reachableFromInitial.AndNot(reachableFromAccept);

      // return reachableFromInitial.isEmpty() == false;
      // return reachableFromInitial.Count > 0;

      return reachableFromInitial.AnyBitSet;
   }

   // TODO: move to test-framework?
   /** Returns true if there are dead states that reach an accept state. */
   public static bool hasDeadStatesToAccept(Automaton a)
   {
      var reachableFromInitial = getLiveStatesFromInitial(a);
      var reachableFromAccept = getLiveStatesToAccept(a);
      // reachableFromAccept.andNot(reachableFromInitial);
      reachableFromAccept.AndNot(reachableFromInitial);
      // return reachableFromAccept.Count > 0;
      //   return reachableFromAccept.isEmpty() == false;
      return reachableFromAccept.AnyBitSet;
   }

   /**
    * Returns true if the language of <code>a1</code> is a subset of the language
    * of <code>a2</code>. Both automata must be determinized and must have no dead
    * states.
    * <p>
    * Complexity: quadratic in number of states.
    */
   public static bool subsetOf(Automaton a1, Automaton a2)
   {
      if (a1.isDeterministic() == false)
      {
         throw new ArgumentException("a1 must be deterministic");
      }
      if (a2.isDeterministic() == false)
      {
         throw new ArgumentException("a2 must be deterministic");
      }
      //    assert hasDeadStatesFromInitial(a1) == false;
      //    assert hasDeadStatesFromInitial(a2) == false;
      if (a1.getNumStates() == 0)
      {
         // Empty language is alwyas a subset of any other language
         return true;
      }
      else if (a2.getNumStates() == 0)
      {
         return isEmpty(a1);
      }

      // TODO: cutover to iterators instead
      Transition[][] transitions1 = a1.getSortedTransitions();
      Transition[][] transitions2 = a2.getSortedTransitions();
      LinkedList<StatePair> worklist = new LinkedList<StatePair>();
      HashSet<StatePair> visited = new HashSet<StatePair>();
      StatePair p = new StatePair(-1, 0, 0);
      worklist.AddLast(p);
      visited.Add(p);
      while (worklist.Count > 0)
      {
         p = worklist.First.Value; worklist.RemoveFirst(); // worklist.removeFirst();
         if (a1.isAccept(p.s1) && a2.isAccept(p.s2) == false)
         {
            return false;
         }
         Transition[] t1 = transitions1[p.s1];
         Transition[] t2 = transitions2[p.s2];
         for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
         {
            while (b2 < t2.Length && t2[b2].max < t1[n1].min)
            {
               b2++;
            }
            int min1 = t1[n1].min, max1 = t1[n1].max;

            for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
            {
               if (t2[n2].min > min1)
               {
                  return false;
               }
               if (t2[n2].max < CodePoints.MAX_CODE_POINT)
               { // Character.MAX_CODE_POINT) {
                  min1 = t2[n2].max + 1;
               }
               else
               {
                  min1 = CodePoints.MAX_CODE_POINT; // Character.MAX_CODE_POINT;
                  max1 = CodePoints.MAX_CODE_POINT; // Character.MIN_CODE_POINT;
               }
               StatePair q = new StatePair(-1, t1[n1].dest, t2[n2].dest);
               if (!visited.Contains(q))
               {
                  worklist.AddLast(q);
                  visited.Add(q);
               }
            }
            if (min1 <= max1)
            {
               return false;
            }
         }
      }
      return true;
   }

   /**
    * Returns an automaton that accepts the union of the languages of the given
    * automata.
    * <p>
    * Complexity: linear in number of states.
    */
   public static Automaton union(Automaton a1, Automaton a2)
   {
      // return union(Arrays.asList(a1, a2));
      return union(new List<Automaton> { a1, a2 });
   }

   /**
    * Returns an automaton that accepts the union of the languages of the given
    * automata.
    * <p>
    * Complexity: linear in number of states.
    */
   public static Automaton union(IEnumerable<Automaton> l)
   {
      Automaton result = new Automaton();

      // Create initial state:
      result.createState();

      // Copy over all automata
      Transition t = new Transition();
      foreach (Automaton a in l)
      {
         result.copy(a);
      }

      // Add epsilon transition from new initial state
      int stateOffset = 1;
      foreach (Automaton a in l)
      {
         if (a.getNumStates() == 0)
         {
            continue;
         }
         result.addEpsilon(0, stateOffset);
         stateOffset += a.getNumStates();
      }

      result.finishState();

      return removeDeadStates(result);
   }

   // Simple custom ArrayList<Transition>
   private class TransitionList
   {
      // dest, min, max
      public int[] transitions = new int[3];
      public int next;

      public void add(Transition t)
      {
         if (transitions.Length < next + 3)
         {
            transitions = ArrayUtil.grow(transitions, next + 3);
         }
         transitions[next] = t.dest;
         transitions[next + 1] = t.min;
         transitions[next + 2] = t.max;
         next += 3;
      }
   }

   // Holds all transitions that start on this int point, or
   // end at this point-1
   private class PointTransitions : IComparable<PointTransitions>, IEquatable<PointTransitions>
   { // Comparable<PointTransitions> {
      public int point;
      public TransitionList ends = new TransitionList();
      public TransitionList starts = new TransitionList();

      //@Override
      //public int compareTo(PointTransitions other) {
      //  return point - other.point;
      //}

      public void reset(int point)
      {
         this.point = point;
         ends.next = 0;
         starts.next = 0;
      }

      override
      public bool Equals(Object other)
      {
         return ((PointTransitions)other).point == point;
      }

      public override int GetHashCode()
      {
         return point;
      }

      public int CompareTo(PointTransitions other)
      {
         return point - other.point;
      }

      public bool Equals(PointTransitions other)
      {
         return other.point == point;
      }
   }

   private class PointTransitionSet
   {
      public int count;
      public PointTransitions[] points = new PointTransitions[5];

      private static readonly int HASHMAP_CUTOVER = 30;
      //private HashMap<Integer,PointTransitions> map = new HashMap<>();
      private Dictionary<int, PointTransitions> map = new Dictionary<int, PointTransitions>();

      private bool useHash = false;

      private PointTransitions next(int point)
      {
         // 1st time we are seeing this point
         if (count == points.Length)
         {
            PointTransitions[] newArray = new PointTransitions[ArrayUtil.oversize(1 + count, 4 /* TODO RamUsageEstimator.NUM_BYTES_OBJECT_REF */)];
            // System.arraycopy(points, 0, newArray, 0, count);
            Array.Copy(points, 0, newArray, 0, count);
            points = newArray;
         }
         PointTransitions points0 = points[count];
         if (points0 == null)
         {
            points0 = points[count] = new PointTransitions();
         }
         points0.reset(point);
         count++;
         return points0;
      }

      private PointTransitions find(int point)
      {
         if (useHash)
         {
            int pi = point;
            PointTransitions p; // = map.get(pi);
            //if (p == null) {
            if (!map.TryGetValue(pi, out p))
            {
               p = next(point);
               map.Add(pi, p);
            }
            return p;
         }
         else
         {
            for (int i = 0; i < count; i++)
            {
               if (points[i].point == point)
               {
                  return points[i];
               }
            }

            PointTransitions p = next(point);
            if (count == HASHMAP_CUTOVER)
            {
               // switch to HashMap on the fly
               Debug.Assert(map.Count == 0);
               for (int i = 0; i < count; i++)
               {
                  map.Add(points[i].point, points[i]);
               }
               useHash = true;
            }
            return p;
         }
      }

      public void reset()
      {
         if (useHash)
         {
            map.Clear();
            useHash = false;
         }
         count = 0;
      }

      public void sort()
      {
         if (count > 1) Array.Sort(points, 0, count); // todo for perf could copy tim (?) sort

         //// Tim sort performs well on already sorted arrays:
         //if (count > 1) ArrayUtil.timSort(points, 0, count);
      }

      public void add(Transition t)
      {
         find(t.min).starts.add(t);
         find(1 + t.max).ends.add(t);
      }

      override
     public String ToString()
      {
         StringBuilder s = new StringBuilder();
         for (int i = 0; i < count; i++)
         {
            if (i > 0)
            {
               s.Append(' ');
            }
            s.Append(points[i].point).Append(':').Append(points[i].starts.next / 3).Append(',').Append(points[i].ends.next / 3);
         }
         return s.ToString();
      }
   }

   /**
    * Determinizes the given automaton.
    * <p>
    * Worst case complexity: exponential in number of states.
    */
   public static Automaton determinize(Automaton a)
   {
      if (a.isDeterministic())
      {
         // Already determinized
         return a;
      }
      if (a.getNumStates() <= 1)
      {
         // Already determinized
         return a;
      }

      // subset construction
      Automaton.Builder b = new Automaton.Builder();

      //System.out.println("DET:");
      //a.writeDot("/l/la/lucene/core/detin.dot");

      FrozenIntSet initialset = new FrozenIntSet(0, 0);

      // Create state 0:
      b.createState();

      LinkedList<FrozenIntSet> worklist = new LinkedList<FrozenIntSet>();
      var newstate = new Dictionary<Object, int>();

      worklist.AddLast(initialset);

      b.setAccept(0, a.isAccept(0));
      newstate.Add(initialset, 0);

      int newStateUpto = 0;
      int[] newStatesArray = new int[5];
      newStatesArray[newStateUpto] = 0;
      newStateUpto++;

      // like Set<Integer,PointTransitions>
      PointTransitionSet points = new PointTransitionSet();

      // like SortedMap<Integer,Integer>
      SortedIntSet statesSet = new SortedIntSet(5);

      Transition t = new Transition();

      while (worklist.Count > 0)
      {
         var s = worklist.First.Value; worklist.RemoveFirst(); //worklist.removeFirst();
         //System.out.println("det: pop set=" + s);

         // Collate all outgoing transitions by min/1+max:
         for (int i = 0; i < s.values.Length; i++)
         {
            int s0 = s.values[i];
            int numTransitions = a.getNumTransitions(s0);
            a.initTransition(s0, t);
            for (int j = 0; j < numTransitions; j++)
            {
               a.getNextTransition(t);
               points.add(t);
            }
         }

         if (points.count == 0)
         {
            // No outgoing transitions -- skip it
            continue;
         }

         points.sort();

         int lastPoint = -1;
         int accCount = 0;

         int r = s.state;

         for (int i = 0; i < points.count; i++)
         {

            int point = points.points[i].point;

            if (statesSet.upto > 0)
            {
               Debug.Assert(lastPoint != -1);

               statesSet.computeHash();

               //Integer q = newstate.get(statesSet);
               int q;
               //  if (q == null) {
               if (!newstate.TryGetValue(statesSet, out q))
               {
                  q = b.createState();
                  FrozenIntSet p = statesSet.freeze(q);
                  //System.out.println("  make new state=" + q + " -> " + p + " accCount=" + accCount);
                  worklist.AddLast(p);
                  b.setAccept(q, accCount > 0);
                  newstate.Add(p, q);
               }
               else
               {
                  Debug.Assert((accCount > 0 ? true : false) == b.isAccept(q), "accCount=" + accCount + " vs existing accept=" +
                    b.isAccept(q) + " states=" + statesSet);
               }

               // System.out.println("  add trans src=" + r + " dest=" + q + " min=" + lastPoint + " max=" + (point-1));

               b.addTransition(r, q, lastPoint, point - 1);
            }

            // process transitions that end on this point
            // (closes an overlapping interval)
            int[] transitions = points.points[i].ends.transitions;
            int limit = points.points[i].ends.next;
            for (int j = 0; j < limit; j += 3)
            {
               int dest = transitions[j];
               statesSet.decr(dest);
               accCount -= a.isAccept(dest) ? 1 : 0;
            }
            points.points[i].ends.next = 0;

            // process transitions that start on this point
            // (opens a new interval)
            transitions = points.points[i].starts.transitions;
            limit = points.points[i].starts.next;
            for (int j = 0; j < limit; j += 3)
            {
               int dest = transitions[j];
               statesSet.incr(dest);
               accCount += a.isAccept(dest) ? 1 : 0;
            }
            lastPoint = point;
            points.points[i].starts.next = 0;
         }
         points.reset();
         Debug.Assert(statesSet.upto == 0, "upto=" + statesSet.upto);
      }

      Automaton result = b.finish();
      Debug.Assert(result.isDeterministic());
      return result;
   }

   /**
   * Returns true if the given automaton accepts no strings.
   */
   public static bool isEmpty(Automaton a)
   {
      if (a.getNumStates() == 0)
      {
         // Common case: no states
         return true;
      }
      if (a.isAccept(0) == false && a.getNumTransitions(0) == 0)
      {
         // Common case: just one initial state
         return true;
      }
      if (a.isAccept(0) == true)
      {
         // Apparently common case: it accepts the damned empty string
         return false;
      }

      LinkedList<int> workList = new LinkedList<int>();
      var seen = new BitSet(a.getNumStates());
      workList.AddLast(0);
      seen.Set(0, true);

      Transition t = new Transition();
      while (workList.Count > 0)
      { // workList.isEmpty() == false) {
         int state = workList.First.Value; workList.RemoveFirst();// workList.removeFirst();
         if (a.isAccept(state))
         {
            return false;
         }
         int count = a.initTransition(state, t);
         for (int i = 0; i < count; i++)
         {
            a.getNextTransition(t);
            if (seen.Get(t.dest) == false)
            {
               workList.AddLast(t.dest);
               seen.Set(t.dest, true);
            }
         }
      }

      return true;
   }

   /**
    * Returns true if the given automaton accepts all strings.  The automaton must be minimized.
    */
   public static bool isTotal(Automaton a)
   {
      if (a.isAccept(0) && a.getNumTransitions(0) == 1)
      {
         Transition t = new Transition();
         a.getTransition(0, 0, t);
         return t.dest == 0 && t.min == CodePoints.MIN_CODE_POINT
             && t.max == CodePoints.MAX_CODE_POINT;
      }
      return false;
   }

   /**
    * Returns true if the given string is accepted by the automaton.  The input must be deterministic.
    * <p>
    * Complexity: linear in the length of the string.
    * <p>
    * <b>Note:</b> for full performance, use the {@link RunAutomaton} class.
    */
   public static bool run(Automaton a, String s)
   {
      //  assert a.isDeterministic();
      int state = 0;
      for (int i = 0, cp = 0; i < s.Length; i += CodePoints.charCount(cp))
      { // Character.charCount(cp)) {
         int nextState = a.step_binsearch(state, cp = CodePoints.codePointAt(s, i)); // s.codePointAt(i));
         if (nextState == -1)
         {
            return false;
         }
         state = nextState;
      }
      return a.isAccept(state);
   }

#if TODO_PORT
  /**
   * Returns true if the given string (expressed as unicode codepoints) is accepted by the automaton.  The input must be deterministic.
   * <p>
   * Complexity: linear in the length of the string.
   * <p>
   * <b>Note:</b> for full performance, use the {@link RunAutomaton} class.
   */
  public static bool run(Automaton a, IntsRef s) {
    assert a.isDeterministic();
    int state = 0;
    for (int i=0;i<s.length;i++) {
      int nextState = a.step(state, s.ints[s.offset+i]);
      if (nextState == -1) {
        return false;
      }
      state = nextState;
    }
    return a.isAccept(state);
  }
#endif

   /**
   * Returns the set of live states. A state is "live" if an accept state is
   * reachable from it and if it is reachable from the initial state.
   */
   private static BitSet getLiveStates(Automaton a)
   {
      var live = getLiveStatesFromInitial(a);
      live.And(getLiveStatesToAccept(a));
      return live;
   }

   /** Returns bitset marking states reachable from the initial state. */
   private static BitSet getLiveStatesFromInitial(Automaton a)
   {
      int numStates = a.getNumStates();
      var live = new BitSet(numStates);
      if (numStates == 0)
      {
         return live;
      }
      LinkedList<int> workList = new LinkedList<int>();
      live.Set(0, true);
      workList.AddLast(0);

      Transition t = new Transition();
      while (workList.Count > 0)
      { // workList.isEmpty() == false) {
         int s = workList.First.Value; workList.RemoveFirst(); // workList.removeFirst();
         int count = a.initTransition(s, t);
         for (int i = 0; i < count; i++)
         {
            a.getNextTransition(t);
            if (live.Get(t.dest) == false)
            {
               live.Set(t.dest, true);
               workList.AddLast(t.dest);
            }
         }
      }

      return live;
   }

   /** Returns bitset marking states that can reach an accept state. */
   private static BitSet getLiveStatesToAccept(Automaton a)
   {
      Automaton.Builder builder = new Automaton.Builder();

      // NOTE: not quite the same thing as what SpecialOperations.reverse does:
      Transition t = new Transition();
      int numStates = a.getNumStates();
      for (int s = 0; s < numStates; s++)
      {
         builder.createState();
      }
      for (int s = 0; s < numStates; s++)
      {
         int count = a.initTransition(s, t);
         for (int i = 0; i < count; i++)
         {
            a.getNextTransition(t);
            builder.addTransition(t.dest, s, t.min, t.max);
         }
      }
      Automaton a2 = builder.finish();

      LinkedList<int> workList = new LinkedList<int>();
      var live = new BitSet(numStates);
      var acceptBits = a.getAcceptStates();
      int s2 = 0;
      while (s2 < numStates && (s2 = acceptBits.NextSetBit(s2)) != -1)
      {
         live.Set(s2, true);
         workList.AddLast(s2);
         s2++;
      }

      while (workList.Count > 0)
      {  // workList.isEmpty() == false) {
         s2 = workList.First.Value; workList.RemoveFirst(); // workList.removeFirst();
         int count = a2.initTransition(s2, t);
         for (int i = 0; i < count; i++)
         {
            a2.getNextTransition(t);
            if (live.Get(t.dest) == false)
            {
               live.Set(t.dest, true);
               workList.AddLast(t.dest);
            }
         }
      }

      return live;
   }

   /**
    * Removes transitions to dead states (a state is "dead" if it is not
    * reachable from the initial state or no accept state is reachable from it.)
    */
   public static Automaton removeDeadStates(Automaton a)
   {
      int numStates = a.getNumStates();
      var liveSet = getLiveStates(a);

      int[] map = new int[numStates];

      Automaton result = new Automaton();
      //System.out.println("liveSet: " + liveSet + " numStates=" + numStates);
      for (int i = 0; i < numStates; i++)
      {
         if (liveSet.Get(i))
         {
            map[i] = result.createState();
            result.setAccept(map[i], a.isAccept(i));
         }
      }

      Transition t = new Transition();

      for (int i = 0; i < numStates; i++)
      {
         if (liveSet.Get(i))
         {
            int numTransitions = a.initTransition(i, t);
            // filter out transitions to dead states:
            for (int j = 0; j < numTransitions; j++)
            {
               a.getNextTransition(t);
               if (liveSet.Get(t.dest))
               {
                  result.addTransition(map[i], map[t.dest], t.min, t.max);
               }
            }
         }
      }

      result.finishState();
      //   assert hasDeadStates(result) == false;
      return result;
   }

   /**
    * Finds the largest entry whose value is less than or equal to c, or 0 if
    * there is no such entry.
    */
   public static int findIndex(int c, int[] points)
   {
      int a = 0;
      int b = points.Length;
      while (b - a > 1)
      {
         int d = (int)((uint)(a + b) >> 1);
         if (points[d] > c) b = d;
         else if (points[d] < c) a = d;
         else return d;
      }
      return a;
   }

   /**
    * Returns true if the language of this automaton is finite.  The
    * automaton must not have any dead states.
    */
   public static bool isFinite(Automaton a)
   {
      if (a.getNumStates() == 0)
      {
         return true;
      }
      return isFinite(new Transition(), a, 0, new BitSet(a.getNumStates()), new BitSet(a.getNumStates()));
   }

   /**
    * Checks whether there is a loop containing state. (This is sufficient since
    * there are never transitions to dead states.)
    */
   // TODO: not great that this is recursive... in theory a
   // large automata could exceed java's stack
   private static bool isFinite(Transition scratch, Automaton a, int state, BitSet path, BitSet visited)
   {
      path.Set(state, true);
      int numTransitions = a.initTransition(state, scratch);
      for (int t = 0; t < numTransitions; t++)
      {
         a.getTransition(state, t, scratch);
         if (path.Get(scratch.dest) || (!visited.Get(scratch.dest) && !isFinite(scratch, a, scratch.dest, path, visited)))
         {
            return false;
         }
      }
      path.Set(state, false);
      visited.Set(state, true);
      return true;
   }

   /**
    * Returns the longest string that is a prefix of all accepted strings and
    * visits each state at most once.  The automaton must be deterministic.
    * 
    * @return common prefix
    */
   public static String getCommonPrefix(Automaton a)
   {
      if (a.isDeterministic() == false)
      {
         throw new ArgumentException("input automaton must be deterministic");
      }
      StringBuilder b = new StringBuilder();
      HashSet<int> visited = new HashSet<int>();
      int s = 0;
      bool done = false;
      Transition t = new Transition();
      do
      {
         done = true;
         visited.Add(s);
         if (a.isAccept(s) == false && a.getNumTransitions(s) == 1)
         {
            a.getTransition(s, 0, t);
            if (t.min == t.max && !visited.Contains(t.dest))
            {
               CodePoints.appendCodePoint(b, t.min);
               // b.appendCodePoint(t.min);
               s = t.dest;
               done = false;
            }
         }
      } while (!done);

      return b.ToString();
   }

#if TODO_PORT
  // TODO: this currently requires a determinized machine,
  // but it need not -- we can speed it up by walking the
  // NFA instead.  it'd still be fail fast.
  /**
   * Returns the longest BytesRef that is a prefix of all accepted strings and
   * visits each state at most once.  The automaton must be deterministic.
   * 
   * @return common prefix
   */
  public static BytesRef getCommonPrefixBytesRef(Automaton a) {
    BytesRefBuilder builder = new BytesRefBuilder();
    HashSet<Integer> visited = new HashSet<>();
    int s = 0;
    boolean done;
    Transition t = new Transition();
    do {
      done = true;
      visited.add(s);
      if (a.isAccept(s) == false && a.getNumTransitions(s) == 1) {
        a.getTransition(s, 0, t);
        if (t.min == t.max && !visited.contains(t.dest)) {
          builder.append((byte) t.min);
          s = t.dest;
          done = false;
        }
      }
    } while (!done);

    return builder.get();
  }


  /**
   * Returns the longest BytesRef that is a suffix of all accepted strings.
   * Worst case complexity: exponential in number of states (this calls
   * determinize).
   *
   * @return common suffix
   */
  public static BytesRef getCommonSuffixBytesRef(Automaton a) {
    // reverse the language of the automaton, then reverse its common prefix.
    Automaton r = Operations.determinize(reverse(a));
    BytesRef ref = getCommonPrefixBytesRef(r);
    reverseBytes(ref);
    return ref;
  }

  
  private static void reverseBytes(BytesRef ref) {
    if (ref.length <= 1) return;
    int num = ref.length >> 1;
    for (int i = ref.offset; i < ( ref.offset + num ); i++) {
      byte b = ref.bytes[i];
      ref.bytes[i] = ref.bytes[ref.offset * 2 + ref.length - i - 1];
      ref.bytes[ref.offset * 2 + ref.length - i - 1] = b;
    }
  }
#endif

   /** Returns an automaton accepting the reverse language. */
   public static Automaton reverse(Automaton a)
   {
      return reverse(a, null);
   }

   /** Reverses the automaton, returning the new initial states. */
   static Automaton reverse(Automaton a, HashSet<int> initialStates)
   {

      if (Operations.isEmpty(a))
      {
         return new Automaton();
      }

      int numStates = a.getNumStates();

      // Build a new automaton with all edges reversed
      Automaton.Builder builder = new Automaton.Builder();

      // Initial node; we'll add epsilon transitions in the end:
      builder.createState();

      for (int s = 0; s < numStates; s++)
      {
         builder.createState();
      }

      // Old initial state becomes new accept state:
      builder.setAccept(1, true);

      Transition t = new Transition();
      for (int s = 0; s < numStates; s++)
      {
         int numTransitions = a.getNumTransitions(s);
         a.initTransition(s, t);
         for (int i = 0; i < numTransitions; i++)
         {
            a.getNextTransition(t);
            builder.addTransition(t.dest + 1, s + 1, t.min, t.max);
         }
      }

      Automaton result = builder.finish();

      int s2 = 0;
      var acceptStates = a.getAcceptStates();
      while (s2 < numStates && (s2 = acceptStates.NextSetBit(s2)) != -1)
      {
         result.addEpsilon(0, s2 + 1);
         if (initialStates != null)
         {
            initialStates.Add(s2 + 1);
         }
         s2++;
      }

      result.finishState();

      return result;
   }

   private class PathNode
   {

      /** Which state the path node ends on, whose
       *  transitions we are enumerating. */
      public int state;

      /** Which state the current transition leads to. */
      public int to;

      /** Which transition we are on. */
      public int transition;

      /** Which label we are on, in the min-max range of the
       *  current Transition */
      public int label;

      private Transition t = new Transition();

      public void resetState(Automaton a, int state)
      {
         //     assert a.getNumTransitions(state) != 0;
         this.state = state;
         transition = 0;
         a.getTransition(state, 0, t);
         label = t.min;
         to = t.dest;
      }

      /** Returns next label of current transition, or
       *  advances to next transition and returns its first
       *  label, if current one is exhausted.  If there are
       *  no more transitions, returns -1. */
      public int nextLabel(Automaton a)
      {
         if (label > t.max)
         {
            // We've exhaused the current transition's labels;
            // move to next transitions:
            transition++;
            if (transition >= a.getNumTransitions(state))
            {
               // We're done iterating transitions leaving this state
               return -1;
            }
            a.getTransition(state, transition, t);
            label = t.min;
            to = t.dest;
         }
         return label++;
      }
   }

#if TODO_PORT
  private static PathNode getNode(PathNode[] nodes, int index) {
 //   assert index < nodes.length;
    if (nodes[index] == null) {
      nodes[index] = new PathNode();
    }
    return nodes[index];
  }



  // TODO: this is a dangerous method ... Automaton could be
  // huge ... and it's better in general for caller to
  // enumerate & process in a single walk:

  /** Returns the set of accepted strings, up to at most
   *  <code>limit</code> strings. If more than <code>limit</code> 
   *  strings are accepted, the first limit strings found are returned. If <code>limit</code> == -1, then 
   *  the limit is infinite.  If the {@link Automaton} has
   *  cycles then this method might throw {@code
   *  IllegalArgumentException} but that is not guaranteed
   *  when the limit is set. */
  public static Set<IntsRef> getFiniteStrings(Automaton a, int limit) {
    Set<IntsRef> results = new HashSet<>();

    if (limit == -1 || limit > 0) {
      // OK
    } else {
      throw new IllegalArgumentException("limit must be -1 (which means no limit), or > 0; got: " + limit);
    }

    if (a.isAccept(0)) {
      // Special case the empty string, as usual:
      results.add(new IntsRef());
    }

    if (a.getNumTransitions(0) > 0 && (limit == -1 || results.size() < limit)) {

      int numStates = a.getNumStates();

      // Tracks which states are in the current path, for
      // cycle detection:
      BitSet pathStates = new BitSet(numStates);

      // Stack to hold our current state in the
      // recursion/iteration:
      PathNode[] nodes = new PathNode[4];

      pathStates.set(0);
      PathNode root = getNode(nodes, 0);
      root.resetState(a, 0);

      IntsRefBuilder string = new IntsRefBuilder();
      string.append(0);

      while (string.length() > 0) {

        PathNode node = nodes[string.length()-1];

        // Get next label leaving the current node:
        int label = node.nextLabel(a);

        if (label != -1) {
          string.setIntAt(string.length()-1, label);

          if (a.isAccept(node.to)) {
            // This transition leads to an accept state,
            // so we save the current string:
            results.add(string.toIntsRef());
            if (results.size() == limit) {
              break;
            }
          }

          if (a.getNumTransitions(node.to) != 0) {
            // Now recurse: the destination of this transition has
            // outgoing transitions:
            if (pathStates.get(node.to)) {
              throw new IllegalArgumentException("automaton has cycles");
            }
            pathStates.set(node.to);

            // Push node onto stack:
            if (nodes.length == string.length()) {
              PathNode[] newNodes = new PathNode[ArrayUtil.oversize(nodes.length+1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
              System.arraycopy(nodes, 0, newNodes, 0, nodes.length);
              nodes = newNodes;
            }
            getNode(nodes, string.length()).resetState(a, node.to);
            string.setLength(string.length() + 1);
            string.grow(string.length());
          }
        } else {
          // No more transitions leaving this state,
          // pop/return back to previous state:
          assert pathStates.get(node.state);
          pathStates.clear(node.state);
          string.setLength(string.length() - 1);
        }
      }
    }

    return results;
  }
#endif

   /** Returns a new automaton accepting the same language with added
   *  transitions to a dead state so that from every state and every label
   *  there is a transition. */
   public static Automaton totalize(Automaton a)
   {
      Automaton result = new Automaton();
      int numStates = a.getNumStates();
      for (int i = 0; i < numStates; i++)
      {
         result.createState();
         result.setAccept(i, a.isAccept(i));
      }

      int deadState = result.createState();
      result.addTransition(deadState, deadState, CodePoints.MIN_CODE_POINT, CodePoints.MAX_CODE_POINT);

      Transition t = new Transition();
      for (int i = 0; i < numStates; i++)
      {
         int maxi = CodePoints.MIN_CODE_POINT;
         int count = a.initTransition(i, t);
         for (int j = 0; j < count; j++)
         {
            a.getNextTransition(t);
            result.addTransition(i, t.dest, t.min, t.max);
            if (t.min > maxi)
            {
               result.addTransition(i, deadState, maxi, t.min - 1);
            }
            if (t.max + 1 > maxi)
            {
               maxi = t.max + 1;
            }
         }

         if (maxi <= CodePoints.MAX_CODE_POINT)
         {
            result.addTransition(i, deadState, maxi, CodePoints.MAX_CODE_POINT);
         }
      }

      result.finishState();
      return result;
   }
}

