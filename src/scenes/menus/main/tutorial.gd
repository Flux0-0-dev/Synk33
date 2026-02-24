extends Button

@export var dialogue:DialogueResource

func _pressed() -> void:
	DialogueBalloon.start(dialogue, "start")
