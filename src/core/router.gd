extends Node

var world: Node
var current: Node
var screen := ""
var overworld: Node
var in_battle := false


func attach(world_node: Node) -> void:
	world = world_node


func touch_allowed() -> bool:
	return (screen == "overworld" and not in_battle) or screen == "test"


func set_screen(node: Node, name: String) -> void:
	if current and is_instance_valid(current) and current != overworld:
		current.queue_free()
	if name != "overworld" and overworld and is_instance_valid(overworld):
		overworld.queue_free()
		overworld = null
	current = node
	screen = name
	if node.get_parent() == null:
		world.add_child(node)


func start() -> void:
	goto_title()


func goto_title() -> void:
	GameState.game_active = false
	var t: Node = load("res://scenes/title.tscn").instantiate()
	set_screen(t, "title")
	Ui.set_fade(0.0)


func start_overworld(map_id: String, pos: Vector2i, facing: String) -> void:
	GameState.map_id = map_id
	GameState.pos = pos
	GameState.facing = facing
	if overworld and is_instance_valid(overworld):
		overworld.queue_free()
	overworld = load("res://scenes/overworld.tscn").instantiate()
	set_screen(overworld, "overworld")


func start_battle(cfg: Dictionary) -> Dictionary:
	in_battle = true
	Ui.busy_count += 1
	var music_before := Audio.current_music
	var battle: BattleScene = BattleScene.new().setup(cfg)
	if overworld and is_instance_valid(overworld):
		overworld.process_mode = Node.PROCESS_MODE_DISABLED
		overworld.visible = false
	Ui.set_fade(0.0)
	var layer := CanvasLayer.new()
	layer.layer = 5
	layer.add_child(battle)
	world.add_child(layer)
	var result: Dictionary = await battle.finished
	layer.queue_free()
	for p in result.get("evolutions", []):
		if not p.is_fainted() or true:
			await evolve(p)
	if overworld and is_instance_valid(overworld):
		overworld.visible = true
		overworld.process_mode = Node.PROCESS_MODE_INHERIT
	Ui.busy_count -= 1
	in_battle = false
	if result.get("outcome", "") != "lose" and music_before != "":
		Audio.play_music(music_before)
	return result


func evolve(p: Politician) -> void:
	var target := p.evolution_target()
	if target <= 0:
		return
	var layer := CanvasLayer.new()
	layer.layer = 6
	world.add_child(layer)
	var bg := ColorRect.new()
	bg.color = UiStyle.PAPER
	bg.size = Vector2(ScreenScaler.logical_size)
	layer.add_child(bg)
	var spr := TextureRect.new()
	var old_tex := UiStyle.tex(GameData.sprite_path(p.species_id, "front"))
	var new_tex := UiStyle.tex(GameData.sprite_path(target, "front"))
	spr.texture = old_tex
	spr.size = Vector2(112, 112)
	spr.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	spr.stretch_mode = TextureRect.STRETCH_SCALE
	var safe := ScreenScaler.safe_rect()
	spr.position = (Vector2(ScreenScaler.logical_size.x / 2.0 - 56, safe.position.y + (safe.size.y - Ui.dialog.box_height()) / 2.0 - 56)).round()
	layer.add_child(spr)
	await Ui.fade_in(0.2)
	var old_name := p.display_name()
	Audio.play_music("evolution")
	await Ui.say("¿Qué? ¡%s está cambiando de cargo!" % old_name)
	var delay := 0.3
	for i in range(14):
		spr.texture = new_tex if i % 2 == 0 else old_tex
		spr.modulate = Color(0.2, 0.2, 0.2) if i % 2 == 0 else Color.WHITE
		await get_tree().create_timer(delay).timeout
		delay = maxf(0.06, delay * 0.82)
	spr.texture = new_tex
	spr.modulate = Color.WHITE
	await Ui.flash(Color.WHITE, 2)
	p.evolve_to(target)
	GameState.mark_caught(target)
	Audio.sfx("levelup")
	await Ui.say("¡Enhorabuena! ¡%s ha ascendido a %s!" % [old_name, p.display_name()])
	await Ui.fade_out(0.2)
	layer.queue_free()


func show_victory() -> void:
	await Ui.fade_out(0.6)
	var v: Node = load("res://scenes/victory.tscn").instantiate()
	set_screen(v, "victory")
	await Ui.fade_in(0.6)
