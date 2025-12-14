using System.Collections.Generic;
using Godot;
using SYNK33.chart;

namespace SYNK33.core;

public partial class InputManager : Node {
    [Signal]
    public delegate void ButtonPressedEventHandler(NoteType type);
    
    [Signal]
    public delegate void ButtonReleasedEventHandler(NoteType type);
    
    [Export] public double SongStartTime { get; set; }
    private readonly Queue<RhythmInput> _inputs = new();


    public override void _Input(InputEvent @event) {
        if (@event is not InputEventKey keyEvent) return;
        var noteType = keyEvent switch {
            _ when @event.IsActionPressed("left") => NoteType.Left,
            _ when @event.IsActionPressed("middle") => NoteType.Middle,
            _ when @event.IsActionPressed("right") => NoteType.Right,
            _ when @event.IsActionReleased("left") => NoteType.Left,
            _ when @event.IsActionReleased("middle") => NoteType.Middle,
            _ when @event.IsActionReleased("right") => NoteType.Right,
            _ => (NoteType?)null
        };

        if (noteType.HasValue) {
            AddInput(noteType.Value, @event.IsPressed(), keyEvent.PhysicalKeycode);
        }
    }

    public RhythmInput? PopInput() {
        return _inputs.Count > 0 ? _inputs.Dequeue() : null;
    }

    private void AddInput(NoteType type, bool pressed, Key physicalKey) {
        var timestamp = Time.GetUnixTimeFromSystem();
        var hit = new RhythmInput(type, pressed, timestamp - SongStartTime, physicalKey);
        _inputs.Enqueue(hit);

        EmitSignal(pressed ? SignalName.ButtonPressed : SignalName.ButtonReleased, (int)type);
    }
}

public record RhythmInput(
    NoteType NoteType,
    bool Pressed,
    double Timestamp,
    Key PhysicalKey
);