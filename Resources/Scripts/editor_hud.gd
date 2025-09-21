extends CanvasLayer

const INVENTORY_BACKSTAGE = Vector2(1152.0, 0.0)

signal selection_changed(hotbar_idx)

func _on_hotbar_button_pressed(idx: int) -> void:
	selection_changed.emit(idx)

func _on_show_inventory_toggled(toggled_on: bool) -> void:
	var tween = get_tree().create_tween()
	tween.set_trans(Tween.TRANS_SINE)
	if toggled_on:
		$HUD/ShowInventory.text = "Hide Inventory"
		tween.tween_property($HUD/PartInventory, "position", Vector2.ZERO, 0.25)
	else:
		$HUD/ShowInventory.text = "Show Inventory"
		tween.tween_property($HUD/PartInventory, "position", INVENTORY_BACKSTAGE, 0.25)
