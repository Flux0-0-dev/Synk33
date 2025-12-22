extends SpinBox


@export var property:StringName


func _ready() -> void:
	value_changed.connect(_value_changed)


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_VISIBILITY_CHANGED:
			set_value_no_signal(SettingsManager.tenative.get(property))


func _value_changed(new_value: float) -> void:
	SettingsManager.tenative.set(property, new_value)
