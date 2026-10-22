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
		["komorebi-overview",Vector3(33,29,-219),Vector3(0,7,-256),Vector3(0,4.6,-236)],
		["cedar-bridge",Vector3(32,19,-191),Vector3(9,4,-212),Vector3(10,4.2,-204)],
		["pass-shelter",Vector3(4,12,-176),Vector3(-9,5,-191),Vector3(-12,5,-187)],
		["valley-overlook",Vector3(42,14,-214),Vector3(26,5,-227),Vector3(29,7,-223)],
		["komorebi-square",Vector3(10,11,-237),Vector3(0,6,-258),Vector3(0,4.6,-248)],
		["original-village",Vector3(30,23,53),Vector3(0,2,20),Vector3(0,.1,31)],
		["relay-junction",Vector3(2,4.5,-9),Vector3(8,5,-46),Vector3(1,1,-22)],
		["relay-switchback",Vector3(22,18,-18),Vector3(40,4,-42),Vector3(44,3.4,-34)],
		["relay-teahouse",Vector3(25,10.5,-50),Vector3(10,8,-58),Vector3(21,7.6,-54)],
		["relay-high-road",Vector3(29,11,-66),Vector3(24,9,-112),Vector3(28,8,-80)],
		["relay-forest-valley",Vector3(-2,5,-34),Vector3(-12,3,-62),Vector3(-8,1.5,-42)],
		["relay-quarry",Vector3(-20,9,-120),Vector3(-38,4.5,-135),Vector3(-34,4.4,-132)],
		["relay-plaza",Vector3(10,9,-146),Vector3(-3,6,-170),Vector3(1,3,-162)],
		["relay-aerial",Vector3(70,130,-30),Vector3(0,0,-100),Vector3(0,1,-30)]]
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
