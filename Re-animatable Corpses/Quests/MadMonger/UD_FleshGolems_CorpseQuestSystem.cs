using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Qud.API;

using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Quests;
using XRL.Language;
using XRL.Wish;

using CorpseItem = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem;
using CorpseTaxonomy = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem.CorpseTaxonomy;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

namespace XRL.World.QuestManagers
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_CorpseQuestSystem : IQuestSystem
    {
        public static int RequiredToComplete => 3;

        public static string QuestName = "Find 3 Questionable Materials (for Science)";

        public static string QuestGiverBlueprint = "UD_FleshGolems Mad Monger";

        public static List<string> SpeciesExclusions => new()
        {
            "robot",
            "species",
            "baetyl",
            "mecha",
            "mech",
            "infrastructure",
            "slime",
            "humor",
            "clam",
            "living flesh",
            "*",
            "[",
            "]",
        };

        public static List<string> FactionExclusions => new()
        {
            "robots",
            "slimes",
            "oozes",
            "baetyl",
        };

        public static List<string> AllBaseCorpses => new(GetAllBaseCorpses());
        public static List<string> AllSpecies => new(GetAllSpecies(SpeciesIsNotExcluded));
        public static List<string> AllFactions => new(GetFactionsWithLivingMembersWhoDropCorpses());

        public UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem;

        public Quest ParentQuest;

        public string MyQuestID;

        public QuestStep ParentQuestStep => (ParentQuest == null || ParentQuest.StepsByID.IsNullOrEmpty()) ? null : ParentQuest.StepsByID["UD_FleshGolems I'm dead serious!"];

        public List<UD_FleshGolems_CorpseQuestStep> Steps;

        public int CompletedSteps => (from step in Steps where step.Finished select step).Count();

        public bool Completable// => CompletedSteps >= RequiredToComplete;
        {
            get
            {
                Quest?.SetProperty(nameof(CompletedSteps), CompletedSteps);
                Quest?.SetProperty(nameof(Completable), (CompletedSteps >= RequiredToComplete).ToString());
                return Quest != null && Quest.GetProperty(nameof(Completable)).EqualsNoCase("true");
            }
        }

        public string InfluencerRefName => GetInfluencer()?.GetReferenceDisplayName(Short: true);

        [NonSerialized]
        private bool PlayerAdvisedCompletable;

        private GameObject _Influencer;

        public UD_FleshGolems_CorpseQuestSystem()
        {
            ParentSystem = null;
            ParentQuest = null;

            MyQuestID = null;

            Steps = new();

            PlayerAdvisedCompletable = false;

            _Influencer = null;
        }

        public override GameObject GetInfluencer() => _Influencer ??=
            GameObject.FindByBlueprint(QuestGiverBlueprint) 
            ?? GameObject.CreateSample(QuestGiverBlueprint);

        public static bool IsCorpse(GameObject Object)
        {
            return Object != null
                && Object.GetBlueprint().InheritsFrom("Corpse");
        }

        public static bool IsCorpse(UD_FleshGolems_CorpseQuestStep QuestItem, out CorpseItem CorpseItem)
        {
            CorpseItem = QuestItem?.Corpse;
            return QuestItem != null
                && CorpseItem != null;
        }
        public static bool IsCorpse(UD_FleshGolems_CorpseQuestStep QuestItem)
        {
            return IsCorpse(QuestItem, out _);
        }

        public static IEnumerable<string> GetAllSpecies(Predicate<string> Filter = null)
        {
            List<string> speciesList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && !speciesList.Contains(species)
                    && !species.ToLower().StartsWith("base")
                    && (Filter == null || Filter(species)))
                {
                    speciesList.Add(species);
                    yield return species;
                }
            }
        }
        public static bool SpeciesIsNotExcluded(string Species)
        {
            foreach (string exclusion in SpeciesExclusions)
            {
                if (Species.ToLower().Contains(exclusion.ToLower())
                    || Species.ToLower() == exclusion.ToLower())
                {
                    return false;
                }
            }
            return true;
        }
        public static IEnumerable<string> GetAllCorpsesOfSpecies(string Species)
        {
            List<string> corpseBlueprintList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.Tags.ContainsKey("Species")
                    && bp.Tags["Species"] is string species
                    && species == Species
                    && bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBleprint)
                    && GameObjectFactory.Factory.GetBlueprintIfExists(corpseBleprint) is GameObjectBlueprint corpseBP
                    && corpseBP.InheritsFrom("Corpse")
                    && !corpseBlueprintList.Contains(corpseBleprint))
                {
                    corpseBlueprintList.Add(corpseBleprint);
                    yield return corpseBleprint;
                }
            }
        }
        public static bool CheckCorpseSpecies(UD_FleshGolems_CorpseQuestStep QuestItem, GameObject CorpseObject)
        {
            return IsCorpse(QuestItem, out CorpseItem corpseItem)
                && IsCorpse(CorpseObject)
                && corpseItem.IsSpecies
                && GetAllCorpsesOfSpecies(corpseItem.Value).Contains(CorpseObject.Blueprint);
        }

        public static IEnumerable<string> GetAllBaseCorpses(Predicate<GameObjectBlueprint> Filter = null)
        {
            List<string> baseCorpseList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.InheritsFrom("Corpse")
                    && bp.Name != "Corpse"
                    && bp.IsBaseBlueprint()
                    && !bp.Name.Contains("Robot")
                    && !bp.Name.StartsWith("Base")
                    && bp.Name.EndsWith(" Corpse")
                    && (Filter == null || Filter(bp))
                    && !baseCorpseList.Contains(bp.Name))
                {
                    baseCorpseList.Add(bp.Name);
                    yield return bp.Name;
                }
            }
        }
        public static IEnumerable<string> GetAllCorpsesOfBase(string BaseCorpse)
        {
            List<string> baseCorpseList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.BlueprintList)
            {
                if (bp.InheritsFrom(BaseCorpse)
                    && bp.Name != "Corpse"
                    && bp.InheritsFrom("Corpse")
                    && !bp.Name.Contains("Robot")
                    && !bp.Name.StartsWith("Base")
                    && bp.Name.EndsWith(" Corpse")
                    && !baseCorpseList.Contains(bp.Name))
                {
                    baseCorpseList.Add(bp.Name);
                    yield return bp.Name;
                }
            }
        }

        public static bool FactionHasAtLeastOneMember(string Faction)
        {
            return GameObjectFactory.Factory.AnyFactionMembers(Faction);
        }
        public static bool FactionHasAtLeastOneMember(Faction Faction)
        {
            return FactionHasAtLeastOneMember(Faction.Name);
        }
        public static IEnumerable<string> GetAllCorpsesOfFaction(string Faction)
        {
            List<string> corpseBlueprintList = new();
            foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetFactionMembers(Faction, SkipExclude: true, ReadOnly: true))
            {
                if (bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance)
                    && corpseChance > 0
                    && bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseBlueprint), out string corpseBleprint)
                    && bp.InheritsFrom("Corpse")
                    && !corpseBlueprintList.Contains(corpseBleprint))
                {
                    corpseBlueprintList.Add(corpseBleprint);
                    yield return corpseBleprint;
                }
            }
        }
        public static IEnumerable<string> GetFactionsWithLivingMembersWhoDropCorpses()
        {
            foreach (string factionName in Factions.GetFactionNames())
            {
                if (!FactionHasAtLeastOneMember(factionName))
                {
                    continue;
                }
                if (Factions.Get(factionName) is Faction faction
                    && !faction.Visible)
                {
                    continue;
                }
                if (factionName.ToLower().Contains("village"))
                {
                    continue;
                }
                bool anyDroppableCorpse = false;
                foreach (GameObjectBlueprint bp in GameObjectFactory.Factory.GetFactionMembers(factionName, SkipExclude: true, ReadOnly: true))
                {
                    if (bp.TryGetPartParameter(nameof(Corpse), nameof(Corpse.CorpseChance), out int corpseChance)
                        && corpseChance > 0)
                    {
                        anyDroppableCorpse = true;
                        break;
                    }
                }
                if (anyDroppableCorpse && !FactionExclusions.Contains(factionName))
                {
                    yield return factionName;
                }
            }
        }

        public static bool CorpseMatchesAnyQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (!IsCorpse(CorpseObject))
                {
                    continue;
                }
                if (IsCorpse(questStep, out CorpseItem corpseItem)
                    && corpseItem.CorpseCompletesThisStep(CorpseObject))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }
            return false;
        }
        public bool CorpseMatchesAnyQuestStep(GameObject CorpseObject)
        {
            return CorpseMatchesAnyQuestStep(Steps, CorpseObject);
        }

        public static IEnumerable<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(List<UD_FleshGolems_CorpseQuestStep> Steps, GameObject CorpseObject)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (questStep.Corpse.CorpseCompletesThisStep(CorpseObject))
                {
                    yield return questStep;
                }
            }
        }
        public IEnumerable<UD_FleshGolems_CorpseQuestStep> GetQuestStepsMatchingCorpse(GameObject CorpseObject)
        {
            return GetQuestStepsMatchingCorpse(Steps, CorpseObject);
        }
        public int GetCountMatchingCorpsesHeldByPlayer()
        {
            int matches = 0;
            foreach (GameObject item in Player?.GetInventoryAndEquipmentReadonly())
            {
                if (CorpseMatchesAnyQuestStep(item))
                {
                    matches++;
                }
            }
            return matches;
        }
        public static bool HasQuestStepWithCorpseItemValue(List<UD_FleshGolems_CorpseQuestStep> Steps, string CorpseItemValue)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (IsCorpse(questStep, out CorpseItem corpseItem)
                    && corpseItem.Value == CorpseItemValue)
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasQuestStepWithCorpseItemValue(string CorpseItemValue)
        {
            return HasQuestStepWithCorpseItemValue(Steps, CorpseItemValue);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateASpeciesCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string species = AllSpecies.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(species.Capitalize()) + " Species Corpse",
                Corpse = new(CorpseTaxonomy.Species, species),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateASpeciesCorpseQuestStep()
        {
            return CreateASpeciesCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string baseBlueprint = AllBaseCorpses.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(baseBlueprint.Capitalize()),
                Corpse = new(CorpseTaxonomy.Base, baseBlueprint),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateABaseCorpseQuestStep()
        {
            return CreateABaseCorpseQuestStep(Steps, this);
        }
        public static UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep(List<UD_FleshGolems_CorpseQuestStep> Steps, UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            string factionName = AllFactions.GetRandomElementCosmetic(Exclude: s => HasQuestStepWithCorpseItemValue(Steps, s));
            string factionDisplayName = Factions.Get(factionName).DisplayName;
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find " + Grammar.A(Grammar.MakeTitleCase(factionDisplayName)) + " Faction Corpse",
                Corpse = new(CorpseTaxonomy.Faction, factionName),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateAFactionCorpseQuestStep()
        {
            return CreateAFactionCorpseQuestStep(Steps, this);
        }

        public static UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
        {
            UD_FleshGolems_CorpseQuestStep questStep = new(ParentSystem)
            {
                Name = "Find Any Corpse",
                Corpse = new(CorpseTaxonomy.Any, "Any"),
            };
            return questStep?.SetGeneratedStepText();
        }
        public UD_FleshGolems_CorpseQuestStep CreateAnAnyCorpseQuestStep()
        {
            return CreateAnAnyCorpseQuestStep(this);
        }

        static void debugLog(string Field, object Value = null, int Indent = 0)
        {
            string indent = " ".ThisManyTimes(Math.Min(12, Indent) * 4);
            string output = indent + Field;
            if (Value != null &&
                !Value.ToString().IsNullOrEmpty())
            {
                output += ": " + Value;
            }
            // UnityEngine.Debug.Log(output);
        }

        public void Init()
        {
            if (Steps != null && Steps.Count > 0)
            {
                return;
            }
            Steps ??= new();

            int desiredCorpsSteps = 10;
            bool haveAnAnyQuestStep = false;
            debugLog(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(Init));
            while (Steps.Count < desiredCorpsSteps)
            {
                UD_FleshGolems_CorpseQuestStep corpseQuestStep = null;

                int questStepRoll = Stat.Random(1, (haveAnAnyQuestStep ? 12 : 13));
                if (questStepRoll == 13)
                {
                    corpseQuestStep = CreateAnAnyCorpseQuestStep();
                    haveAnAnyQuestStep = true;
                }
                else
                {
                    CorpseTaxonomy questStepTaxonomy = (CorpseTaxonomy)((questStepRoll % 3) + 1);
                    debugLog(nameof(questStepTaxonomy) + " roll " + ((questStepRoll % 3) + 1));
                    corpseQuestStep = (questStepTaxonomy) switch
                    {
                        CorpseTaxonomy.Species => CreateASpeciesCorpseQuestStep(),
                        CorpseTaxonomy.Base => CreateABaseCorpseQuestStep(),
                        CorpseTaxonomy.Faction => CreateAFactionCorpseQuestStep(),
                        _ => CreateAnAnyCorpseQuestStep(),
                    };
                }
                debugLog(corpseQuestStep.Corpse.Taxonomy.ToString(), corpseQuestStep.Name, 1);
                if (!HasStepWithName(corpseQuestStep.Name))
                {
                    Steps.Add(corpseQuestStep);
                }
            }
        }
        private bool HasStepWithName(string s)
        {
            foreach (UD_FleshGolems_CorpseQuestStep questStep in Steps)
            {
                if (questStep.Name == s)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool CheckItem(UD_FleshGolems_CorpseQuestSystem CorpseQuestSystem, GameObject Item, bool Unfinish = false)
        {
            foreach (UD_FleshGolems_CorpseQuestStep finishableQuestStep in CorpseQuestSystem?.GetQuestStepsMatchingCorpse(Item))
            {
                if (!Unfinish)
                {
                    if (finishableQuestStep.FinishStep())
                    {
                        Item.SetIntProperty(nameof(UD_FleshGolems_CorpseQuestSystem), 1);
                        if (CorpseQuestSystem.Completable && !CorpseQuestSystem.PlayerAdvisedCompletable)
                        {
                            Popup.Show(
                                "You have the " + RequiredToComplete + " questionable materials that " +
                                CorpseQuestSystem.InfluencerRefName + " said he needed.");
                            CorpseQuestSystem.PlayerAdvisedCompletable = true;
                        }
                        return true;
                    }
                }
                else
                {
                    if (finishableQuestStep.UnfinishStep())
                    {
                        Item.SetIntProperty(nameof(UD_FleshGolems_CorpseQuestSystem), 0, true);
                        if (!CorpseQuestSystem.Completable && CorpseQuestSystem.PlayerAdvisedCompletable)
                        {
                            Popup.Show(
                                "You no longer have the " + RequiredToComplete + " questionable materials that " +
                                CorpseQuestSystem.InfluencerRefName + " said he needed.");
                            CorpseQuestSystem.PlayerAdvisedCompletable = false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        public bool CheckItem(GameObject Item, bool Unfinish = false)
        {
            return CheckItem(this, Item, Unfinish);
        }

        public void ProcessEvent(IActOnItemEvent E, bool Unfinish = false)
        {
            if (E.Item is GameObject item
                && CheckItem(item, Unfinish))
            {
                if (!Unfinish)
                {
                    item.RequirePart<UD_FleshGolems_CorpseQuestHelperPart>();
                    item.RegisterEvent(this, DroppedEvent.ID, Serialize: true);
                    item.RegisterEvent(this, TakenEvent.ID, Serialize: true);
                }
                else
                {
                    item.RemovePart<UD_FleshGolems_CorpseQuestHelperPart>();
                    item.UnregisterEvent(this, DroppedEvent.ID);
                    item.UnregisterEvent(this, TakenEvent.ID);
                }
            }
        }

        public static bool StartQuest()
        {
            return The.Game.StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID) != null;
        }
        public static bool StartQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem ParentSystem, Quest ParentQuest)
        {
            try
            {
                var corpseQuestSystem = The.Game.RequireSystem<UD_FleshGolems_CorpseQuestSystem>();
                corpseQuestSystem.ParentQuest = ParentQuest;
                corpseQuestSystem.ParentSystem = ParentSystem;
                debugLog(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(StartQuest));
                corpseQuestSystem.Init();
                Quest quest = new()
                {
                    ID = "UD_FleshGolems " + QuestName,
                    System = corpseQuestSystem,
                    Name = QuestName,
                    Level = 20,
                    Factions = "Newly Sentient Beings",
                    Reputation = "100",
                    Finished = false,
                    Accomplishment = "Conspiring with a \"scientist\" most mad, you broke \"important\" scientfic discovery into the nature of life and death.",
                    Hagiograph = "Forget not the bloody " + Calendar.GetDay() + " of darkest " + Calendar.GetMonth() + ", when =name= demanded of =pronouns.possessive= loyal servant, that he take the first born babe of every denizen of Bethesda Susa in recompence for the sins of the Saads of old!",
                    HagiographCategory = "DoesSomethingRad",
                    StepsByID = new Dictionary<string, QuestStep>()
                };
                corpseQuestSystem.MyQuestID = quest.ID;
                for (int i = 0; i < corpseQuestSystem.Steps.Count; i++)
                {
                    QuestStep questStep = new()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Name = corpseQuestSystem.Steps[i].Name,
                        Finished = false,
                        Text = corpseQuestSystem.Steps[i].Text,
                        XP = 0,
                        Awarded = true,
                    };
                    corpseQuestSystem.Steps[i].Name = questStep.ID;
                    quest.StepsByID.Add(questStep.ID, questStep);
                }
                if (corpseQuestSystem.Steps.Count > 0)
                {
                    The.Game.StartQuest(quest, corpseQuestSystem.InfluencerRefName);
                    foreach (GameObject item in corpseQuestSystem.Player.GetInventory())
                    {
                        corpseQuestSystem.CheckItem(item);
                    }
                    _ = corpseQuestSystem.Completable;
                    return true;
                }
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(StartQuest), x, "game_mod_exception");
            }
            return false;
        }

        public override void Finish()
        {
            debugLog(nameof(UD_FleshGolems_CorpseQuestSystem) + "." + nameof(Finish));
            if (ParentQuest == null)
            {
                Game.TryGetQuest(UD_FleshGolems_YouRaiseMeUpQuestSystem.MongerQuestID, out ParentQuest);
            }
            ParentQuest?.FinishStep(ParentQuestStep);
            base.Finish();
        }

        public override void Register(XRLGame Game, IEventRegistrar Registrar)
        {
            Registrar.Register(ZoneActivatedEvent.ID);
        }

        public override void RegisterPlayer(GameObject Player, IEventRegistrar Registrar)
        {
            Registrar.Register(TookEvent.ID);
            // Registrar.Register("PerformDrop");
        }

        public override bool HandleEvent(ZoneActivatedEvent E)
        {
            /*
            if (E.Zone.X == 1 && E.Zone.Y == 1 && E.Zone.Z == 10 && E.Zone.GetTerrainObject()?.Blueprint == "TerrainFungalCenter")
            {
                The.Game.FinishQuestStep("Pax Klanq, I Presume?", "Seek the Heart of the Rainbow");
            }
            */
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TookEvent E)
        {
            ProcessEvent(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(DroppedEvent E)
        {
            ProcessEvent(E, Unfinish: true);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TakenEvent E)
        {
            ProcessEvent(E, Unfinish: true);
            return base.HandleEvent(E);
        }

        /*
         * 
         * Wishes!
         * 
         */
        [WishCommand(Command = "UD_FleshGolems debug quest species")]
        public static void DebugQuestSpecies_Wish()
        {
            UnityEngine.Debug.Log("All species (included or not)...");
            foreach (string species in GetAllSpecies())
            {
                UnityEngine.Debug.Log(AppendYehNah("    ", SpeciesIsNotExcluded(species)) + species);
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest species corpses")]
        public static void DebugQuestSpeciesCorpses_Wish()
        {
            if (Popup.PickOption("pick which species", Options: AllSpecies, AllowEscape: true) is int pickedOption)
            {
                if (AllFactions.Count > pickedOption)
                {
                    UnityEngine.Debug.Log("All " + AllSpecies[pickedOption] + " corpses...");
                    foreach (string corpse in GetAllCorpsesOfSpecies(AllSpecies[pickedOption]))
                    {
                        UnityEngine.Debug.Log(AppendYehNah("    ", SpeciesIsNotExcluded(corpse)) + corpse);
                    }
                }
                else
                {
                    Popup.Show("Something went wrong there, " + nameof(AllSpecies) + " doesn't appear to have the picked entry.");
                }
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest base")]
        public static void DebugQuestBases_Wish()
        {
            UnityEngine.Debug.Log("All Base Corpse blueprints...");
            foreach (string baseCorpse in GetAllBaseCorpses())
            {
                UnityEngine.Debug.Log("    " + baseCorpse);
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest faction corpses")]
        public static void DebugQuestFactionCorpses_Wish()
        {
            List<string> factionNames = new();
            foreach (string factionName in AllFactions)
            {
                factionNames.Add(Factions.Get(factionName).DisplayName);
            }
            if (Popup.PickOption("pick which faction", Options: factionNames, AllowEscape: true) is int pickedOption)
            {
                if (AllFactions.Count < pickedOption)
                {
                    UnityEngine.Debug.Log("All " + AllFactions[pickedOption] + " corpses...");
                    foreach (string corpse in GetAllCorpsesOfSpecies(AllFactions[pickedOption]))
                    {
                        UnityEngine.Debug.Log("    " + corpse);
                    }
                }
                else
                {
                    Popup.Show("Something went wrong there, " + nameof(AllFactions) + " doesn't appear to have the picked entry.");
                }
            }
        }
        [WishCommand(Command = "UD_FleshGolems debug quest corpses")]
        public static void DebugQuestCorpses_Wish()
        {
            if (The.Game.HasQuest("UD_FleshGolems You Raise Me Up")
                && The.Game.GetSystem<UD_FleshGolems_CorpseQuestSystem>() is var corpseQuestSystem
                && !corpseQuestSystem.Steps.IsNullOrEmpty())
            {
                debugLog(nameof(corpseQuestSystem.ParentQuest), corpseQuestSystem.ParentQuest.ID);
                debugLog(nameof(corpseQuestSystem.Quest), corpseQuestSystem.Quest.ID);
                debugLog("UD_FleshGolems debug quest corpses...");
                foreach (UD_FleshGolems_CorpseQuestStep questStep in corpseQuestSystem.Steps)
                {
                    if (questStep.Corpse is CorpseItem corpseItem)
                    {
                        debugLog(corpseItem.Taxonomy.ToString(), corpseItem.Value, 1);
                        for (int i = 0; i < 3; i++)
                        {

                            if (corpseItem.GetACorpseForThisStep() is string corpseBlueprint)
                            {
                                debugLog(i.ToString(), corpseBlueprint, 2);
                                if (GameObject.Create(corpseBlueprint) is GameObject corpseObject)
                                {
                                    The.Player.CurrentCell
                                        .GetAdjacentCells(2)
                                        .GetRandomElementCosmetic(c => c.IsEmptyFor(corpseObject))
                                        .AddObject(corpseObject);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Popup.Show("You don't have that quest. Spawn the Mad Monger first and start it.");
            }
        }
    }
}