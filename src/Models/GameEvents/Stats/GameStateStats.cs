using System;
using System.Collections.Generic;

namespace VikingJamGame.Models.GameEvents.Stats;

internal static class GameStateStats
{
    public static bool MeetsAll(
        PlayerInfo playerInfo,
        GameResources gameResources,
        IReadOnlyList<StatAmount> requirements)
    {
        foreach (StatAmount requirement in requirements)
        {
            if (Get(playerInfo, gameResources, requirement.Stat) < requirement.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public static bool CanPayAll(
        PlayerInfo playerInfo,
        GameResources gameResources,
        IReadOnlyList<StatAmount> costs)
    {
        foreach (StatAmount cost in costs)
        {
            if (Get(playerInfo, gameResources, cost.Stat) < cost.Amount)
            {
                return false;
            }
        }

        return true;
    }

    public static void PayAll(
        PlayerInfo playerInfo,
        GameResources gameResources,
        IReadOnlyList<StatAmount> costs)
    {
        foreach (StatAmount cost in costs)
        {
            Spend(playerInfo, gameResources, cost.Stat, cost.Amount);
        }
    }

    public static bool IsGameOverReached(PlayerInfo playerInfo, GameResources gameResources)
    {
        return gameResources.Population == 0 || playerInfo.Strength == 0;
    }

    public static int Get(PlayerInfo playerInfo, GameResources gameResources, StatId id) => id switch
    {
        StatId.Population => gameResources.Population,
        StatId.Food => gameResources.Food,
        StatId.Gold => gameResources.Gold,
        StatId.Strength => playerInfo.Strength,
        StatId.Honor => playerInfo.Honor,
        StatId.Feats => playerInfo.Feats,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };

    public static void Add(PlayerInfo playerInfo, GameResources gameResources, StatId id, int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("Add amount must be >= 0. Use Spend for losses.");
        }

        switch (id)
        {
            case StatId.Population:
                gameResources.AddPopulation(amount);
                break;
            case StatId.Food:
                gameResources.AddFood(amount);
                break;
            case StatId.Gold:
                gameResources.AddGold(amount);
                break;
            case StatId.Strength:
                playerInfo.AddStrength(amount);
                break;
            case StatId.Honor:
                playerInfo.AddHonor(amount);
                break;
            case StatId.Feats:
                playerInfo.AddFeats(amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id));
        }
    }

    internal static void Spend(PlayerInfo playerInfo, GameResources gameResources, StatId id, int amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("Cost amount must be >= 0.");
        }

        switch (id)
        {
            case StatId.Population:
                gameResources.RemovePopulation(amount);
                break;
            case StatId.Food:
                gameResources.SpendFood(amount);
                break;
            case StatId.Gold:
                gameResources.SpendGold(amount);
                break;
            case StatId.Strength:
                playerInfo.RemoveStrength(amount);
                break;
            case StatId.Honor:
                playerInfo.RemoveHonor(amount);
                break;
            case StatId.Feats:
                playerInfo.RemoveFeats(amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id));
        }
    }
}
