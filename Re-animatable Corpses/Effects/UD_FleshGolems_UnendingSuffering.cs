using System;

using XRL.Core;
using XRL.Rules;
using XRL.World.Capabilities;
using XRL.World.Conversations;
using XRL.World.Parts;
using XRL.World.ObjectBuilders;

using UD_FleshGolems;
using static UD_FleshGolems.Const;
using static UD_FleshGolems.Utils;

using SerializeField = UnityEngine.SerializeField;

namespace XRL.World.Effects
{
    [HasConversationDelegate]
    [Serializable]
    public class UD_FleshGolems_UnendingSuffering : IScribedEffect, ITierInitialized
    {
        public const string ENDLESSLY_SUFFERING = "{{UD_FleshGolems_reanimated|endlessly suffering}}";

        [SerializeField]
        private int _FrameOffset;
        private int FrameOffset
        {
            get
            {
                if (_FrameOffset <= int.MinValue)
                    return (Object != null && int.TryParse(Object.ID, out int result))
                        ? _FrameOffset = (result % FrameOffsetMod) + 1
                        : Stat.RollCached("1d" + FrameOffsetMod);
                    
                return _FrameOffset;
            }
        }

        [SerializeField]
        private bool? _FlipRenderColors;
        private bool FlipRenderColors
        {
            get
            {
                if (_FlipRenderColors != null)
                return _FlipRenderColors.GetValueOrDefault();

                if (Object != null && int.TryParse(Object.ID, out int result))
                {
                    _FlipRenderColors = (result % 2) == 0;
                    return _FlipRenderColors.GetValueOrDefault();
                }
                return Stat.RollCached("1d2") == 1;
            }
        }

        private bool _ColorLatch;
        private bool ColorLatch
        {
            set
            {
                if (_ColorLatch != value && value)
                {
                    ColorToggle = !ColorToggle;
                }
                _ColorLatch = value;
            }
        }

        private bool ColorToggle;

        private static int FrameMod => 60;
        private static int FrameOffsetMod => 24;

        public static string MeatSufferColor => "R";
        public static string RobotSufferColor => "W";
        public static string PlantSufferColor => "W";
        public static string FungusSufferColor => "B";

        public static int BASE_SMEAR_CHANCE => 5;
        public static int BASE_SPATTER_CHANCE => 2;
        public static int GracePeriodTurns => 2;

        private int GracePeriod;

        public GameObject SourceObject;

        public string Damage;

        public int ChanceToDamage;
        public int ChanceToSmear;
        public int ChanceToSpatter;

        public string SufferColor;

        [SerializeField]
        private int CurrentTier;

        [SerializeField]
        private int CumulativeSuffering;

        public UD_FleshGolems_UnendingSuffering()
        {
            _FrameOffset = int.MinValue;
            _FlipRenderColors = null;

            _ColorLatch = false;
            ColorToggle = false;

            GracePeriod = GracePeriodTurns;

            SourceObject = null;
            Damage = "1";
            ChanceToDamage = 10;
            ChanceToSmear = BASE_SMEAR_CHANCE;
            ChanceToSpatter = BASE_SPATTER_CHANCE;

            DisplayName = ENDLESSLY_SUFFERING;
            Duration = DURATION_INDEFINITE;

            SufferColor = MeatSufferColor;

            CurrentTier = 0;

            CumulativeSuffering = 0;
        }

        public UD_FleshGolems_UnendingSuffering(GameObject Source)
            : this()
        {
            SourceObject = Source;
        }

        public UD_FleshGolems_UnendingSuffering(int Tier)
            : this()
        {
            Initialize(Tier);
        }

        public UD_FleshGolems_UnendingSuffering(GameObject Source, int Tier, int TimesReanimated = 1)
            : this(Source)
        {
            Initialize(Tier, TimesReanimated);
        }

        public UD_FleshGolems_UnendingSuffering(string Damage, int Duration, GameObject Source, int ChanceToSmear, int ChanceToSpatter)
            : this(Source)
        {
            this.Damage = Damage;
            this.ChanceToSmear = ChanceToSmear;
            this.ChanceToSpatter = ChanceToSpatter;

            this.Duration = Duration;
        }

        public bool GetFlipRenderColors()
        {
            if (_FlipRenderColors != null)
                return _FlipRenderColors.GetValueOrDefault();

            if (Object != null && int.TryParse(Object.ID, out int result))
            {
                _FlipRenderColors = (result % 2) == 0;
                return _FlipRenderColors.GetValueOrDefault();
            }
            return Stat.RollCached("1d2") == 1;
        }

