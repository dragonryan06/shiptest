extends CanvasLayer

const INVENTORY_BACKSTAGE = Vector2(1152.0, 0.0)

# part_id is the order of the part in parts.json
signal selection_changed(part_id: int)

func _on_hotbar_button_pressed(idx: int) -> void:
	pass
	#selection_changed.emit(part_id)

func _on_show_inventory_toggled(toggled_on: bool) -> void:
	var tween = get_tree().create_tween()
	tween.set_trans(Tween.TRANS_SINE)
	if toggled_on:
		$ShowInventory.text = "Hide Inventory"
		tween.tween_property($PartInventory, "position", Vector2.ZERO, 0.25)
	else:
		$ShowInventory.text = "Show Inventory"
		tween.tween_property($PartInventory, "position", INVENTORY_BACKSTAGE, 0.25)
