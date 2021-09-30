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
using System.Linq;
using Risk.Dice.Utility;
using UnityEngine;

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

namespace Risk.Dice
{
    [Serializable]
    public sealed class BalancedBattleInfo : BattleInfo
    {
        [SerializeField] private BalanceConfig _balanceConfig;
        [SerializeField] private bool _balanceApplied;

        public override bool IsReady => _balanceApplied && base.IsReady;

        public BalancedBattleInfo (BattleInfo battleInfo, BalanceConfig balanceConfig) : base(battleInfo.BattleConfig, battleInfo.RoundConfig)
        {
            _balanceConfig = balanceConfig;

            if (battleInfo.IsReady)
            {
                _attackLossChances = battleInfo.AttackLossChances.ToArray();
                _defendLossChances = battleInfo.DefendLossChances.ToArray();
            }
        }

        public BalancedBattleInfo (BalanceConfig balanceConfig, BattleConfig battleConfig, RoundConfig roundConfig) : base(battleConfig, roundConfig)
        {
            _balanceConfig = balanceConfig;
        }

        public override void Calculate ()
        {
            base.Calculate();

            ApplyBalance();
        }

        public void ApplyBalance ()
        {
            if (_balanceApplied)
            {
                return;
            }

#if UNITY_ASSERTIONS
            Assert.IsTrue(base.IsReady);
#endif

            ApplyWinChanceCutoff();
            ApplyWinChancePower();
            ApplyOutcomeCutoff();
            ApplyOutcomePower();

#if UNITY_ASSERTIONS
            Assert.AreApproximatelyEqual((float) (AttackWinChance + DefendWinChance + UnresolvedChance), (float) 1.0);
            Assert.AreApproximatelyEqual((float) _attackLossChances.SumAsDouble() + (float) UnresolvedChance, (float) 1.0);
            Assert.AreApproximatelyEqual((float) _defendLossChances.SumAsDouble(), (float) 1.0);
#endif

            _balanceApplied = true;
        }

        private void ApplyWinChanceCutoff ()
        {
            // If the overall win OR lose chance is less than the cutoff value it will get rounded to 0% or 100%.
            // Example 1) 97% win chance with a 5% cutoff will turn into 100% win chance
            // Example 2) 3% win chance with a 5% cutoff will turn into 0% win chance
            // Example 3) 70% win chance with a 5% cutoff will remain a 70% win chance

            if (_balanceConfig.WinChanceCutoff <= 0)
            {
                return;
            }

            double[] loseChances = null;
            double[] winChances = null;

            if (AttackWinChance <= _balanceConfig.WinChanceCutoff)
            {
                loseChances = _attackLossChances;
                winChances = _defendLossChances;
            }

            if (_battleConfig.StopUntil > 0)
            {
                if (UnresolvedChance <= _balanceConfig.WinChanceCutoff)
                {
                    loseChances = _defendLossChances;
                    winChances = _attackLossChances;
                }
            }
            else
            {
                if (DefendWinChance <= _balanceConfig.WinChanceCutoff)
                {
                    loseChances = _defendLossChances;
                    winChances = _attackLossChances;
                }
            }

            if (winChances != null && loseChances != null)
            {
                for (int i = 0; i < loseChances.Length - 1; i++)
                {
                    loseChances[i] = 0.0;
                }

                if (loseChances == _attackLossChances && _battleConfig.StopUntil > 0)
                {
                    loseChances[loseChances.Length - 1] = 0.0;
                }
                else
                {
                    loseChances[loseChances.Length - 1] = 1.0;
                }

                winChances[winChances.Length - 1] = 0.0;
                MathUtil.NormalizeSum(winChances, 1.0, 0, winChances.Length - 1);
            }
        }

