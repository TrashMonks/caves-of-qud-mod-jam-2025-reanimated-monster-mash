using System;

using XRL;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;

using static UD_FleshGolems.Const;
using XRL.World;

namespace UD_FleshGolems
{
    [HasVariableReplacer]
    public static class Utils
    {
        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

        public static string GetPlayerBlueprint()
        {
            if (EmbarkBuilder.gameObject.GetComponent<EmbarkBuilder>() is not EmbarkBuilder builder)
                return "Humanoid";

            var body = (builder.GetModule<QudGenotypeModule>()?.data?.Entry?.BodyObject)
                .Coalesce(builder.GetModule<QudSubtypeModule>()?.data?.Entry?.BodyObject)
                .Coalesce("Humanoid");

            return (builder.info?.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, body))
                .Coalesce(body);
        }

        [VariableReplacer]
        public static string ud_nbsp(DelegateContext Context)
        {
            string nbsp = "\xFF";
            string output = nbsp;
            if (!Context.Parameters.IsNullOrEmpty() && int.TryParse(Context.Parameters[0], out int count))
            {
                for (int i = 1; i < count; i++)
                {
                    output += nbsp;
                }
            }
            return output;
        }

        public static string AppendTick(string String, bool WithSpaceAfter = true)
            => String + "[" + TICK + "]" + (WithSpaceAfter ? " " : "");

        public static string AppendCross(string String, bool WithSpaceAfter = true)
            => String + "[" + CROSS + "]" + (WithSpaceAfter ? " " : "");

        public static string AppendYehNah(string String, bool Yeh, bool WithSpaceAfter = true)
            => Yeh
            ? AppendTick(String, WithSpaceAfter)
            : AppendCross(String, WithSpaceAfter)
            ;

        public static int CapDamageTo1HPRemaining(GameObject Creature, int DamageAmount)
            => Creature?.GetStat("Hitpoints") is Statistic hitpoints
            ? Math.Clamp(hitpoints.Value - 1, 0, DamageAmount)
            : 0;
    }
}