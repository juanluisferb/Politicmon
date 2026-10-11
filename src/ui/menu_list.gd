class_name MenuList
extends Modal

signal finished(index: int)

const ROW_H := 12
const PAD := 8

var options: Array = []
var enabled: Array = []
var index := 0
var allow_cancel := true
var max_rows := 0
var scroll := 0
var min_width := 0
var frame: NinePatchRect
var rows: Array[Label] = []
var rights: Array[Label] = []
var cursor: TextureRect
var up_arrow: Label
var down_arrow: Label
var anchor_point := Vector2.ZERO
var anchor_mode := "topleft"
var title := ""
var _title_label: Label


func setup(opts: Array, start: int = 0, cancel: bool = true) -> MenuList:
	options = opts
	index = clampi(start, 0, maxi(0, opts.size() - 1))
	allow_cancel = cancel
	return self


func place(point: Vector2, mode: String = "topleft") -> MenuList:
	anchor_point = point
	anchor_mode = mode
	return self


func _ready() -> void:
	var visible_rows := options.size() if max_rows <= 0 else mini(max_rows, options.size())
	var w := float(min_width)
	for o in options:
		var left: String = o[0] if o is Array else str(o)
		var right: String = o[1] if o is Array and o.size() > 1 else ""
		w = maxf(w, UiStyle.text_width(left) + (UiStyle.text_width(right) + 10 if right != "" else 0.0))
	var title_h := 12 if title != "" else 0
	size = Vector2(ceilf(w) + PAD * 2 + 8, visible_rows * ROW_H + 14 + title_h)
	match anchor_mode:
		"topleft": position = anchor_point
		"topright": position = anchor_point - Vector2(size.x, 0)
		"bottomright": position = anchor_point - size
		"bottomleft": position = anchor_point - Vector2(0, size.y)
		"center": position = anchor_point - size / 2
	position = position.round()
	frame = UiStyle.panel(Rect2(Vector2.ZERO, size))
	add_child(frame)
	if title != "":
		_title_label = UiStyle.label(title, Vector2(PAD, 6), UiStyle.TEXT_LIGHT)
		add_child(_title_label)
	for i in range(visible_rows):
		var l := UiStyle.label("", Vector2(PAD + 8, 8 + title_h + i * ROW_H))
		add_child(l)
		rows.append(l)
		var r := UiStyle.label("", Vector2(0, 8 + title_h + i * ROW_H))
		r.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		r.size.x = size.x - PAD - 2
		add_child(r)
		rights.append(r)
	cursor = TextureRect.new()
	cursor.texture = UiStyle.CURSOR
	cursor.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(cursor)
	up_arrow = UiStyle.label("▲", Vector2(size.x / 2 - 3, 1))
	down_arrow = UiStyle.label("▼", Vector2(size.x / 2 - 3, size.y - 9))
	add_child(up_arrow)
	add_child(down_arrow)
	_refresh()


func _row_count() -> int:
	return rows.size()


func _refresh() -> void:
	if index < scroll:
		scroll = index
	if index >= scroll + _row_count():
		scroll = index - _row_count() + 1
	for i in range(_row_count()):
		var oi := scroll + i
		var o = options[oi] if oi < options.size() else ""
		rows[i].text = o[0] if o is Array else str(o)
		rights[i].text = o[1] if o is Array and o.size() > 1 else ""
		var en: bool = enabled.is_empty() or (oi < enabled.size() and enabled[oi])
		rows[i].add_theme_color_override("font_color", UiStyle.TEXT if en else UiStyle.TEXT_LIGHT)
	cursor.position = Vector2(PAD, rows[index - scroll].position.y + 1) if not rows.is_empty() else Vector2.ZERO
	up_arrow.visible = scroll > 0
	down_arrow.visible = scroll + _row_count() < options.size()
	selection_changed(index)


func selection_changed(_i: int) -> void:
	pass


func move(delta: int) -> void:
	if options.is_empty():
		return
	index = wrapi(index + delta, 0, options.size())
	Audio.sfx("cursor")
	_refresh()


func handle_input(event: InputEvent) -> void:
	if not accepts_input():
		return
	if pressed(event, "up"):
		move(-1)
	elif pressed(event, "down"):
		move(1)
	elif pressed(event, "left") and max_rows > 0:
		index = maxi(0, index - _row_count())
		_refresh()
	elif pressed(event, "right") and max_rows > 0:
		index = mini(options.size() - 1, index + _row_count())
		_refresh()
	elif pressed(event, "a"):
		_choose(index)
	elif pressed(event, "b") and allow_cancel:
		Audio.sfx("select")
		finished.emit(-1)
	elif is_tap(event):
		var local: Vector2 = event.position - global_position
		if Rect2(Vector2.ZERO, size).has_point(local):
			if down_arrow.visible and local.y > size.y - 10:
				move(1)
				return
			if up_arrow.visible and local.y < 8:
				move(-1)
				return
			for i in range(_row_count()):
				if local.y >= rows[i].position.y - 2 and local.y < rows[i].position.y + ROW_H - 2:
					if scroll + i < options.size():
						index = scroll + i
						_refresh()
						_choose(index)
					return
		elif allow_cancel:
			finished.emit(-1)


func _choose(i: int) -> void:
	if not enabled.is_empty() and i < enabled.size() and not enabled[i]:
		Audio.sfx("error")
		return
	Audio.sfx("select")
	finished.emit(i)
