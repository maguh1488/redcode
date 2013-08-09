using System;
using System.Collections.Generic;
using System.Linq;

using CommonBehaviors.Actions;
using Honorbuddy.QuestBehaviorCore;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using Action = Styx.TreeSharp.Action;


namespace Honorbuddy.Quest_Behaviors.SpecificQuests.DefenseOfDarkshore
{
	[CustomBehaviorFileName(@"13885-DefenseOfDarkshore")]
	public class DefenseOfDarkshore : CustomForcedBehavior
	{
		public DefenseOfDarkshore(Dictionary<string, string> args)
			: base(args)
		{
			try
			{
				QuestId = 13885;//GetAttributeAsQuestId("QuestId", true, null) ?? 0;
			}
			catch
			{
				Logging.Write("Problem parsing a QuestId in behavior: The Defense of Darkshore");
			}
		}
		public int QuestId { get; set; }
		private bool _isBehaviorDone;

		public uint[] Mobs = new uint[] { 34318, 34396 }; // Deer
		public uint[] Mobs2 = new uint[] { 2237, 2071 }; // Cats
		public uint[] Mobs3 = new uint[] { 2165, 34417 }; // Bears

		private Composite _root;
		private readonly LocalBlacklist _weaponBlacklist = new LocalBlacklist();
		public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
		public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
		static public bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') or not(GetBonusBarOffset()==0) then return 1 else return 0 end", 0) == 1; } }
		
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
				if (TreeRoot.Current != null && TreeRoot.Current.Root != null && TreeRoot.Current.Root.LastStatus != RunStatus.Running)
				{
					var currentRoot = TreeRoot.Current.Root;
					if (currentRoot is GroupComposite)
					{
						var root = (GroupComposite)currentRoot;
						root.InsertChild(0, CreateBehavior());
					}
				}
				PlayerQuest Quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
				TreeRoot.GoalText = ((Quest != null) ? ("\"" + Quest.Name + "\"") : "In Progress");
			}
		}

		public WoWUnit Deer
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(
					u => Mobs.Contains(u.Entry) && !u.IsDead && u.Distance < 100 && !_weaponBlacklist.Contains(u)).OrderBy(u => u.Distance).
					FirstOrDefault();
			}
		}

		public WoWUnit Cats
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(
					u => Mobs2.Contains(u.Entry) && !u.IsDead && u.Distance < 100 && !_weaponBlacklist.Contains(u)).OrderBy(u => u.Distance).
					FirstOrDefault();
			}
		}

		public WoWUnit Bear
		{
			get
			{
				return ObjectManager.GetObjectsOfType<WoWUnit>(true).Where(
					u => Mobs3.Contains(u.Entry) && !u.IsDead && u.Distance < 100 && !_weaponBlacklist.Contains(u)).OrderBy(u => u.Distance).
					FirstOrDefault();
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

		public Composite KillDeer
		{
			get
			{
				return new Decorator(r => !IsObjectiveComplete(3, (uint)QuestId) && Deer != null, new Action(r =>
				{
					Lua.DoString("CastPetAction(1)");
					SpellManager.ClickRemoteLocation(Deer.Location);
					_weaponBlacklist.Add(Deer.Guid, TimeSpan.FromSeconds(90));
				}));
			}
		}

		public Composite KillCats
		{
			get
			{
				return new Decorator(r => !IsObjectiveComplete(2, (uint)QuestId) && Cats != null, new Action(r =>
				{
					Lua.DoString("CastPetAction(1)");
					SpellManager.ClickRemoteLocation(Cats.Location);
					_weaponBlacklist.Add(Cats.Guid, TimeSpan.FromSeconds(90));
				}));
			}
		}
		
		public Composite KillBear
		{
			get
			{
				return new Decorator(r => !IsObjectiveComplete(1, (uint)QuestId) && Bear != null, new Action(r =>
				{
					Lua.DoString("CastPetAction(1)");
					SpellManager.ClickRemoteLocation(Bear.Location);
					_weaponBlacklist.Add(Bear.Guid, TimeSpan.FromSeconds(90));
				}));
			}
		}

		protected override Composite CreateBehavior()
		{
			return _root ?? (_root = new Decorator(ret => !_isBehaviorDone,
				new PrioritySelector(
					new Decorator(context => !InVehicle,
						new Action(context => { _isBehaviorDone = true; })),
					DoneYet, 
					KillDeer, KillCats, KillBear, 
					new ActionAlwaysSucceed())));
		}
	}
}
