namespace VikingJamGame.Models.Navigation;

public sealed class NavigationMapGenerationParameters
{
    public int PeakFrontierWidth { get; init; } = 6;
    public int MaxMergeDistance { get; init; } = 3;
    
    public static NavigationMapGenerationParameters Default { get; } = new();
}
