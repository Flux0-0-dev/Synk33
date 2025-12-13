using System;
using Godot;
using SYNK33.chart;

namespace SYNK33.editor;

public static class EditorSelection {
    public static void SelectBeat(
        EditorState state,
        Chart chart,
        Vector2 mousePosition,
        Label selectedTimeLabel
    ) {
        var beatTime = -(mousePosition.Y - state.PanY) / state.Zoom;
        if (beatTime < 0) beatTime = 0;

        var totalSixteenthsPerBar = chart.BeatsPerMeasure * 4.0;
        var effectiveSnapping = state.GetEffectiveSnapping();
        var snapGridSize = totalSixteenthsPerBar / effectiveSnapping;

        var totalSixteenths = beatTime * 4.0;
        var snappedTotalSixteenths = Math.Round(totalSixteenths / snapGridSize) * snapGridSize;

        var bar = Math.Floor(snappedTotalSixteenths / totalSixteenthsPerBar);
        var remainingSixteenths = snappedTotalSixteenths % totalSixteenthsPerBar;
        var beat = Math.Floor(remainingSixteenths / 4.0);
        var sixteenth = remainingSixteenths % 4.0;

        var sixteenthInt = (int)Math.Floor(sixteenth);
        var sixteenthFrac = sixteenth - sixteenthInt;
        
        var timeText = $"{bar + 1}.{beat + 1}.{sixteenthInt + 1}";
        if (sixteenthFrac > 0.001) {
            timeText += $"{sixteenthFrac:0.##}".Substring(1) + "+";
        }

        selectedTimeLabel.Text = timeText;
        state.SelectedTime = new NoteTime((long)bar, (long)beat, sixteenth);
    }

    public static void SelectLane(
        EditorState state,
        Vector2 mousePosition,
        float viewportWidth,
        Label selectedLaneLabel
    ) {
        const int totalLaneWidth = EditorConstants.MaxLanes * EditorConstants.LaneWidth;
        var startX = (viewportWidth - totalLaneWidth) / 2;
        var relativeX = mousePosition.X - startX;
        
        var oldLane = state.SelectedLane;
        state.SelectedLane = (int)Math.Clamp(Math.Floor(relativeX / EditorConstants.LaneWidth), 0, EditorConstants.MaxLanes - 1);
        
        if (oldLane != state.SelectedLane) {
            GD.Print($"Lane selected: {state.SelectedLane}");
        }
        
        selectedLaneLabel.Text = $"Lane: {state.SelectedLane}";
    }
    
    public static float GetTimeAtMouseY(
        float mouseY,
        float zoom,
        float panY,
        float bpm
    ) {
        var beatTime = -(mouseY - panY) / zoom;
        if (beatTime < 0) beatTime = 0;
        
        var secondsPerBeat = 60.0f / bpm;
        return beatTime * secondsPerBeat;
    }
}
