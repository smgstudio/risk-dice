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
using Risk.Dice.Utility;
using UnityEngine;

namespace Risk.Dice
{
    [Serializable]
    public sealed class RoundInfo
    {
        [SerializeField] private RoundConfig _config;
        [SerializeField] private double[] _attackLossChances;

        public RoundConfig Config => _config;
        public bool IsReady => _attackLossChances != null && _attackLossChances.Length > 0;
        public double[] AttackLossChances => _attackLossChances;

        internal RoundInfo ()
        {
        }

        public RoundInfo (RoundConfig config)
        {
            _config = config;
        }

        public void Calculate ()
        {
            if (IsReady)
            {
                return;
            }

            // Localize vars

            int diceFaceCount = _config.DiceFaceCount;
            int attackDiceCount = _config.AttackDiceCount;
            int defendDiceCount = _config.DefendDiceCount;
            int powersLength = attackDiceCount + defendDiceCount + 1;
            bool favourDefenderOnDraw = _config.FavourDefenderOnDraw;
            int challengeCount = _config.ChallengeCount;

            // Calculate powers

            double[] attackLossChances = new double[challengeCount + 1];

            int[] powers = new int[powersLength];

            for (int i = 0; i < powersLength; i++)
            {
                powers[i] = (int) Mathf.Pow(diceFaceCount, i);
            }

            // Calculate losses - Setup

            int totalPermutationCount = powers[powersLength - 1];
            int attackPermutationCount = powers[attackDiceCount];
            int defendPermutationCount = powers[defendDiceCount];

            DescendingIntComparer descendingIntComparer = new DescendingIntComparer();

            int[] attackDiceRolls = new int[attackDiceCount];
            int[] defendDiceRolls = new int[defendDiceCount];
            int[] orderedAttackDiceRolls = new int[attackDiceCount];
            int[] orderedDefendDiceRolls = new int[defendDiceCount];

            int[] attackLosses = new int[totalPermutationCount];

            // Calculate losses - Core

            int permutationIndex = 0;

            for (int a = 0; a < attackPermutationCount; a++)
            {
                // Iterate attack roll values

                for (int i = 0; i < attackDiceCount; i++)
                {
                    if (a % powers[i] == 0)
                    {
                        attackDiceRolls[i]++;
                    }

                    if (attackDiceRolls[i] > diceFaceCount)
                    {
                        attackDiceRolls[i] = 1;
                    }
                }

                for (int d = 0; d < defendPermutationCount; d++)
                {
                    // Iterate defend roll values

                    for (int i = 0; i < defendDiceCount; i++)
                    {
                        if (d % powers[i] == 0)
                        {
                            defendDiceRolls[i]++;
                        }

                        if (defendDiceRolls[i] > diceFaceCount)
                        {
                            defendDiceRolls[i] = 1;
                        }
                    }

                    // Sort rolls in descending order

                    Array.Copy(attackDiceRolls, orderedAttackDiceRolls, attackDiceCount);
                    Array.Copy(defendDiceRolls, orderedDefendDiceRolls, defendDiceCount);

                    Array.Sort(orderedAttackDiceRolls, 0, attackDiceCount, descendingIntComparer);
                    Array.Sort(orderedDefendDiceRolls, 0, defendDiceCount, descendingIntComparer);

                    // Determine losses

                    int attackLossCount = 0;

                    for (int i = 0; i < challengeCount; i++)
                    {
                        if (orderedAttackDiceRolls[i] == orderedDefendDiceRolls[i])
                        {
                            if (favourDefenderOnDraw)
                            {
                                attackLossCount++;
                            }
                        }
                        else if (orderedAttackDiceRolls[i] < orderedDefendDiceRolls[i])
                        {
                            attackLossCount++;
                        }
                    }

                    // Store loss counts

                    attackLosses[permutationIndex] = attackLossCount;

                    permutationIndex++;
                }
            }

            // Calculate chances

            for (int i = 0; i < challengeCount + 1; i++)
            {
                int unitLossCount = 0;

                for (int j = 0; j < totalPermutationCount; j++)
                {
                    if (attackLosses[j] == i)
                    {
                        unitLossCount++;
                    }
                }

                attackLossChances[i] = (double) unitLossCount / totalPermutationCount;
            }

            // Complete

            _attackLossChances = attackLossChances;
        }
    }
}