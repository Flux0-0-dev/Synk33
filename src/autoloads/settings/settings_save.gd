class_name SettingsSave
extends Resource
## Saves options for easy referencing


const CATEGORY_GAMEPLAY := "gameplay"
const CATEGORY_INPUT := "input"
const CATEGORY_VIDEO := "video"
const CATEGORY_AUDIO := "audio"

const KEY_PHYSICAL_PREFIX:String = "!"

const MAXIMUM_LOOK_SENSITIVITY:float = 1 / PI / 30
const WINDOW_MODE_NAMES:PackedStringArray = [
	"windowed", "minimized", "maximized", "fullscreen", "exclusive_fullscreen"
]
const VSYNC_MODE_NAMES:PackedStringArray = [
	"disabled", "enabled", "adaptive", "mailbox"
]

const ERROR_MSG_INPUT_INVALID_KEY := 'Invalid Action name "%s" in config input section, skipping... %s'
const ERROR_MSG_INPUT_BAD_VALUE := 'Bad value for action "%s" in config input section, skipping...'
const ERROR_MSG_INPUT_BAD_CODESTRING := 'Bad codestring "%s" for action "%s" in config input section, skipping...'
const ERROR_MSG_INPUT_UNKNOWN_SUBSECTION := 'Unknown subsection "%s" for action in config input section, skipping...'

static var input_regex:RegEx = RegEx.create_from_string(r"([^\/]+)(?:\/(.+))?")

#region Gameplay
@export_group("Gameplay")
@export_range(0, 1.0, 0.001, "or_greater") var custom_offset:float = 0.0
@export_range(0.1, 10.0, 0.01, "or_greater") var scroll_speed:float = 2.8
#endregion Gameplay

#region Input
@export_group("Input")
@export_custom(
	PROPERTY_HINT_TYPE_STRING,
	# StringName/PROPERTY_HINT_INPUT_NAME:"show_builtin";Object/PROPERTY_HINT_RESOURCE_TYPE:"InputEvent"
	"21/43:show_builtin;28:24/17:InputEvent",
	PROPERTY_USAGE_EDITOR | PROPERTY_USAGE_STORAGE | PROPERTY_USAGE_ALWAYS_DUPLICATE
) var input_map:Dictionary[StringName,Array] = {}
#endregion Input

#region Video
@export_group("Video")
## Mode of the main window.
@export var window_mode:DisplayServer.WindowMode = DisplayServer.WINDOW_MODE_WINDOWED
@export var vsync_mode:DisplayServer.VSyncMode = DisplayServer.VSYNC_ENABLED
@export var max_fps:int = 0
@export var borderless:bool = false
#endregion Video

#region Audio
@export_group("Audio")
## Linear volumes of busses.
@export var bus_volumes:Array[float] = [
	1.0,
	1.0,
	1.0,
	1.0
]
#endregion Audio


static func load_config(path:String) -> SettingsSave:
	var instance := SettingsSave.new()
	if not FileAccess.file_exists(path):
		instance._load_default_input_map()
		return instance
	
	var file := ConfigFile.new()
	var error := file.load(path)
	
	if error == ERR_FILE_CANT_OPEN:
		push_error("Can't open options config file at \"%s\", returning default options" % path)
		return instance
	
	instance._load_gameplay_settings(file)
	instance._load_input_map(file)
	instance._load_video_settings(file)
	instance._load_audio_volumes(file)
	return instance


static func stringnum_deserialize(
			file:ConfigFile, 
			section:String, 
			property:String, 
			stringnum:PackedStringArray, 
			default:int
		) -> int:
	
	if not file.has_section_key(section, property):
		return default
	
	var index = stringnum.find(file.get_value(
		section, property, ""
	))
	if index == -1:
		return default
	return index


