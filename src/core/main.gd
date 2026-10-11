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
	if args.has("inputs"):
		_play_inputs(args["inputs"].split(","), args.has("quit_after_inputs"))
	if args.has("quit_after"):
		_quit_after(float(args["quit_after"]))
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


func _play_inputs(tokens: PackedStringArray, quit_when_done: bool) -> void:
	await get_tree().create_timer(1.0).timeout
	for tok in tokens:
		var parts := tok.split(":")
		var action := parts[0]
		if action == "wait":
			await get_tree().create_timer(float(parts[1])).timeout
			continue
		var hold := 0.1 if parts.size() < 2 else float(parts[1]) * 0.21 + 0.1
		_send_action(action, true)
		await get_tree().create_timer(hold).timeout
		_send_action(action, false)
		await get_tree().create_timer(0.25).timeout
	print("ui_busy=%s modals=%d dialog=%s busy_count=%d ow_busy=%s" % [Ui.is_busy(), Ui.modals.size(), Ui.dialog.visible, Ui.busy_count, Router.overworld.busy if Router.overworld else "-"])
	print("inputs done: map=%s pos=%s party=%s flags=%s defeated=%s money=%d badges=%s" % [GameState.map_id, GameState.pos, GameState.party.map(func(p): return "%s L%d" % [p.display_name(), p.level]), GameState.flags.keys(), GameState.defeated_trainers.keys(), GameState.money, GameState.badges])
	if quit_when_done:
		get_tree().quit()


func _send_action(action: String, is_pressed: bool) -> void:
	var ev := InputEventAction.new()
	ev.action = action
	ev.pressed = is_pressed
	Input.parse_input_event(ev)


func _quit_after(t: float) -> void:
	await get_tree().create_timer(t).timeout
	print("quit_after: map=%s pos=%s party=%s" % [GameState.map_id, GameState.pos, GameState.party.map(func(p): return "%s L%d" % [p.display_name(), p.level])])
	get_tree().quit()


static func parse_args() -> Dictionary:
	var d := {}
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--") and "=" in a:
			var kv := a.substr(2).split("=", true, 1)
			d[kv[0]] = kv[1]
		elif a.begins_with("--"):
			d[a.substr(2)] = "1"
	return d
