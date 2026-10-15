using RogueLibsCore;

namespace SoRCharacters.Railgunner
{
    [ItemCategories(RogueCategories.Technology, RogueCategories.Usable)]
    public class RailgunItem : CustomItem, IItemUsable
    {
        public override void SetupDetails()
        {
            Item.itemType = ItemTypes.Tool;
            Item.stackable = false;
            Item.initCount = 1;
            Item.rewardCount = 1;
            Item.itemValue = 0;
            Item.cantDrop = true;
            Item.cantStoreInATMMachine = true;
            Item.notInLoadoutMachine = true;
            Item.cantBeCloned = true;
            Item.goesInToolbar = false;
            Item.noCountText = true;
        }

        public bool UseItem()
        {
            var owner = Owner;
            if (owner != null)
            {
                GameController.gameController.spawnerMain.SpawnStatusText(owner, "Countdown", "Hold the special ability to charge it.");
            }
            return false;
        }
    }
}
