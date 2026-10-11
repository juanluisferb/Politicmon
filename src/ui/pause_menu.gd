class_name PauseMenu
extends RefCounted


static func open(ow: Node) -> void:
	var last := 0
	while true:
		var opts := ["EQUIPO", "POLITIDEX", "OBJETOS", "FICHA", "GUARDAR", "OPCIONES", "SALIR"]
		var en := [not GameState.party.is_empty(), true, true, true, true, true, true]
		var i: int = await Ui.choose(opts, {"mode": "topright", "point": Vector2(Ui.area().x, 0), "start": last, "enabled": en, "min_width": 60})
		if i < 0 or i == 6:
			return
		last = i
		match i:
			0:
				await party_menu()
			1:
				await Ui.run_modal(DexScreen.new())
			2:
				await bag_menu()
			3:
				await Ui.run_modal(TrainerCard.new())
			4:
				if await Ui.yes_no("¿Quieres guardar la partida?"):
					if SaveSystem.save_game():
						Audio.sfx("save")
						await Ui.say("¡%s ha guardado la partida! Promesa cumplida." % GameState.player_name)
					else:
						await Ui.say("No se ha podido guardar. Culpa de la oposición.")
			5:
				await options_menu()


static func party_menu() -> void:
	var screen := PartyScreen.new().setup(GameState.party)
	Ui.push_modal(screen)
	while true:
		var i: int = await screen.finished
		if i < 0:
			break
		var p: Politician = GameState.party[i]
		var opts := ["FICHA", "MOVER", "CANCELAR"]
		var c: int = await Ui.choose(opts, {"mode": "bottomright", "point": Ui.area() - Vector2(0, 30)})
		if c == 0:
			await Ui.run_modal(SummaryScreen.new().setup(p))
		elif c == 1:
			screen.set_prompt("¿Con quién lo intercambias?")
			screen.swap_from = i
			screen.reload(GameState.party)
			var j: int = await screen.finished
			screen.swap_from = -1
			if j >= 0 and j != i:
				var tmp: Politician = GameState.party[i]
				GameState.party[i] = GameState.party[j]
				GameState.party[j] = tmp
			screen.set_prompt("Elige un afiliado.")
			screen.reload(GameState.party)
	Ui.pop_modal(screen)
	screen.queue_free()


static func bag_menu() -> void:
	while true:
		var ids: Array = []
		var opts: Array = []
		for id in GameData.items:
			if GameState.item_count(id) > 0:
				ids.append(id)
				opts.append([GameData.item(id)["name"], "x%d" % GameState.item_count(id)])
		var badge_line := "Escaños: %d/3   Fondos: %d€" % [GameState.badges.size(), GameState.money]
		if ids.is_empty():
			await Ui.say("La mochila está vacía. " + badge_line)
			return
		var hover := func(k: int) -> void:
			Ui.set_dialog_text(GameData.item(ids[k]).get("desc", ""))
		var i: int = await Ui.choose(opts, {"title": "OBJETOS", "on_hover": hover, "mode": "topright", "point": Vector2(Ui.area().x, 0), "max_rows": 8})
		Ui.close_dialog()
		if i < 0:
			return
		var id: String = ids[i]
		var it := GameData.item(id)
		if it.get("use", "") != "both":
			await Ui.say("%s solo se puede usar en un debate." % it["name"])
			continue
		var screen := PartyScreen.new().setup(GameState.party, "¿Con quién usas %s?" % it["name"])
		Ui.push_modal(screen)
		while true:
			var k: int = await screen.finished
			if k < 0:
				break
			var p: Politician = GameState.party[k]
			if not ItemEffects.can_use(id, p):
				Audio.sfx("error")
				screen.set_prompt("No tendría ningún efecto.")
				continue
			var msg := ItemEffects.apply(id, p)
			GameState.remove_item(id)
			Audio.sfx("heal")
			screen.reload(GameState.party)
			screen.set_prompt(msg)
			if GameState.item_count(id) <= 0:
				await Ui.get_tree().create_timer(0.8).timeout
				break
		Ui.pop_modal(screen)
		screen.queue_free()


static func options_menu() -> void:
	while true:
		var opts := [["SONIDO", "NO" if Audio.muted else "SÍ"], ["CONTROLES TÁCTILES", "SÍ" if TouchControls.enabled else "NO"], "VOLVER"]
		var i: int = await Ui.choose(opts, {"mode": "topright", "point": Vector2(Ui.area().x, 0)})
		match i:
			0:
				Audio.set_muted(not Audio.muted)
			1:
				TouchControls.enabled = not TouchControls.enabled
			_:
				return


class TrainerCard:
	extends Modal
	signal finished(r: int)

	func _ready() -> void:
		var area: Vector2 = Ui.area()
		size = area
		var bg := ColorRect.new()
		bg.color = UiStyle.PAPER
		bg.size = area
		add_child(bg)
		add_child(UiStyle.panel(Rect2(2, 4, area.x - 4, 150)))
		add_child(UiStyle.label("CARNET DE LÍDER", Vector2(10, 12), UiStyle.TEXT_LIGHT))
		add_child(UiStyle.label(GameState.player_name, Vector2(10, 26)))
		add_child(UiStyle.label("Partido Nuevo", Vector2(10, 38)))
		add_child(UiStyle.label("Fondos: %d€" % GameState.money, Vector2(10, 54)))
		add_child(UiStyle.label("Politidex: %d" % GameState.caught.size(), Vector2(10, 66)))
		add_child(UiStyle.label("Tiempo: %s" % GameState.time_string(), Vector2(10, 78)))
		var face := TextureRect.new()
		face.texture = UiStyle.tex("res://assets/sprites/trainers/player.png")
		face.position = Vector2(area.x - 62, 18)
		add_child(face)
		add_child(UiStyle.label("ESCAÑOS", Vector2(10, 100), UiStyle.TEXT_LIGHT))
		var x := 10.0
		for z in ["madrid", "andalucia", "galicia"]:
			var b := TextureRect.new()
			b.texture = UiStyle.tex("res://assets/sprites/items/escano_%s.png" % z)
			b.position = Vector2(x, 112)
			if not GameState.has_badge(z):
				b.modulate = Color(0, 0, 0, 0.25)
			add_child(b)
			add_child(UiStyle.label({"madrid": "MAD", "andalucia": "SUR", "galicia": "NOR"}[z], Vector2(x + 1, 130), UiStyle.TEXT_LIGHT))
			x += 30

	func handle_input(event: InputEvent) -> void:
		if accepts_input() and (pressed(event, "a") or pressed(event, "b") or is_tap(event)):
			finished.emit(0)
