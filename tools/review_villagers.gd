extends SceneTree

# Sora, Nao and Toma (Tripo DCC Bridge imports): -- --only=sora,toma narrows the set.
const HEIGHTS={"Sora":1.78,"Nao":1.64,"Toma":1.74}

func _initialize():
	call_deferred("capture")

func shot(camera,from,to,path):
	camera.position=from
	camera.look_at(to)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	assert(root.get_texture().get_image().save_png(path)==OK)

func capture():
	var only=[]
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--only="): only=arg.trim_prefix("--only=").split(",")
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	game.set_process_unhandled_input(false)
	game.set_process(false)
	for node in game.get_children():
		if node is CharacterBody3D: node.set_physics_process(false)
		if node is AudioStreamPlayer: node.stop()
	var camera=game.find_children("*","Camera3D",true,false)[0]
	camera.reparent(game)
	camera.fov=38
	var player=game.get_node("Mio")
	for who in HEIGHTS:
		if only and not who.to_lower() in only: continue
		var h=HEIGHTS[who]
		var output="res://artifacts/%s-integration/"%who.to_lower()
		DirAccess.make_dir_recursive_absolute(output)
		for node in game.get_children():
			if node is CanvasLayer: node.visible=false
		var npc=game.find_child(who,true,false)
		var labels=npc.find_children("*","Label3D",true,false)
		for label in labels: label.visible=false
		var animator=npc.find_children("*","AnimationPlayer",true,false)[0]
		animator.callback_mode_process=AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
		animator.play("idle",0)
		for view in [["front",Vector3(0,.7*h,2.25*h),Vector3(0,.55*h,0),0.0],
			["face",Vector3(.15,.93*h,.8),Vector3(0,.9*h,0),0.0],
			["idle",Vector3(.15,.93*h,.8),Vector3(0,.9*h,0),1.0]]:
			animator.seek(view[3],true)
			await shot(camera,npc.to_global(view[1]),npc.to_global(view[2]),output+view[0]+".png")
		var location=npc.transform
		# Profile and back in the open Hoshikawa street, clear of each resident's buildings and orchard rows.
		var query=PhysicsRayQueryParameters3D.create(Vector3(0,10,16),Vector3(0,-10,16),1)
		var ground=npc.get_world_3d().direct_space_state.intersect_ray(query)
		assert(not ground.is_empty())
		npc.position=ground.position
		npc.rotation=Vector3.ZERO
		animator.seek(0,true)
		for view in [["side",Vector3(2.25*h,.7*h,0)],["back",Vector3(0,.7*h,-2.25*h)]]:
			await shot(camera,npc.position+view[1],npc.position+Vector3(0,.55*h,0),output+view[0]+".png")
		npc.transform=location
		for label in labels: label.visible=true
		player.position=npc.to_global(Vector3(1.4,.05,1))
		player.get("Model").rotation.y=npc.rotation.y-2.1
		game.call("Interact")
		assert(game.get("Speaker")==who)
		game.call("Advance") # processing is off, so finish typing the line out
		for node in game.get_children():
			if node is CanvasLayer: node.visible=true
		await shot(camera,npc.to_global(Vector3(3.4,1.25*h,4.2)),npc.to_global(Vector3(.4,.45*h,0)),output+"dialogue.png")
		game.call("EndDialogue")
		print("VILLAGER_CAPTURE_OK %s: front, face, idle, side, back, actual village dialogue"%who)
	quit()
