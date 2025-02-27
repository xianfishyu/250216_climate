extends Camera2D

var _scale = 1.0
@export var scaleFactor = 0.125
@export var minScale = 0.125
@export var maxScale = 4.0

var moveInput = Vector2.ZERO
var nextPos = Vector2.ZERO
@export var keyMoveFactor = 10.0
@export var moveSpeed = 1.25

var mousePos = Vector2.ZERO

func _ready():
	nextPos = position

func _process(delta):
	_key_pos_update()
	_scale_update()
	_mouse_pos_update()

func _key_pos_update():
	nextPos += pow(2, -_scale) * keyMoveFactor * moveSpeed * moveInput
	position = position.lerp(nextPos, 0.1)

func _scale_update():
	zoom = zoom.lerp(Vector2(_scale, _scale), 0.1)

func _mouse_pos_update():
	if Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE):
		var deltaPos = mousePos - get_global_mouse_position()
		position += deltaPos
		nextPos = position

func _input(event):
	# WASD
	if not Input.is_mouse_button_pressed(MOUSE_BUTTON_MIDDLE):
		moveInput = Input.get_vector("KeyBoard_MoveLeft", "KeyBoard_MoveRight", "KeyBoard_MoveUp", "KeyBoard_MoveDown")

	# MouseWheel
	if event is InputEventMouseButton and event.pressed:
		var mouseEvent = event as InputEventMouseButton
		match mouseEvent.button_index:
			MOUSE_BUTTON_WHEEL_UP:
				_scale += scaleFactor * _scale
				_scale = min(max(_scale, minScale), maxScale)
			MOUSE_BUTTON_WHEEL_DOWN:
				_scale -= scaleFactor * _scale
				_scale = min(max(_scale, minScale), maxScale)
			MOUSE_BUTTON_MIDDLE:
				mousePos = get_global_mouse_position()
