using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;

namespace RecallTracker
{
	class Program
	{
		private static readonly List<Recall> _recalls = new List<Recall>();
		
		static void Main(string[] args)
		{
			CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
		}
		
		static void Game_OnGameLoad(EventArgs args)
		{
			foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValid && hero.IsEnemy))
			{
				_recalls.Add(new Recall(hero));
			}
			Obj_AI_Base.OnTeleport += Obj_AI_Base_OnTeleport;
			Drawing.OnDraw += Drawing_OnDraw;
			Game.PrintChat("Recall Tracker加载成功 花边汉化!");
		}
		
		static void Drawing_OnDraw(EventArgs args)
		{
			int count = 0;
			foreach (Recall recall in _recalls)
			{
				if (recall.LastStatus != Packet.S2C.Teleport.Status.Unknown)
				{
					var text = recall.ToString();
					if (recall.Update() && !string.IsNullOrWhiteSpace(text))
					{
						Drawing.DrawText(Drawing.Width - 655, Drawing.Height - 200 + 15*count++, recall.ToColor(),
						                 text);
					}
				}
			}
		}

		static void Obj_AI_Base_OnTeleport(GameObject sender, GameObjectTeleportEventArgs args)
		{
			Packet.S2C.Teleport.Struct decoded = Packet.S2C.Teleport.Decoded(sender, args);
			var recall = _recalls.FirstOrDefault(r => r.Hero.NetworkId == decoded.UnitNetworkId);
			if (!Equals(recall, default(Recall)))
			{
				recall.Duration = decoded.Duration;
				recall.LastStatus = decoded.Status;
				recall.LastStatusType = decoded.Type;
			}
		}
		
		private class Recall
		{
			#region Fields

			public readonly Obj_AI_Hero Hero;
			private float _duration;
			private float _lastActionTime;
			private Packet.S2C.Teleport.Status _lastStatus;
			private Packet.S2C.Teleport.Type _lastStatusType;
			private float _recallStart;

			#endregion

			#region Constructors

			public Recall(Obj_AI_Hero hero)
			{
				Hero = hero;
				LastStatus = Packet.S2C.Teleport.Status.Unknown;
				LastStatusType = Packet.S2C.Teleport.Type.Unknown;
			}

			#endregion

			#region Properties
			public Packet.S2C.Teleport.Type LastStatusType
			{
				get {return _lastStatusType;}
				set {_lastStatusType = value;}
			}

			public float Duration
			{
				private get { return _duration; }
				set { _duration = value/1000; }
			}

			public Packet.S2C.Teleport.Status LastStatus
			{
				get { return _lastStatus; }
				set
				{
					_lastStatus = value;
					_recallStart = _lastStatus == Packet.S2C.Teleport.Status.Start
						? Game.Time
						: 0f;
					_lastActionTime = Game.Time;
				}
			}

			#endregion

			#region Methods

			public override string ToString()
			{
				var time = _recallStart + Duration - Game.Time;
				if (time <= 0)
				{
					time = Game.Time - _lastActionTime;
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Recall)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("Recall: {0}({1}%) Recalling ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("Recall: {0}({1}%) Recalled ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("Recall: {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Teleport)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("Teleport: {0}({1}%) Teleporting ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("Teleport: {0}({1}%) Ported ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("Teleport: {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				if (LastStatusType == Packet.S2C.Teleport.Type.Shen || LastStatusType == Packet.S2C.Teleport.Type.TwistedFate)
				{
					if (LastStatus == Packet.S2C.Teleport.Status.Start)
						return string.Format("(R): {0}({1}%) Transporting ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Finish)
						return string.Format("(R): {0}({1}%) Ported ({2:0.00})", Hero.ChampionName,
						                     (int) Hero.HealthPercentage(), time);
					if (LastStatus == Packet.S2C.Teleport.Status.Abort)
						return string.Format("(R): {0}({1}%) Aborted", Hero.ChampionName, (int) Hero.HealthPercentage());
				}
				return string.Empty;
			}

			public Color ToColor()
			{
				if (LastStatus == Packet.S2C.Teleport.Status.Start)
					return Color.Beige;
				if (LastStatus == Packet.S2C.Teleport.Status.Finish)
					return Color.GreenYellow;
				if (LastStatus == Packet.S2C.Teleport.Status.Abort)
					return Color.Red;
				return Color.Black;
			}

			public bool Update()
			{
				var additional = LastStatus == Packet.S2C.Teleport.Status.Start
					? Duration + 20f
					: 20f;
				if (_lastActionTime + additional <= Game.Time)
				{
					_lastActionTime = 0f;
					return false;
				}
				return true;
			}

			#endregion
		}
	}
}
