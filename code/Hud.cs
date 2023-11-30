using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.UI;

namespace SpaceTest;

public partial class GameHud : HudEntity<RootPanel>
{
	public GameHud()
	{
		if ( !Game.IsClient )
			return;

		RootPanel.AddChild<ChatBox>();
		RootPanel.AddChild<VoiceList>();
		RootPanel.AddChild<VoiceSpeaker>();
		RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
	}
}
