extends SceneTree

# Usage: $GODOT_MONO --path . --script res://tools/review_hamlet.gd [-- --out=subdir] [--only=name,name]
func _initialize():
	call_deferred("review")

func review():
	var out="res://artifacts/hamlet"
	var only=[]
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="): out+="/"+arg.substr(6)
		if arg.begins_with("--only="): only=arg.substr(7).split(",")
	DirAccess.make_dir_recursive_absolute(out)
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
		["square",Vector3(9,7,34),Vector3(-3,1,22),Vector3(1,.1,27)],
		["workshop",Vector3(3,5.5,1),Vector3(-11,2,10),Vector3(-3,.1,12)],
		["east-row",Vector3(-5,6,16),Vector3(12,2,30),Vector3(3,.1,20)],
		["brook-bridge",Vector3(27,4.5,7),Vector3(19.5,0,15),Vector3(15,.1,15)],
		["shrine-approach",Vector3(3,4,43),Vector3(-1,3,62),Vector3(-.5,.1,48)],
		["terrace",Vector3(-15,4.5,53),Vector3(-21,1,30),Vector3(-26,1.3,40)],
		["overview",Vector3(32,26,-8),Vector3(0,0,28),Vector3(0,.1,31)]]
	for view in views:
		if only.size()>0 and not only.has(view[0]): continue
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
		assert(root.get_texture().get_image().save_png(out+"/"+view[0]+".png")==OK)
		print("HAMLET_CAPTURE ",view[0])
	print("HAMLET_REVIEW_OK")
	quit()
