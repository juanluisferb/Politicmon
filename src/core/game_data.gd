extends Node

const DATA_DIR := "res://data/"

var types: Dictionary = {}
var chart: Dictionary = {}
var regional_bonus := 1.3
var regional_type := "regionalista"
var moves: Dictionary = {}
var politicians: Dictionary = {}
var items: Dictionary = {}
var parties: Dictionary = {}
var trainers: Dictionary = {}
var palettes: Dictionary = {}
var tiles: Dictionary = {}
var _maps: Dictionary = {}


func _ready() -> void:
	load_all()


func load_all() -> void:
	var t: Dictionary = load_json("types.json")
	types = t.get("types", {})
	chart = t.get("chart", {})
	regional_bonus = t.get("regional_bonus", 1.3)
	regional_type = t.get("regional_type", "regionalista")
	moves = _strip(load_json("moves.json"))
	items = _strip(load_json("items.json"))
	parties = _strip(load_json("parties.json"))
	trainers = _strip(load_json("trainers.json"))
	palettes = load_json("palettes.json")
	tiles = _strip(load_json("tiles.json"))
	politicians.clear()
	var p: Dictionary = _strip(load_json("politicians.json"))
	for k in p:
		var d: Dictionary = p[k]
		d["id"] = int(k)
		politicians[int(k)] = d


static func load_json(file: String) -> Variant:
	var path := DATA_DIR + file
	if not FileAccess.file_exists(path):
		return {}
	var txt := FileAccess.get_file_as_string(path)
	var v = JSON.parse_string(txt)
	if v == null:
		push_error("Invalid JSON: " + path)
		return {}
	return v


static func _strip(d: Variant) -> Dictionary:
	var out := {}
	if d is Dictionary:
		for k in d:
			if not str(k).begins_with("_"):
				out[k] = d[k]
	return out


func type_multiplier(attack_type: String, defender_types: Array) -> float:
	var m := 1.0
	if attack_type == "" or not chart.has(attack_type):
		return m
	var row: Dictionary = chart[attack_type]
	for dt in defender_types:
		m *= float(row.get(dt, 1.0))
	return m


func type_name(t: String) -> String:
	return types.get(t, {}).get("name", t)


func type_color(t: String) -> Color:
	return Color(types.get(t, {}).get("color", "#808080"))


func species(id: int) -> Dictionary:
	return politicians.get(id, {})


func move(id: String) -> Dictionary:
	return moves.get(id, {})


func item(id: String) -> Dictionary:
	return items.get(id, {})


func trainer(id: String) -> Dictionary:
	return trainers.get(id, {})


func species_count() -> int:
	return politicians.size()


func map_data(id: String) -> Dictionary:
	if not _maps.has(id):
		_maps[id] = load_json("maps/%s.json" % id)
	return _maps[id]


func sprite_path(id: int, kind: String) -> String:
	var s := species(id)
	return "res://assets/sprites/politicians/%02d_%s_%s.png" % [id, s.get("key", "x"), kind]
