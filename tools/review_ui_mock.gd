extends SceneTree
# Captures the dialogue and save point screens as they are (cur-*) and the same views with the HUD hidden (bg-*), for UI mockups.
var out="res://artifacts/ui-mock/"
var failed=false

func _initialize():
	call_deferred("capture")

func shot(game,name,typed=true):
	if typed: game.set("Reveal",1000.0)
	for i in range(4): await process_frame
	RenderingServer.force_draw()
	var err=root.get_texture().get_image().save_png(out+name+".png")
	if err!=OK:
		push_error("UI_MOCK_FAIL "+name+" "+str(err));failed=true;quit(1)

func both(game,name,typed=true):
	var hud=game.get("Hud")
	hud.visible=true
	await shot(game,"cur-"+name,typed)
	hud.visible=false
	await shot(game,"bg-"+name,typed)
	hud.visible=true

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
		await both(game,view[0])

func capture():
	DirAccess.make_dir_recursive_absolute(out)
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	for node in game.get_children():
		if node is AudioStreamPlayer: node.stop()
	game.set_process_unhandled_input(false)
	game.call("Begin")
	await both(game,"opening")
	advance(game,3)
	await both(game,"zero-room")
	advance(game,3)
	await both(game,"mio-room")
	game.call("EndDialogue")
	var player=game.get_node("Mio")
	game.call("LeaveRoom")
	await both(game,"yui-gate")
	advance(game,1)
	await both(game,"mio-gate")
	await talk(game,"Renji",[["renji",0],["zero-briefing",8]])
	await talk(game,"Chiyo",[["chiyo-narration",6]])
	game.call("EndDialogue")
	var renji=game.get("World").find_child("Renji",false,false)
	var well=game.get_world_3d().direct_space_state.intersect_ray(PhysicsRayQueryParameters3D.create(Vector3(-1.2,20,24),Vector3(-1.2,-30,24),1))
	player.global_position=well.position+Vector3(0,.1,0)
	game.set("CameraYaw",0.0)
	game.call("Earn",420)
	for i in range(30): await process_frame
	game.set("ToastTime",0.0)
	await both(game,"hud-savepoint",false)
	game.call("Interact")
	await both(game,"savepoint",false)
	game.call("LeaveRest")
	if failed: return
	print("UI_MOCK_OK")
	quit()
