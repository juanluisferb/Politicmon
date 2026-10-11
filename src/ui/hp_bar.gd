class_name HpBar
extends Control

const GOOD := Color("#40b848")
const MID := Color("#e8b800")
const LOW := Color("#e04030")
const EXP := Color("#4890e0")
const BACK := Color("#101820")
const EMPTY := Color("#f8f8f0")

var ratio := 1.0
var exp_bar := false


func _init(w: int = 48, is_exp: bool = false) -> void:
	custom_minimum_size = Vector2(w, 4 if not is_exp else 3)
	size = custom_minimum_size
	exp_bar = is_exp
	mouse_filter = Control.MOUSE_FILTER_IGNORE


func set_ratio(r: float) -> void:
	ratio = clampf(r, 0.0, 1.0)
	queue_redraw()


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), BACK)
	draw_rect(Rect2(Vector2(1, 1), size - Vector2(2, 2)), EMPTY)
	var w := floorf((size.x - 2) * ratio)
	if ratio > 0.0:
		w = maxf(w, 1.0)
	var col := EXP if exp_bar else (GOOD if ratio > 0.5 else (MID if ratio > 0.2 else LOW))
	draw_rect(Rect2(Vector2(1, 1), Vector2(w, size.y - 2)), col)
