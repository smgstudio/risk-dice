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

using System.Collections.Generic;

namespace Risk.Dice
{
    public static class FastBattleEndCache
    {
        private static readonly object _lock = new object();

        // Every entry in the low attackers cache has the number of attackers
        // equal to the number of attacker dice in the RoundConfig and the
        // number of defenders equal to one more than the list index
        private static Dictionary<RoundConfig, List<FastBattleEndInfo>> _lowAttackersCache = new Dictionary<RoundConfig, List<FastBattleEndInfo>>(new RoundConfigComparer());

        // Every entry in the low defenders cache has the number of defenders
        // equal to the number of defender dice in the RoundConfig and the
        // number of attackers equal to one more than the list index
        private static Dictionary<RoundConfig, List<FastBattleEndInfo>> _lowDefendersCache = new Dictionary<RoundConfig, List<FastBattleEndInfo>>(new RoundConfigComparer());

        // Returns a fully calculated FastBattleEndInfo.  At most one of the
        // Unit counts in the BattleConfig may exceed the matching dice count
        // in the RoundConfig.
        public static FastBattleEndInfo Get (RoundConfig roundConfig, BattleConfig battleConfig)
        {
            FastBattleEndInfo battleInfo = default;

            lock (_lock)
            {
                battleInfo = GetUnlocked(roundConfig, battleConfig);
            }

            return battleInfo;
        }

        // Meant to be called from within another call to Get or GetUnlocked
        public static FastBattleEndInfo GetUnlocked (RoundConfig roundConfig, BattleConfig battleConfig)
        {
            int attackUnitCount = battleConfig.AttackUnitCount;
            int defendUnitCount = battleConfig.DefendUnitCount;

            roundConfig = roundConfig.WithBattle(battleConfig);

            if (attackUnitCount > defendUnitCount)
            {
                if (!_lowDefendersCache.ContainsKey(roundConfig))
                {
                    _lowDefendersCache[roundConfig] = new List<FastBattleEndInfo>();
                }

                List<FastBattleEndInfo> battleInfoList = _lowDefendersCache[roundConfig];

                while (battleInfoList.Count < attackUnitCount)
                {
                    if (battleInfoList.Count > 0 && !battleInfoList[battleInfoList.Count - 1].UseAllAttackers)
                    {
                        return battleInfoList[battleInfoList.Count - 1];
                    }
                    BattleConfig nextBattleConfig = battleConfig.WithNewUnits(battleInfoList.Count + 1, defendUnitCount);
                    FastBattleEndInfo nextBattleInfo = new FastBattleEndInfo(nextBattleConfig, roundConfig);
                    nextBattleInfo.Calculate();
                    battleInfoList.Add(nextBattleInfo);
                }

                return battleInfoList[attackUnitCount - 1];
            }
            else
            {
                if (!_lowAttackersCache.ContainsKey(roundConfig))
                {
                    _lowAttackersCache[roundConfig] = new List<FastBattleEndInfo>();
                }

                List<FastBattleEndInfo> battleInfoList = _lowAttackersCache[roundConfig];

                while (battleInfoList.Count < defendUnitCount)
                {
                    if (battleInfoList.Count > 0 && !battleInfoList[battleInfoList.Count - 1].UseAllDefenders)
                    {
                        return battleInfoList[battleInfoList.Count - 1];
                    }
                    BattleConfig nextBattleConfig = battleConfig.WithNewUnits(attackUnitCount, battleInfoList.Count + 1);
                    FastBattleEndInfo nextBattleInfo = new FastBattleEndInfo(nextBattleConfig, roundConfig);
                    nextBattleInfo.Calculate();
                    battleInfoList.Add(nextBattleInfo);
                }

                return battleInfoList[defendUnitCount - 1];
            }
        }

        public static void Clear ()
        {
            lock (_lock)
            {
                _lowDefendersCache.Clear();
                _lowAttackersCache.Clear();
            }
        }
    }
}
