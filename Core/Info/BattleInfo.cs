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
    public class BattleInfo
    {
        [SerializeField] protected BattleConfig _battleConfig;
        [SerializeField] protected RoundConfig _roundConfig;
        [SerializeField] protected double[] _attackLossChances;
        [SerializeField] protected double[] _defendLossChances;

        public BattleConfig BattleConfig => _battleConfig;
        public RoundConfig RoundConfig => _roundConfig;
        public double[] AttackLossChances => _attackLossChances;
        public double[] DefendLossChances => _defendLossChances;
        public double AttackWinChance => _defendLossChances[_battleConfig.DefendUnitCount];
        public double DefendWinChance => _attackLossChances[_battleConfig.AttackUnitCount];
        public double UnresolvedChance => _battleConfig.StopUntil > 0 ? Math.Max(1.0 - AttackWinChance - DefendWinChance, 0.0) : 0.0;
        public virtual bool IsReady => _attackLossChances != null && _defendLossChances != null && _attackLossChances.Length > 0 && _defendLossChances.Length > 0;

        internal BattleInfo (RoundConfig roundConfig)
        {
            _roundConfig = roundConfig;
        }

        public BattleInfo (BattleConfig battleConfig, RoundConfig roundConfig)
        {
            _battleConfig = battleConfig;
            _roundConfig = roundConfig;
        }

        public virtual void Calculate ()
        {
            if (IsReady)
            {
                return;
            }

            int attackUnitCount = _battleConfig.AttackUnitCount;
            int defendUnitCount = _battleConfig.DefendUnitCount;
            int stopUntil = _battleConfig.StopUntil;

            double[] attackLossChances = new double[attackUnitCount + 1];
            double[] defendLossChances = new double[defendUnitCount + 1];

            RoundConfig roundConfig = _roundConfig.WithBattle(_battleConfig);
            RoundInfo roundInfo = RoundCache.Get(roundConfig);
            roundInfo.Calculate();

            if (stopUntil == 0)
            {
                // Only calculate if not using early stop

                for (int i = 0; i < roundInfo.AttackLossChances.Length; i++)
                {
                    double roundChance = roundInfo.AttackLossChances[i];

                    if (roundChance <= 0.0)
                    {
                        continue;
                    }

                    int remainingAttackUnitCount = attackUnitCount - i;
                    int remainingDefendUnitCount = defendUnitCount - (roundConfig.ChallengeCount - i);

                    if (remainingAttackUnitCount <= 0 || remainingDefendUnitCount <= 0)
                    {
                        // Battle chain is over: accumulate chance
                        attackLossChances[attackUnitCount - remainingAttackUnitCount] += roundChance;
                        defendLossChances[defendUnitCount - remainingDefendUnitCount] += roundChance;
                    }
                    else
                    {
                        // Battle chain continues
                        BattleConfig nextBattleConfig = _battleConfig.WithNewUnits(remainingAttackUnitCount, remainingDefendUnitCount);
                        BattleInfo battleInfo = BattleCache.Get(_roundConfig, nextBattleConfig);
                        battleInfo.Calculate();

                        for (int a = 0; a < battleInfo._attackLossChances.Length; a++)
                        {
                            attackLossChances[attackUnitCount - remainingAttackUnitCount + a] += roundChance * battleInfo._attackLossChances[a];
                        }

                        for (int d = 0; d < battleInfo._defendLossChances.Length; d++)
                        {
                            defendLossChances[defendUnitCount - remainingDefendUnitCount + d] += roundChance * battleInfo._defendLossChances[d];
                        }
                    }
                }
            }
            else
            {
                BattleConfig baseBattleConfig = _battleConfig.WithoutStopUntil();
                BattleInfo baseBattleInfo = BattleCache.Get(_roundConfig, baseBattleConfig);
                baseBattleInfo.Calculate();

                for (int i = 0; i < attackLossChances.Length; i++)
                {
                    if (i < baseBattleInfo._attackLossChances.Length - 1)
                    {
                        attackLossChances[i] = baseBattleInfo._attackLossChances[i];
                    }
                }

                for (int i = 0; i < defendLossChances.Length; i++)
                {
                    defendLossChances[i] = baseBattleInfo._defendLossChances[i];
                }
            }

            // Complete

            _attackLossChances = attackLossChances;
            _defendLossChances = defendLossChances;

#if UNITY_ASSERTIONS
            Assert.AreApproximatelyEqual((float) (AttackWinChance + DefendWinChance + UnresolvedChance), (float) 1.0);
            Assert.AreApproximatelyEqual((float) _attackLossChances.SumAsDouble() + (float) UnresolvedChance, (float) 1.0);
            Assert.AreApproximatelyEqual((float) _defendLossChances.SumAsDouble(), (float) 1.0);
#endif
        }

        public virtual double GetOutcomeChance (int lostAttackCount, int lostDefendCount)
        {
            if (lostAttackCount == _battleConfig.AttackUnitCount - _battleConfig.StopUntil)
            {
                return _attackLossChances[lostAttackCount];
            }
            else if (lostDefendCount == _battleConfig.DefendUnitCount)
            {
                return _defendLossChances[lostDefendCount];
            }
            else
            {
                return -1;
            }
        }
    }
}