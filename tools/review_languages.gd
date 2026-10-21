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
	game.call("Begin")
	await shot(game,"intro-zero")
	advance(game,1)
	await shot(game,"intro-mio")
	var player=game.get_node("Mio")
	await talk(game,"Ren",[["briefing-ren",0],["briefing-zero",5]])
	await talk(game,"Hana",[["hana-koma",1]])
	await talk(game,"Chiyo",[["chiyo-narration",6]])
	game.call("EndDialogue")
	player.global_position=Vector3(0,0.2,-21)
	game.set("CameraYaw",.15)
	game.call("ToggleLock")
	game.call("Toast","THE TRAIN WITH NO PASSENGERS","Clear the patrols. Reopen the road.",30.0)
	game.call("Feedback","DEFLECT",Color("e7c873"))
	game.set("FeedbackTime",30.0)
	await shot(game,"hud-patrol",false)
	var ren=game.get("World").find_child("Ren",false,false)
	player.global_position=ren.global_position+Vector3(0,.1,2)
	game.set("CameraYaw",0.0)
	game.set("ToastTime",0.0)
	game.set("FeedbackTime",0.0)
	await shot(game,"hud-village",false)
	game.call("SetPaused",true)
	await shot(game,"pause",false)
	game.call("SetPaused",false)
	game.call("Die")
	await shot(game,"defeated",false)
	game.call("Retry")
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
