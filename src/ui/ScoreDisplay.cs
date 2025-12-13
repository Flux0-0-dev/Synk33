using Godot;
using SYNK33.core;

namespace SYNK33.ui;

public partial class ScoreDisplay : Node3D {
    [Export] public required ScoreManager ScoreManager;

    private Label3D? _comboNumber;
    private Label3D? _multiNumber;
    private Label3D? _xpNumber;

    public override void _Ready() {
        _comboNumber = GetNode<Label3D>("top_cont/combo_number");
        _multiNumber = GetNode<Label3D>("bottom_cont/multi_number");
        _xpNumber = GetNode<Label3D>("bottom_cont2/xp_number");
        
        ScoreManager.ComboChanged += OnComboChanged;
        ScoreManager.ScoreChanged += OnScoreChanged;
        
        UpdateDisplay();
    }

    private void OnComboChanged(int combo) {
        UpdateComboDisplay(combo);
        UpdateMultiplierDisplay();
    }

    private void OnScoreChanged(int score) {
        UpdateScoreDisplay(score);
        UpdateMultiplierDisplay();
    }

    private void UpdateDisplay() {
        UpdateComboDisplay(ScoreManager.CurrentCombo);
        UpdateScoreDisplay(ScoreManager.Score);
    }

    private void UpdateComboDisplay(int combo) {
        if (_comboNumber != null) {
            _comboNumber.Text = combo.ToString();
        }
    }

    private void UpdateScoreDisplay(int score) {
        if (_xpNumber != null) {
            _xpNumber.Text = score.ToString();
        }
    }
    
    private void UpdateMultiplierDisplay() {
        if (_multiNumber != null) {
            _multiNumber.Text = ScoreManager.Multiplier.ToString();
        }
    }
}

