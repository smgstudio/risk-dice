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
using Sirenix.OdinInspector;
using Risk.Dice.RNG;
using Risk.Dice.Utility;
using UnityEngine;

#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif

namespace Risk.Dice
{
    [Serializable]
    public sealed class BattleSimulator
    {
        public enum RoundMethod
        {
            DiceRoll,
            OddsBased
        }

        public enum BlitzMethod
        {
            DiceRoll,
            OddsBasedRound,
            OddsBasedBattle,
        }

        public enum StatusType
        {
            Unresolved,
            AttackerWin,
            DefenderWin
        }

        private IRNG _rng;

        [SerializeField] private RoundConfig _roundConfig;
        [SerializeField] private BattleConfig _battleConfig;
        [SerializeField] private BalanceConfig _balanceConfig;
        [SerializeField] private int _remainingAttackCount;
        [SerializeField] private int _remainingDefendCount;
        [SerializeField] private int _lastAttackLossCount;
        [SerializeField] private int _lastDefendLossCount;
        [SerializeField] private int[] _attackDiceRollTally;
        [SerializeField] private int[] _defendDiceRollTally;
        [SerializeField] private List<int> _attackDiceRolls;
        [SerializeField] private List<int> _defendDiceRolls;
        [SerializeField] private List<int> _lastAttackDiceRolls;
        [SerializeField] private List<int> _lastDefendDiceRolls;
        [SerializeField] private int _simulatedAttackDiceRollCount;
        [SerializeField] private int _simulatedDefendDiceRollCount;

        public RoundConfig RoundConfig => _roundConfig;
        public BattleConfig BattleConfig => _battleConfig;
        public BalanceConfig BalanceConfig => _balanceConfig;
        public int RemainingAttackCount => _remainingAttackCount;
        public int RemainingDefendCount => _remainingDefendCount;
        public int LastAttackLossCount => _lastAttackLossCount;
        public int LastDefendLossCount => _lastDefendLossCount;
        public int AttackLossCount => _battleConfig.AttackUnitCount - _remainingAttackCount;
        public int DefendLossCount => _battleConfig.DefendUnitCount - _remainingDefendCount;
        public int[] AttackDiceRollTally => _attackDiceRollTally;
        public int[] DefendDiceRollTally => _defendDiceRollTally;
        public List<int> AttackDiceRolls => _attackDiceRolls;
        public List<int> DefendDiceRolls => _defendDiceRolls;
        public List<int> LastAttackDiceRolls => _lastAttackDiceRolls;
        public List<int> LastDefendDiceRolls => _lastDefendDiceRolls;
        public int SimulatedAttackDiceRollCount => _simulatedAttackDiceRollCount;
        public int SimulatedDefendDiceRollCount => _simulatedDefendDiceRollCount;

        [ShowInInspector] public bool IsComplete => _remainingAttackCount <= _battleConfig.StopUntil || _remainingDefendCount == 0;
        [ShowInInspector] public bool IsAttackerWin => IsComplete && _remainingDefendCount == 0;

        public BattleSimulator (int attackUnitCount, int defendUnitCount) : this(new BattleConfig(attackUnitCount, defendUnitCount, 0))
        {
        }

        public BattleSimulator (BattleConfig battleConfig) : this(battleConfig, RoundConfig.Default)
        {
        }

        public BattleSimulator (BattleConfig battleConfig, RoundConfig roundConfig) : this(battleConfig, roundConfig, RNGConfig.Default)
        {
        }

        public BattleSimulator (BattleConfig battleConfig, RoundConfig roundConfig, RNGConfig rngConfig) : this(battleConfig, roundConfig, null, rngConfig)
        {
        }

        public BattleSimulator (BattleConfig battleConfig, RoundConfig roundConfig, BalanceConfig balanceConfig) : this(battleConfig, roundConfig, balanceConfig, RNGConfig.Default)
        {
        }

        public BattleSimulator (BattleConfig battleConfig, RoundConfig roundConfig, BalanceConfig balanceConfig, RNGConfig rngConfig) : this(battleConfig, roundConfig, balanceConfig, rngConfig.GetRNG())
        {
        }

        public BattleSimulator (BattleConfig battleConfig, RoundConfig roundConfig, BalanceConfig balanceConfig, IRNG rng)
        {
            _rng = rng;
            _roundConfig = roundConfig;
            _battleConfig = battleConfig;
            _balanceConfig = balanceConfig;

            _remainingAttackCount = battleConfig.AttackUnitCount;
            _remainingDefendCount = battleConfig.DefendUnitCount;

            _attackDiceRollTally = new int[roundConfig.DiceFaceCount];
            _defendDiceRollTally = new int[roundConfig.DiceFaceCount];

            _attackDiceRolls = new List<int>();
            _defendDiceRolls = new List<int>();

            _lastAttackDiceRolls = new List<int>(roundConfig.AttackDiceCount);
            _lastDefendDiceRolls = new List<int>(roundConfig.DefendDiceCount);
        }

