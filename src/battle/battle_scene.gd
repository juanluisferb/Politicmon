class_name BattleScene
extends Control

signal finished(result: Dictionary)

const FAIL_LINES := [
	"¡Oh, no! ¡Ha roto el carnet en directo!",
	"¡Casi! Solo quería hacerse una foto contigo.",
	"¡Prefiere seguir siendo independiente!",
	"¡Ha pedido leer la letra pequeña primero!",
]
const STAT_LABELS := {"oratoria": "Oratoria", "aguante": "Aguante", "dialectica": "Dialéctica", "templanza": "Templanza", "agilidad": "Agilidad"}

var cfg: Dictionary = {}
var is_trainer := false
var trainer: Dictionary = {}
var trainer_id := ""
var enemy_team: Array = []
var enemy_idx := 0
var enemy: Politician
var player: Politician
var zone := ""
var rng: RandomNumberGenerator
var run_attempts := 0
var participants: Array = []
var evolutions: Array = []
var outcome := ""
var home_announced: Dictionary = {}

var bg_top: ColorRect
var bg_bottom: ColorRect
var enemy_sprite: TextureRect
var player_sprite: TextureRect
var enemy_plat: TextureRect
var player_plat: TextureRect
var trainer_sprite: TextureRect
var carnet_sprite: TextureRect
var enemy_hud: BattleHud
var player_hud: BattleHud
var enemy_home := Vector2.ZERO
var player_home := Vector2.ZERO


func setup(c: Dictionary) -> BattleScene:
	cfg = c
	trainer_id = c.get("trainer_id", "")
	is_trainer = trainer_id != ""
	trainer = GameData.trainer(trainer_id) if is_trainer else {}
	enemy_team = c.get("enemies", [])
	zone = c.get("zone", "")
	rng = c.get("rng", GameState.rng)
	return self


func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_build()
	ScreenScaler.layout_changed.connect(_layout)
	_layout()
	if not cfg.get("no_autostart", false):
		run.call_deferred()


func _build() -> void:
	var pal: Array = GameData.palettes.get("ui", {}).get("battle_bg", ["#f8f8f0", "#e0e8d8", "#a8b8a0", "#405040"])
	bg_top = ColorRect.new()
	bg_top.color = Color(pal[0])
	add_child(bg_top)
	bg_bottom = ColorRect.new()
	bg_bottom.color = Color(pal[1])
	add_child(bg_bottom)
	var plat_tex: Texture2D = UiStyle.tex("res://assets/ui/platform.png")
	enemy_plat = _tex_rect(plat_tex, Vector2(64, 12))
	player_plat = _tex_rect(plat_tex, Vector2(96, 18))
	enemy_sprite = _tex_rect(null, Vector2(56, 56))
	player_sprite = _tex_rect(null, Vector2(64, 64))
	trainer_sprite = _tex_rect(null, Vector2(56, 56))
	trainer_sprite.hide()
	carnet_sprite = _tex_rect(null, Vector2(16, 16))
	carnet_sprite.hide()
	enemy_hud = BattleHud.new(false)
	add_child(enemy_hud)
	player_hud = BattleHud.new(true)
	add_child(player_hud)
	enemy_hud.hide()
	player_hud.hide()


func _tex_rect(t: Texture2D, s: Vector2) -> TextureRect:
	var r := TextureRect.new()
	r.texture = t
	r.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	r.stretch_mode = TextureRect.STRETCH_SCALE
	r.size = s
	r.pivot_offset = Vector2(s.x / 2, s.y)
	r.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(r)
	return r


func _layout() -> void:
	var full := Vector2(ScreenScaler.logical_size)
	var safe := ScreenScaler.safe_rect()
	size = full
	var stage_bottom := safe.end.y - Ui.dialog.box_height()
	var stage_top := safe.position.y
	var avail := stage_bottom - stage_top
	var split := stage_top + avail * 0.5
	bg_top.size = Vector2(full.x, split)
	bg_bottom.position = Vector2(0, split)
	bg_bottom.size = Vector2(full.x, full.y - split)
	var ey := stage_top + clampf(avail * 0.2, 14.0, 80.0)
	enemy_home = Vector2(safe.end.x - 56 - 8, ey).round()
	enemy_sprite.position = enemy_home
	enemy_plat.position = enemy_home + Vector2(-4, 48)
	trainer_sprite.position = enemy_home
	enemy_hud.position = Vector2(safe.position.x + 4, ey + 4).round()
	player_home = Vector2(safe.position.x + 8, stage_bottom - 64).round()
	player_sprite.position = player_home
	player_plat.position = Vector2(safe.position.x - 8, stage_bottom - 14).round()
	player_hud.position = Vector2(safe.end.x - player_hud.size.x - 4, stage_bottom - player_hud.size.y - 8).round()
	move_child(player_plat, 2)
	move_child(enemy_plat, 2)


