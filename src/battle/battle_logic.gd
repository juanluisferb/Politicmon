class_name BattleLogic
extends RefCounted

const CRIT_CHANCE := 1.0 / 16.0
const CRIT_MULT := 1.5
const STAB := 1.5
const IMPUTADO_SKIP := 0.25
const ESCANDALO_FRACTION := 1.0 / 8.0
const CAMPAIGN_SPEED := 1.5
const STATUS_CATCH_BONUS := 1.5
const TRAINER_EXP_MULT := 1.5
const SHARE_EXP_FRACTION := 0.5


static func type_multiplier(move_type: String, defender_types: Array) -> float:
	return GameData.type_multiplier(move_type, defender_types)


static func stage_multiplier(s: int) -> float:
	s = clampi(s, -6, 6)
	return (2.0 + s) / 2.0 if s >= 0 else 2.0 / (2.0 - s)


static func effective_stat(p: Politician, stat: String) -> float:
	var v := float(p.stat(stat)) * stage_multiplier(p.stage(stat))
	if stat == "agilidad" and p.campaign_turns > 0:
		v *= CAMPAIGN_SPEED
	return v


static func is_home(p: Politician, zone: String) -> bool:
	var home: String = p.species().get("zone", "")
	return zone != "" and home == zone and p.has_type(GameData.regional_type)


static func regional_factor(attacker: Politician, defender: Politician, zone: String) -> float:
	var f := 1.0
	if is_home(attacker, zone):
		f *= GameData.regional_bonus
	if is_home(defender, zone):
		f /= GameData.regional_bonus
	return f


## opts: crit (bool), roll (0.85..1.0)
static func calc_damage(attacker: Politician, defender: Politician, move_id: String, zone: String, rng: RandomNumberGenerator, opts: Dictionary = {}) -> Dictionary:
	var mv := GameData.move(move_id)
	var power := int(mv.get("power", 0))
	var result := {"damage": 0, "effectiveness": 1.0, "crit": false}
	if power <= 0 or mv.get("category", "estado") == "estado":
		return result
	var physical: bool = mv.get("category") == "fisico"
	var atk := effective_stat(attacker, "oratoria" if physical else "dialectica")
	var def := effective_stat(defender, "aguante" if physical else "templanza")
	var base := floorf(floorf(floorf(2.0 * attacker.level / 5.0 + 2.0) * power * atk / maxf(def, 1.0)) / 50.0) + 2.0
	var mtype: String = mv.get("type", "")
	var eff := type_multiplier(mtype, defender.types())
	var stab := STAB if mtype != "" and attacker.has_type(mtype) else 1.0
	var crit: bool = opts["crit"] if opts.has("crit") else rng.randf() < CRIT_CHANCE
	var roll: float = opts["roll"] if opts.has("roll") else rng.randf_range(0.85, 1.0)
	var dmg := base * stab * eff * (CRIT_MULT if crit else 1.0) * roll * regional_factor(attacker, defender, zone)
	result["effectiveness"] = eff
	result["crit"] = crit
	result["damage"] = 0 if eff == 0.0 else maxi(1, int(floorf(dmg)))
	return result


static func accuracy_hits(move_id: String, rng: RandomNumberGenerator) -> bool:
	var acc := int(GameData.move(move_id).get("accuracy", 100))
	return acc >= 100 or rng.randi_range(1, 100) <= acc


static func imputado_blocks(p: Politician, rng: RandomNumberGenerator) -> bool:
	return p.status == "imputado" and rng.randf() < IMPUTADO_SKIP


static func escandalo_damage(p: Politician) -> int:
	if p.status != "escandalo":
		return 0
	return maxi(1, int(p.max_hp() * ESCANDALO_FRACTION))


static func can_inflict(p: Politician, status: String) -> bool:
	return p.status == "" and not p.is_fainted() and status in ["escandalo", "imputado"]


