class_name PartyScreen
extends Modal

signal finished(index: int)

const ROW_H := 30

var mons: Array = []
var index := 0
var prompt := "Elige un afiliado."
var allow_cancel := true
var rows_root: Control
var cursor: TextureRect
var prompt_label: Label
var swap_from := -1


func setup(list: Array, text: String = "Elige un afiliado.", cancel: bool = true, start: int = 0) -> PartyScreen:
	mons = list
	prompt = text
	allow_cancel = cancel
	index = clampi(start, 0, maxi(0, list.size() - 1))
	return self


func _ready() -> void:
	var area: Vector2 = Ui.area()
	size = area
	var bg := ColorRect.new()
	bg.color = UiStyle.PAPER
	bg.size = area
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(bg)
	rows_root = Control.new()
	rows_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(rows_root)
	_build_rows()
	cursor = TextureRect.new()
	cursor.texture = UiStyle.CURSOR
	add_child(cursor)
	add_child(UiStyle.panel(Rect2(0, area.y - 30, area.x, 30)))
	prompt_label = UiStyle.label(prompt, Vector2(8, area.y - 21))
	add_child(prompt_label)
	_refresh()


func _build_rows() -> void:
	for c in rows_root.get_children():
		c.queue_free()
	var area: Vector2 = Ui.area()
	for i in range(mons.size()):
		var p: Politician = mons[i]
		var row := Control.new()
		row.position = Vector2(0, 4 + i * ROW_H)
		row.size = Vector2(area.x, ROW_H)
		row.mouse_filter = Control.MOUSE_FILTER_IGNORE
		rows_root.add_child(row)
		row.add_child(UiStyle.panel(Rect2(2, 0, area.x - 4, ROW_H - 2)))
		var icon := TextureRect.new()
		icon.texture = UiStyle.tex(GameData.sprite_path(p.species_id, "icon"))
		icon.position = Vector2(14, 6)
		row.add_child(icon)
		row.add_child(UiStyle.label(p.display_name(), Vector2(34, 5)))
		var lv := UiStyle.label("Nv%d" % p.level, Vector2(0, 5))
		lv.size.x = area.x - 10
		lv.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		row.add_child(lv)
		var bar := HpBar.new(int(area.x) - 100)
		bar.position = Vector2(34, 18)
		bar.set_ratio(p.hp / float(p.max_hp()))
		row.add_child(bar)
		var hp := UiStyle.label("%d/%d" % [p.hp, p.max_hp()], Vector2(0, 15))
		hp.size.x = area.x - 10
		hp.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		row.add_child(hp)
		var tag := ""
		if p.is_fainted():
			tag = "DIM"
		elif p.status != "":
			tag = BattleHud.STATUS_TAG.get(p.status, "")
		if tag != "":
			var t := UiStyle.label(tag, Vector2(0, 5), HpBar.LOW)
			t.size.x = area.x - 34
			t.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			row.add_child(t)
		if i == swap_from:
			row.modulate = Color(0.8, 0.8, 1.0)


func reload(list: Array) -> void:
	mons = list
	_build_rows()
	_refresh()


func set_prompt(t: String) -> void:
	prompt = t
	if prompt_label:
		prompt_label.text = t


func _refresh() -> void:
	if mons.is_empty():
		cursor.hide()
		return
	cursor.position = Vector2(6, 4 + index * ROW_H + 10)


func handle_input(event: InputEvent) -> void:
	if not accepts_input():
		return
	if pressed(event, "up") and not mons.is_empty():
		index = wrapi(index - 1, 0, mons.size())
		Audio.sfx("cursor")
		_refresh()
	elif pressed(event, "down") and not mons.is_empty():
		index = wrapi(index + 1, 0, mons.size())
		Audio.sfx("cursor")
		_refresh()
	elif pressed(event, "a") and not mons.is_empty():
		Audio.sfx("select")
		finished.emit(index)
	elif pressed(event, "b") and allow_cancel:
		Audio.sfx("select")
		finished.emit(-1)
	elif is_tap(event):
		var local: Vector2 = event.position - global_position
		var i := int((local.y - 4) / ROW_H)
		if local.y >= 4 and i >= 0 and i < mons.size():
			index = i
			_refresh()
			Audio.sfx("select")
			finished.emit(i)
		elif allow_cancel and local.y > size.y - 30:
			finished.emit(-1)