func say(t: String) -> void:
	await Ui.say(t)


func enemy_label(p: Politician = null) -> String:
	var m := p if p else enemy
	return m.display_name() + (" rival" if is_trainer else " salvaje")


func run() -> void:
	Audio.play_music(cfg.get("music", "battle"))
	for p: Politician in GameState.party:
		p.reset_battle_state()
	await _intro()
	while outcome == "":
		var action: Dictionary = await choose_action()
		await run_turn(action)
	await _end()


func _intro() -> void:
	await Ui.flash(Color.WHITE, 3)
	var full_w := float(ScreenScaler.logical_size.x)
	player_sprite.texture = UiStyle.tex("res://assets/sprites/trainers/player_back.png")
	player_sprite.position = player_home + Vector2(full_w, 0)
	if is_trainer:
		trainer_sprite.texture = UiStyle.tex("res://assets/sprites/trainers/%s.png" % trainer.get("sprite", "rival_ppi"))
		trainer_sprite.show()
		trainer_sprite.position = enemy_home - Vector2(full_w, 0)
		enemy_sprite.hide()
	else:
		enemy = enemy_team[0]
		enemy_sprite.texture = UiStyle.tex(GameData.sprite_path(enemy.species_id, "front"))
		enemy_sprite.position = enemy_home - Vector2(full_w, 0)
	var tw := create_tween().set_parallel(true)
	tw.tween_property(player_sprite, "position", player_home, 0.6)
	tw.tween_property(trainer_sprite if is_trainer else enemy_sprite, "position", enemy_home, 0.6)
	await tw.finished
	if is_trainer:
		var party_name: String = GameData.parties.get(trainer.get("party", ""), {}).get("name", "")
		if party_name != "":
			await say("¡%s, del %s, te reta a un debate!" % [trainer.get("name", "Rival"), party_name])
		else:
			await say("¡%s te reta a un debate!" % trainer.get("name", "Rival"))
		var tw2 := create_tween()
		tw2.tween_property(trainer_sprite, "position", enemy_home + Vector2(full_w, 0), 0.35)
		await tw2.finished
		trainer_sprite.hide()
		await _send_enemy(0)
	else:
		GameState.mark_seen(enemy.species_id)
		enemy_hud.bind(enemy)
		enemy_hud.show()
		Audio.sfx("cry")
		await say("¡Un %s salvaje te corta el paso!" % enemy.display_name())
		await _announce_home(enemy)
	var first := GameState.first_able_index()
	var tw3 := create_tween()
	tw3.tween_property(player_sprite, "position", player_home - Vector2(full_w, 0), 0.3)
	await tw3.finished
	await _send_player(first, true)


func _send_enemy(i: int) -> void:
	enemy_idx = i
	enemy = enemy_team[i]
	participants = []
	GameState.mark_seen(enemy.species_id)
	enemy_sprite.texture = UiStyle.tex(GameData.sprite_path(enemy.species_id, "front"))
	enemy_sprite.position = enemy_home
	enemy_sprite.scale = Vector2.ZERO
	enemy_sprite.show()
	await say("%s saca a %s." % [trainer.get("name", ""), enemy.display_name()])
	Audio.sfx("cry")
	var tw := create_tween()
	tw.tween_property(enemy_sprite, "scale", Vector2.ONE, 0.25)
	await tw.finished
	enemy_hud.bind(enemy)
	enemy_hud.show()
	await _announce_home(enemy)


