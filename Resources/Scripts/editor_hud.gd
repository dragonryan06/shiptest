extends CanvasLayer

const INVENTORY_BACKSTAGE = Vector2(1152.0, 0.0)
const INVENTORY_PATH = ^"PartInventory/PanelContainer/VBoxContainer/ScrollContainer/MarginContainer/GridContainer"

signal selection_changed(part_id: int)
var last_selection = -1

var selected_inventory_part = null

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
