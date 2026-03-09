using System;
using System.Collections.Generic;

using ConsoleLib.Console;

using Qud.API;

using XRL.Core;
using XRL.Rules;
using XRL.UI;
using XRL.World.Effects;
using XRL.World.ObjectBuilders;
using XRL.World.Capabilities;

using UD_FleshGolems;

using static UD_FleshGolems.Const;
using XRL.World.Parts.Mutation;
using System.Linq;

namespace XRL.World.Parts
{
    [Serializable]
    public class UD_FleshGolems_DestinedForReanimation : IScribedPart
    {
        public GameObject Corpse;

        public bool BuiltToBeReanimated;

        public bool Attempted;

        private List<int> FailedToRegisterEvents;

        public static bool HaveFakedDeath = false;

        public bool PlayerWantsFakeDie;

        public UD_FleshGolems_DestinedForReanimation()
        {
            Corpse = null;
            BuiltToBeReanimated = false;
            Attempted = false;
            FailedToRegisterEvents = new();
            PlayerWantsFakeDie = false;
        }

        public static bool FakeDeath(
            GameObject Dying,
            GameObject Killer = null,
            GameObject Weapon = null,
            GameObject Projectile = null,
            bool Accidental = false,
            bool AlwaysUsePopups = false,
            string KillerText = null,
            string Reason = null,
            string ThirdPersonReason = null,
            bool DoFakeMessage = true,
            bool DoJournal = true,
            bool DoAchievement = false
            )
        {
            if (Dying == null)
                return false;

            AfterDieEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            Dying.StopMoving();

            KilledPlayerEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            string deathMessage = "You died.\n\n You were " + (Reason ?? The.Game.DeathReason) + ".";
            string deathCategory = The.Game.DeathCategory;
            Dictionary<string, Renderable> deathIcons = CheckpointingSystem.deathIcons;
            string deathMessageTitle = "";
            if (deathMessage.Contains("."))
            {
                int titleSubstring = deathMessage.IndexOf('.') + 1;
                int messageSubstring = deathMessage.IndexOf('.') + 2;
                deathMessageTitle = deathMessage[..titleSubstring];
                deathMessage = deathMessage[messageSubstring..];
            }
            Renderable deathIcon = null;
            if (!deathCategory.IsNullOrEmpty() && deathIcons.ContainsKey(deathCategory))
            {
                deathMessage = deathMessage.Replace("You died.", "");
                deathIcon = deathIcons[deathCategory];
            }
            if (DoFakeMessage
                && (Dying.IsPlayer()
                    || Dying.IsPlayerDuringWorldGen()))
            {
                Popup.ShowSpace(
                    Message: deathMessage,
                    Title: deathMessageTitle,
                    Sound: "Sounds/UI/ui_notification_death",
                    AfterRender: deathIcon,
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "DeathMessage");

                IRenderable playerIcon = Dying.RenderForUI();
                Popup.ShowSpace(
                    Message: "... and yet...\n\n=ud_nbsp:12=...You don't {{UD_FleshGolems_reanimated|relent}}.".StartReplace().ToString(),
                    AfterRender: deathIcon != null ? (Renderable)playerIcon : null,
                    LogMessage: true,
                    ShowContextFrame: deathIcon != null,
                    PopupID: "DeathMessage");
            }

            string deathReason = Reason ?? The.Game.DeathReason ?? deathCategory;
            if (!deathReason.IsNullOrEmpty())
                deathReason = deathReason[0].ToString().ToLower() + deathReason.Substring(1);

            if (DoJournal && !deathReason.IsNullOrEmpty() && The.Player != null && Dying.IsPlayer())
            {
                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " + deathReason?.Replace("!", "."),
                    muralText: "",
                    gospelText: "");

                JournalAPI.AddAccomplishment(
                    text: "On the " + Calendar.GetDay() + " of " + Calendar.GetMonth() + ", " +
                        "you returned from the great beyond.",
                    muralText: "O! Fancieth way to say! Thou hatheth returned whence the thin-veil twixt living and the yonder!",
                    gospelText: "You just, sorta... woke back up from dying...");
            }

            if (DoAchievement && Dying.IsPlayer())
                Achievement.DIE.Unlock();


