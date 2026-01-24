using Godot;
using SYNK33.chart;
using SYNK33.core;
using SYNK33.scenes.menus.results;

namespace SYNK33.scenemanager;

public enum BasicScene {
    MainMenu,
    Settings,
    SongSelect,
    Credits,
}

public partial class SceneManager : Node {
    private static PackedScene MainMenuPacked = GD.Load<PackedScene>("uid://dnk0thv8iasnk");
    private static PackedScene SettingsPacked = GD.Load<PackedScene>("uid://bwpjtqjpf7sp0");
    private static PackedScene SongSelectPacked = GD.Load<PackedScene>("uid://c75ew37wbtpa2");
    private static PackedScene CreditsPacked = GD.Load<PackedScene>("uid://ovi7mj6ghtqq");
    private static PackedScene GameScene = GD.Load<PackedScene>("uid://dkluvwvfg2iyk");
    private static PackedScene ResultsScene = GD.Load<PackedScene>("uid://djsdst1f8sf2u");
    private bool isCurrentSceneForeign = true;

    private Node MainMenu = null!;
    private Node Settings = null!;
    private Node SongSelect = null!;
    private Node Credits = null!;

    public override void _Ready() {
        base._Ready();
        MainMenu = MainMenuPacked.Instantiate();
        Settings = SettingsPacked.Instantiate();
        SongSelect = SongSelectPacked.Instantiate();
        Credits = CreditsPacked.Instantiate();
    }
    /// <summary>
    /// Switch to a basic scene that requires no arguments 
    /// </summary>
    /// <param name="scene"></param>
    public void ChangeSceneToBasicScene(BasicScene scene) {
        Node newScene = null!;
        switch (scene) {
            case BasicScene.MainMenu:
                newScene = MainMenu;
                break;
            case BasicScene.Settings:
                newScene = Settings;
                break;
            case BasicScene.SongSelect:
                newScene = SongSelect;
                break;
            case BasicScene.Credits:
                newScene = Credits;
                break;
        }
        SceneSwap(newScene);
        isCurrentSceneForeign = false;
    }
    public void ChangeSceneToForeignScene(PackedScene scene) {
        Node newScene = scene.Instantiate();
        SceneSwap(newScene);
        isCurrentSceneForeign = true;
    }
    public void ChangeSceneToGame(Chart chart) {
        Node gameInstance = GameScene.Instantiate();
        Conductor conductor = gameInstance.GetNode<Conductor>("Gameplay/Conductor");
        conductor.Chart = chart;
        SceneSwap(gameInstance);
        isCurrentSceneForeign = true;
    }
    public void ChangeSceneToResults(GameplayResults gameplayResults) {
        Node resultsInstance = ResultsScene.Instantiate();
        if (resultsInstance is ResultsScreen resultsScreen) {
            SceneSwap(resultsInstance);
            resultsScreen.DisplayResults(gameplayResults);
        }
        isCurrentSceneForeign = true;
    }
    private void SceneSwap(Node newScene) {
        Node currentScene = GetTree().CurrentScene;
        currentScene.GetParent().RemoveChild(currentScene);
        if (isCurrentSceneForeign) {
            currentScene.QueueFree();
        }
        GetTree().Root.AddChild(newScene);
        GetTree().CurrentScene = newScene;
    }
}
