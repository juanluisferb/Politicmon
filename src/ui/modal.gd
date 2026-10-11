class_name Modal
extends Control

var opened_at := 0
var consume_input := true


func _init() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	opened_at = Time.get_ticks_msec()


func accepts_input() -> bool:
	return Time.get_ticks_msec() - opened_at > 120


func handle_input(_event: InputEvent) -> void:
	pass


static func is_tap(event: InputEvent) -> bool:
	return event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and event.pressed


static func pressed(event: InputEvent, action: String) -> bool:
	return event.is_action_pressed(action, true)
