namespace SoRCharacters
{
    internal static class GameCompat
    {
        private static GameController Gc => GameController.gameController;

        public static bool IsOnline => Gc.multiplayerMode;

        public static bool IsHost => Gc.serverPlayer;

        public static bool IsPreviewOrPool(Agent a)
        {
            return a.isDummy || a.isBackup || Gc.levelType == "HomeBase";
        }

        public static bool IsRailgunner(Agent a)
        {
            return a.statusEffects.hasTrait("RailgunnerKit");
        }

        public static bool HasSupercapacitors(Agent a)
        {
            return a.statusEffects.hasTrait("RailgunSupercapacitors");
        }

        public static bool HasBreachingSlug(Agent a)
        {
            return a.statusEffects.hasTrait("RailgunBreachingSlug");
        }
    }
}
