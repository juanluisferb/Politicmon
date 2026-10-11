extends Node2D


func _ready() -> void:
	ScreenScaler.layout_changed.connect(queue_redraw)


func _draw() -> void:
	var size := Vector2(ScreenScaler.logical_size)
	var cols := ceili(size.x / 16.0)
	var rows := ceili(size.y / 16.0)
	for y in range(rows):
		for x in range(cols):
			var c := Color("#88c858") if (x + y) % 2 == 0 else Color("#387830")
			draw_rect(Rect2(x * 16, y * 16, 16, 16), c)
	var safe := ScreenScaler.safe_rect()
	draw_rect(safe.grow(-0.5), Color("#e8b400"), false, 1.0)
	draw_string(UiStyle.FONT, Vector2(4, 12), "%dx%d  x%d" % [ScreenScaler.logical_size.x, ScreenScaler.logical_size.y, ScreenScaler.scale_factor], HORIZONTAL_ALIGNMENT_LEFT, -1, 8, Color.WHITE)
