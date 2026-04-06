extends Control


@export var player:AudioStreamPlayer


@onready var slider:HSlider = $PlaySlider


func _process(_delta: float) -> void:
	if player.playing == false:
		return
	slider.max_value = player.stream.get_length()
	slider.set_value_no_signal(player.get_playback_position() + AudioServer.get_time_since_last_mix())


func _on_play_slider_drag_started() -> void:
	if player.playing == false:
		return
	player.stream_paused = true


func _on_play_slider_drag_ended(value_changed: bool) -> void:
	player.stream_paused = false
	if value_changed:
		player.seek(slider.value)
