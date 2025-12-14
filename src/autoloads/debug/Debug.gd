extends Node

func _input(event):
	# Why do those actions keep dissapearing from options.cfg?
	if event.is_action_pressed("restart"):
		get_tree().reload_current_scene()
	if event is InputEventKey:
		if event.key_label == KEY_R && event.pressed:
			get_tree().reload_current_scene()
