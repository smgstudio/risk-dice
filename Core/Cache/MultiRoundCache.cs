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

namespace Risk.Dice
{
    // Data holder for a distribution of losses.  This can represent a
    // distribution of attacker losses for a fixed number of defender losses,
    // in which case the attacker losses increase by roundConfig.ChallengeCount
    // as the array index increases by 1, or it can represent a distribution of
    // defender losses for a fixed number of attacker losses, in which case the
    // defender losses increase by roundConfig.ChallengeCount as the array index
    // increases by 1.  In both cases initialLoss refers to the loss at array
    // index zero.  Also in both cases this is not necessarily a complete
    // distribution and the sum of all outcomeChances here may be less than one.
    //
    // Internal to MultiRoundCacheInfo we also use this for a distribution of
    // losses over a fixed number of rounds.  In this use case initialLoss is
    // the number of attacker troops lost at array index zero and each increment
    // of the array index indicates one fewer attacker troop lost and one more
    // defender troop lost.
    public class MultiRoundLossInfo
    {
        public int InitialLoss;
        public double[] OutcomeChances;
        public MultiRoundLossInfo (int initialLoss, double[] outcomeChances)
        {
            InitialLoss = initialLoss;
            OutcomeChances = outcomeChances;
        }
    }

    // Holder for all of the information about multiple rounds of one single RoundConfig
    public class MultiRoundCacheInfo
    {
        private RoundInfo _roundInfo;

        // List index i represents losses after i rounds
        private List<MultiRoundLossInfo> _losses;

        // Cutoff to determine when odds are too small to affect calculations
        private const double _oddsCutoff = 1e-16;

        // Cutoff for the number of rounds where we compute exactly instead of
        // using a gaussian approximation
        private const int _maxRounds = 1000;

        // Precomputed variables for gaussian approximation
        private double _roundMean;
        private double _roundVar;
        private double _logCutoff;

        public int ChallengeCount => _roundInfo.Config.ChallengeCount;

        public MultiRoundCacheInfo (RoundConfig roundConfig)
        {
            _roundInfo = RoundCache.Get(roundConfig);
            _roundInfo.Calculate();
            _losses = new List<MultiRoundLossInfo>();
            MultiRoundLossInfo nullLosses = new MultiRoundLossInfo(0, new double[1]);
            nullLosses.OutcomeChances[0] = 1;
            _losses.Add(nullLosses);
            _roundMean = 0;
            double squaresMean = 0;
            for (int i = 1; i < _roundInfo.AttackLossChances.Length; i++)
            {
                _roundMean += i * _roundInfo.AttackLossChances[i];
                squaresMean += i * i * _roundInfo.AttackLossChances[i];
            }
            _roundVar = squaresMean - _roundMean * _roundMean;
            _logCutoff = -Math.Log(2 * _oddsCutoff * _oddsCutoff * Math.PI * _roundVar);
        }

        // Given a distribution of loss info for N rounds, compute and return
        // the distribution of loss info for N+1 rounds.
        private MultiRoundLossInfo GetNext (MultiRoundLossInfo lastLossInfo)
        {
            int initialLossIncrease = 0;
            int finalLossIncrease = ChallengeCount;

            // Determine how few troops the attackers can lose while keeping the
            // odds of losing so few troops above the odds cutoff.
            while (initialLossIncrease < ChallengeCount)
            {
                double startOdds = 0;
                for (int i = 0; i <= initialLossIncrease; i++)
                {
                    startOdds += lastLossInfo.OutcomeChances[initialLossIncrease - i] * _roundInfo.AttackLossChances[i];
                }
                if (startOdds > _oddsCutoff)
                {
                    break;
                }
                initialLossIncrease++;
            }

            // Determine how few troops the defenders can lose while keeping the
            // odds of losing so few troops above the odds cutoff.
            while (finalLossIncrease > 0)
            {
                double endOdds = 0;
                int outcomeOffset = lastLossInfo.OutcomeChances.Length - 1;
                for (int i = finalLossIncrease; i <= ChallengeCount; i++)
                {
                    endOdds += lastLossInfo.OutcomeChances[outcomeOffset + finalLossIncrease - i] * _roundInfo.AttackLossChances[i];
                }
                if (endOdds > _oddsCutoff)
                {
                    break;
                }
                finalLossIncrease--;
            }

            double[] outcomeChances = new double[lastLossInfo.OutcomeChances.Length - initialLossIncrease + finalLossIncrease];

            // Populate outcome chances for the next round
            for (int i = 0; i < outcomeChances.Length; i++)
            {
                for (int a = 0; a <= ChallengeCount; a++)
                {
                    int j = i - a + initialLossIncrease;
                    if (j >= lastLossInfo.OutcomeChances.Length)
                    {
                        continue;
                    }
                    if (j < 0)
                    {
                        break;
                    }
                    outcomeChances[i] += lastLossInfo.OutcomeChances[j] * _roundInfo.AttackLossChances[a];
                }
            }

            int initialLoss = lastLossInfo.InitialLoss + initialLossIncrease;
            return new MultiRoundLossInfo(initialLoss, outcomeChances);
        }

