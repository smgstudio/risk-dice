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
using UnityEngine;

namespace Risk.Dice.Utility
{
    public static class MathUtil
    {
        public static unsafe double Increment (double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value;
            }

            ulong intValue = *(ulong*) &value;

            if (value > 0)
            {
                intValue++;
            }
            else if (value < 0)
            {
                intValue--;
            }
            else if (value == 0)
            {
                return double.Epsilon;
            }

            return *(double*) &intValue;
        }

        public static unsafe double Decrement (double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return value;
            }

            ulong intValue = *(ulong*) &value;

            if (value > 0)
            {
                intValue--;
            }
            else if (value < 0)
            {
                intValue++;
            }
            else if (value == 0)
            {
                return -double.Epsilon;
            }

            return *(double*) &intValue;
        }

        public static double RoundToNearest (double source, double nearest)
        {
            double inverse = Math.Pow(nearest, -1);

            return Math.Round(source * inverse, MidpointRounding.AwayFromZero) / inverse;
        }

        public static double RoundToNearest (double source, double nearest, double zeroOffset)
        {
            double roundedSouce = RoundToNearest(source, nearest);
            double offset = zeroOffset % nearest;

            return roundedSouce + offset;
        }

        public static int NextPowerOfTwo (int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;

            return value;
        }

        public static int FloorToInt (double value)
        {
            return (int) Math.Floor(value);
        }

        public static int RoundToInt (double value)
        {
            return (int) Math.Round(value);
        }

        public static int CeilToInt (double value)
        {
            return (int) Math.Ceiling(value);
        }

        public static bool IsApproximatelyEqual (double a, double b, double tolerance = 0.0001)
        {
            return Math.Abs(a - b) <= tolerance;
        }

        public static int Clamp (int value, int min, int max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static float Clamp (float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static double Clamp (double value, double min, double max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        public static double Clamp01 (double value)
        {
            if (value >= 1.0)
            {
                return 1.0;
            }

            if (value <= 0.0)
            {
                return 0.0;
            }

            return value;
        }

        public static double Clamp01Exclusive (double value)
        {
            if (value >= 1.0)
            {
                return Decrement(1.0);
            }

            if (value <= 0.0)
            {
                return Increment(0.0);
            }

            return value;
        }

        public static double Lerp (double min, double max, double t, bool clamp = false)
        {
            return min + ((max - min) * (clamp ? Clamp01(t) : t));
        }

        public static double Normalize (double source, double curMin, double curMax, double newMin, double newMax, bool clamp = false)
        {
            double t = (source - curMin) / (curMax - curMin);

            return newMin + ((newMax - newMin) * (clamp ? Clamp01(t) : t));
        }

        public static double Normalize (double source, double curMin, double curMax, bool clamp = false)
        {
            return Normalize(source, curMin, curMax, 0, 1, clamp);
        }

        public static double Normalize (double source, double curMax, bool clamp = false)
        {
            return Normalize(source, 0, curMax, 0, 1, clamp);
        }

        public static double PercentDifference (double a, double b)
        {
            return Math.Abs((a - b) / ((a + b) * 0.5)) * 100.0;
        }

        public static double UniformDeviation (double min, double max)
        {
            return Math.Sqrt(Math.Pow(max - min, 2) / 12.0);
        }

        public static double UniformDeviation (int min, int max)
        {
            return Math.Sqrt(Math.Pow(max - min, 2) / 12.0);
        }

        public static double UniformDeviation (uint min, uint max)
        {
            return Math.Sqrt(Math.Pow(max - min, 2) / 12.0);
        }

        public static double UniformDeviation (ulong min, ulong max)
        {
            return Math.Sqrt(Math.Pow(max - min, 2) / 12.0);
        }

        public static double SumAsDouble (this IList<double> values)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumAsDouble (this IList<int> values)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumAsDouble (this IList<uint> values)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumAsDouble (this IList<ulong> values)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumAsDouble (this IList<byte> values)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;

            for (int i = 0; i < values.Count; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumAsDouble (this IList<double> values, int offset, int length = -1)
        {
            if (values == null)
            {
                return default;
            }

            double sum = 0;
            int limit = length >= 0 ? Math.Min(values.Count, offset + length) : values.Count;

            for (int i = offset; i < limit; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        public static double SumOfSquares (IList<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            double squareSum = 0f;

            for (int i = 0; i < values.Count; i++)
            {
                squareSum += values[i] * values[i];
            }

            double average = Mean(values);

            squareSum -= average * average * values.Count;

            return squareSum;
        }

        public static double SumOfSquares (IList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0f;
            }

            double squareSum = 0f;

            for (int i = 0; i < values.Count; i++)
            {
                squareSum += values[i] * values[i];
            }

            double average = Mean(values);

            squareSum -= average * average * values.Count;

            return squareSum;
        }

        public static double Mean (this IList<double> values)
        {
            if (values == null)
            {
                return default;
            }

            return SumAsDouble(values) / values.Count;
        }

        public static double Mean (this IList<int> values)
        {
            if (values == null)
            {
                return default;
            }

            return SumAsDouble(values) / values.Count;
        }

        public static double Mean (this IList<uint> values)
        {
            if (values == null)
            {
                return default;
            }

            return SumAsDouble(values) / values.Count;
        }

        public static double Mean (this IList<ulong> values)
        {
            if (values == null)
            {
                return default;
            }

            return SumAsDouble(values) / values.Count;
        }

        public static double Mean (this IList<byte> values)
        {
            if (values == null)
            {
                return default;
            }

            return SumAsDouble(values) / values.Count;
        }

        public static double Max (this IList<double> values, out int index)
        {
            index = -1;

            if (values == null)
            {
                return default;
            }

            int count = values.Count;
            double max = double.NegativeInfinity;

            for (int i = 0; i < count; i++)
            {
                double value = values[i];

                if (value > max)
                {
                    max = value;
                    index = i;
                }
            }

            return max;
        }

        public static double Median (this IList<double> sortedValues)
        {
            if (sortedValues == null)
            {
                return default;
            }

            int count = sortedValues.Count;

            if (count <= 0)
            {
                return default;
            }

            if (count == 1)
            {
                return sortedValues[0];
            }

            if (count % 2 == 0)
            {
                int index = Mathf.RoundToInt(count * 0.5f);

                double value1 = sortedValues[index];
                double value2 = sortedValues[index - 1];

                return (value1 + value2) * 0.5;
            }
            else
            {
                return sortedValues[Mathf.FloorToInt(count * 0.5f)];
            }
        }

        public static double Median (this IList<int> sortedValues)
        {
            if (sortedValues == null)
            {
                return default;
            }

            int count = sortedValues.Count;

            if (count <= 0)
            {
                return default;
            }

            if (count == 1)
            {
                return sortedValues[0];
            }

            if (count % 2 == 0)
            {
                int index = Mathf.RoundToInt(count * 0.5f);

                int value1 = sortedValues[index];
                int value2 = sortedValues[index - 1];

                return ((double) value1 + value2) * 0.5;
            }
            else
            {
                return sortedValues[Mathf.FloorToInt(count * 0.5f)];
            }
        }

        public static double Median (this IList<uint> sortedValues)
        {
            if (sortedValues == null)
            {
                return default;
            }

            int count = sortedValues.Count;

            if (count <= 0)
            {
                return default;
            }

            if (count == 1)
            {
                return sortedValues[0];
            }

            if (count % 2 == 0)
            {
                int index = Mathf.RoundToInt(count * 0.5f);

                uint value1 = sortedValues[index];
                uint value2 = sortedValues[index - 1];

                return ((double) value1 + value2) * 0.5;
            }
            else
            {
                return sortedValues[Mathf.FloorToInt(count * 0.5f)];
            }
        }

        public static double Median (this IList<ulong> sortedValues)
        {
            if (sortedValues == null)
            {
                return default;
            }

            int count = sortedValues.Count;

            if (count <= 0)
            {
                return default;
            }

            if (count == 1)
            {
                return sortedValues[0];
            }

            if (count % 2 == 0)
            {
                int index = Mathf.RoundToInt(count * 0.5f);

                ulong value1 = sortedValues[index];
                ulong value2 = sortedValues[index - 1];

                return ((double) value1 + value2) * 0.5;
            }
            else
            {
                return sortedValues[Mathf.FloorToInt(count * 0.5f)];
            }
        }

        public static double Median (this IList<byte> sortedValues)
        {
            if (sortedValues == null)
            {
                return default;
            }

            int count = sortedValues.Count;

            if (count <= 0)
            {
                return default;
            }

            if (count == 1)
            {
                return sortedValues[0];
            }

            if (count % 2 == 0)
            {
                int index = Mathf.RoundToInt(count * 0.5f);

                byte value1 = sortedValues[index];
                byte value2 = sortedValues[index - 1];

                return ((double) value1 + value2) * 0.5;
            }
            else
            {
                return sortedValues[Mathf.FloorToInt(count * 0.5f)];
            }
        }

        public static double StandardDeviation (this IList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            double relativeSquaredSum = 0f;
            double mean = Mean(values);

            for (int i = 0; i < values.Count; i++)
            {
                relativeSquaredSum += Math.Pow(values[i] - mean, 2);
            }

            return Math.Sqrt(relativeSquaredSum / (values.Count - 1));
        }

        public static double StandardDeviation (this IList<int> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            double relativeSquaredSum = 0f;
            double mean = Mean(values);

            for (int i = 0; i < values.Count; i++)
            {
                relativeSquaredSum += Math.Pow(values[i] - mean, 2);
            }

            return Math.Sqrt(relativeSquaredSum / (values.Count - 1));
        }

        public static double StandardDeviation (this IList<uint> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            double relativeSquaredSum = 0f;
            double mean = Mean(values);

            for (int i = 0; i < values.Count; i++)
            {
                relativeSquaredSum += Math.Pow(values[i] - mean, 2);
            }

            return Math.Sqrt(relativeSquaredSum / (values.Count - 1));
        }

        public static double StandardDeviation (this IList<ulong> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            double relativeSquaredSum = 0f;
            double mean = Mean(values);

            for (int i = 0; i < values.Count; i++)
            {
                relativeSquaredSum += Math.Pow(values[i] - mean, 2);
            }

            return Math.Sqrt(relativeSquaredSum / (values.Count - 1));
        }

        public static double StandardDeviation (this IList<byte> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            double relativeSquaredSum = 0f;
            double mean = Mean(values);

            for (int i = 0; i < values.Count; i++)
            {
                relativeSquaredSum += Math.Pow(values[i] - mean, 2);
            }

            return Math.Sqrt(relativeSquaredSum / (values.Count - 1));
        }

        public static void NormalizeSum (IList<double> values, double normalizedSum)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            if (normalizedSum <= 0f)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] = 0f;
                }

                return;
            }

            double sum = SumAsDouble(values);

            if (sum <= 0f)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    values[i] = normalizedSum / values.Count;
                }

                return;
            }

            double normalizationRatio = normalizedSum / sum;

            for (int i = 0; i < values.Count; i++)
            {
                values[i] *= normalizationRatio;
            }
        }

        public static void NormalizeSum (IList<double> values, double normalizedSum, int offset, int length = -1)
        {
            if (values == null || values.Count == 0)
            {
                return;
            }

            int limit = length >= 0 ? Math.Min(values.Count, offset + length) : values.Count;

            if (normalizedSum <= 0f)
            {
                for (int i = offset; i < limit; i++)
                {
                    values[i] *= 0f;
                }

                return;
            }

            double sum = SumAsDouble(values, offset, length);

            if (sum <= 0f)
            {
                for (int i = offset; i < limit; i++)
                {
                    values[i] = normalizedSum / (limit - offset);
                }

                return;
            }

            double normalizationRatio = normalizedSum / sum;

            for (int i = offset; i < limit; i++)
            {
                values[i] *= normalizationRatio;
            }
        }
    }
}