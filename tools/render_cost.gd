extends SceneTree
# Render cost at three village viewpoints. Draw calls and primitives repeat exactly, so compare those between commits;
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
	for spot in [Vector3(0,.1,31),Vector3(0,.1,16),Vector3(-3,2.5,58)]:
		mio.position=spot
		for i in range(90): await process_frame
		var cpu=[]
		for i in range(240):
			await process_frame
			cpu.append(RenderingServer.viewport_get_measured_render_time_cpu(vp))
		cpu.sort()
		print("RENDER ",spot," draws=",Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)," prims=",Performance.get_monitor(Performance.RENDER_TOTAL_PRIMITIVES_IN_FRAME)," cpu_ms_median=",snapped(cpu[120],.01)," fps=",Engine.get_frames_per_second())
	quit()
