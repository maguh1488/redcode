using CommonBehaviors.Actions;

using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Action = Styx.TreeSharp.Action;

namespace Honorbuddy.Quest_Behaviors.SpecificQuests.PlantingtheSeedofFear
{
	[CustomBehaviorFileName(@"SpecificQuests\24999-TirisfalGlades-PlantingtheSeedofFear")]
	public class PlantingtheSeedofFear : CustomForcedBehavior
	{
		public PlantingtheSeedofFear(Dictionary<string, string> args)
			: base(args)
		{
			try
			{
				QuestId = 24999;
			}
			catch
			{
				Logging.Write("Problem parsing a QuestId in behavior: PlantingtheSeedofFear");
			}
		}
		private Composite _behaviorTreeHook_LootMain;
		public int QuestId { get; set; }
		private bool _isBehaviorDone;
		public int MurlocMobId = 38937;
		public int count;
		public string targ = "target";
		private Composite _root;
		public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
		public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;

		public override bool IsDone
		{
			get
			{
				return _isBehaviorDone;
			}
		}
		
		private LocalPlayer Me
		{
			get { return (StyxWoW.Me); }
		}
		
		public override void OnStart()
		{
			OnStart_HandleAttributeProblem();
			if (!IsDone)
			{
				_behaviorTreeHook_LootMain = CreateBehavior_LootMain();
				TreeHooks.Instance.InsertHook("Loot_Main", 0, _behaviorTreeHook_LootMain);
			}
		}
		
		protected virtual Composite CreateBehavior_LootMain()
		{
			return new Decorator(context => !IsDone,
				new PrioritySelector(
					// Disable the LootRoutine
					DoneYet, Scare, new ActionAlwaysSucceed()
			));
		}

		public List<WoWUnit> Murloc
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>().Where(
					u => u.Entry == MurlocMobId && !u.IsDead && u.Distance < 500)
					.OrderBy(u => u.Distance).ToList();
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
						TreeRoot.StatusText = "Finished!";
						_isBehaviorDone = true;
						return RunStatus.Success;
					}));
			}
		}
		
		public Composite Scare
		{
			get
			{
				return
					new Decorator(c => Murloc != null, 
						new Sequence(
							new Action(c =>	Murloc[0].Target()),
							new Action(c => count = Lua.GetReturnVal<int>("local for i=1,40 do local _,_,_,count,_,_,_,_,_,_,spellId=UnitAura("+targ+",i); if spellId==73133 then return count end end", 0)),
							new DecoratorContinue(c => count < 3,
								new Sequence(
									new DecoratorContinue(c => Murloc[0].Location.Distance(Me.Location) > 5,
										new Action(c =>	Navigator.MoveTo(Murloc[0].Location))),
									new DecoratorContinue(c => Murloc[0].Location.Distance(Me.Location) < 5,
										new Sequence(
											new Action(c =>	WoWMovement.MoveStop()),
											new WaitContinue(TimeSpan.FromSeconds(1),
												context => Murloc[0].Location.Distance(Me.Location) > 5,
												new ActionAlwaysSucceed())
					))))));
			}
		}
		
		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone, new PrioritySelector(new ActionAlwaysSucceed())));
		}
	}
}