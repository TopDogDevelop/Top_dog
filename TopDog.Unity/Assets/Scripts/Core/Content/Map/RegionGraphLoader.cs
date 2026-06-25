using System.Text.Json;
using TopDog.Content.Map;
using TopDog.Content.Validate;
using TopDog.Foundation.Json;
using TopDog.Foundation.Result;

namespace TopDog.Content.Map;

public sealed class RegionGraphLoader
{
    private readonly MapSchemaValidator _validator = new();

    public Result<LoadedMap, ValidationError> Load(string contentMapRoot)
    {
        try
        {
            var project = new MapProject
            {
                projectName = Path.GetFileName(contentMapRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            };

            LoadDirectory(Path.Combine(contentMapRoot, "regions"), project.regions);
            LoadDirectory(Path.Combine(contentMapRoot, "constellations"), project.constellations);
            LoadDirectory(Path.Combine(contentMapRoot, "systems"), project.systems);
            LoadDirectory(Path.Combine(contentMapRoot, "jump_bridges"), project.bridges);

            var bands = LoadSecurityBands(Path.Combine(contentMapRoot, "security_bands.json"));

            var errors = _validator.Validate(project);
            if (errors.Count > 0)
            {
                return Result<LoadedMap, ValidationError>.FailList(errors);
            }
            return Result<LoadedMap, ValidationError>.Ok(new LoadedMap(project, bands));
        }
        catch (IOException e)
        {
            return Result<LoadedMap, ValidationError>.FailList(new[] { new ValidationError("io", e.Message) });
        }
    }

    private static SecurityBands LoadSecurityBands(string path)
    {
        if (!File.Exists(path))
        {
            return DefaultBands();
        }
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SecurityBands>(json, TopDogJson.Options) ?? DefaultBands();
    }

    private static SecurityBands DefaultBands()
    {
        var bands = new SecurityBands();
        bands.bands.Add(new SecurityBands.Band
        {
            id = "HIGHSEC",
            minSecurity = 0.5f,
            maxSecurity = 1f,
            uiColor = "#4a9eff",
        });
        bands.bands.Add(new SecurityBands.Band
        {
            id = "LOWSEC",
            minSecurity = 0.01f,
            maxSecurity = 0.49f,
            uiColor = "#e6a817",
        });
        bands.bands.Add(new SecurityBands.Band
        {
            id = "NULL",
            minSecurity = -1f,
            maxSecurity = 0f,
            uiColor = "#ff4444",
        });
        return bands;
    }

    private static void LoadDirectory<T>(string dir, List<T> outList)
    {
        if (!Directory.Exists(dir))
        {
            return;
        }
        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var json = File.ReadAllText(file);
            var item = JsonSerializer.Deserialize<T>(json, TopDogJson.Options);
            if (item != null)
            {
                outList.Add(item);
            }
        }
    }
}
