namespace Hoshikawa;

// Scene 1's visual-novel script. A blank speaker is narration. Zero is a caring robot in the city that talks through Mio's wrist
// terminal and has no portrait: nobody in the valley has seen its body. The city lets it know only what its work needs, so it is
// curious like a child about everything else; it counts things and minds Mio's heartbeat, sleep and meals.
// It asks about everything except Mio's mother; the epilogue's change of subject is the first hint of its secret. Its other job, loading
// freight at the city's freight terminal by night, is how it knows the train. Everyone knows it as Zero, the name of Mio's terminal, and she
// calls it Zero anywhere; nobody but her knows a robot in the city is on the other end. Yui, the pass courier, is quick, bright and never
// short of words; she, Sota and Renji fight as well as Mio, and keeping the robots out of their villages is their work beside their trades.
public static class Prologue
{
    // Over the valley before Mio wakes: Game.Begin() flies the camera from the city on the eastern ridge to her house.
    public static readonly (string Speaker,string Text)[] Opening=
    {
        ("","In 2147, the great cities still shine at night. Their streets belong to SIRIUS and its machines."),
        ("","Twelve years ago, the cities went dark to the valleys. Out in the Mikawa Highlands, Hoshikawa learned to live offline."),
        ("","A city freight line still crosses the highlands. Its trains never stop."),
    };
    // In Mio's room, its own small map: she wakes, gets up, hears of the medicine Zero packed onto the train, and sets out for Renji.
    public static readonly (string Speaker,string Text)[] Intro=
    {
        ("Zero","Mio? Mio. Are you awake?"),
        ("Mio","...Mm. No."),
        ("Zero","Your heartbeat says you're awake. Seventy-one beats a minute."),
        ("Mio","Good morning to you too, Zero. It's rude to count someone's heartbeat, you know."),
        ("Zero","Is it? Sorry. I'll count something else. Six tools on the floor. One telephone in pieces. You fell asleep fixing again."),
        ("Mio","It's Renji's old line telephone. The wiring is older than me."),
        ("Zero","...Wait. What is that smell? It's green, and wet, and a little bit sweet."),
        ("Mio","Rice paddies after the rain. Hoshikawa smells like this every morning in early summer."),
        ("Zero","Rice paddies after the rain. I'm keeping that one."),
        ("Zero","Mio, guess what my work was last night. I packed a freight train. Eleven carriages: rice, dried fish, tinned food, and two hundred boxes of medicine."),
        ("Mio","Medicine? Where is it going?"),
        ("Zero","I don't know. The city only tells me what my work needs. Pack the boxes, close the doors. It leaves in three days."),
        ("Mio","There's only one line out of the city, Zero. It runs straight through this valley."),
        ("Zero","Then it goes right past you! Does it stop?"),
        ("Mio","Not anymore. City freight runs straight through on platform two and never slows down. No train has stopped at Hoshikawa Station in twelve years."),
        ("Zero","A station where no train stops is a very lonely station."),
        ("Mio","And Chiaki's shelves have been running low since spring. Two hundred boxes, and not one of them for us."),
        ("Mio","Renji has to hear this. Come on, Zero."),
        ("Zero","Wait, Mio. You haven't eaten since yesterday noon. Nineteen hours. I counted."),
        ("Mio","I'll grab a rice ball on the way. Let's go."),
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
        ("Yui","Tell Zero good morning from me too! ...I know, I know. It only ever answers you, Mio."),
        ("Yui","So ask it for me. What's in my bag?"),
        ("Mio","...Zero, what's in Yui's bag?"),
        ("Zero","Letters. Her bag always has letters."),
        ("Yui","Ha! Letters, mostly. Other people's words. And this one's for Hoshikawa."),
        ("Yui","It's from Emiko. Komorebi has one week of medicine left. She's asking if you can spare any."),
        ("Mio","We can't. We're down to a week too. And Grandpa Tetsu's cough is getting worse."),
        ("Mio","...Maybe there's a way. Something's coming down the line."),
        ("Yui","Down the line? You mean a train? Okay, now you have to tell me everything."),
        ("Mio","Come on. I'll tell you and Renji together."),
        ("Yui","Race you to the workshop!"),
    };
    public static readonly (string Speaker,string Text)[] Briefing=
    {
        ("Renji","Mio. And Yui? You're both up early."),
        ("Yui","Morning, Renji! We made it. Don't ask who won."),
        ("Mio","My terminal still reads the freight line's traffic over the old wires, so I asked it last night. A city train runs through on platform two in three days, with food and two hundred boxes of medicine."),
        ("Yui","And Komorebi has a week left. So do you. Emiko wrote it all down. Here."),
        ("Renji","...Then we take the medicine off that train. Only the medicine. Our paddies feed us; the food can go on to people who need it more."),
        ("Renji","We can't stop a city train out on the line. But the old line's signals are still standing. Dead for twelve years, every one of them."),
        ("Renji","The manual relay below the viaduct feeds them. Bring it back, and the line answers to us again."),
        ("Renji","Yui saw patrol units camped on the relay road. Clear any still there and restart the relay. ...Mio, can Zero keep count of them?"),
        ("Mio","Zero? Can you?"),
        ("Zero","Yes. The patrols poll the relay over the old line. This terminal will count the ones still out there."),
        ("Yui","I'm coming too! I know that road. Well, I know around that road."),
        ("Renji","Not this time. You walked all night, and Emiko needs an answer. Go and see Chiaki at the clinic. She'll want to know what that train is carrying."),
        ("Yui","...Fine. But when that train comes, I'm going. That's a promise, not a question!"),
        ("Mio","Cross the bridge, follow the lanterns uphill. I know the way."),
        ("Renji","Sota and I will keep watch over the village. Mio, come back in one piece. That terminal on your wrist is the only ear we have on that line."),
    };
    // Yui at the workshop door before Renji has heard the news.
    public static readonly (string Speaker,string Text)[] YuiWaits=
    {
        ("Yui","Renji's right there! Go on, go on. I'd tell him myself, but I'd tell it too fast."),
    };
    public static readonly (string Speaker,string Text)[] RenjiReminder=
    {
        ("Renji","Cross the bridge and follow the lanterns uphill. Patrol units are still blocking the manual relay. Do not let them surround you."),
    };
    public static readonly (string Speaker,string Text)[] RenjiRoadClear=
    {
        ("Renji","The road is clear? Then the relay is yours. Follow the lanterns up to the viaduct and wake it up."),
    };
    public static readonly (string Speaker,string Text)[] Homecoming=
    {
        ("Renji","I saw the relay's lamp from the workshop roof. Green, for the first time since the line closed. Mio, ask Zero about the rest of the line."),
        ("Mio","Zero, does every signal down the line have power again? Are they all green?"),
        ("Zero","All green. ...One near the village just went dark. ...It is green again now."),
        ("Renji","Good. When that train comes, platform two shows it red, and for once it will have to stop. You woke the line up, Mio."),
        ("","Night falls over Hoshikawa. Mio walks home alone, and one by one the lanterns are lit."),
        ("Zero","Mio, what are the little lights for? There are twenty-three of them. Twenty-four now."),
        ("Mio","Lanterns. Chiyo says every lantern means someone is waiting for you."),
        ("Zero","Who's waiting for you?"),
        ("Mio","Everyone here, I suppose. My mother used to light one on our porch every night, even after the cities went dark to us—"),
        ("Zero","Mio! The freight terminal's lights just changed. Something is moving on the loading platform. I have to go and look at the locks."),
        ("Mio","...Zero?"),
        ("Zero","Goodnight, Mio. Thank you for the rice paddies."),
        ("","The link goes quiet. Far beyond the viaduct, the city keeps shining."),
    };
    // The second day, from Mio's room: Renji learns platform two's departure signal answers only to the station's own lever, so someone has to get
    // inside, and she gets ready to set out with two companions. Day cards (Cards) open the mornings over black.
    public static readonly (string Speaker,string Text)[] DayTwo=
    {
        ("","Day two."),
        ("Zero","Mio? Morning. Sixty-eight beats a minute. I'm only counting a little."),
        ("Mio","...Morning, Zero."),
        ("Mio","Last night, on the loading platform. What was moving there?"),
        ("Zero","Nothing. ...That isn't my work."),
        ("Mio","...That doesn't sound like you."),
        ("Zero","Footsteps! I can hear footsteps outside. Three people. Is the village awake already?"),
        ("Mio","Hoshikawa's always up before me. Two days until that train. Let's go and see Renji."),
    };
    public static readonly (string Speaker,string Text)[] HalfDone=
    {
        ("Renji","Morning, Mio. Something kept me up all night. Ask Zero about the signals at Hoshikawa Station."),
        ("Mio","Zero, how are the signals at Hoshikawa Station?"),
        ("Zero","Every signal on the line has power. Platform two's departure signal at Hoshikawa Station has power too, but answers only to the lever in the station's signal box."),
        ("Renji","...Last night I said platform two would show that train red. I was wrong."),
        ("Renji","Nothing out here can turn that signal. Someone has to get inside the station and pull the lever. Then that train stops right beside platform two. And it comes the day after tomorrow."),
        ("Mio","Then I'll go."),
        ("Renji","The station road is under a rockfall, and past it the station belongs to SIRIUS. And I can't go with you."),
        ("Renji","If the relay goes down, every signal goes dark, and I'm the only one who can fix it. That plaza is also the robots' way down to the village."),
        ("Renji","The village has taken turns up there since last night. From this evening I'll hold it myself. But you're not going alone. Find two people to go with you."),
        ("Renji","And my telephone: is it working yet? I'll hook it onto the line by the relay. There are still old phone boxes along the line. Find one on the way and call me."),
        ("Mio","Almost. One more wire."),
        ("Renji","Then finish it. And ask KOMA about that rockfall: no one here can lift those boulders. Bring me the telephone when you're ready."),
    };
    public static readonly (string Speaker,string Text)[] RenjiReady=
    {
        ("Renji","Two people to go with you, KOMA to shift that rockfall, and my telephone working. Then come and see me."),
    };
    // The four errands, in any order (Game.Errands). Sota's replaces his first meeting, whose "tell me first" it would answer.
    public static readonly (string Speaker,string Text)[] SotaJoins=
    {
        ("Mio","Sota. The train comes the day after tomorrow, and someone has to get inside Hoshikawa Station to stop it."),
        ("Sota","...Then I'm coming."),
        ("Sota","Not along the tracks. They're the city's road now, with rail crawlers on them day and night. And the old road's tunnel came down in the spring rains."),
        ("Sota","I've run letters all over these hills. There's a way round the mountain. A whole day's walk, and a night out on the way."),
        ("Sota","My brother was taken at that station. Five years ago. I'm going."),
        ("Sota","The signal box is upstairs, above the main hall."),
        ("Mio","How do you know that?"),
        ("Sota","...My brother told me. A long time ago."),
    };
    public static readonly (string Speaker,string Text)[] Crossbow=
    {
        ("Chiaki","Mio, good. Ask Zero what medicine is on that train. Every box, if it can."),
        ("Mio","Zero, what medicine is on the train? Every box."),
        ("Zero","The old freight system still logs each load on the line by its category code. Cough syrup, sixty boxes. Fever medicine, forty. Antibiotics, forty. Bandages and salve, sixty. Two hundred boxes in all."),
        ("Chiaki","I can't leave the clinic. So, Yui, you learn them. Cough syrup first, for Grandpa Tetsu. Then the fever medicine, then the antibiotics."),
        ("Yui","Cough syrup, fever medicine, anti... antibiotics! Cough, fever, the long one. Got it!"),
        ("Yui","Oh! And Mio, if I'm coming with you, I need my crossbow. I fell on the night road and its trigger spring snapped. I can't do springs."),
        ("Mio","Give it here. I've got a spare in my kit."),
        ("","Mio opens the crossbow's lock, fits the spare spring and works the trigger until it clicks clean."),
        ("Yui","It clicks! Mio, you're amazing!"),
        ("Chiaki","Here, two remedies for the road. And bring everyone back to me in one piece."),
    };
    // KOMA-7 answers to its keeper, not to Mio: Grandpa Tetsu gives the order from inside the house, a voice with no portrait (Offstage).
    public static readonly (string Speaker,string Text)[] KomaAsks=
    {
        ("Mio","KOMA, the station road is under a rockfall. Can you move the boulders?"),
        ("KOMA-7","This request requires the keeper's consent."),
        ("","Mio fetches Haruka from the fields, and the three of them go round to the Mori house's engawa."),
        ("Haruka","Grandpa? KOMA needs your word. Mio's going to the station."),
        ("","From inside the house comes a long cough, then Grandpa Tetsu's voice."),
        ("Tetsuzo","...KOMA. Go and help them."),
        ("Tetsuzo","Mio, I'm counting on you for the cough medicine. KOMA, you come back safe too."),
        ("KOMA-7","Keeper's voice confirmed. This unit walks slowly. It will set out tonight."),
        ("Haruka","Forty years, and KOMA's never taken an order from anyone but Grandpa. He'll go up with Renji at dusk."),
    };
    public static readonly (string Speaker,string Text)[] PhoneFixed=
    {
        ("","Mio twists the last wire home, closes the telephone's case and gives its crank a turn. The bell answers with one small ring."),
        ("Zero","A telephone is a terminal for two people. I like it."),
        ("Mio","Renji will like it even more."),
    };
    public static readonly (string Speaker,string Text)[] ChiyoLamp=
    {
        ("Chiyo","So you are going to the station. When you all come home, I will light a lantern at the east gate for you."),
        ("Mio","Thank you, Chiyo. We'll follow that light home."),
    };
    public static readonly (string Speaker,string Text)[] Evening=
    {
        ("Mio","Renji, your telephone. It works. And Sota and Yui are coming with me."),
        ("Renji","...Good. I'd have picked the same two."),
        ("Renji","Then at first light tomorrow, meet at the rockfall east of the relay plaza. I'm going up tonight with KOMA, and I'll take over the watch there."),
        ("Renji","And you may not have to carry the medicine home. The station should still have an old track trolley, the kind line crews used to push."),
        ("Renji","The crawlers keep off the rails while a city train is on the line. Once that one's stopped, load the trolley and push it home along the rails."),
        ("Mio","Home along the line. Twelve years, and it might finally carry something for us."),
        ("Renji","Get some sleep, Mio. Tomorrow's a long walk."),
        ("","The sun goes down behind the mountains, and one by one Hoshikawa's lanterns are lit again."),
    };
    public static readonly (string Speaker,string Text)[] RenjiNight=
    {
        ("Renji","Go home and sleep, Mio. KOMA and I have the plaza tonight. First light, here at the rockfall."),
    };
    public static readonly (string Speaker,string Text)[] NightTwo=
    {
        ("Zero","Mio. Sota's brother disappeared at that station, didn't he?"),
        ("Zero","Is Sota scared?"),
        ("Mio","If he is, he won't say so."),
        ("Zero","...My night shift is starting. I have to go."),
        ("Zero","Goodnight, Mio. Sleep well. I won't count anything tonight."),
        ("Mio","Goodnight, Zero."),
    };
    public static readonly (string Speaker,string Text)[] DayThree=
    {
        ("","Day three."),
        ("","Mio rises before dawn. The terminal on her wrist is still quiet."),
    };
    // At the rockfall east of the relay plaza: Renji opens the barrier, KOMA-7 the rockfall (World.ClearRockfall, at Cleared), and the
    // three set out. Zero keeps silent with the others there.
    public static readonly (string Speaker,string Text)[] Rockfall=
    {
        ("Yui","Mio! Over here! We beat you here, for once!"),
        ("Sota","Morning. The rail crawlers went past an hour ago. If we leave now, we'll be round the mountain by dark."),
        ("Renji","Then let's open the road."),
        ("","Renji hauls the barrier aside, and KOMA-7 lifts the boulders one by one and sets them down off the road."),
        ("KOMA-7","This unit is very good at moving rocks. Also tomatoes."),
        ("","Renji clips his telephone onto the old line's junction box and cranks it. Somewhere down the cleared road, faint but clear, a phone box's bell rings with it."),
        ("Renji","The old line still carries a voice. Find a phone box when you can, and call me."),
        ("Renji","All three of you come back."),
        ("Mio","We will. Stay by that telephone, Renji."),
        ("Yui","All three of us, and the medicine too! That's a promise, not a question!"),
        ("KOMA-7","This unit will return to the tomatoes. Bring back the cough medicine."),
        ("Sota","Let's go. I'll show you the way round the mountain."),
    };
    public const int Cleared=3;
    public static readonly (string Speaker,string Text)[] RenjiSeesOff=
    {
        ("Renji","Find a phone box when you can, and call me. All three of you come back."),
    };
    // The beat each scene's night falls on, from which the HUD darkens its scenes: the homecoming's walk home, the second evening, and the
    // second night in Mio's room.
    public static readonly Dictionary<string,int> Nightfall=new(){["Homecoming"]=4,["Evening"]=7,["NightTwo"]=0};
    // Beats drawn as a title card over black rather than in the text box.
    public static readonly HashSet<string> Cards=new(){"DayTwo.0","DayThree.0"};
    // Speakers heard but never seen, who have no portrait.
    public static readonly HashSet<string> Offstage=new(){"Tetsuzo"};
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
            ("Chiyo","...That terminal on your wrist. You never let anyone look at it. May I? Gold casing, a little jade lamp."),
            ("Mio","It's just the old one from the signal box, Chiyo. Nothing special."),
            ("Chiyo","Shio wore one just like it. Your mother never went anywhere without hers."),
            ("Mio","Everyone who worked the line had one, didn't they?"),
            ("Chiyo","Perhaps. Perhaps they did."),
            ("","Chiyo looks past Mio, toward the city, and says nothing more."),
        },
        ["Haruka"]=new[]
        {
            ("Haruka","KOMA keeps our irrigation running without a network. Old machines are our neighbors. The amber-eyed patrols beyond the bridge are not."),
            ("KOMA-7","Correction: this unit is the best neighbor."),
            ("Haruka","He's been like this since Grandpa Tetsu registered him. Forty years of the same jokes."),
            ("KOMA-7","Registered keeper: Mori Tetsuzo. Keeper status: napping on the engawa. This unit answers to its keeper, not to the network."),
            ("Mio","And that's why the patrols can't order you around. Lucky you."),
            ("KOMA-7","Correct. Patrols have no keepers. Very lonely machines."),
        },
        ["KOMA-7"]=new[]
        {
            ("KOMA-7","Morning diagnostic: tomatoes healthy. Humans worried. Reminder for Mio: this unit is still far better company than your terminal."),
            ("KOMA-7","Query, relayed through Mio: does Zero know what tomatoes feel like?"),
            ("Mio","...Fine. Zero, do you know what tomatoes feel like?"),
            ("Zero","No. This terminal has never touched one."),
            ("KOMA-7","Round. Warm in the afternoon. Second query, relayed through Mio: where is Zero's body?"),
            ("Mio","It doesn't have one, KOMA. It's just a terminal. Go and look after your tomatoes."),
        },
        ["Chiaki"]=new[]
        {
            ("Chiaki","Hold your guard against a strike, or tap it just before impact to deflect. A broken posture leaves you exposed. Let me patch you up."),
            ("Chiaki","And Mio, my shelves are nearly bare. One week of medicine for the whole village. If you find any out there, bring it home."),
        },
        ["Sota"]=new[]
        {
            ("Sota","Watch for the red diamond on a patrol: that sweeping attack cannot be blocked. Dodge through it or jump away."),
            ("Sota","...Say, Mio. Ask Zero who's on the other end of it."),
            ("Mio","...Zero. Who's on the other end of you?"),
            ("Zero","This terminal answers queries about the old line."),
            ("Mio","Told you, Sota. It's old wiring from the signal box. It only knows the line."),
            ("Sota","Huh. Then if it ever knows when that train is due, tell me first. I'd want to be there."),
            ("","Sota keeps his eyes on Mio's wrist a moment longer than he needs to."),
        },
        ["Yui"]=new[]
        {
            ("Yui","Chiaki is teaching me medicines! This one's for fevers, this one's for coughs, and this one tastes awful. I tried it."),
            ("Mio","What did it taste like?"),
            ("Yui","Like the inside of an old boot! Mio, have Zero remember that one: old boot."),
            ("Mio","Zero, remember that one."),
            ("Zero","Saved. Old boot."),
        },
    };
    // Zero's relay reminders repeat on every visit, so they stay unvoiced and IntegrationCheck does not ask for their mp3s.
    public static readonly HashSet<string> Unvoiced=new(){"RelayAsleep","RelayGuardedNone","RelayGuardedOne","RelayGuardedTwo","RelayGuardedThree","RelaySteady"};
    // The speakers with a Japanese voice; everyone else's lines play silent.
    public static readonly HashSet<string> Cast=new(){"Mio","Zero","Yui","Renji"};
    // The cast's lines written or changed since the last recording, which IntegrationCheck lets go without an mp3 until they are voiced.
    public static readonly HashSet<string> Unrecorded=new(){
        "Intro.17","Intro.18","Gate.3","Gate.4","Gate.5","Briefing.7","Briefing.8","Homecoming.0","Homecoming.1","DayTwo.1","DayTwo.2","DayTwo.3",
        "DayTwo.4","DayTwo.5","DayTwo.6","DayTwo.7","HalfDone.0","HalfDone.1","HalfDone.2","HalfDone.3","HalfDone.4","HalfDone.5",
        "HalfDone.6","HalfDone.7","HalfDone.8","HalfDone.9","HalfDone.10","HalfDone.11","RenjiReady.0","SotaJoins.0","SotaJoins.6","Crossbow.1",
        "Crossbow.2","Crossbow.4","Crossbow.5","Crossbow.6","Crossbow.8","KomaAsks.0","PhoneFixed.1","PhoneFixed.2","ChiyoLamp.1","Evening.0",
        "Evening.1","Evening.2","Evening.3","Evening.4","Evening.5","Evening.6","RenjiNight.0","NightTwo.0","NightTwo.1","NightTwo.2",
        "NightTwo.3","NightTwo.4","NightTwo.5","Rockfall.0","Rockfall.2","Rockfall.6","Rockfall.7","Rockfall.8","Rockfall.9","RenjiSeesOff.0","FirstMeeting.KOMA-7.2",
        "FirstMeeting.Sota.2","FirstMeeting.Yui.2","FirstMeeting.Yui.3"};
    // What the 3D scene does when a beat starts: a camera shot and the clips its actors play ("Mio clip, Yui clip"). A beat with no entry, or
    // an empty part, keeps what was there. Shots: "over A B" looks past A's shoulder at B, "two A B" frames both from the side, "face A"
    // is in front of A, and any other name is a fixed shot (Game.Shots). Every actor goes back to its resting clip when the scene ends.
    // A scene cuts only where it starts: the owner finds frequent complete view changes sickening, so a shot holds through a stretch of
    // lines and the camera moves, never cuts, to the next (Game.CameraMove).
    public static readonly Dictionary<string,(string Shot,string Acts)> Stage=new()
    {
        ["Opening.0"]=("city",""),["Opening.1"]=("valley",""),["Opening.2"]=("home",""),
        ["Intro.0"]=("bed","Mio sleep"),["Intro.2"]=("futon","Mio wake"),["Intro.3"]=("","Mio terminal_sit"),["Intro.4"]=("floor","Mio sit"),
        ["Intro.6"]=("room","Mio rise"),["Intro.7"]=("shoji","Mio stand"),["Intro.9"]=("face Mio","Mio terminal"),["Intro.11"]=("","Mio stand"),
        ["Intro.15"]=("","Mio terminal"),["Intro.16"]=("","Mio stand"),["Intro.17"]=("","Mio terminal"),["Intro.19"]=("","Mio stand"),
        ["Photo.0"]=("photo",""),
        ["Gate.0"]=("two Mio Yui","Yui wave"),["Gate.1"]=("","Yui idle"),["Gate.2"]=("","Yui point"),["Gate.3"]=("","Yui idle"),
        ["Gate.5"]=("","Mio terminal"),["Gate.7"]=("over Mio Yui","Mio stand, Yui give"),["Gate.8"]=("","Mio terminal, Yui idle"),
        ["Gate.9"]=("","Mio stand"),["Gate.11"]=("","Yui wave"),["Gate.12"]=("","Yui idle"),["Gate.13"]=("","Yui point"),
        ["Briefing.0"]=("two Mio Renji","Renji idle"),["Briefing.1"]=("","Yui wave"),["Briefing.2"]=("","Mio terminal, Yui idle"),
        ["Briefing.3"]=("","Mio stand, Yui give"),["Briefing.4"]=("","Renji read, Yui idle"),["Briefing.5"]=("","Renji point"),
        ["Briefing.6"]=("","Renji idle"),["Briefing.8"]=("","Mio terminal"),["Briefing.10"]=("face Yui","Mio stand, Yui wave"),
        ["Briefing.11"]=("","Yui idle"),["Briefing.12"]=("","Yui fold"),["Briefing.13"]=("two Mio Renji",""),
        ["DayTwo.0"]=("bed","Mio sleep"),["DayTwo.1"]=("futon","Mio wake"),["DayTwo.3"]=("","Mio terminal_sit"),["DayTwo.6"]=("room","Mio rise"),["DayTwo.7"]=("","Mio terminal"),
        ["HalfDone.0"]=("two Mio Renji","Renji idle"),["HalfDone.1"]=("","Mio terminal"),["HalfDone.3"]=("","Mio stand"),["HalfDone.9"]=("","Renji point"),["HalfDone.10"]=("","Renji idle"),
        ["PhoneFixed.0"]=("floor",""),["Evening.0"]=("two Mio Renji","Renji idle"),["NightTwo.0"]=("face Mio","Mio terminal"),["NightTwo.5"]=("","Mio stand"),
        ["DayThree.0"]=("bed","Mio sleep"),["DayThree.1"]=("room","Mio rise"),
        ["Rockfall.0"]=("two Mio Renji","Yui wave"),["Rockfall.1"]=("","Yui idle"),["Rockfall.9"]=("","Yui wave"),["Rockfall.10"]=("","Yui idle"),
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
        ("RelayAsleep",RelayAsleep),("RelayGuardedNone",RelayGuardedNone),("RelayGuardedOne",RelayGuardedOne),
        ("RelayGuardedTwo",RelayGuardedTwo),("RelayGuardedThree",RelayGuardedThree),("RelaySteady",RelaySteady),
        ("DayTwo",DayTwo),("HalfDone",HalfDone),("RenjiReady",RenjiReady),("SotaJoins",SotaJoins),("Crossbow",Crossbow),("KomaAsks",KomaAsks),("PhoneFixed",PhoneFixed),
        ("ChiyoLamp",ChiyoLamp),("Evening",Evening),("RenjiNight",RenjiNight),("NightTwo",NightTwo),("DayThree",DayThree),("Rockfall",Rockfall),("RenjiSeesOff",RenjiSeesOff)}
        .Concat(FirstMeeting.Select(m=>("FirstMeeting."+m.Key,m.Value))).Concat(Komorebi.Select(m=>("Komorebi."+m.Key,m.Value)))
        .Concat(openers.Select(m=>("FirstMeeting."+m.Key,m.Value))).ToDictionary(s=>s.Item2,s=>s.Item1);
    // A scene by its name, for tables such as Game.Errands that name them.
    public static (string Speaker,string Text)[] Named(string name)=>SceneNames.First(s=>s.Value==name).Key;
}
