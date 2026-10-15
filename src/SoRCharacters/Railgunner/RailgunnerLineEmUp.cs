using RogueLibsCore;
using SoRCharacters.Core.Railgunner;

namespace SoRCharacters.Railgunner
{
    public class RailgunnerLineEmUp : CustomBigQuest
    {
        public RailgunnerLineEmUp()
        {
            State = NewState();
            Total = State.ShotsNeeded;
        }

        public LineEmUpState State { get; private set; }

        public override void SetupQuestMarkers()
        {
            State = NewState();
            Current = 0;
            Total = State.ShotsNeeded;
            Plugin.Log.LogInfo($"[Railgunner] line 'em up: new level, need {State.ShotsNeeded} shots with {State.KillsPerShot}+ kills");
        }

        public override bool CheckCompleted() => State.Completed;

        public override string GetProgress() => State.ProgressText;

        internal static void RecordShot(Agent shooter, int npcKills)
        {
            var quest = shooter.GetHook<CustomBigQuest>() as RailgunnerLineEmUp;
            if (quest == null) return;

            quest.State.RecordShot(npcKills);
            quest.Current = quest.State.QualifyingShots;
            Plugin.Log.LogInfo($"[Railgunner] line 'em up: {npcKills} this shot, {quest.State.QualifyingShots}/{quest.State.ShotsNeeded} qualifying");
        }

        private static LineEmUpState NewState()
        {
            var settings = RailgunnerModule.Settings;
            return new LineEmUpState(settings.LineEmUpShots, settings.LineEmUpKills);
        }
    }
}
