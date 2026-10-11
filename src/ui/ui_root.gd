extends CanvasLayer

var root: Control
var dialog: DialogBox
var modals: Array = []
var busy_count := 0
var _fade_layer: CanvasLayer
var _fade: ColorRect
var _banner: Control


func _ready() -> void:
	layer = 20
	process_mode = Node.PROCESS_MODE_ALWAYS
	root = Control.new()
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)
	dialog = DialogBox.new()
	root.add_child(dialog)
	dialog.hide()
	_fade_layer = CanvasLayer.new()
	_fade_layer.layer = 100
	add_child(_fade_layer)
	_fade = ColorRect.new()
	_fade.color = Color(0, 0, 0, 0)
	_fade.mouse_filter = Control.MOUSE_FILTER_IGNORE
	_fade_layer.add_child(_fade)
	ScreenScaler.layout_changed.connect(_layout)
	_layout()


func _layout() -> void:
	var safe := ScreenScaler.safe_rect()
	root.position = safe.position
	root.size = safe.size
	_fade.position = Vector2.ZERO
	_fade.size = Vector2(ScreenScaler.logical_size)
	var h := dialog.box_height()
	dialog.layout(Rect2(0, root.size.y - h, root.size.x, h))


func area() -> Vector2:
	return root.size


func is_busy() -> bool:
	return not modals.is_empty() or dialog.visible or busy_count > 0


func push_modal(m: Modal) -> void:
	if m.get_parent() == null:
		root.add_child(m)
	m.opened_at = Time.get_ticks_msec()
	modals.append(m)


func pop_modal(m: Modal) -> void:
	modals.erase(m)


func _input(event: InputEvent) -> void:
	if modals.is_empty():
		return
	if event is InputEventScreenTouch or event is InputEventScreenDrag or event is InputEventMouseMotion:
		return
	var top: Modal = modals.back()
	if not top.consume_input:
		return
	get_viewport().set_input_as_handled()
	top.handle_input(event)


func say(text: String, wait_last: bool = true) -> void:
	dialog.show()
	dialog.move_to_front()
	push_modal(dialog)
	await dialog.play(text, wait_last)
	pop_modal(dialog)
	if wait_last:
		dialog.hide()


func set_dialog_text(text: String) -> void:
	dialog.show()
	dialog.text_label.text = "\n".join(UiStyle.wrap(text, dialog.text_width()))
	dialog.text_label.visible_characters = -1


func close_dialog() -> void:
	dialog.hide()


func say_all(lines: Array) -> void:
	for l in lines:
		await say(str(l))


func choose(options: Array, opts: Dictionary = {}) -> int:
	var m := MenuList.new()
	m.setup(options, opts.get("start", 0), opts.get("cancel", true))
	m.max_rows = opts.get("max_rows", 0)
	m.min_width = opts.get("min_width", 0)
	m.title = opts.get("title", "")
	m.enabled = opts.get("enabled", [])
	var mode: String = opts.get("mode", "bottomright")
	var point: Vector2 = opts.get("point", Vector2(root.size.x, root.size.y - (dialog.box_height() if dialog.visible else 0)))
	m.place(point, mode)
	if opts.has("on_hover"):
		m.hovered.connect(opts["on_hover"])
	push_modal(m)
	var result: int = await m.finished
	pop_modal(m)
	m.queue_free()
	return result


func ask(text: String, options: Array, opts: Dictionary = {}) -> int:
	await say(text, false)
	var r := await choose(options, opts)
	close_dialog()
	return r


func yes_no(text: String) -> bool:
	var r := await ask(text, ["SÍ", "NO"])
	return r == 0


func run_modal(m: Modal) -> Variant:
	push_modal(m)
	var result = await m.finished
	pop_modal(m)
	m.queue_free()
	return result


func fade_out(t: float = 0.25) -> void:
	busy_count += 1
	var tw := create_tween()
	tw.tween_property(_fade, "color:a", 1.0, t)
	await tw.finished
	busy_count -= 1


func fade_in(t: float = 0.25) -> void:
	busy_count += 1
	var tw := create_tween()
	tw.tween_property(_fade, "color:a", 0.0, t)
	await tw.finished
	busy_count -= 1


func set_fade(a: float, color: Color = Color.BLACK) -> void:
	_fade.color = Color(color.r, color.g, color.b, a)


func flash(color: Color = Color.WHITE, times: int = 2) -> void:
	for i in range(times):
		set_fade(1.0, color)
		await get_tree().create_timer(0.06).timeout
		set_fade(0.0, color)
		await get_tree().create_timer(0.06).timeout


func banner(text: String) -> void:
	if _banner:
		_banner.queue_free()
	var w := UiStyle.text_width(text) + 20
	var b := Control.new()
	b.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var p := UiStyle.panel(Rect2(0, 0, w, 24))
	b.add_child(p)
	b.add_child(UiStyle.label(text, Vector2(10, 8)))
	b.position = Vector2(4, -26)
	root.add_child(b)
	_banner = b
	var tw := b.create_tween()
	tw.tween_property(b, "position:y", 4.0, 0.2)
	tw.tween_interval(1.6)
	tw.tween_property(b, "position:y", -26.0, 0.2)
	tw.tween_callback(b.queue_free)
