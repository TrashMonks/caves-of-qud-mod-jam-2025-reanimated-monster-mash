using System;
using System.Collections.Generic;
using XRL.World;
using XRL.World.AI;
using XRL.World.AI.GoalHandlers;
using XRL.World.Anatomy;
using XRL.World.Effects;
using XRL.World.Parts;
using HDBrownie.Effects;
using HDBrownie.Goals;

namespace XRL.World.Parts
{
    [Serializable]
    public abstract class HDBrownie_BaseAILouse : AIBehaviorPart
    {
        public long LastTick = 0;

        public override bool WantEvent(int id, int cascade)
        {
            return
                base.WantEvent(id, cascade)
                || id == AIBoredEvent.ID
            ;
        }

        public override bool HandleEvent(AIBoredEvent @event)
        {
            ConsiderNests();
            return base.HandleEvent(@event);
        }

        public void ConsiderNests()
        {
            var potentialNest = FindPotentialNest();
            if (potentialNest != null)
            {
                InvestigateNest(potentialNest);
            }
        }

        public GameObject FindPotentialNest()
        {
            var potentialNests = new List<(GameObject @object, int score)>();
            foreach (var cell in ParentObject.CurrentCell.IterateAdjacent(Radius: 80, LocalOnly: true))
            {
                foreach (var @object in cell.Objects)
                {
                    var objectScore = ComputeNestScore(@object);
                    if (IsInhabitable(@object) && objectScore != 0)
                    {
                        potentialNests.Add((@object, objectScore));
                    }

                    foreach (var item in @object.GetInventoryAndEquipment())
                    {
                        var itemScore = ComputeNestScore(item);
                        if (IsInhabitable(item) && itemScore != 0)
                        {
                            potentialNests.Add((item, itemScore));
                        }
                    }
                }
            }

            if (potentialNests.Count < 1) { return null; }
            potentialNests.StableSortInPlace((a, b) => b.score - a.score);
            return potentialNests[0].@object;
        }

        public bool IsInhabitable(GameObject @object)
        {
            return @object != ParentObject
                && Inhabited.IsInhabitable(@object);
        }

        public void InvestigateNest(GameObject @object)
        {
            ParentObject.Brain.PushGoal(new InvestigateNest
            {
                Target = @object,
                Part = this,
            });
        }

        public bool NestIn(GameObject @object)
        {
            LastTick = The.Game.TimeTicks;
            return @object.ApplyEffect(new Inhabited(ParentObject));
        }

        public abstract int ComputeNestScore(GameObject @object);
    }

    [Serializable]
    public class HDBrownie_AITableLouse : HDBrownie_BaseAILouse
    {
        public const string DEATH_REASON = "You were consumed from inside out.";
        public string DamageRoll = "1d4";
        public int TotalDamage = 0;

        public override bool WantEvent(int id, int cascade)
        {
            return
                base.WantEvent(id, cascade)
            ;
        }

        public override int ComputeNestScore(GameObject @object)
        {
            if (@object.TryGetPart(out Container container))
            {
                if (container.Preposition == "on")
                {
                    return 2;
                }
                else
                {
                    return 1;
                }
            }

            return 0;
        }

        public override void Register(GameObject @object, IEventRegistrar registrar)
        {
            registrar.Register("HDBrownie_InhabitingTick");
            registrar.Register("HDBrownie_NestDamaged");
            base.Register(@object, registrar);
        }

        public override bool FireEvent(Event @event)
        {
            var nest = @event.GetParameter<GameObject>("Nest");

            switch (@event.ID)
            {
                case "HDBrownie_InhabitingTick":
                    var thisTick = @event.GetParameter<long>("TimeTick");
                    for (var n = 0; n <= thisTick - LastTick; n++)
                    {
                        var damage = DamageRoll.Roll();
                        TotalDamage += damage;
                        nest.TakeDamage(
                            Amount: damage,
                            Attributes: "Unavoidable",
                            DeathReason: DEATH_REASON,
                            Message: "from infestation!"
                        );
                    }
                    LastTick = thisTick;
                    break;
                case "HDBrownie_NestDamaged":
                    if (TotalDamage > nest.baseHitpoints / 2)
                    {
                        var colony = nest.DeepCopy();
                        colony.hitpoints = TotalDamage;
                        colony.AddPart(new HDBrownie_Colony(ParentObject));
                        nest.GetCurrentCell().AddObject(colony);
                        return false;
                    }
                    break;
            }
            return base.FireEvent(@event);
        }
    }

    [Serializable]
    public class HDBrownie_AIDrinkLouse : HDBrownie_BaseAILouse
    {
        const long THIRST_THRESHOLD = 1000;
        long Thirst = 0;

