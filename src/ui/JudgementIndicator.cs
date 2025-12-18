using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.ui;

public partial class JudgementIndicator : Control {
    [Export] public Vector2 PositionOffset { get; set; } = Vector2.Zero;
    
    [ExportGroup("Judgement Colors")]
    [Export] public Color PerfectColor = new(0.5f, 0.3f, 1.0f);
    [Export] public Color GreatColor = new(0.0f, 0.8f, 1.0f);
    [Export] public Color OkayColor = new(0.0f, 1.0f, 0.5f);
    [Export] public Color MissColor = new(0.87f, 0.0f, 0.04f);
    
    [ExportGroup("Timing Colors")]
    [Export] public Color EarlyColor = new(0.3f, 0.6f, 1.0f);
    [Export] public Color LateColor = new(1.0f, 0.3f, 0.3f);
    
    private Label _judgementLabel = null!;
    private Container _timing = null!;
    private Label _timingLabel = null!;
    private Panel _background = null!;
    private AnimationPlayer _animationPlayer = null!;
    private Vector2 _basePosition;
    
    public override void _Ready() {
        base._Ready();
        CacheNodes();
    }
    
    public void DisplayJudgement(Judgement judgement, NoteType noteType, float laneOffset, TimingWindow? timing = null) {
        SetJudgementText(judgement);
        SetJudgementColor(judgement);
        SetTimingText(timing);
        PositionForLane(noteType, laneOffset);
        PlayAppearAnimation();
    }
    
    public void SetBasePosition(Vector2 position) {
        _basePosition = position;
        Position = _basePosition + PositionOffset;
    }
    
    private void CacheNodes() {
        _judgementLabel = GetNode<Label>("%Judgement");
        _timing = GetNode<Container>("%TimingContainer");
        _timingLabel = GetNode<Label>("%Timing");
        _background = GetNode<Panel>("%Background");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }
    
    private void PositionForLane(NoteType noteType, float laneOffset) {
        var horizontalOffset = CalculateLaneOffset(noteType, laneOffset);
        Position = _basePosition + new Vector2(horizontalOffset, 0) + PositionOffset;
    }
    
    private static float CalculateLaneOffset(NoteType noteType, float laneOffset) {
        return noteType switch {
            NoteType.Left => -laneOffset,
            NoteType.Middle => 0,
            NoteType.Right => laneOffset,
            _ => 0
        };
    }
    
    private void SetJudgementText(Judgement judgement) {
        _judgementLabel.Text = GetJudgementDisplayText(judgement);
    }
    
    private static string GetJudgementDisplayText(Judgement judgement) {
        return judgement switch {
            Judgement.Perfect => "Perfect",
            Judgement.Great => "Great",
            Judgement.Okay => "Okay",
            Judgement.Miss => "Miss",
            _ => "Unknown"
        };
    }
    
    private void SetJudgementColor(Judgement judgement) {
        var color = GetJudgementColor(judgement);
        ApplyColorToText(color);
    }
    
    private Color GetJudgementColor(Judgement judgement) {
        return judgement switch {
            Judgement.Perfect => PerfectColor,
            Judgement.Great => GreatColor,
            Judgement.Okay => OkayColor,
            _ => MissColor
        };
    }
    
    private void ApplyColorToBackground(Color color) {
        if (_background.Get("theme_override_styles/panel").AsGodotObject() is StyleBoxFlat styleBox) {
            styleBox.BgColor = color;
        }
    }
    
    private void ApplyColorToText(Color color) {
        _judgementLabel.Modulate = color;
    }
    
    private void SetTimingText(TimingWindow? timing) {
        if (timing == null || timing.Offset == TimingOffset.OnTime) {
            _timing.Visible = false;
            return;
        }
        
        _timing.Visible = true;
        _timingLabel.Text = GetTimingDisplayText(timing);
        ApplyColorToBackground(GetTimingColor(timing));
    }
    
    private static string GetTimingDisplayText(TimingWindow timing) {
        return timing.Offset switch {
            TimingOffset.Early => "Early",
            TimingOffset.Late => "Late",
            _ => ""
        };
    }
    
    private Color GetTimingColor(TimingWindow timing) {
        return timing.Offset switch {
            TimingOffset.Early => EarlyColor,
            TimingOffset.Late => LateColor,
            _ => Colors.White
        };
    }
    
    private void PlayAppearAnimation() {
        _animationPlayer.Play("appear");
        _animationPlayer.Queue("show");
        _animationPlayer.Queue("disappear");
    }
}
