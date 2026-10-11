class_name ItemEffects
extends RefCounted


static func can_use(id: String, p: Politician) -> bool:
	var it := GameData.item(id)
	match it.get("kind", ""):
		"heal":
			return not p.is_fainted() and p.hp < p.max_hp()
		"cure":
			return not p.is_fainted() and p.status == it.get("status", "")
	return false


static func apply(id: String, p: Politician) -> String:
	var it := GameData.item(id)
	match it.get("kind", ""):
		"heal":
			var before := p.hp
			p.hp = mini(p.max_hp(), p.hp + int(it.get("amount", 20)))
			return "¡%s recupera %d PS de Carisma!" % [p.display_name(), p.hp - before]
		"cure":
			p.status = ""
			return "¡Bulo desmentido! %s ya no está en un Escándalo." % p.display_name()
	return "No ha tenido efecto."
