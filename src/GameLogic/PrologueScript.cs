using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Chickensoft.AutoInject;
using Chickensoft.Introspection;
using Godot;
using VikingJamGame.Models;
using VikingJamGame.TemplateUtils;

namespace VikingJamGame.GameLogic;

[GlobalClass]
[Meta(typeof(IAutoNode))]
public partial class PrologueScript : Node2D
{
    private const string DEFAULT_EDITOR_PROLOGUE_RESOURCE_FILE = "res://src/definitions/prologue/prologue.txt";
    private const string FALLBACK_EDITOR_PROLOGUE_RESOURCE_FILE = "res://definitions/prologue/prologue.txt";
    private const string DEFAULT_BUILD_PROLOGUE_RELATIVE_FILE = "definitions/prologue/prologue.txt";

    private static readonly Regex NarrationLineRegex = MyRegex();

    [Dependency] private PlayerInfo PlayerInfo => this.DependOn<PlayerInfo>();
    [Dependency] private GameResources GameResources => this.DependOn<GameResources>();
    [Dependency] private GameStateMachine Machine => this.DependOn<GameStateMachine>();

    [Export] private Label NarrationLabel { get; set; } = null!;
    [Export] private Label WaitingIndicator { get; set; } = null!;

    [Export] private AnimationPlayer NarrationPlayer { get; set; } = null!;
    [Export] private AnimationPlayer WaitingPlayer { get; set; } = null!;

    [Export] private HBoxContainer GenderChoiceContainer { get; set; } = null!;
    [Export] private HBoxContainer NameContainer { get; set; } = null!;
    [Export] private HBoxContainer StartContainer { get; set; } = null!;

    [Export] private Button BoyButton { get; set; } = null!;
    [Export] private Button GirlButton { get; set; } = null!;
    [Export] private Button NonBinaryButton { get; set; } = null!;
    [Export] private LineEdit NameInput { get; set; } = null!;
    [Export] private Button NameContinue { get; set; } = null!;
    [Export] private Button StartButton { get; set; } = null!;

    [Export]
    public string EditorPrologueResourceFile { get; set; } =
        DEFAULT_EDITOR_PROLOGUE_RESOURCE_FILE;

    [Export]
    public string BuildPrologueRelativeFile { get; set; } =
        DEFAULT_BUILD_PROLOGUE_RELATIVE_FILE;

    private string[] _narration = [];
    private int _currentLine;
    private int _maxLines;
    private bool _playingTextAnimation;
    private BirthChoice _birthChoice = BirthChoice.ChildOfOmen;
    private string _name = "{Name}";
    private string _title = "{Title}";
    private GameDataWrapper _initialData = null!;

    public void OnResolved()
    {
        ReadNarrationFiles();
        TriggerNarration();
        NarrationPlayer.AnimationFinished += BlinkWaiting;
        WaitingPlayer.GetAnimation("text_waiting").LoopMode = Animation.LoopModeEnum.Pingpong;
        BoyButton.Pressed += OnBoyChosen;
        GirlButton.Pressed += OnGirlChosen;
        NonBinaryButton.Pressed += OnNonBinaryChosen;
        NameContinue.Pressed += OnNameChosen;
        StartButton.Pressed += OnStartClicked;
    }

    private void OnBoyChosen()
    {
        _birthChoice = BirthChoice.Boy;
        ForcedAdvanceNarration();
    }

    private void OnGirlChosen()
    {
        _birthChoice = BirthChoice.Girl;
        ForcedAdvanceNarration();
    }

    private void OnNonBinaryChosen()
    {
        _birthChoice = BirthChoice.ChildOfOmen;
        ForcedAdvanceNarration();
    }

    private void OnNameChosen()
    {
        SetCharacterName(NameInput.Text);
        _initialData = InitialResourcesFactory.FromPrologueData(_birthChoice, _name);
        _name = _initialData.PlayerInfo.Name;
        _title = _initialData.PlayerInfo.Title;
        ForcedAdvanceNarration();
    }

