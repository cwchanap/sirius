namespace Hoshikawa;

// Scene 1's visual-novel script. A blank speaker is narration. Zero talks through Mio's wrist terminal and has no portrait:
// nobody in the valley has seen its body. It is curious like a child, counts things, and only knows railway words.
// It asks about everything except Mio's mother; the epilogue's change of subject is the first hint of its secret.
// Yui, the pass courier, is quick, bright and never short of words.
public static class Prologue
{
    // Over the valley before Mio wakes: Game.Begin() flies the camera from the city on the eastern ridge to her house.
    public static readonly (string Speaker,string Text)[] Opening=
    {
        ("","In 2147, the great cities still shine at night. Their streets belong to SIRIUS and its machines."),
        ("","Twelve years ago, the cities went dark to the valleys. Out in the Mikawa Highlands, Hoshikawa learned to live offline."),
        ("","A city freight line still crosses the highlands. Its trains never stop."),
    };
    // In Mio's room, its own small map: she wakes, gets up, and Zero reads her the train's manifest.
    public static readonly (string Speaker,string Text)[] Intro=
    {
        ("Zero","Mio? Mio. Are you awake?"),
        ("Mio","...Mm. No."),
        ("Zero","Your heartbeat says you're awake. Seventy-one departures a minute."),
        ("Mio","Good morning to you too, Zero. It's rude to count someone's heartbeat, you know."),
        ("Zero","Is it? Sorry. I'll count something else. Six tools on the floor. One telephone in pieces. You fell asleep fixing again."),
        ("Mio","It's Renji's old line telephone. The wiring is older than me."),
        ("Zero","...Wait. What is that smell? It's green, and wet, and a little bit sweet."),
        ("Mio","Rice paddies after the rain. Hoshikawa smells like this every morning in early summer."),
        ("Zero","Rice paddies after the rain. I'm keeping that one."),
        ("Zero","Mio, I read the city timetable while you were asleep. A freight train crosses the valley in three days. Eleven carriages."),
        ("Zero","Cargo: medicine, batteries, rice seed. Passengers listed: zero."),
        ("Mio","Medicine... Chiaki's shelves have been empty since spring."),
        ("Zero","Why doesn't the train stop? My map has a station in your valley. Hoshikawa Station, platform two."),
        ("Mio","No train has stopped there since the cities went dark. Twelve years. They just pass through."),
        ("Zero","A station where no train stops is a very lonely station."),
        ("Mio","How do you even read a city timetable, Zero?"),
        ("Zero","I'm a railway system. Timetables are what I'm made of. The city never deleted me. It just forgot I was here."),
    };
    // The old photograph on her wall. Zero, who asks about everything, does not ask about this.
    public static readonly (string Speaker,string Text)[] Photo=
    {
        ("Mio","Morning, Mom."),
        ("Zero","Mio, the door is this way!"),
    };
    // Outside her door, when the village loads: Yui has come down from Komorebi overnight.
    public static readonly (string Speaker,string Text)[] Gate=
    {
        ("Yui","Mio! Mio, Mio, Mio! Good, you're up!"),
        ("Mio","Yui? You came all the way down from Komorebi?"),
        ("Yui","Walked all night! Well, jogged some of it. I went around the relay road, because guess what's camped on it now? Patrols! Three of them, with those creepy amber eyes."),
        ("Zero","One, two, three. I counted them too. ...Mio, who is this? She talks faster than an express train."),
        ("Yui","...Wait. Your wrist just talked. Mio, your wrist just talked!"),
        ("Mio","That's Zero. Zero, this is Yui. She carries letters over the pass."),
        ("Zero","Hello, Yui! What's in your bag?"),
        ("Yui","Hello, Zero! Ooh, you're polite. Letters, mostly. Other people's words. And this one's for Hoshikawa."),
        ("Yui","It's from Emiko. Komorebi has one week of medicine left. She's asking if you can spare any."),
        ("Mio","We can't. We're down to a week too. And Grandpa Tetsu's cough is getting worse."),
        ("Zero","Mio. The train has medicine."),
        ("Yui","Wait, what train? There's a train? Okay, now you have to tell me everything."),
        ("Mio","Come on. Renji needs to hear this."),
        ("Yui","Race you to the workshop!"),
    };
    public static readonly (string Speaker,string Text)[] Briefing=
    {
        ("Renji","Mio. And Yui? You're both up early."),
        ("Yui","Morning, Renji! I won. Don't ask what."),
        ("Mio","Zero read the city timetable. A freight train comes through in three days, and it's carrying medicine."),
        ("Yui","And Komorebi has a week left. So do you. Emiko wrote it all down. Here."),
        ("Renji","...Then we take that medicine off the train."),
        ("Renji","We can't stop a city train out on the line. But the old line's signals are still standing. Dead for twelve years, every one of them."),
        ("Renji","The manual relay below the viaduct feeds them. Bring it back, and the line answers to us again."),
        ("Renji","Three patrol units are camped on the relay road. Clear them and restart the relay."),
        ("Zero","Three patrols. Amber eyes, no keepers. I'll count them down for you."),
        ("Yui","I'm coming too! I know that road. Well, I know around that road."),
        ("Renji","Not this time. You walked all night, and Emiko needs an answer. Go and see Chiaki at the clinic. She'll want to know what that train is carrying."),
        ("Yui","...Fine. But when that train comes, I'm going. That's a promise, not a question!"),
        ("Mio","Cross the bridge, follow the lanterns uphill. I know the way."),
        ("Renji","Don't let them surround you. And Mio, come back in one piece. That terminal on your wrist is the only line we have into the city."),
    };
    // Yui at the workshop door before Renji has heard the news.
    public static readonly (string Speaker,string Text)[] YuiWaits=
    {
        ("Yui","Renji's right there! Go on, go on. I'd tell him myself, but I'd tell it too fast."),
    };
    public static readonly (string Speaker,string Text)[] RenjiReminder=
    {
        ("Renji","Cross the bridge and follow the lanterns uphill. Three patrol units are blocking the manual relay. Do not let them surround you."),
    };
    public static readonly (string Speaker,string Text)[] RenjiRoadClear=
    {
        ("Renji","The road is clear? Then the relay is yours. Follow the lanterns up to the viaduct and wake it up."),
    };
    public static readonly (string Speaker,string Text)[] RenjiRest=
    {
        ("Renji","The signals are ready. Zero is still in the city, watching the timetable for us. Rest for a while. You have earned it."),
    };
    public static readonly (string Speaker,string Text)[] Homecoming=
    {
        ("Renji","I can see the relay's lamp from the workshop roof. Green, for the first time since the line closed."),
        ("Renji","Every signal down the line has power again. When that train comes, Hoshikawa Station can show it red, and for once it will have to stop."),
        ("Zero","Eleven carriages. I'll open every single one. Locks are just very small level crossings."),
        ("Renji","You woke the line up, Mio. Tonight, that is enough."),
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
    public static readonly (string Speaker,string Text)[] RelayAsleep=
    {
        ("Zero","This is the manual relay! It's asleep. Renji has a plan for it. Go and find Renji first, okay?"),
    };
    public static readonly (string Speaker,string Text)[] RelayGuardedNone=
    {
        ("Zero","The patrols keep polling this relay. I count none still out there. Chase them away first, okay?"),
    };
    public static readonly (string Speaker,string Text)[] RelayGuardedOne=
    {
        ("Zero","The patrols keep polling this relay. I count one still out there. Chase them away first, okay?"),
    };
    public static readonly (string Speaker,string Text)[] RelayGuardedTwo=
    {
        ("Zero","The patrols keep polling this relay. I count two still out there. Chase them away first, okay?"),
    };
    public static readonly (string Speaker,string Text)[] RelayGuardedThree=
    {
        ("Zero","The patrols keep polling this relay. I count three still out there. Chase them away first, okay?"),
    };
    // Zero counts the patrols that are left; a scene each, so every language words the count its own way.
    public static (string Speaker,string Text)[] RelayGuardedFor(int left)=>Math.Clamp(left,0,3) switch {0=>RelayGuardedNone,1=>RelayGuardedOne,2=>RelayGuardedTwo,_=>RelayGuardedThree};
    public static readonly (string Speaker,string Text)[] RelaySteady=
    {
        ("Zero","The relay is steady. Head back to Renji. ...Mio, one day I want to see this valley with my own eyes."),
    };
    public static readonly (string Speaker,string Text)[] RelayOnline=
    {
        ("Zero","Got it! The relay lamp just turned green. That's your signal, Mio. It's so clear."),
        ("Zero","Wait. Is that wind? It came down the mountain and it smells sharp and clean. What is it?"),
        ("Mio","Cedar. The whole pass is full of them."),
        ("Zero","Cedar. I'm keeping that one too."),
        ("Zero","Every signal on the old line just woke up, all the way to Hoshikawa Station. Now that train has to listen to us. Go tell Renji!"),
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
        ["Haruka"]=new[]
        {
            ("Haruka","KOMA keeps our irrigation running without a network. Old machines are our neighbors. The amber-eyed patrols beyond the bridge are not."),
            ("KOMA-7","Correction: this unit is the best neighbor."),
            ("Haruka","He's been like this since Grandpa Tetsu registered him. Forty years of the same jokes."),
            ("KOMA-7","Registered keeper: Mori Tetsuzo. Keeper status: napping on the engawa. This unit answers to its keeper, not to the network."),
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
        ["Chiaki"]=new[]
        {
            ("Chiaki","Hold your guard against a strike, or tap it just before impact to deflect. A broken posture leaves you exposed. Let me patch you up."),
            ("Chiaki","And Mio, my shelves are bare. One week of medicine for the whole village. If that train is carrying any, bring it home."),
        },
        ["Sota"]=new[]
        {
            ("Sota","Watch for the red diamond on a patrol: that sweeping attack cannot be blocked. Dodge through it or jump away."),
            ("Zero","A red diamond. Like a stop signal! Stop signals keep everyone safe."),
            ("Sota","...Mio, is your wrist talking again?"),
        },
        ["Yui"]=new[]
        {
            ("Yui","Chiaki is teaching me medicines! This one's for fevers, this one's for coughs, and this one tastes awful. I tried it."),
            ("Zero","What does awful taste like?"),
            ("Yui","Like the inside of an old boot! You're lucky you don't have a mouth, Zero."),
        },
    };
    // Zero's relay reminders repeat on every visit, so they stay unvoiced and IntegrationCheck does not ask for their mp3s.
    public static readonly HashSet<string> Unvoiced=new(){"RelayAsleep","RelayGuardedNone","RelayGuardedOne","RelayGuardedTwo","RelayGuardedThree","RelaySteady"};
    // Mio's and Zero's lines written or changed since the last recording, which IntegrationCheck lets go without an mp3 until they are voiced.
    public static readonly HashSet<string> Unrecorded=new(){"Intro.0","Intro.1","Intro.2","Intro.4","Intro.5","Intro.6","Intro.9","Intro.10","Intro.11","Intro.12",
        "Intro.13","Intro.14","Intro.15","Intro.16","Photo.0","Photo.1","Gate.1","Gate.3","Gate.5","Gate.6","Gate.9","Gate.10","Gate.12","Briefing.2","Briefing.8",
        "RelayOnline.4","FirstMeeting.Yui.1"};
    // What the 3D scene does when a beat starts: a camera shot and the clips its actors play ("Mio clip, Yui clip"). A beat with no entry, or
    // an empty part, keeps what was there. Shots: "over A B" looks past A's shoulder at B, "two A B" frames both from the side, "face A"
    // is in front of A, and any other name is a fixed shot (Game.Shots). Every actor goes back to its resting clip when the scene ends.
    public static readonly Dictionary<string,(string Shot,string Acts)> Stage=new()
    {
        ["Opening.0"]=("city",""),["Opening.1"]=("valley",""),["Opening.2"]=("home",""),
        ["Intro.0"]=("bed","Mio sleep"),["Intro.2"]=("futon","Mio wake"),["Intro.3"]=("face Mio","Mio terminal_sit"),["Intro.4"]=("floor","Mio sit"),
        ["Intro.5"]=("face Mio",""),["Intro.6"]=("room","Mio rise"),["Intro.7"]=("shoji","Mio idle"),["Intro.8"]=("backlit",""),
        ["Intro.9"]=("face Mio","Mio terminal"),["Intro.11"]=("shoji","Mio idle"),["Intro.13"]=("room",""),["Intro.14"]=("backlit",""),
        ["Intro.15"]=("face Mio","Mio terminal"),["Intro.16"]=("","Mio idle"),
        ["Photo.0"]=("photo",""),
        ["Gate.0"]=("two Mio Yui","Yui wave"),["Gate.1"]=("over Mio Yui","Yui idle"),["Gate.2"]=("","Yui point"),["Gate.3"]=("two Mio Yui","Mio terminal, Yui idle"),
        ["Gate.4"]=("over Mio Yui","Mio idle"),["Gate.5"]=("over Yui Mio",""),["Gate.6"]=("two Mio Yui","Mio terminal"),["Gate.7"]=("over Mio Yui","Mio idle, Yui give"),
        ["Gate.8"]=("","Mio terminal, Yui idle"),["Gate.9"]=("over Yui Mio","Mio idle"),["Gate.10"]=("two Mio Yui",""),["Gate.11"]=("over Mio Yui","Yui wave"),
        ["Gate.12"]=("over Yui Mio","Yui idle"),["Gate.13"]=("two Mio Yui","Yui point"),
        ["Briefing.0"]=("over Mio Renji","Renji idle"),["Briefing.1"]=("face Yui","Yui wave"),["Briefing.2"]=("two Mio Renji","Mio terminal, Yui idle"),
        ["Briefing.3"]=("face Yui","Mio idle, Yui give"),["Briefing.4"]=("over Mio Renji","Renji read, Yui idle"),["Briefing.5"]=("","Renji point"),
        ["Briefing.6"]=("","Renji idle"),["Briefing.7"]=("two Mio Renji",""),["Briefing.8"]=("","Mio terminal"),["Briefing.9"]=("face Yui","Mio idle, Yui wave"),
        ["Briefing.10"]=("over Mio Renji","Yui idle"),["Briefing.11"]=("face Yui","Yui fold"),["Briefing.12"]=("over Renji Mio",""),["Briefing.13"]=("over Mio Renji",""),
    };
    // What the Komorebi residents say every time: they have no first-meeting scene.
    public static readonly Dictionary<string,(string Speaker,string Text)[]> Komorebi=new()
    {
        ["Naomi"]=new[]
        {
            ("Naomi","Welcome to Komorebi, Mio. Travelers from Hoshikawa take the cedar bridge below the orchards. There is always a place by our stove when the mountain mist rolls in."),
        },
        ["Emiko"]=new[]
        {
            ("Emiko","That pass is a long walk. Sit for a moment: I can treat your wounds and refill your remedies. The clinic keeps its doors open for both villages."),
        },
        ["Toma"]=new[]
        {
            ("Toma","These trees fed our grandparents before the city grid reached the valley. Now they feed our neighbors again. Follow the south lanterns to the bridge if you are returning to Hoshikawa."),
        },
    };
    // Afterwards the others repeat the first line of their first meeting, which keeps that beat's key (and voice file).
    private static readonly Dictionary<string,(string Speaker,string Text)[]> openers=FirstMeeting.ToDictionary(m=>m.Key,m=>m.Value[..1]);
    public static (string Speaker,string Text)[] Usual(string name)=>Komorebi.TryGetValue(name,out var line)?line:openers[name];
    // Beat i of a scene is voiced by assets/audio/voice/ja/<Speaker>/<name>.<i>.mp3, the same key as assets/dialogue/<lang>.json.
    // Declared last so the scenes it names are already initialized.
    public static readonly Dictionary<(string Speaker,string Text)[],string> SceneNames=new[]{("Opening",Opening),("Intro",Intro),("Photo",Photo),("Gate",Gate),
        ("Briefing",Briefing),("YuiWaits",YuiWaits),("Homecoming",Homecoming),("RelayOnline",RelayOnline),("RenjiReminder",RenjiReminder),("RenjiRoadClear",RenjiRoadClear),
        ("RenjiRest",RenjiRest),("RelayAsleep",RelayAsleep),("RelayGuardedNone",RelayGuardedNone),("RelayGuardedOne",RelayGuardedOne),
        ("RelayGuardedTwo",RelayGuardedTwo),("RelayGuardedThree",RelayGuardedThree),("RelaySteady",RelaySteady)}
        .Concat(FirstMeeting.Select(m=>("FirstMeeting."+m.Key,m.Value))).Concat(Komorebi.Select(m=>("Komorebi."+m.Key,m.Value)))
        .Concat(openers.Select(m=>("FirstMeeting."+m.Key,m.Value))).ToDictionary(s=>s.Item2,s=>s.Item1);
}
