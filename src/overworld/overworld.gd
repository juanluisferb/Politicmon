class_name Overworld
extends Node2D

const WALK_TIME := 0.2
const RUN_TIME := 0.11
const BORDER := 16
const TURN_DELAY := 0.09

static var _tileset: TileSet

var map: Dictionary = {}
var map_id := ""
var width := 0
var height := 0
var names: Array = []
var ground: TileMapLayer
var entities: Node2D
var player: Actor
var camera: Camera2D
var npcs: Dictionary = {}
var items: Dictionary = {}
var signs: Dictionary = {}
var warps: Dictionary = {}
var busy := false
var current_area := ""
var grass_overlay: Sprite2D
var turn_timer := 0.0
var bump_cooldown := 0.0
var events: OverworldEvents


func _ready() -> void:
	events = OverworldEvents.new(self)
	ground = TileMapLayer.new()
	ground.tile_set = tileset()
	add_child(ground)
	entities = Node2D.new()
	entities.y_sort_enabled = true
	add_child(entities)
	load_map(GameState.map_id, GameState.pos, GameState.facing)
	Ui.fade_in(0.3)
	_post_load.call_deferred()


static func tileset() -> TileSet:
	if _tileset:
		return _tileset
	var ts := TileSet.new()
	ts.tile_size = Vector2i(16, 16)
	var src := TileSetAtlasSource.new()
	src.texture = load("res://assets/tiles/metatiles.png")
	src.texture_region_size = Vector2i(16, 16)
	var cols: int = int(GameData.tiles.get("_columns", 16)) if GameData.tiles.has("_columns") else 16
	var count := 0
	for k in GameData.tiles:
		count = maxi(count, int(GameData.tiles[k]["index"]) + 1)
	for i in range(count):
		src.create_tile(Vector2i(i % cols, i / cols))
	ts.add_source(src, 0)
	_tileset = ts
	return ts


static func atlas_coords(tile_name: String) -> Vector2i:
	var i := int(GameData.tiles.get(tile_name, {"index": 0})["index"])
	return Vector2i(i % 16, i / 16)


func load_map(id: String, pos: Vector2i, facing: String) -> void:
	for c in entities.get_children():
		c.queue_free()
	npcs.clear()
	items.clear()
	signs.clear()
	warps.clear()
	map_id = id
	map = GameData.map_data(id)
	width = int(map["width"])
	height = int(map["height"])
	var legend: Dictionary = map["legend"]
	names = []
	for row in map["rows"]:
		var r: Array = []
		for ch in row:
			r.append(legend[ch])
		names.append(r)
	ground.clear()
	var border_name: String = legend.get(map.get("border", "T"), "void")
	for y in range(-BORDER, height + BORDER):
		for x in range(-BORDER, width + BORDER):
			var n := border_name
			if x >= 0 and y >= 0 and x < width and y < height:
				n = names[y][x]
			ground.set_cell(Vector2i(x, y), 0, atlas_coords(n))
	for w in map.get("warps", []):
		warps[Vector2i(int(w["x"]), int(w["y"]))] = w
	for s in map.get("signs", []):
		signs[Vector2i(int(s["x"]), int(s["y"]))] = s
	for it in map.get("items", []):
		if GameState.flag("item:" + it["id"]):
			continue
		var spr := Sprite2D.new()
		spr.texture = load("res://assets/sprites/items/%s.png" % GameData.item(it["item"]).get("icon", "carnet"))
		spr.centered = false
		spr.position = Vector2(int(it["x"]) * 16, int(it["y"]) * 16)
		entities.add_child(spr)
		items[Vector2i(int(it["x"]), int(it["y"]))] = {"node": spr, "data": it}
	for n in map.get("npcs", []):
		if not _visible_npc(n):
			continue
		var a := Actor.new().setup(n["sprite"], Vector2i(int(n["x"]), int(n["y"])), n.get("dir", "down"))
		a.data = n
		entities.add_child(a)
		npcs[n["id"]] = a
	player = Actor.new().setup("player", pos, facing)
	entities.add_child(player)
	camera = Camera2D.new()
	camera.offset = Vector2(8, 4)
	player.add_child(camera)
	camera.make_current()
	grass_overlay = Sprite2D.new()
	var at := AtlasTexture.new()
	at.atlas = load("res://assets/tiles/metatiles.png")
	var gi := atlas_coords(_encounter_tile_name())
	at.region = Rect2(gi.x * 16, gi.y * 16 + 10, 16, 6)
	grass_overlay.texture = at
	grass_overlay.centered = false
	grass_overlay.position = Vector2(0, 10)
	grass_overlay.z_index = 1
	player.add_child(grass_overlay)
	GameState.map_id = id
	GameState.pos = pos
	GameState.facing = facing
	current_area = ""
	_update_overlay()
	Audio.play_music(map.get("music", "madrid"))


func _encounter_tile_name() -> String:
	for ch in map["legend"]:
		var n: String = map["legend"][ch]
		if GameData.tiles.get(n, {}).get("encounter", false):
			return n
	return "tall_grass"


