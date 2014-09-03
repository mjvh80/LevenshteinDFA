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


public abstract class InPlaceMergeSorter<T> : Sorter<T>
{
   /** Create a new {@link InPlaceMergeSorter} */
   public InPlaceMergeSorter() { }

   protected T[] arrayToSort;

   override
   public void sort(T[] array, Int32 from, Int32 to)
   {
      arrayToSort = array;

      checkRange(from, to);
      mergeSort(from, to);
   }

   void mergeSort(Int32 from, Int32 to)
   {
      if (to - from < THRESHOLD)
      {
         insertionSort(from, to);
      }
      else
      {
         Int32 mid = (Int32)((UInt32)(from + to) >> 1);
         mergeSort(from, mid);
         mergeSort(mid, to);
         mergeInPlace(from, mid, to);
      }
   }

   //protected override int compare(int i, int j)
   //{
   //   Debug.Assert(CompareFn != null);
   //   return CompareFn(i, j);
   //}

   //protected override void swap(int i, int j)
   //{
   //   Debug.Assert(SwapFn != null);
   //   SwapFn(i, j);
   //}

   //public Func<Int32, Int32, Int32> CompareFn;
   //public Action<Int32, Int32> SwapFn;
   //public Action<Int32, Int32> SwapOneFn;
}

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

/** Base class for sorting algorithms implementations.
 * @lucene.internal */

// MJ changes, made this generic because it depends too much on javas anonymous classes.
// The sort method sig changed to receive the array to sort as we cannot easily capture it in C#.
public abstract class Sorter<T>
{
   protected static Int32 THRESHOLD = 20;

   /** Sole constructor, used for inheritance. */
   protected Sorter() { }

   /** Compare entries found in slots <code>i</code> and <code>j</code>.
    *  The contract for the returned value is the same as
    *  {@link Comparator#compare(Object, Object)}. */
   protected abstract Int32 compare(Int32 i, Int32 j);

   /** Swap values at slots <code>i</code> and <code>j</code>. */
   protected abstract void swap(Int32 i, Int32 j);

   /** Sort the slice which starts at <code>from</code> (inclusive) and ends at
    *  <code>to</code> (exclusive). */
   public abstract void sort(T[] array, Int32 from, Int32 to);

   protected void checkRange(Int32 from, Int32 to)
   {
      if (to < from)
      {
         throw new ArgumentException("'to' must be >= 'from', got from=" + from + " and to=" + to);
      }
   }

   protected void mergeInPlace(Int32 from, Int32 mid, Int32 to)
   {
      if (from == mid || mid == to || compare(mid - 1, mid) <= 0)
      {
         return;
      }
      else if (to - from == 2)
      {
         swap(mid - 1, mid);
         return;
      }
      while (compare(from, mid) <= 0)
      {
         ++from;
      }
      while (compare(mid - 1, to - 1) <= 0)
      {
         --to;
      }
      Int32 first_cut, second_cut;
      Int32 len11, len22;
      if (mid - from > to - mid)
      {
         len11 = (Int32)(((UInt32)(mid - from)) >> 1);
         first_cut = from + len11;
         second_cut = lower(mid, to, first_cut);
         len22 = second_cut - mid;
      }
      else
      {
         len22 = (Int32)((UInt32)(to - mid) >> 1);
         second_cut = mid + len22;
         first_cut = upper(from, mid, second_cut);
         len11 = first_cut - from;
      }
      rotate(first_cut, mid, second_cut);
      Int32 new_mid = first_cut + len22;
      mergeInPlace(from, first_cut, new_mid);
      mergeInPlace(new_mid, second_cut, to);
   }

   Int32 lower(Int32 from, Int32 to, Int32 val)
   {
      Int32 len = to - from;
      while (len > 0)
      {
         Int32 half = (Int32)((UInt32)len >> 1);
         Int32 mid = from + half;
         if (compare(mid, val) < 0)
         {
            from = mid + 1;
            len = len - half - 1;
         }
         else
         {
            len = half;
         }
      }
      return from;
   }

   Int32 upper(Int32 from, Int32 to, Int32 val)
   {
      Int32 len = to - from;
      while (len > 0)
      {
         Int32 half = (Int32)((UInt32)len >> 1);
         Int32 mid = from + half;
         if (compare(val, mid) < 0)
         {
            len = half;
         }
         else
         {
            from = mid + 1;
            len = len - half - 1;
         }
      }
      return from;
   }

