extends Node
## Plays an abstract run of the whole game with the real battle formulas and reports
## levels, battles fought and an estimated play time. Run: tools/balance.sh

const ZONES := ["madrid", "andalucia", "galicia"]
const TRAINERS := {"madrid": ["t_mad_1", "t_mad_2", "t_mad_3"], "andalucia": ["t_and_1", "t_and_2", "t_and_3"], "galicia": ["t_gal_1", "t_gal_2", "t_gal_3"]}
const LEADERS := {"madrid": "leader_madrid", "andalucia": "leader_andalucia", "galicia": "leader_galicia"}
const SECONDS := {"wild": 32.0, "trainer": 55.0, "leader": 110.0, "zone_walk": 300.0, "intro": 150.0, "catch": 20.0}

var rng := RandomNumberGenerator.new()
var party: Array = []
var battles := {"wild": 0, "trainer": 0, "leader": 0, "losses": 0}
var seconds := 0.0
var loss_by := {}


func _ready() -> void:
	var runs := 20
	var totals := []
	var report := ""
	for r in range(runs):
		rng.seed = 1000 + r
		var res := run_game(1 + (r % 3) * 0)
		totals.append(res)
		if r == 0:
			report = res["summary"]
	print(report)
	var avg_min := 0.0
	var avg_wild := 0.0
	var avg_losses := 0.0
	for t in totals:
		avg_min += t["minutes"] / runs
		avg_wild += t["wild"] / float(runs)
		avg_losses += t["losses"] / float(runs)
	print("AVERAGE over %d runs: %.1f min, %.1f wild battles, %.1f lost battles" % [runs, avg_min, avg_wild, avg_losses])
	print("losses by fight: ", loss_by)
	get_tree().quit()


func run_game(_starter: int) -> Dictionary:
	var starters := [1, 10, 15]
	party = [Politician.create(starters[rng.randi_range(0, 2)], 5)]
	battles = {"wild": 0, "trainer": 0, "leader": 0, "losses": 0}
	seconds = SECONDS["intro"]
	var summary := ""
	var caught_species := {}
	for z in ZONES:
		seconds += SECONDS["zone_walk"]
		var enc: Dictionary = GameData.map_data(z)["encounters"]
		var zone_catches := 0
		var sequence: Array = TRAINERS[z].duplicate()
		sequence.append(LEADERS[z])
		var grind := 0
		for tid in sequence:
			var won := false
			var attempts := 0
			while not won and attempts < 8:
				attempts += 1
				for i in range(2 + grind):
					var foe := wild(enc)
					if zone_catches < 2 and party.size() < 6 and not caught_species.has(foe.species_id):
						caught_species[foe.species_id] = true
						zone_catches += 1
						party.append(foe)
						seconds += SECONDS["catch"]
						continue
					fight([foe], false, z)
					battles["wild"] += 1
					seconds += SECONDS["wild"]
					heal()
				heal()
				var t := GameData.trainer(tid)
				var team: Array = []
				for e in t["team"]:
					team.append(Politician.create(int(e[0]), int(e[1])))
				won = fight(team, true, z)
				var kind := "leader" if t.get("leader", false) else "trainer"
				battles[kind] += 1
				seconds += SECONDS[kind]
				if not won:
					battles["losses"] += 1
					loss_by[tid] = loss_by.get(tid, 0) + 1
					grind = 3
				else:
					grind = 0
				heal()
		summary += "%s cleared: party %s | wild so far %d, losses %d, %.1f min\n" % [z, party.map(func(p): return "%s L%d" % [p.display_name(), p.level]), battles["wild"], battles["losses"], seconds / 60.0]
	var legend := Politician.create(25, 36)
	heal()
	var legend_won := fight([legend], false, "")
	seconds += SECONDS["leader"]
	summary += "legend %s\n" % ("beaten" if legend_won else "LOST")
	return {"minutes": seconds / 60.0, "wild": battles["wild"], "losses": battles["losses"], "summary": summary}


func wild(enc: Dictionary) -> Politician:
	var table: Array = enc["table"]
	var total := 0
	for e in table:
		total += int(e[3])
	var r := rng.randi_range(1, total)
	for e in table:
		r -= int(e[3])
		if r <= 0:
			return Politician.create(int(e[0]), rng.randi_range(int(e[1]), int(e[2])))
	return Politician.create(int(table[0][0]), int(table[0][1]))


func heal() -> void:
	for p in party:
		p.heal_full()


func best_active() -> Politician:
	for p in party:
		if not p.is_fainted():
			return p
	return null


func fight(enemies: Array, trainer: bool, zone: String) -> bool:
	party.sort_custom(func(a, b): return a.level > b.level)
	for p: Politician in party:
		p.reset_battle_state()
	var participants := {}
	for foe in enemies:
		var me := best_active()
		if me == null:
			return false
		participants = {me: true}
		var turns := 0
		while not foe.is_fainted() and turns < 60:
			turns += 1
			me = best_active()
			if me == null:
				return false
			participants[me] = true
			var pm := BattleLogic.choose_ai_move(me, foe, rng, true)
			var em := BattleLogic.choose_ai_move(foe, me, rng, trainer)
			var order := [[me, foe, pm], [foe, me, em]]
			if not BattleLogic.goes_first(me, foe, rng):
				order.reverse()
			for step in order:
				var u: Politician = step[0]
				var t: Politician = step[1]
				if u.is_fainted() or t.is_fainted():
					continue
				act(u, t, int(step[2]), zone)
			for x in [me, foe]:
				if not x.is_fainted():
					x.hp = maxi(0, x.hp - BattleLogic.escandalo_damage(x))
		if not foe.is_fainted():
			return false
		var base := BattleLogic.exp_yield(foe, trainer)
		for p: Politician in party:
			if p.is_fainted():
				continue
			p.gain_exp(base if participants.has(p) else int(base * BattleLogic.SHARE_EXP_FRACTION))
			var evo: int = p.evolution_target()
			if evo > 0:
				p.evolve_to(evo)
	return best_active() != null


func act(u: Politician, t: Politician, idx: int, zone: String) -> void:
	if BattleLogic.imputado_blocks(u, rng):
		return
	var mid := "balbuceo"
	if idx >= 0:
		mid = u.moves[idx]["id"]
	if not BattleLogic.accuracy_hits(mid, rng):
		return
	var mv := GameData.move(mid)
	var eff: Dictionary = mv.get("effect", {})
	if int(mv.get("power", 0)) > 0:
		var r := BattleLogic.calc_damage(u, t, mid, zone, rng)
		t.hp = maxi(0, t.hp - int(r["damage"]))
		if eff.get("kind", "") == "drain":
			u.hp = mini(u.max_hp(), u.hp + int(r["damage"] * 0.5))
	match eff.get("kind", ""):
		"status":
			if BattleLogic.can_inflict(t, eff["status"]) and rng.randi_range(1, 100) <= int(eff.get("chance", 100)):
				t.status = eff["status"]
		"stat":
			if rng.randi_range(1, 100) <= int(eff.get("chance", 100)):
				BattleLogic.apply_stage(u if eff.get("target") == "self" else t, eff["stat"], int(eff["stages"]))
		"heal":
			u.hp = mini(u.max_hp(), u.hp + int(u.max_hp() * float(eff.get("fraction", 0.5))))
		"campaign":
			u.campaign_turns = int(eff.get("turns", 5))
