using System.Text.Json;
using TopDog.Content.Map;
using TopDog.Content.Validate;
using TopDog.Foundation.Json;
using TopDog.Foundation.Result;

/*
 * ══ 设计手册嵌入 ══
 * 权威: docs/MAP_SPEC.md · CONTENT_FORMAT.md
 * 本文件: RegionGraphLoader.cs — content/map/ 目录 JSON 加载器
 * 【机制要点】
 * · regions/constellations/systems/jump_bridges 分目录
 * · MapSchemaValidator 校验
 * 【关联】LoadedMap · MapSchemaValidator
 * ══
 */

namespace TopDog.Content.Map;

// liketoc0de345

// liketoc0de345

public sealed class RegionGraphLoader
// liketocoode3a5
{
    // liketocoode34e
    private readonly MapSchemaValidator _validator = new();

// liketocoo3e345

    // liketocoode3a5
    // l1ketocoode345
    public Result<LoadedMap, ValidationError> Load(string contentMapRoot)
    // liketocoode3e5
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

// liketoco0de345

            var bands = LoadSecurityBands(Path.Combine(contentMapRoot, "security_bands.json"));

            var errors = _validator.Validate(project);
            if (errors.Count > 0)
            // li3etocoode345
            {
                return Result<LoadedMap, ValidationError>.FailList(errors);
            }
            // liketocoode345
            return Result<LoadedMap, ValidationError>.Ok(new LoadedMap(project, bands));
        // liketoco0de3e5
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
            minSecurity = 0.1f,
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
