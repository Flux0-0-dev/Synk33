using Godot;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.ui;

public partial class JudgementIndicatorManager : Control {
    [Export] public PackedScene? IndicatorScene { get; set; }
    [Export] public Vector2 SpawnPosition { get; set; } = new(0, 0);
    [Export] public float LaneSpacing { get; set; } = 200.0f;
    [Export] public int MaxActiveIndicators { get; set; } = 10;
    
    private JudgementIndicator[] _indicators = [];
    private int _nextIndicatorIndex;
    
    public override void _Ready() {
        base._Ready();
        ValidateIndicatorScene();
        InitializeIndicators();
    }
    
    public void ShowJudgement(Judgement judgement, NoteType noteType, TimingWindow? timing = null) {
        var indicator = AcquireIndicator();
        PrepareIndicator(indicator);
        indicator.DisplayJudgement(judgement, noteType, LaneSpacing, timing);
    }
    
    private void ValidateIndicatorScene() {
        if (IndicatorScene == null) {
            GD.PushError("JudgementIndicatorManager: IndicatorScene is not assigned");
        }
    }
    
    private void InitializeIndicators() {
        _indicators = new JudgementIndicator[MaxActiveIndicators];
        for (var i = 0; i < MaxActiveIndicators; i++) {
            _indicators[i] = CreateIndicator();
            _indicators[i].Visible = false;
        }
    }
    
    private JudgementIndicator AcquireIndicator() {
        var indicator = _indicators[_nextIndicatorIndex];
        _nextIndicatorIndex = (_nextIndicatorIndex + 1) % MaxActiveIndicators;
        
        return indicator;
    }
    
    private JudgementIndicator CreateIndicator() {
        var indicator = IndicatorScene!.Instantiate<JudgementIndicator>();
        AddChild(indicator);
        return indicator;
    }
    
    private void PrepareIndicator(JudgementIndicator indicator) {
        indicator.SetBasePosition(SpawnPosition);
        indicator.Visible = true;
    }
}

