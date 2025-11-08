using System;
using XRL.World;

#nullable disable
namespace XRL.World.Parts
{
    [Serializable]
    public class Brothers_ZombieNameBuilder : IPart
    {
        public static string GetSpecies(GameObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var props = obj.Property;
            var tags = obj.GetBlueprint().Tags;

            // corpse specific logic
            if (obj.HasTag("Corpse"))
            {
                string sourceBlueprint = obj.GetStringProperty("SourceBlueprint");
                if (!string.IsNullOrEmpty(sourceBlueprint) &&
                    GameObjectFactory.Factory.Blueprints.TryGetValue(sourceBlueprint, out var blueprint))
                {
                    tags = blueprint.Tags;
                }
            }


            // regular species logic
            string species = tags.GetValue<string, string>("SpeciesOverride")
                             ?? props.GetValue<string, string>("SpeciesOverride")
                             ?? tags.GetValue<string, string>("Species")
                             ?? props.GetValue<string, string>("Species")
                             ?? tags.GetValue<string, string>("Class")
                             ?? props.GetValue<string, string>("Class");

            return species; // Can be null
        }

        private static string Capitalize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return char.ToUpper(text[0]) + text.Substring(1);
        }

        public static string GetZombieName(GameObject obj)
        {
            string species = GetSpecies(obj);
            // Early return if species is dybbuk to avoid the infinite loop
            if (string.Equals(species, "dybbuk", StringComparison.OrdinalIgnoreCase))
                return null;

            species = Capitalize(species);
            
            // Check blueprint existence
            string blueprintId = $"Brothers_Puppets_Zombie{species}";
            GameObjectBlueprint blueprint;
            
            
            if (!GameObjectFactory.Factory.Blueprints.TryGetValue(blueprintId, out blueprint))
            {
                //check again but with just the class
                var tags = obj.GetBlueprint().Tags;

                if (obj.HasTag("Corpse"))
                {
                    string sourceBlueprint = obj.GetStringProperty("SourceBlueprint");
                    if (!string.IsNullOrEmpty(sourceBlueprint) &&
                        GameObjectFactory.Factory.Blueprints.TryGetValue(sourceBlueprint, out var blueprintVar))
                    {
                        tags = blueprintVar.Tags;
                    }
                }
                string newSpecies = tags.GetValue<string, string>("Class") ?? obj.Property.GetValue<string, string>("Class");
                if (!string.IsNullOrEmpty(newSpecies))
                {
                    newSpecies = Capitalize(newSpecies);
                    blueprintId = $"Brothers_Puppets_Zombie{newSpecies}";
                }
                else
                {
                    // Fallback if blueprint doesn't exist
                    blueprintId = "Brothers_Puppets_ZombieOddity";
                }
            }

            return blueprintId;
        }
    }
}
