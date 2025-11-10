using System;

using static XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestSystem;

using CorpseTaxonomy = XRL.World.QuestManagers.UD_FleshGolems_CorpseQuestStep.CorpseItem.CorpseTaxonomy;

using SerializeField = UnityEngine.SerializeField;

using UD_FleshGolems;
using System.Linq;
using System.Collections.Generic;

namespace XRL.World.QuestManagers
{
    [Serializable]
    public class UD_FleshGolems_CorpseQuestStep : IComposite
    {
        [Serializable]
        public class CorpseItem : IComposite
        {
            public enum CorpseTaxonomy : int
            {
                Empty = -1,
                Any = 0,
                Species = 1,
                Base = 2,
                Faction = 3,
            }

            public CorpseTaxonomy Taxonomy;
            public string Value;

            public bool IsAny => Taxonomy == CorpseTaxonomy.Any;
            public bool IsSpecies => Taxonomy == CorpseTaxonomy.Species;
            public bool IsBase => Taxonomy == CorpseTaxonomy.Base;
            public bool IsFaction => Taxonomy == CorpseTaxonomy.Faction;

            private CorpseItem()
            {
                Taxonomy = CorpseTaxonomy.Empty;
                Value = null;
            }

            public CorpseItem(CorpseTaxonomy Taxonomy, string Value)
                : this()
            {
                this.Taxonomy = Taxonomy;
                this.Value = Value;
            }

            public bool CorpseCompletesThisStep(GameObject CorpseObject) => Taxonomy switch
            {
                CorpseTaxonomy.Any =>
                    IsCorpse(CorpseObject),

                CorpseTaxonomy.Species =>
                    IsCorpse(CorpseObject)
                    && GetAllCorpsesOfSpecies(Value).Contains(CorpseObject.Blueprint),

                CorpseTaxonomy.Base =>
                    IsCorpse(CorpseObject)
                    && CorpseObject.GetBlueprint().InheritsFrom(Value),

                CorpseTaxonomy.Faction =>
                    IsCorpse(CorpseObject)
                    && GetAllCorpsesOfFaction(Value).Contains(CorpseObject.Blueprint),

                _ => false,
            };

            public string GetACorpseForThisStep() => Taxonomy switch
            {
                CorpseTaxonomy.Any =>
                    GameObjectFactory.Factory
                    ?.GetBlueprintsInheritingFrom("Corpse")
                    ?.GetRandomElementCosmetic(bp => !bp.IsBaseBlueprint()).Name,

                CorpseTaxonomy.Species =>
                    GetAllCorpsesOfSpecies(Value)
                    ?.GetRandomElementCosmetic(),

                CorpseTaxonomy.Base =>
                    GetAllCorpsesOfBase(Value)
                    ?.GetRandomElementCosmetic(),

                CorpseTaxonomy.Faction =>
                    GetAllCorpsesOfFaction(Value)
                    ?.GetRandomElementCosmetic(),

                _ => null,
            };
        }
        public static string ReplaceFind => "*Find*";
        public static string ReplaceType => "*type*";

        public static List<string> FindVerbs => new()
        {
            "Acquire",
            "Attain",
            "Collect",
            "Fetch",
            "Find",
            "Gather",
            "Get",
            "Locate",
            "Obtain",
            "Procure",
            "Secure",
            "Snag",
            "Source",
        };
        public static List<string> GenericCorpseQuestText => new()
        {
            "*Find* one of any kind of *type* corpse...",
            "*Find* the corpse of any type of *type*...",
            "*Find* a single *type* corpse...",
        };
        public static List<string> AnyCorpseQuestText => new()
        {
            "*Find* quite literally any kind of corpse...",
        };
        public static List<string> SpeciesCorpseQuestText => new()
        {
            "*Find* the corpse of any species of *type*...",
            "*Find* a *type* species corpse...",
        };
        public static List<string> BaseCorpseQuestText => new()
        {
            "*Find* any *type* creature's corpse...",
            "*Find* one of any *type* variety of corpse...",
        };
        public static List<string> FactionCorpseQuestText => new()
        {
            "*Find* a corpse from a member of the *type* faction...",
        };

        private UD_FleshGolems_CorpseQuestSystem ParentSystem;

        public string Name;

        public string Text;

        public CorpseItem Corpse;

        public string Item;

        public bool Finished;

        private UD_FleshGolems_CorpseQuestStep()
        {
            Name = null;
            Text = null;
            Corpse = null;
            Item = null;
            Finished = false;
        }

        public UD_FleshGolems_CorpseQuestStep(UD_FleshGolems_CorpseQuestSystem ParentSystem)
            : this()
        {
            this.ParentSystem = ParentSystem;
        }

        public string GenerateStepText()
        {
            List<string> entries = new(GenericCorpseQuestText);
            switch (Corpse.Taxonomy)
            {
                case CorpseTaxonomy.Any:
                    return AnyCorpseQuestText
                        .GetRandomElementCosmetic()
                        .Replace(ReplaceFind, FindVerbs.GetRandomElementCosmetic());

                case CorpseTaxonomy.Species:
                    entries.AddRange(SpeciesCorpseQuestText);
                    return entries
                        .GetRandomElementCosmetic()
                        .Replace(ReplaceFind, FindVerbs.GetRandomElementCosmetic())
                        .Replace(ReplaceType, Corpse.Value);

                case CorpseTaxonomy.Base:
                    entries.AddRange(BaseCorpseQuestText);
                    return entries
                        .GetRandomElementCosmetic()
                        .Replace(ReplaceFind, FindVerbs.GetRandomElementCosmetic())
                        .Replace(ReplaceType, Corpse.Value);

                case CorpseTaxonomy.Faction:
                    entries.AddRange(FactionCorpseQuestText);
                    return entries
                        .GetRandomElementCosmetic()
                        .Replace(ReplaceFind, FindVerbs.GetRandomElementCosmetic())
                        .Replace(ReplaceType, Corpse.Value);

                default:
                    return entries
                        .GetRandomElementCosmetic()
                        .Replace(ReplaceFind, FindVerbs.GetRandomElementCosmetic())
                        .Replace(ReplaceType, Corpse.Value);
            }
        }

        public UD_FleshGolems_CorpseQuestStep SetGeneratedStepText()
        {
            Text = GenerateStepText();
            return this;
        }

        public bool FinishStep()
        {
            if (!Finished)
            {
                Finished = true;
                if (!Name.IsNullOrEmpty() 
                    && ParentSystem?.Quest?.StepsByID[Name] is QuestStep thisStep)
                {
                    thisStep.Finished = true;
                }
                return true;
            }
            return false;
        }
        public bool UnfinishStep()
        {
            if (Finished)
            {
                Finished = false;
                if (!Name.IsNullOrEmpty()
                    && ParentSystem?.Quest?.StepsByID[Name] is QuestStep thisStep)
                {
                    thisStep.Finished = false;
                }
                return true;
            }
            return false;
        }

        public void Write(SerializationWriter Writer)
        {
            Writer.WriteObject(ParentSystem);
        }

        public void Read(SerializationReader Reader)
        {
            ParentSystem = Reader.ReadObject() as UD_FleshGolems_CorpseQuestSystem;
        }
    }
}