class_name Actor
extends Node2D

const DIRS := {"down": Vector2i(0, 1), "up": Vector2i(0, -1), "left": Vector2i(-1, 0), "right": Vector2i(1, 0)}
const ROWS := {"down": 0, "up": 1, "left": 2, "right": 3}
const TILE := 16

var cell := Vector2i.ZERO
var facing := "down"
var moving := false
var sprite: Sprite2D
var data: Dictionary = {}
var step_parity := false


func setup(sheet_name: String, c: Vector2i, dir: String) -> Actor:
	sprite = Sprite2D.new()
	sprite.texture = load("res://assets/sprites/overworld/%s.png" % sheet_name)
	sprite.hframes = 2
	sprite.vframes = 4
	sprite.centered = false
	sprite.offset = Vector2(0, -4)
	add_child(sprite)
	cell = c
	position = Vector2(c * TILE)
	face(dir)
	return self


func face(dir: String) -> void:
	facing = dir
	_set_frame(false)


func _set_frame(walking: bool) -> void:
	if sprite == null:
		return
	var row: int = ROWS[facing]
	sprite.frame = row * 2 + (1 if walking else 0)
	sprite.flip_h = walking and step_parity and facing in ["down", "up"]


func front_cell() -> Vector2i:
	return cell + DIRS[facing]


static func dir_between(a: Vector2i, b: Vector2i) -> String:
	var d := b - a
	if absi(d.x) > absi(d.y):
		return "right" if d.x > 0 else "left"
	return "down" if d.y > 0 else "up"


static func opposite(dir: String) -> String:
	return {"down": "up", "up": "down", "left": "right", "right": "left"}[dir]


func step_to(target: Vector2i, duration: float) -> void:
	moving = true
	var from := position
	var to := Vector2(target * TILE)
	cell = target
	step_parity = not step_parity
	var t := 0.0
	_set_frame(true)
	while t < duration:
		await get_tree().process_frame
		t += get_process_delta_time()
		var k := clampf(t / duration, 0.0, 1.0)
		position = from.lerp(to, k).round()
		if k > 0.5:
			_set_frame(false)
	position = to
	_set_frame(false)
	moving = false


func hop_in_place() -> void:
	var tw := create_tween()
	tw.tween_property(sprite, "position:y", -3.0, 0.08)
	tw.tween_property(sprite, "position:y", 0.0, 0.08)
	await tw.finished
