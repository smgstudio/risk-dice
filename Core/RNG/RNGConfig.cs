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
using Sirenix.OdinInspector;
using Risk.Dice.Utility;
using UnityEngine;

namespace Risk.Dice.RNG
{
    public enum RNGType
    {
        Crypto = 0,
        System = 1,
#if UNITY_EDITOR || UNITY_64
        Unity = 2,
        UnityStateless = 3,
#endif
        MersenneTwister = 4,
        XorShift = 5,
        PCG = 6
    }

    public enum SeedMode
    {
        Auto,
        None,
        Int,
        Ulong,
        Ulong2,
        UInts
    }

    [Serializable]
    public sealed class RNGConfig : IEquatable<RNGConfig>
    {
        [SerializeField] [LabelText("RNG Type")] private RNGType _rngType;
        [SerializeField] [InlineButton(nameof(RandomizeSeeds), "Randomize")] [ValidateInput(nameof(ValidateSeedMode))] private SeedMode _seedMode;
        [SerializeField] [ShowIf(nameof(_seedMode), SeedMode.Int)] private int _intSeed;
        [SerializeField] [ShowIf(nameof(ShowUlong))] private ulong _ulongSeed1;
        [SerializeField] [ShowIf(nameof(_seedMode), SeedMode.Ulong2)] private ulong _ulongSeed2;
        [SerializeField] [ShowIf(nameof(_seedMode), SeedMode.UInts)] private uint[] _uintsSeed;

        public bool IsValid => ValidateSeedMode();

        public static RNGConfig Default => new RNGConfig();

        public RNGConfig () : this(RNGType.PCG, SeedMode.Auto)
        {
        }

        public RNGConfig (RNGType type) : this(type, SeedMode.Auto)
        {
        }

        public RNGConfig (RNGType type, SeedMode seedMode)
        {
            _rngType = type;
            _seedMode = seedMode;
        }

        public void SetSeed ()
        {
            _seedMode = SeedMode.None;
        }

        public void SetSeed (int intSeed)
        {
            _seedMode = SeedMode.Int;
            _intSeed = intSeed;
        }

        public void SetSeed (ulong ulongSeed)
        {
            _seedMode = SeedMode.Ulong;
            _ulongSeed1 = ulongSeed;
        }

        public void SetSeed (ulong ulongSeed1, ulong ulongSeed2)
        {
            _seedMode = SeedMode.Ulong2;
            _ulongSeed1 = ulongSeed1;
            _ulongSeed2 = ulongSeed2;
        }

        public void SetSeed (uint[] uintsSeed)
        {
            _seedMode = SeedMode.UInts;
            _uintsSeed = uintsSeed;
        }

