extends SceneTree
# Render cost at three village viewpoints, four in Tunnel Mountain (the Court, the Row, the Signal yard and the Mast) and two inside Hoshikawa
# Station (the concourse and the mall's south alley). Draw calls and primitives repeat exactly,
# so compare those between commits;
# fps and CPU render time drift with machine load. GPU time reads 0 under Metal Forward Mobile, so it is not sampled.
func _initialize():
	call_deferred("run")
func run():
	var game=load("res://scenes/Main.tscn").instantiate()
	root.add_child(game)
	game.call("Start")
	# Start() captures the mouse, and captured motion turns the camera; keep the view fixed whatever the real mouse does.
	Input.mouse_mode=Input.MOUSE_MODE_VISIBLE
	var vp=root.get_viewport_rid()
	RenderingServer.viewport_set_measure_render_time(vp,true)
	for spot in [Vector3(0,.1,31),Vector3(0,.1,16),Vector3(-3,2.5,58),Vector3(92,10.1,-154),Vector3(232,20.1,-128),Vector3(112,16.6,-178),Vector3(324,34.1,-164)]:
		await sample(game,vp,spot)
	# The station's inside is shown in the valley's place once Mio goes in.
	game.call("EnterStation","PARCELS DOOR")
	for spot in [Vector3(2070,300.1,-2014),Vector3(2060,295.1,-1968)]:
		await sample(game,vp,spot)
	quit()
func sample(game,vp,spot):
	var mio=game.get_node("Mio")
	mio.position=spot
	mio.velocity=Vector3.ZERO
	# The Row's spot stands a few metres from a patrol: keep Mio unhurt so every sample is of live play.
	mio.set("Invulnerable",1e9)
	for i in range(90): await process_frame
	var cpu=[]
	for i in range(240):
		await process_frame
		cpu.append(RenderingServer.viewport_get_measured_render_time_cpu(vp))
	cpu.sort()
	assert(game.get("Mode")==1,"the sample left live play")
	print("RENDER ",spot," draws=",Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)," prims=",Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)," cpu_ms_median=",snapped(cpu[120],.01)," fps=",Engine.get_frames_per_second())
