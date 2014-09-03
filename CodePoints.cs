using System;
using System.Text;


public static class CodePoints
{
   public static readonly Int32 MAX_CODE_POINT = (Int32)Char.MaxValue; // Lucene uses 0x10FFFF /* Character.MAX_CODE_POINT */
   public static readonly Int32 MIN_CODE_POINT = 0;

   // let's, to make things easy, stick with .NET char for now.
   // These are not codepoints as we ignore e.g. surrogate pairs and what not, but it'll do for our purposes.
   // It should otherwise be simple to support "full" unicode by implementing this method "correctly".
   // Of course we're wasting double the memory as shorts would suffice here but for now we'll accept this.
   // We can consider optimizing this later (todo, consider making this class generic over short/int?).
   // Any functionality in the original Java Lucene implementation, using Character class for example, is
   // ported here.

   public static String newString(int[] codePoints, int index, int length)
   {
      var charArray = new Char[length - index];
      for (var i = index; i < index + length; i++)
         charArray[i - index] = (Char)codePoints[i];
      return new String(charArray);
   }

   public static int codePointCount(String input)
   {
      return input.Length;
   }

   public static StringBuilder appendCodePoint(StringBuilder b, int cp)
   {
      return b.Append((Char)cp);
   }

   public static int charCount(int codePoint)
   {
      return 1; // as we ignore e.g. surrogate pairs for now, this is 1-1
   }

   public static Char codePointToChar(int codePoint)
   {
      return (Char)codePoint;
   }

   public static int codePointAt(String input, int i)
   {
      return (int)input[i];
   }

   public static int[] codePoints(String input)
   {
      var result = new int[input.Length];
      for (var i = 0; i < input.Length; i++)
         result[i] = (int)input[i];
      return result;

      // Original java code:
      //int length = Character.codePointCount(input, 0, input.length());
      //int[] word = new int[length];
      //for (int i = 0, j = 0, cp = 0; i < input.Length; i += Character.charCount(cp)) {
      //  word[j++] = cp = input.codePointAt(i);
      //}
      //return word;
   }
}

