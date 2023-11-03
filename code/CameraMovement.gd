extends Camera2D

var zoom_min = Vector2(.2,.2)
var zoom_max = Vector2(2,2)
var zoom_speed = Vector2(.2,.2)

func _input(event):
	if event is InputEventMouseButton:
		if event.is_pressed():
			if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
				if zoom > zoom_min:
					zoom -= zoom_speed
			if event.button_index == MOUSE_BUTTON_WHEEL_UP:
				if zoom < zoom_max:
					zoom += zoom_speed


func _unhandled_input(event):
	if event is InputEventMouseMotion:
		if event.button_mask == MOUSE_BUTTON_MASK_MIDDLE:
			position -= event.relative / zoom