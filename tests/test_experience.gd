extends TestCase


func test_exp_curve() -> void:
	eq(BattleLogic.exp_for_level(1), 0, "L1")
	eq(BattleLogic.exp_for_level(5), 100, "L5")
	eq(BattleLogic.exp_for_level(10), 800, "L10")
	for n in range(2, 60):
		check(BattleLogic.exp_for_level(n + 1) > BattleLogic.exp_for_level(n), "monotonic %d" % n)
		eq(BattleLogic.level_for_exp(BattleLogic.exp_for_level(n)), n, "inverse %d" % n)


func test_exp_yield() -> void:
	var w := Politician.create(1, 10)
	eq(BattleLogic.exp_yield(w, false), 137, "wild becario L10")
	eq(BattleLogic.exp_yield(w, true), 206, "trainer x1.5")


func test_level_up_increases_stats_and_hp() -> void:
	var p := Politician.create(10, 5)
	var hp_before := p.max_hp()
	p.hp = p.max_hp() - 3
	var levels := p.gain_exp(BattleLogic.exp_for_level(8) - p.xp)
	eq(levels, [6, 7, 8], "levels gained")
	eq(p.level, 8, "now L8")
	check(p.max_hp() > hp_before, "max hp grows")
	eq(p.hp, p.max_hp() - 3, "damage preserved")


func test_learns_moves_by_level() -> void:
	var p := Politician.create(15, 3)
	eq(p.moves.size(), 1, "jornalero L3")
	eq(p.moves_learned_at(4), ["sentada"], "learns sentada at 4")


func test_evolution_by_level() -> void:
	var p := Politician.create(1, 13)
	eq(p.evolution_target(), 0, "no evo at 13")
	p.gain_exp(BattleLogic.exp_for_level(14) - p.xp)
	eq(p.evolution_target(), 2, "evolves at 14")
	p.evolve_to(2)
	eq(p.species_id, 2, "now asesor")
	eq(p.display_name(), "Asesor Áulico", "name")
	check(p.moves.size() <= 4, "max 4 moves")
	check(p.knows("titular_viral"), "keeps signature move")


func test_serialization_roundtrip() -> void:
	var p := Politician.create(19, 31)
	p.hp = 7
	p.status = "imputado"
	p.moves[0]["pp"] = 2
	var q := Politician.from_dict(p.to_dict())
	eq(q.species_id, 19, "species")
	eq(q.level, 31, "level")
	eq(q.xp, p.xp, "exp")
	eq(q.hp, 7, "hp")
	eq(q.status, "imputado", "status")
	eq(q.moves[0]["pp"], 2, "pp")
