extends Node

signal layout_changed

const MIN_LOGICAL := Vector2(160, 256)

var scale_factor: int = 1
var logical_size: Vector2i = Vector2i(180, 320)
var safe_margins: Rect2 = Rect2()
var forced_safe_area: Rect2i = Rect2i()


func _ready() -> void:
	get_tree().root.size_changed.connect(_update)
	_update()


func compute_scale(window_size: Vector2i) -> int:
	var s := floori(minf(window_size.x / MIN_LOGICAL.x, window_size.y / MIN_LOGICAL.y))
	return maxi(1, s)


func compute_logical_size(window_size: Vector2i, s: int) -> Vector2i:
	return Vector2i(window_size.x / s, window_size.y / s)


func _update() -> void:
	var root := get_tree().root
	var win: Vector2i = DisplayServer.window_get_size() if root.mode != Window.MODE_MINIMIZED else root.size
	if win.x <= 0 or win.y <= 0:
		return
	scale_factor = compute_scale(win)
	logical_size = compute_logical_size(win, scale_factor)
	root.content_scale_mode = Window.CONTENT_SCALE_MODE_VIEWPORT
	root.content_scale_aspect = Window.CONTENT_SCALE_ASPECT_EXPAND
	root.content_scale_stretch = Window.CONTENT_SCALE_STRETCH_INTEGER
	root.content_scale_size = logical_size
	_update_safe_area(win)
	layout_changed.emit()


func _update_safe_area(win: Vector2i) -> void:
	var safe: Rect2i = forced_safe_area
	if safe.size == Vector2i.ZERO:
		safe = DisplayServer.get_display_safe_area()
		var screen_size := DisplayServer.screen_get_size()
		if safe.size == Vector2i.ZERO or screen_size != win:
			safe = Rect2i(Vector2i.ZERO, win)
	var left := ceilf(safe.position.x / float(scale_factor))
	var top := ceilf(safe.position.y / float(scale_factor))
	var right := ceilf((win.x - safe.end.x) / float(scale_factor))
	var bottom := ceilf((win.y - safe.end.y) / float(scale_factor))
	safe_margins = Rect2(left, top, maxf(right, 0), maxf(bottom, 0))


func set_forced_safe_area(rect: Rect2i) -> void:
	forced_safe_area = rect
	_update()


func safe_rect() -> Rect2:
	var size := Vector2(logical_size)
	return Rect2(safe_margins.position, size - safe_margins.position - safe_margins.size)
