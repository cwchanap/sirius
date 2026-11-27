using Godot;
namespace Hoshikawa;

// Tunnel Mountain, the relay road's extension east of its rockfall: a maze cut into a block of mountain by the relay road's own carving
// rule (Carve() in RelayRoad.cs), its trails here. A ring of legs round two Starwells: the way in over the Wash to the Court and Starwell 1,
// leg 1 round by the shrine, the canyon, the Catwalk and the Hairpin to the Bell ledge over the shrine (the Bell stair), leg 2 the long way
// round by the Gorge, the logging valley and the Row to the Halt and Starwell 2 (the Cut gate), leg 3 up the terraces through the Drying
// yard, past the Station overlook and over the Mast to the Signalman's ledge over the Halt (the Halt stair), and leg 4 round the north wood
// and along the Long ledge to the Signal yard on the cliff over the Court (the Great stair), where the Station tunnel's mouth ends the map.
// Off the route, the east side runs down to the East yard, and the old line west over the girder bridge onto the viaduct's last spans.
public partial class World
{
    public const string MazeName="Tunnel Mountain";
    // The 1 m ground and its collider east of Fine, over the maze; Fine's own east edge runs through the Wash.
    public static readonly Rect2I MazeFine=new(104,-300,268,288);
    public static bool IsFine(Vector2I p)
    {
        foreach(var f in FineAreas)if(f.HasPoint(p))return true;
        return false;
    }
    // The maze from the rockfall's gate east, the region the HUD names.
    public static bool InMaze(float x,float z)=>x>52&&z<RelaySouth&&z>MazeFine.Position.Y&&(x>HalfWidth||z>RelayNorth);
    // East of the play area the maze's carved ground cuts down into the mountain `y`, the rivers' gorges and all, wherever it lies lower:
    // within 18 m of a maze floor that is Carve()'s ground, and past that it climbs 2 m a metre back into the ranges, so their ridges, the
    // rivers' springs and their gorges stand as they were. Where the mountain lies lower than a floor's own walls, as over the north wood,
    // they stand up out of it, Carve()'s within 10 m of the floor and falling away 1.5 m a metre beyond, so no floor runs out onto it.
    public static float MazeCut(float x,float z,float y){var (ground,_,maze,_)=Carve(x,z);return Math.Min(Math.Max(y,ground-1.5f*Math.Max(0,maze-10)),ground+2*Math.Max(0,maze-18));}
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
        ("Trestle foot",1.6f,1.1f,new Vector3[]{new(262,14.5f,-170.3f),new(267.6f,14.5f,-170.5f)}),
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
        ("Ramp 1-2",2,1.1f,new Vector3[]{new(288,18,-100),new(289.5f,19.4f,-92.5f),new(295.5f,21.2f,-93),new(297,22,-100)}),
        ("Bench 2",3,1.1f,new Vector3[]{new(297,22,-100),new(298.5f,22,-118),new(298.5f,22,-135),new(297,22,-150)}),
        ("Drying yard",4.5f,0,new Vector3[]{new(298.5f,22,-129),new(298.5f,22,-125)}),
        ("Ramp 2-3",2,1.1f,new Vector3[]{new(297,22,-150),new(298.5f,23.4f,-157.5f),new(304.5f,25.2f,-158),new(306,26,-150)}),
        ("Bench 3",3,1.1f,new Vector3[]{new(306,26,-150),new(307.5f,26,-135),new(307.5f,26,-118),new(306,26,-100)}),
        ("Overlook road",1.8f,1.1f,new Vector3[]{new(308,26,-112),new(317,26.6f,-113.5f),new(325,27.2f,-117),new(331.5f,27.5f,-121.5f)}),
        ("Station overlook",3.5f,1.1f,new Vector3[]{new(335.5f,27.5f,-126),new(335.5f,27.5f,-122)}),
        ("Shelf",1.8f,1.1f,new Vector3[]{new(324.5f,34,-159),new(327,32.4f,-151.5f),new(329.5f,30.6f,-142.5f),new(331.5f,28.8f,-134.5f),new(333.5f,27.8f,-129.5f)}),
        ("The Mast",5.5f,1.1f,new Vector3[]{new(318.5f,34,-161),new(320.5f,34,-164)}),
        ("Shelf turn",1.8f,0,new Vector3[]{new(336,27,-118.5f),new(337.5f,25,-113),new(341,23,-110.5f),new(343.5f,20.8f,-115.5f)}),
        ("Shelf lower",1.8f,0,new Vector3[]{new(343.5f,20.8f,-115.5f),new(343,18.6f,-125),new(342.5f,16.2f,-137),new(343,13.8f,-150),new(344.5f,12.4f,-160),new(346,12.1f,-165)}),
        ("East yard",9,0,new Vector3[]{new(347,12,-172),new(348,12,-172)}),
        ("Buried path",2.5f,0,new Vector3[]{new(356,12,-172),new(361,11.6f,-172.6f)}),
        ("East rim",1.6f,0,new Vector3[]{new(343.5f,20.8f,-115.5f),new(346,19.6f,-104),new(345,18.4f,-93),new(339,17.4f,-83),new(331,16.8f,-75),new(324,16.3f,-70)}),
        ("Ridge path",1.8f,1.1f,new Vector3[]{new(315,33.6f,-170),new(310,32.2f,-180),new(302,30.4f,-190),new(293,28,-196),new(284,26,-199)}),
        ("Signalman's ledge",1.9f,1.1f,new Vector3[]{new(284,26,-199),new(275,24.5f,-200),new(266,23,-197),new(258,22,-190),new(252,21.5f,-183.5f),new(242,21.5f,-182.5f)}),
        ("Wood gate",1.8f,1.1f,new Vector3[]{new(284,26,-199),new(286,26.5f,-212),new(292,27.2f,-224)}),
        ("Rim way",1.8f,1.1f,new Vector3[]{new(292,27.2f,-224),new(304,28.5f,-234),new(314,30,-246),new(320,30.5f,-254)}),
        ("Ropeway head",8,1.1f,new Vector3[]{new(322,30.5f,-258),new(326,30.5f,-258)}),
        ("Rim way N",1.8f,1.1f,new Vector3[]{new(318,30.7f,-263),new(312,31,-270),new(298,31.5f,-280),new(278,32,-288),new(258,32,-290),new(240,31.5f,-286),new(224,30.5f,-278),new(212,29.5f,-268)}),
        ("Wood road",1.8f,1.1f,new Vector3[]{new(212,29.5f,-268),new(202,29,-256),new(196,28,-242),new(192,27,-228),new(188,26.5f,-214),new(184,26.2f,-202),new(180,26,-194)}),
        ("Deer track",1.6f,0,new Vector3[]{new(240,28,-253),new(246,28.2f,-250),new(262,28.6f,-244),new(278,29,-236),new(288,29.4f,-228.5f)}),
        ("Jizo hollow",7,0,new Vector3[]{new(232,28,-254),new(238,28,-254)}),
        ("Lane end",1.8f,0,new Vector3[]{new(228,28.2f,-257),new(219,29,-263),new(212,29.5f,-268)}),
        ("Hollow path",1.6f,0,new Vector3[]{new(236,28,-248),new(238,27,-234),new(240,25.6f,-218),new(241,24.2f,-203),new(241,23.6f,-187.5f)}),
        ("Stump stair",1.6f,0,new Vector3[]{new(234,28.4f,-260),new(236,30,-270),new(240,31.4f,-284)}),
        ("Hut path",1.6f,0,new Vector3[]{new(262,28.6f,-244),new(268,28.4f,-254),new(272,28.2f,-262)}),
        ("Woodcutter's hut",4.5f,0,new Vector3[]{new(274,28.2f,-266),new(276,28.2f,-266)}),
        ("Pit path",1.6f,0,new Vector3[]{new(196,28,-242),new(206,27.6f,-238),new(214,27.2f,-232)}),
        ("Charcoal pits",5.5f,0,new Vector3[]{new(218,27.2f,-228),new(222,27.2f,-228)}),
        ("Long ledge",2,1.1f,new Vector3[]{new(180,26,-194),new(164,24.5f,-193.5f),new(146,22.8f,-194),new(128,21.2f,-193.5f),new(122,21,-193.5f)}),
        ("Signal spur",1.6f,0,new Vector3[]{new(176,25.6f,-193.8f),new(175,24.5f,-186),new(174.5f,24,-180)}),
        ("Yard ramp",1.8f,1.1f,new Vector3[]{new(122,21,-193.5f),new(115,19,-192),new(110,17.4f,-188),new(107.5f,16.6f,-184.5f)}),
        ("Signal yard",7.5f,1.1f,new Vector3[]{new(100,16.5f,-176),new(121,16.5f,-176)}),
        // The Great stair's head, a landing out from the yard's end so its brow drops clean to the Court below, with no rib of rock beside it.
        ("Great stair head",2.5f,0,new Vector3[]{new(97,16.5f,-172),new(97,16.5f,-169)}),
        ("Station tunnel",2.6f,1.1f,new Vector3[]{new(128,16.5f,-176),new(136,16.5f,-176)}),
        ("Old line E",2,0,new Vector3[]{new(93,16.5f,-176),new(88,15,-175.5f),new(84.5f,14.3f,-174)}),
        ("Old line W",2,0,new Vector3[]{new(68,14,-174),new(61,13.85f,-171.5f),new(50,13.7f,-170.5f),new(40,13.55f,-168.5f)}),
        ("Viaduct apron",3.5f,0,new Vector3[]{new(40,13.55f,-168.5f),new(36,13.55f,-168)}),
    };
    // The maze's east end, at the Station tunnel's mouth: the door to Hoshikawa Station and the objective's last mark.
    public static readonly Vector2 StationMouth=new(134,-176);
    public Vector3 MazeEnd=>new(StationMouth.X,Height(StationMouth.X,StationMouth.Y),StationMouth.Y);
    // The built crossings, each a deck from A to B pitched between the ground at its ends: the old road's bridge over the Wash, the rail
    // trestle from the Halt over the Gorge's head, the hanging bridge from the Row's bridgehead to Bench 1's landing, and the old line's girder
    // bridge over the Wash's head. A rotten plank, one of the traps in Parts, gives way under her between stringers she can still cross on.
    public static readonly (string Name,Vector2 A,Vector2 B,float Half,Vector2? Rotten)[] Decks={
        ("Road bridge",new(65,-160),new(83,-160),2.9f,null),
        ("Trestle",new(264,-170.4f),new(280,-171),1.1f,new(272,-170.6f)),
        ("Hanging bridge",new(266,-133),new(282,-133),.9f,new(273,-133)),("Girder bridge",new(84.5f,-174),new(68,-174),1.6f,new(76,-174))};
    // The rolling loads, each parked at From, the head of its run, until Mio comes within reach of To, its foot: rocks off the Hairpin's
    // corner down the pass road, logs down the Mill ravine and the Rim way, barrels down the back stair, and a cable drum down the Shelf.
    public static readonly (string Name,string Kind,Vector2 From,Vector2 To)[] Loads={
        ("Hairpin rocks","rocks",new(146,-117.6f),new(134,-118.4f)),("Ravine logs","logs",new(207,-66.8f),new(192,-83.5f)),("Back stair barrels","barrels",new(241.5f,-117.8f),new(230,-103)),
        ("Shelf drum","drum",new(325,-157.5f),new(331,-136.5f)),("Rim way logs","logs",new(313.5f,-245.5f),new(303,-233.2f))};
    // The stairs, each let down from a high floor's brow, where it runs out to a low floor's edge so the two meet at a clean drop rather than
    // over a ridge: the Bell stair from the Bell ledge onto the shrine terrace, the Halt stair from the Signalman's ledge onto the Halt and
    // the Great stair from the Signal yard into the Court. The Cut gate is unbarred from the Halt's side.
    // Each stair's foot lies far enough out for it to climb at under 40 degrees from the brow, under Mio's 50 degree floor limit.
    public static readonly (string Name,Vector2 Top,Vector2 Foot)[] Stairs={("Bell stair",new(104.5f,-137.5f),new(95.4f,-128.9f)),("Halt stair",new(242,-182.5f),new(242,-171)),("Great stair",new(97,-172),new(97,-158.5f))};
    public static readonly Vector2 BellTop=Stairs[0].Top,BellFoot=Stairs[0].Foot,CutGate=new(190,-165.7f);
    // The lock-in yards, each a trail's floor whose ways in are barred by gates that hang raised over them until she steps in: each gate a
    // point on the trail it bars.
    public static readonly (string Yard,(Vector2 At,string Trail)[] Gates)[] Locks={
        ("Drying yard",new[]{(new Vector2(298.5f,-119.8f),"Bench 2"),(new Vector2(298.5f,-134.2f),"Bench 2")}),("Charcoal pits",new[]{(new Vector2(212.8f,-232.9f),"Pit path")})};
    // Whether `p` stands in a lock-in yard, `past` metres beyond each of its gates: on its floor or within 2 m of it, at its level.
    public static bool InYard(string yard,Vector3 p,float past=0)
    {
        var t=MazeTrails.Single(t=>t.Name==yard);var (gap,level)=Near(t.Points,p.X,p.Z);if(gap>t.Half+2||Math.Abs(p.Y-level)>3)return false;
        var middle=new Vector2(t.Points[0].X+t.Points[^1].X,t.Points[0].Z+t.Points[^1].Z)/2;var at=new Vector2(p.X,p.Z);
        return Locks.Single(l=>l.Yard==yard).Gates.All(g=>(at-g.At).Dot((middle-g.At).Normalized())>past);
    }
    // Tunnel Mountain's robots: a kind, a home and, for a scout lying dormant until Mio comes near, the way it faces in degrees. The patrols
    // hold the Narrows, the Lone Cedar saddle, the Crossing, the Sawmill hollow, the Row, the Drying yard, the Mast, the East yard, the
    // Ropeway head and, two of them, the Signal yard; none of them is the relay mission's. The second half's rows follow the first's.
    public static readonly (string Kind,Vector2 At,float? Sleeps)[] MazeRobots={
        ("patrol",new(99,-100.2f),null),("patrol",new(122,-146),null),("patrol",new(174,-164),null),("patrol",new(231,-55),null),("patrol",new(234,-131),null),
        ("scout",new(75.2f,-168),null),("scout",new(76.5f,-129.5f),null),("scout",new(145.5f,-101),null),("scout",new(150,-125),null),("scout",new(166,-98.3f),null),("scout",new(225,-51.5f),null),
        ("scout",new(237,-59.5f),null),("scout",new(311.5f,-57),null),("scout",new(250,-100.6f),null),("scout",new(220,-136),null),("scout",new(246,-126),null),("scout",new(289.4f,-142),null),
        ("scout",new(81.5f,-125),-135),("scout",new(117.5f,-98.5f),-90),("scout",new(265,-32),-90),("scout",new(271,-28.2f),114),("scout",new(315.5f,-62.5f),-113),("scout",new(216,-126.4f),180),("scout",new(251.5f,-135.4f),0),
        ("drone",new(76.3f,-181),null),("drone",new(122,-96.7f),null),("drone",new(132,-97.2f),null),("drone",new(140.5f,-105.9f),null),("drone",new(129,-102.9f),null),("drone",new(137,-132.1f),null),
        ("drone",new(182,-99.2f),null),("drone",new(193,-82.2f),null),("drone",new(270,-51),null),("drone",new(291.5f,-35),null),("drone",new(288.5f,-75.5f),null),("drone",new(232,-98.2f),null),
        ("drone",new(262,-133),null),("drone",new(283.5f,-133),null),("drone",new(207,-167.8f),null),
        ("patrol",new(298.5f,-127),null),("patrol",new(319.5f,-162.5f),null),("patrol",new(349,-173),null),("patrol",new(324,-258),null),("patrol",new(107,-176),null),("patrol",new(117,-176),null),
        ("scout",new(298.4f,-112),null),("scout",new(307.4f,-140),null),("scout",new(316,-163.6f),null),("scout",new(306,-185),null),("scout",new(266.5f,-197.2f),null),("scout",new(344,-176.5f),null),
        ("scout",new(286,-212),null),("scout",new(318.6f,-256.2f),null),("scout",new(298,-280),null),("scout",new(240,-286),null),("scout",new(192,-228),null),("scout",new(150,-193.9f),null),
        ("scout",new(298.5f,-130.7f),0),("scout",new(298.5f,-123.3f),180),("scout",new(254.5f,-186),135),("scout",new(230,-251.5f),117),("scout",new(236.5f,-258.5f),-18),("scout",new(240.5f,-252),-110),
        ("scout",new(275,-264.8f),-160),("scout",new(216.5f,-226),130),("scout",new(223.5f,-229.5f),-60),
        ("drone",new(307.4f,-112),null),("drone",new(321,-115.2f),null),("drone",new(330.5f,-138.5f),null),("drone",new(326,-154.5f),null),("drone",new(297.5f,-193),null),("drone",new(345.5f,-98.5f),null),
        ("drone",new(298,-229),null),("drone",new(309,-240),null),("drone",new(278,-288),null),("drone",new(258,-290),null),("drone",new(262,-244),null),("drone",new(198,-247),null),
        ("drone",new(170,-193.7f),null),("drone",new(134,-193.7f),null),("drone",new(55.5f,-171),null)};
    // How far a robot at `at` roams, as a share of its kind's loop: less on a narrow trail, so it keeps off the walls.
    public static float Room(Vector2 at)=>Mathf.Clamp(SiteTrails.MinBy(t=>Near(t.Points,at.X,at.Y).Gap).Half/3.5f,.35f,1);
    // The forest railway's line, broken where the trestle carries it over the Gorge's head.
    public static readonly Vector2[][] Railway={new Vector2[]{new(161.5f,-167.7f),new(171,-165.2f),new(184,-165),new(200,-167),new(214,-168.5f),new(226,-169),new(258,-170),new(264,-170.4f)},
        new Vector2[]{new(280,-171),new(285.8f,-171)}};
    // The main line, from the Station tunnel's mouth across the Signal yard and west along the old track bed, over the girder bridge (whose
    // deck carries it) and onto the viaduct, whose spans bring their own rails.
    public const float MainGauge=.72f;
    public static readonly Vector2[][] MainLine={new Vector2[]{new(139,-176),new(93,-176),new(88,-175.5f),new(84.5f,-174)},new Vector2[]{new(68,-174),new(61,-171.5f),new(50,-170.5f),new(40,-168.5f),new(34,-168)}};
    public static readonly Vector2[] MazeCells={new(76.6f,-185),new(110.5f,-120.3f),new(192.5f,-100.7f),new(273,-182.5f),
        new(337,-125.3f),new(322.6f,-165.4f),new(329,-254.5f),new(235,-250),new(272.4f,-267.4f),new(218.4f,-224.4f),new(174.6f,-181.5f)};
    public static readonly Vector2 KilnPouch=new(268,-27),EastPouch=new(353,-176);
    private void Maze()
    {
        foreach(var (name,a,b,half,rotten) in Decks)Deck(name,a,b,half,rotten);
        foreach(var (name,kind,from,to) in Loads)Load(name,kind,from,to);
        // Each stair hangs from its floor's brow, found by walking from the floor's end toward its foot until the ground falls away.
        foreach(var (name,at,foot) in Stairs)
        {
            var dir=(foot-at).Normalized();float ledge=Height(at.X,at.Y);var brow=at;
            // Its hinge stays on the level floor: past it the 1 m ground mesh already slopes toward the drop, a lip she could not climb off the stair.
            while(Height(brow.X+dir.X*.1f,brow.Y+dir.Y*.1f)>ledge-.05f)brow+=dir*.1f;
            var top=new Vector3(brow.X,ledge,brow.Y);
            // Drawn up, it stands on end over the brow, a mark to read from the floor below.
            Stair(name,"stair",top,new(foot.X,Height(foot.X,foot.Y),foot.Y),-Mathf.Pi/2,top-new Vector3(dir.X,0,dir.Y)*1.3f);
        }
        Gate();
        foreach(var (yard,gates) in Locks)foreach(var (at,trail) in gates)Lock(yard,at,trail);
        foreach(var cell in MazeCells)Cell(cell);
        Pouch(KilnPouch,"Kiln pouch","Kiln barrel");Pouch(EastPouch,"East yard pouch","Yard barrel");
        Vector3 Ground(float x,float z)=>new(x,Height(x,z),z);
        // The Starwells: a shrine well carved with stars under the Court's cliff, its front to the Court, and the Halt's pump well turned to
        // the line, under a tin roof the camera keeps below.
        var court=SavePoints.Single(p=>p.Place=="Court").At;Piece("Court well","court_starwell",Ground(court.X,court.Y),0,2.2f);
        var halt=SavePoints.Single(p=>p.Place=="Halt").At;var pump=Piece("Halt well","halt_well",Ground(halt.X,halt.Y),180,2.1f);
        var roof=new Node3D{Position=new(0,2.45f,0)};pump.AddChild(roof);CameraStop(roof,new BoxShape3D{Size=new(3,.7f,3.1f)});
        // The wayside shrine on its terrace over the Court, its torii at the head of the stone steps.
        ShrineHall("Wayside shrine",new(92,-124.5f),180);Torii(Ground(96,-135.5f),0,"Wayside torii");
        foreach(var (x,z) in new[]{(90.2f,-128.6f),(93.8f,-128.6f),(87.5f,-156.5f),(96.5f,-153.5f)})StoneLantern(this,Ground(x,z));
        // The old road's tunnel at the Court's east end, the forest railway's at either end of its line and again where it comes out at the
        // East yard, and the main line's Station tunnel at the Signal yard's east end, each fallen in: one Tripo portal,
        // its rubble-filled arch on its own +Z face and 3.93 m of rock behind it that the cliff swallows. (x, z) is its front, 1.5 m out from
        // the cliff's foot as the arch is set back in it, and the yaw its way out, square to the cliff, which at the line's west end runs
        // askew across it, so the line bends into that arch.
        foreach(var (x,z,yaw) in new[]{(103.5f,-159f,-90f),(161.3f,-167.8f,75f),(285.8f,-171f,-90f),(338.6f,-172f,90f),(137.1f,-176f,-90f)})
        {var face=Ground(x+3.93f*Mathf.Sin(Mathf.DegToRad(yaw)),z+3.93f*Mathf.Cos(Mathf.DegToRad(yaw)));Piece("Tunnel portal","tunnel_portal",new(x-3.93f*Mathf.Sin(Mathf.DegToRad(yaw)),face.Y,z-3.93f*Mathf.Cos(Mathf.DegToRad(yaw))),yaw);}
        foreach(var (x,z,size,turn) in new[]{(101.4f,-164,1.4f,.9f),(101.8f,-154.6f,1.2f,1.4f)})Boulder("Tunnel rubble",x,z,size,turn);
        // The Row, an abandoned street of houses, three to a side, facing each other across it.
        foreach(var (x,z,yaw,style) in new[]{(216f,-121.5f,180f,"kura"),(228f,-122.5f,180f,"farmhouse"),(250f,-121.5f,180f,"clinic"),(212f,-140.5f,0f,"kura"),(226f,-140.5f,0f,"teahouse"),(251.5f,-140.5f,0f,"kura")})
            TripoHouse("Row house",new(x,z),3,yaw,style);
        // The Halt's shelter, on the platform side of the line west of the Starwell.
        Shelter("Halt shelter",new(236.5f,-174.3f));
        // Its platform across the line, the name board and lamp facing the well, its ramp east; a log trolley left on the line at its west
        // end, its wheels on the rails; and the line telephone at the Crossing, by the Cut.
        Piece("Halt platform","halt_platform",Ground(244,-167.2f),0);
        var line=(Railway[0][6]-Railway[0][5]).Normalized();var trolley=Railway[0][5]+line*4;
        Piece("Log trolley","trolley",Ground(trolley.X,trolley.Y)+Vector3.Up*.19f,Mathf.RadToDeg(Mathf.Atan2(line.X,line.Y)));
        Piece("Line telephone","phone_box",Ground(180,-161.5f),0);
        // The sawmill, its back in the hollow's north wall, open to the hollow; and the charcoal kilns dug into the Kilns' sides, each mouth to
        // the floor and a dormant scout asleep before it.
        Piece("Sawmill","sawmill",Ground(245,-60),0,4.5f);
        foreach(var (x,z,yaw) in new[]{(264.5f,-35f,0f),(271.5f,-25f,180f)})
        {var mouth=Ground(x+2.17f*Mathf.Sin(Mathf.DegToRad(yaw)),z+2.17f*Mathf.Cos(Mathf.DegToRad(yaw)));Piece("Charcoal kiln","kiln",new(x,mouth.Y,z),yaw);}
        Handcart("Sawmill cart",new(229,-60.5f),20);
        var woodpile=Model(this,"Sawmill woodpile","woodpile",Ground(235.5f,-49),-80,new(1.3f,1.3f,1.3f));
        var stack=new StaticBody3D{Position=new(0,.6f,0)};woodpile.AddChild(stack);stack.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(1.77f,1.2f,1.11f)}});
        foreach(var (x,z) in new[]{(268.5f,-33.5f),(263.8f,-27.4f),(272.3f,-31.2f)})
        {Barrel(this,"Kiln barrel",Ground(x,z),x);var body=new StaticBody3D{Position=Ground(x,z)+Vector3.Up*.42f};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=.32f,Height=.84f}});}
        Kakashi("Field scarecrow",new(313,-60),200);
        // The forest railway on sleepers at a narrow gauge, from its buried west tunnel over the Crossing, along the Cut and the Halt and, past
        // the trestle (whose rails are its deck's), to its buried east tunnel; and the main line at its standard gauge.
        foreach(var (track,gauge) in Railway.Select(l=>(l,.53f)).Concat(MainLine.Select(l=>(l,MainGauge))))
        {
            for(int i=0;i<track.Length-1;i++)
            {
                var a=track[i];var b=track[i+1];float length=a.DistanceTo(b),yaw=Mathf.Atan2(b.X-a.X,b.Y-a.Y);var across=new Vector2(a.Y-b.Y,b.X-a.X)/length;
                for(float t=.4f;t<length;t+=.9f){var p=a.Lerp(b,t/length);Box(this,"Rail sleeper",Ground(p.X,p.Y)+Vector3.Up*.04f,new(gauge*3.8f,.12f,.24f),wood).Rotation=new(0,yaw,0);}
                foreach(float side in new[]{-gauge,gauge}){var m=(a+b)/2+across*side;var rail=Box(this,"Rail",Ground(m.X,m.Y)+Vector3.Up*.14f,new(.07f,.1f,length),Mat(new("6f6a62")));rail.Rotation=new(Mathf.Atan2(Height(a.X,a.Y)-Height(b.X,b.Y),length),yaw,0);}
            }
        }
        // The Catwalk's planks, laid across it on the cliff.
        var catwalk=MazeTrails.Single(t=>t.Name=="Catwalk");
        for(int i=0;i<catwalk.Points.Length-1;i++)
        {
            var a=new Vector2(catwalk.Points[i].X,catwalk.Points[i].Z);var b=new Vector2(catwalk.Points[i+1].X,catwalk.Points[i+1].Z);float length=a.DistanceTo(b),yaw=Mathf.Atan2(b.X-a.X,b.Y-a.Y);
            for(float t=.2f;t<length;t+=.38f){var p=a.Lerp(b,t/length);Box(this,"Catwalk plank",Ground(p.X,p.Y)+Vector3.Up*.03f,new(2*catwalk.Half-.3f,.06f,.32f),wood).Rotation=new(Mathf.Atan2(Height(a.X,a.Y)-Height(b.X,b.Y),length),yaw,0);}
        }
        NorthHalf();
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
    // A lock-in gate across `trail` at `at`, hung raised over it: the Tripo leaf, reaching a metre into the rock either side, between posts
    // and under a lintel, that Game drops to the floor while Mio is in its yard and its robots are not all shut down.
    private void Lock(string yard,Vector2 at,string trail)
    {
        var t=MazeTrails.Single(t=>t.Name==trail);var reach=t.Points.Zip(t.Points.Skip(1)).Select(p=>(A:new Vector2(p.First.X,p.First.Z),B:new Vector2(p.Second.X,p.Second.Z)))
            .MinBy(r=>Near(new Vector3(r.A.X,0,r.A.Y),new Vector3(r.B.X,0,r.B.Y),at.X,at.Y).Gap);
        var dir=(reach.B-reach.A).Normalized();var across=new Vector2(-dir.Y,dir.X);float half=t.Half+1,yaw=Mathf.Atan2(across.X,across.Y),floor=Height(at.X,at.Y);
        // Built down, so its Part's Rest is the floor, then raised.
        var leaf=new AnimatableBody3D{Name=yard+" gate",Position=new(at.X,floor,at.Y),Rotation=new(0,yaw,0),CollisionMask=0};AddChild(leaf);
        leaf.AddChild(new CollisionShape3D{Position=new(0,1.4f,0),Shape=new BoxShape3D{Size=new(.24f,2.8f,2*half)}});
        Model(leaf,"Gate leaf","cut_gate",Vector3.Zero,-90,new(1.3f,1.3f,1.3f)).Scale=new(2*half/5.12f,1.05f,1.2f);
        var dark=Mat(new("4a3a2c"));
        foreach(float s in new[]{-half,half}){var post=at+across*s;Box(this,"Lock gate post",new(post.X,floor+3.2f,post.Y),new(.4f,6.4f,.4f),dark,true);}
        Box(this,"Lock gate lintel",new(at.X,floor+6.2f,at.Y),new(.4f,.4f,2*half+.4f),dark).Rotation=new(0,yaw,0);
        var gate=new Part(leaf.Name,"lock",leaf,new(at.X,floor,at.Y),Open:3.3f,Yaw:yaw,Yard:yard);leaf.Position+=Vector3.Up*gate.Open;Parts.Add(gate);
    }
    // The north half's placeholders, from models the game already has, until its own art comes.
    private void NorthHalf()
    {
        Vector3 Ground(float x,float z)=>new(x,Height(x,z),z);
        var steel=Mat(new("829b91"));
        void Solid(Node3D n,Vector3 at,Vector3 size){var body=new StaticBody3D{CollisionMask=0};n.AddChild(body);body.AddChild(new CollisionShape3D{Position=at,Shape=new BoxShape3D{Size=size}});}
        // The Drying yard's rack along its east side, and the Station overlook's deck at the east face's brow.
        Hasa(new(302,-127),90);
        Overlook("Station overlook",new(336.6f,-121.6f));
        // On the Mast's pad, an old signal mast; at the Ropeway head, the ropeway's pylon, its sheave still on its beam.
        var mast=Ground(324.5f,-166.5f);Box(this,"Mast footing",mast+Vector3.Up*.3f,new(1.2f,.6f,1.2f),stone,true);
        Cylinder(this,"Signal mast",mast+Vector3.Up*6.6f,.16f,12,steel);
        for(int i=0;i<3;i++)Box(this,"Mast crossbar",mast+Vector3.Up*(10.4f+i*.7f),new(3.2f-i*.7f,.08f,.08f),steel).Rotation=new(0,Mathf.Pi/4,0);
        var pylon=Ground(331,-261);
        foreach(float z in new[]{-1.6f,1.6f})Box(this,"Pylon leg",pylon+new Vector3(0,5,z),new(.45f,10,.45f),steel,true);
        Box(this,"Pylon beam",pylon+new Vector3(0,10,0),new(.5f,.5f,4.6f),steel);
        Cylinder(this,"Pylon sheave",pylon+new Vector3(.4f,10.6f,0),.55f,.18f,Mat(new("5d5246"))).Rotation=new(0,0,Mathf.Pi/2);
        // The Wood gate's torii at the wood's edge, the Jizo hollow's two lanterns, and the woodcutter's hut, its woodpile by it.
        Torii(Ground(285.2f,-205.5f),171,"Wood gate torii");
        foreach(var (x,z) in new[]{(230f,-248.5f),(241.5f,-258f)})StoneLantern(this,Ground(x,z));
        Shelter("Woodcutter's hut",new(275,-266));
        var woodpile=Model(this,"Hut woodpile","woodpile",Ground(279.5f,-264),0,new(1.3f,1.3f,1.3f));Solid(woodpile,new(0,.6f,0),new(1.77f,1.2f,1.11f));
        // The charcoal pits' kilns, dug into the pits' north and east sides, each mouth to the floor.
        foreach(var (x,z,yaw) in new[]{(220f,-233.5f,0f),(227.5f,-228f,-90f)})
        {var mouth=Ground(x+2.17f*Mathf.Sin(Mathf.DegToRad(yaw)),z+2.17f*Mathf.Cos(Mathf.DegToRad(yaw)));Piece("Charcoal kiln","kiln",new(x,mouth.Y,z),yaw);}
        // The signalman's telephone on the ledge over the Halt.
        Piece("Signal telephone","phone_box",Ground(249.5f,-185.3f),0);
        // The Signal yard: lamps along its north side, its signpost, and barrels in its north-east corner.
        foreach(float x in new[]{104f,116})Lamp(Ground(x,-182.5f));
        Signpost(new(124,-171),StationSign,Mathf.Pi/2);
        foreach(var (x,z,turn) in new[]{(119f,-181.5f,.4f),(120.2f,-182.4f,2.1f),(118.6f,-182.7f,1.2f)})
        {Barrel(this,"Yard barrel",Ground(x,z),turn);var body=new StaticBody3D{Position=Ground(x,z)+Vector3.Up*.42f};AddChild(body);body.AddChild(new CollisionShape3D{Shape=new CylinderShape3D{Radius=.32f,Height=.84f}});}
        // The East yard's older way on to the station, buried under the mountain's fall.
        foreach(var (x,z,size,turn) in new[]{(362.6f,-172.6f,2.6f,.6f),(361.4f,-170.4f,1.7f,1.9f),(361.8f,-174.9f,1.9f,2.7f)})Boulder("Buried rubble",x,z,size,turn);
    }
    // One of the maze's own Tripo models (assets/models/MODEL.glb), placed whole, its faces below `top` its collider.
    private Node3D Piece(string name,string model,Vector3 at,float yaw,float top=float.MaxValue)
    {
        var n=Model(this,name,model,at,yaw,new(1.3f,1.3f,1.3f));var mesh=ActorVisual.Descendants(n).OfType<MeshInstance3D>().Single();
        var local=n.GlobalTransform.AffineInverse()*mesh.GlobalTransform;var faces=mesh.Mesh.GetFaces().Select(v=>local*v).ToArray();
        var low=Enumerable.Range(0,faces.Length/3).Where(i=>faces[(3*i)..(3*i+3)].All(v=>v.Y<top)).SelectMany(i=>faces[(3*i)..(3*i+3)]).ToArray();
        var body=new StaticBody3D{CollisionMask=0};n.AddChild(body);body.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=low,BackfaceCollision=true}});
        return n;
    }
    private void Deck(string name,Vector2 a,Vector2 b,float half,Vector2? rotten)
    {
        var from=new Vector3(a.X,Height(a.X,a.Y),a.Y);var to=new Vector3(b.X,Height(b.X,b.Y),b.Y);var d=to-from;float length=d.Length();
        // The frame's Z runs along the deck, pitched with it, its origin at the deck's top midway.
        var frame=new Node3D{Name=name,Position=(from+to)/2,Basis=Basis.LookingAt(d,Vector3.Up,true)};AddChild(frame);
        var body=new StaticBody3D{CollisionMask=0};frame.AddChild(body);
        void Solid(Vector3 at,Vector3 size,float pitch=0)=>body.AddChild(new CollisionShape3D{Position=at,Rotation=new(pitch,0,0),Shape=new BoxShape3D{Size=size}});
        float rot=rotten is {} p?(p-a).Dot((b-a).Normalized())*length/a.DistanceTo(b)-length/2:float.NaN;
        // The trestle and the hanging bridge are Tripo models, along their X, cut through where the rotten plank lies. The trestle's
        // sleepers are 6.18 m up its legs; the hanging bridge's boards sag from 1.43 m at its posts to 1.08 m midway, 35 cm, and its
        // boards' collision follows them in metre lengths.
        string? art=name switch{"Trestle"=>"trestle","Hanging bridge"=>"hanging_bridge",_=>null};
        if(art!=null)Model(frame,name,art,new(0,art=="trestle"?-6.18f:-1.43f,0),-90,new(1.3f,1.3f,1.3f)).Scale=new(length/(art=="trestle"?16.15f:15.71f),1,1);
        // A model's faces wholly below `under`, its piers or the foot of its legs, are solid where they stand; the trestle's only from 5 m
        // down, its last metre, so the beams and braces under its rotten plank let her fall through to the gorge's floor.
        void Legs(Node3D model,float under)
        {
            var mesh=ActorVisual.Descendants(model).OfType<MeshInstance3D>().Single();var local=frame.GlobalTransform.AffineInverse()*mesh.GlobalTransform;var faces=mesh.Mesh.GetFaces().Select(v=>local*v).ToArray();
            var piers=Enumerable.Range(0,faces.Length/3).Where(i=>faces[(3*i)..(3*i+3)].All(v=>v.Y<under)).SelectMany(i=>faces[(3*i)..(3*i+3)]).ToArray();
            body.AddChild(new CollisionShape3D{Shape=new ConcavePolygonShape3D{Data=piers,BackfaceCollision=true}});
        }
        if(art=="trestle")Legs(frame.GetNode<Node3D>(name),-5);
        float Sag(float z)=>art=="hanging_bridge"?-.35f*(1-4*z*z/(length*length)):0;
        float Pitch(float z)=>-Mathf.Atan((Sag(z+.01f)-Sag(z-.01f))/.02f);
        // Its boards, broken round the rotten plank, where stringers along each side carry on over the gap.
        foreach(var (z0,z1) in rotten==null?new[]{(-length/2,length/2)}:new[]{(-length/2,rot-.6f),(rot+.6f,length/2)})
        {
            int n=art=="hanging_bridge"?Mathf.CeilToInt(z1-z0):1;
            for(int i=0;i<n;i++)
            {
                float s0=Mathf.Lerp(z0,z1,(float)i/n),s1=Mathf.Lerp(z0,z1,(i+1f)/n);var at=new Vector3(0,(Sag(s0)+Sag(s1))/2-.1f,(s0+s1)/2);var size=new Vector3(2*half,.2f,new Vector2(s1-s0,Sag(s1)-Sag(s0)).Length());
                if(art==null&&name!="Road bridge")Box(frame,"Deck boards",at,size,wood);Solid(at,size,-Mathf.Atan2(Sag(s1)-Sag(s0),s1-s0));
            }
        }
        // The trestle's own sleepers run on past the hole either side; the hanging bridge's boards are cut through, so it needs these.
        if(rotten!=null)foreach(float x in new[]{half-.2f,.2f-half})
        {var at=new Vector3(x,Sag(rot)-.1f,rot);if(art!="trestle")Box(frame,"Deck stringer",at,new(.4f,.2f,1.2f),wood).Rotation=new(Pitch(rot),0,0);Solid(at,new(.4f,.2f,1.2f),Pitch(rot));}
        // The main line runs on across the girder bridge on sleepers of its own, its rails spanning the rotten plank's hole.
        if(name=="Girder bridge")
        {
            for(float z=.4f-length/2;z<length/2;z+=.9f)if(!(Math.Abs(z-rot)<.6f))Box(frame,"Rail sleeper",new(0,.04f,z),new(MainGauge*3.8f,.12f,.24f),wood);
            foreach(float side in new[]{-MainGauge,MainGauge})Box(frame,"Rail",new(side,.14f,0),new(.07f,.1f,length),Mat(new("6f6a62")));
        }
        // Rails each side, solid to her waist, and their posts. The models bring their own: the hanging bridge's ropes, and on the trestle,
        // which has none, the same solid edge it always had.
        foreach(float x in new[]{half+.05f,-half-.05f})
        {
            Solid(new(x,.55f,0),new(.1f,1.1f,length));
            if(name=="Road bridge"||art!=null)continue;
            Box(frame,"Deck rail",new(x,1,0),new(.08f,.08f,length),wood);
            for(float z=-length/2;z<=length/2+.01f;z+=2)Box(frame,"Deck rail post",new(x,.5f,z),new(.1f,1,.1f),wood);
        }
        if(name=="Road bridge")
        {
            // The Tripo bridge (13.26 m along its Z, deck 4.03 m over its pier feet) stretched to the span; its piers stand in the Wash.
            var model=Model(frame,"Timber bridge","bridge",new(0,-4.03f,0),0,new(1.3f,1.3f,1.3f));model.Scale=new(1,1,(length+.5f)/13.26f);Legs(model,-.6f);
        }
        if(rotten is {} at2)
        {
            // The rotten plank, darker than the rest, a body of its own that Game drops away under her.
            var plank=new StaticBody3D{Name=name+" rotten plank",Position=new(0,Sag(rot)-.1f,rot),Rotation=new(Pitch(rot),0,0),CollisionMask=0};frame.AddChild(plank);
            plank.AddChild(new MeshInstance3D{Mesh=new BoxMesh{Size=new(2*half-.8f,.2f,1.2f)},MaterialOverride=Mat(new("3d3227"))});plank.AddChild(new CollisionShape3D{Shape=new BoxShape3D{Size=new(2*half-.8f,.2f,1.2f)}});
            Parts.Add(new(plank.Name,"plank",plank,frame.ToGlobal(new(0,Sag(rot),rot)),Size:.8f));
        }
    }
    private void Load(string name,string kind,Vector2 a,Vector2 b)
    {
        var dir=(b-a).Normalized();float yaw=Mathf.Atan2(dir.X,dir.Y);
        float radius=kind switch{"rocks"=>.75f,"logs"=>.35f,"drum"=>.6f,_=>.32f},span=kind switch{"rocks"=>.75f,"logs"=>1.3f,"drum"=>.55f,_=>.42f};
        var body=new StaticBody3D{Name=name,Position=new(a.X,Height(a.X,a.Y),a.Y),Rotation=new(0,yaw,0),CollisionMask=0};AddChild(body);
        body.AddChild(new CollisionShape3D{Position=new(0,radius,0),Shape=new BoxShape3D{Size=new(2*span,2*radius,2*radius)}});
        // It turns about Spin, at its middle, as it rolls along its Z.
        var spin=new Node3D{Name="Spin",Position=new(0,radius,0)};body.AddChild(spin);
        if(kind=="rocks")Model(spin,"Rock","boulder",new(0,-radius*.8f,0),0,new(1.05f,1.05f,1.05f)).Scale=Vector3.One*2*radius/1.58f;
        else if(kind=="logs")Cylinder(spin,"Log",Vector3.Zero,radius,2*span,wood).Rotation=new(0,0,Mathf.Pi/2);
        // A cable drum: its barrel wound with rusted cable between two wooden flanges, the flanges its rims.
        else if(kind=="drum")
        {
            Cylinder(spin,"Drum cable",Vector3.Zero,radius*.72f,2*span-.12f,Mat(new("5d5246"))).Rotation=new(0,0,Mathf.Pi/2);
            foreach(float x in new[]{span-.04f,.04f-span})Cylinder(spin,"Drum flange",new(x,0,0),radius,.08f,wood).Rotation=new(0,0,Mathf.Pi/2);
        }
        else{var barrel=Model(spin,"Barrel","village_barrel",new(span,0,0),0,new(1.3f,1.3f,1.3f));barrel.Rotation=new(0,0,Mathf.Pi/2);}
        Parts.Add(new(name,kind,body,body.Position,new(b.X,Height(b.X,b.Y),b.Y),Yaw:yaw,Size:radius,Span:span));
    }
    // The Cut gate, across the Cut at the Crossing, barred on the Halt's side: a leaf hinged in the north wall, reaching a metre into the
    // rock either side so she cannot slip round its ends, that swings back west along the wall once Mio lifts the bar.
    private void Gate()
    {
        var points=MazeTrails.Single(t=>t.Name=="The Cut");var a=new Vector2(points.Points[0].X,points.Points[0].Z);var b=new Vector2(points.Points[1].X,points.Points[1].Z);
        var dir=(b-a).Normalized();
        Gate("Cut gate",CutGate,dir,points.Half+1,new(CutGate.X+dir.X*2.2f,CutGate.Y+dir.Y*2.2f),new(CutGate.X-dir.X*2.2f,CutGate.Y-dir.Y*2.2f),-1.66f);
    }
    // A hinged gate across a way at `at`, spanning 2*reach along `dir`'s across, `high` metres of leaf hinged at at-across*reach between two
    // solid posts: worked at From, refused at To, swinging `open` radians about its hinge (positive toward its left). The Cut's and S1's.
    private void Gate(string name,Vector2 at,Vector2 dir,float reach,Vector2 from,Vector2 to,float open,float high=3)
    {
        var across=new Vector2(-dir.Y,dir.X);float yaw=Mathf.Atan2(across.X,across.Y),floor=Height(at.X,at.Y);
        var hinge=at-across*reach;
        var leaf=new AnimatableBody3D{Name=name,Position=new(hinge.X,floor,hinge.Y),CollisionMask=0};AddChild(leaf);
        leaf.AddChild(new CollisionShape3D{Position=new(0,high/2,reach),Shape=new BoxShape3D{Size=new(.24f,high,2*reach)}});
        // The Tripo leaf (5.12 m along its X, hinged at -X, barred on both faces) stretched along the leaf's Z from the hinge.
        Model(leaf,"Gate leaf","cut_gate",new(0,0,reach),-90,new(1.3f,1.3f,1.3f)).Scale=new(2*reach/5.12f,1.2f,1.2f);
        foreach(float s in new[]{-reach,reach}){var post=at+across*s;Box(this,"Gate post",new(post.X,floor+(high+.4f)/2,post.Y),new(.4f,high+.4f,.4f),Mat(new("4a3a2c")),true);}
        var gate=new Part(name,"gate",leaf,new(from.X,floor,from.Y),new(to.X,floor,to.Y),0,open,yaw);
        gate.Swing(0);Parts.Add(gate);
    }
}
