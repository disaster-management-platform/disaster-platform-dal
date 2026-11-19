using System.Text.Json;

namespace DisasterLocation.Core;

public sealed record DisasterScope(string Actor, string Organization, bool OrganizationWide, int[] DisasterIds)
{
    public bool CanAccess(Disaster disaster)
    {
        var owner = OriginalOrganization(disaster) ?? disaster.RegisteringOrganizationId;
        var ownsOrganization = owner == Organization;
        if (ownsOrganization && OrganizationWide) return true;
        try
        {
            if (ownsOrganization && disaster.CreationSnapshot is not null)
            {
                using var snapshot = JsonDocument.Parse(disaster.CreationSnapshot);
                if (snapshot.RootElement.TryGetProperty("CreatedBy", out var creator)
                    && creator.ValueKind == JsonValueKind.String && creator.GetString() == Actor) return true;
            }
            if (!DisasterIds.Contains(disaster.Id)) return false;
            var participants = disaster.OrganizationsJson is null ? null
                : JsonSerializer.Deserialize<DisasterOrganization[]>(disaster.OrganizationsJson);
            return ownsOrganization || participants?.Any(p => p.OrganizationId == Organization) == true
                || owner is null && (participants is null || participants.Length == 0);
        }
        catch (JsonException) { return false; }
    }
    public static string? OriginalOrganization(Disaster disaster)
    {
        if (disaster.CreationSnapshot is null) return null;
        try
        {
            using var snapshot = JsonDocument.Parse(disaster.CreationSnapshot);
            return snapshot.RootElement.TryGetProperty("RegisteringOrganizationId", out var value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