func _send_player(i: int, first_time: bool = false) -> void:
	player = GameState.party[i]
	player.reset_battle_state()
	if not player in participants:
		participants.append(player)
	player_sprite.texture = UiStyle.tex(GameData.sprite_path(player.species_id, "back"))
	player_sprite.position = player_home
	player_sprite.scale = Vector2.ZERO
	player_sprite.show()
	Audio.sfx("cry")
	var tw := create_tween()
	tw.tween_property(player_sprite, "scale", Vector2.ONE, 0.25)
	await tw.finished
	player_hud.bind(player)
	player_hud.show()
	await say("¡Adelante, %s!" % player.display_name())
	await _announce_home(player)


func _announce_home(p: Politician) -> void:
	if BattleLogic.is_home(p, zone) and not home_announced.has(p):
		home_announced[p] = true
		await say("¡%s juega en casa! Su Hecho Diferencial le da ventaja." % p.display_name())


# ------------------------------------------------------------------ choosing
func choose_action() -> Dictionary:
	while true:
		await Ui.say("¿Qué hará %s?" % player.display_name(), false)
		var i: int = await Ui.choose(["LUCHAR", "EQUIPO", "OBJETOS", "HUIR"], {"cancel": false, "min_width": 50})
		match i:
			0:
				if not player.has_usable_moves():
					Ui.close_dialog()
					return {"kind": "fight", "move": -1}
				var m: int = await choose_move()
				if m >= 0:
					Ui.close_dialog()
					return {"kind": "fight", "move": m}
			1:
				Ui.close_dialog()
				var s: int = await choose_switch(false)
				if s >= 0:
					return {"kind": "switch", "to": s}
			2:
				Ui.close_dialog()
				var it: Dictionary = await choose_item()
				if not it.is_empty():
					return it
			3:
				Ui.close_dialog()
				return {"kind": "run"}
	return {}


func choose_move() -> int:
	var opts: Array = []
	var en: Array = []
	for m in player.moves:
		var mv := GameData.move(m["id"])
		opts.append([mv.get("name", "?"), "%d/%d" % [m["pp"], mv.get("pp", 0)]])
		en.append(int(m["pp"]) > 0)
	var hover := func(i: int) -> void:
		var mv := GameData.move(player.moves[i]["id"])
		var cat: String = {"fisico": "Físico", "especial": "Especial", "estado": "Estado"}.get(mv.get("category", ""), "")
		var pw := int(mv.get("power", 0))
		Ui.set_dialog_text("Tipo %s · %s%s" % [GameData.type_name(mv.get("type", "")), cat, (" · Pot %d" % pw) if pw > 0 else ""])
	var r: int = await Ui.choose(opts, {"enabled": en, "on_hover": hover, "mode": "bottomright"})
	return r


func choose_switch(forced: bool) -> int:
	var start := 0
	for j in range(GameState.party.size()):
		if not GameState.party[j].is_fainted() and GameState.party[j] != player:
			start = j
			break
	var screen := PartyScreen.new().setup(GameState.party, "¿A quién sacas?" if forced else "Elige un afiliado.", not forced, start)
	Ui.push_modal(screen)
	while true:
		var i: int = await screen.finished
		if i < 0:
			break
		var p: Politician = GameState.party[i]
		if p.is_fainted():
			screen.set_prompt("¡Ha dimitido! No puede salir.")
			Audio.sfx("error")
			continue
		if p == player and not player.is_fainted():
			screen.set_prompt("¡Ya está en el debate!")
			Audio.sfx("error")
			continue
		Ui.pop_modal(screen)
		screen.queue_free()
		return i
	Ui.pop_modal(screen)
	screen.queue_free()
	return -1


func choose_item() -> Dictionary:
	var ids: Array = []
	var opts: Array = []
	for id in GameData.items:
		var it := GameData.item(id)
		if GameState.item_count(id) > 0 and it.get("use", "") in ["battle", "both"]:
			ids.append(id)
			opts.append([it["name"], "x%d" % GameState.item_count(id)])
	if ids.is_empty():
		await say("No tienes objetos que usar en un debate.")
		return {}
	var hover := func(i: int) -> void:
		Ui.set_dialog_text(GameData.item(ids[i]).get("desc", ""))
	while true:
		var i: int = await Ui.choose(opts, {"on_hover": hover, "title": "OBJETOS"})
		Ui.close_dialog()
		if i < 0:
			return {}
		var id: String = ids[i]
		var it := GameData.item(id)
		match it.get("kind", ""):
			"carnet":
				if is_trainer:
					await say("¡No puedes afiliar a militantes de otro partido! Sería transfuguismo.")
					continue
				return {"kind": "carnet", "item": id}
			"scan":
				GameState.remove_item(id)
				await Ui.run_modal(SummaryScreen.new().setup(enemy, true))
				await say("La Encuesta revela los datos de %s." % enemy.display_name())
				continue
			"heal", "cure":
				var t: int = await _choose_item_target(id)
				if t >= 0:
					return {"kind": "use", "item": id, "target": t}
	return {}


