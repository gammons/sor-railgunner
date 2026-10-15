namespace SoRCharacters.Core.Railgunner
{
    public sealed class LineEmUpState
    {
        public LineEmUpState(int shotsNeeded, int killsPerShot)
        {
            ShotsNeeded = shotsNeeded;
            KillsPerShot = killsPerShot;
        }

        public int ShotsNeeded { get; }
        public int KillsPerShot { get; }
        public int QualifyingShots { get; private set; }
        public bool Completed => QualifyingShots >= ShotsNeeded;

        public void RecordShot(int npcKills)
        {
            if (npcKills >= KillsPerShot) QualifyingShots++;
        }

        public string ProgressText => Completed
            ? $"Lined them up {QualifyingShots} times!"
            : $"Railshots with {KillsPerShot}+ kills: {QualifyingShots}/{ShotsNeeded}";
    }
}
