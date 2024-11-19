namespace VoidTemplate.Objects;

class ClimbTutorial : TutorialTrigger
{
	public ClimbTutorial(Room room) : base(room, new RWCustom.IntRect(215, 0, room.Width, room.Height),
		new("Your body is unstable, and each of your deaths brings you closer to getting out of the cycle.", 0, 444),
        new("But you have enough strength to get out of here.", 0, 444),
		new("Hold down the 'Direction' and 'Up' buttons to climb the wall.", 0, 444)
		)
	{
	}
}
