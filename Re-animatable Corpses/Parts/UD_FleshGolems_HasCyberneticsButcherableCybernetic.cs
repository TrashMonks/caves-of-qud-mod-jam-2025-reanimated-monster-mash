using System;
using System.Collections.Generic;
using System.Text;

using Qud.API;

using UD_FleshGolems;
using static UD_FleshGolems.Const;

using XRL.World.Anatomy;
using System.Linq;
using XRL.Rules;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_HasCyberneticsButcherableCybernetic : IScribedPart
    {
        public static List<string> Wildcards => new() { "*", "any", "Any", "ANY" };

        public string Base;

        public string Cybernetics;
        public string Table;
        public string ForLimb;

        public string Count;

        public bool UseImplantedAdjectiveIfImplanted;
        public double Commerce;
        public int BlurValueAmount;
        public double BlurValueMargin;

        public bool KeepOnFail;
        private bool MarkedForOblivion;

        public UD_FleshGolems_HasCyberneticsButcherableCybernetic()
        {
            Base = null;

            Cybernetics = null;
            Table = null;
            ForLimb = null;

            Count = "1";

            UseImplantedAdjectiveIfImplanted = true;
            Commerce = -1.0;
            BlurValueAmount = 0;
            BlurValueMargin = 0.0;

            KeepOnFail = true;
            MarkedForOblivion = false;
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public static bool IsBlueprintForLimb(GameObjectBlueprint b, string Limb) =>
            b.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Slots), out string slots)
            && (slots.Contains(Limb) || Wildcards.Contains(Limb));
        public static bool IsBlueprintForLimbAroundCost(GameObjectBlueprint b, string Limb, int Cost) =>
            IsBlueprintForLimb(b, Limb)
            && b.TryGetPartParameter(nameof(CyberneticsBaseItem), nameof(CyberneticsBaseItem.Cost), out int cost)
            && Math.Abs(cost - Cost) < 3;

        public static void SanitizeLimbType(string ForLimb, out string SafeLimb, List<string> Wildcards = null)
        {
            SafeLimb = ForLimb;
            if (ForLimb.IsNullOrEmpty() || Anatomies.GetBodyPartType(ForLimb) == null || (!Wildcards.IsNullOrEmpty() && Wildcards.Contains(ForLimb)))
            {
                SafeLimb = 
                    (from type in Anatomies.BodyPartTypeList
                     where type.Appendage == true && type.Plural == false
                     select type)
                     .GetRandomElement()
                     .FinalType;
            }
        }

        public static bool BlueprintMatchesSpec(
            GameObjectBlueprint GameObjectBlueprint,
            string Blueprint = null,
            string Type = null,
            string Tag = null,
            string Base = null,
            Predicate<GameObjectBlueprint> Filter = null)
        {
            if (GameObjectBlueprint == null || !GameObjectBlueprint.HasPart(nameof(Body)))
            {
                return false;
            }
            if (Blueprint != null && GameObjectBlueprint.Name != Blueprint)
            {
                return false;
            }
            /* if (Type != null 
                && GameObjectBlueprint.TryGetPartParameter(nameof(Body), nameof(Body.Anatomy), out string anatomyName)
                && !Anatomies.GetAnatomy(anatomyName).Parts.Any(p => p.Type.Type == Type))
            {
                return false;
            }*/
            if (Tag != null && !GameObjectBlueprint.Tags.ContainsKey(Tag))
            {
                return false;
            }
            if (Base != null && !GameObjectBlueprint.InheritsFrom(Base))
            {
                return false;
            }
            if (Filter != null && !Filter(GameObjectBlueprint))
            {
                return false;
            }
            return true;
        }
        public static GameObjectBlueprint GetCreatureBlueprintFromSpec(
            string Blueprint = null,
            string Type = null,
            string Tag = null,
            string Base = null,
            Predicate<GameObjectBlueprint> Filter = null)
        {
            return EncountersAPI.GetACreatureBlueprintModel(BP => BlueprintMatchesSpec(BP, Blueprint, Type, Tag, Base, Filter));
        }
        public static GameObject GetCreatureFromSpec(
            string Blueprint = null,
            string Type = null,
            string Tag = null,
            string Base = null,
            Predicate<GameObjectBlueprint> Filter = null)
        {
            return GameObject.CreateSample(GetCreatureBlueprintFromSpec(Blueprint, Type, Tag, Base, Filter).Name);
        }

        public static bool TryGetButcherableCyberneticPart(GameObject Corpse, out CyberneticsButcherableCybernetic ButcherableCyberneticPart)
        {
            ButcherableCyberneticPart = null;
            if (Corpse == null)
            {
                return false;
            }
            ButcherableCyberneticPart = Corpse.RequirePart<CyberneticsButcherableCybernetic>();
            return true;
        }
        public static bool EmbedCyberneticWithSuccessChance(
            CyberneticsButcherableCybernetic ButcherableCyberneticPart,
            GameObject CyberneticObject,
            int SuccessChance)
        {
            ButcherableCyberneticPart.Cybernetics.Add(CyberneticObject);
            if (ButcherableCyberneticPart.Cybernetics.Contains(CyberneticObject))
            {
                ButcherableCyberneticPart.BaseSuccessChance = SuccessChance;
                return true;
            }
            return false;
        }
        public bool EmbedButcherableCybernetics()
        {
            bool any = false;

            any = EmbedButcherableCyberneticsList() || any;
            any = EmbedButcherableCyberneticsTable() || any;
            any = EmbedButcherableCyberneticsForLimb() || any;

            if (any && UseImplantedAdjectiveIfImplanted)
            {
                var cyberneticsHasRandomImplants = new CyberneticsHasRandomImplants();
                ParentObject.RequirePart<DisplayNameAdjectives>().AddAdjective(cyberneticsHasRandomImplants.Adjective);
            }

            return any;
        }

        // List Only
        public static bool EmbedButcherableCyberneticsList(GameObject Corpse, string Cybernetics)
        {
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                /*
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsList) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? NULL) + ", " +
                    nameof(Cybernetics) + ": \"" + Cybernetics + "\"");
                */
                butcherableCybernetic.Cybernetics ??= new();

                if (!Cybernetics.IsNullOrEmpty())
                {
                    foreach (string cyberneticBlueprint in Cybernetics.CachedCommaExpansion())
                    {
                        // UnityEngine.Debug.Log("    " + nameof(cyberneticBlueprint) + ": " + cyberneticBlueprint);
                        if (GameObject.Create(cyberneticBlueprint) is GameObject cyberneticObject)
                        {
                            any = EmbedCyberneticWithSuccessChance(butcherableCybernetic, cyberneticObject, 100);
                        }
                    }
                }
                // UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsList()
        {
            return EmbedButcherableCyberneticsList(ParentObject, Cybernetics);
        }

        // Table Only
        public static bool EmbedButcherableCyberneticsTable(GameObject Corpse, string Table, string Count)
        {
            Count ??= "0";
            int rolledCount = Stat.RollCached(Count);
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                /*
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsTable) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? NULL) + ", " +
                    nameof(Table) + ": \"" + Table + "\"" + ", " +
                    nameof(rolledCount) + ": " + rolledCount);
                */
                butcherableCybernetic.Cybernetics ??= new();

                if (!Table.IsNullOrEmpty())
                {
                    bool processTable = Table.Contains("~#~");
                    string processedTable = Table.Replace("~#~", Stat.RollCached("1d8").ToString());

                    if (processTable)
                    {
                        // UnityEngine.Debug.Log(nameof(processTable) + ": \"" + processTable + "\"");
                    }

                    int maxAtempts = 5;
                    for (int i = 0; i < rolledCount; i++)
                    {
                        for (int j = 0; j < maxAtempts; j++)
                        {
                            if (PopulationManager.CreateOneFrom(processedTable) is GameObject cyberneticObject)
                            {
                                if (!cyberneticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                                {
                                    MetricsManager.LogModError(
                                        mod: Utils.ThisMod,
                                        Message: nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + " " +
                                            "pulled non-cybernetic " + (cyberneticObject?.DebugName ?? NULL) + " from table " + Table);
                                    cyberneticObject.Obliterate();
                                }
                                else
                                {
                                    any = EmbedCyberneticWithSuccessChance(butcherableCybernetic, cyberneticObject, 100);
                                    break;
                                }
                            }
                        }
                    }
                }
                // UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsTable()
        {
            return EmbedButcherableCyberneticsTable(ParentObject, Table, Count);
        }

        // ForLimb Only
        public static bool EmbedButcherableCyberneticsForLimb(GameObject Corpse, string ForLimb, string Count)
        {
            Count ??= "0";
            int rolledCount = Stat.RollCached(Count);
            bool any = false;
            if (TryGetButcherableCyberneticPart(Corpse, out CyberneticsButcherableCybernetic butcherableCybernetic))
            {
                /*
                UnityEngine.Debug.Log(
                    nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + "." + nameof(EmbedButcherableCyberneticsForLimb) + ", " +
                    nameof(Corpse) + ": " + (Corpse?.DebugName ?? NULL) + ", " +
                    nameof(ForLimb) + ": \"" + (ForLimb ?? NULL)+ "\"" + ", " +
                    nameof(rolledCount) + ": " + rolledCount);
                */
                butcherableCybernetic.Cybernetics ??= new();

                if (!ForLimb.IsNullOrEmpty())
                {
                    SanitizeLimbType(ForLimb, out ForLimb, Wildcards);
                    for (int i = 0; i < rolledCount; i++)
                    {
                        if (GameObject.Create(EncountersAPI.GetAnItemBlueprint(b => IsBlueprintForLimb(b, ForLimb))) is GameObject cyberneticObject)
                        {
                            if (!cyberneticObject.TryGetPart(out CyberneticsBaseItem cyberneticsBaseItem))
                            {
                                MetricsManager.LogModError(
                                    mod: Utils.ThisMod,
                                    Message: nameof(UD_FleshGolems_HasCyberneticsButcherableCybernetic) + " " +
                                        "pulled non-cybernetic " + (cyberneticObject?.DebugName ?? NULL) + " " +
                                        nameof(ForLimb) + " " + ForLimb);
                                cyberneticObject.Obliterate();
                            }
                            else
                            {
                                any = EmbedCyberneticWithSuccessChance(butcherableCybernetic, cyberneticObject, 100);
                                break;
                            }
                        }
                    }
                }
                // UnityEngine.Debug.Log("    [" + (any ? TICK : CROSS) + "] " + (any ? "Success" : "Fail") + "!");
            }
            return any;
        }
        public bool EmbedButcherableCyberneticsForLimb()
        {
            return EmbedButcherableCyberneticsForLimb(ParentObject, ForLimb, Count);
        }

        public static bool SetCommerceValue(GameObject Corpse, double Commerce)
        {
            if (Commerce > -1.0)
            {
                Corpse.RequirePart<Commerce>().Value = Commerce;
                return true;
            }
            return false;
        }
        public bool SetCommerceValue(double Commerce)
        {
            return SetCommerceValue(ParentObject, Commerce);
        }
        public static void BlurValue(GameObject Corpse, int BlurValueAmount)
        {
            if (Corpse.RequirePart<Commerce>() is var corpseCommerce)
            {
                if (BlurValueAmount > 0)
                {
                    corpseCommerce.BlurValue(BlurValueAmount);
                }
            }
        }
        public void BlurValue(int BlurValueAmount)
        {
            BlurValue(ParentObject, BlurValueAmount);
        }
        public static void BlurValue(GameObject Corpse, double BlurValueMargin)
        {
            if (Corpse.RequirePart<Commerce>() is var corpseCommerce)
            {
                if (BlurValueMargin > 0.0)
                {
                    corpseCommerce.BlurValue(BlurValueMargin);
                }
            }
        }
        public void BlurValue(double BlurValueMargin)
        {
            BlurValue(ParentObject, BlurValueMargin);
        }
        public static void BlurValue(GameObject Corpse, int BlurValueAmount, double BlurValueMargin)
        {
            BlurValue(Corpse, BlurValueAmount);
            BlurValue(Corpse, BlurValueMargin);
        }
        public void BlurValue(int BlurValueAmount, double BlurValueMargin)
        {
            BlurValue(BlurValueAmount);
            BlurValue(BlurValueMargin);
        }
        public static void AdjustCommerceValue(GameObject Corpse, double Commerce, int BlurValueAmount, double BlurValueMargin)
        {
            SetCommerceValue(Corpse, Commerce);
            BlurValue(Corpse, BlurValueAmount, BlurValueMargin);
        }
        public void AdjustCommerceValue(double Commerce, int BlurValueAmount, double BlurValueMargin)
        {
            AdjustCommerceValue(ParentObject, Commerce, BlurValueAmount, BlurValueMargin);
        }

        public override bool WantEvent(int ID, int Cascade)
        {
            return base.WantEvent(ID, Cascade)
                || ID == EnvironmentalUpdateEvent.ID
                || ID == TakenEvent.ID
                || ID == AfterObjectCreatedEvent.ID;
        }
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
            if (ParentObject == E.Object)
            {
                if (EmbedButcherableCybernetics())
                {
                    GameObjectBlueprint randomCreatureBlueprint = GetCreatureBlueprintFromSpec(Tag: "UD_FleshGolems_NonRobot", Base: Base);
                    randomCreatureBlueprint ??= EncountersAPI.GetACreatureBlueprintModel(bp => bp.HasTagOrProperty("UD_FleshGolems_NonRobot") && Base == null || bp.InheritsFrom(Base));
                    if (randomCreatureBlueprint != null
                        && ParentObject.Render is Render render)
                    {
                        render.DisplayName = render.DisplayName.Replace(" ", " " + randomCreatureBlueprint.DisplayName() + " ");
                    }
                    if (UseImplantedAdjectiveIfImplanted)
                    {
                        var cyberneticsHasRandomImplants = new CyberneticsHasRandomImplants();
                        E.Object.RequirePart<DisplayNameAdjectives>().AddAdjective(cyberneticsHasRandomImplants.Adjective);
                    }
                    ParentObject.RemovePart<Food>();
                    AdjustCommerceValue(Commerce, BlurValueAmount, BlurValueMargin);
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
