extends TestCase

var old_path := ""


func before_each() -> void:
	old_path = SaveSystem.path
	SaveSystem.path = "user://test_save_%d.json" % Time.get_ticks_usec()


func after_each() -> void:
	SaveSystem.delete_save()
	SaveSystem.path = old_path


func test_roundtrip_full_state() -> void:
	GameState.reset_new_game("NOA")
	var a := Politician.create(19, 31)
	a.hp = 12
	a.status = "imputado"
	a.moves[1]["pp"] = 3
	GameState.add_politician(a)
	GameState.add_politician(Politician.create(8, 9))
	GameState.box.append(Politician.create(22, 25))
	GameState.add_item("puesto_lista", 4)
	GameState.money = 4321
	GameState.add_badge("madrid")
	GameState.add_badge("andalucia")
	GameState.set_flag("has_starter")
	GameState.set_flag("item:mad_item1")
	GameState.defeated_trainers["t_mad_1"] = true
	GameState.mark_seen(5)
	GameState.map_id = "galicia"
	GameState.pos = Vector2i(17, 22)
	GameState.facing = "left"
	GameState.respawn = {"map": "sede_galicia", "x": 4, "y": 4}
	GameState.play_time = 1234.5
	check(SaveSystem.save_game(), "saved")
	check(SaveSystem.has_save(), "file exists")
	GameState.reset_new_game("OTRO")
	check(SaveSystem.load_game(), "loaded")
	eq(GameState.player_name, "NOA", "name")
	eq(GameState.party.size(), 2, "party size")
	eq(GameState.party[0].species_id, 19, "species")
	eq(GameState.party[0].level, 31, "level")
	eq(GameState.party[0].hp, 12, "hp")
	eq(GameState.party[0].status, "imputado", "status")
	eq(GameState.party[0].moves[1]["pp"], 3, "pp")
	eq(GameState.box.size(), 1, "box")
	eq(GameState.item_count("puesto_lista"), 4, "items")
	eq(GameState.item_count("carnet_normal"), 5, "starting items kept")
	eq(GameState.money, 4321, "money")
	eq(GameState.badges, ["madrid", "andalucia"], "badges")
	check(GameState.flag("has_starter") and GameState.flag("item:mad_item1"), "flags")
	check(GameState.trainer_defeated("t_mad_1"), "trainers")
	check(GameState.is_caught(19) and GameState.is_seen(5) and not GameState.is_caught(5), "politidex")
	eq(GameState.map_id, "galicia", "map")
	eq(GameState.pos, Vector2i(17, 22), "pos")
	eq(GameState.facing, "left", "facing")
	eq(int(GameState.respawn["x"]), 4, "respawn")
	near(GameState.play_time, 1234.5, 0.01, "time")


func test_missing_or_corrupt_save() -> void:
	check(not SaveSystem.has_save(), "no save")
	check(not SaveSystem.load_game(), "load fails without file")
	var f := FileAccess.open(SaveSystem.path, FileAccess.WRITE)
	f.store_string("{esto no es json")
	f.close()
	check(not SaveSystem.load_game(), "corrupt save rejected")


func test_overwrite_keeps_single_file() -> void:
	GameState.reset_new_game("A")
	check(SaveSystem.save_game(), "first save")
	GameState.money = 7
	check(SaveSystem.save_game(), "second save")
	eq(int(SaveSystem.read_save()["money"]), 7, "latest data")
	check(not FileAccess.file_exists(SaveSystem.path + ".tmp"), "no temp file left")
