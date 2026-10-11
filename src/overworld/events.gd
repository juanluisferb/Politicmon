class_name OverworldEvents
extends RefCounted

const STARTERS := [1, 10, 15]
const LEADER_GIFTS := {"leader_madrid": ["carnet_premium", 3], "leader_andalucia": ["puesto_lista", 2], "leader_galicia": ["puesto_lista", 3]}
const LEGEND_LEVEL := 36

var ow: Overworld


func _init(overworld: Overworld) -> void:
	ow = overworld


func on_map_loaded() -> void:
	if ow.map_id == "madrid_garaje" and not GameState.flag("has_starter"):
		ow.busy = true
		await starter_intro()
		ow.busy = false


# ------------------------------------------------------------------ talking
func talk_to(a: Actor) -> void:
	var d: Dictionary = a.data
	if d.get("event", "") != "heal" and d.get("event", "") != "shop":
		a.face(Actor.opposite(ow.player.facing))
	if d.has("trainer"):
		if GameState.trainer_defeated(d["trainer"]):
			var t := GameData.trainer(d["trainer"])
			await Ui.say("%s: «%s»" % [t["name"], t.get("after", "...")])
		else:
			await trainer_battle(a)
		return
	match d.get("event", "talk"):
		"starter":
			if GameState.flag("has_starter"):
				await Ui.say_all(["Profesora Urna: «Recuerda: gana los tres Escaños (Madrid, Andalucía y Galicia) y el país será tuyo.»",
					"«Si un afiliado tiene poco Carisma o está en un Escándalo, es más fácil que se una al partido.»"])
			else:
				await starter_intro()
		"heal":
			await heal()
		"shop":
			await shop(d.get("stock", []))
		"legend":
			await legend(a)
		_:
			for line in d.get("text", ["..."]):
				await Ui.say(line)


# ------------------------------------------------------------------ starter
func starter_intro() -> void:
	var mentor: Actor = ow.npcs.get("mentor")
	var n := GameState.player_name
	await Ui.say_all([
		"Profesora Urna: «¡%s! ¡Por fin despiertas! Bienvenida, bienvenido al Partido Nuevo.»" % n,
		"«Lo fundaste anoche en este garaje, entre dos cajas de folletos y una urna de cartón.»",
		"«Un partido sin afiliados es solo un logo. Por suerte, tengo tres fichas de afiliación sobre la mesa.»",
	])
	while true:
		var opts: Array = []
		for id in STARTERS:
			var s := GameData.species(id)
			opts.append([s["name"], GameData.type_name(s["types"][0])])
		var preview := TextureRect.new()
		preview.position = Vector2(4, 4)
		var frame := UiStyle.panel(Rect2(0, 0, 64, 64))
		var holder := Control.new()
		holder.add_child(frame)
		holder.add_child(preview)
		holder.position = Vector2(4, 8)
		Ui.root.add_child(holder)
		var hover := func(i: int) -> void:
			preview.texture = UiStyle.tex(GameData.sprite_path(STARTERS[i], "front"))
		await Ui.say("«¿Con quién quieres empezar tu carrera política?»", false)
		var i: int = await Ui.choose(opts, {"cancel": false, "on_hover": hover, "mode": "bottomright"})
		holder.queue_free()
		Ui.close_dialog()
		var sid: int = STARTERS[i]
		var sp := GameData.species(sid)
		await Ui.say(sp["desc"])
		if await Ui.yes_no("¿Afilias a %s, de tipo %s?" % [sp["name"], GameData.type_name(sp["types"][0])]):
			var p := Politician.create(sid, 5)
			GameState.add_politician(p)
			GameState.set_flag("has_starter")
			GameState.set_flag("starter_%d" % sid)
			Audio.sfx("capture")
			await Ui.say("¡%s se une al Partido Nuevo!" % sp["name"])
			break
	await Ui.say_all([
		"Profesora Urna: «¡Estupendo! Toma también estos 5 Carnets de afiliación y 3 Mítines para el camino.»",
		"«En la hierba alta hay políticos sin partido. Debilítalos en un debate y ofréceles un Carnet.»",
		"«Gana los Escaños de los tres Parlamentos: Madrid, Andalucía y Galicia. Empieza por la Asamblea, al norte de la Gran Vía.»",
		"«Si tus afiliados dimiten de cansancio, la Sede del Partido los pone a punto. ¡Suerte, %s!»" % GameState.player_name,
	])
	if mentor:
		mentor.face("down")


# ------------------------------------------------------------------ trainers
func check_trainers() -> bool:
	for id in ow.npcs:
		var a: Actor = ow.npcs[id]
		var d: Dictionary = a.data
		if not d.has("trainer") or int(d.get("sight", 0)) <= 0 or GameState.trainer_defeated(d["trainer"]):
			continue
		var step: Vector2i = Actor.DIRS[a.facing]
		for i in range(1, int(d["sight"]) + 1):
			var c: Vector2i = a.cell + step * i
			if c == ow.player.cell:
				await spotted(a)
				return true
			if ow.tile_prop(c, "solid") or ow.npc_at(c) != null:
				break
	return false


