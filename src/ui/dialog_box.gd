class_name DialogBox
extends Modal

signal advanced

const CHARS_PER_SEC := 60.0
const PAD_X := 8
const PAD_Y := 7

var lines_per_page := 3
var frame: NinePatchRect
var text_label: Label
var arrow: TextureRect
var typing := false
var waiting := false
var _shown := 0.0
var _blink := 0.0


func _ready() -> void:
	frame = UiStyle.panel(Rect2(Vector2.ZERO, size))
	add_child(frame)
	text_label = UiStyle.label("", Vector2(PAD_X, PAD_Y))
	add_child(text_label)
	arrow = TextureRect.new()
	arrow.texture = UiStyle.ARROW_DOWN
	arrow.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(arrow)
	arrow.hide()


func box_height() -> int:
	return lines_per_page * UiStyle.LINE_H + PAD_Y * 2 - 2


func layout(rect: Rect2) -> void:
	position = rect.position
	size = rect.size
	if frame:
		frame.size = size
		text_label.size = Vector2(size.x - PAD_X * 2, size.y - PAD_Y)
		arrow.position = Vector2(size.x - 12, size.y - 9)


func text_width() -> float:
	return size.x - PAD_X * 2


func paginate(text: String) -> Array:
	var lines := UiStyle.wrap(text, text_width())
	var pages: Array = []
	var i := 0
	while i < lines.size():
		pages.append("\n".join(lines.slice(i, i + lines_per_page)))
		i += lines_per_page
	return pages


func play(text: String, wait_last: bool = true) -> void:
	var pages := paginate(text)
	for p in range(pages.size()):
		await _type_page(pages[p])
		if p < pages.size() - 1 or wait_last:
			await _wait_advance()


func _type_page(page: String) -> void:
	text_label.text = page
	text_label.visible_characters = 0
	_shown = 0.0
	typing = true
	arrow.hide()
	while typing:
		await get_tree().process_frame
		_shown += get_process_delta_time() * CHARS_PER_SEC
		text_label.visible_characters = int(_shown)
		if text_label.visible_characters >= page.length():
			typing = false
	text_label.visible_characters = -1


func _wait_advance() -> void:
	waiting = true
	opened_at = Time.get_ticks_msec()
	await advanced
	waiting = false
	arrow.hide()
	Audio.sfx("select")


func _process(delta: float) -> void:
	if waiting:
		_blink += delta
		arrow.visible = fmod(_blink, 0.8) < 0.5


func handle_input(event: InputEvent) -> void:
	if not accepts_input():
		return
	var go := pressed(event, "a") or pressed(event, "b") or is_tap(event)
	if not go:
		return
	if typing:
		typing = false
	elif waiting:
		advanced.emit()
