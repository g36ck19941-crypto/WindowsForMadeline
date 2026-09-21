namespace CelesteDesktop.Install;

public static class CelesteInstallProfiles
{
    public static InstallValidationProfile WindowsFoundation { get; } = new(
        "celeste-windows-foundation-v1",
        new[]
        {
            "Celeste.exe",
            "Content/Graphics/Atlases/Gameplay.meta",
            "Content/Graphics/Sprites.xml"
        });
}
