namespace SoRCharacters.Core.Presets
{
    public static class DataRoot
    {
        // Mirrors the game's own choice (LevelEditor, SaveLoad, ...): My Documents only when the game is set to
        // use it and this is neither a Mac, Linux nor UWP build; otherwise Unity's persistent data path.
        public static string Choose(bool usingMyDocuments, bool macVersion, bool linuxVersion, bool usingUWP,
            string persistentDataPath, string myDocumentsPath)
        {
            return usingMyDocuments && !macVersion && !linuxVersion && !usingUWP
                ? myDocumentsPath
                : persistentDataPath;
        }
    }
}
