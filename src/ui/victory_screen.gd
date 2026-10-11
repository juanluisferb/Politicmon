extends Control

const CREDITS := [
	"¡HAS GANADO LAS ELECCIONES!",
	"",
	"El Partido Nuevo, fundado en un garaje,",
	"gobierna ahora con mayoría absoluta.",
	"",
	"Escaños conseguidos: 3 de 3",
	"",
	"— CRÉDITOS —",
	"",
	"Diseño, código y datos",
	"generados para Politicmon",
	"",
	"Pixel art Game Boy Color",
	"dibujado píxel a píxel por script",
	"",
	"Música chiptune",
	"cuadradas y ruido, por script",
	"",
	"Motor: Godot 4",
	"",
	"Todos los personajes y partidos",
	"son ficticios. Ninguno representa",
	"a personas ni partidos reales.",
	"",
	"Gracias por jugar.",
	"Y recuerda: vota siempre.",
]

var scroller: Control
var bg: ColorRect
var done := false
var t := 0.0
var end_label: Label


func _ready() -> void:
	bg = ColorRect.new()
	bg.color = Color("#101820")
	add_child(bg)
	scroller = Control.new()
	add_child(scroller)
	var y := 0.0
	var full := Vector2(ScreenScaler.logical_size)
	var stats := "Afiliados en la Politidex: %d · Tiempo: %s" % [GameState.caught.size(), GameState.time_string()]
	var lines: Array = CREDITS.duplicate()
	lines.insert(6, stats)
	for line in lines:
		for sub in UiStyle.wrap(line, full.x - 16):
			var l := UiStyle.label(sub, Vector2(floorf((full.x - UiStyle.text_width(sub)) / 2.0), y), Color("#f8f0d0"))
			scroller.add_child(l)
			y += 12
	y += 12
	var x := 8.0
	for p in GameState.party:
		var r := TextureRect.new()
		r.texture = UiStyle.tex(GameData.sprite_path(p.species_id, "front"))
		r.position = Vector2(x, y)
		scroller.add_child(r)
		x += 58
		if x > full.x - 56:
			x = 8
			y += 58
	scroller.position = Vector2(0, full.y)
	scroller.size = Vector2(full.x, y + 70)
	end_label = UiStyle.label("FIN · Pulsa A", Vector2.ZERO, Color("#f8e8a0"))
	end_label.position = Vector2(floorf((full.x - UiStyle.text_width(end_label.text)) / 2.0), full.y - ScreenScaler.safe_margins.size.y - 20)
	end_label.hide()
	add_child(end_label)
	bg.size = full
	Audio.play_music("ending")
	GameState.set_flag("game_completed")
	SaveSystem.save_game()


func _process(delta: float) -> void:
	t += delta
	var full := Vector2(ScreenScaler.logical_size)
	var target := minf(full.y * 0.1, full.y - scroller.size.y)
	if scroller.position.y > target:
		scroller.position.y = maxf(target, full.y - t * 18.0)
	else:
		done = true
	end_label.visible = done and fmod(t, 1.0) < 0.7


func _input(event: InputEvent) -> void:
	if event.is_action_pressed("a") or (event is InputEventMouseButton and event.pressed):
		if done:
			Router.goto_title()
		else:
			t += 4.0
