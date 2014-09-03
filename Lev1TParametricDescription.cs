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

/** Parametric description for generating a Levenshtein automaton of degree 1, 
    with transpositions as primitive edits */
class Lev1TParametricDescription : ParametricDescription
{
   public override
   Int32 transition(Int32 absState, Int32 position, Int32 vector)
   {
      // null absState should never be passed in

      Debug.Assert(absState != -1);

      // decode absState -> state, offset
      Int32 state = absState / (w + 1);
      Int32 offset = absState % (w + 1);
      Debug.Assert(offset >= 0);

      if (position == w)
      {
         if (state < 2)
         {
            Int32 loc = vector * 2 + state;
            offset += unpack(offsetIncrs0, loc, 1);
            state = unpack(toStates0, loc, 2) - 1;
         }
      }
      else if (position == w - 1)
      {
         if (state < 3)
         {
            Int32 loc = vector * 3 + state;
            offset += unpack(offsetIncrs1, loc, 1);
            state = unpack(toStates1, loc, 2) - 1;
         }
      }
      else if (position == w - 2)
      {
         if (state < 6)
         {
            Int32 loc = vector * 6 + state;
            offset += unpack(offsetIncrs2, loc, 2);
            state = unpack(toStates2, loc, 3) - 1;
         }
      }
      else
      {
         if (state < 6)
         {
            Int32 loc = vector * 6 + state;
            offset += unpack(offsetIncrs3, loc, 2);
            state = unpack(toStates3, loc, 3) - 1;
         }
      }

      if (state == -1)
      {
         // null state
         return -1;
      }
      else
      {
         // translate back to abs
         return state * (w + 1) + offset;
      }
   }

   // 1 vectors; 2 states per vector; array length = 2
   private static Int64[] toStates0 = new Int64[] /*2 bits per value */ {
    0x2L
  };
   private static Int64[] offsetIncrs0 = new Int64[] /*1 bits per value */ {
    0x0L
  };

   // 2 vectors; 3 states per vector; array length = 6
   private static Int64[] toStates1 = new Int64[] /*2 bits per value */ {
    0xa43L
  };
   private static Int64[] offsetIncrs1 = new Int64[] /*1 bits per value */ {
    0x38L
  };

   // 4 vectors; 6 states per vector; array length = 24
   private static Int64[] toStates2 = new Int64[] /*3 bits per value */ {
    0x3453491482140003L,0x6dL
  };
   private static Int64[] offsetIncrs2 = new Int64[] /*2 bits per value */ {
    0x555555a20000L
  };

   // 8 vectors; 6 states per vector; array length = 48
   private static Int64[] toStates3 = new Int64[] /*3 bits per value */ {
    0x21520854900c0003L,0x5b4d19a24534916dL,0xda34L
  };
   private static Int64[] offsetIncrs3 = new Int64[] /*2 bits per value */ {
    0x5555ae0a20fc0000L,0x55555555L
  };

   // state map
   //   0 -> [(0, 0)]
   //   1 -> [(0, 1)]
   //   2 -> [(0, 1), (1, 1)]
   //   3 -> [(0, 1), (2, 1)]
   //   4 -> [t(0, 1), (0, 1), (1, 1), (2, 1)]
   //   5 -> [(0, 1), (1, 1), (2, 1)]


   public Lev1TParametricDescription(Int32 w) :
      base(w, 1, new Int32[] { 0, 1, 0, -1, -1, -1 })
   {
   }
}
