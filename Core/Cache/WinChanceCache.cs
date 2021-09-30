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

namespace Risk.Dice
{
    public static class WinChanceCache
    {
        private static readonly object _lock = new object();
        private static List<WinChanceInfo> _cache = new List<WinChanceInfo>();

        public static List<WinChanceInfo> Cache => _cache;

        public static WinChanceInfo Get (int requiredSize, RoundConfig roundConfig, BalanceConfig balanceConfig)
        {
            int nextSize = GetNextChacheSize(requiredSize);

            WinChanceInfo winChanceInfo = default;
            bool hasBalance = balanceConfig != null;

            lock (_lock)
            {
                foreach (WinChanceInfo winChance in _cache)
                {
                    if (hasBalance && winChance.BalanceConfig != null)
                    {
                        if (winChance.RoundConfig.Equals(roundConfig) && winChance.BalanceConfig.Equals(balanceConfig))
                        {
                            winChanceInfo = winChance;
                            break;
                        }
                    }
                    else if (!hasBalance && winChance.BalanceConfig == null)
                    {
                        if (winChance.RoundConfig.Equals(roundConfig))
                        {
                            winChanceInfo = winChance;
                            break;
                        }
                    }
                }

                if (winChanceInfo == null)
                {
                    winChanceInfo = new WinChanceInfo(nextSize + 1, roundConfig, balanceConfig);
                    _cache.Add(winChanceInfo);
                }
                else if (requiredSize >= winChanceInfo.Size)
                {
                    _cache.Remove(winChanceInfo);

                    // TODO: Use smaller win chance size to speed up next calculation

                    winChanceInfo = new WinChanceInfo(nextSize + 1, roundConfig, balanceConfig);
                    _cache.Add(winChanceInfo);
                }
            }

            return winChanceInfo;
        }

        public static void Clear ()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }

        private static int GetNextChacheSize (int requiredSize)
        {
            return Math.Max(MathUtil.NextPowerOfTwo(requiredSize + 1), 64);
        }
    }
}