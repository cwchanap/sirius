using GdUnit4;
using GodotE2E;
using static GdUnit4.Assertions;
using static Hoshikawa.E2E.Hoshikawa;
namespace Hoshikawa.E2E;

[TestSuite]
public class PrologueFlowTest
{
    [TestCase]
    public async Task TitleOpensOverTheValleyThenInMiosRoom()=>await E2EGame.RunAsync(Options(),async (game,_)=>
    {
        AssertThat(await Mode(game)).IsEqual(Title);
        await PressEnter(game);await game.WaitForPropertyAsync(Root,"Mode",Dialogue);
        AssertThat(await Get<string>(game,Root,"Speaker")).IsEqual("");
        // Under --fixed-fps every frame is 1/60 s of game time: three seconds lets the opening line finish typing, so one E moves on.
        await Frames(game,180);await PressE(game);
        AssertThat(await Get<int>(game,Root,"Beat")).IsEqual(1);
        // Escape skips the narration into Mio's room, where Zero wakes her; a second one leaves her free to head outside.
        await PressEscape(game);await Frames(game,2);
        AssertThat(await Get<string>(game,Root,"Speaker")).IsEqual("Zero");AssertThat(await game.CallMethodAsync<bool>(Root,"get","InRoom")).IsTrue();
        await PressEscape(game);await game.WaitForPropertyAsync(Root,"Mode",Playing);
        AssertThat(await game.CallMethodAsync<string>(Root,"get","Objective")).IsEqual("Head outside");
        AssertThat(await Get<int>(game,Root,"QuestStage")).IsEqual(0);
    });
    [TestCase]
    public async Task EscapePausesAndResumesTheJourney()=>await E2EGame.RunAsync(Options(),async (game,_)=>
    {
        await StartPlaying(game);
        await PressEscape(game);AssertThat(await Mode(game)).IsEqual(Paused);
        await PressEscape(game);AssertThat(await Mode(game)).IsEqual(Playing);
    });
    [TestCase]
    public async Task MioRunsWhereSheIsSteered()=>await E2EGame.RunAsync(Options(),async (game,_)=>
    {
        await StartPlaying(game);var start=await Position(game,Mio);
        await game.InputActionAsync("forward",true);await Frames(game,90);await game.InputActionAsync("forward",false);
        var end=await Position(game,Mio);
        // Out of her door the camera faces west, down -X along the house front towards the village.
        AssertThat(start.X-end.X).IsGreater(2.0);AssertThat(Math.Abs(end.Z-start.Z)).IsLess(.5);
    });
    [TestCase]
    public async Task TalkingToRenjiStartsTheRelayMission()=>await E2EGame.RunAsync(Options(),async (game,_)=>
    {
        await StartPlaying(game);
        var at=await Position(game,await Find(game,"Renji"));
        // Two metres south of Renji is the talk spot the self-test proves standable; Mio settles onto it under physics.
        await Place(game,Mio,at.X,at.Y+.3,at.Z+2);await Frames(game,10);
        await PressE(game);
        AssertThat(await Mode(game)).IsEqual(Dialogue);AssertThat(await Get<string>(game,Root,"Speaker")).IsEqual("Renji");
        AssertThat(await Get<int>(game,Root,"QuestStage")).IsEqual(1);
        AssertThat(await game.CallMethodAsync<string>(Root,"get","Objective")).StartsWith("Clear the old relay road");
    });
}
