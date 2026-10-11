extends TestCase


func test_mitin_heals_up_to_max() -> void:
	var p := Politician.create(9, 20)
	check(not ItemEffects.can_use("mitin", p), "full hp cannot use mitin")
	p.hp = 5
	check(ItemEffects.can_use("mitin", p), "can heal")
	ItemEffects.apply("mitin", p)
	eq(p.hp, mini(p.max_hp(), 55), "heals 50")
	p.hp = 0
	check(not ItemEffects.can_use("mitin", p), "cannot heal dimitido")


func test_factcheck_only_cures_escandalo() -> void:
	var p := Politician.create(9, 20)
	p.status = "imputado"
	check(not ItemEffects.can_use("factcheck", p), "not for imputado")
	p.status = "escandalo"
	check(ItemEffects.can_use("factcheck", p), "cures escandalo")
	ItemEffects.apply("factcheck", p)
	eq(p.status, "", "cured")


func test_inventory() -> void:
	GameState.reset_new_game()
	eq(GameState.item_count("carnet_normal"), 5, "starting carnets")
	GameState.add_item("encuesta", 2)
	check(GameState.remove_item("encuesta"), "remove one")
	eq(GameState.item_count("encuesta"), 1, "one left")
	check(GameState.remove_item("encuesta"), "remove last")
	check(not GameState.items.has("encuesta"), "erased at zero")
	check(not GameState.remove_item("encuesta"), "cannot go negative")


func test_party_limit_sends_to_box() -> void:
	GameState.reset_new_game()
	for i in range(6):
		eq(GameState.add_politician(Politician.create(1 + i, 5)), "party", "slot %d" % i)
	eq(GameState.add_politician(Politician.create(9, 5)), "box", "7th goes to box")
	eq(GameState.party.size(), 6, "party max 6")
	eq(GameState.box.size(), 1, "box")
	check(GameState.is_caught(9), "registered in politidex")
