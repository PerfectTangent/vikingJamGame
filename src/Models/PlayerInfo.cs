using System;
using VikingJamGame.TemplateUtils;

namespace VikingJamGame.Models;

public sealed class PlayerInfo
{
    public string Name { get; private set; } = "";
    public BirthChoice BirthChoice { get; private set; } = BirthChoice.Boy;
    public string Title { get; private set; } = "";
    public int Strength { get; private set; }
    public int MaxStrength { get; private set; }
    public int Honor { get; private set; }
    public int MaxHonor { get; private set; }
    public int Feats { get; private set; }
    public int MaxFeats { get; private set; }

    public event Action? PlayerInfoChanged;

    public void SetInitialInfo(
        string name, BirthChoice birthChoice, string title,
        int strength, int maxStrength,
        int honor, int maxHonor,
        int feats, int maxFeats)
    {
        Name = name;
        BirthChoice = birthChoice;
        Title = title;
        MaxStrength = maxStrength;
        MaxHonor = maxHonor;
        MaxFeats = maxFeats;
        Strength = Math.Clamp(strength, 0, MaxStrength);
        Honor = Math.Clamp(honor, 0, MaxHonor);
        Feats = Math.Clamp(feats, 0, MaxFeats);
        PlayerInfoChanged?.Invoke();
    }

    public void AddStrength(int amount) => Mutate(() => Strength = Math.Clamp(Strength + amount, 0, MaxStrength));
    public void RemoveStrength(int amount) => Mutate(() => Strength = Math.Clamp(Strength - amount, 0, MaxStrength));

    public void AddHonor(int amount) => Mutate(() => Honor = Math.Clamp(Honor + amount, 0, MaxHonor));
    public void RemoveHonor(int amount) => Mutate(() => Honor = Math.Clamp(Honor - amount, 0, MaxHonor));

    public void AddFeats(int amount) => Mutate(() => Feats = Math.Clamp(Feats + amount, 0, MaxFeats));
    public void RemoveFeats(int amount) => Mutate(() => Feats = Math.Clamp(Feats - amount, 0, MaxFeats));

    private void Mutate(Action mutation)
    {
        mutation();
        PlayerInfoChanged?.Invoke();
    }
}