        public void SetRNG (RNGConfig rngConfig)
        {
#if UNITY_ASSERTIONS
            Assert.IsTrue(rngConfig.IsValid);
#endif

            _rng = rngConfig.GetRNG();
        }

        public StatusType GetStatus ()
        {
            if (_remainingDefendCount <= 0)
            {
                return StatusType.AttackerWin;
            }
            else if (_remainingAttackCount <= 0)
            {
                return StatusType.DefenderWin;
            }
            else
            {
                return StatusType.Unresolved;
            }
        }

        public void NextRound (RoundMethod method)
        {
            switch (method)
            {
                case RoundMethod.DiceRoll:
                    RoundDiceRoll();
                    break;
                case RoundMethod.OddsBased:
                    RoundOddsBased();
                    break;
            }
        }

        public void Blitz (BlitzMethod method)
        {
            if (IsComplete)
            {
                return;
            }

            switch (method)
            {
                case BlitzMethod.DiceRoll:
                    {
                        while (!IsComplete)
                        {
                            RoundDiceRoll();
                        }

                        break;
                    }

                case BlitzMethod.OddsBasedRound:
                    {
                        while (!IsComplete)
                        {
                            RoundOddsBased();
                        }

                        break;
                    }

                case BlitzMethod.OddsBasedBattle:
                    {
                        BlitzOddsBased();
                        break;
                    }
            }
        }

        private void RoundDiceRoll ()
        {
            if (IsComplete)
            {
                return;
            }

            BattleConfig currentBattleConfig = _battleConfig.WithNewUnits(_remainingAttackCount, _remainingDefendCount);
            RoundConfig currentRoundConfig = _roundConfig.WithBattle(currentBattleConfig);

            // Roll the dice

            _lastAttackDiceRolls.Clear();
            _lastDefendDiceRolls.Clear();

            for (int i = 0; i < currentRoundConfig.AttackDiceCount; i++)
            {
                int diceRoll = _rng.NextInt(0, currentRoundConfig.DiceFaceCount);

                _attackDiceRolls.Add(diceRoll);
                _lastAttackDiceRolls.Add(diceRoll);
                _attackDiceRollTally[diceRoll]++;
            }

            for (int i = 0; i < currentRoundConfig.DefendDiceCount; i++)
            {
                int diceRoll = _rng.NextInt(0, currentRoundConfig.DiceFaceCount);

                _defendDiceRolls.Add(diceRoll);
                _lastDefendDiceRolls.Add(diceRoll);
                _defendDiceRollTally[diceRoll]++;
            }

            // Sort the dice

            DescendingIntComparer descendingIntComparer = new DescendingIntComparer();

            _lastAttackDiceRolls.Sort(descendingIntComparer);
            _lastDefendDiceRolls.Sort(descendingIntComparer);

            // Compare the dice

            int attackLossCount = 0;
            int challengeCount = currentRoundConfig.ChallengeCount;

            for (int i = 0; i < challengeCount; i++)
            {
                if (_lastAttackDiceRolls[i] == _lastDefendDiceRolls[i])
                {
                    if (currentRoundConfig.FavourDefenderOnDraw)
                    {
                        attackLossCount++;
                    }
                }
                else if (_lastAttackDiceRolls[i] < _lastDefendDiceRolls[i])
                {
                    attackLossCount++;
                }
            }

            int defendLossCount = challengeCount - attackLossCount;

#if UNITY_ASSERTIONS
            Assert.IsFalse(attackLossCount == 0 && defendLossCount == 0);
            Assert.IsFalse(attackLossCount > _remainingAttackCount);
            Assert.IsFalse(defendLossCount > _remainingDefendCount);
#endif

            _lastAttackLossCount = attackLossCount;
            _lastDefendLossCount = defendLossCount;

            _remainingAttackCount -= attackLossCount;
            _remainingDefendCount -= defendLossCount;
        }

        private void RoundOddsBased ()
        {
            if (IsComplete)
            {
                return;
            }

            BattleConfig currentBattleConfig = _battleConfig.WithNewUnits(_remainingAttackCount, _remainingDefendCount);
            RoundConfig currentRoundConfig = _roundConfig.WithBattle(currentBattleConfig);
            RoundInfo roundInfo = RoundCache.Get(currentRoundConfig);
            roundInfo.Calculate();

            ApplyOddsBasedRound(roundInfo);
        }

        private void BlitzOddsBased ()
        {
            if (IsComplete)
            {
                return;
            }

            BattleConfig currentBattleConfig = _battleConfig.WithNewUnits(_remainingAttackCount, _remainingDefendCount).WithoutStopUntil();
            BattleInfo battleInfo = BattleCache.Get(_roundConfig, currentBattleConfig);
            battleInfo.Calculate();

            if (_balanceConfig != null)
            {
                BalancedBattleInfo balancedBattleInfo = new BalancedBattleInfo(battleInfo, _balanceConfig);
                balancedBattleInfo.ApplyBalance();

                battleInfo = balancedBattleInfo;
            }

            ApplyOddsBasedBattle(battleInfo);
        }

