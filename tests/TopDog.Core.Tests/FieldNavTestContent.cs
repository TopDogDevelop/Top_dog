using TopDog.Content.Modules;
using TopDog.Foundation.Io;
using TopDog.Sim.Realtime;

namespace TopDog.Core.Tests;

internal static class FieldNavTestContent
{
    internal static void PinRepoContentRoot()
    {
        AppRoot.InvalidateCache();
        AppRoot.SetOverrideRoot(RepoRoot());
    }

    internal static ModuleRegistry LoadModules()
    {
        PinRepoContentRoot();
        return ModuleRegistry.LoadDefault();
    }

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "content", "map", "systems")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("top_dog_unity repo root not found");
    }
}
