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

    protected MeshInstance3D? NoteMesh;

    private double _lastSongPosition = -1;

    public override void _Ready() {
        NoteMesh = GetNodeOrNull<MeshInstance3D>("NoteMesh");
        AssignMaterials();
    }

    protected virtual void AssignMaterials() {
        NoteMesh?.SetSurfaceOverrideMaterial(0, Material);
        NoteMesh?.SetSurfaceOverrideMaterial(1, MaterialGlow);
    }

    public override void _Process(double delta) {
        base._Process(delta);
        var effectiveDelta = CalculateEffectiveDelta(delta);
        Position = Position with { Z = (float)(Position.Z + Speed * effectiveDelta) };
        if (Conductor != null) _lastSongPosition = Conductor.SongPosition;
    }

    public virtual void SetMissed(NoteType type, long bar, long beat, double sixteenth) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
    }

    public void SetHit(NoteType type, long bar, long beat, double sixteenth, Judgement judgement, TimingOffset offset) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        if (NoteMesh != null) NoteMesh.Visible = false;
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
