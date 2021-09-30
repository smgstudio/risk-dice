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
using System.Collections;
using Risk.Dice.Utility;
using UnityEngine;

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

namespace Risk.Dice
{
    [Serializable]
    public class WinChanceInfo
    {
        [SerializeField] private int _size;
        [SerializeField] private RoundConfig _roundConfig;
        [SerializeField] private BalanceConfig _balanceConfig;
        private float[,] _winChances;

        public int Size => _size;
        public RoundConfig RoundConfig => _roundConfig;
        public BalanceConfig BalanceConfig => _balanceConfig;
        public float[,] WinChances => _winChances;
        public bool IsReady => _winChances != null;

        public WinChanceInfo (int size) : this(size, RoundConfig.Default, null)
        {
        }

        public WinChanceInfo (int size, RoundConfig roundConfig) : this (size, roundConfig, null)
        {
        }

        public WinChanceInfo (int size, RoundConfig roundConfig, BalanceConfig balanceConfig)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(size > 2);
#endif

            _size = size;
            _roundConfig = roundConfig;
            _balanceConfig = balanceConfig;
        }

        public void Calculate ()
        {
            if (IsReady)
            {
                return;
            }

            int maxA = _roundConfig.AttackDiceCount;
            int maxD = _roundConfig.DefendDiceCount;

            // Find round chances

            float[,][] roundChances = new float[maxA + 1, maxD + 1][];

            for (int a = 1; a <= maxA; a++)
            {
                for (int d = 1; d <= maxD; d++)
                {
                    RoundConfig roundConfig = _roundConfig.WithBattle(new BattleConfig(a, d, 0));
                    RoundInfo roundInfo = RoundCache.Get(roundConfig);
                    roundInfo.Calculate();

                    roundChances[a, d] = new float[roundConfig.ChallengeCount + 1];

                    for (int c = 0; c <= roundConfig.ChallengeCount; c++)
                    {
                        roundChances[a, d][c] = (float) roundInfo.AttackLossChances[c];
                    }
                }
            }

            // Find win chances

            float[,] winChances = new float[_size, _size];

            for (int a = 0; a < _size; a++)
            {
                for (int d = 0; d < _size; d++)
                {
                    if (d == 0 || a == 0)
                    {
                        winChances[a, d] = a > 0 ? 1f : 0f;
                    }
                    else
                    {
                        int roundA = Math.Min(a, maxA);
                        int roundD = Math.Min(d, maxD);
                        int challengeCount = Math.Min(roundA, roundD);

                        for (int o = 0; o < challengeCount + 1 && a - o > 0; o++)
                        {
                            winChances[a, d] += roundChances[roundA, roundD][o] * winChances[a - o, (d - challengeCount) + o];
                        }
                    }
                }
            }

            // Apply balanced blitz

            if (_balanceConfig != null)
            {
                for (int a = 0; a < _size; a++)
                {
                    for (int d = 0; d < _size; d++)
                    {
                        winChances[a, d] = ApplyBalance(winChances[a, d]);
                    }
                }
            }

            // Complete

            _winChances = winChances;
        }

        private float ApplyBalance (float winChance)
        {
            winChance = ApplyWinChanceCutoff(winChance);
            winChance = ApplyWinChancePower(winChance);
            winChance = ApplyOutcomeCutoff(winChance);

            return winChance;
        }

        private float ApplyWinChanceCutoff (float winChance)
        {
            if (_balanceConfig.WinChanceCutoff <= 0f)
            {
                return winChance;
            }
            else if (winChance < _balanceConfig.WinChanceCutoff)
            {
                return 0f;
            }
            else if (winChance > 1f - _balanceConfig.WinChanceCutoff)
            {
                return 1f;
            }

            return winChance;
        }

        private float ApplyWinChancePower (float winChance)
        {
            float a = Mathf.Pow(winChance, (float) _balanceConfig.WinChancePower);
            float d = Mathf.Pow(1f - winChance, (float) _balanceConfig.WinChancePower);

            float ratio = 1f / (a + d);

            winChance = a * ratio;

            return winChance;
        }

        private float ApplyOutcomeCutoff (float winChance)
        {
            float a = winChance - (float) _balanceConfig.OutcomeCutoff;
            float d = (1f - winChance) - (float) _balanceConfig.OutcomeCutoff;

            if (a < 0)
            {
                d += -a;
                a = 0f;
            }
            
            if (d < 0)
            {
                a += -d;
                d = 0f;
            }

            float ratio = 1f / (a + d);

            winChance = a * ratio;

            return winChance;
        }
    }
}