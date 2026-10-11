class_name DebugScenarios
extends RefCounted


static func run(name: String) -> void:
	match name:
		"test":
			Router.set_screen(load("res://src/core/test_pattern.gd").new(), "test")
		_:
			push_error("unknown scenario " + name)
