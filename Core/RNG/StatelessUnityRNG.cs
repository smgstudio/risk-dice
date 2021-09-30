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

#if UNITY_EDITOR || UNITY_64

using UnityEngine;

namespace Risk.Dice.RNG
{
    public class StatelessUnityRNG : IRNG
    {
        public double NextDouble ()
        {
            return Random.value;
        }

        public int NextInt (int min, int max)
        {
            return Random.Range(min, max);
        }

        public uint NextUInt ()
        {
            uint loRange = (uint) Random.Range(0, 1 << 30);
            uint hiRange = (uint) Random.Range(0, 1 << 2);

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

        public unsafe void NextBytes (byte[] data)
        {
            uint value = 0;

            for (int i = 0; i < data.Length; i++)
            {
                int part = i % sizeof(uint);

                if (part == 0)
                {
                    value = NextUInt();
                }

                byte* valuePtr = (byte*) &value;
                data[i] = valuePtr[part];
            }
        }
    }
}
#endif