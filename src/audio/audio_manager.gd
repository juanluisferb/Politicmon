extends Node

const MUSIC_DIR := "res://assets/audio/music_%s.wav"
const SFX_DIR := "res://assets/audio/sfx_%s.wav"

var music_player: AudioStreamPlayer
var sfx_players: Array[AudioStreamPlayer] = []
var current_music := ""
var _cache: Dictionary = {}
var music_volume_db := -6.0
var muted := false


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	music_player = AudioStreamPlayer.new()
	music_player.volume_db = music_volume_db
	add_child(music_player)
	for i in range(4):
		var p := AudioStreamPlayer.new()
		p.volume_db = -4.0
		add_child(p)
		sfx_players.append(p)


func _load(path: String, loop: bool) -> AudioStream:
	if _cache.has(path):
		return _cache[path]
	if not ResourceLoader.exists(path):
		_cache[path] = null
		return null
	var s = load(path)
	if s is AudioStreamWAV and loop:
		s = s.duplicate()
		s.loop_mode = AudioStreamWAV.LOOP_FORWARD
		s.loop_begin = 0
		s.loop_end = int(round(s.get_length() * s.mix_rate))
	_cache[path] = s
	return s


func play_music(name: String) -> void:
	if name == current_music and music_player.playing:
		return
	current_music = name
	var s := _load(MUSIC_DIR % name, true)
	music_player.stop()
	if s and not muted:
		music_player.stream = s
		music_player.play()


func stop_music() -> void:
	current_music = ""
	music_player.stop()


func sfx(name: String) -> void:
	if muted:
		return
	var s := _load(SFX_DIR % name, false)
	if s == null:
		return
	for p in sfx_players:
		if not p.playing:
			p.stream = s
			p.play()
			return
	sfx_players[0].stream = s
	sfx_players[0].play()


func sfx_wait(name: String) -> void:
	var s := _load(SFX_DIR % name, false)
	if s == null or muted:
		return
	sfx(name)
	await get_tree().create_timer(s.get_length()).timeout


func set_muted(m: bool) -> void:
	muted = m
	if m:
		music_player.stop()
	elif current_music != "":
		var n := current_music
		current_music = ""
		play_music(n)
