extends SceneTree
# Cuts a dialogue portrait out of a flat white-background illustration.
# --src=<png> --out=<png> [--rect=x,y,w,h] [--flip] [--ground=<rows>]
# The background is the white region reached from the border, plus pure-white pockets of 8+ pixels
# (gaps between hair strands, arm and strap), so an ivory shirt enclosed by the coat stays opaque.
# Edge pixels within 2 px of the background are un-mixed from white rather than thresholded.
# A source that already has transparency keeps it: its clear pixels are background and its soft edges are left as they are.
# It skips the pocket pass, since its gaps are already clear and its pure white is highlight (KOMA-7's visor rim).
# --ground also clears a painted ground shadow: grey (every channel 100+, spread 16 or less) reached from the background
# within that many rows of the figure's lowest row.

func _initialize():
	var opts={}
	for a in OS.get_cmdline_user_args():
		var kv=a.trim_prefix("--").split("=",true,1)
		opts[kv[0]]=kv[1] if kv.size()>1 else "1"
	var img=Image.load_from_file(opts["src"])
	if opts.has("rect"):
		var r=opts["rect"].split(",")
		img=img.get_region(Rect2i(int(r[0]),int(r[1]),int(r[2]),int(r[3])))
	img.convert(Image.FORMAT_RGBA8)
	var w=img.get_width();var h=img.get_height();var px=img.get_data()
	var white=PackedByteArray();white.resize(w*h)
	var strict=PackedByteArray();strict.resize(w*h)
	for i in w*h:
		var r=px[i*4];var g=px[i*4+1];var b=px[i*4+2]
		var lo=mini(r,mini(g,b));var hi=maxi(r,maxi(g,b));var clear=px[i*4+3]<8
		white[i]=1 if clear or (lo>=226 and hi-lo<=18) else 0
		strict[i]=1 if clear or (lo>=246 and hi-lo<=8) else 0
	var bg=PackedByteArray();bg.resize(w*h)
	var seeds=PackedInt32Array()
	for x in w: seeds.append_array([x,(h-1)*w+x])
	for y in h: seeds.append_array([y*w,y*w+w-1])
	bg=fill(white,bg,seeds,w,h)
	var opaque=true
	for i in w*h:
		if px[i*4+3]<255: opaque=false;break
	var seen=PackedByteArray();seen.resize(w*h)
	for i in w*h:
		if not opaque: break
		if strict[i]==0 or bg[i]==1 or seen[i]==1: continue
		var part=PackedInt32Array([i]);seen[i]=1;var k=0
		while k<part.size():
			var j=part[k];k+=1
			for n in neighbours(j,w,h):
				if strict[n]==1 and bg[n]==0 and seen[n]==0: seen[n]=1;part.append(n)
		if part.size()>=8: bg=fill(white,bg,part,w,h)
	if opts.has("ground"):
		var bottom=0
		for i in w*h:
			if bg[i]==0: bottom=i/w
		var top=maxi(0,bottom-int(opts["ground"]))
		var grey=PackedByteArray();grey.resize(w*h);var band=PackedInt32Array()
		for i in range(top*w,w*h):
			var r=px[i*4];var g=px[i*4+1];var b=px[i*4+2]
			if bg[i]==1 or (mini(r,mini(g,b))>=100 and maxi(r,maxi(g,b))-mini(r,mini(g,b))<=16): grey[i]=1
			if bg[i]==1: band.append(i)
		var ground=PackedByteArray();ground.resize(w*h);ground=fill(grey,ground,band,w,h)
		for i in range(top*w,w*h):
			if ground[i]==1: bg[i]=1
	var near=dilate(dilate(bg,w,h,1),w,h,w)
	var out=PackedByteArray();out.resize(w*h*4)
	for y in h:
		for x in w:
			var i=y*w+x;var o=(y*w+(w-1-x if opts.has("flip") else x))*4
			var alpha=px[i*4+3]/255.0;var mixed=false
			if bg[i]==1: alpha=0.0
			elif near[i]==1 and px[i*4+3]==255: alpha=clampf(maxi(255-px[i*4],maxi(255-px[i*4+1],255-px[i*4+2]))/255.0*1.8,0,1);mixed=true
			for c in 3:
				var v=px[i*4+c]
				if mixed and alpha>0 and alpha<1: v=clampi(roundi(255-(255-v)/alpha),0,255)
				out[o+c]=v
			out[o+3]=roundi(alpha*255)
	var result=Image.create_from_data(w,h,false,Image.FORMAT_RGBA8,out)
	result=result.get_region(result.get_used_rect())
	print("PORTRAIT ",result.get_size()," ",result.save_png(opts["out"])," ",opts["out"])
	quit()

func neighbours(i,w,h):
	var x=i%w;var y=i/w;var n=[]
	if x>0: n.append(i-1)
	if x<w-1: n.append(i+1)
	if y>0: n.append(i-w)
	if y<h-1: n.append(i+w)
	return n

func fill(mask,bg,seeds,w,h):
	var queue=PackedInt32Array();var k=0
	for s in seeds:
		if mask[s]==1 and bg[s]==0: bg[s]=1;queue.append(s)
	while k<queue.size():
		var j=queue[k];k+=1
		for n in neighbours(j,w,h):
			if mask[n]==1 and bg[n]==0: bg[n]=1;queue.append(n)
	return bg

# Two-pixel max filter along one axis (step 1 = rows, step w = columns).
func dilate(src,w,h,step):
	var dst=src.duplicate()
	for i in w*h:
		if src[i]==0: continue
		for d in [-2,-1,1,2]:
			var j=i+d*step
			if j<0 or j>=w*h or (step==1 and j/w!=i/w): continue
			dst[j]=1
	return dst
