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

/** Holds one transition from an {@link Automaton}.  This is typically
 *  used temporarily when iterating through transitions by invoking
 *  {@link Automaton#initTransition} and {@link Automaton#getNextTransition}. */

public class Transition
{
   /** Source state. */
   public Int32 source;

   /** Destination state. */
   public Int32 dest;

   /** Minimum accepted label (inclusive). */
   public Int32 min;

   /** Maximum accepted label (inclusive). */
   public Int32 max;

   /** Remembers where we are in the iteration; init to -1 to provoke
    *  exception if nextTransition is called without first initTransition. */
   public Int32 transitionUpto = -1;

   public override String ToString()
   {
      return source + " --> " + dest + " " + (Char)min + "-" + (Char)max;
   }
}

