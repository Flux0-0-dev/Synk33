using System.Linq;
using Godot;
using Godot.Collections;
using SYNK33.chart;
using SYNK33.scenes.menus.results;
using static SYNK33.core.Judgement;

namespace SYNK33.core;

public partial class ScoreManager : Node {
    [Signal]
    public delegate void ComboChangedEventHandler(int combo);

    [Signal]
    public delegate void ScoreChangedEventHandler(int score);

    [Signal]
    public delegate void ComboBrokenEventHandler(int combo);

    private readonly System.Collections.Generic.Dictionary<Judgement, int> _judgementCounts = new() {
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
    public int MaxScore => CalculateMaxPossibleScore();
    public bool IsAllPerfect => MissCount == 0 && _judgementCounts[Perfect] == TotalNotes;

    public bool IsFullCombo => MissCount == 0 && TotalHits == TotalNotes;

    public int TotalHits => _judgementCounts[Perfect]
                            + _judgementCounts[Great]
                            + _judgementCounts[Okay];

    public int Multiplier => CalculateMultiplier(CurrentCombo);

    public int EarlyCount { get; private set; }
    public int LateCount { get; private set; }

    public double AccuracyRatio => MaxScore > 0 ? Score / (double)MaxScore : 0.0;

    public Rank CurrentRank => CalculateRank(AccuracyRatio);

    public int GetJudgementCount(Judgement judgement) => _judgementCounts[judgement];

    public void InitializeForChart(Array<GodotNote> notes) {
        // Until we add better HoldNote handling we have to count beginning and end separately
        TotalNotes = notes.Sum(note => note.IsHoldNote() ? 2 : 1);
    }

    public GameplayResults ToGameplayResults(long chartHash) {
        return new GameplayResults {
            PerfectCount = _judgementCounts[Perfect],
            GreatCount = _judgementCounts[Great],
            OkayCount = _judgementCounts[Okay],
            MissCount = _judgementCounts[Miss],
            TotalHits = TotalHits,
            TotalNotes = TotalNotes,
            EarlyCount = EarlyCount,
            LateCount = LateCount,
            Score = Score,
            MaxScore = MaxScore,
            MaxCombo = BestCombo,
            AccuracyRatio = AccuracyRatio,
            Rank = CurrentRank,
            SongFinished = (TotalHits + MissCount) == TotalNotes,
            ChartHash = chartHash
        };
    }

    public void RegisterHit(Judgement judgement, TimingOffset offset) {
        _judgementCounts[judgement]++;

        if (offset == TimingOffset.Early) {
            EarlyCount++;
        } else if (offset == TimingOffset.Late) {
            LateCount++;
        }

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

    private static int CalculateMultiplier(int combo) {
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
        EarlyCount = 0;
        LateCount = 0;
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
        EmitSignalComboBroken(CurrentCombo);
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

    private int CalculateMaxPossibleScore() {
        if (TotalNotes == 0) return 0;

        var baseScore = CalculateScoreForJudgement(Perfect);
        var maxScore = 0;

        for (var i = 1; i <= TotalNotes; i++) {
            maxScore += baseScore * CalculateMultiplier(i);
        }

        return maxScore;
    }

    private static Rank CalculateRank(double accuracyRatio) {
        return accuracyRatio switch {
            >= 0.99 => Rank.SPlus,
            >= 0.95 => Rank.S,
            >= 0.85 => Rank.A,
            >= 0.75 => Rank.B,
            >= 0.60 => Rank.C,
            _ => Rank.D
        };
    }
}

public enum Rank {
    D = 0,
    C = 1,
    B = 2,
    A = 3,
    S = 4,
    SPlus = 5
}
