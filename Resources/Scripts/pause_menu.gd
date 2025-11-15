extends PanelContainer

func _unhandled_key_input(event: InputEvent) -> void:
	var key_event := event as InputEventKey
	if (key_event.pressed and key_event.keycode == KEY_ESCAPE):
		visible = !visible

func _on_main_menu_pressed() -> void:
	get_tree().change_scene_to_file("res://Resources/Scenes/main_menu.tscn")
