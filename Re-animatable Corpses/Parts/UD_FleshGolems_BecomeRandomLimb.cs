using System;
using System.Collections.Generic;
using System.Text;

using Qud.API;
using XRL.World.Anatomy;
using static XRL.World.Parts.UD_FleshGolems_HasCyberneticsButcherableCybernetic;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using XRL.Rules;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_BecomeRandomLimb : IScribedPart
    {
        public static Dictionary<string, string> LimbToSlotTranslations => new()
        {
            { "Arm", "Arm" },
            { "Leg", "Feet" },
            { "Foot", "Feet" },
            { "Hand", "Hands" },
            { "Head", "Head" },
            { "Face", "Face" },
        };
        public static Dictionary<string, List<string>> SlotToLimbTranslations => new()
        {
            { "Arm", new() { "Arm" } },
            { "Feet", new() { "Leg", "Foot" } },
            { "Hands", new() { "Hand" } },
            { "Head", new() { "Head" } },
            { "Face", new() { "Face" } },
        };

        public string TypesOneOf;

        public string BasesOneOf;

        public string BlueprintsOneOf;

        public string TagsOneOf;

        public string CyberneticsBlueprints;

        public string CyberneticsTable;

        public string CyberneticsForLimb;

        public string Count;

        public bool UseImplantedAdjectiveIfImplanted;
        public double Commerce;
        public int BlurValueAmount;
        public double BlurValueMargin;

        public bool KeepOnFail;
        private bool MarkedForOblivion;

        public UD_FleshGolems_BecomeRandomLimb()
        {
            TypesOneOf = null;
            BasesOneOf = null;
            BlueprintsOneOf = null;
            TagsOneOf = null;

            CyberneticsBlueprints = null;

            CyberneticsTable = null;

            CyberneticsForLimb = null;

            Count = "1";

            UseImplantedAdjectiveIfImplanted = true;
            Commerce = -1.0;
            BlurValueAmount = 0;
            BlurValueMargin = 0.0;

            KeepOnFail = false;
            MarkedForOblivion = false;
        }

        public override bool AllowStaticRegistration() => true;

        public static string ProcessSpec(string Spec, List<string> Wildcards = null)
        {
            string processed = null;
            if (!Spec.IsNullOrEmpty() && (Wildcards.IsNullOrEmpty() || !Wildcards.Contains(Spec)))
            {
                processed = Spec.CachedCommaExpansion().GetRandomElement();
                if (!Wildcards.IsNullOrEmpty() && Wildcards.Contains(processed))
                {
                    processed = null;
                }
            }
            return processed;
        }

        public static int GetCyberneticsCount(int? PossibleCount = null, int BaseCount = 1) => Math.Max(1, PossibleCount ?? BaseCount);

        public static bool CyberneticsSlotsContainsSeverableLimbType(string CyberneticsSlots) => 
               CyberneticsSlots.Contains("Arm")
            || CyberneticsSlots.Contains("Feet")
            || CyberneticsSlots.Contains("Hands")
            || CyberneticsSlots.Contains("Head")
            || CyberneticsSlots.Contains("Face");

        public override bool WantEvent(int ID, int Cascade) => base.WantEvent(ID, Cascade)
            || ID == EnvironmentalUpdateEvent.ID
            || ID == TakenEvent.ID
            || ID == AfterObjectCreatedEvent.ID;

        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!KeepOnFail && MarkedForOblivion)
            {
                ParentObject.Obliterate();
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(TakenEvent E)
        {
            if (!KeepOnFail && MarkedForOblivion)
            {
                ParentObject.Obliterate();
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {
            if (E.Object == ParentObject)
            {
                string blueprint = ProcessSpec(BlueprintsOneOf, Wildcards);
                string @base = ProcessSpec(BasesOneOf, Wildcards);
                string tag = ProcessSpec(TagsOneOf, Wildcards);
                string type = ProcessSpec(TypesOneOf, Wildcards);

                if (!CyberneticsForLimb.IsNullOrEmpty() 
                    && Anatomies.GetBodyPartType(CyberneticsForLimb) != null)
                {
                    type = CyberneticsForLimb;
                }
                type ??= ParentObject.GetPropertyOrTag("UD_FleshGolems LimbType");

                SanitizeLimbType(type, out type, Wildcards);
                /*
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_BecomeRandomLimb) + "." + nameof(BeforeObjectCreatedEvent) + ", " +
                    nameof(E.Object) + ": " + (E.Object?.DebugName ?? NULL) + ", " +
                    nameof(blueprint) + ": " + (blueprint ?? NULL) + ", " +
                    nameof(@base) + ": " + (@base ?? NULL) + ", " +
                    nameof(tag) + ": " + (tag ?? NULL) + ", " +
                    nameof(type) + ": " + (type ?? NULL));
                */
                string tableSlotType = null;
                string tableBlueprint = null;
                if (!CyberneticsTable.IsNullOrEmpty())
                {
                    string processedTable = CyberneticsTable.Replace("~#~", Stat.RollCached("1d8").ToString());
                    /*
                    UnityEngine.Debug.Log(
                        "    " + nameof(CyberneticsTable) + ": " + (CyberneticsTable ?? NULL) + ", " +
                        nameof(processedTable) + ": " + processedTable);
                    */
                    for (int i = 0; i < 10; i++)
                    {
                        PopulationResult cyberneticsPopulationResult = PopulationManager.RollOneFrom(processedTable);
                        if (cyberneticsPopulationResult != null
                            && GameObjectFactory.Factory.GetBlueprintIfExists(cyberneticsPopulationResult.Blueprint) is GameObjectBlueprint cyberneticsBlueprint
                            && cyberneticsBlueprint.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string tableCyberneticSlots)
                            && CyberneticsSlotsContainsSeverableLimbType(tableCyberneticSlots))
                        {
                            /*
                            UnityEngine.Debug.Log(
                                "    " + "    [" + i + "] population result: " + (cyberneticsPopulationResult.Blueprint ?? "null") + ", " +
                                nameof(tableCyberneticSlots) + ": " + tableCyberneticSlots);
                            */
                            if (tableCyberneticSlots.Contains(','))
                            {
                                foreach (string slotType in tableCyberneticSlots.Split(',').ShuffleInPlace())
                                {
                                    if (SlotToLimbTranslations.ContainsKey(slotType))
                                    {
                                        tableSlotType = SlotToLimbTranslations[slotType].GetRandomElement();
                                        tableBlueprint = cyberneticsBlueprint.Name;
                                        break;
                                    }
                                }
                            }
                            else
                            if (SlotToLimbTranslations.ContainsKey(tableCyberneticSlots))
                            {
                                tableSlotType = SlotToLimbTranslations[tableCyberneticSlots].GetRandomElement();
                                tableBlueprint = cyberneticsBlueprint.Name;
                                break;
                            }
                        }
                    }
                    bool good = !tableSlotType.IsNullOrEmpty();
                    /*
                    UnityEngine.Debug.Log(
                        "    " + "    [" + (good ? TICK : CROSS) + "] " + (good ? "Success" : "Fail") + "!" +
                        " " + nameof(tableSlotType) + ": " + tableSlotType + ", " + 
                        " " + nameof(tableBlueprint) + ": " + tableBlueprint);
                    */
                }
                if (!tableSlotType.IsNullOrEmpty())
                {
                    type = tableSlotType;
                }

                if (BodyPart.MakeSeveredBodyPart(blueprint, type, tag, @base) is GameObject severedLimbObject)
                {
                    bool hasCybernetics = false;
                    if (!CyberneticsBlueprints.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsList(severedLimbObject, CyberneticsBlueprints)
                            || hasCybernetics;
                    }
                    if (!tableBlueprint.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsList(severedLimbObject, tableBlueprint)
                            || hasCybernetics;
                    }
                    else
                    if (!CyberneticsTable.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsTable(severedLimbObject, CyberneticsTable, Count)
                            || hasCybernetics;
                    }
                    if (!CyberneticsForLimb.IsNullOrEmpty())
                    {
                        hasCybernetics = EmbedButcherableCyberneticsForLimb(severedLimbObject, CyberneticsForLimb, Count)
                            || hasCybernetics;
                    }
                    if (CyberneticsBlueprints.IsNullOrEmpty()
                        && CyberneticsTable.IsNullOrEmpty()
                        && CyberneticsForLimb.IsNullOrEmpty()
                        && !Count.IsNullOrEmpty()
                        && Count.RollMax() > 0)
                    {
                        hasCybernetics = EmbedButcherableCyberneticsForLimb(severedLimbObject, type, Count)
                            || hasCybernetics;
                    }
                    if (hasCybernetics && UseImplantedAdjectiveIfImplanted)
                    {
                        var cyberneticsHasRandomImplants = new CyberneticsHasRandomImplants();
                        severedLimbObject.RequirePart<DisplayNameAdjectives>().AddAdjective(cyberneticsHasRandomImplants.Adjective);
                    }
                    if (severedLimbObject.TryGetPart(out CyberneticsButcherableCybernetic butcherableCybernetics)
                        && butcherableCybernetics.Cybernetics.IsNullOrEmpty())
                    {
                        severedLimbObject.RemovePart(butcherableCybernetics);
                    }
                    if (hasCybernetics)
                    {
                        AdjustCommerceValue(severedLimbObject, Commerce, BlurValueAmount, BlurValueMargin);
                        E.ReplacementObject = severedLimbObject;
                    }
                    else
                    {
                        MarkedForOblivion = !KeepOnFail;
                    }
                }
                else
                {
                    MarkedForOblivion = !KeepOnFail;
                }
            }
            return base.HandleEvent(E);
        }
    }
}
