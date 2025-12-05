extends Control

signal debug_spawning(filename)

func _ready() -> void:
	$Actions.get_popup().id_pressed.connect(_on_actions_id_pressed)

func _process(_delta) -> void:
	if (visible):
		$DataList/FPSCounter.text = str(Engine.get_frames_per_second()) + " fps"

func _on_actions_id_pressed(id : int) -> void:
	match id:
		0:
			# Spawn...
			var file_dialog = FileDialog.new()
			get_viewport().add_child(file_dialog)
			file_dialog.access = FileDialog.ACCESS_FILESYSTEM
			file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
			file_dialog.add_filter("*.json", "JSON files")
			file_dialog.current_path = ProjectSettings.globalize_path("user://")
			file_dialog.canceled.connect(file_dialog.queue_free)
			file_dialog.popup_centered()
			
			await file_dialog.file_selected
			var filename = file_dialog.current_path
			file_dialog.queue_free()
			debug_spawning.emit(filename)
