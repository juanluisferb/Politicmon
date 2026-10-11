class_name BattleHud
extends Control

const STATUS_TAG := {"escandalo": "ESC", "imputado": "IMP"}

var mon: Politician
var is_player := false
var name_label: Label
var level_label: Label
var hp_label: Label
var status_label: Label
var hp_bar: HpBar
var exp_bar: HpBar
var shown_hp := 0.0


func _init(player_side: bool) -> void:
	is_player = player_side
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	size = Vector2(98, 34 if player_side else 26)


func _ready() -> void:
	add_child(UiStyle.panel(Rect2(Vector2.ZERO, size)))
	name_label = UiStyle.label("", Vector2(6, 5))
	add_child(name_label)
	level_label = UiStyle.label("", Vector2(0, 5))
	level_label.size.x = size.x - 6
	level_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	add_child(level_label)
	add_child(UiStyle.label("PS", Vector2(6, 14), UiStyle.TEXT_LIGHT))
	hp_bar = HpBar.new(int(size.x) - 26)
	hp_bar.position = Vector2(19, 16)
	add_child(hp_bar)
	status_label = UiStyle.label("", Vector2(0, 5), HpBar.LOW)
	add_child(status_label)
	if is_player:
		hp_label = UiStyle.label("", Vector2(0, 21))
		hp_label.size.x = size.x - 8
		hp_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		add_child(hp_label)
		exp_bar = HpBar.new(int(size.x) - 26, true)
		exp_bar.position = Vector2(19, 31)
		add_child(exp_bar)
		add_child(UiStyle.label("EX", Vector2(6, 26), UiStyle.TEXT_LIGHT))
		exp_bar.position = Vector2(19, 28)


func bind(p: Politician) -> void:
	mon = p
	shown_hp = p.hp
	refresh()


func refresh() -> void:
	if mon == null:
		return
	var n := mon.display_name()
	if UiStyle.text_width(n) > size.x - 34:
		while UiStyle.text_width(n + ".") > size.x - 34 and n.length() > 3:
			n = n.substr(0, n.length() - 1)
		n += "."
	name_label.text = n
	level_label.text = "Nv%d" % mon.level
	var tag: String = STATUS_TAG.get(mon.status, "")
	if mon.campaign_turns > 0 and tag == "":
		tag = "CAMP"
	status_label.text = tag
	status_label.position.x = 6 + UiStyle.text_width(n) + 4
	hp_bar.set_ratio(shown_hp / float(mon.max_hp()))
	if hp_label:
		hp_label.text = "%d/%d" % [int(round(shown_hp)), mon.max_hp()]
		exp_bar.set_ratio(mon.exp_progress())


func animate_hp(target: int, duration: float = 0.5) -> void:
	var start := shown_hp
	var t := 0.0
	var dur := maxf(0.15, duration * absf(target - start) / maxf(1.0, float(mon.max_hp())) * 1.5)
	while t < dur:
		await get_tree().process_frame
		t += get_process_delta_time()
		shown_hp = lerpf(start, float(target), clampf(t / dur, 0.0, 1.0))
		refresh()
	shown_hp = target
	refresh()


func animate_exp(from_ratio: float, to_ratio: float, duration: float = 0.5) -> void:
	if exp_bar == null:
		return
	var t := 0.0
	while t < duration:
		await get_tree().process_frame
		t += get_process_delta_time()
		exp_bar.set_ratio(lerpf(from_ratio, to_ratio, clampf(t / duration, 0.0, 1.0)))
