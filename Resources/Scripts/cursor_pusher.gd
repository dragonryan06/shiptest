extends CharacterBody2D
class_name CursorPusher

const radius = 8
var mouse_down = false

func _draw() -> void:
	if mouse_down:
		draw_circle(Vector2.ZERO, radius, Color.RED)

func _unhandled_input(event: InputEvent) -> void:
	if (event is InputEventMouseButton):
		match event.button_index:
			MOUSE_BUTTON_LEFT:
				mouse_down = event.pressed
				if (mouse_down):
					$CollisionShape2D.shape = CircleShape2D.new()
				else:
					$CollisionShape2D.shape = null
				queue_redraw()

func _process(delta: float) -> void:
	position = lerp(position, get_global_mouse_position(), delta+0.5)
	queue_redraw()
