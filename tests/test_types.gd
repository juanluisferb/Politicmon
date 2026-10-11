extends TestCase

const ALL := ["populista", "tecnocrata", "mediatico", "burocrata", "activista", "baron", "lobista", "leguleyo", "tuitero", "regionalista"]


func test_ten_types_defined() -> void:
	eq(GameData.types.size(), 10, "type count")
	for t in ALL:
		check(GameData.types.has(t), "type " + t)


func test_triangles() -> void:
	eq(GameData.type_multiplier("populista", ["tecnocrata"]), 2.0, "pop>tec")
	eq(GameData.type_multiplier("tecnocrata", ["mediatico"]), 2.0, "tec>med")
	eq(GameData.type_multiplier("mediatico", ["populista"]), 2.0, "med>pop")
	eq(GameData.type_multiplier("burocrata", ["activista"]), 2.0, "bur>act")
	eq(GameData.type_multiplier("activista", ["baron"]), 2.0, "act>bar")
	eq(GameData.type_multiplier("baron", ["burocrata"]), 2.0, "bar>bur")


func test_inverse_is_half() -> void:
	eq(GameData.type_multiplier("tecnocrata", ["populista"]), 0.5, "tec<pop")
	eq(GameData.type_multiplier("mediatico", ["tecnocrata"]), 0.5, "med<tec")
	eq(GameData.type_multiplier("populista", ["mediatico"]), 0.5, "pop<med")
	eq(GameData.type_multiplier("activista", ["burocrata"]), 0.5, "act<bur")
	eq(GameData.type_multiplier("baron", ["activista"]), 0.5, "bar<act")
	eq(GameData.type_multiplier("burocrata", ["baron"]), 0.5, "bur<bar")


func test_lobista_leguleyo_tuitero() -> void:
	eq(GameData.type_multiplier("lobista", ["baron"]), 2.0, "lob>bar")
	eq(GameData.type_multiplier("lobista", ["burocrata"]), 2.0, "lob>bur")
	eq(GameData.type_multiplier("leguleyo", ["lobista"]), 2.0, "leg>lob")
	eq(GameData.type_multiplier("activista", ["lobista"]), 2.0, "act>lob")
	eq(GameData.type_multiplier("lobista", ["leguleyo"]), 0.5, "lob<leg")
	eq(GameData.type_multiplier("lobista", ["activista"]), 0.5, "lob<act")
	eq(GameData.type_multiplier("leguleyo", ["populista"]), 2.0, "leg>pop")
	eq(GameData.type_multiplier("burocrata", ["leguleyo"]), 2.0, "bur>leg")
	eq(GameData.type_multiplier("leguleyo", ["burocrata"]), 0.5, "leg<bur")
	eq(GameData.type_multiplier("tuitero", ["mediatico"]), 2.0, "tui>med")
	eq(GameData.type_multiplier("tuitero", ["tecnocrata"]), 2.0, "tui>tec")
	eq(GameData.type_multiplier("leguleyo", ["tuitero"]), 2.0, "leg>tui")
	eq(GameData.type_multiplier("tuitero", ["leguleyo"]), 0.5, "tui<leg")


func test_inverse_symmetry_holds_for_whole_chart() -> void:
	for a in GameData.chart:
		for d in GameData.chart[a]:
			var m: float = GameData.chart[a][d]
			if m == 2.0:
				eq(GameData.type_multiplier(d, [a]), 0.5, "inverse of %s>%s" % [a, d])


func test_regionalista_neutral() -> void:
	for t in ALL:
		eq(GameData.type_multiplier("regionalista", [t]), 1.0, "reg->" + t)
		eq(GameData.type_multiplier(t, ["regionalista"]), 1.0, t + "->reg")


func test_dual_types_multiply() -> void:
	eq(GameData.type_multiplier("leguleyo", ["populista", "baron"]), 2.0, "leg vs pop/bar")
	eq(GameData.type_multiplier("tuitero", ["mediatico", "tecnocrata"]), 4.0, "double weak")
	eq(GameData.type_multiplier("activista", ["baron", "regionalista"]), 2.0, "act vs bar/reg")
	eq(GameData.type_multiplier("populista", ["mediatico", "leguleyo"]), 0.25, "double resist")


func test_neutral_default() -> void:
	eq(GameData.type_multiplier("populista", ["activista"]), 1.0, "neutral")
	eq(GameData.type_multiplier("", ["activista"]), 1.0, "typeless")