        private void ApplyWinChancePower ()
        {
            // Adjusts the overall win chance by improving the odds of the more likely outcome.
            // The more likely the outcome - the more this balance will apply.
            // Example 1) 56.8% win chance with a power of 1.4 will turn into a 59.4% win chance (2.6% difference)
            // Example 2) 43.2% win chance with a power of 1.4 will turn into a 40.6% win chance (2.6% difference)
            // Example 3) 86.1% win chance with a power of 1.4 will turn into a 92.8% win chance (6.7% difference)

            if (_balanceConfig.WinChancePower == 1.0)
            {
                return;
            }

            double targetWinChance, targetLoseChance;
            double[] winChances, loseChances;

            if (_battleConfig.StopUntil > 0)
            {
                if (AttackWinChance > UnresolvedChance)
                {
                    winChances = _attackLossChances;
                    loseChances = _defendLossChances;

                    targetWinChance = Math.Pow(AttackWinChance, _balanceConfig.WinChancePower);
                    targetLoseChance = Math.Pow(UnresolvedChance, _balanceConfig.WinChancePower);
                }
                else
                {
                    winChances = _defendLossChances;
                    loseChances = _attackLossChances;

                    targetWinChance = Math.Pow(UnresolvedChance, _balanceConfig.WinChancePower);
                    targetLoseChance = Math.Pow(AttackWinChance, _balanceConfig.WinChancePower);
                }
            }
            else
            {
                if (AttackWinChance > DefendWinChance)
                {
                    winChances = _attackLossChances;
                    loseChances = _defendLossChances;

                    targetWinChance = Math.Pow(AttackWinChance, _balanceConfig.WinChancePower);
                    targetLoseChance = Math.Pow(DefendWinChance, _balanceConfig.WinChancePower);
                }
                else
                {
                    winChances = _defendLossChances;
                    loseChances = _attackLossChances;

                    targetWinChance = Math.Pow(DefendWinChance, _balanceConfig.WinChancePower);
                    targetLoseChance = Math.Pow(AttackWinChance, _balanceConfig.WinChancePower);
                }
            }

            double normalizationRatio = 1.0 / (targetWinChance + targetLoseChance);
            targetWinChance *= normalizationRatio;
            targetLoseChance *= normalizationRatio;

            MathUtil.NormalizeSum(winChances, targetWinChance, 0, winChances.Length - 1);
            MathUtil.NormalizeSum(loseChances, targetLoseChance, 0, loseChances.Length - 1);

            if (_battleConfig.StopUntil > 0)
            {
                if (winChances == _attackLossChances)
                {
                    loseChances[loseChances.Length - 1] = targetWinChance;
                }
                else
                {
                    winChances[winChances.Length - 1] = targetLoseChance;
                }
            }
            else
            {
                winChances[winChances.Length - 1] = targetLoseChance;
                loseChances[loseChances.Length - 1] = targetWinChance;
            }
        }

