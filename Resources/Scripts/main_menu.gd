extends Node2D

@onready var TestEnvironment = preload("res://Resources/Scenes/world.tscn")
@onready var Editor = preload("res://Resources/Scenes/editor.tscn")

func _on_test_environment_pressed() -> void:
	get_tree().change_scene_to_packed(TestEnvironment)

func _on_editor_pressed() -> void:
	get_tree().change_scene_to_packed(Editor)
