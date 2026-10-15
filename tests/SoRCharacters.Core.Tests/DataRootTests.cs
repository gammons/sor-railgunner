using SoRCharacters.Core.Presets;

namespace SoRCharacters.Core.Tests;

public class DataRootTests
{
    private const string Persistent = "/persistent";
    private const string Docs = "/docs/Streets of Rogue";

    private static string Choose(bool myDocs, bool mac, bool linux, bool uwp) =>
        DataRoot.Choose(myDocs, mac, linux, uwp, Persistent, Docs);

    [Fact] public void MyDocumentsOnWindows_UsesMyDocuments() => Assert.Equal(Docs, Choose(true, false, false, false));
    [Fact] public void MyDocumentsOff_UsesPersistent() => Assert.Equal(Persistent, Choose(false, false, false, false));
    [Fact] public void MyDocumentsOnButLinux_UsesPersistent() => Assert.Equal(Persistent, Choose(true, false, true, false));
    [Fact] public void MyDocumentsOnButMac_UsesPersistent() => Assert.Equal(Persistent, Choose(true, true, false, false));
    [Fact] public void MyDocumentsOnButUwp_UsesPersistent() => Assert.Equal(Persistent, Choose(true, false, false, true));
}
