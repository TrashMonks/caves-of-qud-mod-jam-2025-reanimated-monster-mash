using System;
using System.Text;
using XRL.World.Effects;
using XRL.World.Parts.Skill;

namespace XRL.World.Parts
{

    [Serializable]
    public class AricMJMCleaveonHit : IPart
    {
        public int Chance = 75;

        public bool CriticalOnly;

        public AricMJMCleaveonHit()
        {
        }

        public AricMJMCleaveonHit(int Chance)
            : this()
        {
            this.Chance = Chance;
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade))
            {
                return ID == GetShortDescriptionEvent.ID;
            }
            return true;
        }

        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            E.Postfix.AppendRules(AppendEffect);
            return base.HandleEvent(E);
        }

        public void AppendEffect(StringBuilder SB)
        {
            if (Chance > 0)
            {
                if (Chance < 100)
                {
                    SB.Append(Chance).Append("% chance to cleave ");
                }
                else
                {
                    SB.Append("Cleaves ");
                }

                    SB.Append("mental ");
                
                SB.Append("armor");
                if (CriticalOnly)
                {
                    SB.Compound("on critical hit");
                }
            }
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            if (Object.IsCreature)
            {
                Registrar.Register("AttackerHit");
                return;
            }
            if (Object.IsProjectile)
            {
                Registrar.Register("ProjectileHit");
                return;
            }
            Registrar.Register("WeaponHit");
            Registrar.Register("WeaponThrowHit");
        }

        public static void AricMJMPerformCleave(GameObject Attacker, GameObject Defender, GameObject Weapon, string Skill = null, string Properties = null, int Chance = 75, int AdjustAVPenalty = 0, int? MaxAVPenalty = null)
        {
            if (Attacker == null || Defender == null || Weapon == null || Defender.HasPart<Gas>() || Defender.HasPart<NoDamage>())
            {
                return;
            }
            MeleeWeapon part = Weapon.GetPart<MeleeWeapon>();
            if (Skill != null && (part == null || part.Skill != Skill))
            {
                return;
            }
            string stat = part.Stat;
            string name;
            string text;
            string text2;
            string equipmentEffectName;
            string iD;

                name = "MA";
                text = "mental armor";
                text2 = "ShatterMentalArmor";
                equipmentEffectName = null;
                iD = "CanApplyShatterMentalArmor";
            

            if (Defender == null || !Defender.HasStat(name) || !Defender.FireEvent(iD) || !CanApplyEffectEvent.Check(Defender, text2))
            {
                return;
            }
            int num = Attacker.StatMod(stat);
            int num2;
            if (!MaxAVPenalty.HasValue)
            {
                num2 = num / 2 + AdjustAVPenalty;
                if (num % 2 == 1)
                {
                    num2++;
                }
            }
            else
            {
                num2 = MaxAVPenalty.Value + AdjustAVPenalty;
            }
            if (num2 < 1)
            {
                num2 = 1;
            }
            int num3 = 1;
            bool flag = Properties != null && Properties.HasDelimitedSubstring(',', "Charging") && Attacker.HasSkill("Cudgel_ChargingStrike");
            if (flag)
            {
                num3++;
            }
            int @for = GetCleaveAmountEvent.GetFor(Weapon, Attacker, Defender, num3);
            if (@for <= 0)
            {
                return;
            }
            if (num2 < @for)
            {
                num2 = @for;
            }
            num2 = GetCleaveMaxPenaltyEvent.GetFor(Weapon, Attacker, Defender, num2, flag);
            if (num2 <= 0)
            {
                return;
            }
            Statistic stat2 = Defender.GetStat(name);
            int num4 = Axe_Cleave.GetCurrentPenalty(Defender, text2, equipmentEffectName);
            if (stat2.Value <= stat2.Min || num4 >= num2)
            {
                return;
            }
            GetSpecialEffectChanceEvent.GetFor(Attacker, Weapon, "Skill Cleave", Chance, Defender);
            if (!Chance.in100())
            {
                return;
            }
            bool flag2 = false;
            if (Activator.CreateInstance(ModManager.ResolveType("XRL.World.Effects." + text2)) is IShatterEffect shatterEffect)
            {
                shatterEffect.Duration = 300;
                shatterEffect.SetOwner(Attacker);
                for (int i = 0; i < @for; i++)
                {
                    if (num4 >= num2 - 1)
                    {
                        break;
                    }
                    shatterEffect.IncrementPenalty();
                    num4++;
                }
                flag2 = Defender.ApplyEffect(shatterEffect);
            }
            if (!flag2)
            {
                return;
            }
            if (flag)
            {
                if (Attacker.IsPlayer())
                {
                    IComponent<GameObject>.AddPlayerMessage("The momentum from your charge causes your " + Weapon.ShortDisplayNameSingle + " to cleave deeper through " + Defender.poss(text) + ".", 'g');
                }
                Defender.DustPuff("&c");
            }
            if (Attacker.IsPlayer())
            {
                IComponent<GameObject>.AddPlayerMessage("You cleave through " + Defender.poss(text) + ".", 'G');
            }
            else if (Defender.IsPlayer())
            {
                IComponent<GameObject>.AddPlayerMessage(Attacker.Does("cleave") + " through your " + text + ".", 'R');
            }
            else if (IComponent<GameObject>.Visible(Defender))
            {
                if (Defender.IsPlayerLed())
                {
                    IComponent<GameObject>.AddPlayerMessage(Attacker.Does("cleave") + " through " + Defender.poss(text) + ".", 'r');
                }
                else
                {
                    IComponent<GameObject>.AddPlayerMessage(Attacker.Does("cleave") + " through " + Defender.poss(text) + ".", 'g');
                }
            }
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID.EndsWith("Hit"))
            {
                bool flag = E.HasFlag("Critical");
                if (CriticalOnly && !flag)
                {
                    return base.FireEvent(E);
                }
                AricMJMPerformCleave(E.GetGameObjectParameter("Attacker"), E.GetGameObjectParameter("Defender"), ParentObject, null, Chance: (!CriticalOnly && flag) ? 100 : Chance, Properties: E.GetStringParameter("Properties"), MaxAVPenalty: 5);
            }
            return base.FireEvent(E);
        }
    }
}
