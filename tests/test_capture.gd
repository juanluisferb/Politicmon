extends TestCase

var rng := RandomNumberGenerator.new()


func before_each() -> void:
	rng.seed = 99


func test_lower_hp_is_easier() -> void:
	var p := Politician.create(1, 10)
	var full := BattleLogic.catch_probability(p, 1.0)
	p.hp = 1
	var low := BattleLogic.catch_probability(p, 1.0)
	check(low > full, "low hp easier")
	near(full, 200.0 / 3.0 / 255.0, 0.001, "full hp formula")


func test_carnet_types_rank() -> void:
	var p := Politician.create(12, 30)
	p.hp = p.max_hp() / 2
	var n := BattleLogic.catch_probability(p, GameData.item("carnet_normal")["catch_mult"])
	var pr := BattleLogic.catch_probability(p, GameData.item("carnet_premium")["catch_mult"])
	var li := BattleLogic.catch_probability(p, GameData.item("puesto_lista")["catch_mult"])
	check(n < pr and pr < li, "normal < premium < lista")


func test_status_bonus() -> void:
	var p := Politician.create(12, 30)
	var a := BattleLogic.catch_probability(p, 1.0)
	p.status = "escandalo"
	near(BattleLogic.catch_probability(p, 1.0), a * 1.5, 0.0001, "escandalo x1.5")
	p.status = "imputado"
	near(BattleLogic.catch_probability(p, 1.0), a * 1.5, 0.0001, "imputado x1.5")


func test_attempt_matches_probability() -> void:
	var p := Politician.create(11, 20)
	p.hp = p.max_hp() / 3
	var prob := BattleLogic.catch_probability(p, 1.5)
	var caught := 0
	var n := 5000
	for i in range(n):
		var r := BattleLogic.attempt_capture(p, 1.5, rng)
		if r["caught"]:
			caught += 1
			eq(r["shakes"], 3, "caught means 3 shakes")
	near(caught / float(n), prob, 0.025, "empirical rate")


func test_legendary_is_hard_but_possible() -> void:
	var p := Politician.create(25, 36)
	p.hp = 1
	p.status = "imputado"
	var prob := BattleLogic.catch_probability(p, 2.5)
	check(prob > 0.2 and prob < 0.7, "legendary with best setup: %f" % prob)
	p.hp = p.max_hp()
	p.status = ""
	check(BattleLogic.catch_probability(p, 1.0) < 0.05, "legendary at full hp is very hard")


func test_probability_clamped() -> void:
	var p := Politician.create(1, 5)
	p.hp = 1
	p.status = "escandalo"
	near(BattleLogic.catch_probability(p, 2.5), 1.0, 0.0001, "clamped to 1")
