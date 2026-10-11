extends Node

@onready var world: Node = $World


func _ready() -> void:
	Router.attach(world)
	var args := parse_args()
	if args.has("safe"):
		var p: PackedStringArray = args["safe"].split(",")
		var win := DisplayServer.window_get_size()
		ScreenScaler.set_forced_safe_area(Rect2i(int(p[0]), int(p[1]), win.x - int(p[0]) - int(p[2]), win.y - int(p[1]) - int(p[3])))
	if args.has("autopress"):
		_autopress(args["autopress"].split(","), float(args.get("autopress_rate", "0.05")))
	if args.has("scenario"):
		await DebugScenarios.run(args["scenario"])
	else:
		Router.start()
	if args.has("shot"):
		for i in range(int(args.get("frames", "20"))):
			await get_tree().process_frame
		var win := DisplayServer.window_get_size()
		var img := DisplayServer.screen_get_image_rect(Rect2i(DisplayServer.window_get_position(), win))
		if img == null or img.is_empty():
			img = get_viewport().get_texture().get_image()
			img.resize(win.x, win.y, Image.INTERPOLATE_NEAREST)
		img.save_png(args["shot"])
		print("screenshot saved: ", args["shot"], " logical=", ScreenScaler.logical_size, " scale=", ScreenScaler.scale_factor)
		get_tree().quit()


func _autopress(actions: PackedStringArray, rate: float) -> void:
	var i := 0
	while true:
		await get_tree().create_timer(rate).timeout
		var a := actions[i % actions.size()]
		var ev := InputEventAction.new()
		ev.action = a
		ev.pressed = true
		Input.parse_input_event(ev)
		await get_tree().process_frame
		var up := InputEventAction.new()
		up.action = a
		up.pressed = false
		Input.parse_input_event(up)
		i += 1


static func parse_args() -> Dictionary:
	var d := {}
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--") and "=" in a:
			var kv := a.substr(2).split("=", true, 1)
			d[kv[0]] = kv[1]
		elif a.begins_with("--"):
			d[a.substr(2)] = "1"
	return d
