using Godot;
using SYNK33.core;

namespace SYNK33.chart;

[GlobalClass]
public partial class NoteObject3D : Node3D {
    [Export] public NoteType Type;
    [Export] public required Material Material { get; set; }
    [Export] public required Material MaterialGlow { get; set; }
    
    public float Speed = 1;
    public required NoteTime StartTime;
    public Conductor? Conductor;
    
    private double _lastSongPosition = -1;
    private MeshInstance3D? _noteMesh;

    public override void _Ready() {
        _noteMesh = GetNodeOrNull<MeshInstance3D>("note");
        AssignMaterials();
    }

    protected virtual void AssignMaterials() {
        _noteMesh?.SetSurfaceOverrideMaterial(0, Material);
        _noteMesh?.SetSurfaceOverrideMaterial(1, MaterialGlow);
    }

    public override void _Process(double delta) {
        base._Process(delta);
        var effectiveDelta = CalculateEffectiveDelta(delta);
        Position = Position with { Z = (float)(Position.Z + Speed * effectiveDelta) };
        if (Conductor != null) _lastSongPosition = Conductor.SongPosition;
    }

    public virtual void SetMissed(NoteType type, long bar, long beat, double sixteenth) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        _noteMesh?.SetSurfaceOverrideMaterial(0, null);
        _noteMesh?.SetSurfaceOverrideMaterial(1, null);
    }

    public void SetHit(NoteType type, long bar, long beat, double sixteenth, Judgement judgement) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        var label = GetNode<Label3D>("Score");
        label.Text = judgement.ToString();
        if (_noteMesh != null) _noteMesh.Visible = false;
        // TODO: Add hit effects
    }

    private double CalculateEffectiveDelta(double delta) {
        if (Conductor == null) return delta;
        if (_lastSongPosition < 0) return 0;
        return Conductor.SongPosition - _lastSongPosition;
    }

    protected bool IsEventMatching(NoteType type, long bar, long beat, double sixteenth) {
        return IsEventMatching(type, new NoteTime(bar, beat, sixteenth));
    }

    private bool IsEventMatching(NoteType type, NoteTime time) {
        return Type == type && StartTime == time;
    }
}