func spotted(a: Actor) -> void:
	ow.busy = true
	var ex := Sprite2D.new()
	ex.texture = load("res://assets/ui/exclaim.png")
	ex.centered = false
	ex.position = Vector2(3, -16)
	a.add_child(ex)
	Audio.play_music("encounter")
	Audio.sfx("exclaim")
	await ow.get_tree().create_timer(0.6).timeout
	ex.queue_free()
	while (a.cell - ow.player.cell).length_squared() > 1:
		await a.step_to(a.cell + Actor.DIRS[a.facing], 0.18)
	ow.player.face(Actor.opposite(a.facing))
	await trainer_battle(a)
	ow.busy = false


func trainer_battle(a: Actor) -> void:
	var tid: String = a.data["trainer"]
	var t := GameData.trainer(tid)
	var team: Array = []
	for e in t["team"]:
		team.append(Politician.create(int(e[0]), int(e[1])))
	var leader: bool = t.get("leader", false)
	if not leader:
		Audio.play_music("encounter")
	await Ui.say("%s: «%s»" % [t["name"], t.get("intro", "")])
	var res: Dictionary = await Router.start_battle({"enemies": team, "trainer_id": tid, "zone": ow.map.get("zone", ""), "music": "gym" if leader else "battle"})
	await after_battle(res)
	if res["outcome"] == "win":
		GameState.defeated_trainers[tid] = true
		if leader:
			await leader_reward(tid, t)


func leader_reward(tid: String, t: Dictionary) -> void:
	GameState.add_badge(t["badge"])
	Audio.play_music("badge")
	await Ui.say("¡%s te entrega el %s!" % [t["name"], t["badge_name"]])
	var gift: Array = LEADER_GIFTS.get(tid, [])
	if not gift.is_empty():
		GameState.add_item(gift[0], gift[1])
		await Ui.say("También recibes %d x %s." % [gift[1], GameData.item(gift[0])["name"]])
	await Ui.say("%s: «%s»" % [t["name"], t.get("after", "")])
	if GameState.badges.size() >= 3:
		await Ui.say("¡Tienes los tres Escaños! En Madrid, las puertas del Hemiciclo Nacional ya están abiertas...")
	Audio.play_music(ow.map.get("music", "madrid"))


# ------------------------------------------------------------------ wild
func wild_battle(enc: Dictionary) -> void:
	var table: Array = enc["table"]
	var total := 0
	for e in table:
		total += int(e[3])
	var r := GameState.rng.randi_range(1, total)
	var pick: Array = table[0]
	for e in table:
		r -= int(e[3])
		if r <= 0:
			pick = e
			break
	var lvl := GameState.rng.randi_range(int(pick[1]), int(pick[2]))
	var foe := Politician.create(int(pick[0]), lvl)
	if OS.is_debug_build() and OS.get_cmdline_user_args().size() > 0:
		print("wild encounter: %s L%d" % [foe.display_name(), lvl])
	var res: Dictionary = await Router.start_battle({"enemies": [foe], "zone": ow.map.get("zone", ""), "music": "battle"})
	if OS.is_debug_build() and OS.get_cmdline_user_args().size() > 0:
		print("  outcome: %s" % res.get("outcome", ""))
	await after_battle(res)


func after_battle(res: Dictionary) -> void:
	if res.get("outcome", "") == "lose":
		await respawn()
	else:
		Ui.set_fade(1.0)
		await Ui.fade_in(0.25)


func respawn() -> void:
	GameState.heal_party()
	var rs: Dictionary = GameState.respawn
	ow.load_map(rs["map"], Vector2i(int(rs["x"]), int(rs["y"])), "up")
	await Ui.fade_in(0.3)
	await Ui.say("Tus afiliados se recuperan en la Sede. «Una derrota es solo una encuesta con mala suerte.»")


# ------------------------------------------------------------------ places
func heal() -> void:
	await Ui.say("¡Bienvenida, bienvenido a la Sede del Partido! Aquí ponemos a punto a tus afiliados.")
	if await Ui.yes_no("¿Quieres que tus afiliados descansen?"):
		await Ui.fade_out(0.2)
		Audio.sfx("heal")
		GameState.heal_party()
		await ow.get_tree().create_timer(0.6).timeout
		await Ui.fade_in(0.2)
		GameState.respawn = {"map": ow.map_id, "x": 4, "y": 4}
		await Ui.say("¡Listos para el próximo mitin! Solo una foto para redes... ¡Patata! Vuelve cuando quieras.")
	else:
		await Ui.say("¡Vuelve cuando quieras!")


