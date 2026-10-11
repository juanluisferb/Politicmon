class_name UiStyle
extends RefCounted

const FONT: Font = preload("res://assets/fonts/pixel8.fnt")
const FRAME: Texture2D = preload("res://assets/ui/frame.png")
const CURSOR: Texture2D = preload("res://assets/ui/cursor.png")
const ARROW_DOWN: Texture2D = preload("res://assets/ui/arrow_down.png")

const TEXT := Color("#101820")
const TEXT_LIGHT := Color("#586878")
const PAPER := Color("#f8f8f0")
const LINE_H := 10


static func panel(rect: Rect2) -> NinePatchRect:
	var p := NinePatchRect.new()
	p.texture = FRAME
	p.patch_margin_left = 8
	p.patch_margin_right = 8
	p.patch_margin_top = 8
	p.patch_margin_bottom = 8
	p.position = rect.position
	p.size = rect.size
	p.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return p


static func label(text: String, pos: Vector2 = Vector2.ZERO, color: Color = TEXT) -> Label:
	var l := Label.new()
	l.text = text
	l.position = pos
	l.add_theme_font_override("font", FONT)
	l.add_theme_font_size_override("font_size", 8)
	l.add_theme_color_override("font_color", color)
	l.add_theme_constant_override("line_spacing", 2)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l


static func text_width(text: String) -> float:
	return FONT.get_string_size(text, HORIZONTAL_ALIGNMENT_LEFT, -1, 8).x


static func wrap(text: String, max_width: float) -> PackedStringArray:
	var out := PackedStringArray()
	for paragraph in text.split("\n"):
		var line := ""
		for word in paragraph.split(" ", false):
			var candidate := word if line.is_empty() else line + " " + word
			if text_width(candidate) <= max_width or line.is_empty():
				line = candidate
			else:
				out.append(line)
				line = word
		out.append(line)
	return out


static func tex(path: String) -> Texture2D:
	if ResourceLoader.exists(path):
		return load(path)
	return null
