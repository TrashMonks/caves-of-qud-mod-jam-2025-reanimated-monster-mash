using System;
using System.Collections.Generic;
using System.Text;

using Genkit;
using XRL.Core;
using XRL.Language;
using XRL.Rules;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;
using XRL.World.ObjectBuilders;
using XRL.World.Quests.GolemQuest;
using XRL.World.Skills;
using XRL.World.AI;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_CorpseReanimationHelper : IScribedPart
    {
        public const string REANIMATED_CONVO_ID_TAG = "UD_FleshGolems_ReanimatedConversationID";
        public const string REANIMATED_EPITHETS_TAG = "UD_FleshGolems_ReanimatedEpithets";

        public UD_FleshGolems_PastLife PastLife => ParentObject?.GetPart<UD_FleshGolems_PastLife>();

        public static List<string> PhysicalStats => new() { "Strength", "Agility", "Toughness", };
        public static List<string> MentalStats => new() { "Intelligence", "Willpower", "Ego", };

        public static List<string> IPartsToSkipWhenReanimating => new()
        {
            nameof(Titles),
            nameof(Epithets),
            nameof(Honorifics),
            nameof(Lovely),
            nameof(SecretObject),
            nameof(ConvertSpawner),
            nameof(Leader),
            nameof(Followers),
            nameof(DromadCaravan),
            nameof(HasGuards),
            nameof(SnapjawPack1),
            nameof(BaboonHero1Pack),
            nameof(GoatfolkClan1),
            nameof(EyelessKingCrabSkuttle1),
        };

        public static List<string> BlueprintsToSkipCheckingForCorpses => new()
        {
            "OrPet",
        };

        public bool IsALIVE;

        public bool AlwaysAnimate;

        public string CreatureName;

        public string SourceBlueprint;

        public string CorpseDescription;

        private List<int> FailedToRegisterEvents;

        public UD_FleshGolems_CorpseReanimationHelper()
        {
            IsALIVE = false;
            AlwaysAnimate = false;
            CreatureName = null;
            SourceBlueprint = null;
            CorpseDescription = null;
            FailedToRegisterEvents = new();
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public bool Animate(out GameObject FrankenCorpse)
        {
            FrankenCorpse = null;
            // UnityEngine.Debug.Log(nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(Animate));
            if (!ParentObject.HasPart<AnimatedObject>())
            {
                // UnityEngine.Debug.Log("    " + nameof(ParentObject) + " not " + nameof(AnimatedObject));
                AnimateObject.Animate(ParentObject);

                if (ParentObject.HasPart<AnimatedObject>())
                {
                    FrankenCorpse = ParentObject;
                    return true;
                }
            }
            return false;
        }
        public bool Animate()
        {
            return Animate(out _);
        }

        public static string FigureOutWhatBlueprintThisCorpseCameFrom(GameObject Corpse, UD_FleshGolems_PastLife PastLife = null, bool PrintCheckEvenWhenPastLife = false)
        {
            string blueprint = null;
            if (PastLife?.Blueprint is string pastLifeBlueprint)
            {
                if (!PrintCheckEvenWhenPastLife)
                {
                    return pastLifeBlueprint;
                }
                blueprint = pastLifeBlueprint;
            }
            string corpseDisplayNameLC = Corpse.GetReferenceDisplayName(Stripped: true, Short: true)?.ToLower();
            string baseGameSourceBlueprintLC = Corpse.GetPropertyOrTag("SourceBlueprint")?.ToLower();
            string corpseType = Corpse.Blueprint.Replace(" Corpse", "").Replace("UD_FleshGolems ", "");
            string corpseSpecies = Corpse.GetStringProperty("Species", corpseType);
            List<string> probableBlueprints = new();
            List<string> possibleBlueprints = new();
            List<string> fallbackBlueprints = new();
            /*
            UnityEngine.Debug.Log(
                "Finding Blueprints for " + (Corpse?.DebugName ?? NULL) + " | " +
                nameof(corpseDisplayNameLC) + ": " + (corpseDisplayNameLC ?? NULL) + ", " +
                nameof(baseGameSourceBlueprintLC) + ": " + (baseGameSourceBlueprintLC ?? NULL) + ", " +
                nameof(corpseType) + ": " + (corpseType ?? NULL) + ", " +
                nameof(corpseSpecies) + ": " + (corpseSpecies ?? NULL) + ", " +
                nameof(corpseDisplayNameLC) + ": " + (corpseDisplayNameLC ?? NULL));
            */
            List<GameObjectBlueprint> blueprintsToCheck = new(GameObjectFactory.Factory.Blueprints.Values);
            blueprintsToCheck.ShuffleInPlace();
            int maxChecks = int.MaxValue; // 2500;
            int checkCounter = 0;
            // UnityEngine.Debug.Log("Checking " + maxChecks + "/" + blueprintsToCheck.Count);
            foreach (GameObjectBlueprint gameObjectBlueprint in blueprintsToCheck)
            {
                if (++checkCounter > maxChecks)
                {
                    break;
                }
                string blueprintName = gameObjectBlueprint.Name;
                string blueprintNameLC = blueprintName?.ToLower() ?? "XKCD";
                string gameObjectDisplayName = gameObjectBlueprint.DisplayName()?.Strip()?.ToLower() ?? "XKCD";
                if (blueprintName == Corpse.Blueprint)
                {
                    // UnityEngine.Debug.Log(AppendCross("    ") + blueprintName + " is this corpse");
                    continue;
                }
                if (gameObjectBlueprint.IsBaseBlueprint())
                {

                    // UnityEngine.Debug.Log(AppendCross("    ") + blueprintName + " is base blueprint");
                    continue;
                }
                if (!gameObjectBlueprint.InheritsFrom("Creature")
                    && !gameObjectBlueprint.InheritsFrom("Fungus")
                    && !gameObjectBlueprint.InheritsFrom("Plant")
                    && !gameObjectBlueprint.InheritsFrom("Corpse")
                    && !gameObjectBlueprint.InheritsFrom("Robot"))
                {
                    // UnityEngine.Debug.Log(AppendCross("    ") + blueprintName + " is not an entity capable of dying");
                    continue;
                }
                if (BlueprintsToSkipCheckingForCorpses.Contains(gameObjectBlueprint.Name))
                {
                    // UnityEngine.Debug.Log(AppendCross("    ") + blueprintName + " is definitely not the correct one.");
                    continue;
                }
                bool corpseDisplayNameContainsBlueprintDisplayName = corpseDisplayNameLC != null && corpseDisplayNameLC.Contains(gameObjectDisplayName);
                bool corpseTaggedWithBlueprint = baseGameSourceBlueprintLC != null && baseGameSourceBlueprintLC == blueprintNameLC;
                if (corpseDisplayNameContainsBlueprintDisplayName || corpseTaggedWithBlueprint)
                {
                    if (!probableBlueprints.Contains(blueprintName))
                    {
                        probableBlueprints.Add(blueprintName);
                        /*
                        UnityEngine.Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(probableBlueprints) + " }{ [" +
                            corpseDisplayNameLC + "] contains [" + gameObjectDisplayName + "] || [" + baseGameSourceBlueprintLC + "] == [" + blueprintNameLC + "]");
                        */
                    }
                }
                bool corpseTypeMatchesBlueprintName = corpseType != null && corpseType.ToLower() == blueprintNameLC;
                if (corpseTypeMatchesBlueprintName)
                {
                    if(!possibleBlueprints.Contains(blueprintName))
                    {
                        possibleBlueprints.Add(blueprintName);
                        /*
                        UnityEngine.Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(possibleBlueprints) + " }{ [" +
                            corpseType.ToLower() + "] == [" + blueprintNameLC + "]");
                        */
                    }
                }
                bool corpseSpeciesMatchesBlueprintSpecies = corpseSpecies != null && corpseSpecies?.ToLower() == gameObjectBlueprint.GetPropertyOrTag("Species")?.ToLower();
                if (corpseSpeciesMatchesBlueprintSpecies)
                {
                    if (!fallbackBlueprints.Contains(blueprintName))
                    {
                        fallbackBlueprints.Add(blueprintName);
                        /*
                        UnityEngine.Debug.Log(AppendTick("    ") + blueprintName + " added to " + nameof(fallbackBlueprints) + " }{ [" +
                            corpseSpecies?.ToLower() + "] == [" + gameObjectBlueprint.GetPropertyOrTag("Species")?.ToLower() + "]");
                        */
                    }
                }
            }

            if (possibleBlueprints.IsNullOrEmpty())
            {
                possibleBlueprints.AddRange(fallbackBlueprints);
                fallbackBlueprints.Clear();
            }
            if (probableBlueprints.IsNullOrEmpty())
            {
                probableBlueprints.AddRange(possibleBlueprints);
            }

            // UnityEngine.Debug.Log("Probable Blueprints for " + Corpse.DebugName);
            if (!probableBlueprints.IsNullOrEmpty())
            {
                /*
                probableBlueprints.ShuffleInPlace();
                foreach (string probableBlueprint in probableBlueprints)
                {
                    UnityEngine.Debug.Log("    " + probableBlueprint);
                }
                */
                blueprint ??= probableBlueprints.GetRandomElement();
                // UnityEngine.Debug.Log(nameof(blueprint) + " picked: " + blueprint);
                return blueprint;
            }

            blueprint ??= "Trash Monk";
            UnityEngine.Debug.Log(nameof(blueprint) + " picked: " + blueprint);
            return blueprint;
        }

        public static bool AssignStatsFromStatistics(
            GameObject FrankenCorpse,
            Dictionary<string, Statistic> Statistics,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            bool any = false;
            if (FrankenCorpse == null || Statistics.IsNullOrEmpty())
            {
                return any;
            }
            foreach ((string statName, Statistic sourceStat) in Statistics)
            {
                Statistic statistic = new(sourceStat)
                {
                    Owner = FrankenCorpse,
                };
                if (!FrankenCorpse.HasStat(statName))
                {
                    FrankenCorpse.Statistics.Add(statName, statistic);
                }
                else
                if (Override)
                {
                    FrankenCorpse.Statistics[statName] = statistic;
                }
                if (!StatAdjustments.IsNullOrEmpty() && StatAdjustments.ContainsKey(statName))
                {
                    FrankenCorpse.Statistics[statName].BaseValue += StatAdjustments[statName];
                }
                any = true;
            }
            FrankenCorpse.FinalizeStats();
            return any;
        }
        public static bool AssignStatsFromPastLife(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments);
        }
        public static bool AssignStatsFromPastLifeWithAdjustment(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            int PhysicalAdjustment = 0,
            int MentalAdjustment = 0)
        {
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, new()
            {
                { "Strength", PhysicalAdjustment },
                { "Agility", PhysicalAdjustment },
                { "Toughness", PhysicalAdjustment },
                { "Intelligence", MentalAdjustment },
                { "Willpower", MentalAdjustment },
                { "Ego", MentalAdjustment },
            });
        }
        public static bool AssignStatsFromPastLifeWithFactor(
            GameObject FrankenCorpse,
            UD_FleshGolems_PastLife PastLife,
            bool Override = true,
            float PhysicalAdjustmentFactor = 1f,
            float MentalAdjustmentFactor = 1f)
        {
            if (FrankenCorpse == null || PastLife == null || PastLife.Stats.IsNullOrEmpty())
            {
                return false;
            }
            Dictionary<string, int> StatAdjustments = new();
            foreach ((string statName, Statistic stat) in PastLife?.Stats)
            {
                if (PhysicalStats.Contains(statName) || MentalStats.Contains(statName))
                {
                    float adjustmentFactor = 1f;
                    if (PhysicalStats.Contains(statName))
                    {
                        adjustmentFactor = PhysicalAdjustmentFactor;
                    }
                    else
                    if (MentalStats.Contains(statName))
                    {
                        adjustmentFactor = MentalAdjustmentFactor;
                    }
                    if (adjustmentFactor != 1f)
                    {
                        StatAdjustments.Add(statName, (int)(stat.BaseValue * adjustmentFactor) - stat.BaseValue);
                    }
                }
            }
            return AssignStatsFromStatistics(FrankenCorpse, PastLife.Stats, Override, StatAdjustments);
        }
        public static bool AssignStatsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            bool Override = true,
            Dictionary<string, int> StatAdjustments = null)
        {
            return AssignStatsFromStatistics(FrankenCorpse, SourceBlueprint.Stats, Override, StatAdjustments);
        }

        public static void AssignPartsFromBlueprint(
            GameObject FrankenCorpse,
            GameObjectBlueprint SourceBlueprint,
            Predicate<IPart> Exclude = null)
        {
            if (FrankenCorpse == null || SourceBlueprint == null || SourceBlueprint.allparts.IsNullOrEmpty())
            {
                return;
            }
            foreach (GamePartBlueprint sourcePartBlueprint in SourceBlueprint.allparts.Values)
            {
                if (Stat.Random(1, sourcePartBlueprint.ChanceOneIn) == 1)
                {
                    if (sourcePartBlueprint.T == null)
                    {
                        XRLCore.LogError("Unknown part " + sourcePartBlueprint.Name + "!");
                        return;
                    }
                    IPart sourcePart = sourcePartBlueprint.Reflector?.GetInstance() ?? (Activator.CreateInstance(sourcePartBlueprint.T) as IPart);
                    if (sourcePart == null || Exclude != null && Exclude(sourcePart))
                    {
                        continue;
                    }
                    sourcePart.ParentObject = FrankenCorpse;
                    sourcePartBlueprint.InitializePartInstance(sourcePart);
                    FrankenCorpse.AddPart(sourcePart);

                    if (sourcePartBlueprint.TryGetParameter("Builder", out string partBuilderName)
                        && ModManager.ResolveType("XRL.World.PartBuilders", partBuilderName) is Type partBuilderType
                        && Activator.CreateInstance(partBuilderType) is IPartBuilder partBuilder)
                    {
                        partBuilder.BuildPart(sourcePart);
                    }
                }
            }
        }

        public static bool AssignMutationsFromPastLife(
            Mutations FrankenMutations,
            UD_FleshGolems_PastLife PastLife,
            Predicate<BaseMutation> Exclude = null)
        {
            bool any = false;
            if (FrankenMutations == null || PastLife == null || PastLife.MutationLevels.IsNullOrEmpty())
            {
                return any;
            }
            foreach ((string mutationName, int level) in PastLife.MutationLevels)
            {
                if (MutationFactory.GetMutationEntryByName(mutationName) is MutationEntry mutationEntry
                    && mutationEntry.Instance is BaseMutation baseMutation)
                {
                    if (Exclude != null && Exclude(baseMutation))
                    {
                        continue;
                    }
                    FrankenMutations.AddMutation(baseMutation, level);
                    any = true;
                }
            }
            return any;
        }
        public static bool AssignMutationsFromBlueprint(
            Mutations FrankenMutations,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseMutation> Exclude = null)
        {
            bool any = false;
            if (FrankenMutations == null || SourceBlueprint == null || SourceBlueprint.Mutations.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceMutationBlueprint in SourceBlueprint.Mutations.Values)
            {
                if (Stat.Random(1, sourceMutationBlueprint.ChanceOneIn) != 1)
                {
                    continue;
                }
                string mutationNamespace = "XRL.World.Parts.Mutation." + sourceMutationBlueprint.Name;
                Type mutationType = ModManager.ResolveType(mutationNamespace);

                if (mutationType == null)
                {
                    MetricsManager.LogError("Unknown mutation " + mutationNamespace);
                    return any;
                }
                if ((sourceMutationBlueprint.Reflector?.GetNewInstance() ?? Activator.CreateInstance(mutationType)) is not BaseMutation baseMutation)
                {
                    MetricsManager.LogError("Mutation " + mutationNamespace + " is not a BaseMutation");
                    continue;
                }
                if (Exclude != null && Exclude(baseMutation))
                {
                    continue;
                }
                sourceMutationBlueprint.InitializePartInstance(baseMutation);
                if (sourceMutationBlueprint.TryGetParameter("Builder", out string mutationBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + mutationBuilderName) is Type mutationBuilderType
                    && Activator.CreateInstance(mutationBuilderType) is IPartBuilder mutationBuilder)
                {
                    mutationBuilder.BuildPart(baseMutation, Context: "Initialization");
                }
                if (baseMutation.CapOverride == -1)
                {
                    baseMutation.CapOverride = baseMutation.Level;
                }
                FrankenMutations.AddMutation(baseMutation, baseMutation.Level);
                any = true;
            }
            return any;
        }

        public static bool AssignSkillsFromPastLife(
            Skills FrankenSkills,
            UD_FleshGolems_PastLife PastLife,
            Predicate<BaseSkill> Exclude = null)
        {
            bool any = false;
            if (FrankenSkills == null || PastLife == null || PastLife.Skills.IsNullOrEmpty())
            {
                return any;
            }
            foreach (string skillName in PastLife.Skills)
            {
                if (SkillFactory.Factory.GetSkillIfExists(skillName) is SkillEntry skillEntry
                    && (BaseSkill)skillEntry.Instance is BaseSkill skillPart)
                {
                    if (Exclude != null && Exclude(skillPart))
                    {
                        continue;
                    }
                    any = FrankenSkills.AddSkill(skillPart) || any;
                }
            }
            return any;
        }

        public static bool AssignSkillsFromBlueprint(
            Skills FrankenSkills,
            GameObjectBlueprint SourceBlueprint,
            Predicate<BaseSkill> Exclude = null)
        {
            bool any = false;
            if (FrankenSkills == null || SourceBlueprint == null || SourceBlueprint.Skills.IsNullOrEmpty())
            {
                return any;
            }
            foreach (GamePartBlueprint sourceSkillBlueprint in SourceBlueprint.Skills.Values)
            {
                if (Stat.Random(1, sourceSkillBlueprint.ChanceOneIn) != 1)
                {
                    continue;
                }
                string skillNamespace = "XRL.World.Parts.Skill." + sourceSkillBlueprint.Name;
                Type skillType = ModManager.ResolveType(skillNamespace);

                if (skillType == null)
                {
                    MetricsManager.LogError("Unknown skill " + skillNamespace);
                    return any;
                }
                if ((sourceSkillBlueprint.Reflector?.GetNewInstance() ?? Activator.CreateInstance(skillType)) is not BaseSkill baseSkill)
                {
                    MetricsManager.LogError("Skill " + skillNamespace + " is not a " + nameof(BaseSkill));
                    continue;
                }
                if (Exclude != null && Exclude(baseSkill))
                {
                    continue;
                }
                sourceSkillBlueprint.InitializePartInstance(baseSkill);
                if (sourceSkillBlueprint.TryGetParameter("Builder", out string skillBuilderName)
                    && ModManager.ResolveType("XRL.World.PartBuilders." + skillBuilderName) is Type skillBuilderType
                    && Activator.CreateInstance(skillBuilderType) is IPartBuilder skillBuilder)
                {
                    skillBuilder.BuildPart(baseSkill, Context: "Initialization");
                }
                any = FrankenSkills.AddSkill(baseSkill) || any;
            }
            return any;
        }

        public static bool ImplantCyberneticsFromAttachedParts(GameObject FrankenCorpse)
        {
            bool anyImplanted = false;
            if (FrankenCorpse.Body is Body frankenBody)
            {
                if (FrankenCorpse.TryGetPart(out CyberneticsHasRandomImplants sourceRandomImplants))
                {
                    if (sourceRandomImplants.ImplantChance.RollCached().in100())
                    {
                        int attempts = 0;
                        int maxAttempts = 30;
                        int atLeastLicensePoints = sourceRandomImplants.LicensesAtLeast.RollCached();
                        int availableLicensePoints = FrankenCorpse.GetIntProperty("CyberneticsLicenses");
                        int spentLicensePoints = 0;
                        string implantTable = sourceRandomImplants.ImplantTable;
                        if (availableLicensePoints < atLeastLicensePoints)
                        {
                            FrankenCorpse.SetIntProperty("CyberneticsLicenses", atLeastLicensePoints);
                        }
                        else
                        {
                            atLeastLicensePoints = availableLicensePoints;
                        }
                        while (++attempts <= maxAttempts && spentLicensePoints < atLeastLicensePoints)
                        {
                            string possibleImplantBlueprintName = PopulationManager.RollOneFrom(implantTable).Blueprint;
                            if (possibleImplantBlueprintName == null)
                            {
                                MetricsManager.LogError("got null blueprint from " + sourceRandomImplants.ImplantTable);
                                continue;
                            }
                            if (!GameObjectFactory.Factory.Blueprints.TryGetValue(possibleImplantBlueprintName, out var possibleImplantBlueprint))
                            {
                                MetricsManager.LogError("got invalid blueprint \"" + possibleImplantBlueprintName + "\" from " + implantTable);
                                continue;
                            }
                            if (!possibleImplantBlueprint.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string slotTypes))
                            {
                                MetricsManager.LogError("Weird blueprint in random cybernetics table: " + possibleImplantBlueprintName + " from table " + implantTable);
                                continue;
                            }

                            List<string> slotTypesList = new(slotTypes?.Split(','));
                            slotTypesList.ShuffleInPlace();

                            if (GameObject.Create(possibleImplantBlueprintName) is GameObject cyberneticObject)
                            {
                                if (!cyberneticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                                {
                                    cyberneticObject?.Obliterate();
                                }
                                else
                                {
                                    foreach (string requiredType in slotTypesList)
                                    {
                                        List<BodyPart> bodyPartsList = frankenBody.GetPart(requiredType);
                                        bodyPartsList.ShuffleInPlace();
                                        foreach (BodyPart implantTargetBodyPart in bodyPartsList)
                                        {
                                            if (implantTargetBodyPart == null || implantTargetBodyPart._Cybernetics != null)
                                            {
                                                continue;
                                            }
                                            if (atLeastLicensePoints - spentLicensePoints >= cyberneticsBaseItem.Cost)
                                            {
                                                spentLicensePoints += cyberneticsBaseItem.Cost;
                                                implantTargetBodyPart.Implant(cyberneticObject);
                                                anyImplanted = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (frankenBody.FindCybernetics(cyberneticObject) is not BodyPart implantedLimb)
                                {
                                    cyberneticObject?.Obliterate();
                                }
                            }
                        }
                    }
                }

                if (FrankenCorpse.TryGetPart(out CyberneticsHasImplants sourceImplants))
                {
                    List<string> implantsAtLocationList = new(sourceImplants.Implants.Split(','));

                    foreach (string implantBlueprintLocation in implantsAtLocationList)
                    {
                        string[] implantAtLocation = implantBlueprintLocation.Split('@');
                        if (GameObject.Create(implantAtLocation[0]) is GameObject implantObject)
                        {
                            if (frankenBody.GetPartByNameWithoutCybernetics(implantAtLocation[1]) is BodyPart bodyPartWithoutImplant)
                            {
                                bodyPartWithoutImplant.Implant(implantObject);
                                anyImplanted = true;
                            }
                            else
                            {
                                implantObject?.Obliterate();
                            }
                        }
                    }
                }
            }
            return anyImplanted;
        }

        public static bool ProcessMoveToDeathCell(GameObject corpse, UD_FleshGolems_PastLife PastLife)
        {
            return PastLife == null
                || (corpse != null
                    && PastLife?.DeathAddress is (string deathZone, Location2D deathLocation)
                    && deathZone == corpse.CurrentZone?.ZoneID
                    && deathLocation != corpse.CurrentCell.Location
                    && corpse.Physics is Physics corpsePhysics
                    && corpsePhysics.ProcessTargetedMove(
                        TargetCell: corpse.CurrentZone.GetCell(deathLocation),
                        Type: "DirectMove",
                        PreEvent: "BeforeDirectMove",
                        PostEvent: "AfterDirectMove",
                        EnergyCost: 0,
                        System: true,
                        IgnoreCombat: true,
                        IgnoreGravity: true));
        }

        public static IEnumerable<GameObjectBlueprint> GetRaggedNaturalWeapons(Predicate<GameObjectBlueprint> Filter = null)
        {
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetBlueprintsInheritingFrom("UD_FleshGolems Ragged Weapon"))
            {
                if (bp.IsBaseBlueprint() || (Filter != null && !Filter(bp)))
                {
                    continue;
                }
                yield return bp;
            }
        }
        public static bool MeleeWeaponSlotAndSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint, MeleeWeapon)
                && MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint, MeleeWeapon);
        }
        public static bool MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, string Slot)
        {
            if (!GameObjectBlueprint.TryGetPartParameter(nameof(Parts.MeleeWeapon), nameof(Parts.MeleeWeapon.Slot), out string blueprintMeleeWeaponSlot)
                || Slot != blueprintMeleeWeaponSlot)
            {
                return false;
            }
            return true;
        }
        public static bool MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSlotMatchesBlueprint(GameObjectBlueprint, MeleeWeapon.Slot);
        }
        public static bool MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, string Skill)
        {
            if (!GameObjectBlueprint.TryGetPartParameter(nameof(Parts.MeleeWeapon), nameof(Parts.MeleeWeapon.Skill), out string blueprintMeleeWeaponSkill)
                || Skill != blueprintMeleeWeaponSkill)
            {
                return false;
            }
            return true;
        }
        public static bool MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint GameObjectBlueprint, MeleeWeapon MeleeWeapon)
        {
            return MeleeWeaponSkillMatchesBlueprint(GameObjectBlueprint, MeleeWeapon.Skill);
        }

        public static bool MakeItALIVE(
            GameObject Corpse,
            UD_FleshGolems_PastLife PastLife,
            ref string CreatureName,
            ref string SourceBlueprint,
            ref string CorpseDescription,
            GameObject SourceObject = null)
        {
            // UnityEngine.Debug.Log("    " + nameof(MakeItALIVE) + ", " + nameof(Corpse) + ": " + Corpse?.DebugName ?? "null");
            if (Corpse is GameObject frankenCorpse)
            {
                bool wasPlayer = PastLife != null && PastLife.WasPlayer;

                string corpseType = frankenCorpse.Blueprint.Replace(" Corpse", "").Replace("UD_FleshGolems ", "");
                frankenCorpse.SetIntProperty("NoAnimatedNamePrefix", 1);
                frankenCorpse.SetIntProperty("Bleeds", 1);
                frankenCorpse.SetStringProperty("Species", corpseType);

                frankenCorpse.Render.RenderLayer = 10;

                if (frankenCorpse.GetPropertyOrTag(nameof(CyberneticsButcherableCybernetic)) is string butcherableCyberneticsProp
                    && butcherableCyberneticsProp != null)
                {
                    UD_FleshGolems_HasCyberneticsButcherableCybernetic.EmbedButcherableCyberneticsList(frankenCorpse, butcherableCyberneticsProp);
                }

                string convoID = frankenCorpse.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG);
                if (frankenCorpse.TryGetPart(out ConversationScript convo)
                    && (convo.ConversationID == "NewlySentientBeings" || !convoID.IsNullOrEmpty()))
                {
                    convoID ??= "UD_FleshGolems NewlyReanimatedBeings";
                    convo.ConversationID = convoID;
                }

                Epithets frankenEpithets = null;
                if (frankenCorpse.GetPropertyOrTag(REANIMATED_EPITHETS_TAG) is string frankenEpithetsString)
                {
                    frankenEpithets = frankenCorpse.RequirePart<Epithets>();
                    frankenEpithets.Primary = frankenEpithetsString;
                }

                Description frankenDescription = frankenCorpse.RequirePart<Description>();
                if (frankenDescription != null)
                {
                    List<string> poeticFeatures = new()
                    {
                        "viscera",
                        "muck",
                    };
                    if (frankenCorpse?.GetxTag("TextFragments", "PoeticFeatures") is string poeticFeaturesXTag)
                    {
                        poeticFeatures = new(poeticFeaturesXTag.Split(','));
                    }
                    string firstPoeticFeature = poeticFeatures.GetRandomElement() ?? "Viscera";
                    poeticFeatures.Remove(firstPoeticFeature);
                    string secondPoeticFeature = poeticFeatures.GetRandomElement() ?? "muck";
                    poeticFeatures.Remove(secondPoeticFeature);

                    string poeticVerb = frankenCorpse?.GetxTag("TextFragments", "PoeticVerbs")?.Split(',')?.GetRandomElement() ?? "squirming";

                    string poeticAdjective = frankenCorpse?.GetxTag("TextFragments", "PoeticAdjectives")?.Split(',')?.GetRandomElement() ?? "wet";

                    List<string> poeticNoises = new()
                    {
                        "gurgles",
                        "slurps",
                    };
                    if (frankenCorpse?.GetxTag("TextFragments", "PoeticnNoises") is string poeticNoisesXTag)
                    {
                        poeticFeatures = new(poeticNoisesXTag.Split(','));
                    }
                    string firstPoeticNoise = poeticNoises.GetRandomElement();
                    poeticNoises.Remove(firstPoeticNoise);
                    string secondPoeticNoise = poeticNoises.GetRandomElement();
                    poeticFeatures.Remove(secondPoeticNoise);

                    CorpseDescription = frankenDescription._Short;
                    frankenDescription._Short =
                        ("*FirstFeature* and *secondFeature* are brought *verbing* back into a horrific facsimile of life. " +
                        "*Adjective* *firstNoise* and *secondNoise* escape =subject.possessive= every movement and twist the " +
                        "gut of anyone unfortunate enough to hear it.")
                            .Replace("*FirstFeature*", firstPoeticFeature.Capitalize())
                            .Replace("*secondFeature*", secondPoeticFeature)
                            .Replace("*verbing*", poeticVerb)
                            .Replace("*Adjective*", poeticAdjective.Capitalize())
                            .Replace("*firstNoise*", firstPoeticNoise)
                            .Replace("*secondNoise*", secondPoeticNoise);
                }
                string sourceBlueprintName = FigureOutWhatBlueprintThisCorpseCameFrom(frankenCorpse, PastLife);
                
                SourceBlueprint ??= sourceBlueprintName;

                Corpse frankenCorpseCorpse = frankenCorpse.RequirePart<Corpse>();
                if (frankenCorpseCorpse != null)
                {
                    frankenCorpseCorpse.CorpseBlueprint = frankenCorpse.Blueprint;
                    frankenCorpseCorpse.CorpseChance = 100;
                }

                string frankenGenotype = frankenCorpse?.GetPropertyOrTag("FromGenotype");
                if (frankenGenotype != null)
                {
                    frankenCorpse.SetStringProperty("Genotype", frankenGenotype);
                }
                bool installedCybernetics = false;
                string cyberneticsLicenses = "CyberneticsLicenses";
                string cyberneticsLicensesFree = "FreeCyberneticsLicenses";
                if (frankenCorpse.Body is Body frankenBody)
                {
                    if (frankenCorpse.TryGetPart(out CyberneticsButcherableCybernetic butcherableCybernetics))
                    {
                        int startingLicenses = Stat.RollCached("2d2-1");

                        frankenCorpse.SetIntProperty(cyberneticsLicenses, startingLicenses);
                        frankenCorpse.SetIntProperty(cyberneticsLicensesFree, startingLicenses);

                        List<GameObject> butcherableCyberneticsList = Event.NewGameObjectList(butcherableCybernetics.Cybernetics);
                        foreach (GameObject butcherableCybernetic in butcherableCyberneticsList)
                        {
                            if (butcherableCybernetic.TryGetPart(out CyberneticsBaseItem butcherableCyberneticBasePart)
                                && butcherableCyberneticBasePart.Slots is string slotsString)
                            {
                                int cyberneticsCost = butcherableCyberneticBasePart.Cost;
                                frankenCorpse.ModIntProperty(cyberneticsLicenses, cyberneticsCost);
                                frankenCorpse.ModIntProperty(cyberneticsLicensesFree, cyberneticsCost);

                                List<string> slotsList = slotsString.CachedCommaExpansion();
                                slotsList.ShuffleInPlace();
                                foreach (string slot in slotsList)
                                {
                                    List<BodyPart> bodyParts = frankenBody.GetPart(slot);
                                    bodyParts.ShuffleInPlace();

                                    foreach (BodyPart bodyPart in bodyParts)
                                    {
                                        if (bodyPart.CanReceiveCyberneticImplant()
                                            && !bodyPart.HasInstalledCybernetics())
                                        {
                                            bodyPart.Implant(butcherableCybernetic);
                                            break;
                                        }
                                    }
                                    butcherableCybernetics.Cybernetics.Remove(butcherableCybernetic);
                                }
                            }
                        }
                        frankenCorpse.RemovePart(butcherableCybernetics);
                        installedCybernetics = true;
                    }
                    else
                    if (PastLife != null && !PastLife.InstalledCybernetics.IsNullOrEmpty())
                    {
                        foreach ((string cyberneticID, string bodyPartType) in PastLife.InstalledCybernetics)
                        {
                            if (GameObject.FindByID(cyberneticID) is GameObject pastCyberneticObject
                                && pastCyberneticObject.TryRemoveFromContext())
                            {
                                if (pastCyberneticObject.TryGetPart(out CyberneticsBaseItem butcherableCyberneticBasePart))
                                {
                                    int cyberneticsCost = butcherableCyberneticBasePart.Cost;
                                    frankenCorpse.ModIntProperty(cyberneticsLicenses, cyberneticsCost);
                                    frankenCorpse.ModIntProperty(cyberneticsLicensesFree, cyberneticsCost);

                                    List<BodyPart> bodyParts = frankenBody.GetPart(bodyPartType);
                                    bodyParts.ShuffleInPlace();

                                    foreach (BodyPart bodyPart in bodyParts)
                                    {
                                        if (bodyPart.CanReceiveCyberneticImplant()
                                            && !bodyPart.HasInstalledCybernetics())
                                        {
                                            bodyPart.Implant(pastCyberneticObject);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        installedCybernetics = true;
                    }
                }
                
                Mutations frankenMutations = frankenCorpse.RequirePart<Mutations>();
                Skills frankenSkills = frankenCorpse.RequirePart<Skills>();

                if (wasPlayer)
                {
                    if (!frankenCorpse.HasSkill(nameof(Survival_Camp)))
                    {
                        frankenSkills.AddSkill(nameof(Survival_Camp));
                    }
                    if (!frankenCorpse.HasSkill(nameof(Tactics_Run)))
                    {
                        frankenSkills.AddSkill(nameof(Tactics_Run));
                    }
                }

                if (frankenCorpse.GetBlueprint().Tags.ContainsKey(nameof(Gender)))
                {
                    frankenCorpse.SetGender(frankenCorpse.GetBlueprint().Tags[nameof(Gender)]);
                }
                if (frankenCorpse.GetBlueprint().Tags.ContainsKey(nameof(PronounSet)))
                {
                    frankenCorpse.SetPronounSet(frankenCorpse.GetBlueprint().Tags[nameof(PronounSet)]);
                }

                bool excludedFromDynamicEncounters = false;
                if (GameObjectFactory.Factory.GetBlueprintIfExists(sourceBlueprintName) is GameObjectBlueprint sourceBlueprint)
                {
                    bool isProblemPartOrFollowerPartOrPartAlreadyHave(IPart p)
                    {
                        return IPartsToSkipWhenReanimating.Contains(p.Name)
                            || frankenCorpse.HasPart(p.Name)
                            || (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_Reanimated_PartExclusions") is string propertyPartExclusions
                                && propertyPartExclusions.CachedCommaExpansion() is List<string> partExclusionsList
                                && partExclusionsList.Contains(p.Name));
                    }
                    AssignStatsFromBlueprint(frankenCorpse, sourceBlueprint);

                    float physicalAdjustmentFactor = 1.2f; // wasPlayer ? 1.0f : 1.2f;
                    float mentalAdjustmentFactor = 0.75f; // wasPlayer ? 1.0f : 0.75f;
                    AssignStatsFromPastLifeWithFactor(frankenCorpse, PastLife, PhysicalAdjustmentFactor: physicalAdjustmentFactor, MentalAdjustmentFactor: mentalAdjustmentFactor);
                    if (frankenCorpse.GetStat("Hitpoints") is Statistic frankenHitpoints)
                    {
                        int minHitpoints = Stat.RollCached("4d3+5");
                        // UnityEngine.Debug.Log("        " + nameof(frankenHitpoints) + " " + nameof(minHitpoints) + ": " + minHitpoints);
                        frankenHitpoints.BaseValue = Math.Max(minHitpoints, frankenHitpoints.BaseValue);
                        frankenHitpoints.Penalty = 0;
                        // UnityEngine.Debug.Log("        " + nameof(frankenHitpoints) + ": " + frankenHitpoints.Value + "/" + frankenHitpoints.BaseValue);
                    }

                    AssignPartsFromBlueprint(frankenCorpse, sourceBlueprint, Exclude: isProblemPartOrFollowerPartOrPartAlreadyHave);

                    AssignMutationsFromBlueprint(frankenMutations, sourceBlueprint);
                    AssignMutationsFromPastLife(frankenMutations, PastLife);

                    AssignSkillsFromBlueprint(frankenSkills, sourceBlueprint);
                    AssignSkillsFromPastLife(frankenSkills, PastLife);

                    excludedFromDynamicEncounters = PastLife != null && PastLife.Tags.ContainsKey("ExcludeFromDynamicEncounters");

                    frankenBody = frankenCorpse.Body;
                    if (frankenBody != null)
                    {
                        if (!installedCybernetics)
                        {
                            ImplantCyberneticsFromAttachedParts(frankenCorpse);
                        }
                    }

                    if (frankenCorpse.TryGetPart(out Leveler frankenLeveler))
                    {
                        if (int.TryParse(frankenCorpse.GetPropertyOrTag("UD_FleshGolems_SkipLevelsOnReanimate", "0"), out int SkipLevelsOnReanimate)
                            && SkipLevelsOnReanimate < 1)
                        {
                            frankenLeveler?.LevelUp();
                            if (Stat.RollCached("1d2") == 1)
                            {
                                frankenLeveler?.LevelUp();
                            }
                        }
                        int floorXP = Leveler.GetXPForLevel(frankenCorpse.Level);
                        int ceilingXP = Leveler.GetXPForLevel(frankenCorpse.Level + 1);
                        frankenCorpse.GetStat("XP").BaseValue = Stat.RandomCosmetic(floorXP, ceilingXP);
                    }

                    if (sourceBlueprint.Tags.ContainsKey("Species"))
                    {
                        frankenCorpse.SetStringProperty("Species", sourceBlueprint.Tags["Species"]);
                    }
                    Brain frankenBrain = frankenCorpse.Brain;
                    if (frankenBrain != null
                        && PastLife?.Brain is Brain pastBrain)
                    {
                        if (sourceBlueprint.TryGetPartParameter(nameof(Brain), nameof(Brain.Wanders), out bool sourceBrainWanders))
                        {
                            frankenBrain.Wanders = sourceBrainWanders;
                        }
                        frankenCorpse.Brain.Allegiance ??= new();
                        frankenBrain.Allegiance.Hostile = pastBrain.Allegiance.Hostile;
                        frankenBrain.Allegiance.Calm = pastBrain.Allegiance.Calm;
                        if ((!UD_FleshGolems_Reanimated.HasWorldGenerated || excludedFromDynamicEncounters))
                        {
                            frankenCorpse.Brain.Allegiance.Clear();
                            frankenCorpse.Brain.Allegiance.Add("Newly Sentient Beings", 75);
                            foreach ((string faction, int rep) in pastBrain.Allegiance)
                            {
                                if (!pastBrain.Allegiance.ContainsKey(faction))
                                {
                                    frankenCorpse.Brain.Allegiance.Add(faction, rep);
                                }
                                else
                                {
                                    frankenCorpse.Brain.Allegiance[faction] += rep;
                                }
                            }
                            if (!frankenCorpse.HasPropertyOrTag("StartingPet") && !frankenCorpse.HasPropertyOrTag("Pet"))
                            {
                                frankenCorpse.Brain.PartyLeader = pastBrain.PartyLeader;
                                frankenCorpse.Brain.PartyMembers = pastBrain.PartyMembers;

                                frankenCorpse.Brain.Opinions = pastBrain.Opinions;

                            }
                        }
                        frankenBrain.Wanders = pastBrain.Wanders;
                        frankenBrain.WallWalker = pastBrain.WallWalker;
                        frankenBrain.HostileWalkRadius = pastBrain.HostileWalkRadius;

                        frankenBrain.Mobile = pastBrain.Mobile;
                    }

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_CONVO_ID_TAG) is string sourceCreatureConvoID
                        && convo != null)
                    {
                        convo.ConversationID = sourceCreatureConvoID;
                    }

                    if (sourceBlueprint.GetPropertyOrTag(REANIMATED_EPITHETS_TAG) is string sourceCreatureEpithets)
                    {
                        frankenEpithets = frankenCorpse.RequirePart<Epithets>();
                        frankenEpithets.Primary = sourceCreatureEpithets;
                    }

                    string corpsePartName = nameof(Parts.Corpse);
                    string corpsepartBlueprintName = nameof(Parts.Corpse.CorpseBlueprint);
                    if (frankenCorpseCorpse != null
                        && sourceBlueprint.TryGetPartParameter(corpsePartName, corpsepartBlueprintName, out string frankenCorpseCorpseBlueprint))
                    {
                        frankenCorpseCorpse.CorpseBlueprint = frankenCorpseCorpseBlueprint;
                    }

                    if (sourceBlueprint.DisplayName() is string sourceBlueprintDisplayName)
                    {
                        string sourceCreatureName = null;
                        bool wereProperlyNamed = false;
                        if (GameObject.CreateSample(sourceBlueprint.Name) is GameObject sampleSourceObject)
                        {
                            wereProperlyNamed = sampleSourceObject.HasProperName;
                            if (wereProperlyNamed)
                            {
                                sourceCreatureName = sampleSourceObject.GetReferenceDisplayName(Short: true);
                            }
                            sampleSourceObject?.Obliterate();
                        }
                        if (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_CreatureProperName") is string frankenCorpseProperName)
                        {
                            wereProperlyNamed = true;
                            sourceCreatureName = frankenCorpseProperName;
                        }
                        sourceCreatureName ??= frankenCorpse.GetPropertyOrTag("CreatureName");

                        if (frankenDescription != null
                            && sourceBlueprint.TryGetPartParameter(nameof(Description), nameof(Description.Short), out string sourceDescription))
                        {
                            if (frankenCorpse.GetPropertyOrTag("UD_FleshGolems_CorpseDescription") is string sourceCorpseDescription)
                            {
                                sourceDescription = sourceCorpseDescription;
                            }
                            if (PastLife != null && !PastLife.Description.IsNullOrEmpty())
                            {
                                sourceDescription = PastLife.Description;
                            }
                            if (sourceBlueprintDisplayName.Contains("[") || sourceBlueprintDisplayName.Contains("]"))
                            {
                                wereProperlyNamed = true;
                            }
                            string whoTheyWere = wereProperlyNamed ? sourceCreatureName : sourceBlueprintDisplayName;
                            if (whoTheyWere.ToLower().EndsWith(" corpse") || whoTheyWere.ToLower().StartsWith("corpse of "))
                            {
                                whoTheyWere = UD_FleshGolems_ReanimatedCorpse.REANIMATED_ADJECTIVE + " " + whoTheyWere;
                            }
                            if (whoTheyWere.Contains("[") || whoTheyWere.Contains("]") || frankenCorpse.GetStringProperty("SourceID") == "1")
                            {
                                whoTheyWere = frankenGenotype;
                            }
                            if (!wereProperlyNamed)
                            {
                                whoTheyWere = Grammar.A(whoTheyWere);
                            }

                            frankenDescription._Short += "\n\n" + "In life, this mess was " + (wasPlayer ? "you." : (whoTheyWere + ":\n" + sourceDescription));
                        }
                        if (!sourceCreatureName.IsNullOrEmpty())
                        {
                            frankenCorpse.Render.DisplayName = "corpse of " + sourceCreatureName;

                            frankenCorpse.GiveProperName(sourceCreatureName);
                            frankenCorpse.RequirePart<Honorifics>().Primary = "corpse of";
                        }
                        else
                        {
                            frankenCorpse.Render.DisplayName = sourceBlueprintDisplayName + " corpse";
                        }
                    }

                    string BleedLiquid = null;
                    if (sourceBlueprint.Tags.ContainsKey(nameof(BleedLiquid)))
                    {
                        BleedLiquid = sourceBlueprint.Tags[nameof(BleedLiquid)];
                    }
                    if (BleedLiquid.IsNullOrEmpty())
                    {
                        BleedLiquid = "blood-1000";
                    }
                    frankenCorpse.GetPropertyOrTag("BleedLiquid", BleedLiquid);

                    if (frankenCorpse.GetPropertyOrTag("KillerID") is string killerID
                        && GameObject.FindByID(killerID) is GameObject killer)
                    {
                        frankenCorpse.GetPropertyOrTag("KillerName", killer.GetReferenceDisplayName(Short: true));
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Physics), nameof(Physics.Weight), out int sourceWeight))
                    {
                        frankenCorpse.Physics.Weight = sourceWeight;
                        frankenCorpse.FlushWeightCaches();
                    }

                    if (sourceBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string sourceAnatomy))
                    {
                        if (frankenCorpse.Body == null)
                        {
                            frankenCorpse.AddPart(new Body()).Anatomy = sourceAnatomy;
                        }
                        else
                        {
                            frankenCorpse.Body.Rebuild(sourceAnatomy);
                        }
                    }

                    if (GolemBodySelection.GetBodyBlueprintFor(sourceBlueprint) is GameObjectBlueprint golemBodyBlueprint)
                    {
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string golemAnatomy))
                        {
                            if (frankenCorpse.Body == null)
                            {
                                frankenCorpse.AddPart(new Body()).Anatomy = golemAnatomy;
                            }
                            else
                            {
                                frankenCorpse.Body.Rebuild(golemAnatomy);
                            }
                        }
                        if (golemBodyBlueprint.TryGetPartParameter(nameof(Parts.Render), nameof(Parts.Render.Tile), out string golemTile))
                        {
                            frankenCorpse.Render.Tile = golemTile;
                        }

                        bool giganticIfNotAlready(BaseMutation BM)
                        {
                            return !frankenMutations.HasMutation(BM)
                                && BM.GetMutationClass() == "GigantismPlus";
                        }
                        // AssignStatsFromBlueprint(frankenCorpse, golemBodyBlueprint);
                        AssignMutationsFromBlueprint(frankenMutations, golemBodyBlueprint, Exclude: giganticIfNotAlready);
                        AssignSkillsFromBlueprint(frankenSkills, golemBodyBlueprint);
                    }
                    bool wantDoRequip = false;
                    if (sourceBlueprint.Inventory != null)
                    {
                        foreach (InventoryObject inventoryObject in sourceBlueprint.Inventory)
                        {
                            if (GameObjectFactory.Factory.GetBlueprintIfExists(inventoryObject.Blueprint) is GameObjectBlueprint inventoryObjectBlueprint
                                && inventoryObjectBlueprint.IsNatural())
                            {
                                if (GameObject.CreateSample(inventoryObjectBlueprint.Name) is GameObject sampleNaturalGear
                                    && sampleNaturalGear.EquipAsDefaultBehavior())
                                {
                                    if (sampleNaturalGear.TryGetPart(out MeleeWeapon mw)
                                        && !mw.IsImprovisedWeapon()
                                        && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotAndSkillMatchesBlueprint(bp, mw))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                                        && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject
                                        && frankenCorpse.ReceiveObject(raggedWeaponObject))
                                    {
                                        wantDoRequip = true;
                                    }
                                    sampleNaturalGear?.Obliterate();
                                }
                            }
                        }
                    }
                    foreach (BodyPart frankenLimb in frankenBody.LoopParts())
                    {
                        if (frankenLimb.DefaultBehavior is GameObject frankenNaturalGear
                            && frankenNaturalGear.GetBlueprint() is GameObjectBlueprint frankenNaturalGearBlueprint
                            && !frankenNaturalGearBlueprint.InheritsFrom("UD_FleshGolems Ragged Weapon"))
                        {
                            if (frankenNaturalGear.TryGetPart(out MeleeWeapon mw)
                                && !mw.IsImprovisedWeapon()
                                && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotAndSkillMatchesBlueprint(bp, mw))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                                && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject)
                            {
                                frankenNaturalGear.Obliterate();
                                frankenLimb.DefaultBehavior = raggedWeaponObject;
                                frankenLimb.DefaultBehaviorBlueprint = raggedWeaponBlueprintName;
                            }
                        }
                        else
                        if (20.in100()
                            && GetRaggedNaturalWeapons(bp => MeleeWeaponSlotMatchesBlueprint(bp, frankenLimb.Type))?.GetRandomElement()?.Name is string raggedWeaponBlueprintName
                            && GameObject.CreateUnmodified(raggedWeaponBlueprintName) is GameObject raggedWeaponObject)
                        {
                            frankenLimb.DefaultBehavior = raggedWeaponObject;
                            frankenLimb.DefaultBehaviorBlueprint = raggedWeaponBlueprintName;
                        }
                    }
                    if (wantDoRequip)
                    {
                        frankenBrain?.WantToReequip();
                    }
                    if (frankenMutations != null)
                    {
                        bool giveRegen = true;
                        if (giveRegen
                            && MutationFactory.GetMutationEntryByName("Regeneration").Class is string regenerationMutationClass)
                        {
                            if (frankenMutations.GetMutation(regenerationMutationClass) is not BaseMutation regenerationMutation)
                            {
                                frankenMutations.AddMutation(regenerationMutationClass, Level: 10);
                                regenerationMutation = frankenMutations.GetMutation(regenerationMutationClass);
                            }
                            regenerationMutation.CapOverride = 5;

                            if (regenerationMutation.Level < 5)
                            {
                                regenerationMutation.ChangeLevel(5);
                            }
                        }
                        string nightVisionMutaitonName = "Night Vision";
                        string darkVisionMutationName = "Dark Vision";
                        MutationEntry nightVisionEntry = MutationFactory.GetMutationEntryByName(nightVisionMutaitonName);
                        MutationEntry darkVisionEntry = MutationFactory.GetMutationEntryByName(darkVisionMutationName);
                        if (!frankenMutations.HasMutation(nightVisionEntry.Class) && !frankenMutations.HasMutation(darkVisionEntry.Class))
                        {
                            if (darkVisionEntry.Instance is BaseMutation darkVisionMutation)
                            {
                                if (darkVisionMutation.CapOverride == -1)
                                {
                                    darkVisionMutation.CapOverride = 8;
                                }
                                frankenMutations.AddMutation(darkVisionMutation, 8);
                            }
                        }
                    }
                }

                if (!UD_FleshGolems_Reanimated.HasWorldGenerated || excludedFromDynamicEncounters)
                {
                    if (PastLife?.ConversationScriptID is string pastConversationID)
                    {
                        convo.ConversationID = pastConversationID;
                    }
                }

                if (!frankenCorpse.IsPlayer() && frankenCorpse?.CurrentCell is Cell frankenCell)
                {
                    bool isItemThatNotSelf(GameObject GO)
                    {
                        return GO.GetBlueprint().InheritsFrom("Item")
                            && GO != frankenCorpse;
                    }
                    frankenCorpse.TakeObject(Event.NewGameObjectList(frankenCell.GetObjects(isItemThatNotSelf)));
                    frankenCorpse.Brain?.WantToReequip();

                    if (frankenCorpse.TryGetPart(out GenericInventoryRestocker frankenRestocker))
                    {
                        frankenRestocker.PerformRestock();
                        frankenRestocker.LastRestockTick = The.Game.TimeTicks;
                    }
                }

                if (frankenCorpse != null)
                {
                    var reanimatedCorpse = frankenCorpse.RequirePart<UD_FleshGolems_ReanimatedCorpse>();
                    reanimatedCorpse.SourceObject = SourceObject;
                }
                return true;
            }
            return false;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            base.Register(Object, Registrar);
            try
            {
                Registrar?.Register(DroppedEvent.ID, EventOrder.EXTREMELY_EARLY);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(Register), x, "game_mod_exception");
            }
            finally
            {
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(DroppedEvent.ID))
                {
                    FailedToRegisterEvents.Add(DroppedEvent.ID);
                }
            }
        }
        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || (FailedToRegisterEvents.Contains(DroppedEvent.ID) && ID == DroppedEvent.ID)
                || ID == AnimateEvent.ID
                || ID == EnvironmentalUpdateEvent.ID;
        }
        public override bool HandleEvent(AnimateEvent E)
        {
            /*
            UnityEngine.Debug.Log(
                nameof(UD_FleshGolems_CorpseReanimationHelper) + "." + nameof(AnimateEvent) + ", " +
                nameof(IsALIVE) + ": " + IsALIVE);
            */
            if (!IsALIVE
                && ParentObject == E.Object)
            {
                if (E.Object.GetPropertyOrTag("UD_FleshGolems_PastLife_Blueprint") is string pastLifeBlueprint
                    && GameObject.CreateSample(pastLifeBlueprint) is GameObject samplePastLife)
                {
                    E.Object.RequirePart<UD_FleshGolems_PastLife>().Initialize(samplePastLife);
                    samplePastLife?.Obliterate();
                }
                IsALIVE = true;
                MakeItALIVE(E.Object, PastLife, ref CreatureName, ref SourceBlueprint, ref CorpseDescription, E.Actor);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject is GameObject corpse
                && Animate())
            {
                ProcessMoveToDeathCell(corpse, PastLife);
                return true;
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DroppedEvent E)
        {
            if (E.Item is GameObject corpse
                && corpse == ParentObject
                && E.Actor is GameObject dying
                && dying.IsDying)
            {
                corpse.RequirePart<UD_FleshGolems_PastLife>().Initialize(dying);
            }
            if (AlwaysAnimate
                && !IsALIVE
                && ParentObject != null
                && Animate())
            {
                return true;
            }
            return base.HandleEvent(E);
        }
    }
}
