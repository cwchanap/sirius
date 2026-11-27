extends SceneTree
# Render cost at three village viewpoints, four in Tunnel Mountain (the Court, the Row, the Signal yard and the Mast) and three on
# Hoshikawa Station's outside (the tunnel arrival facing east, the forecourt facing north to the station and the yard facing the gantry).
# Draw calls and primitives repeat exactly, so compare those between commits;
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
	var mio=game.get_node("Mio")
	# The station's samples face what the player sees there: the tunnel arrival faces east down the path, the forecourt south to the
	# station, and the yard west-southwest to the gantry. The seven old samples keep their headings (CameraYaw is untouched for them).
	var headings={Vector3(381.5,20.1,-149.5):-PI/2,Vector3(508,6.1,-52):0,Vector3(618,6.1,-132):PI/2+.27}
	for spot in [Vector3(0,.1,31),Vector3(0,.1,16),Vector3(-3,2.5,58),Vector3(92,10.1,-154),Vector3(232,20.1,-128),Vector3(112,16.6,-178),Vector3(324,34.1,-164),Vector3(381.5,20.1,-149.5),Vector3(508,6.1,-52),Vector3(618,6.1,-132)]:
		mio.position=spot
		mio.velocity=Vector3.ZERO
		if headings.has(spot): game.set("CameraYaw",headings[spot])
		# The Row's spot stands a few metres from a patrol: keep Mio unhurt so every sample is of live play.
		mio.set("Invulnerable",1e9)
		for i in range(90): await process_frame
		var cpu=[]
		for i in range(240):
			await process_frame
			cpu.append(RenderingServer.viewport_get_measured_render_time_cpu(vp))
		cpu.sort()
		assert(game.get("Mode")==1,"the sample left live play")
		print("RENDER ",spot," heading=",snapped(rad_to_deg(game.get("CameraYaw")),.1)," draws=",Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)," prims=",Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)," cpu_ms_median=",snapped(cpu[120],.01)," fps=",Engine.get_frames_per_second())
	quit()
