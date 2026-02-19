namespace VikingJamGame.Models.Navigation;

public interface INavigationRandom
{
    int NextInt(int minInclusive, int maxExclusive);

    double NextDouble();
}
