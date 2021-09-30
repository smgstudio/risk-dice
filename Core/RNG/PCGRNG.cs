// Modified PCG C# implementation based on PCGSharp
// Source: https://github.com/igiagkiozis/PCGSharp/blob/master/PCGSharp/Source/Pcg.cs
// License:

// MIT License
// 
// Copyright (c) 2016 Bismur Studios Ltd.
// Copyright (c) 2016 Ioannis Giagkiozis
//
// This file is based on PCG, the original has the following license: 
/*
 * PCG Random Number Generation for C.
 *
 * Copyright 2014 Melissa O'Neill <oneill@pcg-random.org>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * For additional information about the PCG random number generation scheme,
 * including its license and other licensing options, visit
 *
 *      http://www.pcg-random.org
 */

namespace Risk.Dice.RNG
{
    public sealed class PCGRNG : IRNG
    {
        private ulong _state;
        private ulong _increment = 1442695040888963407ul;

        public PCGRNG (ulong seed) : this(seed, 721347520444481703ul)
        {
        }

        public PCGRNG (ulong seed, ulong sequence)
        {
            Initialize(seed, sequence);
        }

        private void Initialize (ulong seed, ulong sequence)
        {
            _state = 0ul;
            _increment = (sequence << 1) | 1;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public double NextDouble ()
        {
            return NextUInt() * 2.3283064365386963E-10;
        }

        public int NextInt (int min, int max)
        {
            uint uMax = unchecked((uint) (max - min));
            uint threshold = (uint) (-uMax) % uMax;

            while (true)
            {
                uint result = NextUInt();

                if (result >= threshold)
                {
                    return (int) (unchecked((result % uMax) + min));
                }
            }
        }

        public uint NextUInt ()
        {
            ulong prevState = _state;
            _state = unchecked(prevState * 6364136223846793005ul + _increment);
            uint xorShifted = (uint) (((prevState >> 18) ^ prevState) >> 27);
            int rot = (int) (prevState >> 59);
            uint result = (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            return result;
        }

        public unsafe ulong NextULong ()
        {
            ulong value = 0;
            uint* valuePtr = (uint*) &value;
            valuePtr[0] = NextUInt();
            valuePtr[1] = NextUInt();
            return value;
        }

        public byte NextByte ()
        {
            uint result = NextUInt();
            return (byte) (result % 256);
        }

        public void NextBytes (byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = NextByte();
            }
        }
    }
}