func _visible_npc(n: Dictionary) -> bool:
	var cond: String = n.get("hide_if", "")
	if cond == "":
		return true
	return not check_condition(cond)


func check_condition(cond: String) -> bool:
	var parts := cond.split(":")
	match parts[0]:
		"badge":
			return GameState.has_badge(parts[1])
		"flag":
			return GameState.flag(parts[1])
		"badges":
			return GameState.badges.size() >= int(parts[1])
	return false


func _post_load() -> void:
	_check_area()
	await events.on_map_loaded()


# ------------------------------------------------------------------ queries
func tile_name(c: Vector2i) -> String:
	if c.x < 0 or c.y < 0 or c.x >= width or c.y >= height:
		return ""
	return names[c.y][c.x]


func tile_prop(c: Vector2i, prop: String) -> bool:
	return bool(GameData.tiles.get(tile_name(c), {}).get(prop, false))


func npc_at(c: Vector2i) -> Actor:
	for id in npcs:
		var a: Actor = npcs[id]
		if a.cell == c:
			return a
	return null


func walkable(c: Vector2i) -> bool:
	if tile_name(c) == "":
		return false
	if tile_prop(c, "door"):
		return warps.has(c)
	if tile_prop(c, "solid") or tile_prop(c, "water"):
		return false
	if npc_at(c) != null or items.has(c):
		return false
	return c != player.cell


func warp_allowed(w: Dictionary) -> bool:
	if w.has("require_badges") and GameState.badges.size() < int(w["require_badges"]):
		return false
	if w.has("require_flag") and not GameState.flag(w["require_flag"]):
		return false
	return true


# ------------------------------------------------------------------ input
func held_direction() -> String:
	for d in ["up", "down", "left", "right"]:
		if Input.is_action_pressed(d):
			return d
	return ""


func _process(delta: float) -> void:
	bump_cooldown = maxf(0.0, bump_cooldown - delta)
	if busy or Ui.is_busy() or player == null or player.moving:
		return
	if Input.is_action_just_pressed("start"):
		_open_menu()
		return
	if Input.is_action_just_pressed("a"):
		_interact()
		return
	var dir := held_direction()
	if dir == "":
		turn_timer = 0.0
		return
	if dir != player.facing:
		player.face(dir)
		GameState.facing = dir
		turn_timer = TURN_DELAY
		return
	if turn_timer > 0.0:
		turn_timer -= delta
		return
	_try_move(dir)


func _try_move(dir: String) -> void:
	var target: Vector2i = player.cell + Actor.DIRS[dir]
	if warps.has(target) and not warp_allowed(warps[target]):
		busy = true
		await Ui.say(warps[target].get("deny", "No puedes pasar."))
		busy = false
		return
	if not walkable(target):
		if bump_cooldown <= 0.0:
			Audio.sfx("bump")
			bump_cooldown = 0.3
		return
	var running := Input.is_action_pressed("b")
	busy = true
	await player.step_to(target, RUN_TIME if running else WALK_TIME)
	GameState.pos = target
	_update_overlay()
	await _after_step()
	busy = false


func _update_overlay() -> void:
	if grass_overlay:
		grass_overlay.visible = tile_prop(player.cell, "encounter")


func _after_step() -> void:
	if warps.has(player.cell):
		await do_warp(warps[player.cell])
		return
	_check_area()
	if await events.check_trainers():
		return
	if tile_prop(player.cell, "encounter") and map.get("encounters") != null:
		var enc: Dictionary = map["encounters"]
		if GameState.rng.randf() < float(enc.get("rate", 0.1)):
			await events.wild_battle(enc)


func _check_area() -> void:
	var name := ""
	for a in map.get("areas", []):
		var r: Array = a["rect"]
		if Rect2i(int(r[0]), int(r[1]), int(r[2]), int(r[3])).has_point(player.cell):
			name = a["name"]
			break
	if name == "" and current_area == "":
		name = map.get("name", "")
	if name != "" and name != current_area:
		current_area = name
		Ui.banner(name)


func do_warp(w: Dictionary) -> void:
	Audio.sfx("door")
	await Ui.fade_out(0.2)
	load_map(w["to"], Vector2i(int(w["tx"]), int(w["ty"])), w.get("dir", "down"))
	await Ui.fade_in(0.2)
	_check_area()
	await events.on_map_loaded()


func _interact() -> void:
	var fc := player.front_cell()
	var npc := npc_at(fc)
	if npc == null and tile_name(fc) == "counter":
		npc = npc_at(fc + Actor.DIRS[player.facing])
	busy = true
	if npc:
		await events.talk_to(npc)
	elif items.has(fc):
		await events.pickup(fc)
	elif signs.has(fc):
		if signs[fc]["text"] == "PC":
			await events.pc()
		else:
			await Ui.say(signs[fc]["text"])
	busy = false


func _open_menu() -> void:
	busy = true
	Audio.sfx("menu")
	await PauseMenu.open(self)
	busy = false


func remove_npc(id: String) -> void:
	if npcs.has(id):
		npcs[id].queue_free()
		npcs.erase(id)
