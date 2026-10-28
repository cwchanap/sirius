extends SceneTree

func _initialize():
	call_deferred("capture")

func shot(camera,from,to,path):
	camera.position=from
	camera.look_at(to)
	for i in range(8): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(path)==OK)

func capture():
	var output="res://artifacts/drones/"
	DirAccess.make_dir_recursive_absolute(output)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	game.set_process_unhandled_input(false)
	game.set_process(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: node.visible=false
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=45
	var drone=game.get_node("Drone_2")
	var mio=game.get_node("Mio")
	var at=drone.global_position
	var side=Vector3(1,0,.4).normalized()
	drone.get("Model").rotation.y=atan2(side.x,side.z)
	await shot(camera,at+side*2.6+Vector3(0,1.5,0),at+Vector3(0,1.25,0),output+"close.png")
	await shot(camera,at+side.rotated(Vector3.UP,-1.4)*2.8+Vector3(0,1.4,0),at+Vector3(0,1.2,0),output+"side.png")
	# Beside Mio, for its size, and in the forest valley it patrols.
	mio.global_position=at+side*1.8
	mio.get("Model").rotation.y=atan2(-side.x,-side.z)
	await shot(camera,at+side*.9+side.rotated(Vector3.UP,-1.57)*4.5+Vector3(0,1.6,0),at+side*.9+Vector3(0,1.1,0),output+"with-mio.png")
	await shot(camera,at+side*9+Vector3(0,4.5,0),at+Vector3(0,1,0),output+"valley.png")
	mio.global_position=at+side*12
	drone.call("Execute")
	for i in range(60): await process_frame
	await shot(camera,at+side*2.6+Vector3(0,1.6,0),at+Vector3(0,.3,0),output+"down.png")
	print("DRONE_CAPTURE_OK: close, side, with-mio, valley, down")
	quit()
