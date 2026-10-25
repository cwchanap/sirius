extends SceneTree
# Captures the title, HUD, menus and visual-novel scenes in one language into artifacts/i18n/<lang>/.
# godot --path . --script res://tools/review_languages.gd -- --lang=ja   (en, ja or zh_TW; the game reads --lang itself)

var failed=false
var lang="en"
var out=""

func _initialize():
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--lang="): lang=a.substr(7)
	out="res://artifacts/i18n/"+lang+"/"
	call_deferred("capture")

func shot(game,name,typed=true):
	if typed: game.set("Reveal",1000.0)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	var err=root.get_texture().get_image().save_png(out+name+".png")
	if err!=OK:
		push_error("LANGUAGE_CAPTURE_FAIL "+name+" "+str(err));failed=true;quit(1)

func advance(game,beats):
	for i in beats:
		game.set("Reveal",1000.0)
		game.call("Advance")

func talk(game,who,views):
	game.call("EndDialogue")
	var body=game.get("World").find_child(who,false,false)
	game.get_node("Mio").global_position=body.global_position+Vector3(0,.1,2)
	game.call("Interact")
	var at=0
	for view in views:
		advance(game,view[1]-at)
		at=view[1]
		await shot(game,view[0])

func capture():
	DirAccess.make_dir_recursive_absolute(out)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	for node in game.get_children():
		if node is AudioStreamPlayer: node.stop()
	game.set_process_unhandled_input(false)
	await shot(game,"title",false)
	game.call("ShowSettings",true)
	await shot(game,"settings-title",false)
	game.call("ShowSettings",false)
	game.call("Begin")
	await shot(game,"intro-zero")
	advance(game,1)
	await shot(game,"intro-mio")
	var player=game.get_node("Mio")
	await talk(game,"Ren",[["briefing-ren",0],["briefing-zero",5]])
	await talk(game,"Hana",[["hana-koma",1]])
	await talk(game,"Chiyo",[["chiyo-narration",6]])
	game.call("EndDialogue")
	# On the junction's floor: placed under it, Mio is pushed back south over the region line and its toast replaces this one.
	var ground=game.get_world_3d().direct_space_state.intersect_ray(PhysicsRayQueryParameters3D.create(Vector3(0,60,-21),Vector3(0,-30,-21),1))
	player.global_position=ground.position+Vector3(0,.1,0)
	game.set("CameraYaw",.15)
	game.call("ToggleLock")
	await process_frame
	game.call("Toast","THE TRAIN WITH NO PASSENGERS","",30.0,"")
	game.call("Flash","deflect",Color("e7c873"))
	game.set("FlashTime",30.0)
	var target=game.get("Locked")
	if target: target.call("Stun",30.0)
	await shot(game,"hud-patrol",false)
	var ren=game.get("World").find_child("Ren",false,false)
	player.global_position=ren.global_position+Vector3(0,.1,2)
	game.set("CameraYaw",0.0)
	game.set("ToastTime",0.0)
	game.set("FlashTime",0.0)
	await shot(game,"hud-village",false)
	game.call("SetPaused",true)
	await shot(game,"pause",false)
	game.call("ShowSettings",true)
	await shot(game,"settings",false)
	game.call("ShowSettings",false)
	game.call("SetPaused",false)
	game.call("Die")
	await shot(game,"defeated",false)
	game.call("Retry")
	# The items screen with every item held: Mio takes both resonance cells and the pouch first, the prompt and banner captured at the first cell,
	# Mio kept unhurt by the terrace's patrol.
	player.set("Invulnerable",1000.0)
	for spot in [Vector2(19.5,-60.7),Vector2(16,-94.7),Vector2(-40,-135.7)]:
		var floor=game.get_world_3d().direct_space_state.intersect_ray(PhysicsRayQueryParameters3D.create(Vector3(spot.x,20,spot.y),Vector3(spot.x,-30,spot.y),1))
		player.global_position=floor.position+Vector3(0,.1,0)
		for i in range(30): await process_frame
		game.set("ToastTime",0.0)
		if spot.x==19.5: await shot(game,"hud-pickup",false)
		game.call("Interact")
		if spot.x==19.5: await shot(game,"hud-obtained",false)
	game.set("ToastTime",0.0)
	game.call("ShowItems",true)
	await shot(game,"items",false)
	game.get("Hud").call("Select","cell")
	await shot(game,"items-cell",false)
	game.call("ShowItems",false)
	await talk(game,"Nao",[["nao-usual",0]])
	game.call("EndDialogue")
	game.set("QuestStage",3)
	player.global_position=ren.global_position+Vector3(0,.1,2)
	game.call("Interact")
	advance(game,4)
	await shot(game,"homecoming-night")
	advance(game,4)
	await shot(game,"homecoming-mother")
	if failed: return
	print("LANGUAGE_CAPTURE_OK "+lang)
	quit()
