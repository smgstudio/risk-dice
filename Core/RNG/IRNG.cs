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

using System.Collections.Generic;

namespace Risk.Dice.RNG
{
    public interface IRNG
    {
        /// <summary>
        /// Returns a random double between 0 and 1.
        /// </summary>
        double NextDouble ();

        /// <summary>
        /// Returns a random int between min [inclusive] and max [exclusive unless min == max].
        /// </summary>
        int NextInt (int min, int max);

        /// <summary>
        /// Returns a random uint full range.
        /// </summary>
        uint NextUInt ();

        /// <summary>
        /// Returns a random ulong full range.
        /// </summary>
        ulong NextULong ();

        /// <summary>
        /// Randomizes the bytes in the passed in byte array.
        /// </summary>
        void NextBytes (byte[] data);
    }

    public static class RNGUtil
    {
        public static IRNG DefaultSeeder => new CryptoRNG();
        public static IRNG Default => new PCGRNG(DefaultSeeder.NextULong());

        private static IRNG _seeder = DefaultSeeder;
        public static IRNG Seeder => _seeder;

        public static int GetDiceRoll (this IRNG rng)
        {
            return rng.NextInt(0, 6);
        }

        public static T GetElement<T> (this IRNG rng, IList<T> list, T defaultValue = default)
        {
            if (rng == null || list == null || list.Count == 0)
            {
                return defaultValue;
            }

            return list[rng.NextInt(0, list.Count)];
        }
    }
}