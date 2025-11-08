//This has been hacked together from several skills.
//DESIRED BEHAVIOR: Juke/jump-like skill. If target cell is occupied by a living creature, push it away and knock it prone/attack it?
//FUNCTIONAL BEHAVIOR: Just jump except it can only do knight moves.

// Decompiled with JetBrains decompiler
// Type: XRL.World.Parts.Skill.Skirmishing_Skirmish
// Assembly: Assembly-CSharp, Version=2.0.210.24, Culture=neutral, PublicKeyToken=null
// MVID: 50395A8C-9858-45DD-B63E-142AA55CEA37
// Assembly location: D:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.dll
// XML documentation location: D:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed\Assembly-CSharp.xml

using System;
using System.Collections.Generic;
using UnityEngine;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts.Skill
{
  [Serializable]
  public class Skirmishing_Skirmish : BaseSkill
  {
    public const int DEFAULT_MAX_DISTANCE = 2;
    public static readonly string COMMAND_NAME = "CommandSkirmishingSkirmish";
    public int MaxDistance = 2;
    public Guid ActivatedAbilityID = Guid.Empty;
    public string ActiveAbilityName;
    public string ActiveAbilityDescription;
    public int ActiveRange;

    public override bool WantEvent(int ID, int cascade) => base.WantEvent(ID, cascade) || ID == AfterAddSkillEvent.ID || ID == AIGetMovementAbilityListEvent.ID || ID == AIGetOffensiveAbilityListEvent.ID || ID == AIGetRetreatAbilityListEvent.ID || ID == PooledEvent<CommandEvent>.ID || ID == PooledEvent<GetItemElementsEvent>.ID || ID == GetMovementCapabilitiesEvent.ID || ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID;

    public void CollectStats(Templates.StatCollector stats)
    {
      int MinimumRange;
      bool CanJumpOverCreatures;
      GetJumpingBehaviorEvent.Retrieve(this.ParentObject, out int _, out MinimumRange, out string _, out string _, out string _, out CanJumpOverCreatures, stats);
      stats.Set("CanJumpOverCreatures", CanJumpOverCreatures ? "true" : "false");
      stats.Set("MinimumRange", MinimumRange, MinimumRange != 2, MinimumRange - 2);
      int num = stats.CollectBonusModifiers("Range", 2, "Range");
      stats.Set("IsRangeIncreased", num > 2 ? "true" : "false");
      stats.Set("Range", num, num != 2, num - 2);
      stats.CollectCooldownTurns(this.MyActivatedAbility(this.ActivatedAbilityID), this.GetBaseCooldown());
    }

    public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
    {
      this.DescribeMyActivatedAbility(this.ActivatedAbilityID, new Action<Templates.StatCollector>(this.CollectStats));
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(GetMovementCapabilitiesEvent E)
    {
      E.Add("Jump to Square", Skirmishing_Skirmish.COMMAND_NAME, 7000, this.MyActivatedAbility(this.ActivatedAbilityID));
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(AIGetMovementAbilityListEvent E)
    {
      if (XRL.World.GameObject.Validate(E.Actor) && E.TargetCell != null && this.IsMyActivatedAbilityAIUsable(this.ActivatedAbilityID) /*&& E.Actor.HasBodyPart("Feet")*/)
      {
        int RangeModifier;
        string Verb;
        int minimumRange;
        bool canJumpOverCreatures;
        GetJumpingBehaviorEvent.Retrieve(E.Actor, out RangeModifier, out minimumRange, out string _, out Verb, out string _, out canJumpOverCreatures);
        int range = this.GetBaseRange() + RangeModifier;
        //I don't know what Standoffdistance is yet. I'd just comment this whole block out entirely but I worry that's not a good idea.
		if (E.StandoffDistance > 0)
        {
          Cell TargetCellOverride = (Cell) null;
          int num1 = 1000;
          int num2 = 1000;
          foreach (Cell adjacentCellsAtRadiu in E.TargetCell.GetLocalAdjacentCellsAtRadius(E.StandoffDistance))
          {
            if (this.ValidJump(adjacentCellsAtRadiu, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, adjacentCellsAtRadiu, canJumpOverCreatures, Silent: true, Verb: Verb))
            {
              int navigationWeightFor = adjacentCellsAtRadiu.GetNavigationWeightFor(E.Actor);
              if (navigationWeightFor < 10)
              {
                int num3 = E.Actor.DistanceTo(adjacentCellsAtRadiu);
                if (num3 + navigationWeightFor < num1 + num2)
                {
                  TargetCellOverride = adjacentCellsAtRadiu;
                  num1 = num3;
                  num2 = navigationWeightFor;
                }
              }
            }
          }
          if (TargetCellOverride != null /*&& Stat.Random(1, 20) < num1*/)
            E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: TargetCellOverride);
        }
        else if (E.Distance < minimumRange || !Skirmishing_Skirmish.CheckPath(E.Actor, E.TargetCell, canJumpOverCreatures, Silent: true) || E.TargetCell.GetNavigationWeightFor(E.Actor) >= 10)
        {
          Cell localAdjacentCell = E.TargetCell.GetRandomLocalAdjacentCell((Predicate<Cell>) (c => this.ValidJump(c, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, c, canJumpOverCreatures, Silent: true) && c.GetNavigationWeightFor(E.Actor) < 10));
          if (localAdjacentCell != null /*&& Stat.Random(1, 20) < E.Actor.DistanceTo(localAdjacentCell)*/)
            E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: localAdjacentCell);
        }
        else if (this.ValidJump(E.Distance, E.Actor, new int?(range), new int?(minimumRange)) /*&& Stat.Random(1, 20) < E.Actor.DistanceTo(E.TargetCell)*/)
          E.Add(Skirmishing_Skirmish.COMMAND_NAME);
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(AIGetRetreatAbilityListEvent E)
    {
      if (XRL.World.GameObject.Validate(E.Actor) /*&& E.Actor.HasBodyPart("Feet") */&& this.IsMyActivatedAbilityAIUsable(this.ActivatedAbilityID))
      {
        int RangeModifier;
        int minimumRange;
        bool canJumpOverCreatures;
        GetJumpingBehaviorEvent.Retrieve(E.Actor, out RangeModifier, out minimumRange, out string _, out string _, out string _, out canJumpOverCreatures);
        int range = this.GetBaseRange() + RangeModifier;
        if (E.TargetCell == null)
        {
          Cell cell = E.AvoidCell ?? E.Target?.CurrentCell;
          if (cell != null)
          {
            Cell TargetCellOverride = (Cell) null;
            int num1 = 0;
            int num2 = 0;
            foreach (Cell localAdjacentCell in E.Actor.CurrentCell.GetLocalAdjacentCells(range))
            {
              if (this.ValidJump(localAdjacentCell, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, localAdjacentCell, canJumpOverCreatures, Silent: true))
              {
                int navigationWeightFor = localAdjacentCell.GetNavigationWeightFor(E.Actor);
                if (navigationWeightFor < 10)
                {
                  int num3 = cell.DistanceTo(localAdjacentCell);
                  if (num3 - navigationWeightFor > num1 - num2)
                  {
                    TargetCellOverride = localAdjacentCell;
                    num1 = num3;
                    num2 = navigationWeightFor;
                  }
                }
              }
            }
            if (TargetCellOverride != null)
              E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: TargetCellOverride);
          }
          else
          {
            Zone currentZone = E.Actor.CurrentZone;
            Cell randomElement = currentZone != null ? currentZone.GetEmptyReachableCells().GetRandomElement<Cell>((System.Random) null) : (Cell) null;
            if (randomElement != null && this.ValidJump(randomElement, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, randomElement, canJumpOverCreatures, Silent: true) && randomElement.GetNavigationWeightFor(E.Actor) < 10)
              E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: randomElement);
          }
        }
        else if (E.StandoffDistance > 0)
        {
          Cell adjacentCellAtRadius = E.TargetCell.GetRandomLocalAdjacentCellAtRadius(E.StandoffDistance, (Predicate<Cell>) (c => this.ValidJump(c, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, c, canJumpOverCreatures, Silent: true) && c.GetNavigationWeightFor(E.Actor) < 10));
          if (adjacentCellAtRadius != null)
            E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: adjacentCellAtRadius);
        }
        else if (E.Distance < minimumRange || !Skirmishing_Skirmish.CheckPath(E.Actor, E.TargetCell, canJumpOverCreatures, Silent: true) || E.TargetCell.GetNavigationWeightFor(E.Actor) > 10)
        {
          Cell localAdjacentCell = E.TargetCell.GetRandomLocalAdjacentCell((Predicate<Cell>) (c => this.ValidJump(c, E.Actor, new int?(range), new int?(minimumRange)) && Skirmishing_Skirmish.CheckPath(E.Actor, c, canJumpOverCreatures, Silent: true) && c.GetNavigationWeightFor(E.Actor) < 10));
          if (localAdjacentCell != null)
            E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: localAdjacentCell);
        }
        else if (this.ValidJump(E.Distance, E.Actor, new int?(range), new int?(minimumRange)))
          E.Add(Skirmishing_Skirmish.COMMAND_NAME);
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(AIGetOffensiveAbilityListEvent E)
    {
      if (this.IsMyActivatedAbilityAIUsable(this.ActivatedAbilityID) /*&& E.Actor.HasBodyPart("Feet")*/)
      {
        int RangeModifier;
        int MinimumRange;
        GetJumpingBehaviorEvent.Retrieve(E.Actor, out RangeModifier, out MinimumRange, out string _, out string _, out string _, out bool _);
        if (E.Distance > MinimumRange /*+ 1*/)
        {
          int MaximumRange = this.GetBaseRange() + RangeModifier;
          if (E.Distance <= MaximumRange + 40)
          {
            Cell toApproachTarget = Skirmishing_Skirmish.FindCellToApproachTarget(E.Actor, E.Target, MinimumRange, MaximumRange);
            if (toApproachTarget != null)
              E.Add(Skirmishing_Skirmish.COMMAND_NAME, TargetCellOverride: toApproachTarget);
          }
        }
      }
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(CommandEvent E)
    {
      if (E.Command == Skirmishing_Skirmish.COMMAND_NAME && Skirmishing_Skirmish.Jump(this.ParentObject, TargetCell: E.TargetCell))
      {
        this.ParentObject.UseEnergy(1000, "Movement Jump");
        this.CooldownMyActivatedAbility(this.ActivatedAbilityID, this.GetBaseCooldown());
      }
      return base.HandleEvent(E);
    }
	//This is not jump.
    /*public override bool BeforeAddSkill(BeforeAddSkillEvent E)
    {
      if (E.Actor.IsPlayer() && E.Source != null && E.Source.GetClass() == "frog")
        Achievement.LEARN_JUMP.Unlock();
      return base.BeforeAddSkill(E);
    }*/

    public override bool HandleEvent(AfterAddSkillEvent E)
    {
      if (E.Skill == this)
        this.SyncAbility();
      return base.HandleEvent(E);
    }

    public override bool HandleEvent(GetItemElementsEvent E)
    {
      if (E.IsRelevantCreature(this.ParentObject))
        E.Add("travel", 1);
      return base.HandleEvent(E);
    }

    public override bool RemoveSkill(XRL.World.GameObject GO)
    {
      this.RemoveMyActivatedAbility(ref this.ActivatedAbilityID);
      return base.RemoveSkill(GO);
    }

    public static bool CheckPath(
      XRL.World.GameObject Actor,
      Cell TargetCell,
      out XRL.World.GameObject Over,
      out List<Point> Path,
      bool Silent = false,
      bool CanJumpOverCreatures = false,
      bool CanLandOnCreature = false,
      string Verb = "skirmish")
    {
      Over = (XRL.World.GameObject) null;
      Path = (List<Point>) null;
      Cell currentCell = Actor.CurrentCell;
      if (currentCell == null)
        return false;
      Zone parentZone = currentCell.ParentZone;
      Path = Zone.Line(currentCell.X, currentCell.Y, TargetCell.X, TargetCell.Y);
      int num = 0;
      foreach (Point point in Path)
      {
        Cell cell = parentZone.GetCell(point.X, point.Y);
        if (cell != currentCell)
        {
          int Index = 0;
          for (int count = cell.Objects.Count; Index < count; ++Index)
          {
            XRL.World.GameObject gameObject = cell.Objects[Index];
            if (num == Path.Count - 1)
            {
              if (gameObject.ConsiderSolidFor(Actor) || !CanLandOnCreature && gameObject.IsCombatObject(true))
              {
                if (!Silent)
                  Actor.Fail("You can only " + Verb + " into empty spaces.");
                return false;
              }
            }
            else if ((gameObject.ConsiderSolidFor(Actor) && !gameObject.HasPropertyOrTag("Flyover") || !CanJumpOverCreatures && gameObject.IsCombatObject(true)) && gameObject.PhaseAndFlightMatches(Actor))
            {
              if (!Silent)
                Actor.Fail("You can't " + Verb + " over " + gameObject.t(Single: true) + ".");
              return false;
            }
            if (gameObject.IsReal && (Over == null || Over.IsScenery && !gameObject.IsScenery || CanJumpOverCreatures && !Over.IsCreature && gameObject.IsCreature))
              Over = gameObject;
          }
        }
        ++num;
      }
      return true;
    }

    public static bool CheckPath(
      XRL.World.GameObject Actor,
      Cell TargetCell,
      out XRL.World.GameObject Over,
      bool Silent = false,
      bool CanJumpOverCreatures = false,
      bool CanLandOnCreature = false,
      string Verb = "skirmish")
    {
      return Skirmishing_Skirmish.CheckPath(Actor, TargetCell, out Over, out List<Point> _, Silent, CanJumpOverCreatures, CanLandOnCreature, Verb);
    }

    public static bool CheckPath(
      XRL.World.GameObject Actor,
      Cell TargetCell,
      out List<Point> Path,
      bool Silent = false,
      bool CanJumpOverCreatures = false,
      bool CanLandOnCreature = false,
      string Verb = "skirmish")
    {
      return Skirmishing_Skirmish.CheckPath(Actor, TargetCell, out XRL.World.GameObject _, out Path, Silent, CanJumpOverCreatures, CanLandOnCreature, Verb);
    }

    public static bool CheckPath(
      XRL.World.GameObject Actor,
      Cell TargetCell,
      bool CanJumpOverCreatures = false,
      bool CanLandOnCreature = false,
      bool Silent = false,
      string Verb = "skirmish")
    {
      return Skirmishing_Skirmish.CheckPath(Actor, TargetCell, out XRL.World.GameObject _, out List<Point> _, Silent, CanJumpOverCreatures, CanLandOnCreature, Verb);
    }
	//This function used to do something different. It now finds which cell you can jump to will help you approach your target the most.
    public static Cell FindCellToApproachTarget(
      XRL.World.GameObject Actor,
      XRL.World.GameObject Target,
      int MinimumRange,
      int MaximumRange,
      bool CanJumpOverCreatures = false,
      bool CanLandOnCreature = false,
      string Verb = "skirmish")
    {
      if (Target == null || Target.IsInvalid())
        return (Cell) null;
      Cell toApproachTarget = (Cell) null;
      int num1 = 1000;
      int num2 = 1000;
      foreach (Cell localAdjacentCell in Actor.CurrentCell.GetLocalAdjacentCells(MaximumRange,false))
      {
        int navigationWeightFor = localAdjacentCell.GetNavigationWeightFor(Actor);
        if (navigationWeightFor <= 20)
        {
          int num3 = localAdjacentCell.DistanceTo(Target);
          if (num3 >= MinimumRange && num3 <= MaximumRange && num3 + navigationWeightFor < num1 + num2 && localAdjacentCell.IsEmptyOfSolidFor(Actor) /*&& isValidKnightMove(Actor.CurrentCell,Target.CurrentCell)*/ && !localAdjacentCell.HasCombatObject(true) && Skirmishing_Skirmish.CheckPath(Actor, localAdjacentCell, CanJumpOverCreatures, CanLandOnCreature, true, Verb))
          {
            toApproachTarget = localAdjacentCell;
            num1 = num3;
            num2 = navigationWeightFor;
          }
        }
      }
      return toApproachTarget;
    }

    public static int GetBaseRange(XRL.World.GameObject Actor)
    {
      Skirmishing_Skirmish part = Actor.GetPart<Skirmishing_Skirmish>();
      return part != null ? part.GetBaseRange() : 2 + Actor.GetIntProperty("JumpRangeModifier");
    }

    public int GetBaseRange() => this.MaxDistance + this.ParentObject.GetIntProperty("JumpRangeModifier");

    public static int GetActiveRange(XRL.World.GameObject Actor, out int MinimumRange, int RangeModifier = 0)
    {
      int RangeModifier1;
      GetJumpingBehaviorEvent.Retrieve(Actor, out RangeModifier1, out MinimumRange, out string _, out string _, out string _, out bool _);
      return Skirmishing_Skirmish.GetBaseRange(Actor) + RangeModifier + RangeModifier1;
    }

    public static int GetActiveRange(XRL.World.GameObject Actor, int RangeModifier = 0)
    {
      int RangeModifier1;
      GetJumpingBehaviorEvent.Retrieve(Actor, out RangeModifier1, out int _, out string _, out string _, out string _, out bool _);
      return Skirmishing_Skirmish.GetBaseRange(Actor) + RangeModifier + RangeModifier1;
    }

    public int GetActiveRange(int RangeModifier = 0)
    {
      int RangeModifier1;
      GetJumpingBehaviorEvent.Retrieve(this.ParentObject, out RangeModifier1, out int _, out string _, out string _, out string _, out bool _);
      return this.GetBaseRange() + RangeModifier + RangeModifier1;
    }
	
	//NEXT: Make it so that you can only target 1 cell with the jump
    public static bool Jump(
      XRL.World.GameObject Actor,
      int RangeModifier = 0,
      Cell TargetCell = null,
      string SourceKey = null)
    {
      bool flag1 = TargetCell != null;
      int RangeModifier1;
      int MinimumRange;
      string AbilityName;
      string Verb1;
      string ProviderKey;
      bool CanJumpOverCreatures;
      GetJumpingBehaviorEvent.Retrieve(Actor, out RangeModifier1, out MinimumRange, out AbilityName, out Verb1, out ProviderKey, out CanJumpOverCreatures);
      int num1 = Skirmishing_Skirmish.GetBaseRange(Actor) + RangeModifier + RangeModifier1;
      if (Actor.OnWorldMap())
      {
        Actor.Fail("You cannot " + Verb1 + " on the world map.");
        return false;
      }
      if (Actor.IsFlying)
      {
        Actor.Fail("You cannot " + Verb1 + " while flying.");
        return false;
      }
      if (!Actor.CanChangeMovementMode("Jumping", true) || !Actor.CanChangeBodyPosition("Jumping", true))
        return false;
      /*if (!Actor.HasBodyPart("Feet"))
      {
        Actor.Fail("You cannot " + Verb1 + " without feet.");
        return false;
      }*/
      SoundManager.PreloadClipSet("Sounds/Abilities/sfx_ability_jump");
      Cell currentCell = Actor.CurrentCell;
	  //What are these meant to be??? Just setting them both to null solves the problem, but I don't understand what this code does.
      XRL.World.GameObject ZObject = null;
      List<Point> ZPath = null;
      while (true)
      {
        if (TargetCell == null)
        {
          TargetCell = !Actor.IsPlayer() ? Skirmishing_Skirmish.FindCellToApproachTarget(Actor, Actor.Target, MinimumRange, num1, CanJumpOverCreatures, Verb: Verb1) : PickTarget.ShowPicker(PickTarget.PickStyle.Line, num1, num1, currentCell.X, currentCell.Y, false, UsePassability: Actor, Label: "Jump where?", UseTarget: false);
          if (TargetCell == null)
            break;
        }
		
        int num2 = Actor.DistanceTo(TargetCell);
        if (!isValidKnightMove(Actor.CurrentCell,TargetCell))
        {
          if (!flag1)
          {
            Actor.Fail("You must select a knight's move" + "!");
            if (Actor.IsPlayer())
              TargetCell = (Cell) null;
            else
              goto label_16;
          }
          else
            goto label_16;
        }
        else
        {
          XRL.World.GameObject Actor1 = Actor;
          Cell TargetCell1 = TargetCell;
		  //This is causing an error... I think it might just be because of the bad names for the variables. I will change it to "ZObject" and "ZPath" to see what happens.
		  //I am an amateur.
		  //No, it is causing this because the local variables are unassigned... Which I understand, but... But this is directly copied from functional code in the game? Something must be different.
		  //I am missing a piece, surely.
		  //To solve this I am going to attempt to manually set the variables in question to their defaults and see if it helps.
          ref XRL.World.GameObject local1 = ref ZObject;
          ref List<Point> local2 = ref ZPath;
          bool flag2 = CanJumpOverCreatures;
          string str = Verb1;
          int num3 = flag1 ? 1 : 0;
          int num4 = flag2 ? 1 : 0;
          string Verb2 = str;
          if (!Skirmishing_Skirmish.CheckPath(Actor1, TargetCell1, out local1, out local2, num3 != 0, num4 != 0, Verb: Verb2))
          {
            if (!flag1 && Actor.IsPlayer())
              TargetCell = (Cell) null;
            else
              goto label_25;
          }
          else
            goto label_26;
        }
      }
      return false;
label_16:
      return false;
label_21:
      return false;
label_25:
      return false;
label_26:
      Actor?.PlayWorldSound("Sounds/Abilities/sfx_ability_jump");
      Actor.MovementModeChanged("Jumping");
      Actor.BodyPositionChanged("Jumping");
      Skirmishing_Skirmish.PlayAnimation(Actor, TargetCell);
      if (ZObject != null)
        IComponent<XRL.World.GameObject>.XDidYToZ(Actor, Verb1, "over", ZObject, EndMark: ".");
      else
        IComponent<XRL.World.GameObject>.XDidY(Actor, Verb1, EndMark: ".");
      if (Actor.DirectMoveTo(TargetCell, IgnoreCombat: true, IgnoreGravity: true))
        JumpedEvent.Send(Actor, currentCell, TargetCell, ZPath, num1, AbilityName, ProviderKey, SourceKey);
      Actor.Gravitate();
      Skirmishing_Skirmish.Land(currentCell, TargetCell);
      return true;
    }

    public static void PlayAnimation(XRL.World.GameObject Actor, Cell TargetCell)
    {
      if (!Options.UseOverlayCombatEffects || !TargetCell.IsVisible())
        return;
      CombatJuice.BlockUntilFinished((CombatJuiceEntry) CombatJuice.Jump(Actor, Actor.CurrentCell.Location, TargetCell.Location, Stat.Random(0.3f, 0.4f), Stat.Random(0.4f, 0.5f), 0.75f, Actor.IsPlayer()), (IList<XRL.World.GameObject>) new XRL.World.GameObject[1]
      {
        Actor
      });
    }

    public static void Leap(Cell From, Cell To, int Count = 3, int Life = 12)
    {
      if (!From.IsVisible())
        return;
      float Angle = (float) Math.Atan2((double) (From.X - To.X), (double) (From.Y - To.Y));
      Skirmishing_Skirmish.Leap(From.X, From.Y, Angle, Count, Life);
    }

    public static void Leap(int X, int Y, float Angle, int Count = 3, int Life = 12)
    {
      for (int index = 0; index < Count; ++index)
      {
        double f = (double) Stat.RandomCosmetic(-90, 90) * (Math.PI / 180.0) + (double) Angle;
        float xDel = Mathf.Sin((float) f) / (float) Life;
        float yDel = Mathf.Cos((float) f) / (float) Life;
        XRLCore.ParticleManager.Add("&y±", (float) X, (float) Y, xDel, yDel, Life, 0.0f, 0.0f);
      }
    }

    public static void Land(Cell From, Cell To, int Count = 4, int Life = 8)
    {
      if (!To.IsVisible())
        return;
      float Angle = (float) Math.Atan2((double) (To.X - From.X), (double) (To.Y - From.Y));
      Skirmishing_Skirmish.Land(To.X, To.Y, Angle, Count, Life);
    }

    public static void Land(int X, int Y, float Angle, int Count = 4, int Life = 8)
    {
      for (int index = 0; index < Count; ++index)
      {
        double f = (double) Stat.RandomCosmetic(-75, 75) * (Math.PI / 180.0) + (double) Angle;
        float xDel = Mathf.Sin((float) f) / ((float) Life / 2f);
        float yDel = Mathf.Cos((float) f) / ((float) Life / 2f);
        string Text = Stat.RandomCosmetic(1, 4) <= 3 ? "&y." : "&y±";
        XRLCore.ParticleManager.Add(Text, (float) X, (float) Y, xDel, yDel, Life, 0.0f, 0.0f);
      }
    }

    private bool ValidJump(Cell Cell, XRL.World.GameObject Actor = null, int? Range = null, int? MinimumRange = null) => this.ValidJump((Actor ?? this.ParentObject).DistanceTo(Cell), Actor, Range, MinimumRange);

    private bool ValidJump(int Distance, XRL.World.GameObject Actor = null, int? Range = null, int? MinimumRange = null)
    {
      int num1 = Distance;
      int? nullable = MinimumRange;
      int num2 = nullable ?? 2;
      if (num1 < num2)
        return false;
      int num3 = Distance;
      nullable = Range;
      int num4 = nullable ?? this.GetBaseRange();
      return num3 <= num4;
    }

    public void SyncAbility(bool Silent = false)
    {
      int RangeModifier;
      string AbilityName;
      GetJumpingBehaviorEvent.Retrieve(this.ParentObject, out RangeModifier, out int _, out AbilityName, out string _, out string _, out bool _);
      int num = this.GetBaseRange() + RangeModifier;
      if (!(this.ActivatedAbilityID == Guid.Empty) && !(AbilityName != this.ActiveAbilityName) && num == this.ActiveRange)
        return;
      bool flag = this.ActiveAbilityName == AbilityName;
      this.RemoveMyActivatedAbility(ref this.ActivatedAbilityID);
      this.ActiveAbilityName = AbilityName;
      this.ActiveRange = num;
      if (AbilityName.IsNullOrEmpty())
        return;
      this.ActivatedAbilityID = this.AddMyActivatedAbility(AbilityName, Skirmishing_Skirmish.COMMAND_NAME, "Skills", Icon: "\u0017", Silent: Silent | flag);
    }

    public static void SyncAbility(XRL.World.GameObject Actor, bool Silent = false) => Actor.GetPart<Skirmishing_Skirmish>()?.SyncAbility(Silent);

    public int GetBaseCooldown() => 1;
	
	//My lovely method
	public static bool isValidKnightMove(Cell StartCell, Cell TargetCell)
	{
		int differencex = Math.Abs(StartCell.X - TargetCell.X);
		int differencey = Math.Abs(StartCell.Y - TargetCell.Y);
		if ((differencex == 2 && differencey == 1) || (differencex == 1 && differencey == 2))
		{
			return true;
		}
			
		return false;
	}
  }
}
