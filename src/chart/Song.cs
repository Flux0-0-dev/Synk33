using System.ComponentModel;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

namespace SYNK33.chart;

[GlobalClass]
public partial class Song : Resource {
	[Export] public string Name { get; set; }
	[Export] public string Author { get; set; }
	[Export] public string Genre { get; set; }
	[Export] public AudioStream Audio { get; set; }
	[Export] public Texture2D Cover { get; set; }
	[Export] public float Bpm { get; set; }
	[Export(PropertyHint.Flags, "Easy,Medium,Hard,Expert")] public int Difficulties { get; set; }

	private static Dictionary<Difficulty, string> DifficultyMap = new Dictionary<Difficulty, string> {
		{ Difficulty.Easy, "easy" },
		{ Difficulty.Medium, "medium" },
		{ Difficulty.Hard, "hard" },
		{ Difficulty.Expert, "expert" }
	};

	public Chart? GetChartByDifficulty(Difficulty difficulty) {
		if (!HasChart(difficulty)) {
			return null;
		}

		string path = $"{ResourcePath.GetBaseName()}_{DifficultyMap[difficulty]}.tres";
		ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(path, []);
		if (status == ResourceLoader.ThreadLoadStatus.InProgress){
			return GetChartByDifficulty(difficulty);
		}
		Chart? res = ResourceLoader.LoadThreadedGet(path) as Chart;
		ResourceLoader.LoadThreadedRequest(path);
		return res;
	}

	public bool HasChart(Difficulty difficulty) {
		return (Difficulties & (1 << (int)difficulty)) != 0;
	}
	public	void Prepare(){
		string path;
		foreach (var (_, value) in DifficultyMap){	
			path = $"{ResourcePath.GetBaseName()}_{value}.tres";
			if (FileAccess.FileExists(path)){
				ResourceLoader.LoadThreadedRequest(path);
			}
		}
	}

public enum Difficulty {
	Easy,
	Medium,
	Hard,
	Expert
}
}