        private void ApplyOutcomeCutoff ()
        {
            // Trims outcome chances at the high and low end equally then re-normalizes.
            // A cutoff of 20% will trim that much from the most favourable outcomes for both the attacker AND defender.
            // Overall win chance may or may not be affected.

            if (_balanceConfig.OutcomeCutoff <= 0)
            {
                return;
            }

            int outcomeCount = (_attackLossChances.Length - 1) + (_defendLossChances.Length - 1);
            double[] distribution = new double[outcomeCount];

            for (int i = 0; i < outcomeCount; i++)
            {
                if (i < _attackLossChances.Length - 1)
                {
                    distribution[i] = _attackLossChances[i];
                }
                else
                {
                    int flip = (i - _attackLossChances.Length) + 1;
                    int index = _defendLossChances.Length - 2 - flip;
                    distribution[i] = _defendLossChances[index];
                }
            }

            // Low cutoff
            {
                double cutSum = 0;

                for (int i = 0; i < outcomeCount; i++)
                {
                    cutSum += distribution[i];

                    if (cutSum > _balanceConfig.OutcomeCutoff)
                    {
                        distribution[i] = cutSum - _balanceConfig.OutcomeCutoff;
                        break;
                    }
                    else
                    {
                        distribution[i] = 0;
                    }
                }
            }

            // High cutoff
            {
                double cutSum = 0;

                for (int i = outcomeCount - 1; i >= 0; i--)
                {
                    cutSum += distribution[i];

                    if (cutSum > _balanceConfig.OutcomeCutoff)
                    {
                        distribution[i] = cutSum - _balanceConfig.OutcomeCutoff;
                        break;
                    }
                    else
                    {
                        distribution[i] = 0;
                    }
                }
            }

            // Copy back distribution

            for (int i = 0; i < outcomeCount; i++)
            {
                if (i < _attackLossChances.Length - 1)
                {
                    _attackLossChances[i] = distribution[i];
                }
                else
                {
                    int flip = (i - _attackLossChances.Length) + 1;
                    int index = _defendLossChances.Length - 2 - flip;
                    _defendLossChances[index] = distribution[i];
                }
            }

            // Re-normalization

            if (_battleConfig.StopUntil > 0)
            {
                double targetAttackChance = MathUtil.SumAsDouble(_attackLossChances, 0, _attackLossChances.Length - 1);
                double targetUnresolvedChance = MathUtil.SumAsDouble(_defendLossChances, 0, _defendLossChances.Length - 1);

                double normalizationRatio = 1.0 / (targetAttackChance + targetUnresolvedChance);
                targetAttackChance *= normalizationRatio;
                targetUnresolvedChance *= normalizationRatio;

                MathUtil.NormalizeSum(_attackLossChances, targetAttackChance, 0, _attackLossChances.Length - 1);
                MathUtil.NormalizeSum(_defendLossChances, targetUnresolvedChance, 0, _defendLossChances.Length - 1);

                _defendLossChances[_defendLossChances.Length - 1] = targetAttackChance;
            }
            else
            {
                double targetAttackChance = MathUtil.SumAsDouble(_attackLossChances, 0, _attackLossChances.Length - 1);
                double targetDefendChance = MathUtil.SumAsDouble(_defendLossChances, 0, _defendLossChances.Length - 1);

                double normalizationRatio = 1.0 / (targetAttackChance + targetDefendChance);
                targetAttackChance *= normalizationRatio;
                targetDefendChance *= normalizationRatio;

                MathUtil.NormalizeSum(_attackLossChances, targetAttackChance, 0, _attackLossChances.Length - 1);
                MathUtil.NormalizeSum(_defendLossChances, targetDefendChance, 0, _defendLossChances.Length - 1);

                _attackLossChances[_attackLossChances.Length - 1] = targetDefendChance;
                _defendLossChances[_defendLossChances.Length - 1] = targetAttackChance;
            }
        }

        private void ApplyOutcomePower ()
        {
            // Adjusts the individual outcome chances by boosting more likely outcomes and bringing down less likely outcomes.
            // Will NOT change the overall win chance or make any currently existing outcomes impossible or certain.

            if (_balanceConfig.OutcomePower == 1.0)
            {
                return;
            }

            for (int i = 0; i < _attackLossChances.Length - 1; i++)
            {
                _attackLossChances[i] = Math.Pow(_attackLossChances[i], _balanceConfig.OutcomePower);
            }

            for (int i = 0; i < _defendLossChances.Length - 1; i++)
            {
                _defendLossChances[i] = Math.Pow(_defendLossChances[i], _balanceConfig.OutcomePower);
            }

            if (_battleConfig.StopUntil > 0)
            {
                MathUtil.NormalizeSum(_attackLossChances, AttackWinChance, 0, _attackLossChances.Length - 1);
                MathUtil.NormalizeSum(_defendLossChances, UnresolvedChance, 0, _defendLossChances.Length - 1);
            }
            else
            {
                MathUtil.NormalizeSum(_attackLossChances, AttackWinChance, 0, _attackLossChances.Length - 1);
                MathUtil.NormalizeSum(_defendLossChances, DefendWinChance, 0, _defendLossChances.Length - 1);
            }
        }
    }
}