extends TestCase

const MAPS := ["madrid", "andalucia", "galicia", "madrid_garaje", "hemiciclo", "sede_madrid", "sede_andalucia",
	"sede_galicia", "gym_madrid", "gym_andalucia", "gym_galicia"]


func _cell_name(m: Dictionary, c: Vector2i) -> String:
	if c.x < 0 or c.y < 0 or c.x >= int(m["width"]) or c.y >= int(m["height"]):
		return ""
	return m["legend"][m["rows"][c.y][c.x]]


func _passable(m: Dictionary, c: Vector2i, warps: Dictionary) -> bool:
	var n := _cell_name(m, c)
	if n == "":
		return false
	var t: Dictionary = GameData.tiles.get(n, {})
	if t.get("door", false):
		return warps.has(c)
	return not t.get("solid", false) and not t.get("water", false)


func test_maps_well_formed() -> void:
	for id in MAPS:
		var m := GameData.map_data(id)
		check(not m.is_empty(), "map %s exists" % id)
		eq(m["rows"].size(), int(m["height"]), "%s height" % id)
		for r in m["rows"]:
			eq(r.length(), int(m["width"]), "%s row width" % id)
			for ch in r:
				check(m["legend"].has(ch) and GameData.tiles.has(m["legend"][ch]), "%s legend %s" % [id, ch])


func test_warps_point_to_walkable_cells() -> void:
	for id in MAPS:
		var m := GameData.map_data(id)
		for w in m["warps"]:
			check(MAPS.has(w["to"]), "%s warp target %s" % [id, w["to"]])
			var dest := GameData.map_data(w["to"])
			var dw := {}
			for w2 in dest["warps"]:
				dw[Vector2i(int(w2["x"]), int(w2["y"]))] = true
			var c := Vector2i(int(w["tx"]), int(w["ty"]))
			check(_passable(dest, c, {}) and not dw.has(c), "%s -> %s arrival %s walkable and not a warp" % [id, w["to"], c])


func test_objects_on_walkable_cells() -> void:
	for id in MAPS:
		var m := GameData.map_data(id)
		for n in m["npcs"]:
			check(_passable(m, Vector2i(int(n["x"]), int(n["y"])), {}), "%s npc %s on walkable cell" % [id, n["id"]])
		for it in m["items"]:
			check(_passable(m, Vector2i(int(it["x"]), int(it["y"])), {}), "%s item %s" % [id, it["id"]])


func test_three_trainers_and_one_leader_per_zone() -> void:
	var per_zone := {"madrid": [], "andalucia": [], "galicia": []}
	var leaders := {"madrid": 0, "andalucia": 0, "galicia": 0}
	for id in MAPS:
		var m := GameData.map_data(id)
		for n in m["npcs"]:
			if n.has("trainer"):
				var t := GameData.trainer(n["trainer"])
				if t.get("leader", false):
					leaders[m["zone"]] += 1
				else:
					per_zone[m["zone"]].append(n["trainer"])
	for z in per_zone:
		eq(per_zone[z].size(), 3, "rival trainers in " + z)
		eq(leaders[z], 1, "gym leader in " + z)


func test_encounter_tables_use_zone_politicians() -> void:
	for id in ["madrid", "andalucia", "galicia"]:
		var m := GameData.map_data(id)
		var enc: Dictionary = m["encounters"]
		check(enc["table"].size() >= 5, "%s encounter table size" % id)
		for e in enc["table"]:
			eq(GameData.species(int(e[0]))["zone"], id, "%s wild politician %d is from the zone" % [id, int(e[0])])


func _reachable(m: Dictionary, start: Vector2i) -> Dictionary:
	var warps := {}
	for w in m["warps"]:
		warps[Vector2i(int(w["x"]), int(w["y"]))] = true
	var seen := {start: true}
	var queue: Array = [start]
	while not queue.is_empty():
		var c: Vector2i = queue.pop_front()
		if warps.has(c) and c != start:
			continue
		for d in [Vector2i.LEFT, Vector2i.RIGHT, Vector2i.UP, Vector2i.DOWN]:
			var n: Vector2i = c + d
			if not seen.has(n) and _passable(m, n, warps):
				seen[n] = true
				queue.append(n)
	return seen


func test_all_warps_and_npcs_reachable() -> void:
	for id in MAPS:
		var m := GameData.map_data(id)
		var w0: Dictionary = m["warps"][0]
		var start := Vector2i(int(w0["x"]), int(w0["y"]))
		var seen := _reachable(m, start)
		for w in m["warps"]:
			check(seen.has(Vector2i(int(w["x"]), int(w["y"]))), "%s warp %s,%s reachable" % [id, w["x"], w["y"]])
		for n in m["npcs"]:
			var c := Vector2i(int(n["x"]), int(n["y"]))
			var near := false
			for d in [Vector2i.LEFT, Vector2i.RIGHT, Vector2i.UP, Vector2i.DOWN, Vector2i(0, 2)]:
				if seen.has(c + d):
					near = true
			check(near or seen.has(c), "%s npc %s reachable" % [id, n["id"]])
		for it in m["items"]:
			var c := Vector2i(int(it["x"]), int(it["y"]))
			var near := false
			for d in [Vector2i.LEFT, Vector2i.RIGHT, Vector2i.UP, Vector2i.DOWN]:
				if seen.has(c + d):
					near = true
			check(near, "%s item %s reachable" % [id, it["id"]])
