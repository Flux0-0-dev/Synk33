using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SYNK33.chart;

namespace SYNK33.core;

public partial class JudgementManager : Node {
    [Signal]
    public delegate void NoteMissedEventHandler(NoteType type, long bar, long beat, double sixteenth);

    [Signal]
    public delegate void NoteHitEventHandler(NoteType type, long bar, long beat, double sixteenth, Judgement judgement);

    [Signal]
    public delegate void NoteHeldEventHandler(NoteType type, long bar, long beat, double sixteenth);

    [Signal]
    public delegate void NoteReleasedEventHandler(NoteType type, long bar, long beat, double sixteenth);

    [Signal]
    public delegate void HoldJudgedEventHandler(NoteType type, long bar, long beat, double sixteenth, Judgement judgement);

    [Export] public required Conductor Conductor;
    [Export] public required InputManager InputManager;

    private readonly List<Note> _notes = [];
    private readonly List<Note.Hold> _heldNotes = [];
    private readonly Dictionary<Note.Hold, HoldNoteState> _activeHolds = new();

    public override void _Ready() {
        base._Ready();
        CopyNotes();
    }

    public override void _Process(double delta) {
        base._Process(delta);
        Judge(InputManager.PopInput());
        CheckMissedNotes();
        ReleaseHeldNotesAfterEnd();
    }

    private void CopyNotes() {
        foreach (var note in Conductor.Chart.Notes) {
            _notes.Add(note.ToNote());
        }
    }

    private void Judge(RhythmInput? input) {
        if (input == null) return;
        var note = !input.Pressed
            ? _heldNotes.Find(it => it.Type == input.NoteType)
            : _notes.Find(it => it.Type == input.NoteType);
        switch (note) {
            case null:
                return;
            case Note.Hold holdNote:
                HandleHoldNote(input, holdNote);
                break;
            case Note.Tap tapNote:
                HandleTapNote(input, tapNote);
                break;
        }
    }

    private void HandleTapNote(RhythmInput input, Note.Tap note) {
        if (JudgeTiming(input, note) is not { } judgement) return;
        _notes.Remove(note);
        EmitSignalNoteHit(note.Type, note.Bar, note.Beat, note.Sixteenth, judgement);
    }

    private void HandleHoldNote(RhythmInput input, Note.Hold note) {
        switch (input.Pressed) {
            case true:
                HandleHoldNotePress(input, note);
                break;
            case false:
                HandleHoldNoteRelease(input, note);
                break;
        }
    }

    private void HandleHoldNotePress(RhythmInput input, Note.Hold note) {
        if (JudgeTiming(input, note) is not { } initialJudgement) return;

        _heldNotes.Add(note);
        _activeHolds[note] = new HoldNoteState(input.PhysicalKey, Conductor.SongPosition);
        _notes.Remove(note);

        EmitSignalNoteHit(note.Type, note.Bar, note.Beat, note.Sixteenth, initialJudgement);
        EmitSignalNoteHeld(note.Type, note.Bar, note.Beat, note.Sixteenth);
    }

    private void HandleHoldNoteRelease(RhythmInput input, Note.Hold note) {
        if (!_activeHolds.TryGetValue(note, out var state)) return;
        if (state.PressedKey != input.PhysicalKey) return;

        JudgeHoldRelease(note, state.StartTime);
        _heldNotes.Remove(note);
        _activeHolds.Remove(note);
        EmitSignalNoteReleased(note.Type, note.Bar, note.Beat, note.Sixteenth);
    }

    private Judgement? JudgeTiming(RhythmInput input, Note note) {
        var time = note.Time(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure) - input.Timestamp;
        var absoluteTime = Math.Abs(time);
        return absoluteTime switch {
            <= JudgementTiming.Perfect => Judgement.Perfect,
            <= JudgementTiming.Great => Judgement.Great,
            <= JudgementTiming.Okay => Judgement.Okay,
            _ => null
        };
    }

    private void CheckMissedNotes() {
        foreach (var note in _notes.ToList()) {
            var time = Conductor.SongPosition - note.Time(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure);
            if (!(time > JudgementTiming.Miss)) continue;
            _notes.Remove(note);
            EmitSignalNoteMissed(note.Type, note.StartTime.Bar, note.StartTime.Beat, note.Sixteenth);
        }
    }

    private void ReleaseHeldNotesAfterEnd() {
        foreach (Note.Hold note in _heldNotes.ToList()) {
            var time = Conductor.SongPosition - note.EndTime(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure);
            if (time > 0) {
                JudgeHoldComplete(note);
                _heldNotes.Remove(note);
                _activeHolds.Remove(note);
                EmitSignalNoteReleased(note.Type, note.Bar, note.Beat, note.Sixteenth);
            }
        }
    }

    private void JudgeHoldRelease(Note.Hold note, double startTime) {
        var percentage = CalculateHoldPercentage(note, startTime);
        var judgement = DetermineHoldJudgement(percentage);

        EmitSignalHoldJudged(note.Type, note.Bar, note.Beat, note.Sixteenth, judgement);
    }

    private double CalculateHoldPercentage(Note.Hold note, double startTime) {
        var currentTime = Conductor.SongPosition;
        var noteStartTime = note.Time(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure);
        var noteEndTime = note.EndTime(Conductor.SecondsPerBeat, Conductor.Chart.BeatsPerMeasure);
        var noteDuration = noteEndTime - noteStartTime;
        var heldDuration = currentTime - startTime;

        return heldDuration / noteDuration;
    }

    private static Judgement DetermineHoldJudgement(double percentage) {
        return percentage switch {
            >= HoldJudgementTiming.Perfect => Judgement.Perfect,
            >= HoldJudgementTiming.Great => Judgement.Great,
            >= HoldJudgementTiming.Okay => Judgement.Okay,
            _ => Judgement.Miss
        };
    }

    private void JudgeHoldComplete(Note.Hold note) {
        EmitSignalHoldJudged(note.Type, note.Bar, note.Beat, note.Sixteenth, Judgement.Perfect);
    }

    private sealed record HoldNoteState(Key PressedKey, double StartTime);
}

public static class HoldJudgementTiming {
    public const double Perfect = 0.95;
    public const double Great = 0.85;
    public const double Okay = 0.70;
    // Below 70% = Miss
}

public static class Extensions {
    public static double Time(this Note note, float secondsPerBeat, long notesBerBar) {
        return (note.Bar * notesBerBar + note.Beat + note.Sixteenth / 4.0) * secondsPerBeat;
    }

    public static double EndTime(this Note.Hold note, float secondsPerBeat, long notesBerBar) {
        return (note.EndNote.Bar * notesBerBar + note.EndNote.Beat + note.EndNote.Sixteenth / 4.0) * secondsPerBeat;
    }
}

public static class JudgementTiming {
    // public const double Perfect = 0.0165;
    // public const double Great = 0.033;
    // public const double Okay = 0.066;
    // public const double Miss = 0.099;
    public const double Perfect = 0.05;
    public const double Great = 0.08;
    public const double Okay = 0.11;
    public const double Miss = 0.130;
}

public enum Judgement {
    Perfect,
    Great,
    Okay,
    Miss,
}