extends SceneTree

func _initialize():
	call_deferred("capture")

func capture():
	var output="res://artifacts/chiyo-integration/"
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
	var chiyo=game.find_child("Chiyo",true,false)
	var animator=chiyo.find_children("*","AnimationPlayer",true,false)[0]
	animator.callback_mode_process=AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	animator.play("idle",0)
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=38
	for view in [["front",Vector3(0,1.1,3.5),Vector3(0,.9,0)],
		["face",Vector3(.25,1.45,1.25),Vector3(0,1.37,0)]]:
		animator.seek(1,true)
		camera.position=chiyo.to_global(view[1])
		camera.look_at(chiyo.to_global(view[2]))
		for i in range(4): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png(output+view[0]+".png")==OK)
	var location=chiyo.transform
	# Unobstructed street views for the rear/profile, away from the assembly wall.
	chiyo.position=Vector3(0,0,16)
	chiyo.rotation=Vector3.ZERO
	for view in [["side",Vector3(3.4,1.1,0)],["back",Vector3(0,1.1,-3.4)]]:
		camera.position=chiyo.position+view[1]
		camera.look_at(chiyo.position+Vector3(0,.85,0))
		for i in range(4): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png(output+view[0]+".png")==OK)
	chiyo.transform=location
	var player=game.get_node("Mio")
	player.position=chiyo.position+Vector3(1.7,.05,1)
	player.get("Model").rotation.y=-2.1
	game.call("Interact")
	assert(game.get("Speaker")=="Chiyo")
	for node in game.get_children():
		if node is CanvasLayer: node.visible=true
	camera.position=chiyo.position+Vector3(3.6,2.2,4.8)
	camera.look_at(chiyo.position+Vector3(.4,.65,0))
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(output+"dialogue.png")==OK)
	print("CHIYO_CAPTURE_OK: front, face, side, back, actual village dialogue")
	quit()
