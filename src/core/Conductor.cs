using Godot;
using Chart = SYNK33.chart.Chart;

namespace SYNK33.core;

[GlobalClass]
public partial class Conductor : Node {
    [Signal]
    public delegate void SongEndedEventHandler();

    public double SongPosition;
    public double StartingTimestamp;
    [Export] public required Chart Chart { get; set; }
    [Export] public required AudioStreamPlayer Player { get; set; }
    [Export] public float Bpm { get; set; } = 120f;
    [Export] public float OffsetSeconds { get; set; }
    [Export] public float InputOffsetMs { get; set; }

    public float SecondsPerBeat => 60f / Bpm;

    public override void _Ready() {
        //base._Ready();
        Player.Stop();
        Player.Stream = null;
        Player.Stream = Chart.Song.Audio;
        Player.SetPitchScale(Chart.TempoModifier);
        StartingTimestamp = Time.GetUnixTimeFromSystem();
        Player.Finished += OnAudioFinished;
        Player.Play();
        Bpm = Chart.Bpm;
    }

    public override void _ExitTree() {
        base._ExitTree();
        Player.Finished -= OnAudioFinished;
        Player.Stop();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        SongPosition = Player.GetPlaybackPosition() + AudioServer.GetTimeSinceLastMix();
        SongPosition -= AudioServer.GetOutputLatency();
    }

    private void OnAudioFinished() {
        EmitSignalSongEnded();
    }
}