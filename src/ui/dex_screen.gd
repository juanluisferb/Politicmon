class_name DexScreen
extends Modal

signal finished(result: int)

const ROW_H := 12

var index := 0
var scroll := 0
var rows: Array[Label] = []
var icons: Array[TextureRect] = []
var cursor: TextureRect
var visible_rows := 10
var counter: Label
var preview: TextureRect
var detail: Control


func _ready() -> void:
	var area: Vector2 = Ui.area()
	size = area
	var bg := ColorRect.new()
	bg.color = UiStyle.PAPER
	bg.size = area
	add_child(bg)
	add_child(UiStyle.panel(Rect2(0, 0, area.x, 26)))
	add_child(UiStyle.label("POLITIDEX", Vector2(8, 9)))
	counter = UiStyle.label("", Vector2(0, 9))
	counter.size.x = area.x - 8
	counter.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	add_child(counter)
	var list_top := 28.0
	var preview_h := 72.0
	visible_rows = int((area.y - list_top - preview_h - 16) / ROW_H)
	add_child(UiStyle.panel(Rect2(0, list_top, area.x, visible_rows * ROW_H + 14)))
	for i in range(visible_rows):
		var ic := TextureRect.new()
		ic.position = Vector2(12, list_top + 6 + i * ROW_H - 3)
		ic.size = Vector2(16, 16)
		ic.scale = Vector2(0.75, 0.75)
		add_child(ic)
		icons.append(ic)
		var l := UiStyle.label("", Vector2(28, list_top + 7 + i * ROW_H))
		add_child(l)
		rows.append(l)
	cursor = TextureRect.new()
	cursor.texture = UiStyle.CURSOR
	add_child(cursor)
	var py := list_top + visible_rows * ROW_H + 16
	add_child(UiStyle.panel(Rect2(0, py, area.x, area.y - py)))
	preview = TextureRect.new()
	preview.position = Vector2(6, py + 6)
	add_child(preview)
	detail = Control.new()
	detail.position = Vector2(66, py + 8)
	add_child(detail)
	var seen := 0
	var caught := 0
	for id in range(1, GameData.species_count() + 1):
		if GameState.is_seen(id):
			seen += 1
		if GameState.is_caught(id):
			caught += 1
	counter.text = "Vistos %d  Afiliados %d" % [seen, caught]
	_refresh()


func _refresh() -> void:
	var total := GameData.species_count()
	if index < scroll:
		scroll = index
	if index >= scroll + visible_rows:
		scroll = index - visible_rows + 1
	for i in range(visible_rows):
		var id := scroll + i + 1
		if id > total:
			rows[i].text = ""
			icons[i].texture = null
			continue
		var s := GameData.species(id)
		var nm: String = s["name"] if GameState.is_seen(id) else "-----"
		rows[i].text = "%03d %s" % [id, nm]
		icons[i].texture = UiStyle.tex(GameData.sprite_path(id, "icon")) if GameState.is_caught(id) else null
	cursor.position = Vector2(4, rows[index - scroll].position.y + 1)
	var sid := index + 1
	for c in detail.get_children():
		c.queue_free()
	if GameState.is_seen(sid):
		var s := GameData.species(sid)
		preview.texture = UiStyle.tex(GameData.sprite_path(sid, "front"))
		preview.modulate = Color.WHITE if GameState.is_caught(sid) else Color(0.1, 0.1, 0.15)
		var types: Array = s["types"]
		detail.add_child(UiStyle.label(s["name"], Vector2.ZERO))
		detail.add_child(UiStyle.label("/".join(types.map(func(t): return GameData.type_name(t))), Vector2(0, 10), UiStyle.TEXT_LIGHT))
		var z: String = {"madrid": "Madrid", "andalucia": "Andalucía", "galicia": "Galicia"}.get(s.get("zone", ""), "???")
		detail.add_child(UiStyle.label("Zona: " + z, Vector2(0, 20), UiStyle.TEXT_LIGHT))
		detail.add_child(UiStyle.label("A: ver ficha" if GameState.is_caught(sid) else "Sin afiliar", Vector2(0, 34), UiStyle.TEXT_LIGHT))
	else:
		preview.texture = null
		detail.add_child(UiStyle.label("???", Vector2.ZERO))


