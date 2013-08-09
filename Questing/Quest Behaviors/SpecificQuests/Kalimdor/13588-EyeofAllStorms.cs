using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Honorbuddy.Quest_Behaviors.SpecificQuests.EyeofAllStorms
{
	[CustomBehaviorFileName(@"13588-EyeofAllStorms")]
	public class EyeofAllStorms : CustomForcedBehavior
	{
		public EyeofAllStorms(Dictionary<string, string> args)
			: base(args)
		{
			try
			{
				QuestId = 13588;//GetAttributeAsQuestId("QuestId", true, null) ?? 0;
			}
			catch
			{
				Logging.Write("Problem parsing a QuestId in behavior: Enemy at the Gates");
			}
		}
		private Composite _behaviorTreeHook_CombatMain;
		public int QuestId { get; set; }
		private bool _isBehaviorDone;
		public int RiderId = 34282;
		private Composite _root;
		public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
		public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		
		public override bool IsDone
		{ get {	return _isBehaviorDone;	} }
		
		private LocalPlayer Me
		{
			get { return (StyxWoW.Me); }
		}

		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
			if (!IsDone)
			{
				_behaviorTreeHook_CombatMain = CreateBehavior_CombatMain();
				TreeHooks.Instance.InsertHook("Combat_Main", 0, _behaviorTreeHook_CombatMain);
			}
		}
		
		protected virtual Composite CreateBehavior_CombatMain()
		{
			return new Decorator(context => !IsDone,
				new PrioritySelector(
					// Disable the CombatRoutine
					DoneYet, KillOne, KillTwo, new ActionAlwaysSucceed()
			));
		}


		public List<WoWUnit> Portal
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == 34316 && !u.IsDead && u.Distance < 1000).OrderBy(u => u.Distance).ToList();
			}
		}
		
		public List<WoWUnit> Riders
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == RiderId && !u.IsDead && u.Distance < 500).OrderBy(u => u.Distance).ToList();
			}
		}
		
		public bool IsQuestComplete()
		{
			var quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
			return quest == null || quest.IsCompleted;
		}
		
		private bool IsObjectiveComplete(int objectiveId, uint questId)
		{
			if (Me.QuestLog.GetQuestById(questId) == null)
			{
				return false;
			}
			int returnVal = Lua.GetReturnVal<int>("return GetQuestLogIndexByID(" + questId + ")", 0);
			return
				Lua.GetReturnVal<bool>(
					string.Concat(new object[] { "return GetQuestLogLeaderBoard(", objectiveId, ",", returnVal, ")" }), 2);
		}

		public Composite DoneYet
		{
			get
			{
				return
					new Decorator(ret => IsQuestComplete(), new Action(delegate
					{
						Lua.DoString("CastPetAction(6)");
						TreeRoot.StatusText = "Finished!";
						_isBehaviorDone = true;
						return RunStatus.Success;
					}));

			}
		}
		
		public Composite KillOne
		{
			get
			{
				return new Decorator(r => !IsObjectiveComplete(1, (uint)QuestId), new Action(r =>
				{
					var hostile =
						ObjectManager.GetObjectsOfType<WoWUnit>().Where(c => 
								c.Entry != 34401 && c.GotTarget && c.CurrentTarget == Me.CharmedUnit).OrderBy(c=>
								c.Distance).FirstOrDefault();
					WoWUnit tar;
					if (hostile != null)
					{
						tar = hostile;
						tar.Target();
						Lua.DoString("CastPetAction(1)");
					}
					else if (Portal.Count > 0)
					{
						tar = Portal.FirstOrDefault();
						tar.Target();
						Lua.DoString("CastPetAction(1)");
					}
				}));
			}
		}
		
		public Composite KillTwo
		{
			get
			{
				return new Decorator(r => !IsObjectiveComplete(2, (uint)QuestId), new Action(r =>
				{
					var hostile =
						ObjectManager.GetObjectsOfType<WoWUnit>().Where(c => 
								c.Entry != 34401 && c.GotTarget && c.CurrentTarget == Me.CharmedUnit).OrderBy(c=>
								c.Distance).FirstOrDefault();
					WoWUnit tar;
					if (hostile != null)
					{
						tar = hostile;
						tar.Target();
						Lua.DoString("CastPetAction(1)");
					}
					else if (Riders.Count > 0)
					{
						tar = Riders.FirstOrDefault();
						tar.Target();
						Lua.DoString("CastPetAction(1)");
					}
				}));
			}
		}
		
		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(new ActionAlwaysSucceed())));
		}
	}
}
