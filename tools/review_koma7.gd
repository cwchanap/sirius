extends SceneTree

func _initialize():
	call_deferred("capture")

func capture():
	var output="res://artifacts/koma7-integration/"
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
	var koma=game.find_child("KOMA-7",true,false)
	var labels=koma.find_children("*","Label3D",true,false)
	for label in labels: label.visible=false
	var animator=koma.find_children("*","AnimationPlayer",true,false)[0]
	animator.callback_mode_process=AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	animator.play("idle",0)
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=38
	for view in [["front",Vector3(0,.85,2.85),Vector3(0,.64,0),0.0],
		["face",Vector3(0,1.14,1.05),Vector3(0,1.04,0),0.0],
		["idle",Vector3(0,1.14,1.05),Vector3(0,1.04,0),1.5]]:
		animator.seek(view[3],true)
		camera.position=koma.to_global(view[1])
		camera.look_at(koma.to_global(view[2]))
		for i in range(4): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png(output+view[0]+".png")==OK)
	var location=koma.transform
	# Review the tank and silhouette in the street, clear of nearby buildings.
	var query=PhysicsRayQueryParameters3D.create(Vector3(0,10,16),Vector3(0,-10,16),1)
	var ground=koma.get_world_3d().direct_space_state.intersect_ray(query)
	assert(not ground.is_empty())
	koma.position=ground.position
	koma.rotation=Vector3.ZERO
	animator.seek(0,true)
	for view in [["side",Vector3(2.85,.85,0)],["back",Vector3(0,.85,-2.85)]]:
		camera.position=koma.position+view[1]
		camera.look_at(koma.position+Vector3(0,.64,0))
		for i in range(4): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png(output+view[0]+".png")==OK)
	koma.transform=location
	for label in labels: label.visible=true
	var player=game.get_node("Mio")
	player.position=koma.to_global(Vector3(1.4,.05,1))
	player.get("Model").rotation.y=koma.rotation.y-2.1
	game.call("Interact")
	assert(game.get("Speaker")=="KOMA-7")
	for node in game.get_children():
		if node is CanvasLayer: node.visible=true
	camera.position=koma.to_global(Vector3(3.2,1.9,3.8))
	camera.look_at(koma.to_global(Vector3(.4,.6,0)))
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(output+"dialogue.png")==OK)
	print("KOMA7_CAPTURE_OK: front, face, idle, side, back, actual village dialogue")
	quit()
