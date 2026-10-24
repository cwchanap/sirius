using GdUnit4;
using Godot;
using static GdUnit4.Assertions;
namespace Hoshikawa.Tests;

[TestSuite]
public class CombatTest
{
    // A robot is exposed only within 60° of its back (Enemy.BehindDot); height and distance are Player.CanReach's to judge.
    [TestCase]
    public void OnlyARobotsBackIsExposed()
    {
        var robot=new Vector3(2,1,-30);var forward=new Vector3(0,0,1);
        AssertThat(Enemy.Behind(forward,robot,robot-forward*2)).IsTrue();
        AssertThat(Enemy.Behind(forward,robot,robot+new Vector3(1.5f,2,-2))).IsTrue();
        AssertThat(Enemy.Behind(forward,robot,robot+forward*2)).IsFalse();
        AssertThat(Enemy.Behind(forward,robot,robot+new Vector3(2,0,0))).IsFalse();
        AssertThat(Enemy.Behind(forward,robot,robot+new Vector3(2,0,-.5f))).IsFalse();
        AssertThat(Enemy.Behind(forward,robot,robot+Vector3.Up)).IsFalse();
    }
}
