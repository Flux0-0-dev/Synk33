using System;
using Godot;
using SYNK33.core;

namespace SYNK33.chart;

[GlobalClass]
public partial class HoldNoteObject3D : NoteObject3D {
    public required NoteTime EndTime;
    public long BeatsPerMeasure;
    public float UnitsPerBeat = 30f;

    private MeshInstance3D? _trailNode;
    private StandardMaterial3D? _trailMaterial;
    private bool _held;
    private float _holdLength;
    private float _trailScaleAtHoldLength = 1f;
    private float _currentEmission = 12f;
    private float _targetEmission = 12f;
    private float _lerpSpeed = 25f;

    public override void _Ready() {
        base._Ready();
        _trailNode = GetNodeOrNull<MeshInstance3D>("%Trail");

        var beats = EndTime - StartTime;
        var beatCount = (beats.Bar * BeatsPerMeasure + beats.Beat + beats.Sixteenth / 4.0f);

        var worldLength = (float)(beatCount * UnitsPerBeat);
        _holdLength = worldLength;

        if (_trailNode != null) {
            var originalScale = _trailNode.Scale;
            var modelScaleX = Mathf.Max(0.0001f, originalScale.X);
            var fullScaleX = worldLength / modelScaleX;
            _trailNode.Scale = new Vector3(fullScaleX, originalScale.Y, originalScale.Z);
            _trailScaleAtHoldLength = fullScaleX;
            AssignMaterials();
        }
    }

    public override void _Process(double delta) {
        base._Process(delta);
        if (_held) {
            if (_trailNode != null) _trailNode.GlobalPosition = _trailNode.GlobalPosition with { Z = 0 };
            UpdateTrailScale();
            PulseTrailMaterial();
        } else {
            LerpEmission(delta);
        }
    }

    private void LerpEmission(double delta) {
        _currentEmission = Mathf.Lerp(_currentEmission, _targetEmission, (float)delta * _lerpSpeed);
        if (_trailMaterial != null) _trailMaterial.EmissionEnergyMultiplier = _currentEmission;
    }

    private void PulseTrailMaterial() {
        if (_trailMaterial == null || Conductor == null) return;
        var time = (float)Conductor.SongPosition;
        var freq = 2.0f * Mathf.Pi / Conductor.SecondsPerBeat;
        var strength = Type switch {
            NoteType.Left => 0.2f,
            NoteType.Middle => 0.6f,
            NoteType.Right => 0.2f,
            _ => throw new ArgumentOutOfRangeException()
        };
        var pulse = Mathf.Sin(time * freq) * strength + 1.0f;
        _trailMaterial.EmissionEnergyMultiplier = 16 * pulse;
    }

    protected override void AssignMaterials() {
        base.AssignMaterials();
        _trailMaterial = MaterialGlow.Duplicate() as StandardMaterial3D;
        _trailNode?.SetSurfaceOverrideMaterial(0, _trailMaterial);
        _currentEmission = 12f;
        _targetEmission = 12f;
        if (_trailMaterial != null) _trailMaterial.EmissionEnergyMultiplier = _currentEmission;
    }

    public override void SetMissed(NoteType type, long bar, long beat, double sixteenth) {
        if (!IsEventMatching(type, bar, beat, sixteenth)) return;
        base.SetMissed(type, bar, beat, sixteenth);
        _targetEmission = 2f;
    }

    private void UpdateTrailScale() {
        if (_trailNode == null) return;
        if (_holdLength <= 0f) return;
        var remainingDistance = Mathf.Max(0f, _holdLength - Position.Z);
        var newScale = _trailScaleAtHoldLength * (remainingDistance / _holdLength);
        _trailNode.Scale = _trailNode.Scale with { X = Mathf.Max(0f, newScale) };
    }

    public void StartHold(NoteType type, long bar, long beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = true;
            if (NoteMesh != null) NoteMesh.Visible = false;
            _currentEmission = 16f;
            _targetEmission = 16f;
        }
    }

    public void EndHold(NoteType type, long bar, long beat, double sixteenth) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            _held = false;
            _targetEmission = 2f;
        }
    }

    public void NoteJudged(NoteType type, long bar, long beat, double sixteenth, Judgement judgement, TimingOffset offset) {
        if (IsEventMatching(type, bar, beat, sixteenth)) {
            // TODO: Add effect
        }
    }
}
