class_name SummaryScreen
extends Modal

signal finished(result: int)

const STAT_NAMES := {"carisma": "CARISMA", "oratoria": "ORATORIA", "aguante": "AGUANTE", "dialectica": "DIALÉCTICA", "templanza": "TEMPLANZA", "agilidad": "AGILIDAD"}

var mon: Politician
var scan := false


func setup(p: Politician, is_scan: bool = false) -> SummaryScreen:
	mon = p
	scan = is_scan
	return self


func _ready() -> void:
	var area: Vector2 = Ui.area()
	size = area
	var bg := ColorRect.new()
	bg.color = UiStyle.PAPER
	bg.size = area
	add_child(bg)
	var s := mon.species()
	var top := 4.0
	var frame := UiStyle.panel(Rect2(2, top, 64, 64))
	add_child(frame)
	var spr := TextureRect.new()
	spr.texture = UiStyle.tex(GameData.sprite_path(mon.species_id, "front"))
	spr.position = Vector2(6, top + 4)
	add_child(spr)
	add_child(UiStyle.label("Nº%03d" % mon.species_id, Vector2(70, top + 4), UiStyle.TEXT_LIGHT))
	add_child(UiStyle.label(mon.display_name(), Vector2(70, top + 14)))
	add_child(UiStyle.label("Nv%d" % mon.level, Vector2(70, top + 24)))
	var tx := 70.0
	for t in mon.types():
		tx = _type_tag(t, Vector2(tx, top + 36)) + 3
	var st: String = "Estado: " + {"escandalo": "Escándalo", "imputado": "Imputado"}.get(mon.status, "Dimisión" if mon.is_fainted() else "OK")
	add_child(UiStyle.label(st, Vector2(70, top + 48), UiStyle.TEXT_LIGHT))
	var y := top + 70
	add_child(UiStyle.panel(Rect2(2, y, area.x - 4, 74)))
	var i := 0
	for k in Politician.STATS:
		var val := "%d/%d" % [mon.hp, mon.max_hp()] if k == "carisma" else str(mon.stat(k))
		add_child(UiStyle.label(STAT_NAMES[k], Vector2(8, y + 6 + i * 10)))
		var l := UiStyle.label(val, Vector2(0, y + 6 + i * 10))
		l.size.x = area.x - 10
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		add_child(l)
		i += 1
	y += 76
	add_child(UiStyle.panel(Rect2(2, y, area.x - 4, 14 + 10 * maxi(1, mon.moves.size()))))
	for j in range(mon.moves.size()):
		var m: Dictionary = mon.moves[j]
		var mv := GameData.move(m["id"])
		add_child(UiStyle.label(mv.get("name", "?"), Vector2(8, y + 6 + j * 10)))
		var pp := UiStyle.label("%d/%d" % [m["pp"], mv.get("pp", 0)], Vector2(0, y + 6 + j * 10))
		pp.size.x = area.x - 10
		pp.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		add_child(pp)
	y += 16 + 10 * maxi(1, mon.moves.size())
	if not scan:
		add_child(UiStyle.label("EXP %d  Siguiente nivel: %d" % [mon.xp, mon.exp_to_next()], Vector2(6, y), UiStyle.TEXT_LIGHT))
		y += 12
		var lines := UiStyle.wrap(str(s.get("desc", "")), area.x - 12)
		var desc := UiStyle.label("\n".join(lines), Vector2(6, y))
		add_child(desc)
	else:
		add_child(UiStyle.label("Encuesta: margen de error ±3%", Vector2(6, y), UiStyle.TEXT_LIGHT))


func _type_tag(t: String, pos: Vector2) -> float:
	var name := GameData.type_name(t).to_upper()
	var w := UiStyle.text_width(name) + 6
	var r := ColorRect.new()
	r.color = GameData.type_color(t)
	r.position = pos
	r.size = Vector2(w, 10)
	add_child(r)
	var l := UiStyle.label(name, pos + Vector2(3, 1), Color.WHITE)
	add_child(l)
	return pos.x + w


func handle_input(event: InputEvent) -> void:
	if not accepts_input():
		return
	if pressed(event, "a") or pressed(event, "b") or is_tap(event):
		Audio.sfx("select")
		finished.emit(0)
