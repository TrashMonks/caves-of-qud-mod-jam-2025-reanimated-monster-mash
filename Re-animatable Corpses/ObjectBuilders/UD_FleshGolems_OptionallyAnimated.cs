
using System;
using System.Collections.Generic;

using XRL.World.Parts;

using static XRL.World.ObjectBuilders.UD_FleshGolems_OptionallyReanimated;

using static UD_FleshGolems.Options;

namespace XRL.World.ObjectBuilders
{
    [Serializable]
    public class UD_FleshGolems_OptionallyAnimated : IObjectBuilder
    {
        public static int SpecialChanceOneIn => SpecialCorpseAnimatedBuilderChanceOneIn;

        public static Animated AnimatedBuilder;

        public override void Initialize()
        {
            if (AnimatedBuilder == null)
            {
                AnimatedBuilder = new();
                AnimatedBuilder.Initialize();
            }
            base.Initialize();
        }

        public override void Apply(GameObject Object, string Context)
        {
            if (SpecialChanceOneIn > 0 && 1.ChanceIn(SpecialChanceOneIn) && !ContextsToIgnore.Contains(Context))
            {
                if (AnimatedBuilder == null)
                {
                    AnimatedBuilder = new();
                    AnimatedBuilder.Initialize();
                }
                AnimatedBuilder.Apply(Object, Context);
            }
        }
    }
}
