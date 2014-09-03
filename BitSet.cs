using System;
using System.Collections.Generic;

// Like bitarray, but grows. Probably non-optimal implementation.
public class BitSet
{
   private readonly List<int> bitSet;

   public BitSet(int capacity)
   {
      bitSet = new List<int>(capacity / 32);
   }

   public Boolean Get(int index)
   {
      var bucket = index / 32;
      if (bucket >= bitSet.Count) return false;
      var bit = 1 << (index % 32);
      return (bitSet[bucket] & bit) != 0;
   }

   public void Clear()
   {
      bitSet.Clear(); // todo: memory?
   }

   public int Count
   {
      get
      {
         return bitSet.Count * 32;
      }
   }

   public void Set(int index, bool value = true)
   {
      var bucket = index / 32;
      var bit = 1 << (index % 32);

      if (bucket >= bitSet.Count)
      {
         for (var i = bitSet.Count; i < bucket; i++)
            bitSet.Add(0);

         bitSet.Add(value ? bit : 0);
      }
      else
         bitSet[bucket] = value ? bitSet[bucket] | bit : bitSet[bucket] & ~bit;
   }

   public void Clear(int index)
   {
      Set(index, false);
   }

   // todo: this can probably be done much faster
   public int NextSetBit(Int32 idx)
   {
      if (idx < 0) return -1;

      var initBucket = idx / 32;
      if (initBucket >= bitSet.Count) return -1;

      for (var bucket = initBucket; bucket < bitSet.Count; bucket++)
         for (var i = (bucket == initBucket ? (idx % 32) : 0); i < 32; i++)
         {
            if ((bitSet[bucket] & (1 << i)) != 0)
               return bucket * 32 + i;
         }
      return -1;
   }

   public int Cardinality
   {
      get
      {
         var count = 0;
         for (var bucket = 0; bucket < bitSet.Count; bucket++)
            for (var i = 0; i < 32; i++)
               if ((bitSet[bucket] & (1 << i)) != 0)
                  count += 1;
         return count;
      }
   }

   public bool AnyBitSet
   {
      get
      {
         for (var bucket = 0; bucket < bitSet.Count; bucket++)
            if (bitSet[bucket] != 0)
               return true;
         return false;
      }
   }

   public void And(BitSet other)
   {
      // we can ignore buckets out of range (if other is larger) as these are 0 anyway
      var range = Math.Min(bitSet.Count, other.bitSet.Count);
      for (var bucket = 0; bucket < range; bucket++)
         bitSet[bucket] &= other.bitSet[bucket];
      // Buckets out of range are simply 0ed.
      for (var bucket = range; bucket < bitSet.Count; bucket++)
         bitSet[bucket] = 0; // could reduce list size instead...
   }

   public void AndNot(BitSet other)
   {
      // we can ignore buckets out of range because we're only clearing bits, never setting.
      var range = Math.Min(bitSet.Count, other.bitSet.Count);
      for (var bucket = 0; bucket < range; bucket++)
         bitSet[bucket] &= ~other.bitSet[bucket];
   }
}
