class_name InputConsumerButton
extends Button


## Emitted when the user "confirms" a new key to change to.
signal current_event_changed(new: InputEvent)


enum InputFilter {
	KEYBOARD = 1,
	JOYBUTTON = 1 << 1,
	JOYAXIS = 1 << 2,
}


var filter: int = InputFilter.KEYBOARD | InputFilter.JOYBUTTON | InputFilter.JOYAXIS

var current_event: InputEvent:
	set(new):
		if current_event == new:
			return
		if current_event:
			last_event = current_event
		current_event = new
		text = _text_for_event(current_event)

var last_event: InputEvent


static func _text_for_event(p_event:InputEvent) -> String:
	if p_event == null:
		return "listening..."
	
	if p_event is InputEventKey:
		return OS.get_keycode_string(
			p_event.get_keycode_with_modifiers()
		)
	
	return JoypadConverter.get_joypad_event_string(p_event)


func _init() -> void:
	focus_exited.connect(_on_focus_exited)


func _pressed() -> void:
	current_event = null


func _input(event: InputEvent) -> void:
	if current_event:
		return
	
	# "accept" events also press the button, which causes it to null out...
	# So we have a special case against this if we are expecting an input
	if event.is_action_pressed(&"ui_accept"): 
		accept_event()
		current_event = event


func _unhandled_input(event: InputEvent) -> void:
	if current_event:
		return
	
	var accept: bool = false
	# this is a little shoddy but I don't care
	if event is InputEventKey and filter & InputFilter.KEYBOARD and event.is_pressed():
		accept = true
	elif event is InputEventJoypadMotion and filter & InputFilter.JOYAXIS:
		accept = true
	elif event is InputEventJoypadButton and filter & InputFilter.JOYBUTTON and event.is_pressed():
		accept = true
	
	if accept:
		accept_event()
		current_event = event
		current_event_changed.emit()


func _on_focus_exited() -> void:
	if current_event:
		return
	current_event = last_event
