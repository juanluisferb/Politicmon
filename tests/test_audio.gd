extends TestCase

const SRC_DIRS := ["res://src/core/", "res://src/battle/", "res://src/overworld/", "res://src/ui/", "res://src/save/"]


func _scan(regex: String) -> Dictionary:
	var re := RegEx.new()
	re.compile(regex)
	var found := {}
	for d in SRC_DIRS:
		for f in DirAccess.get_files_at(d):
			if not f.ends_with(".gd"):
				continue
			var txt := FileAccess.get_file_as_string(d + f)
			for m in re.search_all(txt):
				found[m.get_string(1)] = d + f
	return found


func test_sfx_referenced_in_code_exist() -> void:
	var names := _scan("sfx(?:_wait)?\\(\"([a-z_]+)\"\\)")
	check(names.size() > 10, "found sfx references")
	for n in names:
		check(ResourceLoader.exists("res://assets/audio/sfx_%s.wav" % n), "sfx %s (%s)" % [n, names[n]])


func test_music_referenced_exist() -> void:
	var names := _scan("play_music\\(\"([a-z_]+)\"\\)")
	names.merge(_scan("\"music\": \"([a-z_]+)\""))
	for id in ["madrid", "andalucia", "galicia", "madrid_garaje", "hemiciclo", "sede_madrid", "gym_madrid"]:
		names[GameData.map_data(id)["music"]] = id
	for n in ["battle", "gym", "legend", "victory", "title", "ending", "evolution", "badge", "encounter"]:
		names[n] = "required"
	for n in names:
		check(ResourceLoader.exists("res://assets/audio/music_%s.wav" % n), "music %s (%s)" % [n, names[n]])


func test_music_streams_load_and_loop() -> void:
	Audio.play_music("madrid")
	var s: AudioStream = Audio.music_player.stream
	check(s is AudioStreamWAV, "wav stream")
	if s is AudioStreamWAV:
		eq(s.loop_mode, AudioStreamWAV.LOOP_FORWARD, "loops")
		check(s.loop_end > 22050 * 5, "loop longer than 5 s")
		check(s.get_length() < 30.0, "short loop")
	Audio.stop_music()