func save_config(path:String) -> void:
	var file:ConfigFile = ConfigFile.new()
	
	file.set_value(CATEGORY_GAMEPLAY, "custom_offset", custom_offset)
	file.set_value(CATEGORY_GAMEPLAY, "scroll_speed", scroll_speed)
	
	_save_input_map(file)
	
	file.set_value(CATEGORY_VIDEO, "window_mode", WINDOW_MODE_NAMES[window_mode])
	file.set_value(CATEGORY_VIDEO, "vsync_mode", VSYNC_MODE_NAMES[vsync_mode])
	file.set_value(CATEGORY_VIDEO, "max_fps", max_fps)
	file.set_value(CATEGORY_VIDEO, "borderless", borderless)
	
	for bus_idx in len(bus_volumes):
		var bus_name := AudioServer.get_bus_name(bus_idx)
		file.set_value(CATEGORY_AUDIO, bus_name, bus_volumes[bus_idx])
	
	file.save(path)


func set_bus_volume(bus_index:int, linear_volume:float) -> void:
	assert(len(bus_volumes) > bus_index, "bus index '0' does not already exist in bus volumes!")
	bus_volumes[bus_index] = linear_volume


func get_bus_volume(bus_index:int) -> float:
	assert(len(bus_volumes) > bus_index, "bus index '0' does not already exist in bus volumes!")
	return bus_volumes[bus_index]


func apply_settings(root:Viewport) -> void:
	_apply_input_map(root)
	_apply_video_settings(root)
	_apply_bus_volumes(root)

#region Loaders
func _load_gameplay_settings(file:ConfigFile) -> void:
	custom_offset = file.get_value(CATEGORY_GAMEPLAY, "custom_offset", custom_offset)
	scroll_speed = file.get_value(CATEGORY_GAMEPLAY, "scroll_speed", custom_offset)


func _load_input_map(file:ConfigFile) -> void:
	if not file.has_section(CATEGORY_INPUT):
		push_error('No "%s" section present in config, using defaults...' % CATEGORY_INPUT)
		_load_default_input_map()
		return
	_create_template_input_map()
	
	for key in file.get_section_keys(CATEGORY_INPUT):
		var key_match := input_regex.search(key)
		if key_match == null:
			push_error(ERROR_MSG_INPUT_INVALID_KEY % [key, "(Not in InputMap)"])
			return
		
		var action:StringName = key_match.get_string(1) as StringName
		var subsection:String = key_match.get_string(2)
		
		if not InputMap.has_action(action):
			push_error(ERROR_MSG_INPUT_INVALID_KEY % [action, "(Not in InputMap)"])
			continue
		
		var value = file.get_value(CATEGORY_INPUT, key)
		
		if value == null:
			push_error(ERROR_MSG_INPUT_BAD_VALUE % key)
			continue
		
		match subsection:
			"keyboard": # Keyboard inputs
				if value is not PackedStringArray:
					push_error(ERROR_MSG_INPUT_BAD_VALUE % key)
					continue
				
				for keycode_string:String in value:
					var event := InputEventKey.new()
					
					var keycode_find:int
					if keycode_string.begins_with(KEY_PHYSICAL_PREFIX): 
						keycode_string = keycode_string.substr(1)
						keycode_find = OS.find_keycode_from_string(keycode_string)
						event.physical_keycode = keycode_find as Key
					else:
						keycode_find = OS.find_keycode_from_string(keycode_string)
						event.keycode = keycode_find as Key
					
					
					if keycode_find == KEY_NONE or keycode_find == KEY_UNKNOWN:
						push_error(ERROR_MSG_INPUT_BAD_CODESTRING % [keycode_string, key])
						continue
					input_map[action].append(event)
			"controller":
				if value is not PackedStringArray:
					push_error(ERROR_MSG_INPUT_BAD_VALUE % key)
					continue
				
				for code_string:String in value:
					var event := JoypadConverter.joypad_input_event_from_string(code_string)
					if event == null:
						push_error(ERROR_MSG_INPUT_BAD_CODESTRING % [code_string, key])
						continue
					input_map[action].append(event)
			_:
				push_error(ERROR_MSG_INPUT_UNKNOWN_SUBSECTION % [subsection, key])
				continue


