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
    public class FastWinChanceInfo
    {
        [SerializeField] private RoundConfig _roundConfig;
        [SerializeField] private BalanceConfig _balanceConfig;

        public RoundConfig RoundConfig => _roundConfig;
        public BalanceConfig BalanceConfig => _balanceConfig;

        public FastWinChanceInfo () : this(RoundConfig.Default, null)
        {
        }

        public FastWinChanceInfo (RoundConfig roundConfig) : this (roundConfig, null)
        {
        }

        public FastWinChanceInfo (RoundConfig roundConfig, BalanceConfig balanceConfig)
        {
            _roundConfig = roundConfig;
            _balanceConfig = balanceConfig;
        }

        public float GetWinChance (int attackers, int defenders)
        {
            double winChance = 0;
            double lossChance = 0;
            if (attackers < _roundConfig.AttackDiceCount || defenders < _roundConfig.DefendDiceCount)
            {
                BattleConfig battleConfig = new BattleConfig(attackers, defenders, 0);
                winChance = FastBattleEndCache.Get(_roundConfig, battleConfig).WinChance;
            }
            else
            {
                MultiRoundCacheInfo multiRoundCacheInfo = MultiRoundCache.Get(_roundConfig);
                int attackerLossTarget = attackers - _roundConfig.AttackDiceCount + 1;
                int defenderLossTarget = defenders - _roundConfig.DefendDiceCount + 1;

                // Go through every case where the defenders fall below their
                // maximum dice roll before or at the same time as the attackers
                // fall below their maximum dice roll.
                List<MultiRoundLossInfo> attackerLosses = multiRoundCacheInfo.GetFixedDefenderLoss(attackerLossTarget, defenderLossTarget);
                for (int i = 0; i < attackerLosses.Count; i++)
                {
                    int remainingDefenders = _roundConfig.DefendDiceCount - 1 - i;
                    double[] outcomeChances = attackerLosses[i].OutcomeChances;
                    if (remainingDefenders == 0)
                    {
                        for (int j = 0; j < outcomeChances.Length; j++)
                        {
                            winChance += outcomeChances[j];
                        }
                        continue;
                    }
                    int remainingAttackers = attackers - attackerLosses[i].InitialLoss;
                    int challengeCount = _roundConfig.ChallengeCount;
                    int k = 0;
                    BattleConfig firstBattleConfig = new BattleConfig(remainingAttackers, remainingDefenders, 0);
                    FastBattleEndInfo firstEndInfo = FastBattleEndCache.Get(_roundConfig, firstBattleConfig);
                    for (; k < outcomeChances.Length && remainingAttackers > firstEndInfo.BattleConfig.AttackUnitCount; k++, remainingAttackers -= challengeCount)
                    {
                        winChance += outcomeChances[k];
                    }
                    for (; k < outcomeChances.Length; k++, remainingAttackers -= challengeCount)
                    {
                        double outcomeChance = outcomeChances[k];
                        BattleConfig battleConfig = new BattleConfig(remainingAttackers, remainingDefenders, 0);
                        FastBattleEndInfo endInfo = FastBattleEndCache.Get(_roundConfig, battleConfig);
                        winChance += outcomeChance * endInfo.WinChance;
                        lossChance += outcomeChance * (1.0 - endInfo.WinChance);
                    }
                }

                // Go through every case where the attackers fall below their
                // maximum dice roll strictly before the attackers fall below
                // maximum dice roll.  GetFixedAttackerLoss does include cases
                // where both fall below the limit at the same time so we do
                // explicitly filter these out here.
                List<MultiRoundLossInfo> defenderLosses = multiRoundCacheInfo.GetFixedAttackerLoss(attackerLossTarget, defenderLossTarget);
                for (int i = 0; i < defenderLosses.Count; i++)
                {
                    int remainingAttackers = _roundConfig.AttackDiceCount - 1 - i;
                    double[] outcomeChances = defenderLosses[i].OutcomeChances;
                    if (remainingAttackers == 0)
                    {
                        for (int j = 0; j < outcomeChances.Length; j++)
                        {
                            lossChance += outcomeChances[j];
                        }
                        continue;
                    }
                    int defenderLoss = defenderLosses[i].InitialLoss;
                    int challengeCount = _roundConfig.ChallengeCount;
                    int k = 0;
                    for (; k < outcomeChances.Length; k++, defenderLoss -= challengeCount)
                    {
                        double outcomeChance = outcomeChances[k];
                        if (outcomeChance <= 0.0 || defenderLoss >= defenderLossTarget)
                        {
                            continue;
                        }
                        BattleConfig battleConfig = new BattleConfig(remainingAttackers, defenders - defenderLoss, 0);
                        FastBattleEndInfo endInfo = FastBattleEndCache.Get(_roundConfig, battleConfig);
                        if (!endInfo.UseAllDefenders)
                        {
                            break;
                        }
                        winChance += outcomeChance * endInfo.WinChance;
                        lossChance += outcomeChance * (1.0 - endInfo.WinChance);
                    }
                    for (; k < outcomeChances.Length; k++, defenderLoss -= challengeCount)
                    {
                        lossChance += outcomeChances[k];
                    }
                }

                // If we used any gaussian approximation our odds might not
                // quite sum to one, so we renormalize them here (except only
                // winChance because we don't care about lossChance anymore).
                winChance = winChance / (winChance + lossChance);
            }
            if (_balanceConfig != null)
            {
                return ApplyBalance((float)winChance);
            }
            else
            {
                return (float)winChance;
            }
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
                return 0f;
            }
            
            if (d < 0)
            {
                return 1f;
            }

            float ratio = 1f / (a + d);

            winChance = a * ratio;

            return winChance;
        }
    }
}
