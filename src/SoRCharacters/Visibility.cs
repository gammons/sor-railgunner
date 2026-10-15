using UnityEngine;

namespace SoRCharacters
{
    internal static class Visibility
    {
        // The game's crime-check visibility test (Relationships.OwnCheck, Relationships.cs:4862-4880).
        public static bool CanSee(Agent npc, Agent target)
        {
            if (target.invisible || target.ghost || target.underBox) return false;
            if (Vector2.Distance(npc.curPosition, target.curPosition) > npc.LOSRange / target.hardToSeeFromDistance) return false;
            return npc.movement.HasLOSObjectNormal(target) && npc.movement.InVisionBoundsAgent(target);
        }
    }
}