   // faster than lower when val is at the end of [from:to[
   Int32 lower2(Int32 from, Int32 to, Int32 val)
   {
      Int32 f = to - 1, t = to;
      while (f > from)
      {
         if (compare(f, val) < 0)
         {
            return lower(f, t, val);
         }
         Int32 delta = t - f;
         t = f;
         f -= delta << 1;
      }
      return lower(from, t, val);
   }

   // faster than upper when val is at the beginning of [from:to[
   Int32 upper2(Int32 from, Int32 to, Int32 val)
   {
      Int32 f = from, t = f + 1;
      while (t < to)
      {
         if (compare(t, val) > 0)
         {
            return upper(f, t, val);
         }
         Int32 delta = t - f;
         f = t;
         t += delta << 1;
      }
      return upper(f, to, val);
   }

   void reverse(Int32 from, Int32 to)
   {
      for (--to; from < to; ++from, --to)
      {
         swap(from, to);
      }
   }

   void rotate(Int32 lo, Int32 mid, Int32 hi)
   {
      Debug.Assert(lo <= mid && mid <= hi);
      if (lo == mid || mid == hi)
      {
         return;
      }
      doRotate(lo, mid, hi);
   }

   void doRotate(Int32 lo, Int32 mid, Int32 hi)
   {
      if (mid - lo == hi - mid)
      {
         // happens rarely but saves n/2 swaps
         while (mid < hi)
         {
            swap(lo++, mid++);
         }
      }
      else
      {
         reverse(lo, mid);
         reverse(mid, hi);
         reverse(lo, hi);
      }
   }

   protected void insertionSort(Int32 from, Int32 to)
   {
      for (Int32 i = from + 1; i < to; ++i)
      {
         for (Int32 j = i; j > from; --j)
         {
            if (compare(j - 1, j) > 0)
            {
               swap(j - 1, j);
            }
            else
            {
               break;
            }
         }
      }
   }

   void binarySort(Int32 from, Int32 to)
   {
      binarySort(from, to, from + 1);
   }

   void binarySort(Int32 from, Int32 to, Int32 i)
   {
      for (; i < to; ++i)
      {
         Int32 l = from;
         Int32 h = i - 1;
         while (l <= h)
         {
            Int32 mid = (Int32)((UInt32)(l + h) >> 1);
            Int32 cmp = compare(i, mid);
            if (cmp < 0)
            {
               h = mid - 1;
            }
            else
            {
               l = mid + 1;
            }
         }
         switch (i - l)
         {
            case 2:
               swap(l + 1, l + 2);
               swap(l, l + 1);
               break;
            case 1:
               swap(l, l + 1);
               break;
            case 0:
               break;
            default:
               for (Int32 j = i; j > l; --j)
               {
                  swap(j - 1, j);
               }
               break;
         }
      }
   }

   void heapSort(Int32 from, Int32 to)
   {
      if (to - from <= 1)
      {
         return;
      }
      heapify(from, to);
      for (Int32 end = to - 1; end > from; --end)
      {
         swap(from, end);
         siftDown(from, from, end);
      }
   }

   void heapify(Int32 from, Int32 to)
   {
      for (Int32 i = heapParent(from, to - 1); i >= from; --i)
      {
         siftDown(i, from, to);
      }
   }

   void siftDown(Int32 i, Int32 from, Int32 to)
   {
      for (Int32 leftChild = heapChild(from, i); leftChild < to; leftChild = heapChild(from, i))
      {
         Int32 rightChild = leftChild + 1;
         if (compare(i, leftChild) < 0)
         {
            if (rightChild < to && compare(leftChild, rightChild) < 0)
            {
               swap(i, rightChild);
               i = rightChild;
            }
            else
            {
               swap(i, leftChild);
               i = leftChild;
            }
         }
         else if (rightChild < to && compare(i, rightChild) < 0)
         {
            swap(i, rightChild);
            i = rightChild;
         }
         else
         {
            break;
         }
      }
   }

   static Int32 heapParent(Int32 from, Int32 i)
   {
      return (Int32)((UInt32)(i - 1 - from) >> 1) + from;
   }

   static Int32 heapChild(Int32 from, Int32 i)
   {
      return ((i - from) << 1) + 1 + from;
   }
}
