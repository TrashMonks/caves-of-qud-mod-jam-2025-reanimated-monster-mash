using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using XRL;
using XRL.Language;
using XRL.World.Text.Attributes;
using XRL.World.Text.Delegates;
using XRL.CharacterBuilds;
using XRL.CharacterBuilds.Qud;

using static UD_FleshGolems.Const;
using Options = UD_FleshGolems.Options;

namespace UD_FleshGolems
{
    [HasVariableReplacer]
    public static class Utils
    {
        public static ModInfo ThisMod => ModManager.GetMod(MOD_ID);

        public static string GetPlayerBlueprint()
        {
            if (!EmbarkBuilderConfiguration.activeModules.IsNullOrEmpty())
            {
                foreach (AbstractEmbarkBuilderModule activeModule in EmbarkBuilderConfiguration.activeModules)
                {
                    if (activeModule.type == nameof(QudSpecificCharacterInitModule))
                    {
                        QudSpecificCharacterInitModule characterInit = activeModule as QudSpecificCharacterInitModule;
                        string blueprint = characterInit?.builder?.GetModule<QudGenotypeModule>()?.data?.Entry?.BodyObject
                            ?? characterInit?.builder?.GetModule<QudSubtypeModule>()?.data?.Entry?.BodyObject
                            ?? "Humanoid";
                        return characterInit.builder.info.fireBootEvent(QudGameBootModule.BOOTEVENT_BOOTPLAYEROBJECTBLUEPRINT, The.Game, blueprint);
                    }
                }
            }
            return null;
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

        [VariableReplacer]
        public static string ud_weird(DelegateContext Context)
        {
            string output = null;
            if (!Context.Parameters.IsNullOrEmpty())
            {
                if (Context.Parameters.Count > 1)
                {
                    output = "{{" + Context.Parameters[0] + "|";
                    for (int i = 1; i < Context.Parameters.Count; i++)
                    {
                        if (i > 1)
                        {
                            output += " ";
                        }
                        output += TextFilters.Weird(Context.Parameters[i]);
                    }
                    output += "}}";
                }
                else
                {
                    return TextFilters.Weird(Context.Parameters[0]);
                }
            }
            return output;
        }

        public static string AppendTick(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + TICK + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendCross(string String, bool WithSpaceAfter = true)
        {
            return String + "[" + CROSS + "]" + (WithSpaceAfter ? " " : "");
        }
        public static string AppendYehNah(string String, bool Yeh, bool WithSpaceAfter = true)
        {
            return String + "[" + (Yeh ? TICK : CROSS) + "]" + (WithSpaceAfter ? " " : "");
        }
    }
}