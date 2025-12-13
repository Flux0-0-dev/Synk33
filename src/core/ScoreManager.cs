using Godot;
using System.Collections.Generic;
using SYNK33.Saving;
using static SYNK33.core.Judgement;

namespace SYNK33.core;

public partial class ScoreManager : Node {
    [Signal]
    public delegate void ComboChangedEventHandler(int combo);

    [Signal]
    public delegate void ScoreChangedEventHandler(int score);

    private readonly Dictionary<Judgement, int> _judgementCounts = new() {
        { Perfect, 0 },
        { Great, 0 },
        { Okay, 0 },
        { Miss, 0 }
    };

    public int CurrentCombo { get; private set; }
    public int BestCombo { get; private set; }
    public int TotalNotes { get; private set; }
    public int MissCount => _judgementCounts[Miss];
    public int Score { get; private set; }
    public int MaxScore => TotalNotes * CalculateScoreForJudgement(Perfect);
    public bool IsAllPerfect => MissCount == 0 && _judgementCounts[Perfect] == TotalNotes;

    public bool IsFullCombo => MissCount == 0 && TotalHits == TotalNotes;

    public int TotalHits => _judgementCounts[Perfect]
                            + _judgementCounts[Great]
                            + _judgementCounts[Okay];

    public int Multiplier => CalculateMultiplier();

    public int GetJudgementCount(Judgement judgement) => _judgementCounts[judgement];

    public void SetTotalNotes(int count) {
        TotalNotes = count;
    }

    public ChartPerformance ToChartPerformance() {
        return new ChartPerformance {
            Highscore = (uint)Mathf.Max(0, Score),
            PerfectHits = (uint)Mathf.Max(0, _judgementCounts[Perfect]),
            TotalHits = (uint)Mathf.Max(0, TotalHits),
            GhostHits = 0, // TODO: Update when ghost notes are added
            ChartTotalNotes = (uint)Mathf.Max(0, TotalNotes),
            ChartTotalGhostNotes = 0, // TODO: Update when ghost notes are added
            MaxCombo = (uint)Mathf.Max(0, BestCombo)
        };
    }

    public void RegisterHit(Judgement judgement) {
        _judgementCounts[judgement]++;
        if (judgement == Miss) {
            ResetCombo();
        } else {
            IncrementCombo();
            AddScore(CalculateScoreForJudgement(judgement) * Multiplier);
        }
    }


    public void RegisterMiss() {
        _judgementCounts[Miss]++;
        ResetCombo();
    }

    private int CalculateMultiplier() {
        var combo = CurrentCombo;
        return combo switch {
            < 10 => 1,
            < 25 => 2,
            < 50 => 3,
            < 100 => 4,
            _ => 5
        };
    }

    public void Reset() {
        foreach (var key in _judgementCounts.Keys) {
            _judgementCounts[key] = 0;
        }

        CurrentCombo = 0;
        BestCombo = 0;
        Score = 0;
        EmitSignalComboChanged(0);
        EmitSignalScoreChanged(0);
    }

    private void IncrementCombo() {
        CurrentCombo++;
        if (CurrentCombo > BestCombo) {
            BestCombo = CurrentCombo;
        }

        EmitSignalComboChanged(CurrentCombo);
    }

    private void ResetCombo() {
        CurrentCombo = 0;
        EmitSignalComboChanged(CurrentCombo);
    }

    private void AddScore(int points) {
        Score += points;
        EmitSignalScoreChanged(Score);
    }

    private static int CalculateScoreForJudgement(Judgement judgement) {
        return judgement switch {
            Perfect => 33,
            Great => 22,
            Okay => 11,
            _ => 0
        };
    }
}