func _choose_item_target(id: String) -> int:
	var it := GameData.item(id)
	var screen := PartyScreen.new().setup(GameState.party, "¿Con quién usas %s?" % it["name"])
	Ui.push_modal(screen)
	var result := -1
	while true:
		var i: int = await screen.finished
		if i < 0:
			break
		var p: Politician = GameState.party[i]
		if ItemEffects.can_use(id, p):
			result = i
			break
		screen.set_prompt("No tendría ningún efecto.")
		Audio.sfx("error")
	Ui.pop_modal(screen)
	screen.queue_free()
	return result


# ------------------------------------------------------------------ turn
func run_turn(action: Dictionary) -> void:
	var enemy_acts := true
	match action["kind"]:
		"run":
			if is_trainer or cfg.get("no_run", false):
				await say("¡No puedes abandonar un debate a medias!")
				return
			run_attempts += 1
			if rng.randf() < BattleLogic.run_chance(player, enemy, run_attempts - 1):
				Audio.sfx("run")
				await say("¡Has escapado sin hacer declaraciones!")
				outcome = "run"
				return
			await say("¡No has podido escapar!")
		"switch":
			await say("¡Vuelve, %s!" % player.display_name())
			await _withdraw_player()
			await _send_player(action["to"])
		"use":
			var target: Politician = GameState.party[action["target"]]
			var msg := ItemEffects.apply(action["item"], target)
			GameState.remove_item(action["item"])
			Audio.sfx("heal")
			if target == player:
				await player_hud.animate_hp(player.hp)
			await say(msg)
		"carnet":
			var caught: bool = await _throw_carnet(action["item"])
			if caught:
				outcome = "caught"
				return
		"fight":
			enemy_acts = false
			var emove := BattleLogic.choose_ai_move(enemy, player, rng, is_trainer)
			var player_first := BattleLogic.goes_first(player, enemy, rng)
			var order := [[player, enemy, action["move"]], [enemy, player, emove]]
			if not player_first:
				order.reverse()
			for step in order:
				var user: Politician = step[0]
				var target: Politician = step[1]
				if user.is_fainted() or target.is_fainted():
					continue
				await do_move(user, target, step[2])
				if await _check_faints():
					return
	if enemy_acts and outcome == "" and not enemy.is_fainted():
		var em := BattleLogic.choose_ai_move(enemy, player, rng, is_trainer)
		await do_move(enemy, player, em)
		if await _check_faints():
			return
	await _end_of_turn()


func _end_of_turn() -> void:
	for p: Politician in [player, enemy]:
		if p.is_fainted():
			continue
		var d := BattleLogic.escandalo_damage(p)
		if d > 0:
			p.hp = maxi(0, p.hp - d)
			Audio.sfx("hit")
			await _hud_for(p).animate_hp(p.hp)
			await say("¡El Escándalo pasa factura a %s!" % _name(p))
		if p.campaign_turns > 0:
			p.campaign_turns -= 1
			if p.campaign_turns == 0:
				_hud_for(p).refresh()
				await say("La campaña de %s ha terminado." % _name(p))
	await _check_faints()


func _hud_for(p: Politician) -> BattleHud:
	return player_hud if p == player else enemy_hud


func _sprite_for(p: Politician) -> TextureRect:
	return player_sprite if p == player else enemy_sprite


func _name(p: Politician) -> String:
	return p.display_name() if p == player else enemy_label(p)


