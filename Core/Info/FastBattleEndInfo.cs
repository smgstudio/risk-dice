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
    public class FastBattleEndInfo
    {
        [SerializeField] protected BattleConfig _battleConfig;
        [SerializeField] protected RoundConfig _roundConfig;
        [SerializeField] protected double[] _outcomeChances;
        [SerializeField] protected double _winChance;
        [SerializeField] protected bool _useAllAttackers;
        [SerializeField] protected bool _useAllDefenders;

        // Note that when you retrieve this object from FastBattleEndCache.Get,
        // you might not get a battle config here that has all of the attackers
        // or all of the defenders from the original battle config
        public BattleConfig BattleConfig => _battleConfig;

        public RoundConfig RoundConfig => _roundConfig;

        // When this is set the number of attackers in the battle config here
        // will always match the requested battle config and the last value in
        // OutcomeChances will represent losing all attackers and no defenders
        // When this is not set, the number of attackers in the battle config
        // here may be less than the number requested, and every entry in
        // OutcomeChances will represent losing all defenders with the first
        // representing losing no attackers
        public bool UseAllAttackers => _useAllAttackers;

        // When this is set the number of defenders in the battle config here
        // will always match the requested battle config and the first value in
        // OutcomeChances will represent losing all defenders and no attackers
        // When this is not set, the number of defenders in the battle config
        // here may be less than the number requested, and every entry in
        // OutcomeChances will represent losing all attackers with the last
        // representing losing no defenders
        public bool UseAllDefenders => _useAllDefenders;

        // The beginning of this array represents the best possible outcome for
        // the attacker and the end represents the best possible outcome for the
        // defender.  The exact meaning of each entry depends on UseAllAttackers
        // and UseAllDefenders but each entry always represents a total loss for
        // exact one of the attacker or defender and stepping forward in the
        // array always either increases the attackers losses by 1 or decreases
        // the defenders losses by 1.
        public double[] OutcomeChances => _outcomeChances;

        public double WinChance => _winChance;

        public virtual bool IsReady => _outcomeChances != null && _outcomeChances.Length > 0;

        // Cutoff to determine when odds are too small to affect calculations
        protected const double _oddsCutoff = 1e-16;

        public FastBattleEndInfo (BattleConfig battleConfig, RoundConfig roundConfig)
        {
            _battleConfig = battleConfig;
            _roundConfig = roundConfig;
            _useAllAttackers = true;
            _useAllDefenders = true;
        }

        // This is only expected to be called from FastBattleEndCache.Get()
        public void Calculate ()
        {
            if (IsReady)
            {
                return;
            }

            int attackUnitCount = _battleConfig.AttackUnitCount;
            int defendUnitCount = _battleConfig.DefendUnitCount;

            double[] outcomeChances = new double[attackUnitCount + defendUnitCount];

            RoundConfig roundConfig = _roundConfig.WithBattle(_battleConfig);
            RoundInfo roundInfo = RoundCache.Get(roundConfig);
            roundInfo.Calculate();

            for (int i = 0; i < roundInfo.AttackLossChances.Length; i++)
            {
                double roundChance = roundInfo.AttackLossChances[i];

                if (roundChance <= 0.0)
                {
                    continue;
                }

                int remainingAttackUnitCount = attackUnitCount - i;
                int remainingDefendUnitCount = defendUnitCount - (roundConfig.ChallengeCount - i);

                if (remainingAttackUnitCount <= 0)
                {
                    outcomeChances[outcomeChances.Length - 1] += roundChance;
                }
                else if (remainingDefendUnitCount <= 0)
                {
                    outcomeChances[0] += roundChance;
                }
                else
                {
                    // Battle chain continues
                    BattleConfig nextBattleConfig = _battleConfig.WithNewUnits(remainingAttackUnitCount, remainingDefendUnitCount);
                    FastBattleEndInfo battleInfo = FastBattleEndCache.GetUnlocked(roundConfig, nextBattleConfig);
                    int offset = battleInfo._useAllDefenders? 0 : remainingAttackUnitCount + remainingDefendUnitCount - battleInfo._outcomeChances.Length;

                    for (int a = 0; a < battleInfo._outcomeChances.Length; a++)
                    {
                        outcomeChances[i + a + offset] += roundChance * battleInfo._outcomeChances[a];
                    }
                }
            }

            // Check if we will always get the same results if we add more of
            // whichever side is already ahead.  When attackers are ahead, the
            // odds of any attacker loss that still leaves the attacker with
            // troops equal to the maximum attacker dice roll will not change,
            // and when defenders are ahead, the odds of any defender loss that
            // still leaves the defender with troops equal to the maximum
            // defender dice roll will not change.
            if (attackUnitCount > defendUnitCount && outcomeChances[attackUnitCount - roundConfig.AttackDiceCount] < _oddsCutoff)
            {
                _winChance = 1;
                _outcomeChances = new double[attackUnitCount - roundConfig.AttackDiceCount];
                Array.Copy(outcomeChances, _outcomeChances, attackUnitCount - roundConfig.AttackDiceCount);
                _useAllAttackers = false;
            }
            else if (attackUnitCount < defendUnitCount && outcomeChances[attackUnitCount - 1 + roundConfig.DefendDiceCount] < _oddsCutoff)
            {
                _winChance = 0;
                _outcomeChances = new double[defendUnitCount - roundConfig.DefendDiceCount];
                Array.Copy(outcomeChances, attackUnitCount + roundConfig.DefendDiceCount, _outcomeChances, 0, defendUnitCount - roundConfig.DefendDiceCount);
                _useAllDefenders = false;
            }
            else
            {
                double winChance = 0;
                for (int i = 0; i < attackUnitCount; i++)
                {
                    winChance += outcomeChances[i];
                }
                _winChance = winChance;
                _outcomeChances = outcomeChances;
            }

#if UNITY_ASSERTIONS
            Assert.AreApproximatelyEqual((float) _outcomeChances.SumAsDouble(), (float) 1.0);
#endif
        }

    }
}
