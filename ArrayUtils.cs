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


internal static class ArrayUtil
{
   internal const Int32 MAX_ARRAY_LENGTH = Int32.MaxValue /* - lucene ram estimator */;
   internal const Int32 RAM_USAGE_NUM_BYTES_LONG = 8;
   internal const Int32 RAM_USAGE_NUM_BYTES_INT = 4;

   public static Int64[] grow(long[] array, Int32 minSize)
   {
      //assert minSize >= 0: "size must be positive (got " + minSize + "): likely integer overflow?";
      if (array.Length < minSize)
      {
         Int64[] newArray = new Int64[oversize(minSize, RAM_USAGE_NUM_BYTES_LONG)];

         Array.Copy(array, 0, newArray, 0, array.Length);

         return newArray;
      }
      else
         return array;
   }

   public static Int64[] grow(long[] array)
   {
      return grow(array, 1 + array.Length);
   }

   public static Int32[] grow(int[] array, Int32 minSize)
   {
      //  assert minSize >= 0: "size must be positive (got " + minSize + "): likely integer overflow?";
      if (array.Length < minSize)
      {
         Int32[] newArray = new Int32[oversize(minSize, RAM_USAGE_NUM_BYTES_INT)];
         Array.Copy(array, 0, newArray, 0, array.Length);
         //System.arraycopy(array, 0, newArray, 0, array.length);
         return newArray;
      }
      else
         return array;
   }

   public static Int32[] grow(int[] array)
   {
      return grow(array, 1 + array.Length);
   }

   /** Returns an array size >= minTargetSize, generally
*  over-allocating exponentially to achieve amortized
*  linear-time cost as the array grows.
*
*  NOTE: this was originally borrowed from Python 2.4.2
*  listobject.c sources (attribution in LICENSE.txt), but
*  has now been substantially changed based on
*  discussions from java-dev thread with subject "Dynamic
*  array reallocation algorithms", started on Jan 12
*  2010.
*
* @param minTargetSize Minimum required value to be returned.
* @param bytesPerElement Bytes used by each element of
* the array.  See constants in {@link RamUsageEstimator}.
*
* @lucene.internal
*/

   public static Int32 oversize(Int32 minTargetSize, Int32 bytesPerElement)
   {

      if (minTargetSize < 0)
      {
         // catch usage that accidentally overflows int
         throw new ArgumentException("invalid array size " + minTargetSize);
      }

      if (minTargetSize == 0)
      {
         // wait until at least one element is requested
         return 0;
      }

      if (minTargetSize > MAX_ARRAY_LENGTH)
      {
         throw new ArgumentException("requested array size " + minTargetSize + " exceeds maximum array in java (" + MAX_ARRAY_LENGTH + ")");
      }

      // asymptotic exponential growth by 1/8th, favors
      // spending a bit more CPU to not tie up too much wasted
      // RAM:
      Int32 extra = minTargetSize >> 3;

      if (extra < 3)
      {
         // for very small arrays, where constant overhead of
         // realloc is presumably relatively high, we grow
         // faster
         extra = 3;
      }

      Int32 newSize = minTargetSize + extra;

      // add 7 to allow for worst case byte alignment addition below:
      if (newSize + 7 < 0 || newSize + 7 > MAX_ARRAY_LENGTH)
      {
         // int overflowed, or we exceeded the maximum array length
         return MAX_ARRAY_LENGTH;
      }

      if (Environment.Is64BitProcess)
      {
         // round up to 8 byte alignment in 64bit env
         switch (bytesPerElement)
         {
            case 4:
               // round up to multiple of 2
               return (newSize + 1) & 0x7ffffffe;
            case 2:
               // round up to multiple of 4
               return (newSize + 3) & 0x7ffffffc;
            case 1:
               // round up to multiple of 8
               return (newSize + 7) & 0x7ffffff8;
            case 8:
            // no rounding
            default:
               // odd (invalid?) size
               return newSize;
         }
      }
      else
      {
         // round up to 4 byte alignment in 64bit env
         switch (bytesPerElement)
         {
            case 2:
               // round up to multiple of 2
               return (newSize + 1) & 0x7ffffffe;
            case 1:
               // round up to multiple of 4
               return (newSize + 3) & 0x7ffffffc;
            case 4:
            case 8:
            // no rounding
            default:
               // odd (invalid?) size
               return newSize;
         }
      }
   }
}
