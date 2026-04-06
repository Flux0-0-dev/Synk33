using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using SYNK33.chart;
using SYNK33.core;

namespace SYNK33.spawner;

public partial class Spawner3D : Node3D {
	[Export] public float ScrollSpeed { get; set; } = 1.0f;
	[Export] public float AudioOffset { get; set; }

	[Export] public required NodePath JudgementLine { get; set; }

	[Export] public required NodePath Conductor { get; set; }
	[Export] public required JudgementManager JudgementManager { get; set; }
	[ExportGroup("Visuals")]
	[Export] public required Material NoteMaterialPurple { get; set; }
	[Export] public required Material NoteMaterialGlowPurple { get; set; }
	[Export] public required Material NoteMaterialBlue { get; set; }
	[Export] public required Material NoteMaterialGlowBlue { get; set; }
	[Export] public required Material NoteMaterialOrange { get; set; }
	[Export] public required Material NoteMaterialGlow { get; set; }
	
	private Chart _chart;

	private Conductor _conductor;

	private PooledSpawner _noteSpawner;
	private PooledSpawner _holdNoteSpawner;

	public override void _Ready() {
		_conductor = GetNode<Conductor>(Conductor);
		_chart = _conductor.Chart;
        _noteSpawner = GetNode<PooledSpawner>("NoteSpawner");
        _holdNoteSpawner = GetNode<PooledSpawner>("HoldNoteSpawner");
        ApplySettings();
        SpawnNotes();
	}

    private void ApplySettings() {
        var settingsManager = GetNode<Node>("/root/SettingsManager");
        var settings = (GodotObject)settingsManager.Get("current");
        var scrollSpeed = (float)settings.Get("scroll_speed");
        ScrollSpeed = scrollSpeed;
        var customOffset = (float)settings.Get("custom_offset");
        AudioOffset = customOffset;
    }

    private void SpawnNotes() {
		var judgementY = GetNode<Marker3D>(JudgementLine).GlobalPosition.Z;
		Parallel.ForEach(_chart.Notes, note =>
		{
			var absoluteBeat = note.Bar * _chart.BeatsPerMeasure + note.Beat + (float)note.Sixteenth/ 4.0f + AudioOffset;
			var spawnY = absoluteBeat * ScrollSpeed * judgementY;
			Callable spawn_note = Callable.From(() => {SpawnNote(note.ToNote(), new Vector2(0, -spawnY));});
			spawn_note.CallDeferred();
		});
	}

	private void SpawnNote(Note note, Vector2 position) {
		var judgementY = GetNode<Marker3D>(JudgementLine).GlobalPosition.Z;
		NoteObject3D noteInstance = note switch {
			Note.Hold => _holdNoteSpawner.Spawn<NoteObject3D>(),
			Note.Tap => _noteSpawner.Spawn<NoteObject3D>(),
			_ => throw new ArgumentOutOfRangeException(nameof(note), note, null)
		};
		
		if (noteInstance is HoldNoteObject3D holdNoteInstance) {
			if (note is Note.Hold holdNote) {
				holdNoteInstance.EndTime = holdNote.EndNote;
				holdNoteInstance.BeatsPerMeasure = _chart.BeatsPerMeasure;
				holdNoteInstance.UnitsPerBeat = ScrollSpeed;
				JudgementManager.NoteHeld += holdNoteInstance.StartHold;
				JudgementManager.NoteReleased += holdNoteInstance.EndHold;
				JudgementManager.HoldJudged += holdNoteInstance.NoteJudged;
			}
		}

		noteInstance.Speed = -judgementY / _conductor.SecondsPerBeat * ScrollSpeed;
		noteInstance.Type = note.Type;
		noteInstance.Conductor = _conductor;
		noteInstance.Material = note.Type switch {
			NoteType.Left => NoteMaterialPurple,
			NoteType.Middle => NoteMaterialBlue,
			NoteType.Right => NoteMaterialOrange,
			_ => throw new ArgumentOutOfRangeException()
		};
		noteInstance.MaterialGlow = note.Type switch {
			NoteType.Left => NoteMaterialGlowPurple,
			NoteType.Middle => NoteMaterialGlowBlue,
			NoteType.Right => NoteMaterialGlow,
			_ => throw new ArgumentOutOfRangeException()
		};
		var lanePosition = note.Type switch {
			NoteType.Left => position.X - 1f,
			NoteType.Middle => position.X,
			NoteType.Right => position.X + 1f,
			_ => throw new ArgumentOutOfRangeException()
		};
		// +2 is a magic number hack for JudgementLine position in the world
		noteInstance.Position = new Vector3(lanePosition, 0, (2 + judgementY - position.Y));
		noteInstance.StartTime = note.StartTime;
		JudgementManager.NoteMissed += noteInstance.SetMissed;
		JudgementManager.NoteHit += noteInstance.SetHit;
		AddChild(noteInstance);
	}
}