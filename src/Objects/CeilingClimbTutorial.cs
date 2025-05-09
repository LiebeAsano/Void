using RWCustom;
using VoidTemplate.Useful;
namespace VoidTemplate.Objects;

internal class CeilingClimbTutorial : UpdatableAndDeletable
{

	public CeilingClimbTutorial(Room room, params Message[] messages)
	{
		messageList = messages;
		this.room = room;
	}

	public override void Update(bool eu)
	{
		if ((room.game.session.Players[0].realizedCreature == null ||
			room.game.cameras[0].hud == null ||
			room.game.cameras[0].hud.textPrompt == null)
			&& room.game.IsVoidStoryCampaign())
		{
			return;
		}
		foreach (Message message in messageList)
		{
			string[] array = message.text.Split('/');
			string text = "";
			foreach (string s in array)
				text += Custom.rainWorld.inGameTranslator.Translate(s);

			room.game.cameras[0].hud.textPrompt.AddMessage(text, message.wait, message.time, true, ModManager.MMF);
		}
		room.game.GetStorySession.saveState.SetMessageShown(true);
		slatedForDeletetion = true;
	}

	public class Message
	{
		public readonly string text;
		public readonly int wait;
		public readonly int time;

		public Message(string text, int waitTime, int holdTime)
		{
			this.text = text;
			wait = waitTime;
			time = holdTime;
		}

		public Message(string text) : this(text, 0, 160)
		{
		}
	}

	private Message[] messageList;
}