func shop(stock: Array) -> void:
	await Ui.say("¡Hola! Tenemos todo lo necesario para una campaña. ¿Qué necesitas?")
	while true:
		var opts: Array = []
		for id in stock:
			var it := GameData.item(id)
			opts.append([it["name"], "%d€" % int(it["price"])])
		var hover := func(i: int) -> void:
			Ui.set_dialog_text(GameData.item(stock[i]).get("desc", ""))
		var i: int = await Ui.choose(opts, {"title": "FONDOS %d€" % GameState.money, "on_hover": hover, "mode": "bottomright"})
		Ui.close_dialog()
		if i < 0:
			break
		var id: String = stock[i]
		var it := GameData.item(id)
		var price := int(it["price"])
		var max_n := mini(9, GameState.money / maxi(1, price))
		if max_n <= 0:
			Audio.sfx("error")
			await Ui.say("No te llegan los fondos. Prueba con un crowdfunding.")
			continue
		var qopts: Array = []
		for q in range(1, max_n + 1):
			qopts.append(["x%d" % q, "%d€" % (q * price)])
		await Ui.say("¿Cuántos %s quieres?" % it["name"], false)
		var q: int = await Ui.choose(qopts, {"max_rows": 5, "mode": "bottomright"})
		Ui.close_dialog()
		if q < 0:
			continue
		var n := q + 1
		GameState.money -= n * price
		GameState.add_item(id, n)
		Audio.sfx("buy")
		await Ui.say("¡Aquí tienes %d x %s! Gracias por tu donación." % [n, it["name"]])
	await Ui.say("¡Vuelve pronto! Y vota con cabeza.")


func pc() -> void:
	await Ui.say("Secretaría de Organización. Aquí esperan los afiliados que no caben en tu equipo.")
	while true:
		await Ui.say("¿Qué quieres hacer? (Reserva: %d)" % GameState.box.size(), false)
		var i: int = await Ui.choose(["SACAR AFILIADO", "DEJAR AFILIADO", "SALIR"])
		Ui.close_dialog()
		if i == 0:
			if GameState.box.is_empty():
				await Ui.say("No hay nadie en la reserva.")
				continue
			if GameState.party.size() >= GameState.PARTY_MAX:
				await Ui.say("Tu equipo está completo. Deja antes a alguien.")
				continue
			var opts: Array = []
			for p in GameState.box:
				opts.append([p.display_name(), "Nv%d" % p.level])
			var k: int = await Ui.choose(opts, {"max_rows": 6, "mode": "center", "point": Ui.area() / 2})
			if k >= 0:
				var p: Politician = GameState.box[k]
				GameState.box.remove_at(k)
				GameState.party.append(p)
				await Ui.say("%s vuelve a tu equipo." % p.display_name())
		elif i == 1:
			if GameState.party.size() <= 1:
				await Ui.say("¡No puedes quedarte sin afiliados en el equipo!")
				continue
			var screen := PartyScreen.new().setup(GameState.party, "¿A quién dejas en la reserva?")
			var k: int = await Ui.run_modal(screen)
			if k >= 0:
				var p: Politician = GameState.party[k]
				GameState.party.remove_at(k)
				GameState.box.append(p)
				await Ui.say("%s se queda en la Sede, repartiendo folletos." % p.display_name())
		else:
			break


func pickup(cell: Vector2i) -> void:
	var entry: Dictionary = ow.items[cell]
	var it: Dictionary = entry["data"]
	GameState.add_item(it["item"], int(it.get("count", 1)))
	GameState.set_flag("item:" + it["id"])
	entry["node"].queue_free()
	ow.items.erase(cell)
	Audio.sfx("item")
	await Ui.say("¡Has encontrado %d x %s!" % [int(it.get("count", 1)), GameData.item(it["item"])["name"]])


# ------------------------------------------------------------------ legend
func legend(a: Actor) -> void:
	await Ui.say_all([
		"Una figura imponente sonríe desde el estrado. La sonrisa no se mueve. Nunca se ha movido.",
		"El Eterno Candidato: «Llevo en campaña desde que se inventaron las urnas. ¿Vienes a votarme?»",
		"«¿No? Entonces... ¡DEBATE!»",
	])
	var foe := Politician.create(25, LEGEND_LEVEL)
	var res: Dictionary = await Router.start_battle({"enemies": [foe], "zone": "", "music": "legend"})
	match res.get("outcome", ""):
		"caught":
			await after_battle(res)
			GameState.set_flag("legend_done")
			GameState.set_flag("legend_caught")
			ow.remove_npc("eterno")
			await Ui.say("El Eterno Candidato se afilia al Partido Nuevo. Por primera vez, su sonrisa parece sincera.")
			await Router.show_victory()
		"win":
			await after_battle(res)
			GameState.set_flag("legend_done")
			ow.remove_npc("eterno")
			await Ui.say("El Eterno Candidato recoge sus carteles. «Volveré... en las próximas.» Y se marcha sonriendo.")
			await Router.show_victory()
		"lose":
			await after_battle(res)
		_:
			await after_battle(res)
			await Ui.say("El Eterno Candidato sigue ahí, sonriendo. Tiene todo el tiempo del mundo.")