    private void OnStartClicked()
    {
        PlayerInfo.SetInitialInfo(
            _initialData.PlayerInfo.Name,
            _initialData.PlayerInfo.BirthChoice,
            _initialData.PlayerInfo.Title,
            _initialData.PlayerInfo.Strength, _initialData.PlayerInfo.MaxStrength,
            _initialData.PlayerInfo.Honor, _initialData.PlayerInfo.MaxHonor,
            _initialData.PlayerInfo.Feats, _initialData.PlayerInfo.MaxFeats
        );
        
        GameResources.SetInitialResources(
            _initialData.GameResources.Population,
            _initialData.GameResources.Food,
            _initialData.GameResources.Gold
        );

        Machine.Input(new GameStateMachine.Input.FinishPrologue());
    }

    public override void _ExitTree()
    {
        NarrationPlayer.AnimationFinished -=  BlinkWaiting;
        BoyButton.Pressed -= OnBoyChosen;
        GirlButton.Pressed -= OnGirlChosen;
        NonBinaryButton.Pressed -= OnNonBinaryChosen;
        NameContinue.Pressed -= OnNameChosen;
        StartButton.Pressed -= OnStartClicked;
    }

    private void TriggerNarration()
    {
        GenderChoiceContainer.Visible = false;
        NameContainer.Visible = false;
        WaitingPlayer.Stop();
        NarrationLabel.VisibleRatio = 0;
        NarrationLabel.Text = Template.Render(
            _narration[_currentLine],
            _birthChoice,
            _name,
            _title);
        WaitingIndicator.VisibleCharacters = 0;
        NarrationPlayer.Play("show_text");
        _playingTextAnimation = true;
    }

    public void SetCharacterName(string name) =>
        _name = string.IsNullOrWhiteSpace(name) ? "{Name}" : name.Trim();

    private bool CanAdvance()
    {
        if (_currentLine is 3 or 4 or 5) return false;
        return !_playingTextAnimation;
    }

    public void AdvanceNarration()
    {
        if (_currentLine >= _narration.Length - 1) return;
        if (!CanAdvance()) return;
        
        _currentLine++;
        TriggerNarration();
    }

    private void ForcedAdvanceNarration()
    {
        if (_currentLine >= _narration.Length - 1) return;
        _currentLine++;
        TriggerNarration();
    }

    private void StopBlinking(StringName _)
    {
        WaitingIndicator.VisibleCharacters = 0;
        WaitingPlayer.Stop();
    }

    private void BlinkWaiting(StringName _)
    {
        _playingTextAnimation = false;
        switch (_currentLine)
        {
            case 3:
                GenderChoiceContainer.Visible = true;
                break;
            case 4:
                NameContainer.Visible = true;
                break;
            case 5:
                StartContainer.Visible = true;
                break;
        }

        if (!CanAdvance()) return;
        WaitingPlayer.Play("text_waiting");
    }

    public override void _Notification(int what) => this.Notify(what);

    private void ReadNarrationFiles()
    {
        string prologueFilePath = ResolvePrologueFilePath(
            EditorPrologueResourceFile,
            BuildPrologueRelativeFile);

        string[] rawLines = File.ReadAllLines(prologueFilePath);
        var entries = new List<(int Index, string Text)>(rawLines.Length);

        for (var i = 0; i < rawLines.Length; i++)
        {
            string line = rawLines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            Match match = NarrationLineRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            int index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            string narrationText = match.Groups[2].Value.Trim();
            entries.Add((index, narrationText));
        }

        _narration = entries
            .OrderBy(entry => entry.Index)
            .Select(entry => entry.Text)
            .ToArray();

        _currentLine = 0;
        _maxLines = _narration.Length;
    }

    private static string ResolvePrologueFilePath(
        string editorPrologueResourceFile,
        string buildPrologueRelativeFile)
    {
        if (OS.HasFeature("editor"))
        {
            string primaryPath = ProjectSettings.GlobalizePath(editorPrologueResourceFile);
            if (File.Exists(primaryPath))
            {
                return Path.GetFullPath(primaryPath);
            }

            string fallbackPath = ProjectSettings.GlobalizePath(FALLBACK_EDITOR_PROLOGUE_RESOURCE_FILE);
            return Path.GetFullPath(fallbackPath);
        }

        string executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath())!;
        return Path.GetFullPath(Path.Combine(executableDirectory, buildPrologueRelativeFile));
    }

    [GeneratedRegex(@"^\s*\[(\d+)\]\s*(.+?)\s*$", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