## Creates a blank template input map based on the defaults from [ProjectSettings]
func _create_template_input_map() -> void:
	input_map.clear()
	for action in InputMap.get_actions():
		input_map[action] = []


func _load_default_input_map() -> void:
	input_map.clear()
	for action in InputMap.get_actions():
		var defaults:Dictionary = ProjectSettings.get_setting_with_override("input/" + action)
		input_map[action] = defaults.events


func _load_video_settings(file:ConfigFile) -> void:
	window_mode = stringnum_deserialize(
		file, CATEGORY_VIDEO, "window_mode", WINDOW_MODE_NAMES, DisplayServer.WINDOW_MODE_WINDOWED
	) as DisplayServer.WindowMode
	vsync_mode = stringnum_deserialize(
		file, CATEGORY_VIDEO, "vsync_mode", VSYNC_MODE_NAMES, DisplayServer.VSYNC_ENABLED
	) as DisplayServer.VSyncMode
	
	max_fps = file.get_value(
		CATEGORY_VIDEO, "max_fps", max_fps
	)
	borderless = file.get_value(
		CATEGORY_VIDEO, "borderless", borderless
	)


func _load_audio_volumes(file:ConfigFile) -> void:
	if not file.has_section(CATEGORY_AUDIO):
		push_error("No Audio Volumes section present in config!")
		return
	
	var bus_names := file.get_section_keys(CATEGORY_AUDIO)
	for bus_name in bus_names:
		var bus_index := AudioServer.get_bus_index(bus_name)
		if bus_index == -1:
			push_error("Invalid bus name \"%s\" in options config" % bus_name)
			continue
		
		bus_volumes[bus_index] = file.get_value(
			CATEGORY_AUDIO, 
			bus_name, 
			bus_volumes[bus_index]
		)
#endregion Loaders

#region Savers
func _save_input_map(file:ConfigFile) -> void:
	# This is reused because why not
	var constructed:PackedStringArray = PackedStringArray()
	for action in input_map:
		var break_index:int = 0
		
		# keyboard pass
		for i in len(input_map[action]):
			if not (input_map[action][i] is InputEventKey):
				break_index = i
				break
			
			var processed:String = ""
			if (input_map[action][i] as InputEventKey).keycode != 0:
				processed = OS.get_keycode_string(
					(input_map[action][i] as InputEventKey).get_keycode_with_modifiers()
				)
			else:
				processed = KEY_PHYSICAL_PREFIX + OS.get_keycode_string(
					(input_map[action][i] as InputEventKey).get_physical_keycode_with_modifiers()
				)
			
			constructed.append(processed)
			
		file.set_value(CATEGORY_INPUT, action + "/keyboard", constructed)
		
		constructed = PackedStringArray()
		if break_index == 0:
			continue
		
		constructed.resize(len(input_map[action]) - break_index)
		# controller pass
		for i in len(input_map[action]) - break_index:
			constructed[i] = JoypadConverter.get_joypad_event_string(input_map[action][i + break_index])
		file.set_value(CATEGORY_INPUT, action + "/controller", constructed)
		
		constructed = PackedStringArray()
#endregion Savers

#region Appliers
func _apply_input_map(_root:Viewport) -> void:
	for action in input_map:
		InputMap.action_erase_events(action)
		for event in input_map[action]:
			InputMap.action_add_event(action, event)


func _apply_video_settings(_root:Viewport) -> void:
	DisplayServer.window_set_mode(window_mode)
	Engine.max_fps = max_fps
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, borderless)


func _apply_bus_volumes(_root:Viewport) -> void:
	for bus_index in len(bus_volumes):
		AudioServer.set_bus_volume_linear(bus_index, bus_volumes[bus_index])
#endregion Appliers
