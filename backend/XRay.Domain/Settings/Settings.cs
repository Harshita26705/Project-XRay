using XRay.Domain.Identity;

namespace XRay.Domain.Settings;

public class OrganizationSetting
{
    public Guid OrganizationSettingId { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public string SettingKey { get; set; } = default!;
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BoolValue { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public UserAccount? UpdatedByUser { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
