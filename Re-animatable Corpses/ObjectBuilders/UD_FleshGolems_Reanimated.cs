
using System;
using System.Collections.Generic;
using System.Linq;

using ConsoleLib.Console;

using Qud.API;

using XRL.Collections;
using XRL.Names;
using XRL.UI;
using XRL.Wish;
using XRL.World.Anatomy;
using XRL.World.Capabilities;
using XRL.World.Parts;
using XRL.World.WorldBuilders;

using UD_FleshGolems;

using static XRL.World.Parts.UD_FleshGolems_DestinedForReanimation;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    [HasWishCommand]
    public class UD_FleshGolems_Reanimated : IObjectBuilder
    {
        public static bool IsGameRunning => The.Game != null && The.Game.Running;
        public static bool HasWorldGenerated => IsGameRunning && The.Player != null;
        public static string ReanimatedEquipped => nameof(UD_FleshGolems_Reanimated) + ":Equipped";

        public static List<string> PartsThatNeedDelayedReanimation => new()
        {
            nameof(ReplaceObject),
            nameof(ConvertSpawner),
            nameof(CherubimSpawner),
        };
        public static List<string> BlueprintsThatNeedDelayedReanimation => new()
        {
            "BaseCherubimSpawn",
        };
        public static List<string> PropertiesAndTagsIndicatingNeedDelayedReanimation => new()
        {
            "AlternateCreatureType",
        };

        public override void Initialize()
        {
        }

        public override void Apply(GameObject Object, string Context = null)
        {
            Unkill(Object, Context);
        }

        public static bool CreatureNeedsDelayedReanimation(GameObject Creature)
            => PartsThatNeedDelayedReanimation.Any(s => Creature.HasPart(s))
            || BlueprintsThatNeedDelayedReanimation.Any(s => Creature.GetBlueprint().InheritsFrom(s))
            || PropertiesAndTagsIndicatingNeedDelayedReanimation.Any(s => Creature.HasPropertyOrTag(s));

        public static GameObject ProduceCorpse(GameObject Creature,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true,
            bool PreemptivelyGiveEnergy = true)
        {
            GameObject corpse = null;
            try
            {
                Body body = Creature.Body;
                string corpseBlueprintName = null;
                GameObjectBlueprint corpseBlueprint = null;
                if (GameObjectFactory.Factory.GetBlueprintIfExists(Creature.GetTag("UD_FleshGolems CorpseBlueprint Override")) is GameObjectBlueprint corpseBlueprintOverride
                    && corpseBlueprintOverride.InheritsFrom("Corpse"))
                {
                    corpseBlueprintName = corpseBlueprintOverride.Name;
                }
                if (corpseBlueprintName.IsNullOrEmpty()
                    && Creature.TryGetPart(out Corpse corpsePart)
                    && !corpsePart.CorpseBlueprint.IsNullOrEmpty())
                {
                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpsePart?.CorpseBlueprint);
                    if (corpseBlueprint.InheritsFrom("Corpse"))
                        corpseBlueprintName = corpsePart.CorpseBlueprint;
                }
                if (corpseBlueprintName.IsNullOrEmpty())
                { 
                    string creatureBaseBlueprint = Creature.GetBlueprint().GetBaseTypeName();
                    corpseBlueprintName = creatureBaseBlueprint + " Corpse";
                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                    string speciesCorpse = Creature.GetSpecies() + " " + nameof(Corpse);
                    string fallbackCorpse = "Fresh " + nameof(Corpse);

                    if (corpseBlueprint == null)
                        corpseBlueprintName = GameObjectFactory.Factory?.GetBlueprintIfExists(creatureBaseBlueprint)
                            ?.GetPartParameter(
                                PartName: nameof(Corpse),
                                ParameterName: nameof(Corpse.CorpseBlueprint),
                                Default: speciesCorpse);

                    corpseBlueprint = GameObjectFactory.Factory.GetBlueprintIfExists(corpseBlueprintName);

                    corpseBlueprintName = corpseBlueprint?.Name ?? fallbackCorpse;
                }
                if ((corpse = GameObject.Create(corpseBlueprintName, Context: nameof(UD_FleshGolems_PastLife))) == null)
                    return null;

                Parts.Temporary.CarryOver(Creature, corpse);
                Phase.carryOver(Creature, corpse);
                if (Creature.HasProperName)
                    corpse.SetStringProperty("CreatureName", Creature.BaseDisplayName);
                else
                {
                    string creatureName = NameMaker.MakeName(Creature, FailureOkay: true);
                    if (creatureName != null)
                        corpse.SetStringProperty("CreatureName", creatureName);
                }
                if (Creature.HasID)
                    corpse.SetStringProperty("SourceID", Creature.ID);

                corpse.SetStringProperty("SourceBlueprint", Creature.Blueprint);
                if (50.in100())
                {
                    string killerBlueprint = EncountersAPI.GetACreatureBlueprint();
                    if (100.in100())
                    {
                        List<GameObject> cachedObjects = Event.NewGameObjectList(The.ZoneManager.CachedObjects.Values);
                        cachedObjects.RemoveAll(GO => !GO.IsAlive);
                        if (cachedObjects.GetRandomElement() is GameObject killer
                            && killer.HasID)
                        {
                            killerBlueprint = killer.Blueprint;
                            corpse.SetStringProperty("KillerID", killer.ID);
                        }
                        cachedObjects.Clear();
                    }
                    corpse.SetStringProperty("KillerBlueprint", killerBlueprint);
                }
                corpse.SetStringProperty("DeathReason", CheckpointingSystem.deathIcons.Keys.GetRandomElement());

                string genotype = Creature.GetGenotype();
                if (!genotype.IsNullOrEmpty())
                    corpse.SetStringProperty("FromGenotype", genotype);

                if (body != null)
                {
                    List<GameObject> list = null;
                    foreach (BodyPart part in body.GetParts())
                        if (part.Cybernetics != null)
                        {
                            list ??= Event.NewGameObjectList();
                            list.Add(part.Cybernetics);
                            UnimplantedEvent.Send(Creature, part.Cybernetics, part);
                            ImplantRemovedEvent.Send(Creature, part.Cybernetics, part);
                        }

                    if (list != null)
                    {
                        var butcherableCybernetics = corpse.RequirePart<CyberneticsButcherableCybernetic>();
                        butcherableCybernetics.Cybernetics.AddRange(list);
                        corpse.RemovePart<Food>();
                    }
                }

                if (OverridePastLife)
                    corpse.RemovePart<UD_FleshGolems_PastLife>();

                var pastLife = corpse.RequirePart<UD_FleshGolems_PastLife>();

                if (Creature.TryGetPart(out UD_FleshGolems_PastLife prevPastLife)
                    && prevPastLife.Init && prevPastLife.IsCorpse)
                {
                    corpse.RemovePart(pastLife);
                    pastLife = corpse.AddPart(prevPastLife);
                }
                else
                    pastLife.Initialize(Creature);

                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(Creature);
                if (ForImmediateReanimation)
                {
                    var corpseReanimationHelper = corpse.RequirePart<UD_FleshGolems_CorpseReanimationHelper>();
                    corpseReanimationHelper.AlwaysAnimate = true;

                    var destinedForReanimation = Creature.RequirePart<UD_FleshGolems_DestinedForReanimation>();
                    destinedForReanimation.Corpse = corpse;
                    destinedForReanimation.BuiltToBeReanimated = true;
                }

                if (PreemptivelyGiveEnergy) // fixes cases where corpses are being added to the action queue before they've been animated.
                {
                    corpse.Statistics ??= new();
                    string energyStatName = "Energy";
                    Statistic energyStat = null;
                    if (GameObjectFactory.Factory.GetBlueprintIfExists(nameof(Creature)) is var baseCreatureBlueprint)
                    {
                        if (!baseCreatureBlueprint.Stats.IsNullOrEmpty()
                            && baseCreatureBlueprint.Stats.ContainsKey(energyStatName))
                            energyStat = new(baseCreatureBlueprint.Stats[energyStatName])
                            {
                                Owner = corpse,
                            };
                    }
                    else
                        energyStat = new()
                        {
                            Name = energyStatName,
                            Min = -100000,
                            Max = 100000,
                            BaseValue = 0,
                            Owner = corpse,
                        };

                    corpse.Statistics.TryAdd(energyStatName, energyStat);
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(ProduceCorpse), x, "game_mod_exception");
            }
            return corpse;
        }

        public static bool TryProduceCorpse(
            GameObject Creature,
            out GameObject Corpse,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true
            )
            => (Corpse = ProduceCorpse(Creature, ForImmediateReanimation, OverridePastLife)) != null;

        public static bool TransferInventory(GameObject Entity, GameObject Corpse, bool SkipNatural)
        {
            if (Entity == null
                || Corpse == null)
                return false;

            string declaringTypeAndMethod = nameof(UD_FleshGolems_Reanimated) + "." + nameof(TransferInventory);

            Inventory entityInventory = Entity.RequirePart<Inventory>();
            Inventory corpseInventory = Corpse.RequirePart<Inventory>();
            Corpse.Inventory = corpseInventory;
            int erroredItems = 0;
            bool any = false;
            bool anyToTransfer = entityInventory.Objects.Count > 1;
            while (entityInventory.Objects.Count > erroredItems)
                try
                {
                    GameObject inventoryItem = entityInventory.Objects[0];
                    entityInventory.RemoveObject(inventoryItem);
                    if (!SkipNatural
                        || !inventoryItem.IsNatural())
                    {
                        corpseInventory.AddObject(inventoryItem);
                        any = true;
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(declaringTypeAndMethod + " transfer", x, "game_mod_exception");
                    erroredItems++;
                }

            if (Entity.Body is not Body entityBody
                || Corpse.Body is not Body corpseBody)
                return any || !anyToTransfer;

            using var equippedItems = ScopeDisposedList<GameObject>.GetFromPool();
            using var equippedLimbs = ScopeDisposedList<BodyPart>.GetFromPool();
            using var entityEquippedLimbItems = ScopeDisposedList<KeyValuePair<BodyPart, GameObject>>.GetFromPool();
            foreach (BodyPart bodyPart in entityBody.LoopParts().Where(bp => bp.Equipped != null && !bp.Equipped.IsNatural()))
                try
                {
                    if (bodyPart.Equipped is GameObject equippedItem
                        && !equippedItem.IsNatural())
                    {
                        equippedItem.SetStringProperty(ReanimatedEquipped, bodyPart.Type);

                        Entity.FireEvent(Event.New("CommandUnequipObject", "BodyPart", bodyPart, "SemiForced", 1));

                        entityEquippedLimbItems.Add(new(bodyPart, equippedItem));
                    }
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(declaringTypeAndMethod + " unequip", x, "game_mod_exception");
                }

            entityInventory.Clear();

            return !anyToTransfer
                || (any && EquipPastLifeItems(Corpse, SkipNatural));
        }

        private static bool WantsToBeEquippedByReanimated(GameObject Item)
            => Item.HasStringProperty(ReanimatedEquipped);

        public static bool EquipPastLifeItems(GameObject FrankenCorpse, bool SkipNatural, bool RemoveProperty = false)
        {
            if (FrankenCorpse == null
                || FrankenCorpse.Body is not Body frankenBody
                || FrankenCorpse.Inventory is not Inventory frankenInventory)
                return false;

            string declaringTypeAndMethod = nameof(UD_FleshGolems_Reanimated) + "." + nameof(EquipPastLifeItems);

            using var itemsToEquip = ScopeDisposedList<GameObject>.GetFromPoolFilledWith(frankenInventory.GetObjects(WantsToBeEquippedByReanimated));

            bool isNotSkipNaturalOrNotNatural(GameObject Item)
                => Item != null
                && (!SkipNatural
                    || !Item.IsNatural());

            bool any = false;
            bool anyToEquip = itemsToEquip?.Any(isNotSkipNaturalOrNotNatural) ?? false;

            using var equippedBodyParts = ScopeDisposedList<int>.GetFromPool();

            bool bodyPartNotHasBeenEquipped(BodyPart BodyPart)
                => !equippedBodyParts.Contains(BodyPart.ID);

            foreach (GameObject inventoryItem in itemsToEquip)
                try
                {
                    if (isNotSkipNaturalOrNotNatural(inventoryItem)
                        && inventoryItem.GetStringProperty(ReanimatedEquipped) is string bodyPartType
                        && frankenBody.GetUnequippedPart(bodyPartType)?.Where(bodyPartNotHasBeenEquipped).ToList() is List<BodyPart> unequippedParts
                        && unequippedParts.GetRandomElementCosmetic() is BodyPart equippablePart)
                        any = FrankenCorpse.FireEvent(Event.New("CommandEquipObject", "Object", inventoryItem, "BodyPart", equippablePart)) || any;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(declaringTypeAndMethod, x, "game_mod_exception");
                }
                finally
                {
                    if (RemoveProperty)
                        inventoryItem.RemoveStringProperty(ReanimatedEquipped);
                }

            return any || !anyToEquip;
        }

        public static bool TryTransferInventoryToCorpse(GameObject Entity, GameObject Corpse, bool SkipNatural)
        {
            bool transferred;
            try
            {
                transferred = TransferInventory(Entity, Corpse, SkipNatural);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(TryTransferInventoryToCorpse), x, "game_mod_exception");
                transferred = false;
            }
            return transferred;
        }

        public static bool Unkill(GameObject Entity, out GameObject Corpse, string Context = null)
        {
            Corpse = null;
            UD_FleshGolems_DestinedForReanimation destinedForReanimation = null;
            if (Entity.IsPlayer()
                || Entity.IsPlayerDuringWorldGen())
            {
                destinedForReanimation = Entity.RequirePart<UD_FleshGolems_DestinedForReanimation>();
                destinedForReanimation.PlayerWantsFakeDie = true;
                destinedForReanimation.BuiltToBeReanimated = true;
                HaveFakedPlayerDeath = false;
            }

            if (Context == "Sample")
                return false;

            if (Context == nameof(UD_FleshGolems_MadMonger_WorldBuilder))
                return false;

            if (Entity.HasPart<UD_FleshGolems_ReanimatedCorpse>())
                return false;

            if (!TryProduceCorpse(Entity, out Corpse))
                return false;

            if (!Corpse.TryGetPart(out destinedForReanimation))
                return false;

            if (Corpse == null)
                if (Entity.IsPlayer())
                {
                    if (!ReplacePlayerWithCorpse())
                    {
                        Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                        return false;
                    }
                }
                else
                if (!ReplaceEntityWithCorpse(Entity))
                    return false;

            return true;
        }
        public static bool Unkill(GameObject Entity, string Context = null)
            => Unkill(Entity, out _, Context);

        public static bool PerformAFakeDeath(
            GameObject Entity,
            GameObject Corpse,
            IDeathEvent DeathEvent = null,
            Renderable CorpseIcon = null)
        {
            if (Entity == null
                || Corpse == null)
                return false;

            if (DeathEvent == null)
                return FakeRandomDeath(Entity, CorpseIcon: CorpseIcon);

            return FakeDeath(Entity, DeathEvent, DoAchievement: true, CorpseIcon: CorpseIcon);
        }

        public static bool ReplaceEntityWithCorpse(
            GameObject Entity,
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true)
        {
            FakedDeath = false;
            if (Entity == null)
                return false;

            if ((Corpse == null || OverridePastLife)
                && !TryProduceCorpse(Entity, out Corpse, ForImmediateReanimation, OverridePastLife))
                return false;

            if (!ForImmediateReanimation)
                return true;

            Corpse.RequireAbilities();

            if (Entity.IsPlayer()
                || Entity.IsPlayerDuringWorldGen())
                Corpse.SetIntProperty("UD_FleshGolems_SkipLevelsOnReanimate", 1);

            if (!Corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper)
                || !reanimationHelper.Animate(out Corpse))
                return false;

            if (!TryTransferInventoryToCorpse(Entity, Corpse, true))
            {
                MetricsManager.LogModError(Utils.ThisMod, 
                    "Failed to " + nameof(ReplaceEntityWithCorpse) + " due to failure of " + nameof(TryTransferInventoryToCorpse));
                return false;
            }

            bool replaced = false;
            try
            {
                if (FakeDeath)
                    PerformAFakeDeath(
                        Entity: Entity,
                        Corpse: Corpse,
                        DeathEvent: DeathEvent,
                        CorpseIcon: new FlippableRenderable(Corpse.RenderForUI(), Entity.IsPlayerDuringWorldGen()));

                ReplaceInContextEvent.Send(Entity, Corpse);

                if (Entity.IsPlayerDuringWorldGen())
                {
                    The.Game.Player.SetBody(Corpse);

                    Corpse.Brain.Allegiance.Clear();
                    Corpse.Brain.Allegiance["Player"] = 100;

                    if (Entity.IsOriginalPlayerBody())
                    {
                        Corpse.BaseID = 1;
                        Corpse.SetStringProperty("id", null);
                        Corpse.InjectGeneID("OriginalPlayer");
                        Corpse.SetStringProperty("OriginalPlayerBody", "1");
                    }

                    Corpse.SetIntProperty("Renamed", 1);
                }

                Entity.MakeInactive();
                Corpse.MakeActive();

                Entity?.Obliterate();

                replaced = true;
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_Reanimated) + "." + nameof(ReplaceEntityWithCorpse), x, "game_mod_exception");
                replaced = false;
            }
            return replaced;
        }
        public static bool ReplaceEntityWithCorpse(
            GameObject Entity,
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true
            )
            => ReplaceEntityWithCorpse(Entity, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);

        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath,
            out bool FakedDeath,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true
            )
            => ReplaceEntityWithCorpse(The.Player, FakeDeath, out FakedDeath, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);

        public static bool ReplacePlayerWithCorpse(
            bool FakeDeath = true,
            IDeathEvent DeathEvent = null,
            GameObject Corpse = null,
            bool ForImmediateReanimation = true,
            bool OverridePastLife = true
            )
            => ReplaceEntityWithCorpse(The.Player, FakeDeath, out _, DeathEvent, Corpse, ForImmediateReanimation, OverridePastLife);

        [WishCommand("UD_FleshGolems reanimated")]
        public static void Reanimated_WishHandler()
            => Reanimated_WishHandler(null);

        [WishCommand("UD_FleshGolems reanimated", null)]
        public static bool Reanimated_WishHandler(string Blueprint)
        {
            GameObject soonToBeCorpse;
            if (Blueprint == null)
            {
                if (Popup.ShowYesNo(
                    "This {{Y|probably}} won't end your run. " +
                    "Last chance to back out.\n\n" +
                    "If you meant to reanimate something else," +
                    "make this wish again but include a blueprint.") == DialogResult.No)
                    return false;

                soonToBeCorpse = The.Player;
            }
            else
            {
                WishResult wishResult = WishSearcher.SearchForBlueprint(Blueprint);
                soonToBeCorpse = GameObjectFactory.Factory.CreateObject(wishResult.Result, Context: "Wish");
            }
            if (Unkill(soonToBeCorpse, out GameObject soonToBeCreature, Context: "Wish"))
            {
                if (!soonToBeCreature.HasPart<AnimatedObject>()
                    && soonToBeCreature.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper corpseReanimationHelper))
                    corpseReanimationHelper.AlwaysAnimate = true;

                if (Blueprint != null)
                    The.PlayerCell.getClosestEmptyCell().AddObject(soonToBeCreature);
                else
                if (Blueprint == null)
                {
                    if (soonToBeCreature == null && !ReplaceEntityWithCorpse(soonToBeCorpse, true, null, soonToBeCreature))
                    {
                        Popup.Show("Something terrible has happened (not really, it just failed).\n\nCheck Player.log for errors.");
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
