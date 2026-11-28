using Godot;
namespace Hoshikawa;

// Into and out of Hoshikawa Station's inside (World.Station()), through the parcels door, and the main doors once leg 1 opens them: each a
// blackout, the valley hidden while Mio is inside. Its floors stack, so what found the ground with World.Height() asks Floor() instead,
// a ray down to whatever floor is under it.
public partial class Game
{
    public Node3D Station=null!;
    public bool InStation=>Hoshikawa.World.InStation(Player.GlobalPosition.X,Player.GlobalPosition.Z);
    private bool barred;
    // Where each door out leaves Mio outside, facing away from it, until the Outside map gives the station its own ground: the Station
    // tunnel's mouth in the Signal yard.
    private Vector3 Outside(string door)=>World.MazeEnd+Vector3.Left*3;
    public void EnterStation(string door)
    {
        var exit=Hoshikawa.World.StationExits.Single(e=>e.Name==door);ShowStation(true);
        Place(Hoshikawa.World.StationPoint(exit.Landing,exit.Floor)+Vector3.Up*.1f,Mathf.Pi-Mathf.DegToRad(exit.Facing),-Mathf.DegToRad(exit.Facing));
        spring.SpringLength=5.5f;Blackout=1;
    }
    private void LeaveStation(Hoshikawa.World.StationExit exit)
    {
        ShowStation(false);var at=Outside(exit.Name);Place(new(at.X,Hoshikawa.World.Height(at.X,at.Z)+.1f,at.Z),-Mathf.Pi/2,Mathf.Pi/2);
        spring.SpringLength=7.5f;Blackout=1;
    }
    private void ShowStation(bool inside){World.Visible=!inside;Station.Visible=inside;}
    private Hoshikawa.World.StationExit? StationExitHere()=>InStation?Hoshikawa.World.StationExits.FirstOrDefault(e=>(e.After==null||Opened.Contains(e.After))
        &&Hoshikawa.World.StationPoint(e.At,e.Floor).DistanceTo(Player.GlobalPosition)<2.2f):null;
    // The Station tunnel's mouth leads in at the parcels door until the Outside map gives the station its own ground. Each door out lands
    // Mio 3 m off it, outside this reach.
    private bool AtStationMouth=>!InStation&&Player.GlobalPosition.DistanceTo(World.MazeEnd)<2.5f;
    // Inside, a robot on another floor is out of her reach however near it is through the ceiling, so lock-on and the target panel pass over it.
    private bool SameFloor(Enemy e)=>!InStation||Hoshikawa.World.StationFloor(e.GlobalPosition)==Hoshikawa.World.StationFloor(Player.GlobalPosition);
    // Leg 1 ends, for now, with the main doors open and the grand stair still barred.
    private string StationObjective=>Opened.Contains("Main doors")?"Climb the grand stair":"Open the station's main doors";
    private Vector3 StationObjectivePosition=>Opened.Contains("Main doors")?Hoshikawa.World.StationPoint(Hoshikawa.World.StairBars,Hoshikawa.World.F1)
        :World.Parts.Single(p=>p.Name=="Main doors").From;
    // The floor under p: the valley's ground outside, and inside the station the first floor a ray meets below p.
    public float Floor(Vector3 p)
    {
        if(!Hoshikawa.World.InStation(p.X,p.Z))return Hoshikawa.World.Height(p.X,p.Z);
        var hit=GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(p+Vector3.Up*1.2f,p+Vector3.Down*6,1));
        return hit.Count>0?((Vector3)hit["position"]).Y:p.Y;
    }
}
