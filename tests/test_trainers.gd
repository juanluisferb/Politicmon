extends TestCase

const ZONES := {"madrid": ["t_mad_1", "t_mad_2", "t_mad_3", "leader_madrid"],
	"andalucia": ["t_and_1", "t_and_2", "t_and_3", "leader_andalucia"],
	"galicia": ["t_gal_1", "t_gal_2", "t_gal_3", "leader_galicia"]}


func _max_level(tid: String) -> int:
	var m := 0
	for e in GameData.trainer(tid)["team"]:
		m = maxi(m, int(e[1]))
	return m


func test_levels_increase_inside_each_zone() -> void:
	for z in ZONES:
		var ids: Array = ZONES[z]
		for i in range(ids.size() - 1):
			check(_max_level(ids[i]) <= _max_level(ids[i + 1]), "%s: %s <= %s" % [z, ids[i], ids[i + 1]])


func test_levels_increase_between_zones() -> void:
	check(_max_level("leader_madrid") < _max_level("t_and_1") + 5, "andalucia starts near madrid leader")
	check(_max_level("leader_madrid") < _max_level("leader_andalucia"), "leader 2 stronger")
	check(_max_level("leader_andalucia") < _max_level("leader_galicia"), "leader 3 stronger")


func test_rivals_belong_to_fictional_parties() -> void:
	for z in ZONES:
		for tid in ZONES[z]:
			var t := GameData.trainer(tid)
			check(str(t.get("intro", "")).length() > 10, "intro line " + tid)
			if not t.get("leader", false):
				check(GameData.parties.has(t["party"]), "party of " + tid)
				check(ResourceLoader.exists("res://assets/sprites/trainers/%s.png" % t["sprite"]), "sprite of " + tid)


func test_leaders_give_badges_and_specialise() -> void:
	var spec := {"leader_madrid": "mediatico", "leader_andalucia": "baron", "leader_galicia": "regionalista"}
	for tid in spec:
		var t := GameData.trainer(tid)
		check(t.has("badge") and t.has("badge_name"), "badge of " + tid)
		for e in t["team"]:
			check(spec[tid] in GameData.species(int(e[0]))["types"], "%s uses %s" % [tid, spec[tid]])


func test_four_rival_parties_with_distinct_colors() -> void:
	var colors := {}
	for k in ["ppi", "asc", "myt", "pas"]:
		var p: Dictionary = GameData.parties[k]
		check(not colors.has(p["colors"][0]), "unique color " + k)
		colors[p["colors"][0]] = true
