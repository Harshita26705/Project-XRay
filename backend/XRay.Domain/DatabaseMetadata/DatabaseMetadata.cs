using XRay.Domain.Integrations;

namespace XRay.Domain.DatabaseMetadata;

public class DatabaseConnection
{
    public Guid DatabaseConnectionId { get; set; }
    public Guid IntegrationConnectionId { get; set; }
    public IntegrationConnection? IntegrationConnection { get; set; }
    public string DatabaseName { get; set; } = default!;
    public string? SchemaFilter { get; set; }
    public bool IsReadOnly { get; set; } = true;
}

public class DatabaseSchema
{
    public Guid DatabaseSchemaId { get; set; }
    public Guid DatabaseConnectionId { get; set; }
    public DatabaseConnection? DatabaseConnection { get; set; }
    public string SchemaName { get; set; } = default!;
}

public class DatabaseObject
{
    public Guid DatabaseObjectId { get; set; }
    public Guid DatabaseSchemaId { get; set; }
    public DatabaseSchema? DatabaseSchema { get; set; }
    public string ObjectTypeCode { get; set; } = default!;
    public string ObjectName { get; set; } = default!;
    public string? DefinitionHash { get; set; }
}

public class DatabaseColumn
{
    public Guid DatabaseColumnId { get; set; }
    public Guid DatabaseObjectId { get; set; }
    public DatabaseObject? DatabaseObject { get; set; }
    public string ColumnName { get; set; } = default!;
    public string DataType { get; set; } = default!;
    public bool IsNullable { get; set; }
    public int? OrdinalPosition { get; set; }
}

public class DatabaseForeignKey
{
    public Guid DatabaseForeignKeyId { get; set; }
    public Guid SourceObjectId { get; set; }
    public DatabaseObject? SourceObject { get; set; }
    public Guid TargetObjectId { get; set; }
    public DatabaseObject? TargetObject { get; set; }
    public string ConstraintName { get; set; } = default!;
}
