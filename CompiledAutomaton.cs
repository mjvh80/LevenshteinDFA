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

public class CompiledAutomaton
{
   //private readonly Automaton automaton;
   private readonly int[] points;
   private readonly int size;
   private readonly bool[] accept;
   private readonly int[] transitions;
   private readonly int[] classmap;

   public CompiledAutomaton(Automaton a)
   {
      //this.automaton = a;
      points = a.getStartPoints();
      //initial = 0;
      size = Math.Max(1, a.getNumStates());
      accept = new bool[size];
      transitions = new int[size * points.Length];

      //Arrays.fill(transitions, -1);
      for (var i = 0; i < transitions.Length; i++)
         transitions[i] = -1;

      for (int n = 0; n < size; n++)
      {
         accept[n] = a.isAccept(n);
         for (int c = 0; c < points.Length; c++)
         {
            int dest = a.step(n, points[c]);
            //     assert dest == -1 || dest < size;
            transitions[n * points.Length + c] = dest;
         }
      }

      var maxInterval = 256;
      classmap = new int[maxInterval + 1];
      int k = 0;
      for (int j = 0; j <= maxInterval; j++)
      {
         if (k + 1 < points.Length && j == points[k + 1])
         {
            k++;
         }
         classmap[j] = k;
      }
   }

   public int EstimatedMemoryUsage
   {
      get
      {
         return (points.Length + 1 + 2 +
                transitions.Length + 1 + 2 +
                classmap.Length + 1 + 2 +
                size) * sizeof(int) +
                accept.Length + 4 + 8 +
                8; // < for this
      }
   }

   private int getCharClass(int c)
   {
      return Operations.findIndex(c, points);
   }

   public Boolean Matches(String input)
   {
      return Matches(CodePoints.codePoints(input));
   }

   public Boolean Matches(int[] inputCodePoints)
   {
      var step = 0;
      //      fixed(int* cp = inputCodePoints, tr = transitions, cm = classmap)
      for (var i = 0; i < inputCodePoints.Length; i++)
      {
         // step = transitions[step * points.Length + getCharClass(inputCodePoints[i])];
         step = transitions[step * points.Length + classmap[inputCodePoints[i]]];

         //  step = *(tr + (step * points.Length + *(cm + *(cp + i))));

         if (step == -1) return false;
      }
      return accept[step];
   }
}
