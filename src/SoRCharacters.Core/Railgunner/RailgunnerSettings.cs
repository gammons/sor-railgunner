namespace SoRCharacters.Core.Railgunner
{
    public sealed class RailgunnerSettings
    {
        public float ChargeSeconds { get; set; } = 1.0f;
        public float ChargedSpeedMultiplier { get; set; } = 0.5f;
        public float ChargedDamageTakenMultiplier { get; set; } = 1.5f;
        public float RechargeSeconds { get; set; } = 15f;
        public float BeamWidthTiles { get; set; } = 0.5f;
        public float BeamRangeTiles { get; set; } = 20f;
        public int LineEmUpKills { get; set; } = 3;
        public int LineEmUpShots { get; set; } = 3;
        public float ShakeSeconds { get; set; } = 0.4f;
        public float ShakeOffset { get; set; } = 200f;
        public float HumIntervalSeconds { get; set; } = 1.0f;
        public float HumLoudAfterSeconds { get; set; } = 2.0f;
        public float ReadyToneVolume { get; set; } = 0.5f;
        public float SupercapacitorsRechargeMultiplier { get; set; } = 0.6667f;
        public float AlertSeconds { get; set; } = 2.0f;
        public float ArmedSpeedMultiplier { get; set; } = 0.15f;
        public float AimTurnDegreesPerSecond { get; set; } = 90f;
        public float ArmedMaxSeconds { get; set; } = 3f;
        public float BeamFadeSeconds { get; set; } = 0.25f;
        public float AfterglowSeconds { get; set; } = 0.5f;
        public float FlashSeconds { get; set; } = 0.12f;
        public int SparksPerTile { get; set; } = 12;
        public string ChargeSound { get; set; } = "ZombieSpitCharge";
        public string FireSound { get; set; } = "MindControlFire";
        public string FireSound2 { get; set; } = "LaserGunFire";
    }
}
