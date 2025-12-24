using Godot;
using SYNK33.autoloads.save_manager;
using SYNK33.core;

namespace SYNK33.scenes.menus.results;

public class GameplayResults {
    public required int PerfectCount { get; init; }
    public required int GreatCount { get; init; }
    public required int OkayCount { get; init; }
    public required int MissCount { get; init; }
    public required int TotalHits { get; init; }
    public required int TotalNotes { get; init; }
    public required int EarlyCount { get; init; }
    public required int LateCount { get; init; }
    public required int Score { get; init; }
    public required int MaxScore { get; init; }
    public required int MaxCombo { get; init; }
    public required double AccuracyRatio { get; init; }
    public required Rank Rank { get; init; }
    public required bool SongFinished { get; init; }
    public required long ChartHash { get; init; }

    public ChartPerformance ToChartPerformance() {
        return new ChartPerformance {
            HighScore = (uint)Mathf.Max(0, Score),
            PerfectHits = (uint)Mathf.Max(0, PerfectCount),
            TotalHits = (uint)Mathf.Max(0, TotalHits),
            GhostHits = 0,
            ChartTotalNotes = (uint)Mathf.Max(0, TotalNotes),
            ChartTotalGhostNotes = 0,
            MaxCombo = (uint)Mathf.Max(0, MaxCombo),
            MaxScore = (uint)Mathf.Max(0, MaxScore)
        };
    }
}
