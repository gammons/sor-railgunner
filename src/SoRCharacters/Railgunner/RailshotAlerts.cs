using System.Collections.Generic;

namespace SoRCharacters.Railgunner
{
    // NPCs who saw the railgun charging: alerted now, hostile AlertSeconds later. Kept apart from the charge so a pending
    // alert still turns hostile after the charge ends; cleared on level change and session reset.
    internal static class RailshotAlerts
    {
        private sealed class Pending
        {
            public Pending(Agent npc, Agent shooter, float remaining)
            {
                Npc = npc;
                NpcId = npc.agentID;
                Shooter = shooter;
                ShooterId = shooter.agentID;
                Remaining = remaining;
            }

            public Agent Npc { get; }
            public int NpcId { get; }
            public Agent Shooter { get; }
            public int ShooterId { get; }
            public float Remaining { get; set; }
        }

        private static readonly List<Pending> Alerts = new List<Pending>();

        public static bool Any => Alerts.Count != 0;

        public static bool IsPending(Agent npc, Agent shooter)
        {
            foreach (var p in Alerts)
            {
                if (p.NpcId == npc.agentID && p.ShooterId == shooter.agentID) return true;
            }
            return false;
        }

        // "StartBattle" is the "!" an NPC shows when it notices an enemy and starts a fight (BrainUpdate.cs:3707,
        // StateIndicator.cs:220-225). The game hides it for NPCs that are off camera (SpawnerMain.cs:6881).
        public static void Alert(GameController gc, Agent npc, Agent shooter)
        {
            Alerts.Add(new Pending(npc, shooter, RailgunnerModule.Settings.AlertSeconds));
            RailshotFire.Guard("alert indicator", () => gc.spawnerMain.SpawnStateIndicator(npc, "StartBattle"));
            Plugin.Log.LogInfo($"[Railgunner] {npc.agentName}#{npc.agentID} alerted");
        }

        public static void Tick(float deltaTime)
        {
            if (Alerts.Count == 0) return;

            for (var i = Alerts.Count - 1; i >= 0; i--)
            {
                var p = Alerts[i];
                p.Remaining -= deltaTime;
                if (p.Remaining > 0f) continue;

                Alerts.RemoveAt(i);
                RailshotFire.Guard("alert turns hostile", () => TurnHostile(p));
            }
        }

        private static void TurnHostile(Pending p)
        {
            var npc = p.Npc;
            var shooter = p.Shooter;
            if (Gone(npc, p.NpcId) || Gone(shooter, p.ShooterId)) return;

            var code = npc.relationships.GetRelCode(shooter);
            if (code == relStatus.Hostile || code == relStatus.Aligned || code == relStatus.Loyal || code == relStatus.Submissive) return;

            npc.relationships.SetRel(shooter, "Hateful");
            npc.relationships.SetRelHate(shooter, 5);
            Plugin.Log.LogInfo($"[Railgunner] {npc.agentName}#{npc.agentID} turned hostile");
        }

        // Agents are pooled, so a recycled object carries a different agentID.
        private static bool Gone(Agent agent, int id)
        {
            return agent == null || agent.agentID != id || agent.dead || agent.ghost;
        }

        public static void Clear()
        {
            Alerts.Clear();
        }
    }
}
