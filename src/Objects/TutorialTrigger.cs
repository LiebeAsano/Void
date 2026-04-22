using RWCustom;
using System.Linq;

namespace VoidTemplate.Objects;

public class TutorialTrigger : UpdatableAndDeletable
{

	public TutorialTrigger(Room room, params Message[] messages)
		: this(room, new(0, 0, room.Width, room.Height), messages)
	{
	}
	public TutorialTrigger(Room room, IntRect triggerRect, params Message[] messages)
	{
		messageList = messages;
		rect = triggerRect;
		this.room = room;
	}

	public override void Update(bool eu)
	{
		if (room.game.session.Players[0].realizedCreature == null ||
			room.game.cameras[0].hud == null ||
			room.game.cameras[0].hud.textPrompt == null)
		{
			return;
		}

		if (room.PlayersInRoom.Any(i =>
				i.abstractCreature.pos.x >= rect.left &&
				i.abstractCreature.pos.x <= rect.right &&
				i.abstractCreature.pos.y >= rect.bottom &&
				i.abstractCreature.pos.y <= rect.top))
		{
			for (int index = 0; index < messageList.Length; index++)
			{
				string[] array = messageList[index].text.Split('/');
				string text = "";
				string[] array2 = array;
				foreach (string s in array2)
					text += Custom.rainWorld.inGameTranslator.Translate(s);


				room.game.cameras[0].hud.textPrompt.AddMessage(text, messageList[index].wait,
					messageList[index].time,
					true, ModManager.MMF);
			}

			slatedForDeletetion = true;
		}
	}

	public class Message(string text, int waitTime, int holdTime)
    {
		public readonly string text = text;
		public readonly int wait = waitTime;
		public readonly int time = holdTime;

        public Message(string text) : this(text, 0, 160)
		{

		}

	}

	private readonly Message[] messageList;
	private IntRect rect;
}
