extends SceneTree

func _initialize():
	call_deferred("review")

func review():
	var output="res://artifacts/tripo-jump/before"
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--jump-review-dir="): output=arg.trim_prefix("--jump-review-dir=")
	DirAccess.make_dir_recursive_absolute(output)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	game.set_process(false)
	game.set_process_unhandled_input(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
		if node is CanvasLayer: node.visible=false
	var player=game.get_node("Mio")
	var model=player.get("Model")
	var animator=model.find_children("*","AnimationPlayer",true,false)[0]
	var skeleton=model.find_children("*","Skeleton3D",true,false)[0]
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=42
	var data={}
	for case in ["standing","running","falling"]:
		player.call("Respawn")
		player.position=Vector3(0,.08,23)
		model.rotation=Vector3.ZERO
		player.set_physics_process(true)
		for i in range(12): await process_frame
		if case=="falling": player.position.y=12
		if case=="running":
			Input.action_press("run")
			Input.action_press("back")
		else: model.rotation=Vector3.ZERO
		if case!="falling": Input.action_press("jump")
		var samples=[]
		for frame in range(110 if case=="falling" else 75):
			await process_frame
			if frame==1: Input.action_release("jump")
			var hip=skeleton.global_transform*skeleton.get_bone_global_pose(skeleton.find_bone("Hips")).origin
			var foot=skeleton.global_transform*skeleton.get_bone_global_pose(skeleton.find_bone("RightFoot")).origin
			samples.append({"frame":frame,"body_y":player.position.y,"hip_y":hip.y,"foot_y":foot.y,
				"vy":player.velocity.y,"floor":player.is_on_floor(),"clip":str(animator.current_animation),
				"time":animator.current_animation_position if animator.current_animation!="" else -1})
			camera.position=Vector3(player.position.x+3,1.8,player.position.z+4.5)
			camera.look_at(Vector3(player.position.x,1.6,player.position.z))
			if DisplayServer.get_name()!="headless" and case!="falling" and frame%3==0:
				RenderingServer.force_draw()
				assert(root.get_texture().get_image().save_png(output+"/"+case+"-%03d.png"%frame)==OK)
		Input.action_release("back")
		Input.action_release("run")
		data[case]=samples
		print("JUMP_REVIEW ",case)
	FileAccess.open(output+"/motion.json",FileAccess.WRITE).store_string(JSON.stringify(data,"\t"))
	quit()
