// Modified XorShift C# implementation based on XorShiftPlus
// Source: http://codingha.us/2018/12/17/xorshift-fast-csharp-random-number-generator/
// License:

/*
===============================[ XorShiftPlus ]==============================
==-------------[ (c) 2018 R. Wildenhaus - Licensed under MIT ]-------------==
=============================================================================
*/

namespace Risk.Dice.RNG
{
    public sealed class XorShiftRNG : IRNG
    {
        private ulong _stateX;
        private ulong _stateY;

        public XorShiftRNG (ulong seedX, ulong seedY)
        {
            _stateX = seedX;
            _stateY = seedY;
        }

        public double NextDouble ()
        {
            double value;
            ulong tempX, tempY, tempZ;

            tempX = _stateY;
            _stateX ^= _stateX << 23; tempY = _stateX ^ _stateY ^ (_stateX >> 17) ^ (_stateY >> 26);

            tempZ = tempY + _stateY;
            value = 4.6566128730773926E-10 * (0x7FFFFFFF & tempZ);

            _stateX = tempX;
            _stateY = tempY;

            return value;
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
            uint value;
            ulong tempX, tempY;

            tempX = _stateY;
            _stateX ^= _stateX << 23; tempY = _stateX ^ _stateY ^ (_stateX >> 17) ^ (_stateY >> 26);

            value = (uint) (tempY + _stateY);

            _stateX = tempX;
            _stateY = tempY;

            return value;
        }

        public unsafe ulong NextULong ()
        {
            ulong value = 0;
            uint* valuePtr = (uint*) &value;
            valuePtr[0] = NextUInt();
            valuePtr[1] = NextUInt();
            return value;
        }

        public unsafe void NextBytes (byte[] buffer)
        {
            ulong x = _stateX, y = _stateY, tempX, tempY, z;

            fixed (byte* pBuffer = buffer)
            {
                ulong* pIndex = (ulong*) pBuffer;
                ulong* pEnd = (ulong*) (pBuffer + buffer.Length);

                while (pIndex <= pEnd - 1)
                {
                    tempX = y;
                    x ^= x << 23; tempY = x ^ y ^ (x >> 17) ^ (y >> 26);

                    *(pIndex++) = tempY + y;

                    x = tempX;
                    y = tempY;
                }

                if (pIndex < pEnd)
                {
                    tempX = y;
                    x ^= x << 23; tempY = x ^ y ^ (x >> 17) ^ (y >> 26);
                    z = tempY + y;

                    byte* pByte = (byte*) pIndex;
                    while (pByte < pEnd) *(pByte++) = (byte) (z >>= 8);
                }
            }

            _stateX = x;
            _stateY = y;
        }
    }
}