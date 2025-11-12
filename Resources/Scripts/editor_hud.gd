extends CanvasLayer

const INVENTORY_BACKSTAGE := Vector2(1152.0, 0.0)
const INVENTORY_PATH := ^"PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer"

signal name_changed(new_name: String)
signal selection_changed(part_id: int)
signal new_file
signal open_file(filename: String)
signal save_file(filename: String)

var last_selection := -1
var last_filename : String

var selected_inventory_part : Button

func _ready() -> void:
	var popup : PopupMenu = $FileMenu.get_popup()
	
	var new_shortcut := Shortcut.new()
	var new_key := InputEventKey.new()
	new_key.ctrl_pressed = true
	new_key.keycode = KEY_N
	new_shortcut.events.append(new_key)
	popup.set_item_shortcut(0, new_shortcut)
	
	var open_shortcut := Shortcut.new()
	var open_key := InputEventKey.new()
	open_key.ctrl_pressed = true
	open_key.keycode = KEY_O
	open_shortcut.events.append(open_key)
	popup.set_item_shortcut(1, open_shortcut)
	
	var save_shortcut := Shortcut.new()
	var save_key := InputEventKey.new()
	save_key.ctrl_pressed = true
	save_key.keycode = KEY_S
	save_shortcut.events.append(save_key)
	popup.set_item_shortcut(2, save_shortcut)
	
	var save_as_shortcut := Shortcut.new()
	var save_as_key := InputEventKey.new()
	save_as_key.shift_pressed = true
	save_as_key.ctrl_pressed = true
	save_as_key.keycode = KEY_S
	save_as_shortcut.events.append(save_as_key)
	popup.set_item_shortcut(3, save_as_shortcut)
	
	popup.id_pressed.connect(_on_filemenu_id_pressed)

func _on_hotbar_button_pressed(idx: int) -> void:
	var button = $Hotbar/HBoxContainer.get_child(idx) as Button
	if (selected_inventory_part != null):
		# we are picking a part from the inventory
		$SelectDialog.hide()
		button.icon = selected_inventory_part.icon.duplicate()
		button.set_meta("part_id", selected_inventory_part.get_meta("part_id"))
		selected_inventory_part = null
	elif (button.has_meta("part_id")):
		var id = button.get_meta("part_id")
		if (id == last_selection):
			last_selection = -1
			selection_changed.emit(-1)
		else:
			last_selection = id
			selection_changed.emit(id)

func _on_show_inventory_toggled(toggled_on: bool) -> void:
	var tween = get_tree().create_tween()
	tween.set_trans(Tween.TRANS_SINE)
	if toggled_on:
		$ShowInventory.text = "Hide Inventory"
		tween.tween_property($PartInventory, "position", Vector2.ZERO, 0.25)
	else:
		$ShowInventory.text = "Show Inventory"
		$SelectDialog.hide()
		selected_inventory_part = null
		tween.tween_property($PartInventory, "position", INVENTORY_BACKSTAGE, 0.25)

func _on_inventory_button_pressed(idx: int):
	selected_inventory_part = get_node(INVENTORY_PATH).get_child(idx)
	$SelectDialog/VBoxContainer/MenuHeader/Label.text = selected_inventory_part.get_node("Label").text
	$SelectDialog.show()

func _on_grid_container_child_entered_tree(node: Node) -> void:
	var button = node as Button
	button.pressed.connect(_on_inventory_button_pressed.bind(button.get_index()))

func _on_select_dialog_cancel_pressed() -> void:
	$SelectDialog.hide()
	selected_inventory_part = null

func _on_name_box_text_submitted(new_text: String) -> void:
	name_changed.emit(new_text)

func _on_filemenu_id_pressed(id: int) -> void:
	match id:
		0:
			# New...
			last_filename = ""
			new_file.emit()
		1:
			# Open...
			var file_dialog = FileDialog.new()
			get_viewport().add_child(file_dialog)
			file_dialog.access = FileDialog.ACCESS_FILESYSTEM
			file_dialog.file_mode = FileDialog.FILE_MODE_OPEN_FILE
			file_dialog.current_path = ProjectSettings.globalize_path("user://")
			file_dialog.popup_centered()
			
			await file_dialog.file_selected
			var filename = file_dialog.current_file
			last_filename = filename
			open_file.emit(filename)
		2:
			# Save
			if (last_filename.is_empty()):
				_on_filemenu_id_pressed(3)
				return
			
			save_file.emit(last_filename)
		3:
			# Save As...
			var file_dialog = FileDialog.new()
			get_viewport().add_child(file_dialog)
			file_dialog.access = FileDialog.ACCESS_FILESYSTEM
			file_dialog.file_mode = FileDialog.FILE_MODE_SAVE_FILE
			file_dialog.current_path = ProjectSettings.globalize_path("user://")
			file_dialog.popup_centered()
			
			await file_dialog.file_selected
			var filename = file_dialog.current_file
			last_filename = filename
			save_file.emit(filename)
