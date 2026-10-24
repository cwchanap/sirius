extends SceneTree
# Mio's three-swing combo and then Resonance Cut against a patrol robot in the village square, seen from her right.
# $GODOT_MONO --path . --fixed-fps 60 --write-movie artifacts/combat/combat.avi --script res://tools/review_combat.gd
# $GODOT_MONO --path . --fixed-fps 60 --script res://tools/review_combat.gd -- --frames   (every other frame, artifacts/combat/)
# Movie Maker writes every frame however slowly it draws, so a software-Vulkan run makes the same video, only later.

const PRESSES=[[0,"attack"],[12,"attack"],[30,"attack"],[150,"skill"]]

func _initialize():
	call_deferred("review")

func review():
	var output="res://artifacts/combat"
	var frames=OS.get_cmdline_user_args().has("--frames")
	DirAccess.make_dir_recursive_absolute(output)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	for node in game.get_children():
		if node is CharacterBody3D and node.name!="Mio": node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: node.visible=false
	var player=game.get_node("Mio")
	player.position=Vector3(0,.08,23)
	player.get("Model").rotation=Vector3(0,PI,0)
	game.set("CameraYaw",1.9)
	game.set("CameraPitch",-.2)
	game.find_children("*","SpringArm3D",true,false)[0].spring_length=3.6
	for i in range(30): await physics_frame
	var robot=game.get_node("Patrol_1")
	robot.position=player.position+Vector3(0,.1,-2.6)
	robot.get("Model").rotation=Vector3.ZERO
	for frame in range(260):
		for press in PRESSES:
			if press[0]==frame: Input.action_press(press[1])
			if press[0]+2==frame: Input.action_release(press[1])
		await physics_frame
		if frames and frame%2==0:
			RenderingServer.force_draw()
			assert(root.get_texture().get_image().save_png(output+"/combat-%03d.png"%frame)==OK)
	print("COMBAT_REVIEW ",output)
	quit()