func do_move(user: Politician, target: Politician, move_idx: int) -> void:
	if BattleLogic.imputado_blocks(user, rng):
		await say("¡%s está Imputado y no puede actuar!" % _name(user))
		return
	var mid := "balbuceo"
	if move_idx >= 0 and move_idx < user.moves.size():
		mid = user.moves[move_idx]["id"]
		user.moves[move_idx]["pp"] = maxi(0, int(user.moves[move_idx]["pp"]) - 1)
	elif user == player:
		await say("¡%s se ha quedado sin argumentos!" % _name(user))
	var mv := GameData.move(mid)
	await say("¡%s usa %s!" % [_name(user), mv.get("name", "?")])
	if not BattleLogic.accuracy_hits(mid, rng):
		await say("¡Pero ha fallado!")
		return
	var eff: Dictionary = mv.get("effect", {})
	var damage := 0
	if int(mv.get("power", 0)) > 0:
		var r := BattleLogic.calc_damage(user, target, mid, zone, rng)
		damage = r["damage"]
		await _attack_anim(user, target)
		target.hp = maxi(0, target.hp - damage)
		await _hud_for(target).animate_hp(target.hp)
		if r["crit"]:
			await say("¡Un golpe crítico!")
		if r["effectiveness"] > 1.0:
			await say("¡Es muy eficaz!")
		elif r["effectiveness"] < 1.0:
			await say("No es muy eficaz...")
		match eff.get("kind", ""):
			"drain":
				var heal := maxi(1, int(damage * float(eff.get("fraction", 0.5))))
				user.hp = mini(user.max_hp(), user.hp + heal)
				await _hud_for(user).animate_hp(user.hp)
				await say("¡%s recupera Carisma!" % _name(user))
			"recoil":
				user.hp = maxi(0, user.hp - maxi(1, int(damage * float(eff.get("fraction", 0.25)))))
				await _hud_for(user).animate_hp(user.hp)
				await say("¡%s se resiente del esfuerzo!" % _name(user))
	else:
		Audio.sfx("status")
		await _status_anim(user if eff.get("target", "self") == "self" or eff.get("kind", "") in ["heal", "campaign"] else target)
	await _apply_effect(user, target, eff, int(mv.get("power", 0)) > 0)


func _apply_effect(user: Politician, target: Politician, eff: Dictionary, damaging: bool) -> void:
	if eff.is_empty():
		return
	var chance := int(eff.get("chance", 100))
	var roll_ok := chance >= 100 or rng.randi_range(1, 100) <= chance
	match eff.get("kind", ""):
		"status":
			if target.is_fainted():
				return
			var st: String = eff.get("status", "")
			if BattleLogic.can_inflict(target, st) and roll_ok:
				target.status = st
				_hud_for(target).refresh()
				Audio.sfx("status")
				if st == "escandalo":
					await say("¡%s se ve envuelto en un Escándalo!" % _name(target))
				else:
					await say("¡%s ha sido Imputado!" % _name(target))
			elif not damaging:
				await say("No ha tenido ningún efecto.")
		"stat":
			if not roll_ok:
				return
			var who: Politician = user if eff.get("target", "self") == "self" else target
			if who.is_fainted():
				return
			var stat: String = eff.get("stat", "")
			var want := int(eff.get("stages", 1))
			var got := BattleLogic.apply_stage(who, stat, want)
			var label: String = STAT_LABELS.get(stat, stat)
			if got == 0:
				await say("¡La %s de %s no puede %s más!" % [label, _name(who), "subir" if want > 0 else "bajar"])
			elif got > 0:
				await say("¡La %s de %s sube%s!" % [label, _name(who), " mucho" if got > 1 else ""])
			else:
				await say("¡La %s de %s baja%s!" % [label, _name(who), " mucho" if got < -1 else ""])
		"heal":
			if user.hp >= user.max_hp():
				await say("¡%s ya tiene el Carisma al máximo!" % _name(user))
				return
			user.hp = mini(user.max_hp(), user.hp + int(user.max_hp() * float(eff.get("fraction", 0.5))))
			Audio.sfx("heal")
			await _hud_for(user).animate_hp(user.hp)
			await say("¡%s recupera Carisma!" % _name(user))
		"campaign":
			if user.campaign_turns > 0:
				await say("¡%s ya está En Campaña!" % _name(user))
				return
			user.campaign_turns = int(eff.get("turns", 5))
			_hud_for(user).refresh()
			await say("¡%s entra En Campaña! Su Agilidad se dispara." % _name(user))


