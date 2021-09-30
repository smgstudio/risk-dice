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
using Risk.Dice.RNG;
using Risk.Dice.Utility;

namespace Risk.Dice
{
    public static class SimulationHelper
    {
        public static float CalculateWinChance (BattleConfig battleConfig, RoundConfig roundConfig, BalanceConfig balanceConfig = null)
        {
            int requiredSize = Math.Max(battleConfig.AttackUnitCount - battleConfig.StopUntil, battleConfig.DefendUnitCount);
            WinChanceInfo winChanceInfo = WinChanceCache.Get(requiredSize, roundConfig, balanceConfig);

            if (!winChanceInfo.IsReady)
            {
                winChanceInfo.Calculate();
            }

            return winChanceInfo.WinChances[battleConfig.AttackUnitCount - battleConfig.StopUntil, battleConfig.DefendUnitCount];
        }

        public static int CalculateIdealUnits (int defendUnits, float winChanceThreshold, RoundConfig roundConfig, BalanceConfig balanceConfig = null)
        {
            winChanceThreshold = MathUtil.Clamp(winChanceThreshold, 0.01f, 0.99f);

            if (defendUnits <= 0)
            {
                return 1;
            }

            int idealAttackUnits = MathUtil.RoundToInt(MathUtil.Normalize(winChanceThreshold, 0, 1, 0, defendUnits * 2f));
            BattleConfig battleConfig = new BattleConfig(idealAttackUnits, defendUnits, 0);
            float winChance = CalculateWinChance(battleConfig, roundConfig, balanceConfig);

            if (winChance >= winChanceThreshold)
            {
                // Search by incrementing attack units down

                bool foundTurningPoint;
                int currentAttackUnits = idealAttackUnits;

                do
                {
                    currentAttackUnits--;

                    if (currentAttackUnits <= 1)
                    {
                        break;
                    }

                    battleConfig = battleConfig.WithNewUnits(currentAttackUnits, defendUnits);

                    winChance = CalculateWinChance(battleConfig, roundConfig, balanceConfig);
                    foundTurningPoint = winChance <= winChanceThreshold;

                    if (!foundTurningPoint)
                    {
                        idealAttackUnits = currentAttackUnits;
                    }
                }
                while (!foundTurningPoint);
            }
            else
            {
                // Search by incrementing attack units up

                bool foundTurningPoint;
                int currentAttackUnits = idealAttackUnits;

                do
                {
                    currentAttackUnits++;

                    battleConfig = battleConfig.WithNewUnits(currentAttackUnits, defendUnits);

                    winChance = CalculateWinChance(battleConfig, roundConfig, balanceConfig);
                    foundTurningPoint = winChance >= winChanceThreshold;

                    if (!foundTurningPoint)
                    {
                        idealAttackUnits = currentAttackUnits;
                    }
                }
                while (!foundTurningPoint);
            }

            return idealAttackUnits;
        }
    }
}