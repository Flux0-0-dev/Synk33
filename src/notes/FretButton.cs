using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.notes;

public partial class FretButton : Node3D {
    [Export] public NoteType ButtonType { get; set; }
    [Export] public required Material Material { get; set; }
    [Export] public required Material MaterialGlow { get; set; }
    
    private AnimationPlayer? _animationPlayer;
    private InputManager? _inputManager;
    private MeshInstance3D? _fretButton;
    private MeshInstance3D? _fretButtonTop;
    
    public override void _Ready() {
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _fretButton = GetNodeOrNull<MeshInstance3D>("%fret_button");
        _fretButtonTop = GetNodeOrNull<MeshInstance3D>("%fret_button_top");
        
        _fretButton?.SetSurfaceOverrideMaterial(0, Material);
        _fretButton?.SetSurfaceOverrideMaterial(1, MaterialGlow);
        _fretButtonTop?.SetSurfaceOverrideMaterial(0, Material);
        
        _inputManager = GetTree().Root.FindChild("InputManager", true, false) as InputManager;
        
        if (_inputManager != null) {
            _inputManager.ButtonPressed += OnButtonPressed;
            _inputManager.ButtonReleased += OnButtonReleased;
        } else {
            GD.PrintErr("FretButton: Could not find InputManager in scene tree");
        }
    }
    
    public override void _ExitTree() {
        if (_inputManager != null) {
            _inputManager.ButtonPressed -= OnButtonPressed;
            _inputManager.ButtonReleased -= OnButtonReleased;
        }
    }
    
    private void OnButtonPressed(NoteType type) {
        if (type != ButtonType || _animationPlayer == null) return;
        _animationPlayer.Play("press");
    }
    
    private void OnButtonReleased(NoteType type) {
        if (type != ButtonType || _animationPlayer == null) return;
        _animationPlayer.Play("release");
    }
}