func _check_faints() -> bool:
	if enemy.is_fainted():
		await _faint(enemy)
		await _award_exp(enemy)
		if is_trainer and enemy_idx + 1 < enemy_team.size():
			await _send_enemy(enemy_idx + 1)
		else:
			outcome = "win"
			return true
	if player.is_fainted():
		await _faint(player)
		if GameState.able_count() == 0:
			outcome = "lose"
			return true
		var i: int = await choose_switch(true)
		await _send_player(i)
	return false


func _faint(p: Politician) -> void:
	var spr := _sprite_for(p)
	Audio.sfx("faint")
	var tw := create_tween()
	tw.tween_property(spr, "position:y", spr.position.y + 40, 0.35)
	tw.parallel().tween_property(spr, "modulate:a", 0.0, 0.35)
	await tw.finished
	spr.hide()
	spr.modulate.a = 1.0
	spr.position = enemy_home if p == enemy else player_home
	p.status = ""
	p.campaign_turns = 0
	_hud_for(p).hide()
	await say("¡%s presenta su Dimisión!" % _name(p))


func _award_exp(defeated: Politician) -> void:
	var base := BattleLogic.exp_yield(defeated, is_trainer)
	var others := 0
	for p: Politician in GameState.party:
		if p.is_fainted():
			continue
		var amt := base if p in participants else int(base * BattleLogic.SHARE_EXP_FRACTION)
		if amt <= 0:
			continue
		var before := p.exp_progress()
		var levels: Array = p.gain_exp(amt)
		if p == player:
			await say("¡%s gana %d puntos de experiencia!" % [p.display_name(), amt])
			await player_hud.animate_exp(before, 1.0 if not levels.is_empty() else p.exp_progress(), 0.4)
		else:
			others = amt
		for lv in levels:
			if p == player:
				player_hud.shown_hp = p.hp
				player_hud.refresh()
			Audio.sfx("levelup")
			await say("¡%s sube al nivel %d!" % [p.display_name(), lv])
			for m in p.moves_learned_at(lv):
				await learn_move(p, m)
		if p.evolution_target() > 0 and not p in evolutions:
			evolutions.append(p)
	if others > 0:
		await say("El resto del equipo también aprende algo: +%d EXP." % others)


func learn_move(p: Politician, move_id: String) -> void:
	if p.knows(move_id):
		return
	var mname: String = GameData.move(move_id).get("name", "?")
	if p.add_move(move_id):
		await say("¡%s aprende %s!" % [p.display_name(), mname])
		return
	await say("%s quiere aprender %s, pero ya domina cuatro argumentos." % [p.display_name(), mname])
	if await Ui.yes_no("¿Olvidar un argumento para aprender %s?" % mname):
		var opts: Array = []
		for m in p.moves:
			opts.append(GameData.move(m["id"]).get("name", "?"))
		var i: int = await Ui.choose(opts, {"title": "¿CUÁL OLVIDA?"})
		if i >= 0:
			var old: String = opts[i]
			p.replace_move(i, move_id)
			await say("1, 2 y... ¡ya está! %s olvida %s y aprende %s." % [p.display_name(), old, mname])
			return
	await say("%s no aprende %s." % [p.display_name(), mname])


func _withdraw_player() -> void:
	var tw := create_tween()
	tw.tween_property(player_sprite, "scale", Vector2.ZERO, 0.2)
	await tw.finished
	player_hud.hide()


