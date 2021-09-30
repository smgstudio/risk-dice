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

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

namespace Risk.Dice
{
    [Serializable]
    public struct BattleConfig : IEquatable<BattleConfig>
    {
        [PropertyTooltip("This does NOT include the extra attack unit that stays behind")]
        [SerializeField] [LabelText("Attack Unit Count (?)")] private int _attackUnitCount;
        [SerializeField] private int _defendUnitCount;
        [SerializeField] private int _stopUntil;

        public int AttackUnitCount => _attackUnitCount;
        public int DefendUnitCount => _defendUnitCount;
        public int StopUntil => _stopUntil;

        public bool IsEarlyStop => _stopUntil > 0;

        public BattleConfig (int attackUnitCount, int defendUnitCount, int stopUntil)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(attackUnitCount > 0);
            Assert.IsTrue(defendUnitCount > 0);
            Assert.IsTrue(stopUntil >= 0);
            Assert.IsTrue(attackUnitCount > stopUntil);
#endif

            _attackUnitCount = attackUnitCount;
            _defendUnitCount = defendUnitCount;
            _stopUntil = stopUntil;
        }

        public BattleConfig WithNewUnits (int attackUnitCount, int defendUnitCount)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(attackUnitCount > 0);
            Assert.IsTrue(defendUnitCount > 0);
#endif

            BattleConfig config = this;
            config._attackUnitCount = attackUnitCount;
            config._defendUnitCount = defendUnitCount;
            return config;
        }

        public BattleConfig WithoutStopUntil ()
        {
            BattleConfig config = this;
            config._attackUnitCount -= _stopUntil;
            config._stopUntil = 0;
            return config;
        }

        public bool Equals (BattleConfig other)
        {
            if (_attackUnitCount != other._attackUnitCount
                    || _defendUnitCount != other._defendUnitCount
                    || _stopUntil != other._stopUntil)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode ()
        {
            int hashCode = -1491950684;
            hashCode = hashCode * -1521134295 + _attackUnitCount.GetHashCode();
            hashCode = hashCode * -1521134295 + _defendUnitCount.GetHashCode();
            hashCode = hashCode * -1521134295 + _stopUntil.GetHashCode();
            return hashCode;
        }
    }
}