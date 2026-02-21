using System;
using System.Collections.Generic;
using VikingJamGame.TemplateUtils;

namespace VikingJamGame.Models;

public record GameDataWrapper
{
    public required GameResources GameResources { get; init; }
    public required PlayerInfo PlayerInfo { get; init; }
}

public readonly record struct TitleDefinition(
    string Name,
    int Population,
    int Food,
    int Gold,
    int Strength,
    int MaxStrength,
    int Honor,
    int MaxHonor,
    int Feats,
    int MaxFeats);

public class InitialResourcesFactory
{
    public static IReadOnlyList<TitleDefinition> Titles { get; } = [
        //                                              Pop  Food Gold Str MaxS Hon MaxH Fts MaxF
        new("the Ironborn",                              24,  18,   8,  9,  18,  6,  14,  0,  12),
        new("the Oathkeeper",                            28,  16,   7,  6,  14,  9,  20,  0,  14),
        new("the Sea Wolf",                              21,  22,   9,  7,  16,  6,  14,  0,  10),
        new("the Stormforged",                           26,  17,   8, 10,  20,  5,  12,  0,  10),
        new("the Hearth-Blessed",                        35,  20,   5,  5,  12,  8,  18,  0,  16),
        new("the Boneless",                              18,  10,   4,  2,   8,  3,  10,  0,   6),
        new("the Oathbreaker",                           20,  12,  12,  5,  16,  2,   8,  0,   8),
        new("the Starved",                               17,   6,   9,  3,  14,  4,  12,  0,  10),
        new("the Coward",                                19,  14,   6,  3,  12,  3,  10,  0,   8),
        new("the Ragged",                                16,   9,   3,  4,  14,  5,  14,  0,  10),
    ];

    public static TitleDefinition RollRandomTitle(Random? random = null)
    {
        var rng = random ?? Random.Shared;
        int index = rng.Next(Titles.Count);
        return Titles[index];
    }
    
    public static GameDataWrapper FromPrologueData(BirthChoice gender, string name)
    {
        TitleDefinition title = RollRandomTitle();
        return FromPrologueData(gender, name, title);
    }

    public static GameDataWrapper FromPrologueData(BirthChoice gender, string name, TitleDefinition title)
    {
        string resolvedName = string.IsNullOrWhiteSpace(name) ? "Nameless" : name.Trim();

        var gameResources = new GameResources();
        gameResources.SetInitialResources(title.Population, title.Food, title.Gold);

        var playerInfo = new PlayerInfo();
        playerInfo.SetInitialInfo(
            resolvedName,
            gender,
            title.Name,
            title.Strength, title.MaxStrength,
            title.Honor, title.MaxHonor,
            title.Feats, title.MaxFeats);

        return new GameDataWrapper
        {
            GameResources = gameResources,
            PlayerInfo = playerInfo,
        };
    }
}