        public void Initialize(int Tier, int TimesReanimated = 1)
        {
            Tier = Capabilities.Tier.Constrain(Stat.Random(Tier - 1, Tier + 1));
            
            Damage = Tier switch
            {
                >= 7 => "3-4",
                >= 5 => "2-3",
                >= 3 => "1-2",
                _    => "1d3-2"
            };

            ChanceToDamage = 3 * (1 + Math.Max(1, Tier));

            ChanceToDamage *= Math.Max(1, TimesReanimated);

            ChanceToSmear *= Tier;
            ChanceToSpatter *= Tier;

            if (CurrentTier > 0 && Tier > CurrentTier)
                WorsenedMessage(Object);

            CurrentTier = Tier;
        }

        public override int GetEffectType()
            => TYPE_MENTAL | TYPE_STRUCTURAL | TYPE_NEUROLOGICAL;

        public override string GetDetails()
        {
            string dueToFolly = null;
            if (SourceObject != null)
            {
                dueToFolly += " due to the existential folly of " + SourceObject.GetReferenceDisplayName(Short: true, Stripped: true);
            }
            return ChanceToDamage + "% chance per turn to suffer " + Damage + " damage" + dueToFolly + ".";
        }
        public virtual string DamageAttributes()
            => "Bleeding Unavoidable Suffering";

        public virtual string DamageMessage()
            => "from " + DisplayNameStripped + ".";

        public override bool Apply(GameObject Object)
        {
            StatShifter.SetStatShift(
                target: Object,
                statName: "AcidResistance",
                amount: 200,
                true);

            SufferColor = MeatSufferColor;
            if (Object.TryGetPart(out UD_FleshGolems_PastLife pastLife)
                && GameObjectFactory.Factory.GetBlueprintIfExists(pastLife.Blueprint) is var pastLifeBlueprint)
            {
                if (pastLifeBlueprint.InheritsFrom("Robot"))
                {
                    SufferColor = RobotSufferColor;
                }
                else
                if (pastLifeBlueprint.InheritsFromAny("Plant", "BasePlant", "MutatedPlant", "BaseSlynth"))
                {
                    SufferColor = PlantSufferColor;
                }
                else
                if (pastLifeBlueprint.InheritsFromAny("Fungus", "ActiveFungus", "MutatedFungus"))
                {
                    SufferColor = FungusSufferColor;
                }
            }
            StartMessage(Object);

            return base.Apply(Object);
        }
        public override void Remove(GameObject Object)
        {
            StatShifter.RemoveStatShifts(Object);
            base.Remove(Object);
        }

