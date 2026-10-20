extends SceneTree

# Fixed before/after viewpoints for the environment revamp: close-ups of the vegetation and light review_hamlet.gd misses,
# the gameplay spring camera at the tea house eaves, and a dialogue background.
# Usage: $GODOT_MONO --path . --script res://tools/review_revamp.gd [-- --out=subdir] [--only=name,name]
var out="res://artifacts/revamp"
var only=[]

func _initialize():
	call_deferred("review")

func wanted(name):
	return only.size()==0 or only.has(name)

func shot(name):
	for i in range(5): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(out+"/"+name+".png")==OK)
	print("REVAMP_CAPTURE ",name)

func review():
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="): out+="/"+arg.substr(6)
		if arg.begins_with("--only="): only=arg.substr(7).split(",")
	DirAccess.make_dir_recursive_absolute(out)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	for i in range(15): await process_frame
	var hud=null
	for node in game.get_children():
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: hud=node
	var mio=game.get_node("Mio")
	for node in game.get_children():
		if node is CharacterBody3D and node!=mio: node.set_physics_process(false)
	var spring=game.find_children("*","SpringArm3D",true,false)[0]
	# The gameplay camera on its spring arm, as a player walking along the tea house front under its eaves sees it.
	hud.visible=false
	for view in [["eaves-front",Vector3(8.1,.1,23),0.0,-.62,7.5],["eaves-side",Vector3(8.1,.1,27.5),-.5,-.75,7.5],["eaves-under",Vector3(8.2,.1,24.5),.3,-.95,9.0]]:
		if not wanted(view[0]): continue
		mio.position=view[1]
		mio.velocity=Vector3.ZERO
		game.set("CameraYaw",view[2])
		game.set("CameraPitch",view[3])
		spring.spring_length=view[4]
		for i in range(60): await physics_frame
		await shot(view[0])
	if wanted("dialogue"):
		hud.visible=true
		var sora=game.get("World").find_child("Sora",false,false)
		mio.position=sora.global_position+Vector3(0,.1,2)
		game.set("CameraYaw",0.0)
		game.set("CameraPitch",-.29)
		spring.spring_length=7.5
		for i in range(60): await physics_frame
		game.call("Interact")
		game.set("Reveal",1000.0)
		for i in range(10): await process_frame
		await shot("dialogue")
		game.call("EndDialogue")
	hud.visible=false
	game.set_process(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.far=400
	camera.fov=55
	var views=[
		["sakura-close",Vector3(-4.6,3.2,30.2),Vector3(-7,3.4,27.5),Vector3(1,.1,27)],
		["sakura-mid",Vector3(0,2.4,34),Vector3(-6.5,3,27.5),Vector3(1,.1,27)],
		["shrine-sakura",Vector3(6,2.6,57),Vector3(2.4,3.2,63.2),Vector3(-.5,.1,48)],
		["lawn-grass",Vector3(-3.6,1.1,47.5),Vector3(-7.5,.1,42),Vector3(1,.1,27)],
		["rice-paddies",Vector3(21.5,2.3,18.5),Vector3(29,0,29),Vector3(1,.1,27)],
		["village-trees",Vector3(-2,8,-4),Vector3(-21,2,14),Vector3(-3,.1,12)],
		["road-countryside",Vector3(1.5,2.6,-24),Vector3(-1,1.5,-48),Vector3(0,.1,-20)],
		["komorebi-road",Vector3(3,6.6,-127),Vector3(-1,4.8,-150),Vector3(0,4.6,-136)]]
	for view in views:
		if not wanted(view[0]): continue
		mio.position=view[3]
		mio.velocity=Vector3.ZERO
		mio.set_physics_process(true)
		for i in range(40): await physics_frame
		mio.set_physics_process(false)
		camera.position=view[1]
		camera.look_at(view[2])
		await shot(view[0])
	# Prop close-ups for phase 2, fixed like the views above so a prop's replacement is framed exactly as the original was.
	var props=[
		["prop-lantern",Vector3(-.7,3.59,59),Vector3(-2.6,3.14,57.5),Vector3(0,.1,16)],
		["prop-lantern-shrine",Vector3(-1.4,3.79,53.9),Vector3(-2.6,3.14,57.5),Vector3(-.5,.1,48)],
		["prop-well",Vector3(.4,2.1,21.4),Vector3(-3.2,1.3,24),Vector3(1,.1,30)],
		["prop-bench",Vector3(-3.3,1.271,26),Vector3(-5.4,.471,27.3),Vector3(1,.1,33)],
		["prop-lamp",Vector3(-1.1746,1.999,4.6),Vector3(-3.5746,1.799,2),Vector3(2,.1,9)],
		["prop-barrels",Vector3(-10,1.266,15.6),Vector3(-12.4,.166,13.6),Vector3(0,.1,22)],
		["prop-bamboo-fence",Vector3(8.3,1.745,16.2),Vector3(9.9,.545,13),Vector3(4,.1,22)]]
	for view in props:
		if not wanted(view[0]): continue
		mio.position=view[3]
		mio.velocity=Vector3.ZERO
		mio.set_physics_process(true)
		for i in range(40): await physics_frame
		mio.set_physics_process(false)
		camera.position=view[1]
		camera.look_at(view[2])
		await shot(view[0])
	print("REVAMP_REVIEW_OK")
	quit()
