extends Node

var world: Node
var current: Node
var screen := ""


func attach(world_node: Node) -> void:
	world = world_node


func touch_allowed() -> bool:
	return screen == "overworld" or screen == "test"


func set_screen(node: Node, name: String) -> void:
	if current and is_instance_valid(current):
		current.queue_free()
	current = node
	screen = name
	world.add_child(node)