        public virtual void StartMessage(GameObject Object)
        {
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "begin", Extra: DisplayNameStripped, EndMark: "!", ColorAsBadFor: Object);
            }
        }

        public virtual void WorsenedMessage(GameObject Object)
        {
            if (UD_FleshGolems_Reanimated.HasWorldGenerated)
            {
                Object?.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_physicalRupture");
                DidX(Verb: "start", Extra: DisplayNameStripped + " even worse", EndMark: "!", ColorAsBadFor: Object);
            }
        }

        public void Suffer()
        {
            if (Object == null
                || Object.CurrentCell == null)
                return;

            int chanceToDamage = ChanceToDamage * 100;
            if (Object.CurrentCell.OnWorldMap() || Options.GreatlyReduceSuffering)
                chanceToDamage = (int)Math.Max(1, chanceToDamage * 0.01);

            int chanceToSmear = ChanceToSmear * 100;
            if (Options.GreatlyReduceSuffering)
                chanceToSmear = (int)Math.Max(1, chanceToSmear * 0.01);

            int chanceToSpatter = ChanceToSpatter * 100;
            if (Options.GreatlyReduceSuffering)
                chanceToSpatter = (int)Math.Max(1, chanceToSpatter * 0.01);

            bool tookDamage = false;
            if (chanceToDamage.in10000())
            {
                string oldAutoActSetting = AutoAct.Setting;
                bool isAutoActing = AutoAct.IsActive();

                if (Object.IsPlayerControlled()
                    && isAutoActing)
                    AutoAct.Setting = "";

                string deathMessage = "=subject.name's= unending suffering... well, ended =subject.objective=."
                    .StartReplace()
                    .AddObject(Object)
                    .ToString();

                int damage = CapDamageTo1HPRemaining(Object, Damage.RollCached());
                tookDamage = Object.TakeDamage(
                    Amount: damage,
                    Attributes: DamageAttributes(),
                    Owner: Object,
                    Message: DamageMessage(),
                    DeathReason: deathMessage,
                    ThirdPersonDeathReason: deathMessage,
                    Source: Object,
                    Indirect: true,
                    SilentIfNoDamage: true);

                if (tookDamage)
                    CumulativeSuffering += damage;

                if (Object.IsPlayerControlled()
                    && isAutoActing)
                    AutoAct.Setting = oldAutoActSetting;
            }

            if (Object.CurrentCell is not Cell suferrerCell
                || suferrerCell.OnWorldMap())
                return;

            bool inLiquid = false;
            string bleedLiquid = Object.GetBleedLiquid();

            foreach (GameObject renderdObject in suferrerCell.GetObjectsWithPartReadonly("Render"))
                if (chanceToSmear.in10000())
                {
                    if (renderdObject.LiquidVolume is LiquidVolume liquidVolumeInCell
                        && liquidVolumeInCell.IsOpenVolume())
                    {
                        liquidVolumeInCell.MixWith(new LiquidVolume(bleedLiquid, 2), null, null, null);
                        inLiquid = true;
                    }
                    else
                        renderdObject.MakeBloody(bleedLiquid, Stat.Random(1, 3));
                }

            if (!inLiquid
                && chanceToSpatter.in10000()
                && GameObject.Create("BloodSplash") is GameObject bloodySplashObject)
            {
                if (bloodySplashObject.LiquidVolume is LiquidVolume bloodSplashVolume)
                {
                    bloodSplashVolume.InitialLiquid = bleedLiquid;
                    suferrerCell.AddObject(bloodySplashObject);
                    if (tookDamage)
                        DidX("spatter", "viscous gunk everywhere", "!");
                }
                else
                {
                    MetricsManager.LogError("generated " + bloodySplashObject.Blueprint + " with no " + nameof(LiquidVolume));
                    bloodySplashObject?.Obliterate();
                }
            }
        }

        public string GetForegroundColor()
            => (ColorToggle == !FlipRenderColors) ? "&K" : ("&" + SufferColor);

        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || ID == GetCompanionStatusEvent.ID
            || ID == EndTurnEvent.ID
            || ID == PhysicalContactEvent.ID
            || ID == AfterLevelGainedEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;
        public override bool HandleEvent(GetCompanionStatusEvent E)
        {
            if (E.Object == Object)
                E.AddStatus("suffering", 20);

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EndTurnEvent E)
        {
            if (GracePeriod < 1)
                Suffer();
            else
                GracePeriod--;

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(PhysicalContactEvent E)
        {
            E.Actor.MakeBloody(E.Object.GetBleedLiquid(), Stat.RollCached("2d2-2"));
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(AfterLevelGainedEvent E)
        {
            Initialize(UD_FleshGolems_ReanimatedCorpse.GetTierFromLevel(Object));
            return base.HandleEvent(E);
        }
        public override bool Render(RenderEvent E)
        {
            int currentFrame = (XRLCore.CurrentFrame + FrameOffset) % FrameMod;

            if (currentFrame > 25 && currentFrame < 35)
            {
                ColorLatch = true;
                E.RenderString = "\u0003";
                E.ApplyColors(GetForegroundColor(), ICON_COLOR_PRIORITY);
                return false;
            }
            else
                ColorLatch = false;

            return base.Render(E);
        }
        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(FrameOffset), FrameOffset);
            E.AddEntry(this, nameof(FlipRenderColors), FlipRenderColors);
            E.AddEntry(this, nameof(SourceObject), SourceObject?.DebugName ?? NULL);
            E.AddEntry(this, nameof(Damage), Damage);
            E.AddEntry(this, nameof(CumulativeSuffering), CumulativeSuffering);
            E.AddEntry(this, nameof(ChanceToDamage), ChanceToDamage);
            E.AddEntry(this, nameof(ChanceToSmear), ChanceToSmear);
            E.AddEntry(this, nameof(ChanceToSpatter), ChanceToSpatter);
            E.AddEntry(this, nameof(SufferColor), SufferColor);
            E.AddEntry(this, nameof(CurrentTier), CurrentTier);
            return base.HandleEvent(E);
        }
    }
}
