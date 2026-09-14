extends SceneTree

func _initialize():
	call_deferred("review")

func review():
	DirAccess.make_dir_recursive_absolute("res://artifacts/countryside")
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	for i in range(15): await process_frame
	game.set_process(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: node.visible=false
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.far=400
	camera.fov=55
	var views=[
		["komorebi-overview",Vector3(33,29,-119),Vector3(0,7,-156),Vector3(0,4.6,-136)],
		["cedar-bridge",Vector3(32,19,-91),Vector3(9,4,-112),Vector3(10,4.2,-104)],
		["pass-shelter",Vector3(4,12,-76),Vector3(-9,5,-91),Vector3(-12,5,-87)],
		["valley-overlook",Vector3(42,14,-114),Vector3(26,5,-127),Vector3(29,7,-123)],
		["komorebi-square",Vector3(10,11,-137),Vector3(0,6,-158),Vector3(0,4.6,-148)],
		["original-village",Vector3(30,23,53),Vector3(0,2,20),Vector3(0,.1,31)]]
	for view in views:
		var mio=game.get_node("Mio")
		mio.position=view[3]
		mio.velocity=Vector3.ZERO
		mio.set_physics_process(true)
		for i in range(40): await physics_frame
		mio.set_physics_process(false)
		camera.position=view[1]
		camera.look_at(view[2])
		for i in range(5): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png("res://artifacts/countryside/"+view[0]+".png")==OK)
		print("VALLEY_CAPTURE ",view[0])
	print("VALLEY_REVIEW_OK")
	quit()
