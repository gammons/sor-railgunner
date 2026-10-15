using System.Collections.Generic;
using SoRCharacters.Core.Presets;

namespace SoRCharacters.Railgunner
{
    internal static class RailgunnerPreset
    {
        public static PresetDefinition Create()
        {
            return new PresetDefinition
            {
                Name = "Railgunner",
                Description = "Charges a railshot that kills everything in a straight line, walls included. Hold the special ability to charge, let go to fire.",
                BodyType = "Demolitionist",
                BodyColorName = "Orange",
                HairType = "Mohawk",
                HairColorName = "Black",
                FacialHair = "None",
                SkinColorName = "MixedSkin",
                LegsColorName = "Black",
                EyesType = "Eyes",
                EyesColorName = "White",
                StartingHeadPiece = "",
                Endurance = 0,
                Strength = 0,
                Accuracy = 1,
                Speed = 1,
                SpecialAbility = "Railshot",
                BigQuest = "RailgunnerLineEmUp",
                Traits = new List<string> { "RailgunnerKit" },
                Items = new List<string> { "Knife", "RailgunItem" },
            };
        }
    }
}
