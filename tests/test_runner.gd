extends Node

const TEST_DIR := "res://tests/"


func _ready() -> void:
	await get_tree().process_frame
	var total := 0
	var failed := 0
	var files: Array[String] = []
	for f in DirAccess.get_files_at(TEST_DIR):
		if f.begins_with("test_") and f.ends_with(".gd") and f != "test_runner.gd" and f != "test_case.gd":
			files.append(f)
	files.sort()
	var only := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--only="):
			only = a.substr(7)
	for f in files:
		if only != "" and not f.contains(only):
			continue
		var script: GDScript = load(TEST_DIR + f)
		for m in script.get_script_method_list():
			var name: String = m["name"]
			if not name.begins_with("test_"):
				continue
			var t: TestCase = script.new()
			if t.has_method("before_each"):
				t.before_each()
			var r = t.call(name)
			if r is Object and r.has_signal("completed"):
				await r
			total += 1
			if t.failures.is_empty():
				print("  ok   %s::%s (%d)" % [f, name, t.asserts])
			else:
				failed += 1
				print("  FAIL %s::%s" % [f, name])
				for msg in t.failures:
					print("        - " + msg)
			if t.has_method("after_each"):
				t.after_each()
	print("\n%d tests, %d failed" % [total, failed])
	get_tree().quit(1 if failed > 0 else 0)