            WeaponUsageTracking.TrackKill(
                Actor: Killer,
                Defender: Dying,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental);

            DeathEvent.Send(
                Dying: Dying,
                Killer: Killer,
                Weapon: Weapon,
                Projectile: Projectile,
                Accidental: Accidental,
                AlwaysUsePopups: AlwaysUsePopups,
                KillerText: KillerText,
                Reason: Reason,
                ThirdPersonReason: ThirdPersonReason);

            return true;
        }
        public static bool FakeDeath(
            GameObject Dying,
            IDeathEvent E,
            bool DoFakeMessage = true,
            bool DoJournal = true,
            bool DoAchievement = false
            )
            => FakeDeath(
                Dying: Dying,
                Killer: E?.Killer,
                Weapon: E?.Weapon,
                Projectile: E?.Projectile,
                Accidental: E == null || E.Accidental,
                AlwaysUsePopups: E == null || E.AlwaysUsePopups,
                KillerText: E?.KillerText,
                Reason: E?.Reason,
                ThirdPersonReason: E?.ThirdPersonReason,
                DoFakeMessage: DoFakeMessage,
                DoJournal: DoJournal,
                DoAchievement: DoAchievement);

        public bool FakeDeath(IDeathEvent E)
            => FakeDeath(ParentObject, E);

        public bool FakeDeath()
            => FakeDeath(null);

        public static bool FakeRandomDeath(GameObject Dying, int ChanceRandomKiller = 50, bool DoAchievement = false)
        {
            GameObject killer = null;
            GameObject weapon = null;
            GameObject projectile = null;
            try
            {
                if (ChanceRandomKiller.in100())
                {
                    if (!1.in10())
                        killer = GameObject.CreateSample(EncountersAPI.GetACreatureBlueprint());
                    else
                        killer = HeroMaker.MakeHero(GameObject.CreateSample(EncountersAPI.GetALegendaryEligibleCreatureBlueprint()));
                }
                if (killer != null)
                {
                    GameObjectBlueprint weaponBlueprint = EncountersAPI.GetAnItemBlueprintModel(
                        bp => (bp.InheritsFrom("MeleeWeapon") && !bp.InheritsFrom("Projectile"))
                        || bp.InheritsFrom("BaseMissileWeapon")
                        || bp.InheritsFrom("BaseThrownWeapon"));
                    weapon = GameObject.CreateSample(weaponBlueprint.Name);
                    if (weaponBlueprint.InheritsFrom("BaseMissileWeapon"))
                    {
                        if (weaponBlueprint.TryGetPartParameter(
                                PartName: nameof(MagazineAmmoLoader),
                                ParameterName: nameof(MagazineAmmoLoader.ProjectileObject),
                                Result: out string projectileObject))
                            projectile = GameObject.CreateSample(projectileObject);
                        else
                        if (weaponBlueprint.TryGetPartParameter(
                                PartName: nameof(MagazineAmmoLoader),
                                ParameterName: nameof(MagazineAmmoLoader.AmmoPart),
                                Result: out string ammoPart))
                            projectile = GameObject.CreateSample(EncountersAPI.GetAnItemBlueprint(GO => GO.HasPart(ammoPart)));
                    }
                }
                string reason = CheckpointingSystem.deathIcons.Keys.GetRandomElement();
                bool accidental = Stat.RollCached("1d2") == 1;

                bool deathFaked = FakeDeath(
                    Dying: Dying,
                    Killer: killer,
                    Weapon: weapon,
                    Projectile: projectile,
                    Accidental: accidental,
                    Reason: reason,
                    ThirdPersonReason: reason,
                    DoAchievement: DoAchievement);

                killer?.Obliterate();
                weapon?.Obliterate();
                projectile?.Obliterate();

                return deathFaked;
            }
            finally
            {
                if (GameObject.Validate(ref killer))
                    killer?.Obliterate();

                if (GameObject.Validate(ref weapon))
                    weapon?.Obliterate();

                if (GameObject.Validate(ref projectile))
                    projectile?.Obliterate();
            }
        }
        public bool FakeRandomDeath(int ChanceRandomKiller = 50, bool DoAchievement = false)
        {
            return FakeRandomDeath(
                Dying: ParentObject,
                ChanceRandomKiller: ChanceRandomKiller,
                DoAchievement: DoAchievement);
        }

