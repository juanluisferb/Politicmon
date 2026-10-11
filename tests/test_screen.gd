extends TestCase


func test_integer_scale_for_common_phones() -> void:
	eq(ScreenScaler.compute_scale(Vector2i(1080, 1920)), 6, "1080x1920")
	eq(ScreenScaler.compute_scale(Vector2i(1170, 2532)), 7, "1170x2532")
	eq(ScreenScaler.compute_scale(Vector2i(720, 1600)), 4, "720x1600")
	eq(ScreenScaler.compute_scale(Vector2i(100, 100)), 1, "tiny")


func test_logical_size_fills_screen() -> void:
	for res in [Vector2i(1080, 1920), Vector2i(1170, 2532), Vector2i(720, 1600), Vector2i(1536, 2048)]:
		var s := ScreenScaler.compute_scale(res)
		var l := ScreenScaler.compute_logical_size(res, s)
		check(res.x - l.x * s < s and res.y - l.y * s < s, "leftover smaller than one logical pixel for %s" % res)
		check(l.x >= 160 and l.y >= 256, "min logical size for %s" % res)
