using System;
using System.Collections.Generic;
using System.Text;

using Genkit;

using XRL.Collections;
using XRL.World;
using XRL.World.AI;
using XRL.World.Parts.Mutation;
using XRL.World.Parts.Skill;

using UD_FleshGolems;
using HarmonyLib;
using XRL.Wish;
using XRL.UI;
using XRL.Language;

namespace XRL.World.Parts
{
    [HasWishCommand]
    [Serializable]
    public class UD_FleshGolems_PastLife : IScribedPart
    {
        public bool Init { get; protected set; }
        public bool IsCorpse => (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint)?.InheritsFrom("Corpse")).GetValueOrDefault();

        public bool WasPlayer;

        public int TimesReanimated;

        public string Blueprint;
        public string BaseDisplayName;
        public Render PastRender;
        public string Description;

        [NonSerialized]
        public (string DeathZone, Location2D DeathLocation) DeathAddress;

        [NonSerialized]
        public Brain Brain;

        [NonSerialized]
        public string GenderName;
        [NonSerialized]
        public Gender Gender;

        [NonSerialized]
        public string PronounSetName;
        [NonSerialized]
        public PronounSet PronounSet;

        public string ConversationScriptID;

        [NonSerialized]
        public Dictionary<string, Statistic> Stats;
        public string Species;
        public string Genotype;
        public string Subtype;

        public Dictionary<string, int> MutationLevels;
        public List<string> Skills;
        public List<KeyValuePair<string, string>> InstalledCybernetics;

        public Dictionary<string, string> Tags;
        public Dictionary<string, string> _Property;
        public Dictionary<string, int> _IntProperty;

        public List<KeyValuePair<string, int>> Effects;

        public Titles Titles;
        public Epithets Epithets;
        public Honorifics Honorifics;

        public UD_FleshGolems_PastLife()
        {
            Init = false;

            WasPlayer = false;

            TimesReanimated = 0;

            Blueprint = null;
            BaseDisplayName = null;
            PastRender = null;
            Description = null;

            DeathAddress = (null, null);

            Brain = null;
            GenderName = null;
            Gender = null;
            PronounSetName = null;
            PronounSet = null;
            ConversationScriptID = null;

            Stats = null;
            Species = null;
            Genotype = null;
            Subtype = null;

            MutationLevels = null;
            Skills = null;
            InstalledCybernetics = null;

            Tags = null;
            _Property = null;
            _IntProperty = null;

            Effects = null;

            Titles = null;
            Epithets = null;
            Honorifics = null;
        }
        public UD_FleshGolems_PastLife(GameObject PastLife)
            : this()
        {
            Initialize(PastLife);
        }
        public UD_FleshGolems_PastLife(UD_FleshGolems_PastLife PrevPastLife)
            : this()
        {
            Initialize(PrevPastLife);
        }

