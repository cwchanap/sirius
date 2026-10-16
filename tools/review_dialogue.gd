extends SceneTree
# Captures Scene 1's visual-novel dialogue through the real scene flow into artifacts/dialogue/.

func _initialize():
	call_deferred("capture")

func shot(game,name,typed=true):
	if typed: game.set("Reveal",1000.0)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	var err=root.get_texture().get_image().save_png("res://artifacts/dialogue/"+name+".png")
	assert(err==OK)

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
	await shot(game,"intro-zero")
	advance(game,1)
	await shot(game,"intro-mio")
	game.set("Reveal",24.0)
	await shot(game,"intro-typing",false)
	var player=game.get_node("Mio")
	for scene in [["Ren",[["briefing-ren",0],["briefing-zero",5]]],["Hana",[["hana-koma",1]]],["Chiyo",[["chiyo-narration",6]]]]:
		game.call("EndDialogue")
		var body=game.get("World").find_child(scene[0],false,false)
		player.global_position=body.global_position+Vector3(0,.1,2)
		game.call("Interact")
		var at=0
		for view in scene[1]:
			advance(game,view[1]-at)
			at=view[1]
			await shot(game,view[0])
	game.call("EndDialogue")
	player.global_position=game.get("World").find_child("Ren",false,false).global_position+Vector3(0,.1,2)
	game.set("QuestStage",3)
	game.call("Interact")
	advance(game,4)
	await shot(game,"homecoming-night")
	advance(game,4)
	await shot(game,"homecoming-mother")
	print("DIALOGUE_CAPTURE_OK: intro, briefing, Hana and KOMA-7, Chiyo, lantern night")
	quit()