func handle_input(event: InputEvent) -> void:
	if not accepts_input():
		return
	var total := GameData.species_count()
	if pressed(event, "up"):
		index = wrapi(index - 1, 0, total)
		Audio.sfx("cursor")
		_refresh()
	elif pressed(event, "down"):
		index = wrapi(index + 1, 0, total)
		Audio.sfx("cursor")
		_refresh()
	elif pressed(event, "left"):
		index = maxi(0, index - visible_rows)
		_refresh()
	elif pressed(event, "right"):
		index = mini(total - 1, index + visible_rows)
		_refresh()
	elif pressed(event, "a"):
		_open_entry()
	elif pressed(event, "b"):
		Audio.sfx("select")
		finished.emit(-1)
	elif is_tap(event):
		var local: Vector2 = event.position - global_position
		for i in range(visible_rows):
			if absf(local.y - rows[i].position.y - 4) <= ROW_H / 2.0 and scroll + i < total:
				if index == scroll + i:
					_open_entry()
				else:
					index = scroll + i
					_refresh()
				return
		if local.y < 26:
			finished.emit(-1)
		elif local.y > rows[visible_rows - 1].position.y + ROW_H:
			_open_entry()


func _open_entry() -> void:
	var sid := index + 1
	if not GameState.is_caught(sid):
		Audio.sfx("error")
		return
	Audio.sfx("select")
	var entry := DexEntry.new().setup(sid)
	Ui.run_modal(entry)


class DexEntry:
	extends Modal
	signal finished(r: int)
	var sid := 1

	func setup(id: int) -> DexEntry:
		sid = id
		return self

	func _ready() -> void:
		var area: Vector2 = Ui.area()
		size = area
		var bg := ColorRect.new()
		bg.color = UiStyle.PAPER
		bg.size = area
		add_child(bg)
		var s := GameData.species(sid)
		add_child(UiStyle.panel(Rect2(2, 4, 64, 64)))
		var spr := TextureRect.new()
		spr.texture = UiStyle.tex(GameData.sprite_path(sid, "front"))
		spr.position = Vector2(6, 8)
		add_child(spr)
		add_child(UiStyle.label("Nº%03d" % sid, Vector2(72, 8), UiStyle.TEXT_LIGHT))
		add_child(UiStyle.label(s["name"], Vector2(72, 20)))
		var types: Array = s["types"]
		add_child(UiStyle.label("/".join(types.map(func(t): return GameData.type_name(t))), Vector2(72, 32)))
		var back := TextureRect.new()
		back.texture = UiStyle.tex(GameData.sprite_path(sid, "back"))
		back.position = Vector2(area.x - 38, 36)
		add_child(back)
		var y := 74.0
		add_child(UiStyle.panel(Rect2(2, y, area.x - 4, 74)))
		var i := 0
		for k in Politician.STATS:
			add_child(UiStyle.label(SummaryScreen.STAT_NAMES[k], Vector2(8, y + 6 + i * 10)))
			var v: int = int(s["stats"][k])
			var bar := HpBar.new(int(area.x) - 100, true)
			bar.position = Vector2(76, y + 9 + i * 10)
			bar.set_ratio(v / 120.0)
			add_child(bar)
			var l := UiStyle.label(str(v), Vector2(0, y + 6 + i * 10))
			l.size.x = area.x - 10
			l.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
			add_child(l)
			i += 1
		y += 78
		var lines := UiStyle.wrap(str(s["desc"]), area.x - 16)
		add_child(UiStyle.panel(Rect2(2, y, area.x - 4, lines.size() * 10 + 14)))
		add_child(UiStyle.label("\n".join(lines), Vector2(8, y + 7)))
		Audio.sfx("cry")

	func handle_input(event: InputEvent) -> void:
		if accepts_input() and (pressed(event, "a") or pressed(event, "b") or is_tap(event)):
			Audio.sfx("select")
			finished.emit(0)
