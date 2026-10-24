extends SceneTree
# Cuts the white-on-black UI art (assets/art/ui/ui-art-generation.md) into white images whose alpha is the source's
# brightness, so anti-aliased edges stay soft and the HUD tints each one by modulating it.
# --src=<png> --out=<dir> --names=a,b,,d   a 4x4 icon sheet: each named cell, row by row, as <dir>/<name>.png, 256 px
#                                          square with the icon centred in 232 px; an empty name skips its cell.
# --src=<png> --out=<png>                  one image (the logo), cut to what it draws plus a 16 px margin, at its own size.
# Brightness 8 or less is background (the sheets' black carries a little noise), 247 or more is fully opaque.

func _initialize():
	var opts={}
	for a in OS.get_cmdline_user_args():
		var kv=a.trim_prefix("--").split("=",true,1)
		opts[kv[0]]=kv[1] if kv.size()>1 else "1"
	var img=lift(Image.load_from_file(opts["src"]))
	var ok=true
	if opts.has("names"):
		var names=opts["names"].split(",");var cell=img.get_width()/4.0
		for i in names.size():
			if names[i]=="": continue
			var x0=roundi(i%4*cell);var y0=roundi(i/4*cell)
			var part=img.get_region(Rect2i(x0,y0,roundi((i%4+1)*cell)-x0,roundi((i/4+1)*cell)-y0))
			part=part.get_region(part.get_used_rect())
			var s=232.0/maxi(part.get_width(),part.get_height())
			part.resize(maxi(1,roundi(part.get_width()*s)),maxi(1,roundi(part.get_height()*s)),Image.INTERPOLATE_LANCZOS)
			var icon=Image.create_empty(256,256,false,Image.FORMAT_RGBA8)
			icon.blit_rect(part,Rect2i(Vector2i.ZERO,part.get_size()),(Vector2i(256,256)-part.get_size())/2)
			ok=save(icon,opts["out"].path_join(names[i]+".png")) and ok
	else:
		var used=img.get_used_rect().grow(16).intersection(Rect2i(Vector2i.ZERO,img.get_size()))
		ok=save(img.get_region(used),opts["out"])
	quit(0 if ok else 1)

func lift(src):
	src.convert(Image.FORMAT_RGBA8)
	var px=src.get_data();var out=PackedByteArray();out.resize(px.size())
	for i in px.size()/4:
		var v=maxi(px[i*4],maxi(px[i*4+1],px[i*4+2]))
		out[i*4]=255;out[i*4+1]=255;out[i*4+2]=255;out[i*4+3]=clampi(roundi((v-8)*255.0/239),0,255)
	return Image.create_from_data(src.get_width(),src.get_height(),false,Image.FORMAT_RGBA8,out)

func save(image,path):
	var err=image.save_png(path)
	print("UI_ART ",image.get_size()," ",err," ",path)
	return err==OK
