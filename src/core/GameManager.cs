using Godot;
using SYNK33.chart;
using System.Collections.Generic;
using System.Linq;

namespace SYNK33.core;

public partial class GameManager : Node {
    [Export] public required Conductor Conductor;
    [Export] public required InputManager InputManager;
    [Export] public required JudgementManager JudgementManager;
    [Export] public required ScoreManager ScoreManager;
    [Export] public required Node Spawner; // Changed from Spawner to Node to support both Spawner and Spawner3D

    private AudioStreamPlayer? _hitNoteSfx;
    private AudioStreamPlayer? _holdNoteSfx;
    private readonly Dictionary<(NoteType type, long bar, long beat, double sixteenth), AudioStreamPlayer> _holdPlayers = new();
    private Label? _holdPlayersLabel;

    public override void _Ready() {
        base._Ready();
        _hitNoteSfx = GetNode<AudioStreamPlayer>("../HitNoteSfx");
        _holdNoteSfx = GetNode<AudioStreamPlayer>("../HoldNoteSfx");
        _holdPlayersLabel = GetNode<Label>("../Debug/HoldPlayers");
        InputManager.SongStartTime = Conductor.StartingTimestamp;
        
        ScoreManager.SetTotalNotes(Conductor.Chart.Notes.Count);
        
        JudgementManager.NoteHit += OnNoteHit;
        JudgementManager.NoteMissed += OnNoteMissed;
        JudgementManager.NoteHeld += OnNoteHeld;
        JudgementManager.NoteReleased += OnNoteReleased;
        JudgementManager.HoldJudged += OnHoldJudged;
    }

    private void OnNoteHit(NoteType type, long bar, long beat, double sixteenth, Judgement judgement) {
        ScoreManager.RegisterHit(judgement);
        _hitNoteSfx?.Play();
    }

    private void OnNoteMissed(NoteType type, long bar, long beat, double sixteenth) {
        ScoreManager.RegisterMiss();
    }

    private void OnNoteHeld(NoteType type, long bar, long beat, double sixteenth) {
        _hitNoteSfx?.Play();
        var player = new AudioStreamPlayer { Stream = _holdNoteSfx!.Stream, MaxPolyphony = 1 };
        AddChild(player);
        player.Play();
        _holdPlayers[(type, bar, beat, sixteenth)] = player;
        UpdateHoldPlayersDebug();
    }

    private void OnHoldJudged(NoteType type, long bar, long beat, double sixteenth, Judgement judgement) {
        ScoreManager.RegisterHit(judgement);
    }

    private void OnNoteReleased(NoteType type, long bar, long beat, double sixteenth) {
        if (_holdPlayers.TryGetValue((type, bar, beat, sixteenth), out var player)) {
            player.Stop();
            player.QueueFree();
            _holdPlayers.Remove((type, bar, beat, sixteenth));
        }
        UpdateHoldPlayersDebug();
    }

    private void UpdateHoldPlayersDebug() {
        if (_holdPlayersLabel != null) {
            _holdPlayersLabel.Text = string.Join("\n", _holdPlayers.Keys.Select(key => $"{key.type} {key.bar}:{key.beat}:{key.sixteenth}"));
        }
    }
}