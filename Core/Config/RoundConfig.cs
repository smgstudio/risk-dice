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
    public struct RoundConfig : IEquatable<RoundConfig>
    {
        [SerializeField] private int _diceFaceCount;
        [SerializeField] private int _attackDiceCount;
        [SerializeField] private int _defendDiceCount;
        [SerializeField] private bool _favourDefenderOnDraw;

        public int DiceFaceCount => _diceFaceCount;
        public int AttackDiceCount => _attackDiceCount;
        public int DefendDiceCount => _defendDiceCount;
        public bool FavourDefenderOnDraw => _favourDefenderOnDraw;

        public int ChallengeCount => Math.Min(_attackDiceCount, _defendDiceCount);

        public static RoundConfig Default => new RoundConfig(6, 3, 2, true);

        public RoundConfig (int diceFaceCount, int attackDiceCount, int defendDiceCount, bool favourDefenderOnDraw)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(diceFaceCount >= 2);
            Assert.IsTrue(attackDiceCount > 0);
            Assert.IsTrue(defendDiceCount > 0);
#endif

            _diceFaceCount = diceFaceCount;
            _attackDiceCount = attackDiceCount;
            _defendDiceCount = defendDiceCount;
            _favourDefenderOnDraw = favourDefenderOnDraw;
        }

        public void SetMaxAttackDice (int maxAttackDice)
        {
            if (maxAttackDice > 0)
            {
                _attackDiceCount = Math.Min(_attackDiceCount, maxAttackDice);
            }
        }

        public void ApplyAugments (DiceAugment attackAugment, DiceAugment defendAugment)
        {
            if ((attackAugment & DiceAugment.IsZombie) != 0)
            {
                _attackDiceCount = Math.Max(_attackDiceCount - 1, 1);
            }

            if ((defendAugment & DiceAugment.OnCapital) != 0)
            {
                _defendDiceCount += 1;
            }

            if ((defendAugment & DiceAugment.IsBehindWall) != 0)
            {
                _defendDiceCount += 1;
            }

            if ((defendAugment & DiceAugment.IsZombie) != 0)
            {
                _favourDefenderOnDraw = false;
            }
        }

        public RoundConfig WithBattle (BattleConfig battleConfig)
        {
            RoundConfig roundConfig = this;
            roundConfig._attackDiceCount = Math.Min(battleConfig.AttackUnitCount - battleConfig.StopUntil, _attackDiceCount);
            roundConfig._defendDiceCount = Math.Min(battleConfig.DefendUnitCount, _defendDiceCount);
            return roundConfig;
        }

        public RoundConfig WithDiceCounts (int attackDiceCount, int defendDiceCount)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(attackDiceCount > 0);
            Assert.IsTrue(defendDiceCount > 0);
#endif

            RoundConfig roundConfig = this;
            roundConfig._attackDiceCount = attackDiceCount;
            roundConfig._defendDiceCount = defendDiceCount;
            return roundConfig;
        }

        public RoundConfig WithAugments (DiceAugment attackAugment, DiceAugment defendAugment)
        {
            RoundConfig roundConfig = this;
            roundConfig.ApplyAugments(attackAugment, defendAugment);
            return roundConfig;
        }

        public bool Equals (RoundConfig other)
        {
            if (_diceFaceCount != other._diceFaceCount
                || _attackDiceCount != other._attackDiceCount
                || _defendDiceCount != other._defendDiceCount
                || _favourDefenderOnDraw != other._favourDefenderOnDraw)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode ()
        {
            int hashCode = 1172639866;
            hashCode = hashCode * -1521134295 + _diceFaceCount.GetHashCode();
            hashCode = hashCode * -1521134295 + _attackDiceCount.GetHashCode();
            hashCode = hashCode * -1521134295 + _defendDiceCount.GetHashCode();
            hashCode = hashCode * -1521134295 + _favourDefenderOnDraw.GetHashCode();
            return hashCode;
        }
    }

    public struct RoundConfigComparer : IEqualityComparer<RoundConfig>
    {
        public bool Equals (RoundConfig x, RoundConfig y)
        {
            return x.Equals(y);
        }

        public int GetHashCode (RoundConfig obj)
        {
            return obj.GetHashCode();
        }
    }
}