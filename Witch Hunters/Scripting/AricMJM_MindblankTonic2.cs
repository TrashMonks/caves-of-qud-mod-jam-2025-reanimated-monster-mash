using System;
using XRL.World.Parts;

namespace XRL.World.Parts
{

    [Serializable]
    public class AricMJM_MindBlank_Tonic_Applicator : IPart
    {

        public override bool WantEvent(int ID, int cascade)
        {
            return
                base.WantEvent(ID, cascade)
                || ID == GetUtilityScoreEvent.ID
            ;
        }

        public override bool HandleEvent(GetUtilityScoreEvent E)
        {
            if (E.Damage == null)
            {
                if (E.Actor.HasEffect<Effects.Terrified>())
                {
                    E.ApplyScore(100);
                }
                else
                {
                    double health = E.Actor.Health();
                    if (health < 0.2)
                    {
                        E.ApplyScore((int)(1 / health));
                    }
                }
            }
            return base.HandleEvent(E);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("ApplyTonic");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "ApplyTonic")
            {
                int dosage = E.GetIntParameter("Dosage");
                if (dosage <= 0)
                {
                    return false;
                }
                GameObject actor = E.GetGameObjectParameter("Actor");
                GameObject subject = E.GetGameObjectParameter("Subject");
                int duration = GetTonicDurationEvent.GetFor(
                    Object: ParentObject,
                    Actor: actor,
                    Subject: subject,
                    Type: "AricMJM_MindBlank",
                    BaseDuration: Rules.Stat.Random(dosage, 10 * dosage) + (40 * dosage),
                    Dosage: dosage
                );
                var fx = new Effects.AricMJM_MindBlank_Tonic(duration);
                if (!subject.ApplyEffect(fx))
                {
                    return false;
                }
            }
            return base.FireEvent(E);
        }

    }

}

namespace XRL.World.Effects
{


    [Serializable]
    public class AricMJM_MindBlank_Tonic_Allergy : Effect
    {
        public AricMJM_MindBlank_Tonic_Allergy()
        {
            DisplayName = "{{white|stupefied}}";
        }

        public AricMJM_MindBlank_Tonic_Allergy(int duration) : this()
        {
            Duration = duration;
        }

        public override int GetEffectType()
        {
            return TYPE_METABOLIC | TYPE_MINOR | TYPE_NEGATIVE | TYPE_REMOVABLE;
        }

        public override bool UseStandardDurationCountdown()
        {
            return true;
        }

        public override string GetDescription()
        {
            return "{{white|stupedfied}}";
        }

        public override bool Apply(GameObject Object)
        {
            Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_enraged");

            AricMJM_MindBlank_Tonic_Allergy FX = Object.GetEffect<AricMJM_MindBlank_Tonic_Allergy>();
            if (FX != null)
            {
                if (FX.Duration < Duration)
                {
                    FX.Duration = Duration;
                }
                return false;
            }

            return true;
        }

        public override void Remove(GameObject Object)
        {
        }

    }

    [Serializable]
    public class AricMJM_MindBlank_Tonic : ITonicEffect, ITierInitialized
    {
        bool bOverdosed = false;
        int BonusHP = 0;
        float HPLoss = 0;

        [NonSerialized] private int TempHPPenalty = 0;

        public AricMJM_MindBlank_Tonic()
        {
        }

        public AricMJM_MindBlank_Tonic(int Duration) : this()
        {
            this.Duration = Duration;
        }

        public void Initialize(int Tier)
        {
            if (HistoryKit.If.CoinFlip()) bOverdosed = true;
            Duration = Rules.Stat.Roll(41, 50);
        }

        public override bool SameAs(Effect e)
        {
            return false;
        }

        public override bool UseStandardDurationCountdown()
        {
            return true;
        }

        public override string GetDescription()
        {
            return "{{white|mindblank}} tonic";
        }

        public override string GetDetails()
        {
            if (Object.IsTrueKin())
            {
                return "grants a mental shield\nPsychic attacks have no effect";
            }
            else
            {
                return "+6 MA\n-6 Ego";
            }
        }

        public override bool Apply(GameObject Object)
        {
            bool result = true;
            AricMJM_MindBlank_Tonic FX = Object.GetEffect<AricMJM_MindBlank_Tonic>();
            if (FX != null)
            {
                if (FX.Duration < Duration)
                {
                    FX.Duration = Duration;
                }
                result = false;
            }
            else
            {
                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_bewilderment");
                if (Object.IsPlayer())
                {
                    UI.Popup.Show("Your eyes glaze as a phlegmatic calm befalls the swirling sea of your mind.");
                }
                ApplyChanges();
            }
            
            return result;
        }

        public override void Remove(GameObject Object)
        {
            if (Object.IsPlayer())
            {
                UI.Popup.Show("Your preternatural focus slips, and you once agin share your mind with your thoughts.");
            }
            UnapplyChanges();
        }

        private void ApplyChanges()
        {

            if (Object.IsTrueKin())
            {

                Object.AddPart(new MentalShield());

            }
            else
            {
                StatShifter.SetStatShift(Object, "MA", 6);
                StatShifter.SetStatShift(Object, "Ego", -6);
            }

        }

        private void UnapplyChanges()
        {
            if (Object.IsTrueKin())
            {

                Object.RemovePart(typeof(MentalShield));

            }
            else
            {
                StatShifter.SetStatShift(Object, "MA", -6);
                StatShifter.SetStatShift(Object, "Ego", +6);
            }
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return
                base.WantEvent(ID, cascade)
                || ID == BeginTakeActionEvent.ID
            ;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register("Overdose");
            base.Register(Object, Registrar);
        }

        public override void ApplyAllergy(GameObject Object)
        {
            if (Object.GetLongProperty("Overdosing") == 1)
            {
                UI.Popup.Show("Your mutant physiology reacts adversely to the tonic. Your mind swims");
            }
            else
            {
                UI.Popup.Show("The tonics you ingested react adversely to each other. Your mind swims");
            }
            Object.ApplyEffect(new AricMJM_MindBlank_Tonic_Allergy(Duration));
        }

        public int x, y;
        public string zone;

        public override bool FireEvent(Event E)
        {

            if (E.ID == "Overdose")
            {
                if (Duration > 0)
                {
                    if (Object.IsPlayer())
                    {

                        if (Object.GetLongProperty("Overdosing") == 1)
                        {
                            UI.Popup.Show("Your mutant physiology reacts adversely to the tonic. Your mind swims.");
                        }
                        else
                        {
                            UI.Popup.Show("The tonics you ingested react adversely to each other. Your mind swims.");
                        }

                        bOverdosed = true;
                        Object.ModIntProperty("ConfusionLevel", 10);

                    }
                }
            }
            else
            if (E.ID == "BeforeDeepCopyWithoutEffects")
            {
                UnapplyChanges();
            }
            else
            if (E.ID == "AfterDeepCopyWithoutEffects")
            {
                ApplyChanges();
            }
            return base.FireEvent(E);
        }

        public override bool Render(RenderEvent E)
        {
            int nFrame = Core.XRLCore.CurrentFrame % 60;
            if (Duration > 0 && nFrame > 35 && nFrame < 45)
            {
                E.Tile = null;
                E.RenderString = "!";

                int i = Rules.Stat.RandomCosmetic(1, 3);
                if (i == 1) E.ColorString = "&G";
            }
            return true;
        }

    }

}
