extends SceneTree

# The map's edges and waters: the sea and its capes, the river mouths, both rivers' falls, gorges and headwaters, the Cedar River's outlet
# and the mountain walls; then Hoshikawa Station from above (north up, with its extent printed) and at ground level from its arrival
# portal, forecourt, yard, warehouse stair, parcels bay and the parcels approach's final turn and both of its sides.
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
		["hoshikawa-headwater",Vector3(30,10,6),Vector3(140,24,-50)],
		["cedar-falls",Vector3(40,8,-204),Vector3(82,4,-212)],
		["cedar-outlet",Vector3(-36,9,-204),Vector3(-76,1,-210)],
		["cedar-mouth",Vector3(-88,34,16),Vector3(-134,0,-30)],
		["north-wall",Vector3(4,10,-264),Vector3(0,8,-320)],
		["east-wall",Vector3(26,6,44),Vector3(96,6,30)],
		["aerial-south",Vector3(10,150,150),Vector3(0,0,-30)],
		["aerial-north",Vector3(10,230,-20),Vector3(0,0,-200)],
		["aerial-station",Vector3(504,176,-110),Vector3(504,6,-110)],
		["aerial-station-world",Vector3(330,364,-155),Vector3(330,6,-155)],
		["station-portal",Vector3(374,32,-149.5),Vector3(434,32,-149.5),59],
		["station-city-bearing",Vector3(381.5,22.5,-149.5),Vector3(270,55,-332)],
		["station-forecourt",Vector3(508,8.5,-52),Vector3(496,9,-84)],
		["station-yard",Vector3(618,8.5,-132),Vector3(592,10,-139)],
		["station-warehouse-stair",Vector3(611,8.5,-131),Vector3(611,8,-122)],
		["station-parcels-bay",Vector3(559,8.5,-112),Vector3(558,8,-130)],
		["parcels-mouth",Vector3(559.7,9.0,-109.5),Vector3(559.2,6.8,-128)],
		["parcels-turn",Vector3(559.6,7.6,-114),Vector3(558.5,7.1,-129)],
		["parcels-west",Vector3(562.8,11.5,-120),Vector3(559.2,6.5,-128)],
		["parcels-east",Vector3(558.8,8.5,-122),Vector3(561.5,7.5,-118)],
		["parcels-door",Vector3(559.9,7.6,-127.2),Vector3(558.6,7.4,-130.2)],
		["parcels-floor",Vector3(559.8,7.6,-110.2),Vector3(559.4,6.4,-131.5)],
		["city-from-valley-overlook",Vector3(29,7.0,-226),Vector3(270,55,-332)],
		["city-from-pass-shelter",Vector3(-12,7.0,-191),Vector3(270,55,-332)],
		["city-from-station-overlook",Vector3(336.6,29.0,-121.6),Vector3(270,55,-332)]]
	# The aerials look down through 150 m of air, which the valley's depth fog would wash out.
	var environment=game.find_children("*","WorldEnvironment",true,false)[0].environment
	# Straight-down aerials use a north-oriented up vector: the default +y up is degenerate with a vertical view.
	var north_up=["aerial-station","aerial-station-world"]
	for view in views:
		if only.size()>0 and not view[0] in only: continue
		environment.fog_enabled=not view[0].begins_with("aerial")
		camera.position=view[1]
		camera.fov = view[3] if view.size()>3 else 60
		camera.look_at(view[2],Vector3(0,0,-1) if view[0] in north_up else Vector3.UP)
		for i in range(5): await process_frame
		RenderingServer.force_draw()
		var image=root.get_texture().get_image()
		assert(image.save_png("res://artifacts/"+out+"/"+view[0]+".png")==OK)
		if view[0] in north_up:
			var size=image.get_size()
			var vertical=2*tan(deg_to_rad(camera.fov/2))*(view[1].y-6)
			print("MAP_AERIAL ",view[0]," eye=",view[1]," ground_y=6 extent_m=",snapped(vertical*float(size.x)/size.y,.1),"x",snapped(vertical,.1),"px_per_m=",snapped(float(size.y)/vertical,.02),"north_up size=",size)
		print("MAP_CAPTURE ",view[0])
	print("MAP_REVIEW_OK")
	quit()
