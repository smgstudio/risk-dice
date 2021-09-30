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
    public static class RoundCache
    {
        private static readonly object _lock = new object();
        private static RoundInfo _lastRoundInfo;
        private static List<RoundInfo> _cache = new List<RoundInfo>();

        public static List<RoundInfo> Cache => _cache;

        public static RoundInfo Get (RoundConfig config)
        {
            RoundInfo roundInfo = default;

            lock (_lock)
            {
                if (_lastRoundInfo != null && _lastRoundInfo.Config.Equals(config))
                {
                    roundInfo = _lastRoundInfo;
                }

                if (roundInfo == null)
                {
                    foreach (RoundInfo round in _cache)
                    {
                        if (round.Config.Equals(config))
                        {
                            roundInfo = round;
                            break;
                        }
                    }
                }

                if (roundInfo == null)
                {
                    roundInfo = new RoundInfo(config);
                    _cache.Add(roundInfo);
                }

                _lastRoundInfo = roundInfo;
            }

            return roundInfo;
        }

        public static void Clear ()
        {
            _lastRoundInfo = null;

            lock (_lock)
            {
                _cache.Clear();
            }
        }
    }
}