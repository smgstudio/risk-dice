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

namespace Risk.Dice.Utility
{
    public struct ByteBuffer
    {
        private readonly byte[] bytes;
        private int position;

        public byte[] Bytes => bytes;
        public bool IsComplete => position == bytes.Length;

        public ByteBuffer (int length)
        {
            bytes = new byte[length];
            position = 0;
        }

        public ByteBuffer (byte[] bytes)
        {
            this.bytes = bytes;
            position = 0;
        }

        public void Reset()
        {
            position = 0;
        }

        public unsafe void WriteInt (int value)
        {
            fixed (byte* bytesPtr = &bytes[position])
            {
                int* ptr = (int*) bytesPtr;
                *ptr = value;
            }

            position += sizeof(int);
        }

        public unsafe void WriteUint (uint value)
        {
            fixed (byte* bytesPtr = &bytes[position])
            {
                uint* ptr = (uint*) bytesPtr;
                *ptr = value;
            }

            position += sizeof(uint);
        }

        public unsafe void WriteFloat (float value)
        {
            fixed (byte* bytesPtr = &bytes[position])
            {
                float* ptr = (float*) bytesPtr;
                *ptr = value;
            }

            position += sizeof(float);
        }

        public unsafe void WriteDouble (double value)
        {
            fixed (byte* bytesPtr = &bytes[position])
            {
                double* ptr = (double*) bytesPtr;
                *ptr = value;
            }

            position += sizeof(double);
        }

        public unsafe void WriteBool (bool value)
        {
            fixed (byte* bytesPtr = &bytes[position])
            {
                bool* ptr = (bool*) bytesPtr;
                *ptr = value;
            }

            position += sizeof(bool);
        }

        public void WriteDoubleArray (double[] array)
        {
            WriteBool(array != null);

            if (array != null)
            {
                WriteInt(array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    WriteDouble(array[i]);
                }
            }
        }

        public unsafe int ReadInt ()
        {
            int value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                int* ptr = (int*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(int);
            return value;
        }

        public unsafe uint ReadUint ()
        {
            uint value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                uint* ptr = (uint*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(uint);
            return value;
        }

        public unsafe float ReadFloat ()
        {
            float value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                float* ptr = (float*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(float);
            return value;
        }

        public unsafe double ReadDouble ()
        {
#if UNITY_64
            double value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                double* ptr = (double*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(double);
            return value;
#else
            double value = BitConverter.ToDouble(bytes, position);
            position += sizeof(double);
            return value;
#endif
        }

        public unsafe bool ReadBool ()
        {
            bool value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                bool* ptr = (bool*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(bool);
            return value;
        }

        public unsafe ulong ReadUlong ()
        {
#if UNITY_64
            ulong value;

            fixed (byte* bytesPtr = &bytes[position])
            {
                ulong* ptr = (ulong*) bytesPtr;
                value = *ptr;
            }

            position += sizeof(ulong);
            return value;
#else
            ulong value = BitConverter.ToUInt64(bytes, position);
            position += sizeof(ulong);
            return value;
#endif
        }

        public double[] ReadDoubleArray ()
        {
            bool hasArray = ReadBool();

            if (hasArray)
            {
                int length = ReadInt();

                double[] array = new double[length];

                for (int i = 0; i < length; i++)
                {
                    array[i] = ReadDouble();
                }

                return array;
            }
            else
            {
                return null;
            }
        }

        public static int GetByteLength (double[] array)
        {
            int length = sizeof(bool);

            if (array != null)
            {
                length += sizeof(int);
                length += sizeof(double) * array.Length;
            }

            return length;
        }
    }
}