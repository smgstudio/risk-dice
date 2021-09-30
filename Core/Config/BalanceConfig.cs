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
using UnityEngine;

namespace Risk.Dice
{
    [Serializable]
    public sealed class BalanceConfig : IEquatable<BalanceConfig>
    {
        [SerializeField] private double _winChanceCutoff;
        [SerializeField] private double _winChancePower;
        [SerializeField] private double _outcomeCutoff;
        [SerializeField] private double _outcomePower;

        public double WinChanceCutoff => _winChanceCutoff;
        public double WinChancePower => _winChancePower;
        public double OutcomeCutoff => _outcomeCutoff;
        public double OutcomePower => _outcomePower;

        public static BalanceConfig Default => new BalanceConfig(0.05, 1.3, 0.1, 1.8);

        public BalanceConfig (double winChanceCutoff, double winChancePower, double outcomeCutoff, double outcomePower)
        {
            _winChanceCutoff = winChanceCutoff;
            _winChancePower = winChancePower;
            _outcomeCutoff = outcomeCutoff;
            _outcomePower = outcomePower;
        }

        public bool Equals (BalanceConfig other)
        {
            if (other == null
                || _winChanceCutoff != other._winChanceCutoff
                || _outcomePower != other._outcomePower)
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode ()
        {
            int hashCode = -762923008;
            hashCode = hashCode * -1521134295 + _winChanceCutoff.GetHashCode();
            hashCode = hashCode * -1521134295 + _winChancePower.GetHashCode();
            hashCode = hashCode * -1521134295 + _outcomeCutoff.GetHashCode();
            hashCode = hashCode * -1521134295 + _outcomePower.GetHashCode();
            return hashCode;
        }
    }
}