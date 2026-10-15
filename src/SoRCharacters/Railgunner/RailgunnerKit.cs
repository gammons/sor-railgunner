using RogueLibsCore;

namespace SoRCharacters.Railgunner
{
    public class RailgunnerKit : CustomTrait
    {
        public override void OnAdded()
        {
            var owner = Owner;
            if (owner == null || GameCompat.IsPreviewOrPool(owner)) return;
            Plugin.Log.LogInfo($"[Railgunner] RailgunnerKit added to {owner.agentRealName}");
        }

        public override void OnRemoved()
        {
            var owner = Owner;
            if (owner == null || GameCompat.IsPreviewOrPool(owner)) return;
            Plugin.Log.LogInfo($"[Railgunner] RailgunnerKit removed from {owner.agentRealName}");
        }
    }
}
