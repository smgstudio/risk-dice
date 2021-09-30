=======================================================================
   Mersenne Twitster Pseudorandom Number Generator Nuget Package
=======================================================================

USAGE:

using MersenneTwister;

Way 1) Easiest Random Number Generation

- Randoms.Next(...)
- Randoms.NextDouble()
- Randoms.NextBytes()

Each method uses a inner singleton well-balanced random object
with ajumbo lock. So for fast or multithread, you may use Way 2.

Way 2) Thread-local Recommended Random objects

- Randoms.WellBalanced   -- totally fast enough
- Randoms.FastestInt32   -- fastest for Next(...) and NextBytes()
- Randoms.FastestDouble  -- fastest for NextDouble()

Each method returns a thread-local System.Random-derived object
with default implementation for purpose. For example,
Randoms.FastestInt32 returns a singleton of MT64Random, a
mt19937_64 original implemetation class, which is the featest for
processing Next().

Way 3) Get a Dedicated Instance or Give a Seed for Initialization

- Randoms.Create(RandomType type = WellBalanced)
- Randoms.Create(Int32 seed, RandomType type = WellBalanced)

Each method returns a new instance of Random.
You have;
- WellBalanced  -> DsfmtRandom
- FastestInt32  -> MT64Random
- FastestDouble -> DsfmtRandom

Way 3) Get a new instance of a Specific Implementation

You can use these below to crate a specific object:
- MTRandom.Create
- MT64Random.Create
- SFMTRandom.Create
- DSFMTRandom.Create

and may give a array of seed for better initialization.


EXTRA:

We provide AccurateRandom that returns more accurate results such that;
- Next(int maxValue) and Next(int minValue, int maxValue) return
   more accurate results by discarding insufficient random results.
   (Probability Bias with Pigion Hole Principle)
- NextDouble returns double value that has 53 bit precision of fraction
  regardless of its exponent. So it returns 4.94065645841247E-324
  aka Double.Epsilon at minimum except for zero.

When you must generate extremely accurate results, use AccurateRandom.

=======================================================================

LICENSE

This software is licensed under the BSD 3-Clause License.
https://opensource.org/licenses/BSD-3-Clause

The large part of this program is derived from other OSSs.
You must follow these licenses in addition to this software's.
For further details take a look at LICENSE-MersenneTwister.txt.

=======================================================================
