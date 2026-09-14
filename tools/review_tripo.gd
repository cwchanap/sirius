extends SceneTree

func _initialize():
	call_deferred("capture")

func capture():
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
	var model=mio.get("Model")
	mio.position=Vector3(0,.02,23)
	model.rotation=Vector3.ZERO
	var animator=model.find_children("*","AnimationPlayer",true,false)[0]
	animator.callback_mode_process=AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=38
	var views=[
		["front","idle",0,Vector3(0,1.25,4.1),Vector3(0,1,0)],
		["face","idle",0,Vector3(.75,1.65,1.75),Vector3(0,1.5,0)],
		["side","idle",0,Vector3(3.9,1.2,.15),Vector3(0,1,0)],
		["back","idle",0,Vector3(0,1.3,-4.1),Vector3(0,1,0)],
		["walk","walk",.35,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["run","run",.3,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["jump","jump",6.0/24,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["attack","attack1",.25,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["guard","guard",.2,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["dodge","dodge",.3,Vector3(2.5,1.5,3.4),Vector3(0,1,0)],
		["skill","skill",.5,Vector3(2.5,1.5,3.4),Vector3(0,1,0)]]
	for view in views:
		assert(animator.has_animation(view[1]))
		animator.play(view[1],0)
		animator.seek(view[2],true)
		camera.position=mio.position+view[3]
		camera.look_at(mio.position+view[4])
		for i in range(5): await process_frame
		RenderingServer.force_draw()
		var result=root.get_texture().get_image().save_png("res://artifacts/tripo-integration/"+view[0]+".png")
		if result!=OK:
			push_error("Capture failed: "+view[0])
			quit(1)
			return
		print("TRIPO_CAPTURE ",view[0])
	print("TRIPO_CAPTURE_OK")
	quit()
