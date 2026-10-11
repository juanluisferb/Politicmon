extends Node

const DEFAULT_PATH := "user://politicmon_save.json"

var path := DEFAULT_PATH


func has_save() -> bool:
	return FileAccess.file_exists(path)


func save_game() -> bool:
	var data := GameState.to_dict()
	data["saved_at"] = Time.get_datetime_string_from_system()
	var f := FileAccess.open(path + ".tmp", FileAccess.WRITE)
	if f == null:
		push_error("Cannot write save: %s" % FileAccess.get_open_error())
		return false
	f.store_string(JSON.stringify(data, "\t"))
	f.close()
	var dir := DirAccess.open(path.get_base_dir())
	if dir == null:
		return false
	if dir.file_exists(path.get_file()):
		dir.remove(path.get_file())
	return dir.rename((path + ".tmp").get_file(), path.get_file()) == OK


func load_game() -> bool:
	var d := read_save()
	if d.is_empty():
		return false
	GameState.from_dict(d)
	return true


func read_save() -> Dictionary:
	if not has_save():
		return {}
	var txt := FileAccess.get_file_as_string(path)
	var json := JSON.new()
	if json.parse(txt) != OK:
		return {}
	var v = json.data
	if not (v is Dictionary) or not v.has("party"):
		return {}
	return v


func delete_save() -> void:
	if has_save():
		DirAccess.remove_absolute(ProjectSettings.globalize_path(path))
