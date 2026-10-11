class_name TestCase
extends RefCounted

var failures: Array[String] = []
var asserts := 0


func check(cond: bool, msg: String = "") -> void:
	asserts += 1
	if not cond:
		failures.append(msg if msg != "" else "assertion failed")


func eq(a: Variant, b: Variant, msg: String = "") -> void:
	asserts += 1
	if typeof(a) != typeof(b) and not (typeof(a) in [TYPE_INT, TYPE_FLOAT] and typeof(b) in [TYPE_INT, TYPE_FLOAT]):
		failures.append("%s: expected %s (%s) got %s (%s)" % [msg, str(b), type_string(typeof(b)), str(a), type_string(typeof(a))])
	elif a != b:
		failures.append("%s: expected %s got %s" % [msg, str(b), str(a)])


func near(a: float, b: float, eps: float = 0.0001, msg: String = "") -> void:
	asserts += 1
	if absf(a - b) > eps:
		failures.append("%s: expected ~%s got %s" % [msg, str(b), str(a)])
