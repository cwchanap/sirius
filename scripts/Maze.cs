using Godot;
namespace Hoshikawa;

// Tunnel Mountain, the relay road's extension east of its rockfall: a maze cut into a block of mountain by the relay road's own carving
// rule (Carve() in RelayRoad.cs), its trails here. A ring of legs round two Starwells: the way in over the Wash to the Court and Starwell 1,
// leg 1 round by the shrine, the canyon, the Catwalk and the Hairpin to the Bell ledge over the shrine (the Bell stair), leg 2 the long way
// round by the Gorge, the logging valley and the Row to the Halt and Starwell 2 (the Cut gate). The north half stays mountain for now.
public partial class World
{
    public const string MazeName="Tunnel Mountain";
    // The 1 m ground and its collider east of Fine, over the maze; Fine's own east edge runs through the Wash.
    public static readonly Rect2I MazeFine=new(104,-200,240,188);
    public static bool IsFine(Vector2I p)=>Fine.HasPoint(p)||MazeFine.HasPoint(p);
    // The maze from the rockfall's gate east, the region the HUD names.
    public static bool InMaze(float x,float z)=>x>52&&z<RelaySouth&&z>MazeFine.Position.Y;
    // East of the play area the maze's carved ground cuts down into the mountain, the rivers' gorges and all, wherever it lies lower:
    // within 18 m of a maze floor that is Carve()'s ground, and past that it climbs 2 m a metre back into the ranges, so their ridges, the
    // rivers' springs and their gorges stand as they were.
    public static float MazeCut(float x,float z){var (ground,_,maze,_)=Carve(x,z);return ground+2*Math.Max(0,maze-18);}
    // How far (x, z) lies outside the nearest of the maze's floors, as RelayEdge() does for every floor.
    public static float MazeEdge(float x,float z)=>Carve(x,z).Maze;
    // The maze's trails, as Trails: (x, ground level, z) points, the half width of their floors and of their paint. The old road in is
    // painted as the station road is, each leg's main way narrower, its side paths not at all.
    public static readonly (string Name,float Half,float Paint,Vector3[] Points)[] MazeTrails={
        ("Wayside road W",3.5f,2,new Vector3[]{new(51.5f,8.5f,-160),new(59,8.8f,-160.4f),new(66,9.2f,-160)}),
        ("Wayside road E",3.5f,2,new Vector3[]{new(82,9.4f,-160),new(89,9.8f,-159.5f)}),
        ("Court",7,1.1f,new Vector3[]{new(93,10,-159),new(97,10,-158.5f)}),
        ("Stone steps",2.2f,0,new Vector3[]{new(96,10.4f,-152),new(98,12,-144.5f),new(96,13.6f,-138)}),
        ("Shrine terrace",8,0,new Vector3[]{new(92,14,-129),new(96,14,-127)}),
        ("Wash",4,1.1f,new Vector3[]{new(76.5f,6.8f,-186),new(76,5.8f,-174),new(74,5,-160),new(73,4.6f,-146),new(77,4.3f,-134)}),
        ("Pit",7,1.1f,new Vector3[]{new(77,4.2f,-131),new(79,4.2f,-126)}),
        ("Abutment stair",1.8f,1.1f,new Vector3[]{new(85.5f,9.5f,-156.6f),new(85,7.8f,-151),new(80.5f,6,-147.5f),new(76.5f,4.9f,-146.5f)}),
        ("Wash arm W",4,1.1f,new Vector3[]{new(79,4.2f,-126),new(84,3.9f,-113),new(92,3.7f,-104.5f)}),
        ("Narrows",2.3f,1.1f,new Vector3[]{new(92,3.7f,-104.5f),new(98,3.6f,-100.5f),new(106,3.6f,-98)}),
        ("Wash arm E",4,1.1f,new Vector3[]{new(106,3.6f,-98),new(116,3.7f,-96.5f),new(130,3.8f,-97),new(142,4,-100)}),
        ("Canyon end",6,1.1f,new Vector3[]{new(142,4,-100),new(147,4.1f,-102)}),
        ("Canyon ramp",1.8f,1.1f,new Vector3[]{new(150,4.2f,-105),new(154,6.5f,-110.5f),new(148,8,-110.8f)}),
        ("Catwalk",1.4f,1.1f,new Vector3[]{new(148,8,-110.8f),new(140,10.5f,-105.5f),new(130,13,-103),new(122,15.2f,-102.7f)}),
        ("Catwalk link",1.8f,1.1f,new Vector3[]{new(122,15.2f,-102.7f),new(120.5f,16.4f,-109),new(120,17,-114.5f)}),
        ("Washout chute",2,0,new Vector3[]{new(115,9.5f,-121.5f),new(115,8,-114),new(115,5.6f,-106),new(115,3.8f,-100)}),
        ("Pass road W",3,0,new Vector3[]{new(101,14.2f,-123.5f),new(107,14.9f,-121),new(111.5f,15.5f,-120)}),
        ("Pass road lower",3,1.1f,new Vector3[]{new(118.5f,17,-117.5f),new(132,18.6f,-118.5f),new(145,20,-117.5f)}),
        ("Hairpin",3,1.1f,new Vector3[]{new(145,20,-117.5f),new(151,20.6f,-122),new(149,21.2f,-128),new(140,22,-131.5f),new(130,22.8f,-133.5f),new(123,23.5f,-138.5f),new(121,24,-144)}),
        ("Saddle",6,1.1f,new Vector3[]{new(121,24,-144),new(123,24,-148)}),
        ("Bell ledge",2.2f,1.1f,new Vector3[]{new(116,23.6f,-143),new(110,21.6f,-140),new(104.5f,20,-137.5f),new(102.3f,20,-135.4f)}),
        ("Pass road E",3,0,new Vector3[]{new(126,24,-150),new(135,22.6f,-152.5f),new(146,20,-155.5f),new(156,16.5f,-159.5f),new(162,14.2f,-162)}),
        ("Crossing",11,0,new Vector3[]{new(171,13.5f,-164),new(175,13.5f,-164)}),
        ("Goat track",1.8f,0,new Vector3[]{new(153,20.4f,-123.5f),new(161,19,-127.5f),new(168.5f,16.8f,-138),new(172,14.6f,-148),new(172.5f,13.7f,-152.5f)}),
        ("The Cut",2.6f,0,new Vector3[]{new(184,13.5f,-165),new(200,13.8f,-167),new(214,14.2f,-168.5f),new(226,14.5f,-169)}),
        ("The Halt",8,1.1f,new Vector3[]{new(226,14.5f,-169),new(258,14.5f,-170)}),
        ("Trestle end",3,1.1f,new Vector3[]{new(281,14.5f,-171),new(283.5f,14.5f,-170.5f)}),
        ("Terrace stair",1.8f,1.1f,new Vector3[]{new(285,14.5f,-167.5f),new(287,16.4f,-160),new(288,18,-152)}),
        ("Goat ledge",1.6f,0,new Vector3[]{new(203,20,-130.5f),new(186,21,-126),new(172,21.8f,-124.5f),new(165.5f,22,-126.5f)}),
        ("The Row",13,1.1f,new Vector3[]{new(214,19.6f,-131),new(250,20,-131)}),
        ("Bridgehead",2,1.1f,new Vector3[]{new(258,20,-133),new(265.5f,20,-133)}),
        ("Bridge landing",2,1.1f,new Vector3[]{new(287,18,-133),new(280.5f,18.3f,-133)}),
        ("Back stair",1.5f,1.1f,new Vector3[]{new(229,7.6f,-101.7f),new(235.5f,13.7f,-110),new(242,19.9f,-118.5f)}),
        ("Gorge W",3.5f,1.1f,new Vector3[]{new(147,4.1f,-102),new(160,4.4f,-98.5f),new(174,4.7f,-98),new(188,5.1f,-100),new(194.5f,5.2f,-101)}),
        ("Gorge mid",3.5f,1.1f,new Vector3[]{new(198.5f,7.6f,-101),new(210,7.6f,-99),new(226,7.6f,-98),new(242,7.7f,-99),new(256,7.7f,-102),new(266,7.8f,-108),new(271.5f,7.9f,-118)}),
        ("Gorge N",4,0,new Vector3[]{new(271.5f,7.9f,-118),new(274,8.1f,-132),new(272,8.3f,-150),new(273.5f,8.6f,-171),new(273,9,-181)}),
        ("Mill ravine",2.2f,1.1f,new Vector3[]{new(186,5,-99.8f),new(187.5f,5.3f,-94),new(191,6.3f,-85),new(198,7.5f,-75),new(208,8.7f,-66),new(216,9.6f,-60)}),
        ("Sawmill hollow",10,1.1f,new Vector3[]{new(224,10,-56),new(238,10,-54)}),
        ("Flume walk",1.5f,1.1f,new Vector3[]{new(248,10.4f,-53),new(262,11.4f,-50),new(278,12.4f,-52),new(292,13.2f,-56),new(303,13.8f,-58)}),
        ("Kiln path",1.8f,0,new Vector3[]{new(240,10,-46),new(250,10.8f,-38),new(262,11.5f,-32)}),
        ("Kilns",5,0,new Vector3[]{new(266,11.6f,-30),new(270,11.6f,-30)}),
        ("Kiln road",1.8f,0,new Vector3[]{new(275,11.8f,-30),new(286,12.6f,-32),new(297,13.2f,-38),new(305,13.7f,-47),new(309,14,-53)}),
        ("Fields",7,1.1f,new Vector3[]{new(310,14,-58),new(316,14,-63)}),
        ("Field path",1.8f,1.1f,new Vector3[]{new(305,13.6f,-67),new(294,12.6f,-72),new(283,11.4f,-79),new(274,10,-88),new(267,8.6f,-96.5f),new(262.5f,7.9f,-103)}),
        ("Bench 1",3,1.1f,new Vector3[]{new(288,18,-152),new(289.5f,18,-135),new(289.5f,18,-118),new(288,18,-100)}),
    };
    // Where part 1 of the maze ends for now: at Starwell 2 on the Halt, where TO BE CONTINUED shows.
    public Vector3 MazeEnd{get{var p=SavePoints.Single(s=>s.Place=="Halt").At;return new(p.X,Height(p.X,p.Y),p.Y);}}
    // The built crossings, each a deck from A to B pitched between the ground at its ends: the old road's bridge over the Wash, the rail
    // trestle from the Halt over the Gorge's head, and the hanging bridge from the Row's bridgehead to Bench 1's landing. A rotten plank, one
    // of the traps in Parts, gives way under her between stringers she can still cross on.
    public static readonly (string Name,Vector2 A,Vector2 B,float Half,Vector2? Rotten)[] Decks={
        ("Road bridge",new(65,-160),new(83,-160),2.9f,null),
        ("Trestle",new(264,-170.4f),new(280,-171),1.1f,new(272,-170.6f)),
        ("Hanging bridge",new(266,-133),new(282,-133),.9f,new(273,-133))};
    // The rolling loads, each parked at From, the head of its run, until Mio comes within reach of To, its foot: rocks off the Hairpin's
    // corner down the pass road, logs down the Mill ravine, barrels down the back stair.
    public static readonly (string Name,string Kind,Vector2 From,Vector2 To)[] Loads={
        ("Hairpin rocks","rocks",new(146,-117.6f),new(134,-118.4f)),("Ravine logs","logs",new(207,-66.8f),new(192,-83.5f)),("Back stair barrels","barrels",new(241.5f,-117.8f),new(230,-103))};
    // The Bell stair is let down from the Bell ledge's brow, where the ledge runs out to the shrine terrace's edge so the two meet at a clean
    // drop rather than over a ridge, onto the terrace; and the Cut gate is unbarred from the Halt's side.
    // The stair's foot lies far enough out on the terrace for it to climb at 38 degrees from the brow, under Mio's 50 degree floor limit.
    public static readonly Vector2 BellTop=new(104.5f,-137.5f),BellFoot=new(95.4f,-128.9f),CutGate=new(190,-165.7f);
    // Tunnel Mountain's robots: a kind, a home and, for a scout lying dormant until Mio comes near, the way it faces in degrees. The patrols
    // hold the Narrows, the Lone Cedar saddle, the Crossing, the Sawmill hollow and the Row; none of them is the relay mission's.
    public static readonly (string Kind,Vector2 At,float? Sleeps)[] MazeRobots={
        ("patrol",new(99,-100.2f),null),("patrol",new(122,-146),null),("patrol",new(174,-164),null),("patrol",new(231,-55),null),("patrol",new(234,-131),null),
        ("scout",new(75.2f,-168),null),("scout",new(76.5f,-129.5f),null),("scout",new(145.5f,-101),null),("scout",new(150,-125),null),("scout",new(166,-98.3f),null),("scout",new(225,-51.5f),null),
        ("scout",new(237,-59.5f),null),("scout",new(311.5f,-57),null),("scout",new(250,-100.6f),null),("scout",new(220,-136),null),("scout",new(246,-126),null),("scout",new(289.4f,-142),null),
        ("scout",new(81.5f,-125),-135),("scout",new(117.5f,-98.5f),-90),("scout",new(265,-32),-90),("scout",new(271,-28.2f),114),("scout",new(315.5f,-62.5f),-113),("scout",new(216,-126.4f),180),("scout",new(251.5f,-135.4f),0),
        ("drone",new(76.3f,-181),null),("drone",new(122,-96.7f),null),("drone",new(132,-97.2f),null),("drone",new(140.5f,-105.9f),null),("drone",new(129,-102.9f),null),("drone",new(137,-132.1f),null),
        ("drone",new(182,-99.2f),null),("drone",new(193,-82.2f),null),("drone",new(270,-51),null),("drone",new(291.5f,-35),null),("drone",new(288.5f,-75.5f),null),("drone",new(232,-98.2f),null),
        ("drone",new(262,-133),null),("drone",new(283.5f,-133),null),("drone",new(207,-167.8f),null)};
    // How far a robot at `at` roams, as a share of its kind's loop: less on a narrow trail, so it keeps off the walls.
    public static float Room(Vector2 at)=>Mathf.Clamp(MazeTrails.MinBy(t=>Near(t.Points,at.X,at.Y).Gap).Half/3.5f,.35f,1);
    public static readonly Vector2[] MazeCells={new(76.6f,-185),new(110.5f,-120.3f),new(192.5f,-100.7f),new(273,-182.5f)};
    public static readonly Vector2 KilnPouch=new(268,-27);
    private void Maze()
    {
        foreach(var (name,a,b,half,rotten) in Decks)Deck(name,a,b,half,rotten);
        foreach(var (name,kind,from,to) in Loads)Load(name,kind,from,to);
        // The Bell stair hangs from the ledge's brow, found by walking from the ledge's end toward its foot until the ground falls away.
        var dir=(BellFoot-BellTop).Normalized();float ledge=Height(BellTop.X,BellTop.Y);var brow=BellTop;
        while(Height(brow.X+dir.X*.1f,brow.Y+dir.Y*.1f)>ledge-.3f)brow+=dir*.1f;
        var top=new Vector3(brow.X,Height(brow.X,brow.Y),brow.Y);
        // Drawn up, it stands on end over the brow, a mark to read from the terrace below.
        Stair("Bell stair","stair",top,new(BellFoot.X,Height(BellFoot.X,BellFoot.Y),BellFoot.Y),-Mathf.Pi/2,top-new Vector3(dir.X,0,dir.Y)*1.3f);
        Gate();
        foreach(var (place,at) in SavePoints.Where(p=>InMaze(p.At.X,p.At.Y)))Well(place+" well",at);
        foreach(var cell in MazeCells)Cell(cell);
        Pouch(KilnPouch,"Kiln pouch","Kiln barrel");
        Vector3 Ground(float x,float z)=>new(x,Height(x,z),z);
        // The wayside shrine on its terrace over the Court, its torii at the head of the stone steps.
        ShrineHall("Wayside shrine",new(92,-124.5f),180);Torii(Ground(96,-135.5f),0,"Wayside torii");
        foreach(var (x,z) in new[]{(90.2f,-128.6f),(93.8f,-128.6f),(87.5f,-156.5f),(96.5f,-153.5f)})StoneLantern(this,Ground(x,z));
        // The Court's east end, where the old line's tunnel has fallen in.
        foreach(var (x,z,size,turn) in new[]{(102.5f,-161,2.2f,.4f),(103.6f,-157.4f,1.8f,1.9f),(101.4f,-164,1.4f,.9f),(104.4f,-163.2f,2.6f,2.6f),(101.8f,-154.6f,1.2f,1.4f)})Boulder("Tunnel rubble",x,z,size,turn);
        // The Row, an abandoned street of houses, two to a side, facing each other across it.
        foreach(var (x,z,yaw,style) in new[]{(216f,-121.5f,180f,"kura"),(228f,-122.5f,180f,"farmhouse"),(226f,-140.5f,0f,"teahouse"),(251.5f,-140.5f,0f,"kura")})TripoHouse("Row house",new(x,z),3,yaw,style);
        Handcart("Sawmill cart",new(229,-60.5f),20);
        var woodpile=Model(this,"Sawmill woodpile","woodpile",Ground(235.5f,-49),-80,new(1.3f,1.3f,1.3f));
        var stack=new StaticBody3D{Position=new(0,.6f,0)};woodpile.AddChild(stack);stack.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(1.77f,1.2f,1.11f)}});
        foreach(var (x,z) in new[]{(268.5f,-33.5f),(263.8f,-27.4f),(272.3f,-31.2f)})
        {Barrel(this,"Kiln barrel",Ground(x,z),x);var body=new StaticBody3D{Position=Ground(x,z)+Vector3.Up*.42f};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=.32f,Height=.84f}});}
        Kakashi("Field scarecrow",new(313,-60),200);
        // Rails along the Cut and the Halt, the line the Halt served, on sleepers at a narrow gauge.
        foreach(var name in new[]{"The Cut","The Halt"})
        {
            var points=MazeTrails.Single(t=>t.Name==name).Points;
            for(int i=0;i<points.Length-1;i++)
            {
                var a=new Vector2(points[i].X,points[i].Z);var b=new Vector2(points[i+1].X,points[i+1].Z);float length=a.DistanceTo(b),yaw=Mathf.Atan2(b.X-a.X,b.Y-a.Y);var across=new Vector2(a.Y-b.Y,b.X-a.X)/length;
                for(float t=.4f;t<length;t+=.9f){var p=a.Lerp(b,t/length);Box(this,"Rail sleeper",Ground(p.X,p.Y)+Vector3.Up*.04f,new(2,.12f,.24f),wood).Rotation=new(0,yaw,0);}
                foreach(float side in new[]{-.53f,.53f}){var m=(a+b)/2+across*side;var rail=Box(this,"Rail",Ground(m.X,m.Y)+Vector3.Up*.14f,new(.07f,.1f,length),Mat(new("6f6a62")));rail.Rotation=new(Mathf.Atan2(Height(a.X,a.Y)-Height(b.X,b.Y),length),yaw,0);}
            }
        }
        // Bench 1 runs on toward the north half of the maze, closed for now.
        foreach(float x in new[]{285f,288,291})
        {
            var n=Model(this,"Bench barricade","barricade",Ground(x,-101.5f),90,new(1.3f,1.3f,1.3f));
            var body=new StaticBody3D();n.AddChild(body);body.AddChild(new CollisionShape3D{Position=new(0,.65f,0),Shape=new BoxShape3D{Size=new(.4f,1.3f,3)}});
        }
        // The maze's own scatter, so nothing it places moves the relay road's or the countryside's.
        var r=new RandomNumberGenerator{Seed=7313};var area=MazeFine;var trees=new List<Vector2>();
        float X()=>r.RandfRange(area.Position.X,area.End.X);float Z()=>r.RandfRange(area.Position.Y,area.End.Y);
        Tree(Ground(125.5f,-149.5f),1.9f,"sugi",r);
        for(int i=0;i<260;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.8f,1.4f);var (_,_,maze,_)=Carve(x,z);
            if(maze>3.5f&&maze<14&&trees.All(c=>c.DistanceTo(new(x,z))>3.5f)){trees.Add(new(x,z));Tree(Ground(x,z),s,i%3==0?"sugi":"countryside",r);}
        }
        for(int i=0;i<900;i++)
        {
            float x=X(),z=Z(),s=r.RandfRange(.4f,1.3f),turn=r.Randf()*Mathf.Tau;var (_,_,maze,_)=Carve(x,z);
            if(maze>-1.2f&&maze<.4f&&RoadGap(x,z)>3.6f)Boulder("Maze boulder",x,z,s,turn,false);
        }
        bool Open(float x,float z)=>Carve(x,z).Maze<-.3f&&RoadGap(x,z)>3.4f&&x>HalfWidth;
        var grass=new List<(Vector3 P,float H)>();
        for(int i=0;i<5000;i++){float x=X(),z=Z(),h=r.RandfRange(.18f,.55f);if(Open(x,z))grass.Add((Ground(x,z),h));}
        Grass("Maze grass",grass,3,p=>Open(p.X,p.Y),5,.45f,new("3a5122"),new("8ea54f"),.1f,7319);
    }
    private void Deck(string name,Vector2 a,Vector2 b,float half,Vector2? rotten)
    {
        var from=new Vector3(a.X,Height(a.X,a.Y),a.Y);var to=new Vector3(b.X,Height(b.X,b.Y),b.Y);var d=to-from;float length=d.Length();
        // The frame's Z runs along the deck, pitched with it, its origin at the deck's top midway.
        var frame=new Node3D{Name=name,Position=(from+to)/2,Basis=Basis.LookingAt(d,Vector3.Up,true)};AddChild(frame);
        var body=new StaticBody3D{CollisionMask=0};frame.AddChild(body);
        void Solid(Vector3 at,Vector3 size)=>body.AddChild(new CollisionShape3D{Position=at,Shape=new BoxShape3D{Size=size}});
        float rot=rotten is {} p?(p-a).Dot((b-a).Normalized())*length/a.DistanceTo(b)-length/2:float.NaN;
        // Its boards, broken round the rotten plank, where stringers along each side carry on over the gap.
        foreach(var (z0,z1) in rotten==null?new[]{(-length/2,length/2)}:new[]{(-length/2,rot-.6f),(rot+.6f,length/2)})
        {var at=new Vector3(0,-.1f,(z0+z1)/2);var size=new Vector3(2*half,.2f,z1-z0);if(name!="Road bridge")Box(frame,"Deck boards",at,size,wood);Solid(at,size);}
        if(rotten!=null)foreach(float x in new[]{half-.2f,.2f-half}){var at=new Vector3(x,-.1f,rot);Box(frame,"Deck stringer",at,new(.4f,.2f,1.2f),wood);Solid(at,new(.4f,.2f,1.2f));}
        // Rails each side, solid to her waist, and their posts.
        foreach(float x in new[]{half+.05f,-half-.05f})
        {
            Solid(new(x,.55f,0),new(.1f,1.1f,length));
            if(name=="Road bridge")continue;
            Box(frame,"Deck rail",new(x,1,0),new(.08f,.08f,length),wood);
            for(float z=-length/2;z<=length/2+.01f;z+=2)Box(frame,"Deck rail post",new(x,.5f,z),new(.1f,1,.1f),wood);
        }
        if(name=="Road bridge")
        {
            // The Tripo bridge (13.26 m along its Z, deck 4.03 m over its pier feet) stretched to the span; its piers stand in the Wash.
            var model=Model(frame,"Timber bridge","bridge",new(0,-4.03f,0),0,new(1.3f,1.3f,1.3f));model.Scale=new(1,1,(length+.5f)/13.26f);
            var mesh=ActorVisual.Descendants(model).OfType<MeshInstance3D>().Single();var local=frame.GlobalTransform.AffineInverse()*mesh.GlobalTransform;var faces=mesh.Mesh.GetFaces().Select(v=>local*v).ToArray();
            var piers=Enumerable.Range(0,faces.Length/3).Where(i=>faces[(3*i)..(3*i+3)].All(v=>v.Y<-.6f)).SelectMany(i=>faces[(3*i)..(3*i+3)]).ToArray();
            body.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=piers,BackfaceCollision=true}});
        }
        // The trestle's legs stand on the gorge's floor, each on the lowest ground under it.
        if(name=="Trestle")for(float z=-length/2+2;z<length/2-1;z+=3)foreach(float x in new[]{-half+.15f,half-.15f})
        {var top=frame.ToGlobal(new(x,-.2f,z));float ground=Height(top.X,top.Z)-.3f;Box(this,"Trestle leg",new(top.X,(top.Y+ground)/2,top.Z),new(.24f,top.Y-ground,.24f),wood,true);}
        if(rotten is {} at2)
        {
            // The rotten plank, darker than the rest, a body of its own that Game drops away under her.
            var plank=new StaticBody3D{Name=name+" rotten plank",Position=new(0,-.1f,rot),CollisionMask=0};frame.AddChild(plank);
            plank.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(2*half-.8f,.2f,1.2f)},MaterialOverride=Mat(new("3d3227"))});plank.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(2*half-.8f,.2f,1.2f)}});
            Parts.Add(new(plank.Name,"plank",plank,frame.ToGlobal(new(0,0,rot)),Size:.8f));
        }
    }
    private void Load(string name,string kind,Vector2 a,Vector2 b)
    {
        var dir=(b-a).Normalized();float yaw=Mathf.Atan2(dir.X,dir.Y);
        float radius=kind switch{"rocks"=>.75f,"logs"=>.35f,_=>.32f},span=kind switch{"rocks"=>.75f,"logs"=>1.3f,_=>.42f};
        var body=new StaticBody3D{Name=name,Position=new(a.X,Height(a.X,a.Y),a.Y),Rotation=new(0,yaw,0),CollisionMask=0};AddChild(body);
        body.AddChild(new CollisionShape3D{Position=new(0,radius,0),Shape=new BoxShape3D{Size=new(2*span,2*radius,2*radius)}});
        // It turns about Spin, at its middle, as it rolls along its Z.
        var spin=new Node3D{Name="Spin",Position=new(0,radius,0)};body.AddChild(spin);
        if(kind=="rocks")Model(spin,"Rock","boulder",new(0,-radius*.8f,0),0,new(1.05f,1.05f,1.05f)).Scale=Vector3.One*2*radius/1.58f;
        else if(kind=="logs")Cylinder(spin,"Log",Vector3.Zero,radius,2*span,wood).Rotation=new(0,0,Mathf.Pi/2);
        else{var barrel=Model(spin,"Barrel","village_barrel",new(span,0,0),0,new(1.3f,1.3f,1.3f));barrel.Rotation=new(0,0,Mathf.Pi/2);}
        Parts.Add(new(name,kind,body,body.Position,new(b.X,Height(b.X,b.Y),b.Y),Yaw:yaw,Size:radius,Span:span));
    }
    // The Cut gate, across the Cut at the Crossing, barred on the Halt's side: a leaf hinged in the north wall, reaching a metre into the
    // rock either side so she cannot slip round its ends, that swings back west along the wall once Mio lifts the bar.
    private void Gate()
    {
        var points=MazeTrails.Single(t=>t.Name=="The Cut");var a=new Vector2(points.Points[0].X,points.Points[0].Z);var b=new Vector2(points.Points[1].X,points.Points[1].Z);
        var dir=(b-a).Normalized();var across=new Vector2(-dir.Y,dir.X);float reach=points.Half+1,yaw=Mathf.Atan2(across.X,across.Y),floor=Height(CutGate.X,CutGate.Y);
        var hinge=CutGate-across*reach;
        var leaf=new AnimatableBody3D{Name="Cut gate",Position=new(hinge.X,floor,hinge.Y),CollisionMask=0};AddChild(leaf);
        leaf.AddChild(new CollisionShape3D{Position=new(0,1.5f,reach),Shape=new BoxShape3D{Size=new(.24f,3,2*reach)}});
        var timber=Mat(new("4a3a2c"));
        leaf.AddChild(Combined("Gate boards",timber,(new(0,1.45f,reach),new(.12f,2.7f,2*reach)),(new(-.1f,.6f,reach),new(.1f,.22f,2*reach)),(new(-.1f,2.3f,reach),new(.1f,.22f,2*reach))));
        // The bar, on the leaf's east face, the Halt's side.
        leaf.AddChild(Combined("Gate bar",Mat(new("6f6a62")),(new(.14f,1.4f,reach),new(.12f,.16f,2*reach-.4f))));
        foreach(float s in new[]{-reach,reach}){var post=CutGate+across*s;Box(this,"Gate post",new(post.X,floor+1.7f,post.Y),new(.4f,3.4f,.4f),timber,true);}
        var gate=new Part("Cut gate","gate",leaf,new(CutGate.X+dir.X*2.2f,floor,CutGate.Y+dir.Y*2.2f),new(CutGate.X-dir.X*2.2f,floor,CutGate.Y-dir.Y*2.2f),0,-1.66f,yaw);
        gate.Swing(0);Parts.Add(gate);
    }
}