        // Return whether any of the nonzero outcome chances in the given round
        // have losses strictly less than both limits at the same time.
        private bool CanCoverLosses (int round, int attackerLoss, int defenderLoss)
        {
            if (round * ChallengeCount > attackerLoss + defenderLoss - 2)
            {
                return false;
            }
            MultiRoundLossInfo lossInfo = _losses[round];
            if (lossInfo.InitialLoss >= attackerLoss)
            {
                return false;
            }
            if (round * ChallengeCount - lossInfo.InitialLoss - lossInfo.OutcomeChances.Length + 1 >= defenderLoss)
            {
                return false;
            }
            return true;
        }

        // Find a round number where the odds of the given attacker or
        // defender loss are (according to gaussian approximation) exactly our
        // odds cutoff.  Passing which = 1 will give a round where the more
        // likely losses are higher and which = -1 will give a round where the
        // more likely losses are lower.
        private double gaussianLossRound (int loss, bool isAttacker, int which) {
            double lossMean = isAttacker? _roundMean : ChallengeCount - _roundMean;
            double round = loss / lossMean;
            double b = 2 * loss * lossMean;
            double scale = 0.5 / (lossMean * lossMean);
            for (int i = 0; i < 2; i++)
            {
                double d = (_logCutoff - Math.Log(round)) * _roundVar;
                round = scale * (b + d + which * Math.Sqrt(d * (2 * b + d)));
            }
            return round;
        }

        // Return the last round number where any outcome in that round has
        // losses strictly less than both limits at the same time AND the
        // odds of such an outcome is more than our cutoff.
        private int lastRelevantGaussianRound (int attackerLoss, int defenderLoss)
        {
            double round = Math.Min(gaussianLossRound(attackerLoss, true, 1), gaussianLossRound(defenderLoss, false, 1));
            return Math.Min((int)(round + 1), (attackerLoss + defenderLoss - 2) / ChallengeCount);
        }

        // Return the last round number where CanCoverLosses is true for that
        // round.  We cache more round losses as needed to make sure we actually
        // have loss info for the returned round.
        private int lastRelevantRound (int attackerLoss, int defenderLoss)
        {
            int round = -1;

            // First we make sure that we have enough rounds cached.  If we
            // cache up to our maximum size and don't have enough we just return
            // the round number according to gaussian approximation.
            while (CanCoverLosses(_losses.Count - 1, attackerLoss, defenderLoss))
            {
                if (_losses.Count > _maxRounds)
                {
                    return lastRelevantGaussianRound(attackerLoss, defenderLoss);
                }
                round = _losses.Count - 1;
                _losses.Add(GetNext(_losses[_losses.Count - 1]));
            }

            // If we added rounds then we already know where the last round is
            if (round >= 0)
            {
                return round;
            }

            // Otherwise we bisect to find the round
            int bottom = 0;
            int top = _losses.Count - 1;
            while (top > bottom + 1)
            {
                int middle = (top + bottom) / 2;
                if (CanCoverLosses(middle, attackerLoss, defenderLoss))
                {
                    bottom = middle;
                }
                else
                {
                    top = middle;
                }
            }
            return bottom;
        }

