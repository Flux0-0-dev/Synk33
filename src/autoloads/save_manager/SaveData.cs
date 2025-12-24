using System.Collections.Generic;
using Godot;
using SYNK33.core;

namespace SYNK33.autoloads.save_manager;

public partial class ChartPerformance : Resource {
    public uint HighScore;
    public uint PerfectHits;
    public uint TotalHits;
    public uint GhostHits;
    public uint ChartTotalNotes;
    public uint ChartTotalGhostNotes;
    public uint MaxCombo;
    public uint MaxScore;

    public double GetGradeRatio() {
        return MaxScore > 0 ? HighScore / (double)MaxScore : 0.0;
    }

    public Rank GetRank() {
        var ratio = GetGradeRatio();
        return ratio switch {
            >= 0.99 => Rank.SPlus,
            >= 0.95 => Rank.S,
            >= 0.85 => Rank.A,
            >= 0.75 => Rank.B,
            >= 0.60 => Rank.C,
            _ => Rank.D
        };
    }

    public uint GetMisses() {
        return ChartTotalNotes - TotalHits;
    }
}

interface ISaveInfo {
    public bool HasChartPerformance(long chartHash);
    public ChartPerformance? GetChartPerformance(long chartHash);
    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance);
}

public partial class SaveData : Resource, ISaveInfo {
    [ExportGroup("StoryFlags")]
    /// <summary>
    /// Whether the player has completed the tutorial.
    /// </summary>
    [Export] public bool TutorialCompleted = false;
    /// <summary>
    /// Points performance of charts by their resource UID.
    /// </summary>
    private readonly Dictionary<long, ChartPerformance> _chartMap = [];

    public bool HasChartPerformance(long chartHash) {
        return _chartMap.ContainsKey(chartHash);
    }
    public ChartPerformance? GetChartPerformance(long chartHash) {
        return _chartMap.GetValueOrDefault(chartHash);
    }

    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance) {
        _chartMap[chartHash] = chartPerformance;
    }

    public void Save(string path) {
        ConfigFile config = new ConfigFile();
        config.SetValue("story_flags", "tutorial_completed", TutorialCompleted);
        config.Save(path);
    }

    public void Load(string path) {
        ConfigFile config = new ConfigFile();
        config.Load(path);
        TutorialCompleted = (bool)config.GetValue("story_flags", "tutorial_completed", false);
    }

    public void SerializeChartMap(FileAccess file) {
        file.Store32((uint)_chartMap.Count);
        foreach (var (key, chartPerformance) in _chartMap) {
            file.Store64((ulong)key);
            file.Store32(chartPerformance.HighScore);
            file.Store32(chartPerformance.PerfectHits);
            file.Store32(chartPerformance.TotalHits);
            file.Store32(chartPerformance.GhostHits);
            file.Store32(chartPerformance.ChartTotalNotes);
            file.Store32(chartPerformance.ChartTotalGhostNotes);
            file.Store32(chartPerformance.MaxCombo);
            file.Store32(chartPerformance.MaxScore);
        }
    }

    public void DeserializeChartMap(FileAccess file) {
        uint count = file.Get32();
        for (uint i = 0; i < count; i++) {
            long chartHash = (long)file.Get64();
            ChartPerformance chartPerformance = new ChartPerformance {
                HighScore = file.Get32(),
                PerfectHits = file.Get32(),
                TotalHits = file.Get32(),
                GhostHits = file.Get32(),
                ChartTotalNotes = file.Get32(),
                ChartTotalGhostNotes = file.Get32(),
                MaxCombo = file.Get32(),
                MaxScore = file.Get32(),
            };
            SetChartPerformance(chartHash, chartPerformance);
        }
    }

    public static void PrintoutChartMap(FileAccess file) {
        uint count = file.Get32();
        GD.Print($"Count: {count}");
        for (uint i = 0; i < count; i++) {
            GD.Print($"({i})\n\tChart UID:{file.Get64()}");
            GD.Print($"\tHighscore:{file.Get32()}");
            GD.Print($"\tPerfect Hits:{file.Get32()}");
            GD.Print($"\tTotal Hits:{file.Get32()}");
            GD.Print($"\tGhost Hits:{file.Get32()}");
            GD.Print($"\tChart Total Notes:{file.Get32()}");
            GD.Print($"\tChart Total Ghost Notes:{file.Get32()}");
            GD.Print($"\tMax Combo:{file.Get32()}");
        }
    }
}
