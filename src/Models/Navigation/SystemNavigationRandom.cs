using System;

namespace VikingJamGame.Models.Navigation;

public sealed class SystemNavigationRandom(Random? random = null) : INavigationRandom
{
    private readonly Random _random = random ?? Random.Shared;

    public int NextInt(int minInclusive, int maxExclusive) =>
        _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
