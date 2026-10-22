extends SceneTree

# The map's edges and waters: the sea, the river mouth, both rivers' falls and gorges, the Cedar River's outlet and the mountain walls.
# Writes artifacts/map/NAME.png; -- --out=<subdir> --only=sea-from-shrine,aerial-south
func _initialize():
	call_deferred("review")

func review():
	var out="map"
	var only=[]
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="): out=arg.substr(6)
		if arg.begins_with("--only="): only=arg.substr(7).split(",")
	DirAccess.make_dir_recursive_absolute("res://artifacts/"+out)
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
	camera.far=900
	camera.fov=60
	var views=[
		["sea-from-shrine",Vector3(12,11,52),Vector3(-60,-1,96)],
		["west-shore",Vector3(-46,5,26),Vector3(-100,-1,44)],
		["river-mouth",Vector3(-36,7,6),Vector3(-80,-1,-12)],
		["hoshikawa-falls",Vector3(38,5,-3),Vector3(84,3,-9)],
		["cedar-falls",Vector3(40,8,-104),Vector3(82,4,-112)],
		["cedar-outlet",Vector3(-36,9,-104),Vector3(-76,1,-110)],
		["north-wall",Vector3(4,10,-164),Vector3(0,8,-220)],
		["east-wall",Vector3(26,6,44),Vector3(96,6,30)],
		["aerial-south",Vector3(10,150,150),Vector3(0,0,-30)],
		["aerial-north",Vector3(10,170,-10),Vector3(0,0,-150)]]
	# The aerials look down through 150 m of air, which the valley's depth fog would wash out.
	var environment=game.find_children("*","WorldEnvironment",true,false)[0].environment
	for view in views:
		if only.size()>0 and not view[0] in only: continue
		environment.fog_enabled=not view[0].begins_with("aerial")
		camera.position=view[1]
		camera.look_at(view[2])
		for i in range(5): await process_frame
		RenderingServer.force_draw()
		assert(root.get_texture().get_image().save_png("res://artifacts/"+out+"/"+view[0]+".png")==OK)
		print("MAP_CAPTURE ",view[0])
	print("MAP_REVIEW_OK")
	quit()