        // Return distributions of defender losses that we expect to see if we
        // roll until we reach or exceed the given attacker loss count and then
        // immediately stop rolling.  We return a list of distributions for each
        // attacker loss at or above our limit that we might end up at; each
        // index i in this list corresponds to an loss of attackerLoss + i.
        //
        // The defenderLoss parameter means that we also stop rolling if we reach
        // or exceed this number of lost defenders.  If we hit this limit on the
        // same round that we hit the attacker limit then this is included in the
        // returned distributions and otherwise it is not.
        public List<MultiRoundLossInfo> GetFixedAttackerLoss (int attackerLoss, int defenderLoss)
        {
            int lastRound = lastRelevantRound(attackerLoss, defenderLoss);
            int lastCachedRound = Math.Min(lastRound, _maxRounds);
            MultiRoundLossInfo lastLossInfo = _losses[lastCachedRound];

            // Find the first round that gets within one round of our attacker
            // loss limit.  If it is in our cache we bisect to find it and
            // otherwise we compute it.  This may compute to less than our last
            // relevant round, indicating that there are actually no relevant
            // rounds and we are just done.
            int firstRound;
            int maxAttackerLoss = lastLossInfo.InitialLoss + lastLossInfo.OutcomeChances.Length - 1;
            if (maxAttackerLoss + ChallengeCount < attackerLoss)
            {
                if (lastRound == lastCachedRound)
                {
                    return new List<MultiRoundLossInfo>();
                }
                firstRound = (int)gaussianLossRound(attackerLoss - ChallengeCount - 1, true, -1);
                if (firstRound > lastRound)
                {
                    return new List<MultiRoundLossInfo>();
                }
            }
            else
            {
                int bottom = -1;
                firstRound = lastCachedRound;
                while (firstRound > bottom + 1)
                {
                    int middle = (firstRound + bottom) / 2;
                    MultiRoundLossInfo middleLossInfo = _losses[middle];
                    maxAttackerLoss = middleLossInfo.InitialLoss + middleLossInfo.OutcomeChances.Length - 1;
                    if (maxAttackerLoss + ChallengeCount < attackerLoss)
                    {
                        bottom = middle;
                    }
                    else
                    {
                        firstRound = middle;
                    }
                }
            }

            // Allocate loss distributions
            List<MultiRoundLossInfo> result = new List<MultiRoundLossInfo>(ChallengeCount);
            int baseInitialLoss = (lastRound + 1) * ChallengeCount - attackerLoss;
            for (int a = 0; a < ChallengeCount; a++)
            {
                result.Add(new MultiRoundLossInfo(baseInitialLoss - a, new double[lastRound - firstRound + 1]));
            }

            // For each round, for each outcome in that round that gets us
            // within one round of our loss limit and for each single round
            // outcome that puts us over that limit, accumulate the overall
            // odds into our output loss distribution.  We do this separately
            // for each round from the cache and for each round that we
            // approximate with a gaussian.
            for (int round = firstRound; round <= lastCachedRound; round++)
            {
                MultiRoundLossInfo lossInfo = _losses[round];
                int lossOffset = attackerLoss - lossInfo.InitialLoss;
                int lossStart = Math.Max(1, lossOffset - lossInfo.OutcomeChances.Length + 1);
                int lossEnd = Math.Min(ChallengeCount, lossOffset);
                lossEnd = Math.Min(lossEnd, defenderLoss + lossOffset + lossInfo.InitialLoss - round * ChallengeCount - 1);
                for (int a = lossStart; a <= lossEnd; a++)
                {
                    double outcomeChance = lossInfo.OutcomeChances[lossOffset - a];
                    for (int i = a; i <= ChallengeCount; i++)
                    {
                        result[i-a].OutcomeChances[lastRound-round] += _roundInfo.AttackLossChances[i] * outcomeChance;
                    }
                }
            }
            for (int round = Math.Max(firstRound, lastCachedRound + 1); round <= lastRound; round++)
            {
                double mean = attackerLoss - _roundMean * round;
                double var = _roundVar * round;
                double scale = Math.Pow(2 * var * Math.PI, -0.5);
                double expScale = -0.5 / var;
                int lossEnd = Math.Min(ChallengeCount, defenderLoss + attackerLoss - round * ChallengeCount - 1);
                for (int a = 1; a <= lossEnd; a++)
                {
                    double deviation = mean - a;
                    double outcomeChance = scale * Math.Exp(expScale * deviation * deviation);
                    if (outcomeChance < _oddsCutoff)
                    {
                        continue;
                    }
                    for (int i = a; i <= ChallengeCount; i++)
                    {
                        result[i-a].OutcomeChances[lastRound-round] += _roundInfo.AttackLossChances[i] * outcomeChance;
                    }
                }
            }

            return result;
        }

