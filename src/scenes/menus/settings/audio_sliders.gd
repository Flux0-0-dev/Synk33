extends GridContainer


func _init() -> void:
	for i in AudioServer.bus_count:
		var bus_name := AudioServer.get_bus_name(i)
		var label := Label.new()
		label.text = bus_name
		label.name = bus_name
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		add_child(label)
		var slider := HSlider.new()
		slider.name = bus_name + "Slider"
		slider.min_value = 0
		slider.max_value = 1
		slider.step = 0.05
		slider.value_changed.connect(_on_slider_changed.bind(i))
		slider.set_value_no_signal(SettingsManager.tenative.get_bus_volume(i))
		slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		slider.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		add_child(slider)


func _notification(what: int) -> void:
	match what:
		NOTIFICATION_VISIBILITY_CHANGED:
			for i in AudioServer.bus_count:
				get_child(i * 2 - 1).set_value_no_signal(SettingsManager.tenative.get_bus_volume(i))


func _on_slider_changed(value:float, bus_index:int) -> void:
	SettingsManager.tenative.set_bus_volume(bus_index, value)
