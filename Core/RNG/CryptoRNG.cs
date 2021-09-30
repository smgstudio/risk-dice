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

using System;
using System.Security.Cryptography;
using Risk.Dice.Utility;

namespace Risk.Dice.RNG
{
    public class CryptoRNG : IRNG
    {
        private readonly byte[] _buffer = new byte[8];

        private RNGCryptoServiceProvider _rng;

        public CryptoRNG ()
        {
            _rng = new RNGCryptoServiceProvider();
        }

        public double NextDouble ()
        {
            return (double) (NextULong() / (decimal) ulong.MaxValue);
        }

        public int NextInt (int min, int max)
        {
            int diff = max - min;
            int mask = diff >> 31;
            int range = (mask ^ diff) - mask;

            if (range == 0)
            {
                return min;
            }

            _rng.GetBytes(_buffer, 0, sizeof(int));

            int value = BitConverter.ToInt32(_buffer, 0);
            mask = 1 << 31;

            if (diff < 0)
            {
                value |= mask;
            }
            else
            {
                value &= ~mask;
            }

            return (value % range) + min;
        }

        public uint NextUInt ()
        {
            _rng.GetBytes(_buffer, 0, sizeof(uint));
            return BitConverter.ToUInt32(_buffer, 0);
        }

        public ulong NextULong ()
        {
            _rng.GetBytes(_buffer, 0, sizeof(ulong));
            return BitConverter.ToUInt64(_buffer, 0);
        }

        public void NextBytes (byte[] data)
        {
            _rng.GetBytes(data);
        }
    }
}