using System;
using Godot;
using SYNK33.chart;
using SYNK33.ui;
using System.Collections.Generic;
using SYNK33.scenemanager;
using SYNK33.spawner;

namespace SYNK33.core;


public partial class GameManager : Node {
    [Export] public required Conductor Conductor;
    [Export] public required InputManager InputManager;
    [Export] public required JudgementManager JudgementManager;
    [Export] public required ScoreManager ScoreManager;
    [Export] public required Spawner3D Spawner;
    [Export] public required JudgementIndicatorManager JudgementIndicatorManager { get; set; }

    private const int MinComboForBreakEffect = 15;

    private AudioStreamPlayer? _hitNotePerfectSfx;
    private AudioStreamPlayer? _hitNoteGreatSfx;
    private AudioStreamPlayer? _hitNoteOkaySfx;
    private AudioStreamPlayer? _hitNoteSfx;
    private AudioStreamPlayer? _holdNoteSfx;
    private AudioStreamPlayer? _comboBreakSfx;
    private AudioStreamPlayer? _missSfx;
    private readonly Dictionary<(NoteType type, long bar, long beat, double sixteenth), AudioStreamPlayer> _holdPlayers = new();

    public override void _Ready() {
        base._Ready();
        _hitNotePerfectSfx = GetNode<AudioStreamPlayer>("../HitNotePerfectSfx");
        _hitNoteGreatSfx = GetNode<AudioStreamPlayer>("../HitNoteGreatSfx");
        _hitNoteOkaySfx = GetNode<AudioStreamPlayer>("../HitNoteOkaySfx");
        _hitNoteSfx = GetNode<AudioStreamPlayer>("../HitNoteSfx");
        _holdNoteSfx = GetNode<AudioStreamPlayer>("../HoldNoteSfx");
        _comboBreakSfx = GetNode<AudioStreamPlayer>("../ComboBreakSfx");
        _missSfx = GetNode<AudioStreamPlayer>("../MissSfx");

        InputManager.SongStartTime = Conductor.StartingTimestamp;
        InputManager.ButtonPressed += OnButtonPressed;

        ScoreManager.InitializeForChart(Conductor.Chart.Notes);
        ScoreManager.ComboBroken += OnComboBroken;

        JudgementManager.NoteHit += OnNoteHit;
        JudgementManager.NoteMissed += OnNoteMissed;
        JudgementManager.NoteHeld += OnNoteHeld;
        JudgementManager.NoteReleased += OnNoteReleased;
        JudgementManager.HoldJudged += OnHoldJudged;

        Conductor.SongEnded += OnSongEnded;
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("ui_cancel")) {
            ExitToResults();
            //GetViewport().SetInputAsHandled(); 
        }
    }

    private void OnNoteHit(NoteType type, long bar, long beat, double sixteenth, Judgement judgement, TimingOffset offset) {
        ScoreManager.RegisterHit(judgement, offset);
        var sfx = judgement switch {
            Judgement.Perfect => _hitNotePerfectSfx,
            Judgement.Great => _hitNoteGreatSfx,
            Judgement.Okay => _hitNoteOkaySfx,
            Judgement.Miss => _missSfx,
            _ => throw new ArgumentOutOfRangeException(nameof(judgement), judgement, null)
        };
        sfx?.Play();
        DisplayJudgement(judgement, type, new TimingWindow(judgement, offset));
    }

    private void OnNoteMissed(NoteType type, long bar, long beat, double sixteenth) {
        ScoreManager.RegisterMiss();
        _missSfx?.Play();
        DisplayJudgement(Judgement.Miss, type);
    }

    private void OnNoteHeld(NoteType type, long bar, long beat, double sixteenth) {
        _hitNoteSfx?.Play();
        var player = new AudioStreamPlayer {
            Stream = _holdNoteSfx!.Stream, 
            MaxPolyphony = 1,
            Bus = "Sfx"
        };
        AddChild(player);
        player.Play();
        _holdPlayers[(type, bar, beat, sixteenth)] = player;
    }

    private void OnHoldJudged(NoteType type, long bar, long beat, double sixteenth, Judgement judgement, TimingOffset offset) {
        ScoreManager.RegisterHit(judgement, offset);
        DisplayJudgement(judgement, type, new TimingWindow(judgement, offset));
    }

    private void OnNoteReleased(NoteType type, long bar, long beat, double sixteenth) {
        if (_holdPlayers.TryGetValue((type, bar, beat, sixteenth), out var player)) {
            player.Stop();
            player.QueueFree();
            _holdPlayers.Remove((type, bar, beat, sixteenth));
        }
    }

    private void OnComboBroken(int combo) {
        if (combo > MinComboForBreakEffect) {
            _comboBreakSfx?.Play();
        }
    }

    private void OnButtonPressed(NoteType type) {
        _hitNoteSfx?.Play();
    }

    private void OnSongEnded() {
        ExitToResults();
    }

    private void ExitToResults() {
        var sceneManager = GetNode<SceneManager>("/root/SceneManager");
        var chartHash = Conductor.Chart.GetSaveHash();
        var gameplayResults = ScoreManager.ToGameplayResults(chartHash);
        sceneManager.ChangeSceneToResults(gameplayResults);
    }

    private void DisplayJudgement(Judgement judgement, NoteType noteType, TimingWindow? timing = null) {
        JudgementIndicatorManager?.ShowJudgement(judgement, noteType, timing);
    }
}
