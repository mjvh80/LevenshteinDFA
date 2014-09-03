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
  * A ParametricDescription describes the structure of a Levenshtein DFA for some degree n.
  * <p>
  * There are four components of a parametric description, all parameterized on the length
  * of the word <code>w</code>:
  * <ol>
  * <li>The number of states: {@link #size()}
  * <li>The set of final states: {@link #isAccept(int)}
  * <li>The transition function: {@link #transition(int, int, int)}
  * <li>Minimal boundary function: {@link #getPosition(int)}
  * </ol>
  */
abstract class ParametricDescription
{
   protected readonly Int32 w;
   protected readonly Int32 n;
   private readonly Int32[] minErrors;

   protected ParametricDescription(Int32 w, Int32 n, Int32[] minErrors)
   {
      this.w = w;
      this.n = n;
      this.minErrors = minErrors;
   }

   /**
    * Return the number of states needed to compute a Levenshtein DFA
    */
   public Int32 size()
   {
      return minErrors.Length * (w + 1);
   }

   /**
    * Returns true if the <code>state</code> in any Levenshtein DFA is an accept state (final state).
    */
   public Boolean isAccept(Int32 absState)
   {
      // decode absState -> state, offset
      Int32 state = absState / (w + 1);
      Int32 offset = absState % (w + 1);
      //  assert offset >= 0;
      return w - offset + minErrors[state] <= n;
   }

   /**
    * Returns the position in the input word for a given <code>state</code>.
    * This is the minimal boundary for the state.
    */
   public Int32 getPosition(Int32 absState)
   {
      return absState % (w + 1);
   }

   /**
    * Returns the state number for a transition from the given <code>state</code>,
    * assuming <code>position</code> and characteristic vector <code>vector</code>
    */
   public abstract Int32 transition(Int32 state, Int32 position, Int32 vector);

   private readonly static Int64[] MASKS = new Int64[] {0x1,0x3,0x7,0xf,
                                                    0x1f,0x3f,0x7f,0xff,
                                                    0x1ff,0x3ff,0x7ff,0xfff,
                                                    0x1fff,0x3fff,0x7fff,0xffff,
                                                    0x1ffff,0x3ffff,0x7ffff,0xfffff,
                                                    0x1fffff,0x3fffff,0x7fffff,0xffffff,
                                                    0x1ffffff,0x3ffffff,0x7ffffff,0xfffffff,
                                                    0x1fffffff,0x3fffffff,0x7fffffffL,0xffffffffL,
                                                    0x1ffffffffL,0x3ffffffffL,0x7ffffffffL,0xfffffffffL,
                                                    0x1fffffffffL,0x3fffffffffL,0x7fffffffffL,0xffffffffffL,
                                                    0x1ffffffffffL,0x3ffffffffffL,0x7ffffffffffL,0xfffffffffffL,
                                                    0x1fffffffffffL,0x3fffffffffffL,0x7fffffffffffL,0xffffffffffffL,
                                                    0x1ffffffffffffL,0x3ffffffffffffL,0x7ffffffffffffL,0xfffffffffffffL,
                                                    0x1fffffffffffffL,0x3fffffffffffffL,0x7fffffffffffffL,0xffffffffffffffL,
                                                    0x1ffffffffffffffL,0x3ffffffffffffffL,0x7ffffffffffffffL,0xfffffffffffffffL,
                                                    0x1fffffffffffffffL,0x3fffffffffffffffL,0x7fffffffffffffffL};

   protected Int32 unpack(long[] data, Int32 index, Int32 bitsPerValue)
   {
      Int64 bitLoc = bitsPerValue * index;
      Int32 dataLoc = (Int32)(bitLoc >> 6);
      Int32 bitStart = (Int32)(bitLoc & 63);
      //System.out.println("index=" + index + " dataLoc=" + dataLoc + " bitStart=" + bitStart + " bitsPerV=" + bitsPerValue);
      if (bitStart + bitsPerValue <= 64)
      {
         // not split
         return (Int32)((data[dataLoc] >> bitStart) & MASKS[bitsPerValue - 1]);
      }
      else
      {
         // split
         Int32 part = 64 - bitStart;
         return (Int32)(((data[dataLoc] >> bitStart) & MASKS[part - 1]) +
                       ((data[1 + dataLoc] & MASKS[bitsPerValue - part - 1]) << part));
      }
   }
}