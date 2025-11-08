// I think I have to have this? Unsure
using System;

namespace XRL.World.Parts.Skill
{
  [Serializable]
  public class Skirmishing : BaseSkill
  {
    public override int Priority => int.MinValue;
  }
}
