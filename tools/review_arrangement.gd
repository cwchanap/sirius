extends SceneTree

func _initialize():
	call_deferred("capture")

func capture():
	DirAccess.make_dir_recursive_absolute("res://artifacts/arrangement-after")
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
	var mio=game.get_node("Mio")
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.far=400
	var world=game.get("World")
	var shelter=world.get_node("Cedar Pass Shelter").position
	var overlook=world.get_node("Valley Overlook").position
	# Traversal is verified by IntegrationCheck; this script only records inspection views.
	var views=[
		["shelter-entry",shelter+Vector3(5,2,5),shelter+Vector3(0,.7,.7),shelter+Vector3(0,.03,.5)],
		["overlook-entry",overlook+Vector3(5,2.5,5),overlook+Vector3(0,.8,.3),overlook+Vector3(0,.03,0)],
		["orchard-access",Vector3(19,6.4,-253.8),Vector3(29,5,-251)],
		["garden-grounding",Vector3(-42,5,-239),Vector3(-32,4.5,-251)],
		["komorebi-top",Vector3(0,80,-253),Vector3(.1,4.5,-254)],
		["hoshikawa-top",Vector3(0,75,23),Vector3(.1,0,22)],
		["rice-grounding",Vector3(42,2,19),Vector3(30,.5,30)],
		["komorebi-services",Vector3(0,6.5,-238),Vector3(0,6,-250)],
		["overlook-view",overlook+Vector3(0,1.7,0),Vector3(6,3,-208)]]
	for view in views:
		mio.visible=view.size()==4
		if mio.visible: mio.position=view[3]
		camera.position=view[1]
		camera.look_at(view[2])
		for i in range(5): await process_frame
		RenderingServer.force_draw()
		var result=root.get_texture().get_image().save_png("res://artifacts/arrangement-after/"+view[0]+".png")
		if result!=OK:
			push_error("Capture failed: "+view[0])
			quit(1)
			return
		print("ARRANGEMENT_CAPTURE ",view[0])
	print("ARRANGEMENT_CAPTURE_OK")
	quit()
