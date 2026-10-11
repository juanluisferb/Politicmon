class_name DebugScenarios
extends RefCounted


static func run(name: String) -> void:
	match name:
		"test":
			Router.set_screen(load("res://src/core/test_pattern.gd").new(), "test")
		"battle", "battle_menu", "battle_trainer":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 12), Politician.create(15, 10)]
			GameState.party[0].hp = 21
			var holder := Node.new()
			Router.set_screen(holder, "battle")
			var cfg := {"enemies": [Politician.create(20, 14)], "zone": "galicia", "no_autostart": true}
			if name == "battle_trainer":
				cfg = {"enemies": [Politician.create(12, 23)], "trainer_id": "leader_andalucia", "zone": "andalucia", "no_autostart": true}
			var b: BattleScene = BattleScene.new().setup(cfg)
			var layer := CanvasLayer.new()
			layer.layer = 5
			layer.add_child(b)
			holder.add_child(layer)
			await b.get_tree().process_frame
			b.enemy = cfg["enemies"][0]
			b.enemy_sprite.texture = UiStyle.tex(GameData.sprite_path(b.enemy.species_id, "front"))
			b.enemy_hud.bind(b.enemy)
			b.enemy_hud.show()
			b.player = GameState.party[0]
			b.player_sprite.texture = UiStyle.tex(GameData.sprite_path(b.player.species_id, "back"))
			b.player_hud.bind(b.player)
			b.player_hud.show()
			if name == "battle_menu":
				Ui.say("¿Qué hará Becario de Gabinete?", false)
				Ui.choose(["LUCHAR", "EQUIPO", "OBJETOS", "HUIR"], {"cancel": false, "min_width": 50})
			else:
				Ui.say("¡Un O Retranqueiro salvaje te corta el paso! Depende.", false)
		"party":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 12), Politician.create(15, 10), Politician.create(25, 40), Politician.create(8, 7)]
			GameState.party[1].status = "escandalo"
			GameState.party[3].hp = 0
			Router.set_screen(Node.new(), "menu")
			Ui.push_modal(PartyScreen.new().setup(GameState.party))
		"summary":
			GameState.reset_new_game()
			Router.set_screen(Node.new(), "menu")
			Ui.push_modal(SummaryScreen.new().setup(Politician.create(21, 26)))
		"battle_auto":
			GameState.reset_new_game()
			GameState.party = [Politician.create(3, 30), Politician.create(15, 12)]
			GameState.items = {"carnet_normal": 3, "mitin": 2}
			Router.set_screen(Node.new(), "battle")
			var wins := 0
			for round_i in range(3):
				var foe := Politician.create([20, 7, 13][round_i], 12 + round_i * 4)
				var r: Dictionary = await Router.start_battle({"enemies": [foe], "zone": "galicia"})
				print("battle %d outcome: %s, party: %s" % [round_i, r["outcome"], GameState.party.map(func(p): return "%s L%d %d/%d" % [p.display_name(), p.level, p.hp, p.max_hp()])])
				Ui.set_fade(0.0)
			var t: Dictionary = await Router.start_battle({"enemies": [Politician.create(10, 10), Politician.create(11, 12)], "trainer_id": "t_and_1", "zone": "andalucia"})
			print("trainer outcome: ", t["outcome"], " money ", GameState.money)
			Router.get_tree().quit()
		"battle_lose":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 5), Politician.create(10, 5)]
			Router.set_screen(Node.new(), "battle")
			var r: Dictionary = await Router.start_battle({"enemies": [Politician.create(12, 25)], "zone": "andalucia", "no_run": true})
			print("lose outcome: ", r["outcome"], " money ", GameState.money, " able ", GameState.able_count())
			Router.get_tree().quit()
		"capture":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 10)]
			GameState.items = {"puesto_lista": 5}
			Router.set_screen(Node.new(), "battle")
			var foe := Politician.create(8, 5)
			foe.hp = 1
			var b: BattleScene = BattleScene.new().setup({"enemies": [foe], "zone": "madrid", "no_autostart": true})
			var layer := CanvasLayer.new()
			Router.world.add_child(layer)
			layer.add_child(b)
			await b.get_tree().process_frame
			b.enemy = foe
			b.player = GameState.party[0]
			var caught := false
			for i in range(5):
				caught = await b._throw_carnet("puesto_lista")
				if caught:
					break
			print("caught: ", caught, " party size ", GameState.party.size(), " caught dex ", GameState.caught.keys())
			Router.get_tree().quit()
		"overworld", "overworld_and", "overworld_gal", "garaje", "gym", "sede":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 12)]
			GameState.set_flag("has_starter")
			var spots := {"overworld": ["madrid", Vector2i(11, 20)], "overworld_and": ["andalucia", Vector2i(12, 14)],
				"overworld_gal": ["galicia", Vector2i(18, 20)], "garaje": ["madrid_garaje", Vector2i(3, 4)],
				"gym": ["gym_madrid", Vector2i(4, 11)], "sede": ["sede_madrid", Vector2i(4, 4)]}
			GameState.badges = ["madrid", "andalucia"]
			Router.start_overworld(spots[name][0], spots[name][1], "up")
		"pause":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 12)]
			GameState.set_flag("has_starter")
			Router.start_overworld("madrid", Vector2i(11, 20), "down")
			await Router.get_tree().create_timer(0.5).timeout
			PauseMenu.open(Router.overworld)
		"dex":
			GameState.reset_new_game()
			for i in range(1, 14):
				GameState.mark_seen(i)
				if i % 2 == 1:
					GameState.mark_caught(i)
			Router.set_screen(Node.new(), "menu")
			Ui.push_modal(DexScreen.new())
		"grass":
			GameState.reset_new_game()
			GameState.party = [Politician.create(1, 9), Politician.create(15, 8)]
			GameState.set_flag("has_starter")
			Router.start_overworld("madrid", Vector2i(4, 35), "left")
		"trainer_walk":
			GameState.reset_new_game()
			GameState.party = [Politician.create(2, 16)]
			GameState.set_flag("has_starter")
			Router.start_overworld("madrid", Vector2i(11, 24), "up")
		"gym_run":
			GameState.reset_new_game()
			GameState.party = [Politician.create(3, 30)]
			GameState.set_flag("has_starter")
			Router.start_overworld("gym_madrid", Vector2i(4, 4), "up")
		"title":
			Router.goto_title()
		"title_menu":
			Router.goto_title()
			await Router.get_tree().create_timer(0.3).timeout
			var ev := InputEventAction.new()
			ev.action = "a"
			ev.pressed = true
			Input.parse_input_event(ev)
		"victory":
			GameState.reset_new_game()
			GameState.party = [Politician.create(3, 38), Politician.create(25, 36), Politician.create(16, 30), Politician.create(19, 33)]
			GameState.badges = ["madrid", "andalucia", "galicia"]
			SaveSystem.path = "user://debug_victory_save.json"
			Router.set_screen(load("res://scenes/victory.tscn").instantiate(), "victory")
		"card":
			GameState.reset_new_game()
			GameState.badges = ["madrid", "andalucia"]
			Router.set_screen(Node.new(), "menu")
			Ui.push_modal(PauseMenu.TrainerCard.new())
		"newgame":
			GameState.reset_new_game("ALEX")
			Router.start_overworld("madrid_garaje", Vector2i(4, 5), "up")
		_:
			push_error("unknown scenario " + name)
