extends CanvasLayer


const DEFAULT_ANIMATION: StringName = &"wipe"


## Emitted at the point in a transition when the screen is totally obscured.
signal transition_midpoint()
## Emitted at the end of a transition.
signal transition_endpoint()


var transitioning:bool


@onready var animation_player:AnimationPlayer = $AnimationPlayer
@onready var audio_stream_player: AudioStreamPlayer = $AudioStreamPlayer

func _input(_event: InputEvent) -> void:
	if transitioning:
		get_viewport().set_input_as_handled()


## Plays a transition. To do something at the end or in the middle of a transition,
## use [signal transition_midpoint] and [signal transition_endpoint]
func play_transition(animation_name:StringName = DEFAULT_ANIMATION) -> void:
	transitioning = true
	animation_player.play(animation_name)
	audio_stream_player.play()

## @deprecated
## please use [method play_transition] and [method SceneManager.SwitchToBasicScene].[br]
## Transition and change main scene to file.
func transition_to_file(path:String, animation_name:StringName = DEFAULT_ANIMATION) -> void:
	play_transition(animation_name)
	await transition_midpoint
	get_tree().change_scene_to_file(path)


## A  version of [method play_transition] that returns the amount of time to wait for either 
## The midpoint or endpoint of the animation. Meant to be used in dialogue, wrapped in
## [code]wait()[/code], as a shorthand for 
## [codeblock]
## do TransitionManager.play_transition(&"wipe")
## do wait(TransitionManaget.get_transition_midpoint_time(&"wipe"))
## [/codeblock]
func dialogue_transition(animation_name:StringName = DEFAULT_ANIMATION, 
		use_midpoint:bool = true) -> float:
	play_transition(animation_name)
	
	if use_midpoint:
		return get_transition_midpoint_time(animation_name)
	else:
		return get_transition_endpoint_time(animation_name)


## Returns the midpoint time in a transition animation. Returns -1 if not applicable.
func get_transition_midpoint_time(animation_name:StringName) -> float:
	return _get_animation_method_callback_time(animation_name, ^".", &"_transition_midpoint_callback")


## Returns the endpoint time in a transition animation. Returns -1 if not applicable.
func get_transition_endpoint_time(animation_name:StringName) -> float:
	var animation := animation_player.get_animation(animation_name)
	if not animation:
		return -1
	return animation.length


## Returns the duration from the midpoint to the endpoint. See [method get_transition_midpoint_time]
## and [method get_transition_endpoint_time].
func get_transition_mid_to_end_time(animation_name:StringName) -> float:
	return get_transition_endpoint_time(animation_name) - get_transition_midpoint_time(animation_name)


func _get_animation_method_callback_time(animation_name:StringName, 
		node:NodePath, method:StringName) -> float:
	var animation := animation_player.get_animation(animation_name)
	if not animation:
		return -1
	
	var track_idx := animation.find_track(node, Animation.TYPE_METHOD)
	if track_idx == -1:
		push_error("Animation \"%s\" has no method track for node \"%s\"" % [animation_name, node])
		return -1
	
	for key_idx in animation.track_get_key_count(track_idx):
		var callback: Dictionary = animation.track_get_key_value(track_idx, key_idx)
		if callback["method"] == method:
			return animation.track_get_key_time(track_idx, key_idx)
	
	push_error("Animation \"%s\" has no method call for node \"%s\" method \"%s\"" % 
			[animation_name, node, method])
	return -1


func _transition_midpoint_callback() -> void:
	transition_midpoint.emit()


func _on_animation_player_animation_finished(anim_name: StringName) -> void:
	if anim_name != &"RESET":
		transitioning = false
		transition_endpoint.emit()
