extends TestCase


func test_25_politicians_with_valid_data() -> void:
	eq(GameData.species_count(), 25, "species count")
	for id in GameData.politicians:
		var s: Dictionary = GameData.politicians[id]
		check(s["types"].size() >= 1 and s["types"].size() <= 2, "types of %d" % id)
		for t in s["types"]:
			check(GameData.types.has(t), "type %s of %d" % [t, id])
		eq(s["stats"].size(), 6, "stats of %d" % id)
		eq(s["moves"].size(), 4, "4 moves for %d" % id)
		for m in s["moves"]:
			check(GameData.moves.has(m[0]), "move %s of %d" % [m[0], id])
		check(str(s.get("desc", "")).length() > 20, "dex text %d" % id)
		if s.has("evolves_to"):
			check(GameData.politicians.has(int(s["evolves_to"])), "evo target of %d" % id)


func test_moves_count_and_fields() -> void:
	check(GameData.moves.size() >= 30, "at least 30 moves")
	for k in GameData.moves:
		var m: Dictionary = GameData.moves[k]
		check(m.has("name") and m.has("category") and m.has("pp"), "fields of " + k)
		check(m["type"] == "" or GameData.types.has(m["type"]), "type of " + k)


func test_trainers_reference_valid_species() -> void:
	for k in GameData.trainers:
		var t: Dictionary = GameData.trainers[k]
		check(t["team"].size() >= 1 and t["team"].size() <= 3, "team size " + k)
		for e in t["team"]:
			check(GameData.politicians.has(int(e[0])), "species in " + k)


func test_starter_moves_at_level_5() -> void:
	var p := Politician.create(1, 5)
	eq(p.moves.size(), 2, "becario L5 knows 2 moves")
	eq(p.max_hp(), 19, "becario L5 hp")
	eq(p.stat("oratoria"), 9, "becario L5 oratoria")
	var q := Politician.create(3, 40)
	eq(q.moves.size(), 4, "jefe L40 knows 4")
