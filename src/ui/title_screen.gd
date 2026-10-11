extends Control

const PARADE := [1, 4, 6, 8, 10, 13, 15, 17, 18, 20, 21, 24]
const NAMES := ["ALEX", "SAM", "NOA", "DANI", "ARI", "CRIS"]

var logo: Label
var subtitle: Label
var press: Label
var hero: TextureRect
var parade: Array[TextureRect] = []
var waiting_start := true
var t := 0.0
var bg: ColorRect
var stripe: ColorRect


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	bg = ColorRect.new()
	bg.color = Color("#f8e8a0")
	add_child(bg)
	stripe = ColorRect.new()
	stripe.color = Color("#e8c050")
	add_child(stripe)
	logo = UiStyle.label("POLITICMON")
	logo.scale = Vector2(3, 3)
	add_child(logo)
	subtitle = UiStyle.label("Edición Escaño · Sátira ficticia", Vector2.ZERO, UiStyle.TEXT_LIGHT)
	add_child(subtitle)
	hero = TextureRect.new()
	hero.texture = UiStyle.tex(GameData.sprite_path(25, "front"))
	hero.scale = Vector2(2, 2)
	add_child(hero)
	for id in PARADE:
		var r := TextureRect.new()
		r.texture = UiStyle.tex(GameData.sprite_path(id, "icon"))
		add_child(r)
		parade.append(r)
	press = UiStyle.label("PULSA A PARA EMPEZAR")
	add_child(press)
	var credit := UiStyle.label("Personajes y partidos 100% ficticios", Vector2.ZERO, UiStyle.TEXT_LIGHT)
	credit.name = "Credit"
	add_child(credit)
	ScreenScaler.layout_changed.connect(_layout)
	_layout()
	Audio.play_music("title")
	Ui.set_fade(0.0)


func _layout() -> void:
	var full := Vector2(ScreenScaler.logical_size)
	var safe := ScreenScaler.safe_rect()
	size = full
	bg.size = full
	var logo_w := UiStyle.text_width("POLITICMON") * 3
	logo.position = Vector2(floorf((full.x - logo_w) / 2.0), safe.position.y + 24)
	subtitle.position = Vector2(floorf((full.x - UiStyle.text_width(subtitle.text)) / 2.0), logo.position.y + 30)
	var hero_y := subtitle.position.y + 16 + maxf(0.0, (safe.size.y - 300) * 0.25)
	hero.position = Vector2(floorf(full.x / 2.0 - 56), hero_y)
	stripe.position = Vector2(0, hero_y + 96)
	stripe.size = Vector2(full.x, 22)
	press.position = Vector2(floorf((full.x - UiStyle.text_width(press.text)) / 2.0), hero_y + 136)
	var credit: Label = get_node("Credit")
	credit.position = Vector2(floorf((full.x - UiStyle.text_width(credit.text)) / 2.0), safe.end.y - 14)


func _process(delta: float) -> void:
	t += delta
	press.visible = waiting_start and fmod(t, 1.0) < 0.65
	var full_w := float(ScreenScaler.logical_size.x)
	var span := PARADE.size() * 24.0
	for i in range(parade.size()):
		var x := fmod(t * 20.0 + i * 24.0, span) - 24.0
		if x > full_w:
			x -= span
		parade[i].position = Vector2(roundf(x), stripe.position.y + 3 + (2 if int(t * 4 + i) % 2 == 0 else 0))


func _input(event: InputEvent) -> void:
	if not waiting_start or Ui.is_busy():
		return
	if event.is_action_pressed("a") or event.is_action_pressed("start") or (event is InputEventMouseButton and event.pressed):
		get_viewport().set_input_as_handled()
		waiting_start = false
		Audio.sfx("select")
		_main_menu()


func _main_menu() -> void:
	while true:
		var opts: Array = []
		var has_save := SaveSystem.has_save()
		if has_save:
			opts.append("CONTINUAR")
		opts.append("NUEVA PARTIDA")
		opts.append("CRÉDITOS")
		var i: int = await Ui.choose(opts, {"mode": "center", "point": Vector2(Ui.area().x / 2, press.position.y - ScreenScaler.safe_rect().position.y + 4), "min_width": 90})
		if i < 0:
			waiting_start = true
			return
		var choice: String = opts[i]
		match choice:
			"CONTINUAR":
				if SaveSystem.load_game():
					await Ui.fade_out(0.4)
					Router.start_overworld(GameState.map_id, GameState.pos, GameState.facing)
					return
				await Ui.say("La partida guardada está dañada. Culpa de las cloacas.")
			"NUEVA PARTIDA":
				if has_save and not await Ui.yes_no("Ya hay una partida guardada. Si guardas la nueva, se sobrescribirá. ¿Seguir?"):
					continue
				var name: String = await _choose_name()
				if name == "":
					continue
				GameState.reset_new_game(name)
				await Ui.fade_out(0.5)
				Router.start_overworld("madrid_garaje", Vector2i(4, 5), "up")
				return
			"CRÉDITOS":
				await Ui.say_all([
					"POLITICMON. Juego, código, gráficos, fuente, música y sonido generados para esta versión.",
					"Motor: Godot 4. Todos los personajes y partidos son ficticios: cualquier parecido es pura casualidad electoral.",
				])


func _choose_name() -> String:
	await Ui.say("¿Cómo se llama quien funda el nuevo partido?", false)
	var opts: Array = NAMES.duplicate()
	opts.append("ESCRIBIR...")
	var i: int = await Ui.choose(opts, {"mode": "bottomright"})
	Ui.close_dialog()
	if i < 0:
		return ""
	if i < NAMES.size():
		return NAMES[i]
	var entry := NameEntry.new()
	var r: String = await Ui.run_modal(entry)
	return r


class NameEntry:
	extends Modal
	signal finished(r: String)
	var edit: LineEdit

	func _ready() -> void:
		var area: Vector2 = Ui.area()
		size = area
		consume_input = false
		var p := UiStyle.panel(Rect2(8, 40, area.x - 16, 56))
		add_child(p)
		add_child(UiStyle.label("Escribe tu nombre (máx. 8):", Vector2(16, 48)))
		edit = LineEdit.new()
		edit.position = Vector2(16, 62)
		edit.size = Vector2(area.x - 32, 16)
		edit.max_length = 8
		edit.add_theme_font_override("font", UiStyle.FONT)
		edit.add_theme_font_size_override("font_size", 8)
		edit.add_theme_color_override("font_color", UiStyle.TEXT)
		var sb := StyleBoxFlat.new()
		sb.bg_color = Color("#e8f0e8")
		sb.content_margin_left = 3
		edit.add_theme_stylebox_override("normal", sb)
		edit.add_theme_stylebox_override("focus", sb)
		add_child(edit)
		add_child(UiStyle.label("A / Intro: aceptar", Vector2(16, 82), UiStyle.TEXT_LIGHT))
		edit.text_submitted.connect(func(_t): _accept())
		edit.grab_focus.call_deferred()
		if DisplayServer.has_feature(DisplayServer.FEATURE_VIRTUAL_KEYBOARD):
			DisplayServer.virtual_keyboard_show("")

	func _accept() -> void:
		var n := edit.text.strip_edges().to_upper()
		if n == "":
			n = "ALEX"
		if DisplayServer.has_feature(DisplayServer.FEATURE_VIRTUAL_KEYBOARD):
			DisplayServer.virtual_keyboard_hide()
		finished.emit(n)