        public IRNG GetRNG ()
        {
            if (!IsValid)
            {
                return null;
            }

            switch (_rngType)
            {
                case RNGType.Crypto:
                    {
                        return new CryptoRNG();
                    }

                case RNGType.System:
                    {
                        switch (_seedMode)
                        {
                            case SeedMode.Auto:
                            case SeedMode.None:
                                return new SystemRNG();
                            case SeedMode.Int:
                                return new SystemRNG(_intSeed);
                        }

                        break;
                    }

#if UNITY_EDITOR || UNITY_64

                case RNGType.Unity:
                    {
                        switch (_seedMode)
                        {
                            case SeedMode.Auto:
                            case SeedMode.None:
                                return new UnityRNG();
                            case SeedMode.Int:
                                return new UnityRNG(_intSeed);
                        }

                        break;
                    }

                case RNGType.UnityStateless:
                    {
                        return new StatelessUnityRNG();
                    }

#endif

                case RNGType.MersenneTwister:
                    {
                        switch (_seedMode)
                        {
                            case SeedMode.Auto:
                            case SeedMode.None:
                                return new MersenneTwisterRNG();
                            case SeedMode.Int:
                                return new MersenneTwisterRNG(_intSeed);
                            case SeedMode.UInts:
                                return new MersenneTwisterRNG(_uintsSeed);
                        }

                        break;
                    }

                case RNGType.XorShift:
                    {
                        switch (_seedMode)
                        {
                            case SeedMode.Auto:
                                {
                                    byte[] data = new byte[16];
                                    RNGUtil.Seeder.NextBytes(data);
                                    ByteBuffer buffer = new ByteBuffer(data);

                                    ulong seed1 = buffer.ReadUlong();
                                    ulong seed2 = buffer.ReadUlong();

                                    return new XorShiftRNG(seed1, seed2);
                                }
                            case SeedMode.Ulong2:
                                return new XorShiftRNG(_ulongSeed1, _ulongSeed2);
                        }

                        break;
                    }

                case RNGType.PCG:
                    {
                        switch (_seedMode)
                        {
                            case SeedMode.Auto:
                                {
                                    byte[] data = new byte[8];
                                    RNGUtil.Seeder.NextBytes(data);
                                    ByteBuffer buffer = new ByteBuffer(data);

                                    ulong seed = buffer.ReadUlong();

                                    return new PCGRNG(seed);
                                }
                            case SeedMode.Ulong:
                                return new PCGRNG(_ulongSeed1);
                            case SeedMode.Ulong2:
                                return new PCGRNG(_ulongSeed1, _ulongSeed2);
                        }

                        break;
                    }
            }

            return default;
        }

        public bool Equals (RNGConfig other)
        {
            if (_rngType != other._rngType
                    || _seedMode != other._seedMode
                    || _intSeed != other._intSeed
                    || _ulongSeed1 != other._ulongSeed1
                    || _ulongSeed2 != other._ulongSeed2)
            {
                return false;
            }

            return true;
        }

        #region ODIN

        private bool ShowUlong => _seedMode == SeedMode.Ulong || _seedMode == SeedMode.Ulong2;

        private bool ValidateSeedMode ()
        {
            string unused = default;
            return ValidateSeedMode(_seedMode, ref unused);
        }

        private bool ValidateSeedMode (SeedMode value, ref string error)
        {
            if (value == SeedMode.Auto)
            {
                return true;
            }

            bool isError = false;

            switch (_rngType)
            {
                case RNGType.Crypto:
                    isError = value != SeedMode.None;
                    break;
                case RNGType.System:
                    isError = value != SeedMode.None && value != SeedMode.Int;
                    break;
#if UNITY_EDITOR || UNITY_64
                case RNGType.Unity:
                    isError = value != SeedMode.None && value != SeedMode.Int;
                    break;
                case RNGType.UnityStateless:
                    isError = value != SeedMode.None;
                    break;
#endif
                case RNGType.MersenneTwister:
                    isError = value != SeedMode.None && value != SeedMode.Int && value != SeedMode.UInts;
                    break;
                case RNGType.XorShift:
                    isError = value != SeedMode.Ulong2;
                    break;
                case RNGType.PCG:
                    isError = value != SeedMode.Ulong && value != SeedMode.Ulong2;
                    break;
            }

            if (isError && string.IsNullOrEmpty(error))
            {
                error = $"Selected RNG does not support seed mode | RNG: {_rngType} | Seed Mode: {value}";
            }

            return !isError;
        }

        private void RandomizeSeeds ()
        {
            byte[] data = new byte[16];
            RNGUtil.Seeder.NextBytes(data);
            ByteBuffer buffer = new ByteBuffer(data);

            _intSeed = buffer.ReadInt();
            buffer.Reset();

            _ulongSeed1 = buffer.ReadUlong();
            _ulongSeed2 = buffer.ReadUlong();
            buffer.Reset();

            _uintsSeed = new uint[4];

            for (int i = 0; i < _uintsSeed.Length; i++)
            {
                _uintsSeed[i] = buffer.ReadUint();
            }
        }

        #endregion
    }
}