        public bool ProcessObjectCreationEvent(IObjectCreationEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && ParentObject is GameObject entity
                && entity == E.Object
                && (Corpse != null
                    || UD_FleshGolems_Reanimated.TryProduceCorpse(entity, out Corpse))
                && Corpse.TryGetPart(out UD_FleshGolems_CorpseReanimationHelper reanimationHelper))
            {
                if (!entity.IsPlayer()
                    && !entity.IsPlayerDuringWorldGen())
                {
                    reanimationHelper.Animate();
                    E.ReplacementObject = Corpse;
                    Attempted = true;
                }

                if (Attempted)
                    return true;
            }
            return false;
        }

        public static bool IsDyingCreatureCorpse(GameObject Dying, out GameObject Corpse)
        {
            Corpse = null;
            if (Dying.HasPart<Corpse>()
                && Dying.GetDropInventory() is Inventory dropInventory)
            {
                GameObject bestMatch = null;
                GameObject secondBestMatch = null;
                GameObject thirdBestMatch = null;
                foreach (GameObject dropItem in dropInventory.GetObjects())
                {
                    if (!dropItem.GetBlueprint().InheritsFrom("Corpse"))
                    {
                        continue;
                    }
                    if (Dying.ID == dropItem.GetStringProperty("SourceID"))
                    {
                        Corpse = dropItem;
                        break;
                    }
                    if (Dying.Blueprint == dropItem.GetStringProperty("SourceBlueprint"))
                    {
                        if (bestMatch != null)
                        {
                            secondBestMatch ??= dropItem;
                            continue;
                        }
                        bestMatch ??= dropItem;
                    }
                    if (Dying.GetSpecies() == dropItem.Blueprint.Replace(" Corpse", "").Replace("UD_FleshGolems ", ""))
                    {
                        if (secondBestMatch != null)
                        {
                            thirdBestMatch ??= dropItem;
                            continue;
                        }
                        secondBestMatch ??= dropItem;
                    }
                }
                Corpse ??= bestMatch ?? secondBestMatch ?? thirdBestMatch;
            }
            return Corpse != null;
        }
        public bool IsDyingCreatureCorpse(GameObject Dying)
            => IsDyingCreatureCorpse(Dying, out _);

        public bool ActuallyDoTheFakeDieAndReanimate()
        {
            if (ParentObject is not GameObject player
                || !PlayerWantsFakeDie
                || HaveFakedDeath
                || (!player.IsPlayer()
                    && !player.IsPlayerDuringWorldGen()))
                return false;

            bool success = UD_FleshGolems_Reanimated.ReplaceEntityWithCorpse(
                Entity: player,
                FakeDeath: PlayerWantsFakeDie,
                FakedDeath: out HaveFakedDeath,
                DeathEvent: null,
                Corpse: Corpse);

            PlayerWantsFakeDie = false;
            if (success)
                Corpse?.SetStringProperty("OriginalPlayerBody", "Not really, but we pretend!");

            return success;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            int eventOrder = EventOrder.EXTREMELY_LATE + EventOrder.EXTREMELY_LATE;
            try
            {
                Registrar?.Register(BeforeObjectCreatedEvent.ID, -eventOrder);
                Registrar?.Register(EnvironmentalUpdateEvent.ID, -eventOrder);
            }
            catch (Exception x)
            {
                MetricsManager.LogException(nameof(UD_FleshGolems_DestinedForReanimation) + "." + nameof(Register), x, "game_mod_exception");
            }
            finally
            {
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(BeforeObjectCreatedEvent.ID))
                {
                    FailedToRegisterEvents.Add(BeforeObjectCreatedEvent.ID);
                }
                if (ParentObject == null
                    || ParentObject.RegisteredEvents == null
                    || !ParentObject.RegisteredEvents.ContainsKey(EnvironmentalUpdateEvent.ID))
                {
                    FailedToRegisterEvents.Add(EnvironmentalUpdateEvent.ID);
                }
            }
            base.Register(Object, Registrar);
        }
        public override bool WantEvent(int ID, int cascade)
            => base.WantEvent(ID, cascade)
            || (FailedToRegisterEvents.Contains(BeforeObjectCreatedEvent.ID) && ID == BeforeObjectCreatedEvent.ID)
            || (FailedToRegisterEvents.Contains(EnvironmentalUpdateEvent.ID) && ID == EnvironmentalUpdateEvent.ID)
            || ID == GetShortDescriptionEvent.ID
            || ID == BeforeDieEvent.ID
            || ID == GetDebugInternalsEvent.ID
            ;
        public override bool HandleEvent(GetShortDescriptionEvent E)
        {
            string persistanceText =
                ("Something about =subject.objective= gives the sense " +
                "=subject.subjective==subject.verb:'re:afterpronoun= unnaturally relentless...")
                    .StartReplace()
                    .AddObject(E.Object)
                    .ToString();

            if (E.Object.HasTag("VerseDescription"))
                E.Base.AppendLine().AppendLine().Append(persistanceText);
            else
            {
                if (!E.Base.IsNullOrEmpty())
                    E.Base.Append(" ");

                E.Base.Append(persistanceText);
            }
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(EnvironmentalUpdateEvent E)
        {
            if (!Attempted
                && BuiltToBeReanimated
                && PlayerWantsFakeDie
                && ParentObject is GameObject entity
                && (entity.IsPlayer()
                    || entity.IsPlayerDuringWorldGen())
                && !HaveFakedDeath
                && (Corpse != null
                    || UD_FleshGolems_Reanimated.TryProduceCorpse(entity, out Corpse)))
                entity.RegisterPartEvent(this, "GameStart");

            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeObjectCreatedEvent E)
        {
            ProcessObjectCreationEvent(E);
            return base.HandleEvent(E);
        }
        public override bool HandleEvent(BeforeDieEvent E)
        {
            if (!BuiltToBeReanimated
                && ParentObject is GameObject dying
                && dying == E.Dying
                && dying.TryGetPart(out Corpse dyingCorpse)
                && !dyingCorpse.CorpseBlueprint.IsNullOrEmpty()
                && dying.IsPlayer()
                && (!PlayerWantsFakeDie || !HaveFakedDeath)
                && UD_FleshGolems_Reanimated.ReplaceEntityWithCorpse(
                    Entity: ParentObject,
                    FakeDeath: PlayerWantsFakeDie,
                    FakedDeath: out HaveFakedDeath,
                    DeathEvent: E,
                    Corpse: Corpse))
                return false;

            return base.HandleEvent(E);
        }
        public override bool FireEvent(Event E)
        {
            if (E.ID == "GameStart"
                && !HaveFakedDeath
                && BuiltToBeReanimated
                && PlayerWantsFakeDie
                && !ActuallyDoTheFakeDieAndReanimate())
                if (UD_FleshGolems_Reanimated.PerformAFakeDeath(ParentObject, Corpse, null))
                    if (ParentObject.GetBlueprint() is GameObjectBlueprint objectBlueprint
                        && (objectBlueprint.InheritsFrom("Corpse")
                            || objectBlueprint.Name == "Corpse"))
                        ParentObject.RemovePart(this);

            return base.FireEvent(E);
        }

        public override bool HandleEvent(GetDebugInternalsEvent E)
        {
            E.AddEntry(this, nameof(Corpse), Corpse?.DebugName ?? NULL);
            E.AddEntry(this, nameof(BuiltToBeReanimated), BuiltToBeReanimated);
            E.AddEntry(this, nameof(Attempted), Attempted);
            if (!FailedToRegisterEvents.IsNullOrEmpty())
                E.AddEntry(this, nameof(FailedToRegisterEvents),
                    FailedToRegisterEvents
                    ?.ConvertAll(id => MinEvent.EventTypes.ContainsKey(id) ? MinEvent.EventTypes[id].ToString() : "Error")
                    ?.Aggregate("", (a,n) => a + (!a.IsNullOrEmpty() ? "\n" : null) + n));
            else
                E.AddEntry(this, nameof(FailedToRegisterEvents), "Empty");

            E.AddEntry(this, nameof(PlayerWantsFakeDie), PlayerWantsFakeDie);
            return base.HandleEvent(E);
        }
    }
}
