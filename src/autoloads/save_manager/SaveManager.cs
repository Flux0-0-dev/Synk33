using Godot;

namespace SYNK33.autoloads.save_manager;

public partial class SaveManager : Node, ISaveInfo {
    private const string SavePath = "user://save.sav";
    private const string ChartMapSavePath = "user://chart_map_save.dat";

    private readonly SaveData _save;

    public SaveManager() {
        _save = new SaveData();
        if (!FileAccess.FileExists(SavePath)) {
            Save();
            return;
        }
        Load();
    }

    public override void _Notification(int what) {
        if (what == NotificationWMCloseRequest) {
            ResourceSaver.Save(_save, SavePath);
        }
    }

    public bool HasChartPerformance(long chartHash) {
        return _save.HasChartPerformance(chartHash);
    }

    public ChartPerformance? GetChartPerformance(long chartHash) {
        return _save.GetChartPerformance(chartHash);
    }

    public long GetChartHighScore(long chartHash) {
        ChartPerformance? performance = _save.GetChartPerformance(chartHash);
        if (performance is null) {
            return -1;
        }
        return performance.HighScore;
    }

    public void SetChartPerformance(long chartHash, ChartPerformance chartPerformance) {
        _save.SetChartPerformance(chartHash, chartPerformance);
    }

    public void Save() {
        _save.Save(SavePath);
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Write);
        _save.SerializeChartMap(file);
        file.Close();
    }

    public void Load() {
        _save.Load(SavePath);
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Read);
        _save.DeserializeChartMap(file);
        file.Close();
    }

    public void PrintoutChartMap() {
        FileAccess file = FileAccess.Open(ChartMapSavePath, FileAccess.ModeFlags.Read);
        SaveData.PrintoutChartMap(file);
        file.Close();
    }
}
