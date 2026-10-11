class_name Politician
extends RefCounted

const STATS := ["carisma", "oratoria", "aguante", "dialectica", "templanza", "agilidad"]
const MAX_LEVEL := 100

var species_id := 1
var level := 5
var xp := 0
var hp := 1
var status := ""
var moves: Array = []
var stages: Dictionary = {}
var campaign_turns := 0
var participated := false


static func create(id: int, lvl: int) -> Politician:
	var p := Politician.new()
	p.species_id = id
	p.level = clampi(lvl, 1, MAX_LEVEL)
	p.xp = BattleLogic.exp_for_level(p.level)
	p.moves = []
	for m in p.learnset_up_to(p.level):
		p.moves.append({"id": m, "pp": int(GameData.move(m).get("pp", 10))})
	p.hp = p.max_hp()
	return p


func species() -> Dictionary:
	return GameData.species(species_id)


func display_name() -> String:
	return species().get("name", "???")


func types() -> Array:
	return species().get("types", [])


func has_type(t: String) -> bool:
	return t in types()


func base_stat(stat: String) -> int:
	return int(species().get("stats", {}).get(stat, 50))


func stat(stat: String) -> int:
	var b := base_stat(stat)
	if stat == "carisma":
		return int(2 * b * level / 100.0) + level + 10
	return int(2 * b * level / 100.0) + 5


func max_hp() -> int:
	return stat("carisma")


func is_fainted() -> bool:
	return hp <= 0


func stage(stat: String) -> int:
	return int(stages.get(stat, 0))


func reset_battle_state() -> void:
	stages = {}
	campaign_turns = 0
	participated = false


func heal_full() -> void:
	hp = max_hp()
	status = ""
	for m in moves:
		m["pp"] = int(GameData.move(m["id"]).get("pp", 10))


func learnset() -> Array:
	return species().get("moves", [])


func learnset_up_to(lvl: int) -> Array:
	var known: Array = []
	for entry in learnset():
		if int(entry[1]) <= lvl and not entry[0] in known:
			known.append(entry[0])
	return known.slice(maxi(0, known.size() - 4))


func moves_learned_at(lvl: int) -> Array:
	var out: Array = []
	for entry in learnset():
		if int(entry[1]) == lvl:
			out.append(entry[0])
	return out


func knows(move_id: String) -> bool:
	for m in moves:
		if m["id"] == move_id:
			return true
	return false


func add_move(move_id: String) -> bool:
	if knows(move_id) or moves.size() >= 4:
		return false
	moves.append({"id": move_id, "pp": int(GameData.move(move_id).get("pp", 10))})
	return true


func replace_move(slot: int, move_id: String) -> void:
	moves[slot] = {"id": move_id, "pp": int(GameData.move(move_id).get("pp", 10))}


func has_usable_moves() -> bool:
	for m in moves:
		if int(m["pp"]) > 0:
			return true
	return false


func exp_to_next() -> int:
	if level >= MAX_LEVEL:
		return 0
	return BattleLogic.exp_for_level(level + 1) - xp


func exp_progress() -> float:
	if level >= MAX_LEVEL:
		return 1.0
	var lo := BattleLogic.exp_for_level(level)
	var hi := BattleLogic.exp_for_level(level + 1)
	return clampf(float(xp - lo) / float(hi - lo), 0.0, 1.0)


func gain_exp(amount: int) -> Array:
	var gained: Array = []
	xp += amount
	while level < MAX_LEVEL and xp >= BattleLogic.exp_for_level(level + 1):
		var old_max := max_hp()
		level += 1
		hp += max_hp() - old_max
		gained.append(level)
	return gained


func evolution_target() -> int:
	var s := species()
	if s.has("evolves_to") and level >= int(s.get("evolve_level", 999)):
		return int(s["evolves_to"])
	return 0


func evolve_to(id: int) -> void:
	var old_max := max_hp()
	species_id = id
	hp = clampi(hp + max_hp() - old_max, 1, max_hp())
	var new_moves := learnset_up_to(level)
	for m in new_moves:
		if not knows(m):
			if moves.size() < 4:
				add_move(m)
			else:
				for i in range(moves.size()):
					if not moves[i]["id"] in new_moves:
						replace_move(i, m)
						break


func to_dict() -> Dictionary:
	return {"species": species_id, "level": level, "exp": xp, "hp": hp, "status": status, "moves": moves.duplicate(true)}


static func from_dict(d: Dictionary) -> Politician:
	var p := Politician.new()
	p.species_id = int(d.get("species", 1))
	p.level = int(d.get("level", 5))
	p.xp = int(d.get("exp", BattleLogic.exp_for_level(p.level)))
	p.status = str(d.get("status", ""))
	p.moves = []
	for m in d.get("moves", []):
		p.moves.append({"id": str(m["id"]), "pp": int(m["pp"])})
	p.hp = clampi(int(d.get("hp", p.max_hp())), 0, p.max_hp())
	return p
