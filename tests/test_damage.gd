extends TestCase

var rng := RandomNumberGenerator.new()


func before_each() -> void:
	rng.seed = 1234


func _dmg(a: Politician, d: Politician, move: String, zone: String = "madrid", crit: bool = false) -> int:
	return BattleLogic.calc_damage(a, d, move, zone, rng, {"crit": crit, "roll": 1.0})["damage"]


func test_known_value() -> void:
	var a := Politician.create(1, 10)
	var d := Politician.create(10, 10)
	# L10: floor(2*10/5+2)=6; oratoria(40)=13, aguante(45)=14; floor(floor(6*45*13/14)/50)+2 = 7
	# rueda_de_prensa is Mediatico (STAB 1.5), neutral vs Baron -> floor(7*1.5) = 10
	eq(_dmg(a, d, "rueda_de_prensa"), 10, "base formula")


func test_super_effective_doubles() -> void:
	var a := Politician.create(13, 20)
	var tec := Politician.create(6, 20)
	var act := Politician.create(15, 20)
	var e := BattleLogic.calc_damage(a, tec, "promesa_imposible", "", rng, {"crit": false, "roll": 1.0})
	eq(e["effectiveness"], 2.0, "pop vs tec")
	var n := BattleLogic.calc_damage(a, act, "promesa_imposible", "", rng, {"crit": false, "roll": 1.0})
	eq(n["effectiveness"], 1.0, "pop vs act")


func test_stab_and_crit() -> void:
	var a := Politician.create(13, 30)
	var d := Politician.create(15, 30)
	var with_stab := _dmg(a, d, "promesa_imposible", "")
	var crit := _dmg(a, d, "promesa_imposible", "", true)
	check(crit > with_stab, "crit increases damage")
	near(float(crit) / with_stab, 1.5, 0.1, "crit ratio")


func test_status_moves_do_no_damage() -> void:
	var a := Politician.create(9, 20)
	var d := Politician.create(1, 20)
	eq(_dmg(a, d, "vuelva_usted_manana"), 0, "status move")


func test_minimum_one_damage() -> void:
	var a := Politician.create(1, 2)
	var d := Politician.create(22, 60)
	var d2 := _dmg(a, d, "rueda_de_prensa")
	check(d2 >= 1 and d2 <= 3, "tiny but at least 1 damage: %d" % d2)


func test_regional_bonus_at_home() -> void:
	var r := Politician.create(20, 30)
	var d := Politician.create(15, 30)
	var away := _dmg(r, d, "hecho_diferencial", "madrid")
	var home := _dmg(r, d, "hecho_diferencial", "galicia")
	check(home > away, "regionalista hits harder at home")
	near(float(home) / away, 1.3, 0.06, "x1.3 at home")
	var taken_away := _dmg(d, r, "recogida_de_firmas", "madrid")
	var taken_home := _dmg(d, r, "recogida_de_firmas", "galicia")
	check(taken_home < taken_away, "regionalista takes less at home")


func test_regional_bonus_only_for_regionalista() -> void:
	var a := Politician.create(23, 30)
	var d := Politician.create(15, 30)
	eq(_dmg(a, d, "pancarta", "galicia"), _dmg(a, d, "pancarta", "madrid"), "activista from galicia gets no bonus")


func test_stage_multipliers() -> void:
	near(BattleLogic.stage_multiplier(0), 1.0)
	near(BattleLogic.stage_multiplier(2), 2.0)
	near(BattleLogic.stage_multiplier(-2), 0.5)
	near(BattleLogic.stage_multiplier(9), 4.0, 0.0001, "clamped")


func test_campaign_boosts_speed() -> void:
	var p := Politician.create(13, 20)
	var before := BattleLogic.effective_stat(p, "agilidad")
	p.campaign_turns = 3
	near(BattleLogic.effective_stat(p, "agilidad"), before * 1.5, 0.001, "en campaña")


func test_escandalo_and_imputado() -> void:
	var p := Politician.create(1, 20)
	eq(BattleLogic.escandalo_damage(p), 0, "no status")
	p.status = "escandalo"
	eq(BattleLogic.escandalo_damage(p), int(p.max_hp() / 8.0), "1/8 hp")
	p.status = "imputado"
	var blocked := 0
	for i in range(4000):
		if BattleLogic.imputado_blocks(p, rng):
			blocked += 1
	near(blocked / 4000.0, 0.25, 0.03, "25% skip")
	check(not BattleLogic.can_inflict(p, "escandalo"), "only one status")