static func apply_stage(p: Politician, stat: String, delta: int) -> int:
	var before := p.stage(stat)
	var after := clampi(before + delta, -6, 6)
	p.stages[stat] = after
	return after - before


static func catch_probability(target: Politician, carnet_mult: float) -> float:
	var m := float(target.max_hp())
	var h := float(maxi(target.hp, 1))
	var rate := float(target.species().get("catch_rate", 100))
	var bonus := STATUS_CATCH_BONUS if target.status in ["escandalo", "imputado"] else 1.0
	var a := ((3.0 * m - 2.0 * h) * rate * carnet_mult) / (3.0 * m) * bonus
	return clampf(a / 255.0, 0.0, 1.0)


## Four checks with probability p^(1/4) each: overall success chance equals p.
static func attempt_capture(target: Politician, carnet_mult: float, rng: RandomNumberGenerator) -> Dictionary:
	var p := catch_probability(target, carnet_mult)
	var q := pow(p, 0.25)
	var passes := 0
	for i in range(4):
		if rng.randf() < q:
			passes += 1
		else:
			break
	return {"caught": passes == 4, "shakes": mini(passes, 3), "probability": p}


static func exp_for_level(n: int) -> int:
	if n <= 1:
		return 0
	return int(4.0 * n * n * n / 5.0)


static func level_for_exp(xp: int) -> int:
	var n := 1
	while n < Politician.MAX_LEVEL and exp_for_level(n + 1) <= xp:
		n += 1
	return n


static func exp_yield(defeated: Politician, trainer: bool) -> int:
	var base := float(defeated.species().get("base_exp", 50))
	var v := base * defeated.level / 5.0
	if trainer:
		v *= TRAINER_EXP_MULT
	return maxi(1, int(v))


static func run_chance(runner: Politician, enemy: Politician, attempts: int) -> float:
	var ps := effective_stat(runner, "agilidad")
	var es := maxf(effective_stat(enemy, "agilidad"), 1.0)
	return clampf(0.5 + 0.5 * (ps - es) / es + 0.15 * attempts, 0.1, 1.0)


static func goes_first(a: Politician, b: Politician, rng: RandomNumberGenerator) -> bool:
	var sa := effective_stat(a, "agilidad")
	var sb := effective_stat(b, "agilidad")
	if sa == sb:
		return rng.randf() < 0.5
	return sa > sb


static func move_score(attacker: Politician, defender: Politician, move_id: String) -> float:
	var mv := GameData.move(move_id)
	var power := float(mv.get("power", 0))
	if power <= 0:
		var eff: Dictionary = mv.get("effect", {})
		match eff.get("kind", ""):
			"heal":
				return 80.0 if attacker.hp < attacker.max_hp() / 2 else 5.0
			"status":
				return 45.0 if defender.status == "" else 0.0
			"campaign":
				return 35.0 if attacker.campaign_turns == 0 else 0.0
			"stat":
				var target: Politician = attacker if eff.get("target") == "self" else defender
				var cur := target.stage(eff.get("stat", ""))
				return 30.0 if absi(cur) < 2 else 2.0
		return 10.0
	var t: String = mv.get("type", "")
	var s := power * type_multiplier(t, defender.types()) * float(mv.get("accuracy", 100)) / 100.0
	if t != "" and attacker.has_type(t):
		s *= STAB
	return s


static func choose_ai_move(attacker: Politician, defender: Politician, rng: RandomNumberGenerator, smart: bool) -> int:
	var usable: Array = []
	for i in range(attacker.moves.size()):
		if int(attacker.moves[i]["pp"]) > 0:
			usable.append(i)
	if usable.is_empty():
		return -1
	if not smart or rng.randf() < 0.25:
		return usable[rng.randi_range(0, usable.size() - 1)]
	var best: int = usable[0]
	var best_score := -1.0
	for i in usable:
		var sc := move_score(attacker, defender, attacker.moves[i]["id"]) * rng.randf_range(0.85, 1.15)
		if sc > best_score:
			best_score = sc
			best = i
	return best
