

# risk-dice
This is the dice code used in the [RISK: Global Domination](https://www.hasbrorisk.com/invite) game written in C# for Unity as of version 3.3.

- Calculates all outcome probabilities for a given Risk battle using brute-force
- Includes the Balanced Blitz dice mode algorithm
- Supports dice augments seen in the game such as Capitals & Zombies
- Supports blitz attack limit (Stop Until)
- Includes six different RNG implementations

# Examples

1. Find the chance of losing 2 troops as the attacker in a standard 3 vs 2 dice round
```csharp
var roundInfo = RoundCache.Get(RoundConfig.Default);
roundInfo.Calculate();

print(roundInfo.AttackLossChances[2]);
// 0.292566872427984
```
2. Find the win chance for attacking 800 zombies with 300 troops
```csharp
var roundConfig = RoundConfig.Default;
roundConfig.ApplyAugments(DiceAugment.None, DiceAugment.IsZombie);
    
var winChanceInfo = WinChanceCache.Get(1000, roundConfig, null);
winChanceInfo.Calculate();
    
print(winChanceInfo.WinChances[300, 800]);
// 0.8897331
```
3. Compare true random and balanced blitz, with the chance of losing 12 out of 30 troops when attacking a capitol territory with 15 troops
```csharp
RoundConfig roundConfig = RoundConfig.Default;
roundConfig.ApplyAugments(DiceAugment.None, DiceAugment.OnCapital);

var battleConfig = new BattleConfig(30, 15, 0);

var battle = BattleCache.Get(roundConfig, battleConfig);
battle.Calculate();

var balancedBattle = new BalancedBattleInfo(battle, BalanceConfig.Default);
balancedBattle.ApplyBalance();

print($"TR: {battle.AttackLossChances[12]} | BB: {balancedBattle.AttackLossChances[12]}");
// TR: 0.0222128001707278 | BB: 0.0100282888709122
```
4. Simulate and print the results of 3 standard 10 vs 10 true random battles using a non-default (MersenneTwister) RNG implementation
```csharp
var battleConfig = new BattleConfig(10, 10, 0);
var rngConfig = new RNGConfig(RNGType.MersenneTwister, SeedMode.Auto);

var simulator = new BattleSimulator(battleConfig, RoundConfig.Default, null, rngConfig);

for (int i = 0; i < 3; i++)
{
    simulator.Blitz(BattleSimulator.BlitzMethod.OddsBasedBattle);
    print($"Battle: {i + 1} | Result: {simulator.GetStatus()} | Remaining: {simulator.RemainingAttackCount} / {simulator.RemainingDefendCount}");
    simulator.Reset();
}

// Battle: 1 | Result: DefenderWin | Remaining: 0 / 4
// Battle: 2 | Result: AttackerWin | Remaining: 4 / 0
// Battle: 3 | Result: AttackerWin | Remaining: 3 / 0
```
5. Simulate 1,000,000 dice rolls using the default RNG (PCG)
```csharp
var rng = RNGConfig.Default.GetRNG();

for (int i = 0; i < 1000000; i++)
    print(rng.NextInt(0, 5));

// 3, 2, 4, 1, 0, 4, 1, 4, 2, 4...
```
6. Calculate the minimum attack troops required to have at least an 80% win chance against 50 defending troops in balanced blitz
```csharp
int idealUnits = SimulationHelper.CalculateIdealUnits(50, 0.8f, RoundConfig.Default, BalanceConfig.Default);

print(idealUnits);
// 49
```
# Additional Info

### One Troop Left Behind

All calculations do **not** include the single attack troop that stays behind when attacking. For example, consider a territory with 20 troops attacking another with 10 troops. The *BattleConfig* setup of that will look like:
```csharp
new BattleConfig(19, 10, 0);
```

### Clearing Caches

When testing performance, it is important to consider clearing the memory cache as that will have a significant impact on results.
```csharp
RoundCache.Clear();
BattleCache.Clear();
WinChanceCache.Clear();
```

### Blitz Methods

There are three methods to choose from when simulating a battle. *Note: Balanced Blitz only supports **OddsBasedBattle***
|Method|RNG Usage|How it works|
|--|--|--|
|DiceRoll|int per dice roll|Simulates each individual dice roll from random ints|
|OddsBasedRound|float per round|Simulates each round result from a single random float|
|OddsBasedBattle|float per battle|Simulates the entire battle from a single random float|

### Limitations

The Balanced Blitz dice mode works by adjusting the true probabilities of every possible outcome in a given Risk battle. Outcome probability does not mean the chance of winning the battle as a whole, but instead considers how many troops are left behind. For example: "attacker winning with 5 troops remaining" or "defender winning with 64 troops remaining".

A brute-force approach is employed to calculate these true outcome probabilities for the Balanced Blitz algorithm to base from. Because of this, large battles get very expensive in terms of CPU usage and memory allocation, especially if a dice augment like Capitals is involved. In an effort to prevent stutters/crashes in older/lower-end devices from these large calculations, an estimation technique is used to determine a result much more quickly.

It works by extrapolating the data from smaller battles of similar attacker vs defender ratio using polynomial regression. The accuracy is not perfect and can result in discrepancies between the game and this code. The estimation source code is **not** provided in this repository, because ideally the brute-force method can be replaced in the future with a much quicker alternative.

# 3rd Party Licenses

 - [Mersenne Twister (BSD 3-Clause)](https://github.com/smgstudio/risk-dice-test/tree/master/Plugins/MersenneTwister)
 - [PCGSharp (MIT)](https://github.com/igiagkiozis/PCGSharp#license)
 - [XorShiftPlus (MIT)](http://codingha.us/2018/12/17/xorshift-fast-csharp-random-number-generator/)

# Copyright

Copyright 2021 SMG Studio.

RISK is a trademark of Hasbro. ©2020 Hasbro.All Rights Reserved.Used under license.

You are hereby granted a non-exclusive, limited right to use and to install, one (1) copy of the
software for internal evaluation purposes only and in accordance with the provisions below.You
may not reproduce, redistribute or publish the software, or any part of it, in any form.

SMG may withdraw this license without notice and/or request you delete any copies of the software
(including backups).

The Agreement does not involve any transfer of any intellectual property rights for the
Software. SMG Studio reserves all rights to the Software not expressly granted in writing to
you.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