# ------------------------------------------------------------------ capture
func _throw_carnet(item_id: String) -> bool:
	GameState.remove_item(item_id)
	var it := GameData.item(item_id)
	await say("¡Ofreces un %s a %s!" % [it["name"], enemy.display_name()])
	carnet_sprite.texture = UiStyle.tex("res://assets/sprites/items/%s.png" % it.get("icon", "carnet"))
	var start := player_home + Vector2(40, 10)
	var target := enemy_home + Vector2(20, 24)
	carnet_sprite.position = start
	carnet_sprite.show()
	Audio.sfx("throw")
	var t := 0.0
	while t < 0.5:
		await get_tree().process_frame
		t += get_process_delta_time()
		var k := clampf(t / 0.5, 0.0, 1.0)
		carnet_sprite.position = start.lerp(target, k) + Vector2(0, -50 * sin(PI * k))
	await Ui.flash(Color.WHITE, 1)
	var tw := create_tween()
	tw.tween_property(enemy_sprite, "scale", Vector2.ZERO, 0.2)
	await tw.finished
	var tw2 := create_tween()
	tw2.tween_property(carnet_sprite, "position:y", enemy_home.y + 40, 0.2)
	await tw2.finished
	var res := BattleLogic.attempt_capture(enemy, float(it.get("catch_mult", 1.0)), rng)
	for i in range(int(res["shakes"])):
		await get_tree().create_timer(0.35).timeout
		Audio.sfx("shake")
		var tw3 := create_tween()
		tw3.tween_property(carnet_sprite, "rotation", 0.35, 0.08)
		tw3.tween_property(carnet_sprite, "rotation", -0.35, 0.12)
		tw3.tween_property(carnet_sprite, "rotation", 0.0, 0.08)
		await tw3.finished
	await get_tree().create_timer(0.3).timeout
	if res["caught"]:
		Audio.stop_music()
		await Audio.sfx_wait("capture")
		await say("¡Enhorabuena! ¡%s se ha afiliado a tu partido!" % enemy.display_name())
		var new_entry := not GameState.is_caught(enemy.species_id)
		enemy.reset_battle_state()
		var where := GameState.add_politician(enemy)
		if new_entry:
			await say("Los datos de %s se han añadido a la Politidex." % enemy.display_name())
		if where == "box":
			await say("Tu equipo está completo: %s espera en la Sede del Partido." % enemy.display_name())
		carnet_sprite.hide()
		return true
	carnet_sprite.hide()
	var tw4 := create_tween()
	tw4.tween_property(enemy_sprite, "scale", Vector2.ONE, 0.2)
	await tw4.finished
	await say(FAIL_LINES[rng.randi_range(0, FAIL_LINES.size() - 1)])
	return false


# ------------------------------------------------------------------ animations
func _attack_anim(user: Politician, target: Politician) -> void:
	var us := _sprite_for(user)
	var ts := _sprite_for(target)
	var dir := Vector2(8, -4) if user == player else Vector2(-8, 4)
	var home := us.position
	var tw := create_tween()
	tw.tween_property(us, "position", home + dir, 0.08)
	tw.tween_property(us, "position", home, 0.1)
	await tw.finished
	Audio.sfx("hit")
	for i in range(3):
		ts.visible = false
		await get_tree().create_timer(0.06).timeout
		ts.visible = true
		await get_tree().create_timer(0.06).timeout


func _status_anim(p: Politician) -> void:
	var s := _sprite_for(p)
	var home := s.position
	var tw := create_tween()
	for i in range(2):
		tw.tween_property(s, "position", home + Vector2(0, -4), 0.07)
		tw.tween_property(s, "position", home, 0.07)
	await tw.finished


# ------------------------------------------------------------------ end
func _end() -> void:
	match outcome:
		"win":
			if is_trainer:
				Audio.play_music("victory")
				trainer_sprite.position = enemy_home + Vector2(ScreenScaler.logical_size.x, 0)
				trainer_sprite.show()
				var tw := create_tween()
				tw.tween_property(trainer_sprite, "position", enemy_home, 0.4)
				await tw.finished
				await say("¡Has ganado el debate contra %s!" % trainer.get("name", ""))
				await say("%s: «%s»" % [trainer.get("name", ""), trainer.get("lose", "")])
				var reward := int(trainer.get("reward", 100))
				GameState.money += reward
				await say("¡Recibes %d€ en donaciones!" % reward)
			else:
				Audio.play_music("victory")
		"lose":
			Audio.stop_music()
			await say("¡No te quedan afiliados en pie!")
			var lost := GameState.money / 2
			GameState.money -= lost
			await say("Pierdes %d€ en la campaña y vuelves corriendo a la Sede..." % lost)
	for p: Politician in GameState.party:
		p.reset_battle_state()
	await Ui.fade_out(0.3)
	finished.emit({"outcome": outcome, "evolutions": evolutions, "trainer_id": trainer_id})