        public override int ComputeNestScore(GameObject @object)
        {
            if (@object.TryGetPart(out LiquidVolume liquid))
            {
                // We're interested in only closed containers held by a
                // creature.
                if (!@object.IsOrganic || liquid.IsOpenVolume() || @object.Holder == null)
                {
                    return 0;
                }

                // Don't go after anything with glotrot.
                if (@object.Holder.HasEffectDescendedFrom<GlotrotOnset>() || @object.Holder.HasEffectDescendedFrom<Glotrot>())
                {
                    return 0;
                }

                // Prefer blood most of all.
                if (liquid.HasPrimaryOrSecondaryLiquid("blood"))
                {
                    return 3;
                }

                // Failing that, maybe we can inhabit the creature by way of
                // its water.
                if (liquid.HasPrimaryOrSecondaryLiquid("water"))
                {
                    return 2;
                }

                return 1;
            }

            return 0;
        }

        public override bool WantEvent(int id, int cascade)
        {
            return base.WantEvent(id, cascade)
                || id == EquippedEvent.ID
                || id == UnequippedEvent.ID
            ;
        }

        public override bool HandleEvent(EquippedEvent @event)
        {
            @event.Actor.RegisterPartEvent(this, "EffectApplied");
            return base.HandleEvent(@event);
        }

        public override bool HandleEvent(UnequippedEvent @event)
        {
            @event.Actor.UnregisterPartEvent(this, "EffectApplied");
            return base.HandleEvent(@event);
        }

        public override void Register(GameObject @object, IEventRegistrar registrar)
        {
            registrar.Register("HDBrownie_InhabitingTick");
            registrar.Register("HDBrownie_NestDrank");
            base.Register(@object, registrar);
        }

        public override bool FireEvent(Event @event)
        {
            var nest = @event.GetParameter<GameObject>("Nest");

            switch (@event.ID)
            {
                case "EffectApplied":
                    var effect = @event.GetParameter<Effect>("Effect");
                    var affected = effect.Object;
                    if (affected == ParentObject.Equipped && (
                        effect is GlotrotOnset || effect is Glotrot
                    ))
                    {
                        ParentObject.ForceUnequipAndRemove(true);
                        ParentObject.Physics.Takeable = false;
                        var cell = affected.CurrentCell.GetRandomLocalAdjacentCell(c => c.IsEmpty()) ?? affected.CurrentCell;
                        cell.AddObject(ParentObject);
                        DidXToY(
                            Verb: "detach",
                            Preposition: "from",
                            Object: affected,
                            ColorAsGoodFor: affected,
                            AlwaysVisible: true,
                            UsePopup: true
                        );
                    }
                    break;
                case "HDBrownie_InhabitingTick":
                    var thisTick = @event.GetParameter<long>("TimeTick");
                    Thirst += @event.GetIntParameter("TimeTick") - LastTick;
                    if (Thirst > 5 * THIRST_THRESHOLD)
                    {
                        Thirst = 5 * THIRST_THRESHOLD;
                    }
                    while (Thirst >= THIRST_THRESHOLD)
                    {
                        if (nest.TryGetPart<LiquidVolume>(out var liquid))
                        {
                            if (liquid.Volume < 1) { break; }
                            liquid.UseDram();
                            Thirst -= THIRST_THRESHOLD;
                        }
                    }
                    LastTick = thisTick;
                    break;
                case "HDBrownie_NestDrank":
                    var drinker = @event.GetParameter<GameObject>("Drinker");
                    if (drinker.HasEffectDescendedFrom<GlotrotOnset>() || drinker.HasEffectDescendedFrom<Glotrot>())
                    {
                        goto Done;
                    }

                    var parts = new List<BodyPart>();
                    drinker.Body.ForeachPart(p =>
                    {
                        if (p.Type == "Face" && (p.Equipped == null || p.Equipped.CanBeUnequipped(SemiForced: true)) && !p.Equipped.HasPartDescendedFrom<HDBrownie_BaseAILouse>())
                        {
                            parts.Add(p);
                        }
                    });
                    parts.ShuffleInPlace();

                    if (parts.Count > 0)
                    {
                        nest.RemoveEffectDescendedFrom<Inhabited>();
                        ParentObject.Physics.Takeable = true;
                        ParentObject.MakeInactive();
                        parts[0].Equip(ParentObject, SemiForced: true);
                        Thirst = 0;
                        DidXToY(
                            Verb: "attach",
                            Preposition: "to",
                            Object: drinker,
                            ColorAsBadFor: drinker,
                            EndMark: "!",
                            AlwaysVisible: true,
                            UsePopup: true
                        );
                    }
                    break;
            }
        Done:
            return base.FireEvent(@event);
        }
    }
}

namespace HDBrownie.Goals
{
    [Serializable]
    public class InvestigateNest : GoalHandler
    {
        public GameObject Target;
        public HDBrownie_BaseAILouse Part;

        public override void Create()
        {
            Think("I'm going to investigate a potential nest.");
        }

        public override void TakeAction()
        {
            if (ParentObject.isAdjacentTo(Target.Holder ?? Target))
            {
                Think("I'm going to try occupying my new nest.");

                if (!Part.NestIn(Target))
                {
                    Think("I couldn't enter the nest!");
                    FailToParent();
                }
            }
            else
            {
                Think("I'm going to move towards the potential nest.");
                PushChildGoal(new MoveTo(Target, careful: true, shortBy: 1));
            }
        }

        public override bool Finished()
        {
            return false;
        }
    }
}
