using TopDog.Content.Map;
using TopDog.Foundation.Result;

namespace TopDog.Content.Validate;

public sealed class MapSchemaValidator
{
    private static readonly HashSet<string> AllowedEventKinds = EventRegionKinds.All;

    public List<ValidationError> Validate(MapProject? project)
    {
        var errors = new List<ValidationError>();
        if (project == null)
        {
            errors.Add(new ValidationError("project", "project is null"));
            return errors;
        }

        var regionIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < project.regions.Count; i++)
        {
            var r = project.regions[i];
            var b = $"regions[{i}]";
            RequireNonBlank(errors, $"{b}.regionId", r.regionId);
            RequireNonBlank(errors, $"{b}.name", r.name);
            if (r.regionId != null && !regionIds.Add(r.regionId))
            {
                errors.Add(new ValidationError($"{b}.regionId", "duplicate regionId"));
            }
        }

        var constellationIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < project.constellations.Count; i++)
        {
            var c = project.constellations[i];
            var b = $"constellations[{i}]";
            RequireNonBlank(errors, $"{b}.constellationId", c.constellationId);
            RequireNonBlank(errors, $"{b}.name", c.name);
            RequireNonBlank(errors, $"{b}.regionId", c.regionId);
            if (c.regionId != null && !regionIds.Contains(c.regionId))
            {
                errors.Add(new ValidationError($"{b}.regionId", "unknown regionId"));
            }
            if (c.constellationId != null && !constellationIds.Add(c.constellationId))
            {
                errors.Add(new ValidationError($"{b}.constellationId", "duplicate constellationId"));
            }
        }

        var systemIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < project.systems.Count; i++)
        {
            var s = project.systems[i];
            var b = $"systems[{i}]";
            RequireNonBlank(errors, $"{b}.solarSystemId", s.solarSystemId);
            RequireNonBlank(errors, $"{b}.name", s.name);
            RequireNonBlank(errors, $"{b}.constellationId", s.constellationId);
            RequireNonBlank(errors, $"{b}.regionId", s.regionId);
            if (s.regionId != null && !regionIds.Contains(s.regionId))
            {
                errors.Add(new ValidationError($"{b}.regionId", "unknown regionId"));
            }
            if (s.constellationId != null && !constellationIds.Contains(s.constellationId))
            {
                errors.Add(new ValidationError($"{b}.constellationId", "unknown constellationId"));
            }
            if (s.solarSystemId != null && !systemIds.Add(s.solarSystemId))
            {
                errors.Add(new ValidationError($"{b}.solarSystemId", "duplicate solarSystemId"));
            }
            if (s.resourceAffluenceIndex < 1 || s.resourceAffluenceIndex > 100)
            {
                errors.Add(new ValidationError($"{b}.resourceAffluenceIndex", "must be 1..100"));
            }
            if (s.developmentDifficulty < 1 || s.developmentDifficulty > 100)
            {
                errors.Add(new ValidationError($"{b}.developmentDifficulty", "must be 1..100"));
            }
            if (s.securityLevel < -1f || s.securityLevel > 1f)
            {
                errors.Add(new ValidationError($"{b}.securityLevel", "must be -1..1"));
            }
            if (s.starMapPositionLy == null || s.starMapPositionLy.Length != 3)
            {
                errors.Add(new ValidationError($"{b}.starMapPositionLy", "must be array of 3"));
            }
            if (s.eventRegions == null || s.eventRegions.Count == 0)
            {
                errors.Add(new ValidationError($"{b}.eventRegions", "must have at least one event region"));
            }
            else
            {
                var erIds = new HashSet<string>(StringComparer.Ordinal);
                var starCount = 0;
                for (var j = 0; j < s.eventRegions.Count; j++)
                {
                    ValidateEventRegion(errors, $"{b}.eventRegions[{j}]", s.eventRegions[j], erIds);
                    if (EventRegionKinds.IsStar(s.eventRegions[j].kind))
                    {
                        starCount++;
                    }
                }
                if (starCount != 1)
                {
                    errors.Add(new ValidationError($"{b}.eventRegions",
                        $"must have exactly one star (found {starCount})"));
                }
            }
        }

        var bridgeIds = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < project.bridges.Count; i++)
        {
            var br = project.bridges[i];
            var b = $"bridges[{i}]";
            RequireNonBlank(errors, $"{b}.bridgeId", br.bridgeId);
            RequireNonBlank(errors, $"{b}.fromSystemId", br.fromSystemId);
            RequireNonBlank(errors, $"{b}.toSystemId", br.toSystemId);
            if (br.bridgeId != null && !bridgeIds.Add(br.bridgeId))
            {
                errors.Add(new ValidationError($"{b}.bridgeId", "duplicate bridgeId"));
            }
            if (br.fromSystemId != null && !systemIds.Contains(br.fromSystemId))
            {
                errors.Add(new ValidationError($"{b}.fromSystemId", "unknown system"));
            }
            if (br.toSystemId != null && !systemIds.Contains(br.toSystemId))
            {
                errors.Add(new ValidationError($"{b}.toSystemId", "unknown system"));
            }
            if (br.fromSystemId != null && br.fromSystemId.Equals(br.toSystemId, StringComparison.Ordinal))
            {
                errors.Add(new ValidationError(b, "jump bridge cannot be self-loop"));
            }
        }

        foreach (var s in project.systems)
        {
            if (s.jumpBridgeIds == null)
            {
                continue;
            }
            foreach (var bid in s.jumpBridgeIds)
            {
                if (!bridgeIds.Contains(bid))
                {
                    errors.Add(new ValidationError($"systems/{s.solarSystemId}",
                        $"jumpBridgeIds references unknown bridge: {bid}"));
                }
            }
        }

        return errors;
    }

    private static void ValidateEventRegion(
        List<ValidationError> errors,
        string b,
        EventRegionDef er,
        HashSet<string> ids)
    {
        RequireNonBlank(errors, $"{b}.eventRegionId", er.eventRegionId);
        RequireNonBlank(errors, $"{b}.kind", er.kind);
        RequireNonBlank(errors, $"{b}.name", er.name);
        if (er.kind != null && !AllowedEventKinds.Contains(er.kind))
        {
            errors.Add(new ValidationError($"{b}.kind", $"unknown kind: {er.kind}"));
        }
        if (er.radiusKm <= 0)
        {
            errors.Add(new ValidationError($"{b}.radiusKm", "must be > 0"));
        }
        if (er.anchorAu == null || er.anchorAu.Length != 3)
        {
            errors.Add(new ValidationError($"{b}.anchorAu", "must be array of 3"));
        }
        if (EventRegionKinds.JumpBridge.Equals(er.kind, StringComparison.Ordinal))
        {
            RequireNonBlank(errors, $"{b}.bridgeId", er.bridgeId);
            RequireNonBlank(errors, $"{b}.targetSystemId", er.targetSystemId);
        }
        if (er.eventRegionId != null && !ids.Add(er.eventRegionId))
        {
            errors.Add(new ValidationError($"{b}.eventRegionId", "duplicate within system"));
        }
    }

    private static void RequireNonBlank(List<ValidationError> errors, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError(path, "required"));
        }
    }
}
