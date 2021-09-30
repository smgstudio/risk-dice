/*
 *
 * Copyright 2021 SMG Studio.
 *
 * RISK is a trademark of Hasbro. ©2020 Hasbro.All Rights Reserved.Used under licence.
 *
 * You are hereby granted a non-exclusive, limited right to use and to install, one (1) copy of the
 * software for internal evaluation purposes only and in accordance with the provisions below.You
 * may not reproduce, redistribute or publish the software, or any part of it, in any form.
 * 
 * SMG may withdraw this licence without notice and/or request you delete any copies of the software
 * (including backups).
 *
 * The Agreement does not involve any transfer of any intellectual property rights for the
 * Software. SMG Studio reserves all rights to the Software not expressly granted in writing to
 * you.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 */

using Random = System.Random;
using MersenneTwister;

namespace Risk.Dice.RNG
{
    public sealed class MersenneTwisterRNG : IRNG
    {
        private Random _random;
        private readonly byte[] _buffer = new byte[8];

        public MersenneTwisterRNG ()
        {
            _random = MTRandom.Create();
        }

        public MersenneTwisterRNG (int seed)
        {
            _random = MTRandom.Create(seed);
        }

        public MersenneTwisterRNG (uint[] seed)
        {
            _random = MTRandom.Create(seed);
        }

        public double NextDouble ()
        {
            return _random.NextDouble();
        }

        public int NextInt (int min, int max)
        {
            return _random.Next(min, max);
        }

        public uint NextUInt ()
        {
            uint loRange = (uint) _random.Next(1 << 30);
            uint hiRange = (uint) _random.Next(1 << 2);

            return (loRange << 2) | hiRange;
        }

        public unsafe ulong NextULong ()
        {
            ulong value = 0;
            uint* valuePtr = (uint*) &value;
            valuePtr[0] = NextUInt();
            valuePtr[1] = NextUInt();
            return value;
        }

        public void NextBytes (byte[] data)
        {
            _random.NextBytes(data);
        }
    }
}