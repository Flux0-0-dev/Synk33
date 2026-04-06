using Godot;
using SYNK33.autoloads.save_manager;
using SYNK33.core;
using SYNK33.scenemanager;

namespace SYNK33.scenes.menus.results;

public partial class ResultsScreen : Control {
    private Label _perfectCountLabel = null!;
    private Label _greatCountLabel = null!;
    private Label _okayCountLabel = null!;
    private Label _missesCountLabel = null!;

    private Label _hitsCountLabel = null!;
    private Label _totalCountLabel = null!;
    private Label _bestComboLabel = null!;
    private Label _maxComboLabel = null!;

    private Label _earlyCountLabel = null!;
    private Label _lateCountLabel = null!;
    private ProgressBar _timingBar = null!;

    private Label _scoreCountLabel = null!;
    private Label _maxScoreCountLabel = null!;
    private TextureRect _rankTexture = null!;
    private Label _dnfLabel = null!;
    private Button _playAgainButton = null!;
    private SaveManager _saveManager = null!;

    private const int RankSpriteWidth = 101;
    private const int RankSpriteHeight = 90;

    public override void _Ready() {
        _perfectCountLabel = GetNode<Label>("%PerfectCount");
        _greatCountLabel = GetNode<Label>("%GreatCount");
        _okayCountLabel = GetNode<Label>("%OkayCount");
        _missesCountLabel = GetNode<Label>("%MissesCount");

        _hitsCountLabel = GetNode<Label>("%HitsCount");
        _totalCountLabel = GetNode<Label>("%TotalCount");
        _bestComboLabel = GetNode<Label>("%BestComboCount");
        _maxComboLabel = GetNode<Label>("%MaxComboCount");

        _earlyCountLabel = GetNode<Label>("%EarlyCount");
        _lateCountLabel = GetNode<Label>("%LateCount");
        _timingBar = GetNode<ProgressBar>("%TimingBar");

        _scoreCountLabel = GetNode<Label>("%ScoreCount");
        _maxScoreCountLabel = GetNode<Label>("%MaxScoreCount");
        _rankTexture = GetNode<TextureRect>("%Rank");
        _dnfLabel = GetNode<Label>("%DnfLabel");
        _playAgainButton = GetNode<Button>("PlayAgain");
        _saveManager = GetNode<SaveManager>("/root/SaveManager");

        _playAgainButton.Pressed += OnPlayAgainPressed;
    }

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("ui_cancel")) {
            GetViewport().SetInputAsHandled();
            ExitToSongSelect();
        }
    }

    public void DisplayResults(GameplayResults gameplayResults) {
        _perfectCountLabel.Text = gameplayResults.PerfectCount.ToString();
        _greatCountLabel.Text = gameplayResults.GreatCount.ToString();
        _okayCountLabel.Text = gameplayResults.OkayCount.ToString();
        _missesCountLabel.Text = gameplayResults.MissCount.ToString();

        _hitsCountLabel.Text = gameplayResults.TotalHits.ToString();
        _totalCountLabel.Text = gameplayResults.TotalNotes.ToString();
        _bestComboLabel.Text = gameplayResults.MaxCombo.ToString();
        _maxComboLabel.Text = gameplayResults.TotalNotes.ToString();

        _earlyCountLabel.Text = gameplayResults.EarlyCount.ToString();
        _lateCountLabel.Text = gameplayResults.LateCount.ToString();
        _dnfLabel.Visible = !gameplayResults.SongFinished;
        _rankTexture.Visible = gameplayResults.SongFinished;
        _scoreCountLabel.Text = gameplayResults.Score.ToString("N0");
        _maxScoreCountLabel.Text = $"/ {gameplayResults.MaxScore:N0}";

        UpdateTimingBar(gameplayResults.EarlyCount, gameplayResults.LateCount);
        if (gameplayResults.SongFinished) {
            UpdateRank(gameplayResults.Rank);
        }
        
        SaveHighScore(gameplayResults);
    }
    
    private void SaveHighScore(GameplayResults gameplayResults) {
        var chartHash = gameplayResults.ChartHash;
        var existingPerformance = _saveManager.GetChartPerformance(chartHash);
        var newPerformance = gameplayResults.ToChartPerformance();
        
        if (existingPerformance == null || newPerformance.HighScore > existingPerformance.HighScore) {
            _saveManager.SetChartPerformance(chartHash, newPerformance);
            _saveManager.Save();
        }
    }

    private void UpdateTimingBar(int earlyCount, int lateCount) {
        var totalTimingHits = earlyCount + lateCount;
        if (totalTimingHits == 0) {
            _timingBar.Value = _timingBar.MaxValue / 2;
            return;
        }

        var earlyRatio = earlyCount / (double)totalTimingHits;
        _timingBar.Value = earlyRatio * _timingBar.MaxValue;
    }

    private void UpdateRank(Rank rank) {
        var atlas = (AtlasTexture)_rankTexture.Texture;
        atlas.Region = new Rect2(0, RankSpriteHeight * (int)rank, RankSpriteWidth, RankSpriteHeight);
    }

    private void OnPlayAgainPressed() {
        ExitToSongSelect();
    }

    private void ExitToSongSelect() {
        var sceneManager = GetNode<SceneManager>("/root/SceneManager");
        sceneManager.ChangeSceneToBasicScene(BasicScene.SongSelect);
    }
}