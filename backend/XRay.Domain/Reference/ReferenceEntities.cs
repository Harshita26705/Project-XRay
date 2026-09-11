namespace XRay.Domain.Reference;

/// <summary>Base shape shared by all small lookup/reference tables (tinyint PK, unique Code).</summary>
public abstract class ReferenceEntity
{
    public byte Id { get; set; }
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}

public class RiskState : ReferenceEntity
{
    public string? Description { get; set; }
    public byte SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ComponentType : ReferenceEntity
{
}

public class GraphEdgeType : ReferenceEntity
{
    public decimal DefaultCost { get; set; }
}

public class EvidenceType : ReferenceEntity
{
}

public class ChangeType : ReferenceEntity
{
}

public class AnalysisStatus : ReferenceEntity
{
}

public class IntegrationProvider : ReferenceEntity
{
}

public class SecuritySeverity : ReferenceEntity
{
    public byte SortOrder { get; set; }
}

public class AnalysisStage : ReferenceEntity
{
    public byte SortOrder { get; set; }
}

public class NotificationEventType : ReferenceEntity
{
}
