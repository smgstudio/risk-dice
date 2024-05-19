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
using System.Collections.Generic;
using Risk.Dice.Utility;
using UnityEngine;

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

namespace Risk.Dice
{
    [Serializable]
    public class FastBattleInfo
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

        internal FastBattleInfo (RoundConfig roundConfig)
        {
            _roundConfig = roundConfig;
        }

        public FastBattleInfo (BattleConfig battleConfig, RoundConfig roundConfig)
        {
            _battleConfig = battleConfig;
            _roundConfig = roundConfig;
        }

        private void AddEndChances(int attackLoss, int defendLoss, double scale)
        {
            int attackUnitCount = _battleConfig.AttackUnitCount;
            int defendUnitCount = _battleConfig.DefendUnitCount;
            int remainingAttackers = attackUnitCount - attackLoss;
            int remainingDefenders = defendUnitCount - defendLoss;

            BattleConfig battleConfig = new BattleConfig(remainingAttackers, remainingDefenders, 0);
            FastBattleEndInfo endInfo = FastBattleEndCache.Get(_roundConfig, battleConfig);
            int length = endInfo.OutcomeChances.Length;
            if (!endInfo.UseAllAttackers)
            {
                for (int i = 0; i < length; i++)
                {
                    _attackLossChances[attackLoss + i] += endInfo.OutcomeChances[i] * scale;
                }
            }
            else if (!endInfo.UseAllDefenders)
            {
                for (int i = 0; i < length; i++)
                {
                    _defendLossChances[defendLoss + i] += endInfo.OutcomeChances[length - 1 - i] * scale;
                }
            }
            else
            {
                for (int i = 0; i < remainingAttackers; i++)
                {
                    _attackLossChances[attackLoss + i] += endInfo.OutcomeChances[i] * scale;
                }
                for (int i = 0; i < remainingDefenders; i++)
                {
                    _defendLossChances[defendLoss + i] += endInfo.OutcomeChances[length - 1 - i] * scale;
                }
            }
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

            _attackLossChances = new double[attackUnitCount + 1];
            _defendLossChances = new double[defendUnitCount + 1];

            if (stopUntil != 0)
            {
                BattleConfig baseBattleConfig = _battleConfig.WithoutStopUntil();
                FastBattleInfo baseBattleInfo = new FastBattleInfo(baseBattleConfig, _roundConfig);
                baseBattleInfo.Calculate();

                for (int i = 0; i < _attackLossChances.Length; i++)
                {
                    if (i < baseBattleInfo._attackLossChances.Length - 1)
                    {
                        _attackLossChances[i] = baseBattleInfo._attackLossChances[i];
                    }
                }

                for (int i = 0; i < _defendLossChances.Length; i++)
                {
                    _defendLossChances[i] = baseBattleInfo._defendLossChances[i];
                }
            }
            else if (attackUnitCount < _roundConfig.AttackDiceCount || defendUnitCount < _roundConfig.DefendDiceCount)
            {
                AddEndChances(0, 0, 1);
                _defendLossChances[defendUnitCount] = MathUtil.SumAsDouble(_attackLossChances, 0, attackUnitCount);
                _attackLossChances[attackUnitCount] = MathUtil.SumAsDouble(_defendLossChances, 0, defendUnitCount);
            }
            else
            {
                MultiRoundCacheInfo multiRoundCacheInfo = MultiRoundCache.Get(_roundConfig);
                int attackerLossTarget = attackUnitCount - _roundConfig.AttackDiceCount + 1;
                int defenderLossTarget = defendUnitCount - _roundConfig.DefendDiceCount + 1;

                // Go through every case where the defenders fall below their
                // maximum dice roll before or at the same time as the attackers
                // fall below their maximum dice roll.
                List<MultiRoundLossInfo> attackerLosses = multiRoundCacheInfo.GetFixedDefenderLoss(attackerLossTarget, defenderLossTarget);
                for (int i = 0; i < attackerLosses.Count; ++i) {
                    int defenderLoss = defenderLossTarget + i;
                    int attackerLoss = attackerLosses[i].InitialLoss;
                    double[] outcomeChances = attackerLosses[i].OutcomeChances;
                    int challengeCount = _roundConfig.ChallengeCount;
                    for (int j = 0; j < outcomeChances.Length; j++, attackerLoss += challengeCount)
                    {
                        double outcomeChance = outcomeChances[j];
                        if (outcomeChance <= 0.0)
                        {
                            continue;
                        }
                        if (defenderLoss == defendUnitCount)
                        {
                            _attackLossChances[attackerLoss] += outcomeChance;
                        }
                        else
                        {
                            AddEndChances(attackerLoss, defenderLoss, outcomeChance);
                        }
                    }
                }

                // Go through every case where the attackers fall below their
                // maximum dice roll strictly before the attackers fall below
                // maximum dice roll.  GetFixedAttackerLoss does include cases
                // where both fall below the limit at the same time so we do
                // explicitly filter these out here.
                List<MultiRoundLossInfo> defenderLosses = multiRoundCacheInfo.GetFixedAttackerLoss(attackerLossTarget, defenderLossTarget);
                for (int i = 0; i < defenderLosses.Count; ++i) {
                    int attackerLoss = attackerLossTarget + i;
                    int defenderLoss = defenderLosses[i].InitialLoss;
                    double[] outcomeChances = defenderLosses[i].OutcomeChances;
                    int challengeCount = _roundConfig.ChallengeCount;
                    for (int j = 0; j < outcomeChances.Length; j++, defenderLoss -= challengeCount)
                    {
                        double outcomeChance = outcomeChances[j];
                        if (outcomeChance <= 0.0 || defenderLoss >= defenderLossTarget)
                        {
                            continue;
                        }
                        if (attackerLoss == attackUnitCount)
                        {
                            _defendLossChances[defenderLoss] += outcomeChance;
                        }
                        else
                        {
                            AddEndChances(attackerLoss, defenderLoss, outcomeChance);
                        }
                    }
                }

                // Calcualte over win and loss chances to completely populate
                // the attack and defend loss chances.  Also if we used any
                // gaussian approximation our odds might not quite sum to one
                // anymore so we renormalize here.
                double winChance = MathUtil.SumAsDouble(_attackLossChances, 0, attackUnitCount);
                double lossChance = MathUtil.SumAsDouble(_defendLossChances, 0, defendUnitCount);
                double normalizationRatio = 1.0 / (winChance + lossChance);
                winChance *= normalizationRatio;
                lossChance *= normalizationRatio;
                MathUtil.NormalizeSum(_attackLossChances, winChance, 0, attackUnitCount);
                MathUtil.NormalizeSum(_defendLossChances, lossChance, 0, defendUnitCount);
                _defendLossChances[defendUnitCount] = winChance;
                _attackLossChances[attackUnitCount] = lossChance;
            }

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
