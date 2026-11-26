using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

// Tunnel Mountain's ground, flooded as Mio walks it (WildsTest.Reach): the maze is closed, each leg's far side is reached only by walking
// it, its drops are one way, its shortcuts are earned by long walks, its trails meet without steps, and from every floor she can walk
// back to a Starwell. The places and the blocks that shut a way off for a check are the plan's prototype's.
[TestSuite]
public class MazeTest
{
    static readonly Dictionary<string,Vector2> Places=new()
    {
        ["Wayside road"]=new(58,-160),["Court"]=new(94,-159),["Shrine terrace"]=new(94,-128),["Wash under bridge"]=new(74,-160),["Wash head"]=new(76,-184),["Pit"]=new(78,-128),
        ["Wash arm W"]=new(84,-113),["Wash arm E"]=new(125,-97),["Canyon end"]=new(145,-101),["Catwalk"]=new(135,-104),["Pass road W brink"]=new(108,-121),["Chute"]=new(115,-112),
        ["Pass road lower"]=new(132,-118.5f),["Hairpin top"]=new(140,-131.5f),["Saddle"]=new(122,-146),["Bell ledge"]=new(106,-138),["Pass road E"]=new(146,-155.5f),["Crossing"]=new(173,-164),
        ["Goat track"]=new(168,-138),["Cut"]=new(205,-167.5f),["Halt"]=new(242,-169.5f),["Trestle end"]=new(282,-171),["Goat ledge"]=new(186,-126),["Row"]=new(232,-131),
        ["Bridgehead"]=new(264,-133),["Bridge landing"]=new(284,-133),["Gorge W"]=new(180,-99),["Gorge mid"]=new(226,-98),["Gorge N"]=new(272,-150),["Gorge head"]=new(273,-180),
        ["Mill ravine"]=new(198,-75),["Sawmill hollow"]=new(231,-55),["Flume walk"]=new(270,-51),["Kilns"]=new(268,-30),["Kiln road"]=new(297,-38),["Fields"]=new(313,-60.5f),
        ["Field path"]=new(283,-79),["Bench 1"]=new(289.5f,-125),
        ["Bench 2"]=new(298.5f,-140),["Bench 3"]=new(307.5f,-130),["Overlook road"]=new(317,-113.5f),["Overlook"]=new(335.5f,-124),["Shelf"]=new(329.5f,-142.5f),["Mast"]=new(319.5f,-162.5f),
        ["Shelf lower"]=new(343,-140),["East yard"]=new(347,-172),["Buried path"]=new(360,-172.5f),["East rim"]=new(345,-93),["Ridge path"]=new(302,-190),["Signalman's ledge"]=new(247,-183),
        ["Wood gate"]=new(286,-212),["Rim way"]=new(304,-234),["Ropeway head"]=new(324,-258),["Rim way N"]=new(258,-290),["Wood road"]=new(192,-228),["Deer track"]=new(262,-244),
        ["Jizo hollow"]=new(235,-254),["Hollow path"]=new(240,-218),["Hut"]=new(275,-266),["Charcoal pits"]=new(220,-228),["Long ledge"]=new(146,-194),["Signal spur"]=new(175,-184),
        ["Signal yard"]=new(110,-176),["Station tunnel"]=new(134,-176),["Old line E"]=new(88,-175.5f),["Old line W"]=new(55,-171),["Viaduct apron"]=new(38,-168),["Relay plaza"]=new(2,-165),
    };
    static readonly Vector2 Gate=new(52,-160),Court=World.SavePoints.Single(s=>s.Place=="Court").At,Halt=World.SavePoints.Single(s=>s.Place=="Halt").At;
    // The built decks, always there, and each stair once let down.
    static readonly (Vector2 A,Vector2 B)[] Decks=World.Decks.Select(d=>(d.A,d.B)).ToArray(),Bell=Stair("Bell stair"),HaltStair=Stair("Halt stair"),Great=Stair("Great stair");
    static (Vector2 A,Vector2 B)[] Deck(string name){var d=World.Decks.Single(d=>d.Name==name);return [(d.A,d.B)];}
    static (Vector2 A,Vector2 B)[] Stair(string name){var s=World.Stairs.Single(s=>s.Name==name);return [(s.Top,s.Foot)];}
    static Vector2 Top(string name)=>World.Stairs.Single(s=>s.Name==name).Top;
    // The Cut gate while it is barred, and the ways the checks shut off: (x, z, radius).
    static readonly (float X,float Z,float R) Barred=(World.CutGate.X,World.CutGate.Y,3.3f),GorgeW=(165,-98.3f,5),Ramp=(152,-108,3),Abutment=(81,-148,3),Mill=(191,-85,4),
        BackStair=(232.5f,-106,2.8f),FieldPath=(283,-79,3.5f),Hanging=(273,-133,3),Trestle=(272,-170.6f,2.5f),TerraceStair=(286.5f,-161,2.6f),Narrows=(99,-100.5f,4.5f),
        ChuteFoot=(115,-104,3),Canyon=(140,-100,8),GoatLedge=(186,-126,3),BenchEnd=(292.5f,-92.5f,3.8f),BellFoot=(113,-141.5f,3),Ramp2=(301.5f,-158,3.8f),
        OverlookRoad=(317,-113.5f,3.5f),Shelf=(329.5f,-142.5f,3),Ridge=(302,-190,3.5f),WoodGate=(286,-212,3.5f),RimN=(258,-290,3.5f),WoodRoad=(192,-228,3.5f),Ledge=(146,-194,4),
        YardRamp=(115,-192,3.5f);
    static bool[,] From(Vector2 start,IEnumerable<(Vector2,Vector2)>? links=null,params (float X,float Z,float R)[] shut)=>
        WildsTest.Reach(new((int)Mathf.Round(start.X),(int)Mathf.Round(start.Y)),(x,z)=>shut.Any(s=>new Vector2(x-s.X,z-s.Z).Length()<=s.R),links).Seen;
    static bool Has(bool[,] seen,string place){var p=Places[place];return seen[(int)Mathf.Round(p.X)-Area.Position.X,(int)Mathf.Round(p.Y)-Area.Position.Y];}
    static readonly Rect2I Area=WildsTest.Area;
    static void Every(bool[,] seen,string what,params string[] places)=>AssertThat(places.Where(p=>!Has(seen,p))).OverrideFailureMessage($"{what}: not reached {string.Join(", ",places.Where(p=>!Has(seen,p)))}").IsEmpty();
    static void None(bool[,] seen,string what,params string[] places)=>AssertThat(places.Where(p=>Has(seen,p))).OverrideFailureMessage($"{what}: reached {string.Join(", ",places.Where(p=>Has(seen,p)))}").IsEmpty();
    [TestCase]
    public void TheWayInAndLegOne()
    {
        var s=From(Gate,Decks,Barred,Narrows,ChuteFoot);
        Every(s,"from the rockfall's gate","Wayside road","Court","Shrine terrace","Wash under bridge","Pit");None(s,"  ...but not the Signal yard over the Court","Signal yard","Station tunnel","Old line E","Long ledge");
        s=From(Court,Decks,Barred,Ramp,GorgeW);
        Every(s,"the Court leads down to the Wash, the Pit and the canyon","Wash under bridge","Pit","Wash arm W","Wash arm E","Canyon end","Chute","Wash head");
        None(s,"  ...and with the canyon ramp shut nothing above","Catwalk","Pass road lower","Saddle","Bell ledge","Crossing");
        s=From(Court,Decks,Barred,GorgeW);
        Every(s,"up the ramp and the Catwalk","Catwalk","Pass road lower","Hairpin top","Saddle","Bell ledge","Pass road E","Crossing","Goat track");
        None(s,"  ...and nothing past the barred gate, nor the Signal yard or the Long ledge","Cut","Halt","Row","Goat ledge","Signal yard","Long ledge","Signal spur");
        Every(From(Places["Bell ledge"],null,Barred,BellFoot),"from the Bell ledge she can drop to the shrine terrace","Shrine terrace","Court");
        AssertThat(Has(From(Court,Decks,Barred,GorgeW,Ramp),"Bell ledge")).OverrideFailureMessage("without the Bell stair the terrace does not lead up to the ledge").IsFalse();
        Every(From(Court,Decks.Concat(Bell),Barred,GorgeW,Ramp),"with the Bell stair down she is on the pass road in a few steps","Bell ledge","Saddle","Crossing");
        Every(From(Places["Pass road W brink"],null,Barred,GorgeW),"the west brink drops into the chute and out to the canyon","Chute","Wash arm E");
        None(From(Places["Wash arm E"],null,Barred,GorgeW,Ramp,Abutment),"the chute does not climb back, and without the abutment stair the Wash has no other way up","Pass road W brink","Catwalk","Court");
    }
    [TestCase]
    public void LegTwoIsTheLongWayRound()
    {
        var s=From(Places["Canyon end"],null,Barred,Mill,Ramp);
        AssertThat(Has(s,"Gorge W")).IsTrue();None(s,"the weir stops the gorge: with the Mill ravine shut, nothing beyond","Gorge mid","Row","Halt","Sawmill hollow");
        s=From(Places["Canyon end"],null,Barred,Ramp,FieldPath);
        Every(s,"the Mill ravine leads to the sawmill, the flume, the kilns and the fields","Mill ravine","Sawmill hollow","Flume walk","Kilns","Kiln road","Fields");
        None(s,"  ...and with the field path shut, no further","Gorge mid","Row","Halt","East rim","Overlook");
        s=From(Places["Fields"],null,Barred,BackStair,Canyon);
        Every(s,"down the field path into the gorge, its north arm and head, and over the weir to its west reach","Field path","Gorge mid","Gorge N","Gorge head","Gorge W");
        None(s,"  ...but with the back stair shut, no way up to the Row, the Halt or the terraces","Row","Halt","Bench 1","Trestle end","Bridgehead");
        s=From(Places["Gorge mid"],null,Barred,Canyon,Hanging,GoatLedge);
        Every(s,"the back stair climbs to the Row","Row","Bridgehead");None(s,"  ...and without the hanging bridge the Row leads nowhere else","Halt","Bench 1","Bridge landing");
        s=From(Places["Row"],Deck("Hanging bridge"),Barred,BackStair,GoatLedge,BenchEnd,Trestle);
        Every(s,"over the hanging bridge to Bench 1 and down the terrace stair to the trestle's end","Bridge landing","Bench 1","Trestle end");
        AssertThat(Has(s,"Halt")).OverrideFailureMessage("  ...and without the trestle the Halt is still out of reach").IsFalse();
        s=From(Places["Trestle end"],Deck("Trestle"),Barred,TerraceStair,BackStair,Canyon);
        Every(s,"over the trestle to the Halt, its Starwell, and the Cut as far as the gate","Halt","Cut");None(s,"  ...but not through it","Crossing","Row");
        Every(From(Places["Halt"],Decks,TerraceStair,Canyon),"with the gate open the Cut runs through to the Crossing and the pass road","Crossing","Pass road E","Saddle");
        Every(From(Places["Row"],null,Barred,BackStair,Hanging),"from the Row the goat ledge drops to the goat track","Goat ledge","Goat track","Crossing");
        None(From(Places["Crossing"],Decks,Barred,GorgeW,Ramp),"  ...which does not climb onto the ledge","Goat ledge","Row");
        AssertThat(Has(From(Places["Halt"],Decks,Barred,TerraceStair,Hanging,BackStair),"Row")).OverrideFailureMessage("the Halt has no stair to the Row").IsFalse();
        s=From(Places["Halt"],null,Barred,Trestle,Canyon,BackStair);
        AssertThat(Has(s,"Gorge N")).OverrideFailureMessage("she can jump from the Halt into the gorge").IsTrue();None(s,"  ...and from there only the back stair or the fields lead out","Row","Bench 1");
    }
    [TestCase]
    public void LegThreeIsTheHeights()
    {
        var s=From(Places["Halt"],Decks,Barred,Hanging,BenchEnd);AssertThat(Has(s,"Bench 1")).IsTrue();None(s,"with the first ramp shut, Bench 1 is as far as she gets","Bench 2","Bench 3","Overlook","Mast");
        s=From(Places["Halt"],Decks,Barred,Hanging,Ramp2);AssertThat(Has(s,"Bench 2")).IsTrue();None(s,"with the second ramp shut, Bench 2","Bench 3","Overlook","Mast");
        s=From(Places["Halt"],Decks,Barred,Hanging,OverlookRoad);AssertThat(Has(s,"Bench 3")).IsTrue();None(s,"with the overlook road shut, Bench 3","Overlook","Mast","Shelf","East yard","Ridge path");
        s=From(Places["Halt"],Decks,Barred,Hanging,Shelf,BackStair,Canyon);
        Every(s,"the overlook road reaches the overlook, and the Shelf turn leads down to the East yard and the East rim","Overlook road","Overlook","Shelf lower","East yard","Buried path","East rim");
        None(s,"  ...and with the Shelf shut the Mast is not reached","Mast","Ridge path","Signalman's ledge","Wood gate");
        AssertThat(Has(From(Places["East rim"],null,Barred,BackStair,Canyon,(343.5f,-115.5f,3)),"Fields")).OverrideFailureMessage("the East rim drops to the Fields").IsTrue();
        None(From(Places["Fields"],null,Barred,FieldPath,(270,-51,3),(297,-38,3)),"  ...which do not climb onto it","East rim","Overlook");
        Every(From(Places["Overlook"],null,Barred,(339.5f,-111.5f,3.8f),WoodGate),"up the Shelf to the Mast, along the Ridge path and round to the Signalman's ledge","Shelf","Mast","Ridge path","Signalman's ledge");
        AssertThat(Has(From(Places["Signalman's ledge"],null,Barred,WoodGate,Ridge),"Halt")).OverrideFailureMessage("the Signalman's ledge drops onto the Halt").IsTrue();
        None(From(Places["Halt"],Decks,Barred,TerraceStair,Hanging,BackStair),"  ...but without the Halt stair the Halt does not lead up to it","Signalman's ledge","Ridge path","Wood gate");
        AssertThat(Has(From(Places["Halt"],Decks.Concat(HaltStair),Barred,TerraceStair,Hanging,BackStair),"Wood gate")).OverrideFailureMessage("with the Halt stair down the Halt leads up to the wood").IsTrue();
        None(From(Places["Mast"],null,Barred,Shelf,Ridge),"the Mast stands alone between the Shelf and the Ridge path, with no drop off it","Bench 3","Bench 2","Halt","Overlook","Signalman's ledge");
    }
    [TestCase]
    public void LegFourIsTheNorthWood()
    {
        var s=From(Places["Wood gate"],null,Barred,Ridge,(275,-200,3),RimN,(236,-270,3));
        Every(s,"from the Wood gate the Rim way leads to the Ropeway head","Rim way","Ropeway head");None(s,"  ...and the deer track's bank is too high to climb","Deer track","Jizo hollow","Hut","Wood road");
        s=From(Places["Ropeway head"],null,Barred,(304,-234,3),WoodRoad);
        Every(s,"the Rim way north leads on, and by the stump stair down to the Jizo hollow, the deer track, the hut and the hollow path","Rim way N","Jizo hollow","Deer track","Hut","Hollow path");
        Every(s,"  ...the deer track drops back to the Wood gate, and the hollow path onto the Signalman's ledge","Wood gate","Signalman's ledge");
        None(From(Places["Signalman's ledge"],null,Barred,WoodGate,Ridge),"  ...which does not climb the hollow path","Hollow path","Jizo hollow");
        Every(From(Places["Wood road"],null,Barred,(202,-256,3)),"down the Wood road to the charcoal pits, the Long ledge, the Signal spur and the Signal yard","Charcoal pits","Long ledge","Signal spur","Signal yard","Station tunnel");
        Every(From(Places["Signal yard"],Decks,Barred,YardRamp),"from the yard the old line runs west over the girder bridge to the viaduct","Old line E","Old line W","Viaduct apron");
        AssertThat(Has(From(Places["Signal spur"],null,Barred,Ledge,(180,-194,3)),"Crossing")).OverrideFailureMessage("the Signal spur drops onto the Crossing").IsTrue();
        None(From(Places["Crossing"],Decks,Barred,GorgeW,Ramp),"  ...which does not climb it","Signal spur","Long ledge");
        AssertThat(Has(From(Places["Signal yard"],null,Barred,YardRamp,(88,-176,2.5f)),"Court")).OverrideFailureMessage("the Signal yard drops into the Court").IsTrue();
        Every(From(Court,Decks.Concat(Great),Barred,GorgeW,Ramp),"with the Great stair down the Court leads up to the yard, the Station tunnel and the Long ledge","Signal yard","Station tunnel","Long ledge");
        AssertThat(Has(From(Places["Old line W"],null,Barred),"Wayside road")).OverrideFailureMessage("the old line drops onto the old road by the gate").IsTrue();
        AssertThat(Has(From(Places["Viaduct apron"],null,Barred),"Relay plaza")).OverrideFailureMessage("the viaduct's apron drops to the station road and the relay plaza").IsTrue();
    }
    // No leg can be skipped: shut each one's middle and nothing past it is reached.
    [TestCase]
    public void NoLegCanBeSkipped()
    {
        None(From(Gate,Decks,Barred,Mill),"without the Mill ravine (leg 2) nothing of the middle, the heights, the wood or the yard is reached","Halt","Row","Bench 1","Mast","Wood gate","Long ledge","Signal yard");
        None(From(Gate,Decks,Barred,Shelf),"without the Shelf (leg 3), neither the Mast, the wood nor the yard","Mast","Signalman's ledge","Wood gate","Long ledge","Signal yard");
        None(From(Gate,Decks,Barred,RimN),"without the Rim way north (leg 4), neither the Wood road nor the yard","Wood road","Long ledge","Signal yard","Station tunnel");
    }
    // The plan's walks, which its layout rules rest on, measured on the built ground: each leg is a long walk, within a tenth of the plan's,
    // and the shortcut it opens saves most of it. The Bell stair's foot lies 4 m further out than the plan's, so its walk is a little longer.
    [TestCase]
    public void EachShortcutIsEarnedByALongWalk()
    {
        static bool Shut(int x,int z)=>new Vector2(x-World.CutGate.X,z-World.CutGate.Y).Length()<=Barred.R;
        var gateEast=new Vector2(194,-166.2f);var bell=Decks.Concat(Bell).ToArray();
        foreach(var (what,walked,plan) in new[]{("the rockfall's gate to Starwell 1",WildsTest.Walk(Gate,Court,Shut,Decks),42f),("leg 1, Starwell 1 to the Bell stair's head",WildsTest.Walk(Court,World.BellTop,Shut,Decks),231),
            ("leg 2, Starwell 1 to Starwell 2",WildsTest.Walk(Court,Halt,Shut,Decks),519),("Starwell 2 back down the Cut to the gate's bar",WildsTest.Walk(Halt,gateEast,Shut,Decks),56),
            ("leg 3, Starwell 2 to the Halt stair's head",WildsTest.Walk(Halt,Top("Halt stair"),Shut,Decks),397),("leg 4, Starwell 2 to the Great stair's head",WildsTest.Walk(Halt,Top("Great stair"),Shut,Decks.Concat(HaltStair)),431),
            ("the way out, the Great stair's head to the Station tunnel",WildsTest.Walk(Top("Great stair"),World.StationMouth,Shut,Decks),39),("the rockfall's gate to the Station tunnel, nothing open",WildsTest.Walk(Gate,World.StationMouth,Shut,Decks),1144)})
            AssertThat(Math.Abs(walked/plan-1)).OverrideFailureMessage($"{what}: {walked:F0} m, the plan's {plan} m").IsLess(.1f);
        AssertThat(WildsTest.Walk(Court,Places["Bell ledge"],Shut,bell)).OverrideFailureMessage("with the Bell stair down the ledge is a short climb from Starwell 1").IsLess(50);
        AssertThat(WildsTest.Walk(Court,Halt,null,bell)).OverrideFailureMessage("with the Cut gate open Starwell 2 is under 210 m from Starwell 1").IsLess(210);
        // With everything open the maze is quick to pass through, within a seventh of the plan's walks (the Halt stair is longer than the
        // plan's), and each shortcut, taken away with the other three open, brings back a long walk to its far side.
        var all=Decks.Concat(Bell).Concat(HaltStair).Concat(Great).ToArray();
        foreach(var (what,walked,plan) in new[]{("the rockfall's gate to the Station tunnel",WildsTest.Walk(Gate,World.StationMouth,null,all),91f),("Starwell 1 to the Station tunnel",WildsTest.Walk(Court,World.StationMouth,null,all),55),
            ("Starwell 2 to the Station tunnel",WildsTest.Walk(Halt,World.StationMouth,null,all),232),("Starwell 2 to the Mast",WildsTest.Walk(Halt,Places["Mast"],null,all),109)})
            AssertThat(walked/plan).OverrideFailureMessage($"everything open, {what}: {walked:F0} m, the plan's {plan} m").IsLess(1.15f);
        foreach(var (what,without,with,plan) in new[]{("the Bell stair",WildsTest.Walk(Court,World.BellTop,null,Decks.Concat(HaltStair).Concat(Great)),WildsTest.Walk(Court,World.BellTop,null,all),205f),
            ("the Cut gate",WildsTest.Walk(Court,Halt,Shut,all),WildsTest.Walk(Court,Halt,null,all),303),
            ("the Halt stair",WildsTest.Walk(Halt,Top("Halt stair"),null,Decks.Concat(Bell).Concat(Great)),WildsTest.Walk(Halt,Top("Halt stair"),null,all),397),
            ("the Great stair",WildsTest.Walk(Court,Top("Great stair"),null,Decks.Concat(Bell).Concat(HaltStair)),WildsTest.Walk(Court,Top("Great stair"),null,all),620)})
        {
            AssertThat(Math.Abs(without/plan-1)).OverrideFailureMessage($"without {what}: {without:F0} m, the plan's {plan} m").IsLess(.1f);
            AssertThat(with).OverrideFailureMessage($"{what} saves too little: {with:F0} m with it, {without:F0} m without").IsLess(without*.7f);
        }
    }
    // The first walk, nothing open but the decks, reaches every place; from every floor she can walk back to a Starwell; and nothing reached
    // lies off the trails' floors and the cliffs she slides down between them (a drop's face lies up to 3.5 m from both its floors), nor
    // where the 1 m ground ends.
    [TestCase]
    public void TheMazeIsClosedAndEveryFloorLeadsBackToAStarwell()
    {
        var walked=From(Gate,Decks,Barred);
        Every(walked,"the first walk","Wayside road","Court","Shrine terrace","Wash under bridge","Wash head","Pit","Wash arm W","Wash arm E","Canyon end","Catwalk","Pass road W brink","Chute",
            "Pass road lower","Hairpin top","Saddle","Bell ledge","Pass road E","Crossing","Goat track","Cut","Halt","Trestle end","Goat ledge","Row","Bridgehead","Bridge landing",
            "Gorge W","Gorge mid","Gorge N","Gorge head","Mill ravine","Sawmill hollow","Flume walk","Kilns","Kiln road","Fields","Field path","Bench 1","Bench 2","Bench 3","Overlook road","Overlook",
            "Shelf","Mast","Shelf lower","East yard","Buried path","East rim","Ridge path","Signalman's ledge","Wood gate","Rim way","Ropeway head","Rim way N","Wood road","Deer track","Jizo hollow",
            "Hollow path","Hut","Charcoal pits","Long ledge","Signal spur","Signal yard","Station tunnel","Old line E","Old line W","Viaduct apron");
        var home=new[]{Court,Halt}.Select(p=>WildsTest.Reach(new((int)p.X,(int)p.Y),(x,z)=>new Vector2(x-World.CutGate.X,z-World.CutGate.Y).Length()<=3.3f,Decks,true).Seen).ToArray();
        var stuck=WildsTest.Cells((walked,Area)).Where(c=>World.InMaze(c.X,c.Y)&&World.MazeEdge(c.X,c.Y)<0&&!home.Any(h=>h[c.X-Area.Position.X,c.Y-Area.Position.Y])).ToArray();
        AssertThat(stuck).OverrideFailureMessage($"{stuck.Length} floor cells with no walk back to a Starwell, e.g. {string.Join(", ",stuck.Take(5))}").IsEmpty();
        var open=From(Gate,Decks.Concat(Bell).Concat(HaltStair).Concat(Great));
        var off=WildsTest.Cells((open,Area)).Where(c=>c.X>World.HalfWidth+16&&World.MazeEdge(c.X,c.Y)>3.5f).ToArray();
        AssertThat(off).OverrideFailureMessage($"{off.Length} cells reached off the trails, e.g. {string.Join(", ",off.Take(5))}").IsEmpty();
        var edge=WildsTest.Cells((open,Area)).Where(c=>new Vector2I[]{new(1,0),new(-1,0),new(0,1),new(0,-1)}.Any(d=>!World.IsFine(c+d))).ToArray();
        AssertThat(edge).OverrideFailureMessage($"reached the 1 m ground's edge at {string.Join(", ",edge.Take(5))}").IsEmpty();
    }
    // The maze's ground is continuous where its trails meet, walked in 5 cm strides along every other metre line as TheRelayGroundHasNoSteps does.
    [TestCase]
    public void TheMazeGroundHasNoSteps()
    {
        float H(Vector2 p)=>World.Height(p.X,p.Y);
        float Jump(Vector2 a,Vector2 b){for(int i=0;i<10;i++){var m=(a+b)/2;if(Math.Abs(H(m)-H(a))>Math.Abs(H(b)-H(m)))b=m;else a=m;}return Math.Abs(H(b)-H(a));}
        IEnumerable<string> Walk(Vector2 from,Vector2 stride,int count)
        {float h=H(from);for(int i=0;i<count;i++){var a=from+stride*i;float next=H(a+stride);if(Math.Abs(next-h)>.2f&&World.MazeEdge(a.X,a.Y)<3&&Jump(a,a+stride)>.05f)yield return $"({a.X:F2}, {a.Y:F2})";h=next;}}
        var m=World.MazeFine;
        var lines=Enumerable.Range(0,m.Size.Y/2).Select(k=>(From:new Vector2(m.Position.X,m.Position.Y+2*k),Stride:new Vector2(.05f,0),Count:m.Size.X*20))
            .Concat(Enumerable.Range(0,(m.End.X-52)/2).Select(k=>(From:new Vector2(52+2*k,m.Position.Y),Stride:new Vector2(0,.05f),Count:m.Size.Y*20)));
        var steps=lines.AsParallel().SelectMany(l=>Walk(l.From,l.Stride,l.Count).ToList()).ToList();
        AssertThat(steps).OverrideFailureMessage($"{steps.Count} steps, e.g. {string.Join(", ",steps.Take(5))}").IsEmpty();
    }
    // Every robot, pickup, Starwell, trap and lock-in gate stands on a floor, each yard's gates outside it and its robots in it; a dormant
    // scout is a scout.
    [TestCase]
    public void WhatStandsInTheMazeStandsOnItsFloors()
    {
        var spots=World.MazeRobots.Select(r=>($"{r.Kind} at {r.At}",r.At)).Concat(World.MazeCells.Select(c=>($"cell at {c}",c))).Append(("pouch",World.KilnPouch)).Append(("pouch",World.EastPouch))
            .Concat(World.SavePoints.Where(s=>World.InMaze(s.At.X,s.At.Y)).Select(s=>(s.Place,s.At))).Concat(World.Loads.SelectMany(l=>new[]{(l.Name,l.From),(l.Name,l.To)}))
            .Concat(World.Locks.SelectMany(l=>l.Gates.Select(g=>($"{l.Yard} gate",g.At))));
        var off=spots.Where(s=>World.MazeEdge(s.Item2.X,s.Item2.Y)>-.4f).Select(s=>s.Item1).ToArray();
        AssertThat(off).OverrideFailureMessage($"off the floors: {string.Join(", ",off)}").IsEmpty();
        AssertThat(World.MazeRobots.Count(r=>r.Kind=="patrol")).IsEqual(11);AssertThat(World.MazeRobots.Count(r=>r.Kind=="scout")).IsEqual(40);AssertThat(World.MazeRobots.Count(r=>r.Kind=="drone")).IsEqual(30);
        AssertThat(World.MazeRobots.Count(r=>r.Sleeps!=null)).IsEqual(16);AssertThat(World.MazeCells.Length).IsEqual(11);
        static Vector3 On(Vector2 p)=>new(p.X,World.Height(p.X,p.Y),p.Y);
        foreach(var (yard,gates) in World.Locks)
        {
            AssertThat(gates.Any(g=>World.InYard(yard,On(g.At)))).OverrideFailureMessage($"a gate stands in the {yard}").IsFalse();
            AssertThat(World.MazeRobots.Count(r=>World.InYard(yard,On(r.At)))).OverrideFailureMessage($"the {yard} holds robots").IsGreater(1);
        }
        AssertThat(World.MazeRobots.Where(r=>r.Sleeps!=null).All(r=>r.Kind=="scout")).IsTrue();
    }
}
