extends CanvasLayer

var enabled := true
var buttons: Dictionary = {}
var dpad_keys := ["up", "down", "left", "right"]


func _ready() -> void:
	layer = 30
	process_mode = Node.PROCESS_MODE_ALWAYS
	for action in ["up", "down", "left", "right", "a", "b", "start"]:
		var b := TouchScreenButton.new()
		b.texture_normal = load("res://assets/ui/touch_%s.png" % action)
		b.action = action
		b.passby_press = action in dpad_keys
		var shape := RectangleShape2D.new()
		var tex_size: Vector2 = b.texture_normal.get_size()
		shape.size = tex_size + Vector2(8, 8)
		b.shape = shape
		b.shape_centered = true
		b.modulate = Color(1, 1, 1, 0.55)
		add_child(b)
		buttons[action] = b
	ScreenScaler.layout_changed.connect(_layout)
	_layout()


func _layout() -> void:
	var safe := ScreenScaler.safe_rect()
	var bottom := safe.end.y
	var c := Vector2(safe.position.x + 30, bottom - 36)
	buttons["up"].position = c + Vector2(-8, -24)
	buttons["down"].position = c + Vector2(-8, 8)
	buttons["left"].position = c + Vector2(-24, -8)
	buttons["right"].position = c + Vector2(8, -8)
	var r := safe.end.x
	buttons["a"].position = Vector2(r - 26, bottom - 58)
	buttons["b"].position = Vector2(r - 50, bottom - 38)
	buttons["start"].position = Vector2(safe.position.x + safe.size.x / 2 - 12, bottom - 16)


func _process(_delta: float) -> void:
	var show := enabled and not Ui.is_busy() and Router.touch_allowed()
	if visible != show:
		visible = show
		if not show:
			for a in buttons:
				if Input.is_action_pressed(a):
					Input.action_release(a)
