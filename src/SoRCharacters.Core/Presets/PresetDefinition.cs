using System.Collections.Generic;

namespace SoRCharacters.Core.Presets
{
    public sealed class PresetDefinition
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string BodyType { get; set; } = "";
        public string BodyColorName { get; set; } = "";
        public string HairType { get; set; } = "";
        public string HairColorName { get; set; } = "";
        public string FacialHair { get; set; } = "";
        public string SkinColorName { get; set; } = "";
        public string LegsColorName { get; set; } = "";
        public string EyesType { get; set; } = "";
        public string EyesColorName { get; set; } = "";
        public string StartingHeadPiece { get; set; } = "";
        public int Endurance { get; set; }
        public int Strength { get; set; }
        public int Accuracy { get; set; }
        public int Speed { get; set; }
        public string SpecialAbility { get; set; } = "";
        public string BigQuest { get; set; } = "";
        public List<string> Traits { get; set; } = new List<string>();
        public List<string> Items { get; set; } = new List<string>();
    }
}
