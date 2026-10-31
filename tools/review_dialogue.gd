extends SceneTree
# Captures Scene 1's visual-novel dialogue and the opening's staging through the real scene flow into artifacts/dialogue/.

var failed=false

func _initialize():
	call_deferred("capture")

func shot(game,name,typed=true):
	if typed: game.set("Reveal",1000.0)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	var err=root.get_texture().get_image().save_png("res://artifacts/dialogue/"+name+".png")
	if err!=OK:
		push_error("DIALOGUE_CAPTURE_FAIL "+name+" "+str(err));failed=true;quit(1)

func wait(frames):
	for i in frames: await process_frame

func advance(game,beats):
	for i in beats:
		game.set("Reveal",1000.0)
		game.call("Advance")

func capture():
	DirAccess.make_dir_recursive_absolute("res://artifacts/dialogue/")
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	for node in game.get_children():
		if node is AudioStreamPlayer: node.stop()
	game.set_process_unhandled_input(false)
	game.call("Begin")
	# A scene's camera moves for two seconds from shot to shot; running the game five times faster lets each move settle in a few
	# dozen frames, so a slow software renderer gets through. The opening's glides are seven seconds and are still on their way.
	Engine.time_scale=5
	for view in ["opening-city","opening-valley","opening-home"]:
		await wait(30)
		await shot(game,view)
		advance(game,1)
	await wait(10)
	await shot(game,"intro-asleep")
	for view in [["intro-waking",2,30],["intro-terminal",1,10],["intro-floor",1,30],["intro-rise",2,30],["intro-shoji",1,30],["intro-face-moving",2,8],["intro-face",0,30]]:
		advance(game,view[1])
		await wait(view[2])
		await shot(game,view[0])
	game.set("Reveal",24.0)
	await shot(game,"intro-typing",false)
	game.call("EndDialogue")
	await wait(20)
	await shot(game,"room",false)
	var player=game.get_node("Mio")
	var room=game.get("Room")
	player.global_position=room.global_position+Vector3(.9,.05,-1.3)
	game.call("Interact")
	await shot(game,"room-photo")
	game.call("EndDialogue")
	player.global_position=room.global_position+Vector3(1.6,.05,.65)
	game.set("CameraYaw",-PI/2)
	await wait(5)
	await shot(game,"room-door",false)
	game.call("LeaveRoom")
	await wait(20)
	await shot(game,"gate-wave")
	for view in [["gate-point",2,20],["gate-zero",1,20],["gate-letter-moving",4,8],["gate-letter",0,30]]:
		advance(game,view[1])
		await wait(view[2])
		await shot(game,view[0])
	game.call("EndDialogue")
	await wait(30)
	await shot(game,"gate-race",false)
	game.call("FinishWalks")
	for scene in [["Renji",[["briefing-renji",0],["briefing-letter",3],["briefing-point",5],["briefing-zero",8],["briefing-yui-moving",9],["briefing-fold",11],["briefing-cross",12]]],["Haruka",[["haruka-koma",1]]],["Chiyo",[["chiyo-narration",6]]]]:
		game.call("EndDialogue")
		var body=game.get("World").find_child(scene[0],false,false)
		player.global_position=body.global_position+Vector3(0,.1,2)
		game.call("Interact")
		var at=0
		for view in scene[1]:
			advance(game,view[1]-at)
			at=view[1]
			await wait(8 if view[0].ends_with("-moving") else 30)
			await shot(game,view[0])
	game.call("EndDialogue")
	await wait(60)
	await shot(game,"yui-clinic-walk",false)
	player.global_position=game.get("World").find_child("Renji",false,false).global_position+Vector3(0,.1,2)
	game.set("QuestStage",3)
	game.call("Interact")
	advance(game,4)
	await shot(game,"homecoming-night")
	advance(game,4)
	await shot(game,"homecoming-mother")
	if failed: return
	print("DIALOGUE_CAPTURE_OK: opening, room, gate, briefing, Haruka and KOMA-7, Chiyo, lantern night")
	quit()