        private void ApplyOddsBasedRound (RoundInfo roundInfo)
        {
            RoundConfig roundConfig = roundInfo.Config;

            // Determine outcome

            int attackLossCount = 0;
            int defendLossCount = 0;

            double random = MathUtil.Clamp01(_rng.NextDouble());
            double current = 0.0;

            for (int i = 0; i < roundInfo.AttackLossChances.Length; i++)
            {
                if (roundInfo.AttackLossChances[i] <= 0)
                {
                    continue;
                }

                current += roundInfo.AttackLossChances[i];

                if (current >= random)
                {
                    attackLossCount = i;
                    defendLossCount = roundConfig.ChallengeCount - i;
                    break;
                }
            }

#if UNITY_ASSERTIONS
            Assert.IsFalse(attackLossCount == 0 && defendLossCount == 0);
            Assert.IsFalse(attackLossCount > _remainingAttackCount);
            Assert.IsFalse(defendLossCount > _remainingDefendCount);
#endif

            _simulatedAttackDiceRollCount += roundConfig.AttackDiceCount;
            _simulatedDefendDiceRollCount += roundConfig.DefendDiceCount;

            _lastAttackDiceRolls.Clear();
            _lastDefendDiceRolls.Clear();

            _lastAttackLossCount = attackLossCount;
            _lastDefendLossCount = defendLossCount;

            _remainingAttackCount -= attackLossCount;
            _remainingDefendCount -= defendLossCount;
        }

        private void ApplyOddsBasedBattle (BattleInfo battleInfo)
        {
            BattleConfig battleConfig = battleInfo.BattleConfig;

            // Determine outcome

            int attackLossCount = -1;
            int defendLossCount = -1;

            double random = MathUtil.Clamp01Exclusive(_rng.NextDouble());
            double current = 0.0;

            if (random <= battleInfo.AttackWinChance)
            {
                // Attacker win

                defendLossCount = battleConfig.DefendUnitCount;

                for (int i = 0; i < battleInfo.AttackLossChances.Length - 1; i++)
                {
                    if (battleInfo.AttackLossChances[i] <= 0)
                    {
                        continue;
                    }

                    current += battleInfo.AttackLossChances[i];

                    if (current >= random)
                    {
                        attackLossCount = i;
                        break;
                    }
                }

                if (attackLossCount == -1)
                {
                    battleInfo.AttackLossChances.Max(out attackLossCount);
                }
            }
            else
            {
                random -= battleInfo.AttackWinChance;

                // Defender win

                attackLossCount = battleConfig.AttackUnitCount - battleConfig.StopUntil;

                for (int i = 0; i < battleInfo.DefendLossChances.Length - 1; i++)
                {
                    if (battleInfo.DefendLossChances[i] <= 0)
                    {
                        continue;
                    }

                    current += battleInfo.DefendLossChances[i];

                    if (current >= random)
                    {
                        defendLossCount = i;
                        break;
                    }
                }

                if (defendLossCount == -1)
                {
                    battleInfo.DefendLossChances.Max(out defendLossCount);
                }
            }

#if UNITY_ASSERTIONS
            Assert.IsFalse(attackLossCount == -1 || defendLossCount == -1);
            Assert.IsFalse(attackLossCount == 0 && defendLossCount == 0);
            Assert.IsFalse(attackLossCount > _remainingAttackCount);
            Assert.IsFalse(defendLossCount > _remainingDefendCount);
#endif

            _simulatedAttackDiceRollCount += attackLossCount + defendLossCount;
            _simulatedDefendDiceRollCount += attackLossCount + defendLossCount;

            _lastAttackDiceRolls.Clear();
            _lastDefendDiceRolls.Clear();

            _lastAttackLossCount = attackLossCount;
            _lastDefendLossCount = defendLossCount;

            _remainingAttackCount -= attackLossCount;
            _remainingDefendCount -= defendLossCount;
        }

        public void Reset ()
        {
            _remainingAttackCount = _battleConfig.AttackUnitCount;
            _remainingDefendCount = _battleConfig.DefendUnitCount;

            for (int i = 0; i < _roundConfig.DiceFaceCount; i++)
            {
                _attackDiceRollTally[i] = 0;
                _defendDiceRollTally[i] = 0;
            }

            _attackDiceRolls.Clear();
            _defendDiceRolls.Clear();

            _lastAttackDiceRolls.Clear();
            _lastDefendDiceRolls.Clear();

            _simulatedAttackDiceRollCount = 0;
            _simulatedDefendDiceRollCount = 0;
        }
    }
}