extends SceneTree
# The patrol robot's clips (tools/patrol_clips.py) on Patrol_1 in the village square, five frames of each from its front right
# and one from its side: artifacts/patrol/<clip>-<n>.png.
# $GODOT_MONO --path . --script res://tools/review_patrol.gd

func _initialize():
	call_deferred("capture")

func shot(camera,from,to,path):
	camera.position=from
	camera.look_at(to)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(path)==OK)

func capture():
	var output="res://artifacts/patrol/"
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
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=40
	game.get_node("Mio").position=Vector3(8,.1,30)
	var robot=game.get_node("Patrol_1")
	robot.position=Vector3(0,.1,21)
	robot.get("Model").rotation=Vector3.ZERO
	var animator=robot.find_children("*","AnimationPlayer",true,false)[0]
	var at=robot.position+Vector3(0,1,0)
	for name in animator.get_animation_list():
		var clip=name.split("/")[-1]
		var length=animator.get_animation(name).length
		for k in range(5):
			animator.play(name)
			animator.seek(length*k/4.0,true)
			animator.pause()
			await shot(camera,at+Vector3(2.2,.5,4.2),at,output+clip+"-"+str(k)+".png")
		animator.seek(length*.3,true)
		await shot(camera,at+Vector3(4.6,.4,0),at,output+clip+"-side.png")
	print("PATROL_CAPTURE_OK: ",animator.get_animation_list())
	quit()
