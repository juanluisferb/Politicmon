extends Node

signal changed

const PARTY_MAX := 6
const START_ITEMS := {"carnet_normal": 5, "mitin": 3}
const START_MONEY := 1000

var player_name := "ALEX"
var party: Array = []
var box: Array = []
var items: Dictionary = {}
var money := START_MONEY
var badges: Array = []
var flags: Dictionary = {}
var defeated_trainers: Dictionary = {}
var seen: Dictionary = {}
var caught: Dictionary = {}
var map_id := "madrid_garaje"
var pos := Vector2i(4, 5)
var facing := "down"
var respawn := {"map": "madrid_garaje", "x": 4, "y": 5}
var play_time := 0.0
var rng := RandomNumberGenerator.new()
var game_active := false


func _ready() -> void:
	rng.randomize()


func _process(delta: float) -> void:
	if game_active:
		play_time += delta


func reset_new_game(name: String = "ALEX") -> void:
	player_name = name
	party = []
	box = []
	items = START_ITEMS.duplicate()
	money = START_MONEY
	badges = []
	flags = {}
	defeated_trainers = {}
	seen = {}
	caught = {}
	map_id = "madrid_garaje"
	pos = Vector2i(4, 5)
	facing = "up"
	respawn = {"map": "madrid_garaje", "x": 4, "y": 5}
	play_time = 0.0
	game_active = true
	changed.emit()


func add_politician(p: Politician) -> String:
	mark_caught(p.species_id)
	if party.size() < PARTY_MAX:
		party.append(p)
		changed.emit()
		return "party"
	box.append(p)
	changed.emit()
	return "box"


func first_able_index() -> int:
	for i in range(party.size()):
		if not party[i].is_fainted():
			return i
	return -1


func able_count() -> int:
	var n := 0
	for p in party:
		if not p.is_fainted():
			n += 1
	return n


func heal_party() -> void:
	for p in party:
		p.heal_full()
	changed.emit()


func item_count(id: String) -> int:
	return int(items.get(id, 0))


func add_item(id: String, n: int = 1) -> void:
	items[id] = item_count(id) + n
	changed.emit()


func remove_item(id: String, n: int = 1) -> bool:
	if item_count(id) < n:
		return false
	items[id] = item_count(id) - n
	if items[id] <= 0:
		items.erase(id)
	changed.emit()
	return true


func has_badge(zone: String) -> bool:
	return zone in badges


func add_badge(zone: String) -> void:
	if not has_badge(zone):
		badges.append(zone)
		changed.emit()


func flag(name: String) -> bool:
	return bool(flags.get(name, false))


func set_flag(name: String, v: bool = true) -> void:
	flags[name] = v


func mark_seen(id: int) -> void:
	seen[str(id)] = true


func mark_caught(id: int) -> void:
	seen[str(id)] = true
	caught[str(id)] = true


func is_seen(id: int) -> bool:
	return seen.has(str(id))


func is_caught(id: int) -> bool:
	return caught.has(str(id))


func trainer_defeated(id: String) -> bool:
	return defeated_trainers.has(id)


func to_dict() -> Dictionary:
	return {
		"version": 1,
		"player_name": player_name,
		"party": party.map(func(p): return p.to_dict()),
		"box": box.map(func(p): return p.to_dict()),
		"items": items.duplicate(),
		"money": money,
		"badges": badges.duplicate(),
		"flags": flags.duplicate(),
		"defeated_trainers": defeated_trainers.duplicate(),
		"seen": seen.duplicate(),
		"caught": caught.duplicate(),
		"map_id": map_id,
		"pos": [pos.x, pos.y],
		"facing": facing,
		"respawn": respawn.duplicate(),
		"play_time": play_time,
	}


func from_dict(d: Dictionary) -> void:
	player_name = str(d.get("player_name", "ALEX"))
	party = []
	for pd in d.get("party", []):
		party.append(Politician.from_dict(pd))
	box = []
	for pd in d.get("box", []):
		box.append(Politician.from_dict(pd))
	items = {}
	var it: Dictionary = d.get("items", {})
	for k in it:
		items[k] = int(it[k])
	money = int(d.get("money", START_MONEY))
	badges = Array(d.get("badges", []))
	flags = d.get("flags", {}).duplicate()
	defeated_trainers = d.get("defeated_trainers", {}).duplicate()
	seen = d.get("seen", {}).duplicate()
	caught = d.get("caught", {}).duplicate()
	map_id = str(d.get("map_id", "madrid_garaje"))
	var p: Array = d.get("pos", [4, 5])
	pos = Vector2i(int(p[0]), int(p[1]))
	facing = str(d.get("facing", "down"))
	respawn = d.get("respawn", respawn).duplicate()
	play_time = float(d.get("play_time", 0.0))
	game_active = true
	changed.emit()


func time_string() -> String:
	var t := int(play_time)
	return "%d:%02d" % [t / 3600, (t / 60) % 60]