        public void Initialize(GameObject PastLife)
        {
            if (!Init)
            {
                try
                {
                    WasPlayer = PastLife != null && PastLife.Blueprint.IsPlayerBlueprint();
                    Blueprint = PastLife?.Blueprint;
                    if (GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint).InheritsFrom("Corpse"))
                    {
                        TimesReanimated = 1;
                    }
                    BaseDisplayName = PastLife?.BaseDisplayName;
                    PastRender = PastLife?.Render?.DeepCopy(ParentObject) as Render;
                    Description = PastLife?.GetPart<Description>()?._Short;

                    DeathAddress = (PastLife?.CurrentZone?.ZoneID, PastLife?.CurrentCell?.Location);

                    if (PastLife?.Brain is Brain pastBrain)
                    {
                        Brain = pastBrain.DeepCopy(ParentObject, null) as Brain;
                        if (Brain != null)
                        {
                            try
                            {
                                foreach ((int flags, PartyMember partyMember) in pastBrain.PartyMembers)
                                {
                                    PartyMember partyMemberCopy = new(partyMember.Reference, partyMember.Flags);
                                    Brain.PartyMembers.TryAdd(flags, partyMemberCopy);
                                }
                            }
                            catch (Exception x)
                            {
                                MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                                Brain.PartyMembers = new();
                            }
                            try
                            {
                                foreach ((int key, OpinionList opinionList) in pastBrain.Opinions)
                                {
                                    OpinionList opinionsCopy = new();
                                    foreach (IOpinion opinionCopy in opinionList)
                                    {
                                        opinionsCopy.Add(opinionCopy);
                                    }
                                    Brain.Opinions.TryAdd(key, opinionsCopy);
                                }
                            }
                            catch (Exception x)
                            {
                                MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                                Brain.Opinions = new();
                            }
                        }
                    }
                    GenderName = PastLife?.GenderName;
                    Gender = new(PastLife?.GetGender(AsIfKnown: true));
                    if (PastLife?.GetGender(AsIfKnown: true) is Gender pastGender)
                    {
                        Gender = new(pastGender);
                    }
                    PronounSetName = PastLife?.PronounSetName;
                    if (PastLife?.GetPronounSet() is PronounSet pastPronouns)
                    {
                        PronounSet = new(pastPronouns);
                    }
                    ConversationScriptID = PastLife?.GetPart<ConversationScript>()?.ConversationID;

                    Stats = new(PastLife?.Statistics);
                    Species = PastLife?.GetSpecies();
                    Genotype = PastLife?.GetGenotype();
                    Subtype = PastLife?.GetSubtype();

                    if (PastLife?.GetPart<Mutations>() is Mutations pastMutations)
                    {
                        MutationLevels = new();
                        foreach (BaseMutation baseMutation in pastMutations.MutationList)
                        {
                            MutationLevels.TryAdd(baseMutation.GetMutationEntry().Name, baseMutation.BaseLevel);
                        }
                    }
                    if (PastLife?.GetPart<Skills>() is Skills pastSkills)
                    {
                        Skills = new();
                        foreach (BaseSkill baseSkill in pastSkills.SkillList)
                        {
                            Skills.Add(baseSkill.Name);
                        }
                    }
                    if (PastLife?.GetInstalledCyberneticsReadonly() is List<GameObject> pastInstalledCybernetics)
                    {
                        InstalledCybernetics = new();
                        foreach (GameObject pastInstalledCybernetic in pastInstalledCybernetics)
                        {
                            if (pastInstalledCybernetic.ID is string cyberneticID
                                && PastLife?.Body?.FindCybernetics(pastInstalledCybernetic)?.Type is string implantedLimb)
                            {
                                InstalledCybernetics.Add(new(cyberneticID, implantedLimb));
                            }
                        }
                    }

                    Tags = new(PastLife?.GetBlueprint()?.Tags);
                    _Property = new(PastLife?._Property);
                    _IntProperty = new(PastLife?._IntProperty);

                    if (PastLife != null && !PastLife._Effects.IsNullOrEmpty())
                    {
                        Effects = new();
                        foreach (Effect pastEffect in PastLife._Effects)
                        {
                            Effects.Add(new(pastEffect.ClassName, pastEffect.Duration));
                        }
                    }

                    Titles = PastLife?.GetPart<Titles>()?.DeepCopy(ParentObject) as Titles;
                    Epithets = PastLife?.GetPart<Epithets>()?.DeepCopy(ParentObject) as Epithets;
                    Honorifics = PastLife?.GetPart<Honorifics>()?.DeepCopy(ParentObject) as Honorifics;
                }
                catch (Exception x)
                {
                    MetricsManager.LogException(Name + "." + nameof(Initialize), x, "game_mod_exception");
                }
                finally
                {
                    Init = true;
                }
            }
        }

        public void Initialize(UD_FleshGolems_PastLife PrevPastLife)
        {
            if (!Init && PrevPastLife != null && PrevPastLife.Init)
            {
                TimesReanimated = PrevPastLife.TimesReanimated;

                Blueprint = PrevPastLife.Blueprint;
                BaseDisplayName = PrevPastLife.BaseDisplayName;
                PastRender = PrevPastLife.PastRender;
                Description = PrevPastLife.Description;

                DeathAddress = PrevPastLife.DeathAddress;

                Brain = PrevPastLife.Brain;
                Gender = PrevPastLife.Gender;
                PronounSet = PrevPastLife.PronounSet;
                ConversationScriptID = PrevPastLife.ConversationScriptID;

                Stats = PrevPastLife.Stats;
                Species = PrevPastLife.Species;
                Genotype = PrevPastLife.Genotype;
                Subtype = PrevPastLife.Subtype;

                MutationLevels = PrevPastLife.MutationLevels;
                Skills = PrevPastLife.Skills;
                InstalledCybernetics = PrevPastLife.InstalledCybernetics;

                Tags = PrevPastLife.Tags;
                _Property = PrevPastLife._Property;
                _IntProperty = PrevPastLife._IntProperty;

                Effects = PrevPastLife.Effects;

                Titles = PrevPastLife.Titles;
                Epithets = PrevPastLife.Epithets;
                Honorifics = PrevPastLife.Honorifics;

                Init = true;
            }
            else
            if (PrevPastLife?.ParentObject is GameObject pastLife)
            {
                Initialize(pastLife);
            }
        }

        public override void Attach()
        {
            if (Init && GameObjectFactory.Factory.GetBlueprintIfExists(Blueprint).InheritsFrom("Corpse"))
            {
                TimesReanimated++;
            }
            base.Attach();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public virtual void DebugOutput()
        {
            void debugLog(string Field, object Value = null, int Indent = 0)
            {
                string indent = " ".ThisManyTimes(Math.Min(12, Indent) * 4);
                string output = indent + Field;
                if (Value != null &&
                    !Value.ToString().IsNullOrEmpty())
                {
                    output += ": " + Value;
                }
                UnityEngine.Debug.Log(output);
            }
            try
            {
                debugLog(nameof(BaseDisplayName), BaseDisplayName);
                debugLog(nameof(Blueprint), Blueprint);

                debugLog(nameof(Init), Init);

                debugLog(nameof(TimesReanimated), TimesReanimated);

                debugLog(nameof(PastRender), PastRender);
                debugLog(nameof(Description), Description);

                debugLog(nameof(DeathAddress), DeathAddress);

                debugLog(nameof(Brain), Brain != null);
                debugLog(nameof(Brain.Allegiance), null, 1);
                foreach ((string faction, int rep) in Brain?.Allegiance ?? new())
                {
                    debugLog(faction, rep, 2);
                }
                if (Brain != null)
                {
                    debugLog("bools", null, 1);
                    Traverse brainWalk = new(Brain);
                    foreach (string field in brainWalk.Fields() ?? new())
                    {
                        string fieldValue = brainWalk?.Field(field)?.GetValue()?.ToString();
                        debugLog(field, fieldValue ?? "??", 2);
                    }
                }
                debugLog(nameof(Gender), Gender);
                debugLog(nameof(PronounSet), PronounSet);
                debugLog(nameof(ConversationScriptID), ConversationScriptID);

                debugLog(nameof(Stats), Stats?.Count);
                foreach ((string statName, Statistic stat) in Stats ?? new())
                {
                    debugLog(statName, stat.BaseValue, 1);
                }
                debugLog(nameof(Species), Species);
                debugLog(nameof(Genotype), Genotype);
                debugLog(nameof(Subtype), Subtype);

                debugLog(nameof(MutationLevels), MutationLevels?.Count);
                foreach ((string mutation, int level) in MutationLevels ?? new())
                {
                    debugLog(mutation, level, 1);
                }
                debugLog(nameof(Skills), Skills?.Count);
                foreach (string skill in Skills ?? new())
                {
                    debugLog(skill, null, 1);
                }
                debugLog(nameof(InstalledCybernetics), InstalledCybernetics?.Count);
                foreach ((string blueprint, string limb) in InstalledCybernetics ?? new())
                {
                    debugLog(blueprint, limb, 1);
                }

                debugLog(nameof(Tags), Tags?.Count);
                foreach ((string name, string value) in Tags ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(_Property), _Property?.Count);
                foreach ((string name, string value) in _Property ?? new())
                {
                    debugLog(name, value, 1);
                }
                debugLog(nameof(_IntProperty), _IntProperty?.Count);
                foreach ((string name, int value) in _IntProperty ?? new())
                {
                    debugLog(name, value, 1);
                }

                debugLog(nameof(Effects), Effects?.Count);
                foreach ((string effectName, int effectDuration) in Effects ?? new())
                {
                    debugLog(effectName + ",  duration" , effectDuration, 1);
                }

                debugLog(nameof(Titles), Titles);
                debugLog(nameof(Epithets), Epithets);
                debugLog(nameof(Honorifics), Honorifics);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(Name + "." + nameof(DebugOutput), x, "game_mod_exception");
            }
        }

        public override void Write(GameObject Basis, SerializationWriter Writer)
        {
            base.Write(Basis, Writer);

            Writer.WriteOptimized(DeathAddress.DeathZone);
            Writer.WriteOptimized(DeathAddress.DeathLocation.X);
            Writer.WriteOptimized(DeathAddress.DeathLocation.Y);

            Brain ??= new();
            Save(Brain, Writer);

            Writer.WriteOptimized(Gender?.Name);
            Writer.WriteOptimized(PronounSet?.Name);

            Writer.Write(Stats.Count);
            foreach ((string _, Statistic stat) in Stats)
            {
                stat.Save(Writer);
            }
        }
        public override void Read(GameObject Basis, SerializationReader Reader)
        {
            base.Read(Basis, Reader);

            DeathAddress = new(Reader.ReadOptimizedString(), new(Reader.ReadOptimizedInt32(), Reader.ReadOptimizedInt32()));

            Brain = Load(Basis, Reader) as Brain;

            GenderName = Reader.ReadOptimizedString();

            PronounSetName = Reader.ReadOptimizedString();

            int statCount = Reader.ReadInt32();
            Stats = new(statCount);
            for (int i = 0; i < statCount; i++)
            {
                Statistic statistic = Statistic.Load(Reader, Basis);
                Stats.TryAdd(statistic.Name, statistic);
            }
        }
        public override void FinalizeRead(SerializationReader Reader)
        {
            base.FinalizeRead(Reader);
            Gender = new(GenderName);
            PronounSet = PronounSet.Get(PronounSetName);
        }

        [WishCommand("UD_FleshGolems debug PastLife")]
        public static void Debug_PastLife_WishHandler()
        {
            int startX = 40;
            int startY = 12;
            if (The.Player.CurrentCell is Cell playerCell)
            {
                startX = playerCell.X;
                startY = playerCell.Y;
            }
            if (PickTarget.ShowPicker(
                Style: PickTarget.PickStyle.EmptyCell,
                StartX: startX,
                StartY: startY,
                VisLevel: AllowVis.Any,
                ObjectTest: GO => GO.HasPart<UD_FleshGolems_PastLife>(),
                Label: "debug " + nameof(UD_FleshGolems_PastLife)) is Cell pickCell
                && Popup.PickGameObject(
                    Title: "pick a thing with a past life",
                    Objects: pickCell.GetObjectsWithPart(nameof(UD_FleshGolems_PastLife)),
                    AllowEscape: true,
                    ShortDisplayNames: true) is GameObject pickedObject)
            {
                pickedObject?.GetPart<UD_FleshGolems_PastLife>().DebugOutput();
                Popup.Show(
                    "debug output for " + Grammar.MakePossessive(pickedObject.ShortDisplayNameSingleStripped) + " " +
                    nameof(UD_FleshGolems_PastLife));
            }
            else
            {
                Popup.Show("nothing selected to debug " + nameof(UD_FleshGolems_PastLife));
            }
        }
    }
}
