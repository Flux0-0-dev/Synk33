@tool
class_name StyleBoxOutburst
extends StyleBox

## Amount of boil slices to generate
const BOIL_AMOUNT:int = 5


static var random := RandomNumberGenerator.new()
static var random_inital_state: int


@export var body_color:Color = Color.BLACK:
	set(new):
		if body_color != new:
			body_color = new
			emit_changed()
@export var outline_color:Color = Color.WHITE:
	set(new):
		if outline_color != new:
			outline_color = new
			emit_changed()

@export var outline_thickness:float = 4.0:
	set(new):
		if outline_thickness != new:
			outline_thickness = new
			emit_changed()
@export var outerline_thickness:float = 2.0:
	set(new):
		if outerline_thickness != new:
			outerline_thickness = new
			emit_changed()

@export var top_left_offset := Vector2(0,0):
	set(new):
		if top_left_offset != new:
			top_left_offset = new
			emit_changed()
@export var top_right_offset := Vector2(0,0):
	set(new):
		if top_right_offset != new:
			top_right_offset = new
			emit_changed()
@export var bottom_left_offset := Vector2(0,0):
	set(new):
		if bottom_left_offset != new:
			bottom_left_offset = new
			emit_changed()
@export var bottom_right_offset := Vector2(0,0):
	set(new):
		if bottom_right_offset != new:
			bottom_right_offset = new
			emit_changed()
@export var tail_offset := Vector2(0,20):
	set(new):
		if tail_offset != new:
			tail_offset = new
			emit_changed()

@export_group("Boiling")
@export_custom(PROPERTY_HINT_GROUP_ENABLE, "") var boiling: bool = false
## Times per second that a new boil frame is drawn
@export var boil_speed: float = 3:
	set(new):
		if boil_speed != new:
			boil_speed = new
			emit_changed()

@export_custom(PROPERTY_HINT_RANGE, "0,20,1,or_greater") \
		var boil_intensity: Vector2 = Vector2(10, 10):
	set(new):
		if boil_intensity != new:
			boil_intensity = new
			emit_changed()


static func _static_init() -> void:
	random.randomize()
	random_inital_state = random.state

func _draw(canvas_item: RID, rect: Rect2) -> void:
	random.state = random_inital_state
	if not boiling:
		draw_box(
			canvas_item, 
			rect, 
			top_left_offset, 
			top_right_offset, 
			bottom_left_offset,
			bottom_right_offset
		)
		return
	
	var boil_rate := 1.0 / boil_speed
	var animation_length := BOIL_AMOUNT * boil_rate
	
	for i in BOIL_AMOUNT:
		RenderingServer.canvas_item_add_animation_slice(
			canvas_item, animation_length, 0, boil_rate, i * boil_rate
		)
		draw_box(
			canvas_item,
			rect,
			_vector_boil(top_left_offset),
			_vector_boil(top_right_offset),
			_vector_boil(bottom_left_offset),
			_vector_boil(bottom_right_offset)
		)


func draw_box(
		canvas_item: RID, 
		rect: Rect2, 
		box_top_left_offset: Vector2, 
		box_top_right_offset: Vector2,
		box_bottom_left_offset: Vector2,
		box_bottom_right_offset: Vector2
	) -> void:
	var bottom_right := rect.position + Vector2(rect.size.x, rect.size.y) + box_bottom_right_offset
	var bottom_left := rect.position + Vector2(0, rect.size.y) + box_bottom_left_offset
	
	var body_points := PackedVector2Array([
		rect.position + box_top_left_offset,
		rect.position + Vector2(rect.size.x, 0) + box_top_right_offset,
		bottom_right,
		bottom_left.move_toward(bottom_right, 40),
		bottom_left.move_toward(bottom_right, 40) + tail_offset,
		bottom_left.move_toward(bottom_right, 20),
		bottom_left,
	])
	var colors := PackedColorArray()
	colors.resize(len(body_points))
	colors.fill(body_color)
	RenderingServer.canvas_item_add_polygon(
		canvas_item,
		body_points,
		colors
	)
	
	var outerline:Array[PackedVector2Array] = Geometry2D.offset_polygon(
		body_points, outline_thickness / 2.0 + outerline_thickness, Geometry2D.JOIN_MITER
	)
	for polygon in outerline:
		colors.resize(len(polygon))
		colors.fill(body_color)
		polygon.append(polygon[0])
		RenderingServer.canvas_item_add_polyline(
			canvas_item,
			polygon,
			colors,
			outerline_thickness,
			true
		)
	body_points.append(body_points[0])
	colors.resize(len(body_points))
	colors.fill(outline_color)
	RenderingServer.canvas_item_add_polyline(
		canvas_item,
		body_points, 
		colors,
		outline_thickness,
		true
	)


func _vector_boil(base:Vector2) -> Vector2:
	return Vector2(
		random.randf_range(-boil_intensity.x, boil_intensity.x),
		random.randf_range(-boil_intensity.y, boil_intensity.y),
	) + base