        // Return distributions of attacker losses that we expect to see if we
        // roll until we reach or exceed the given defender loss count and then
        // immediately stop rolling.  We return a list of distributions for each
        // defender loss at or above our limit that we might end up at; each
        // index i in this list corresponds to an loss of defenderLoss + i.
        //
        // The attackerLoss parameter means that we also stop rolling if we reach
        // or exceed this number of lost attackers.  If we hit this limit on the
        // same round that we hit the defender limit then this is included in the
        // returned distributions and otherwise it is not.
        public List<MultiRoundLossInfo> GetFixedDefenderLoss (int attackerLoss, int defenderLoss)
        {
            int lastRound = lastRelevantRound(attackerLoss, defenderLoss);
            int lastCachedRound = Math.Min(lastRound, _maxRounds);
            MultiRoundLossInfo lastLossInfo = _losses[lastCachedRound];

            // Find the first round that gets within one round of our defender
            // loss limit.  If it is in our cache we bisect to find it and
            // otherwise we compute it.  This may compute to less than our last
            // relevant round, indicating that there are actually no relevant
            // rounds and we are just done.
            int firstRound;
            int maxDefenderLoss = lastRound * ChallengeCount - lastLossInfo.InitialLoss;
            if (maxDefenderLoss + ChallengeCount < defenderLoss)
            {
                if (lastRound == lastCachedRound)
                {
                    return new List<MultiRoundLossInfo>();
                }
                firstRound = (int)gaussianLossRound(defenderLoss - ChallengeCount - 1, false, -1);
                if (firstRound > lastRound)
                {
                    return new List<MultiRoundLossInfo>();
                }
            }
            else
            {
                int bottom = -1;
                firstRound = lastCachedRound;
                while (firstRound > bottom + 1)
                {
                    int middle = (firstRound + bottom) / 2;
                    MultiRoundLossInfo middleLossInfo = _losses[middle];
                    maxDefenderLoss = middle * ChallengeCount - middleLossInfo.InitialLoss;
                    if (maxDefenderLoss + ChallengeCount < defenderLoss)
                    {
                        bottom = middle;
                    }
                    else
                    {
                        firstRound = middle;
                    }
                }
            }

            // Allocate loss distributions
            List<MultiRoundLossInfo> result = new List<MultiRoundLossInfo>(ChallengeCount);
            int baseInitialLoss = (firstRound + 1) * ChallengeCount - defenderLoss;
            for (int a = 0; a < ChallengeCount; a++)
            {
                result.Add(new MultiRoundLossInfo(baseInitialLoss - a, new double[lastRound - firstRound + 1]));
            }

            // For each round, for each outcome in that round that gets us
            // within one round of our loss limit and for each single round
            // outcome that puts us over that limit, accumulate the overall
            // odds into our output loss distribution.  We do this separately
            // for each round from the cache and for each round that we
            // approximate with a gaussian.
            for (int round = firstRound; round <= lastCachedRound; round++)
            {
                MultiRoundLossInfo lossInfo = _losses[round];
                int lossOffset = (round + 1) * ChallengeCount - lossInfo.InitialLoss - defenderLoss;
                int lossStart = Math.Max(0, lossOffset - lossInfo.OutcomeChances.Length + 1);
                int lossEnd = Math.Min(ChallengeCount - 1, lossOffset);
                lossStart = Math.Max(lossStart, lossOffset + lossInfo.InitialLoss + 1 - attackerLoss);
                for (int a = lossStart; a <= lossEnd; a++)
                {
                    double outcomeChance = lossInfo.OutcomeChances[lossOffset - a];
                    for (int i = 0; i <= a; i++)
                    {
                        result[a-i].OutcomeChances[round-firstRound] += _roundInfo.AttackLossChances[i] * outcomeChance;
                    }
                }
            }
            for (int round = Math.Max(firstRound, lastCachedRound + 1); round <= lastRound; round++)
            {
                int roundLoss = (round + 1) * ChallengeCount;
                double mean = roundLoss - defenderLoss - _roundMean * round;
                double var = _roundVar * round;
                double scale = Math.Pow(2 * var * Math.PI, -0.5);
                double expScale = -0.5 / var;
                int lossStart = Math.Max(0, roundLoss + 1 - attackerLoss - defenderLoss);
                for (int a = lossStart; a < ChallengeCount; a++)
                {
                    double deviation = mean - a;
                    double outcomeChance = scale * Math.Exp(expScale * deviation * deviation);
                    if (outcomeChance < _oddsCutoff)
                    {
                        continue;
                    }
                    for (int i = 0; i <= a; i++)
                    {
                        result[a-i].OutcomeChances[round-firstRound] += _roundInfo.AttackLossChances[i] * outcomeChance;
                    }
                }
            }

            return result;
        }
    }

    public static class MultiRoundCache
    {
        private static readonly object _lock = new object();
        private static Dictionary<RoundConfig, MultiRoundCacheInfo> _cache = new Dictionary<RoundConfig, MultiRoundCacheInfo>(new RoundConfigComparer());

        public static MultiRoundCacheInfo Get (RoundConfig roundConfig)
        {
            MultiRoundCacheInfo multiRoundCacheInfo = default;

            lock (_lock)
            {
                if (!_cache.ContainsKey(roundConfig))
                {
                    _cache[roundConfig] = new MultiRoundCacheInfo(roundConfig);
                }

                multiRoundCacheInfo = _cache[roundConfig];
            }

            return multiRoundCacheInfo;
        }

        public static void Clear ()
        {
            lock (_lock)
            {
                _cache.Clear();
            }
        }
    }
}
