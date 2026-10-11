extends Node

const KEYS := {
	"up": [KEY_UP, KEY_W],
	"down": [KEY_DOWN, KEY_S],
	"left": [KEY_LEFT, KEY_A],
	"right": [KEY_RIGHT, KEY_D],
	"a": [KEY_Z, KEY_ENTER, KEY_SPACE, KEY_J],
	"b": [KEY_X, KEY_BACKSPACE, KEY_K, KEY_SHIFT],
	"start": [KEY_ESCAPE, KEY_M, KEY_TAB],
}

const PADS := {
	"up": JOY_BUTTON_DPAD_UP,
	"down": JOY_BUTTON_DPAD_DOWN,
	"left": JOY_BUTTON_DPAD_LEFT,
	"right": JOY_BUTTON_DPAD_RIGHT,
	"a": JOY_BUTTON_A,
	"b": JOY_BUTTON_B,
	"start": JOY_BUTTON_START,
}


func _ready() -> void:
	for action in KEYS:
		if not InputMap.has_action(action):
			InputMap.add_action(action, 0.3)
		for key in KEYS[action]:
			var ev := InputEventKey.new()
			ev.physical_keycode = key
			InputMap.action_add_event(action, ev)
		var jb := InputEventJoypadButton.new()
		jb.button_index = PADS[action]
		InputMap.action_add_event(action, jb)
