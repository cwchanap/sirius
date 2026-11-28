extends SceneTree
# Hoshikawa Station's inside, leg 1, from six viewpoints (artifacts/station/): the parcels room, the freight stair, the Machine room's
# Starwell, the mall's south alley, the concourse and the south hall. Points are in the station frame (x east, z north, metres from the
# building's south-west corner) on a floor, -1 for B1 and 0 for 1F; World.StationPoint() maps them, as here.
func _initialize():
	call_deferred("capture")

func at(x,z,floor,h):
	return Vector3(2000+x,300+floor*5+h,-2000-z)

func shot(camera,from,to,path):
	camera.global_position=from
	camera.look_at(to)
	for i in range(8): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(path)==OK)

func capture():
	var output="res://artifacts/station/"
	DirAccess.make_dir_recursive_absolute(output)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	game.call("EnterStation","PARCELS DOOR")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	game.set_process_unhandled_input(false)
	game.set_process(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: node.visible=false
	game.get_node("Mio").global_position=at(60,-12,0,.1)
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=62
	await shot(camera,at(125,3,0,3.2),at(106,11,0,1),output+"parcels-room.png")
	await shot(camera,at(116,19,0,3.6),at(110.5,31,-1,0),output+"freight-stair.png")
	await shot(camera,at(67,21,-1,3.6),at(79,12,-1,.8),output+"machine-room.png")
	await shot(camera,at(118,-31,-1,2.4),at(60,-32,-1,1),output+"mall-south-alley.png")
	await shot(camera,at(52,4,0,3.8),at(100,18,0,1),output+"concourse.png")
	await shot(camera,at(66,-3,0,3.4),at(66,-34,0,1),output+"south-hall.png")
	print("STATION_CAPTURE_OK")
	quit()
