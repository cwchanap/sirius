namespace Hoshikawa;

// Scene 1's visual-novel script. A blank speaker is narration. Zero talks through Mio's wrist terminal and has no portrait:
// nobody in the valley has seen its body. It is curious like a child, counts things, and only knows railway words.
// It asks about everything except Mio's mother; the epilogue's change of subject is the first hint of its secret.
public static class Prologue
{
    public static readonly (string Speaker,string Text)[] Intro=
    {
        ("Zero","Mio? Mio. Are you awake? Your heartbeat says you're awake. Seventy-one departures a minute."),
        ("Mio","Good morning to you too, Zero. It's rude to count someone's heartbeat, you know."),
        ("Zero","Is it? Sorry. I'll count something else. ...Wait. What is that smell? It's green, and wet, and a little bit sweet."),
        ("Mio","Rice paddies after the rain. Hoshikawa smells like this every morning in early summer."),
        ("Zero","Rice paddies after the rain. I'm keeping that one."),
        ("Zero","Mio, the freight train is still on the timetable. Three days. Eleven carriages. Passengers listed: zero."),
        ("Mio","But there are people inside."),
        ("Zero","Yes. I can hear them through the carriage walls. I told Ren last night, through your terminal. He wants you at the workshop."),
        ("Mio","Then let's not keep him waiting."),
    };
    public static readonly (string Speaker,string Text)[] Briefing=
    {
        ("Ren","Mio. Zero passed the word through your terminal last night. I haven't slept since."),
        ("Ren","A freight train comes through the valley in three days. Zero says its cargo is people, taken out of the cities to somewhere worse."),
        ("Mio","Then we stop it."),
        ("Ren","We can't stop a city train. But the old Hoshikawa line still has a maintenance siding below the viaduct. Bring the manual relay back and we can switch the train onto it."),
        ("Ren","Three patrol units are camped on the relay road. Clear them and restart the relay. Zero can work the carriage locks from the city side."),
        ("Zero","One, two, three. Three patrols. I counted them yesterday. They have amber eyes and no keepers."),
        ("Mio","Cross the bridge, follow the lanterns uphill. I know the way."),
        ("Ren","Don't let them surround you. And Mio, come back in one piece. That terminal on your wrist is the only line we have into the city."),
    };
    public const string RenReminder="Cross the bridge and follow the lanterns uphill. Three patrol units are blocking the manual relay. Do not let them surround you.";
    public const string RenRoadClear="The road is clear? Then the relay is yours. Follow the lanterns up to the viaduct and wake it up.";
    public const string RenRest="The valley is ready. Zero is still in the city, preparing the other half of our rescue. Rest for a while. You have earned it.";
    public static readonly (string Speaker,string Text)[] Homecoming=
    {
        ("Ren","I can see the relay's lamp from the workshop roof. Green, for the first time since the line closed."),
        ("Ren","When the train comes, we switch it onto the siding, Zero opens the carriages, and we walk those people home."),
        ("Zero","Eleven carriages. I'll open every single one. Locks are just very small level crossings."),
        ("Ren","You made a way home, Mio. Tonight, that is enough."),
        ("","Night falls over Hoshikawa. One by one, the lanterns are lit."),
        ("Zero","Mio, what are the little lights for? There are twenty-three of them. Twenty-four now."),
        ("Mio","Lanterns. Chiyo says every lantern means someone is waiting for you."),
        ("Zero","Who's waiting for you?"),
        ("Mio","Everyone here, I suppose. My mother used to light one on our porch every night, even after the cities went dark to us—"),
        ("Zero","Mio! The station lights just changed. Something is moving on platform four. I have to go and look at the locks."),
        ("Mio","...Zero?"),
        ("Zero","Goodnight, Mio. Thank you for the rice paddies."),
        ("","The link goes quiet. Far beyond the viaduct, the city keeps shining."),
    };
    public const int NightFalls=4;
    public const string RelayAsleep="This is the manual relay! It's asleep. Ren has a plan for it. Go and find Ren first, okay?";
    public static string RelayGuarded(int left)=>$"The patrols keep polling this relay. I count {new[]{"none","one","two","three"}[Math.Clamp(left,0,3)]} still out there. Chase them away first, okay?";
    public const string RelaySteady="The relay is steady. Head back to Ren. ...Mio, one day I want to see this valley with my own eyes.";
    public static readonly (string Speaker,string Text)[] RelayOnline=
    {
        ("Zero","Got it! The relay lamp just turned green. That's your signal, Mio. It's so clear."),
        ("Zero","Wait. Is that wind? It came down the mountain and it smells sharp and clean. What is it?"),
        ("Mio","Cedar. The whole pass is full of them."),
        ("Zero","Cedar. I'm keeping that one too."),
        ("Zero","The maintenance siding is reachable now. I'm still at the city station. You opened the way out, so the locks are mine. Go tell Ren!"),
    };
    // Played the first time each resident is spoken to; afterwards they say their usual line.
    public static readonly Dictionary<string,(string Speaker,string Text)[]> FirstMeeting=new()
    {
        ["Chiyo"]=new[]
        {
            ("Chiyo","The cities still shine at night. But their streets belong to machines now. Here, every lantern means someone is waiting for you."),
            ("Chiyo","...That terminal on your wrist. May I? Gold casing, a little jade lamp."),
            ("Mio","I found it in the old signal box on the Hoshikawa line last year. It took me a month to get it working."),
            ("Chiyo","Shio wore one just like it. Your mother never went anywhere without hers."),
            ("Mio","Everyone who worked the line had one, didn't they?"),
            ("Chiyo","Perhaps. Perhaps they did."),
            ("","Chiyo looks past you, toward the city, and says nothing more."),
        },
        ["Hana"]=new[]
        {
            ("Hana","KOMA keeps our irrigation running without a network. Old machines are our neighbors. The amber-eyed patrols beyond the bridge are not."),
            ("KOMA-7","Correction: this unit is the best neighbor."),
            ("Hana","He's been like this since Grandpa Tetsu registered him. Forty years of the same jokes."),
            ("KOMA-7","Registered keeper: Mori Tetsu. Keeper status: napping on the engawa. This unit answers to its keeper, not to the network."),
            ("Mio","So that's why the patrols can't give you orders."),
            ("KOMA-7","Correct. Patrols have no keepers. Very lonely machines."),
        },
        ["KOMA-7"]=new[]
        {
            ("KOMA-7","Morning diagnostic: tomatoes healthy. Humans worried. Zero is still in the city. This unit is KOMA. Different machine; equally excellent company."),
            ("Zero","Hello, KOMA! What do tomatoes feel like?"),
            ("KOMA-7","Round. Warm in the afternoon. Unit Zero, you ask many questions for a railway system."),
            ("Zero","I only ever had trains. Mio has everything else."),
        },
        ["Aki"]=new[]
        {
            ("Aki","Hold your guard against a strike, or tap it just before impact to deflect. A broken posture leaves you exposed. Let me patch you up."),
            ("Aki","And if there really are people on that train, they'll need beds. I'm clearing out the clinic tonight."),
        },
        ["Sora"]=new[]
        {
            ("Sora","I saw three sentries on the old relay road. Watch for the red diamond: that sweeping attack cannot be blocked. Dodge through it or jump away."),
            ("Zero","A red diamond. Like a stop signal! Stop signals keep everyone safe."),
            ("Sora","...Mio, is your wrist talking again?"),
        },
    };
}
