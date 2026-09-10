# Project X-Ray — Microsoft SQL Server Database Schema

> **Purpose:** Production-oriented, normalized SQL Server schema for Project X-Ray.
>
> The schema is designed around the application's current product surface: organizations, users, projects, repositories, Azure DevOps changes, code ingestion, dependency graphs, deterministic impact analysis, security findings, evidence, AI/Foundry explanations, reports, integrations, settings, notifications, and analysis jobs.
>
> The existing product principle is preserved: **the dependency graph and deterministic analysis engine are authoritative; AI output is explanatory and must never become the source of truth.**

---

## 1. Design Goals

### 1.1 Normalization

The transactional schema targets **3NF**:

- one fact is stored in one logical place;
- repeated descriptive values are normalized into reference tables;
- many-to-many relationships use bridge tables;
- project/repository/integration/analysis data is separated by responsibility;
- historical analysis snapshots are preserved rather than overwritten.

Intentional denormalization should be limited to:

- immutable analysis/report snapshots;
- materialized counters used for dashboard performance;
- search/indexing projections outside the core transactional model.

### 1.2 SOLID-aligned database boundaries

SOLID is primarily an application/service design principle, but the schema supports it by keeping bounded responsibilities separate:

```text
Identity
  -> organizations, users, memberships

Project Management
  -> projects, repositories, branches, environments

Ingestion
  -> code files, symbols, parser runs

Graph
  -> graph nodes, graph edges

Change Management
  -> changes, pull requests, work items, commits

Analysis
  -> analyses, progress, targets, scopes, risk assessments

Security
  -> scanners, rules, findings, finding evidence

AI
  -> providers, configurations, prompt templates, generations

Reporting
  -> reports, report sections, recommendations

Integrations
  -> connections and integration-specific configuration

Notifications
  -> channels, rules, deliveries

Operations
  -> jobs, audit events
```

No single table should become a "god table" containing every product concern.

### 1.3 Multi-tenancy

Every organization-owned entity is tenant-scoped through `OrganizationId`.

Application services should enforce:

```text
Authenticated User
      |
      v
Organization Membership
      |
      v
Project Access
      |
      v
Resource
```

Never rely only on the frontend for tenant isolation.

### 1.4 Security

Do **not** store raw:

- Azure DevOps PATs;
- OAuth client secrets;
- API keys;
- Microsoft credentials;
- database passwords;
- AI provider secrets.

Store an external secret-manager reference in `SecretReference`, e.g.:

```text
Azure Key Vault secret URI/reference
```

Secrets must remain outside SQL Server unless an explicit encrypted-secret architecture is introduced.

---

# 2. Logical Domain Model

```text
Organization
 |
 +-- User <-> OrganizationMembership
 |
 +-- Project
 |    |
 |    +-- ProjectMember
 |    +-- Repository
 |    |     +-- Branch
 |    |     +-- Commit
 |    |     +-- CodeFile
 |    |           +-- CodeSymbol
 |    |
 |    +-- Environment
 |    +-- IntegrationConnection
 |    +-- Graph
 |          +-- GraphNode
 |          +-- GraphEdge
 |                |
 |                +-- GraphEdgeEvidence
 |
 +-- Change
 |    +-- PullRequest
 |    +-- WorkItem
 |    +-- Commit
 |
 +-- Analysis
 |    +-- AnalysisTarget
 |    +-- AnalysisScope
 |    +-- AnalysisProgress
 |    +-- AnalysisNodeResult
 |    +-- ImpactPath
 |    +-- Evidence
 |    +-- Recommendation
 |    +-- SecurityFinding
 |    +-- AIGeneration
 |    +-- Report
 |
 +-- IntegrationConnection
 |
 +-- NotificationChannel
 |    +-- NotificationRule
 |    +-- NotificationDelivery
 |
 +-- AuditEvent
```

---

# 3. SQL Server Conventions

Recommended:

- SQL Server 2019+;
- `uniqueidentifier` for distributed/application-generated IDs;
- `datetime2(3)` for timestamps;
- `bit` for booleans;
- `nvarchar` for user-facing text;
- `nvarchar(max)` for long text/code/JSON;
- `varbinary(max)` only when binary storage is genuinely required;
- UTC timestamps using application-generated `SYSUTCDATETIME()` defaults;
- `rowversion` for optimistic concurrency;
- foreign keys for relational integrity;
- unique constraints for business identities;
- filtered indexes for nullable unique business keys.

---

# 4. Reference Tables

Avoid SQL Server `ENUM` emulation through magic integers in application code. Use small reference tables where values are business-visible and may evolve.

## 4.1 RiskState

```sql
CREATE TABLE dbo.RiskState
(
    RiskStateId       tinyint        NOT NULL,
    Code              varchar(30)    NOT NULL,
    DisplayName       nvarchar(50)   NOT NULL,
    Description       nvarchar(500)  NULL,
    SortOrder         tinyint        NOT NULL,
    IsActive          bit            NOT NULL
        CONSTRAINT DF_RiskState_IsActive DEFAULT (1),

    CONSTRAINT PK_RiskState PRIMARY KEY (RiskStateId),
    CONSTRAINT UQ_RiskState_Code UNIQUE (Code)
);
GO

INSERT INTO dbo.RiskState
    (RiskStateId, Code, DisplayName, Description, SortOrder)
VALUES
    (1, 'CRITICAL', 'Critical', 'Strong evidence of direct or high-impact change.', 1),
    (2, 'RISKY',    'Risky',    'Potential downstream or transitive impact.', 2),
    (3, 'SAFE',     'Safe',     'No impact detected with currently available evidence.', 3),
    (4, 'UNKNOWN',  'Unknown',  'Insufficient evidence for a trustworthy conclusion.', 4);
GO
```

## 4.2 ComponentType

```sql
CREATE TABLE dbo.ComponentType
(
    ComponentTypeId tinyint       NOT NULL,
    Code            varchar(40)   NOT NULL,
    DisplayName     nvarchar(80)  NOT NULL,
    CONSTRAINT PK_ComponentType PRIMARY KEY (ComponentTypeId),
    CONSTRAINT UQ_ComponentType_Code UNIQUE (Code)
);
GO
```

Recommended values:

```text
FRONTEND
FRONTEND_MODULE
FRONTEND_COMPONENT
API
CONTROLLER
SERVICE
INTERFACE
REPOSITORY
DATABASE
DATABASE_TABLE
DATABASE_COLUMN
STORED_PROCEDURE
EXTERNAL
UNKNOWN
```

## 4.3 GraphEdgeType

```sql
CREATE TABLE dbo.GraphEdgeType
(
    GraphEdgeTypeId tinyint       NOT NULL,
    Code            varchar(40)   NOT NULL,
    DisplayName     nvarchar(80)  NOT NULL,
    DefaultCost     decimal(10,4) NOT NULL,
    CONSTRAINT PK_GraphEdgeType PRIMARY KEY (GraphEdgeTypeId),
    CONSTRAINT UQ_GraphEdgeType_Code UNIQUE (Code),
    CONSTRAINT CK_GraphEdgeType_DefaultCost CHECK (DefaultCost >= 0)
);
GO
```

Recommended values:

```text
CALLS
DEPENDS_ON
IMPORTS
EXPOSES
READS
WRITES
BINDS
INHERITS
REFERENCES
USES
```

`BINDS` can have cost `0`, consistent with the existing analysis model.

## 4.4 EvidenceType

```sql
CREATE TABLE dbo.EvidenceType
(
    EvidenceTypeId tinyint       NOT NULL,
    Code            varchar(50)  NOT NULL,
    DisplayName     nvarchar(100) NOT NULL,
    CONSTRAINT PK_EvidenceType PRIMARY KEY (EvidenceTypeId),
    CONSTRAINT UQ_EvidenceType_Code UNIQUE (Code)
);
GO
```

Recommended values:

```text
DIRECT_CHANGE
STRUCTURAL_DEPENDENCY
DATABASE_ACCESS
SECURITY_ALERT
PARSER_EVIDENCE
TEST_EVIDENCE
INTEGRATION_EVIDENCE
GRAPH_PATH
```

## 4.5 ChangeType

```sql
CREATE TABLE dbo.ChangeType
(
    ChangeTypeId tinyint       NOT NULL,
    Code         varchar(30)   NOT NULL,
    DisplayName  nvarchar(50)  NOT NULL,
    CONSTRAINT PK_ChangeType PRIMARY KEY (ChangeTypeId),
    CONSTRAINT UQ_ChangeType_Code UNIQUE (Code)
);
GO
```

Values:

```text
PULL_REQUEST
WORK_ITEM
COMMIT
MANUAL
```

## 4.6 AnalysisStatus

```sql
CREATE TABLE dbo.AnalysisStatus
(
    AnalysisStatusId tinyint      NOT NULL,
    Code              varchar(30) NOT NULL,
    DisplayName       nvarchar(60) NOT NULL,
    CONSTRAINT PK_AnalysisStatus PRIMARY KEY (AnalysisStatusId),
    CONSTRAINT UQ_AnalysisStatus_Code UNIQUE (Code)
);
GO
```

Values:

```text
QUEUED
RUNNING
PARTIAL
COMPLETED
FAILED
CANCELLED
```

## 4.7 IntegrationProvider

```sql
CREATE TABLE dbo.IntegrationProvider
(
    IntegrationProviderId tinyint       NOT NULL,
    Code                  varchar(50)   NOT NULL,
    DisplayName           nvarchar(100) NOT NULL,
    CONSTRAINT PK_IntegrationProvider PRIMARY KEY (IntegrationProviderId),
    CONSTRAINT UQ_IntegrationProvider_Code UNIQUE (Code)
);
GO
```

Values:

```text
AZURE_DEVOPS
SQL_SERVER
MICROSOFT_FOUNDRY
AZURE_AI_SEARCH
MICROSOFT_TEAMS
POWER_AUTOMATE
```

## 4.8 SecuritySeverity

```sql
CREATE TABLE dbo.SecuritySeverity
(
    SecuritySeverityId tinyint       NOT NULL,
    Code               varchar(30)   NOT NULL,
    DisplayName        nvarchar(50)  NOT NULL,
    SortOrder          tinyint       NOT NULL,
    CONSTRAINT PK_SecuritySeverity PRIMARY KEY (SecuritySeverityId),
    CONSTRAINT UQ_SecuritySeverity_Code UNIQUE (Code)
);
GO
```

Values:

```text
CRITICAL
HIGH
MEDIUM
LOW
INFO
```

---

# 5. Identity and Organization

## 5.1 Organization

```sql
CREATE TABLE dbo.Organization
(
    OrganizationId uniqueidentifier NOT NULL
        CONSTRAINT DF_Organization_Id DEFAULT NEWSEQUENTIALID(),

    Name           nvarchar(200)    NOT NULL,
    Slug           varchar(100)     NOT NULL,
    IsActive       bit              NOT NULL
        CONSTRAINT DF_Organization_IsActive DEFAULT (1),

    CreatedAtUtc   datetime2(3)     NOT NULL
        CONSTRAINT DF_Organization_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc   datetime2(3)     NOT NULL
        CONSTRAINT DF_Organization_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    RowVersion     rowversion       NOT NULL,

    CONSTRAINT PK_Organization PRIMARY KEY (OrganizationId),
    CONSTRAINT UQ_Organization_Slug UNIQUE (Slug)
);
GO
```

## 5.2 UserAccount

```sql
CREATE TABLE dbo.UserAccount
(
    UserId             uniqueidentifier NOT NULL
        CONSTRAINT DF_UserAccount_Id DEFAULT NEWSEQUENTIALID(),

    ExternalIdentityId nvarchar(300)    NOT NULL,
    Email              nvarchar(320)    NOT NULL,
    DisplayName        nvarchar(200)    NOT NULL,
    AvatarUrl           nvarchar(1000)  NULL,

    IsActive           bit              NOT NULL
        CONSTRAINT DF_UserAccount_IsActive DEFAULT (1),

    LastLoginAtUtc     datetime2(3)     NULL,
    CreatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_UserAccount_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_UserAccount_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    RowVersion         rowversion       NOT NULL,

    CONSTRAINT PK_UserAccount PRIMARY KEY (UserId),
    CONSTRAINT UQ_UserAccount_ExternalIdentity UNIQUE (ExternalIdentityId)
);
GO

CREATE INDEX IX_UserAccount_Email
    ON dbo.UserAccount (Email);
GO
```

## 5.3 OrganizationMembership

```sql
CREATE TABLE dbo.OrganizationMembership
(
    OrganizationMembershipId uniqueidentifier NOT NULL
        CONSTRAINT DF_OrganizationMembership_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId          uniqueidentifier NOT NULL,
    UserId                  uniqueidentifier NOT NULL,

    RoleCode                varchar(50)      NOT NULL,
    IsActive                bit              NOT NULL
        CONSTRAINT DF_OrganizationMembership_IsActive DEFAULT (1),

    CreatedAtUtc            datetime2(3)     NOT NULL
        CONSTRAINT DF_OrganizationMembership_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc            datetime2(3)     NOT NULL
        CONSTRAINT DF_OrganizationMembership_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OrganizationMembership PRIMARY KEY (OrganizationMembershipId),
    CONSTRAINT UQ_OrganizationMembership UNIQUE (OrganizationId, UserId),

    CONSTRAINT FK_OrganizationMembership_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_OrganizationMembership_User
        FOREIGN KEY (UserId) REFERENCES dbo.UserAccount(UserId)
);
GO
```

---

# 6. Projects

## 6.1 Project

```sql
CREATE TABLE dbo.Project
(
    ProjectId          uniqueidentifier NOT NULL
        CONSTRAINT DF_Project_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId     uniqueidentifier NOT NULL,
    Name               nvarchar(200)    NOT NULL,
    Description        nvarchar(1000)   NULL,

    ExternalProjectId  nvarchar(300)    NULL,
    ExternalProjectUrl nvarchar(1000)   NULL,

    IsActive           bit              NOT NULL
        CONSTRAINT DF_Project_IsActive DEFAULT (1),

    CreatedByUserId    uniqueidentifier NOT NULL,
    CreatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Project_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Project_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    RowVersion         rowversion       NOT NULL,

    CONSTRAINT PK_Project PRIMARY KEY (ProjectId),

    CONSTRAINT FK_Project_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_Project_CreatedBy
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.UserAccount(UserId)
);
GO

CREATE UNIQUE INDEX UX_Project_Organization_Name
    ON dbo.Project (OrganizationId, Name)
    WHERE IsActive = 1;
GO
```

## 6.2 ProjectMember

```sql
CREATE TABLE dbo.ProjectMember
(
    ProjectMemberId uniqueidentifier NOT NULL
        CONSTRAINT DF_ProjectMember_Id DEFAULT NEWSEQUENTIALID(),

    ProjectId       uniqueidentifier NOT NULL,
    UserId          uniqueidentifier NOT NULL,
    RoleCode        varchar(50)      NOT NULL,

    CreatedAtUtc    datetime2(3)     NOT NULL
        CONSTRAINT DF_ProjectMember_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ProjectMember PRIMARY KEY (ProjectMemberId),
    CONSTRAINT UQ_ProjectMember UNIQUE (ProjectId, UserId),

    CONSTRAINT FK_ProjectMember_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_ProjectMember_User
        FOREIGN KEY (UserId) REFERENCES dbo.UserAccount(UserId)
);
GO
```

---

# 7. Repositories, Branches, Environments

## 7.1 Repository

```sql
CREATE TABLE dbo.Repository
(
    RepositoryId       uniqueidentifier NOT NULL
        CONSTRAINT DF_Repository_Id DEFAULT NEWSEQUENTIALID(),

    ProjectId          uniqueidentifier NOT NULL,
    IntegrationProviderId tinyint       NULL,

    Name               nvarchar(200)    NOT NULL,
    ProviderRepositoryId nvarchar(300)  NULL,
    CloneUrl           nvarchar(1000)   NULL,
    WebUrl             nvarchar(1000)   NULL,
    DefaultBranchName  nvarchar(200)    NULL,

    IsActive           bit              NOT NULL
        CONSTRAINT DF_Repository_IsActive DEFAULT (1),

    LastIndexedAtUtc   datetime2(3)     NULL,
    CreatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Repository_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Repository_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    RowVersion         rowversion       NOT NULL,

    CONSTRAINT PK_Repository PRIMARY KEY (RepositoryId),

    CONSTRAINT FK_Repository_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_Repository_Provider
        FOREIGN KEY (IntegrationProviderId)
        REFERENCES dbo.IntegrationProvider(IntegrationProviderId)
);
GO

CREATE UNIQUE INDEX UX_Repository_Project_Name
    ON dbo.Repository(ProjectId, Name);
GO
```

## 7.2 Branch

```sql
CREATE TABLE dbo.Branch
(
    BranchId              uniqueidentifier NOT NULL
        CONSTRAINT DF_Branch_Id DEFAULT NEWSEQUENTIALID(),

    RepositoryId          uniqueidentifier NOT NULL,
    Name                  nvarchar(300)    NOT NULL,
    ProviderBranchId      nvarchar(300)    NULL,
    IsDefault             bit              NOT NULL
        CONSTRAINT DF_Branch_IsDefault DEFAULT (0),
    IsActive              bit              NOT NULL
        CONSTRAINT DF_Branch_IsActive DEFAULT (1),

    LastIndexedCommitId   uniqueidentifier NULL,

    CreatedAtUtc          datetime2(3)     NOT NULL
        CONSTRAINT DF_Branch_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Branch PRIMARY KEY (BranchId),
    CONSTRAINT UQ_Branch_Repository_Name UNIQUE (RepositoryId, Name),

    CONSTRAINT FK_Branch_Repository
        FOREIGN KEY (RepositoryId) REFERENCES dbo.Repository(RepositoryId)
);
GO
```

## 7.3 Environment

```sql
CREATE TABLE dbo.Environment
(
    EnvironmentId uniqueidentifier NOT NULL
        CONSTRAINT DF_Environment_Id DEFAULT NEWSEQUENTIALID(),

    ProjectId     uniqueidentifier NOT NULL,
    Name          nvarchar(100)    NOT NULL,
    Description   nvarchar(500)    NULL,

    CreatedAtUtc  datetime2(3)     NOT NULL
        CONSTRAINT DF_Environment_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Environment PRIMARY KEY (EnvironmentId),
    CONSTRAINT UQ_Environment_Project_Name UNIQUE (ProjectId, Name),

    CONSTRAINT FK_Environment_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId)
);
GO
```

---

# 8. Source Ingestion

## 8.1 IngestionRun

Every indexing operation gets an immutable run.

```sql
CREATE TABLE dbo.IngestionRun
(
    IngestionRunId      uniqueidentifier NOT NULL
        CONSTRAINT DF_IngestionRun_Id DEFAULT NEWSEQUENTIALID(),

    ProjectId           uniqueidentifier NOT NULL,
    RepositoryId        uniqueidentifier NOT NULL,
    BranchId            uniqueidentifier NULL,

    TriggerType         varchar(40)      NOT NULL,
    StartedAtUtc        datetime2(3)     NOT NULL
        CONSTRAINT DF_IngestionRun_StartedAtUtc DEFAULT SYSUTCDATETIME(),
    CompletedAtUtc      datetime2(3)     NULL,

    StatusCode          varchar(30)      NOT NULL,
    ErrorMessage        nvarchar(max)    NULL,

    FilesDiscovered     int              NOT NULL DEFAULT 0,
    FilesParsed         int              NOT NULL DEFAULT 0,
    FilesFailed         int              NOT NULL DEFAULT 0,

    CONSTRAINT PK_IngestionRun PRIMARY KEY (IngestionRunId),

    CONSTRAINT FK_IngestionRun_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_IngestionRun_Repository
        FOREIGN KEY (RepositoryId) REFERENCES dbo.Repository(RepositoryId),

    CONSTRAINT FK_IngestionRun_Branch
        FOREIGN KEY (BranchId) REFERENCES dbo.Branch(BranchId)
);
GO
```

## 8.2 CodeFile

A logical source file.

```sql
CREATE TABLE dbo.CodeFile
(
    CodeFileId          uniqueidentifier NOT NULL
        CONSTRAINT DF_CodeFile_Id DEFAULT NEWSEQUENTIALID(),

    RepositoryId        uniqueidentifier NOT NULL,
    RelativePath        nvarchar(1000)   NOT NULL,

    LanguageCode        varchar(50)      NULL,
    FileExtension       varchar(30)      NULL,

    IsDeleted           bit              NOT NULL
        CONSTRAINT DF_CodeFile_IsDeleted DEFAULT (0),

    CurrentContentHash  varchar(128)     NULL,

    CreatedAtUtc        datetime2(3)     NOT NULL
        CONSTRAINT DF_CodeFile_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc        datetime2(3)     NOT NULL
        CONSTRAINT DF_CodeFile_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_CodeFile PRIMARY KEY (CodeFileId),
    CONSTRAINT UQ_CodeFile_Repository_Path UNIQUE (RepositoryId, RelativePath),

    CONSTRAINT FK_CodeFile_Repository
        FOREIGN KEY (RepositoryId) REFERENCES dbo.Repository(RepositoryId)
);
GO
```

## 8.3 CodeFileVersion

Immutable file snapshot associated with an ingestion run.

```sql
CREATE TABLE dbo.CodeFileVersion
(
    CodeFileVersionId uniqueidentifier NOT NULL
        CONSTRAINT DF_CodeFileVersion_Id DEFAULT NEWSEQUENTIALID(),

    CodeFileId        uniqueidentifier NOT NULL,
    IngestionRunId    uniqueidentifier NOT NULL,

    ContentHash       varchar(128)     NOT NULL,
    ContentUri        nvarchar(2000)   NULL,
    MaskedContentUri  nvarchar(2000)   NULL,

    ParserStatusCode  varchar(30)      NOT NULL,
    ParserError       nvarchar(max)    NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_CodeFileVersion_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_CodeFileVersion PRIMARY KEY (CodeFileVersionId),

    CONSTRAINT FK_CodeFileVersion_File
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId),

    CONSTRAINT FK_CodeFileVersion_Ingestion
        FOREIGN KEY (IngestionRunId) REFERENCES dbo.IngestionRun(IngestionRunId)
);
GO

CREATE INDEX IX_CodeFileVersion_File_Run
    ON dbo.CodeFileVersion(CodeFileId, IngestionRunId);
GO
```

> Large source contents are preferably stored in object/blob storage. SQL Server stores metadata and secure references.

---

# 9. Code Symbols

## 9.1 CodeSymbol

Represents classes, interfaces, methods, components, functions, etc.

```sql
CREATE TABLE dbo.CodeSymbol
(
    CodeSymbolId       uniqueidentifier NOT NULL
        CONSTRAINT DF_CodeSymbol_Id DEFAULT NEWSEQUENTIALID(),

    CodeFileVersionId  uniqueidentifier NOT NULL,
    ComponentTypeId    tinyint          NOT NULL,

    SymbolName         nvarchar(500)    NOT NULL,
    FullyQualifiedName nvarchar(1000)   NULL,
    Signature          nvarchar(2000)   NULL,

    StartLine          int              NULL,
    StartColumn        int              NULL,
    EndLine            int              NULL,
    EndColumn          int              NULL,

    IsPartial          bit              NOT NULL DEFAULT 0,

    CONSTRAINT PK_CodeSymbol PRIMARY KEY (CodeSymbolId),

    CONSTRAINT FK_CodeSymbol_FileVersion
        FOREIGN KEY (CodeFileVersionId)
        REFERENCES dbo.CodeFileVersion(CodeFileVersionId),

    CONSTRAINT FK_CodeSymbol_ComponentType
        FOREIGN KEY (ComponentTypeId)
        REFERENCES dbo.ComponentType(ComponentTypeId),

    CONSTRAINT CK_CodeSymbol_LineRange
        CHECK
        (
            (StartLine IS NULL AND EndLine IS NULL)
            OR
            (StartLine IS NOT NULL AND EndLine IS NOT NULL AND EndLine >= StartLine)
        )
);
GO

CREATE INDEX IX_CodeSymbol_File
    ON dbo.CodeSymbol(CodeFileVersionId);
GO

CREATE INDEX IX_CodeSymbol_Name
    ON dbo.CodeSymbol(SymbolName);
GO
```

---

# 10. Dependency Graph

## 10.1 GraphSnapshot

The graph is versioned per ingestion.

```sql
CREATE TABLE dbo.GraphSnapshot
(
    GraphSnapshotId uniqueidentifier NOT NULL
        CONSTRAINT DF_GraphSnapshot_Id DEFAULT NEWSEQUENTIALID(),

    ProjectId       uniqueidentifier NOT NULL,
    IngestionRunId  uniqueidentifier NOT NULL,

    IsCurrent       bit              NOT NULL DEFAULT 1,
    CreatedAtUtc    datetime2(3)     NOT NULL
        CONSTRAINT DF_GraphSnapshot_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_GraphSnapshot PRIMARY KEY (GraphSnapshotId),

    CONSTRAINT FK_GraphSnapshot_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_GraphSnapshot_Ingestion
        FOREIGN KEY (IngestionRunId) REFERENCES dbo.IngestionRun(IngestionRunId)
);
GO

CREATE UNIQUE INDEX UX_GraphSnapshot_Current
    ON dbo.GraphSnapshot(ProjectId)
    WHERE IsCurrent = 1;
GO
```

## 10.2 GraphNode

```sql
CREATE TABLE dbo.GraphNode
(
    GraphNodeId       uniqueidentifier NOT NULL
        CONSTRAINT DF_GraphNode_Id DEFAULT NEWSEQUENTIALID(),

    GraphSnapshotId   uniqueidentifier NOT NULL,
    ComponentTypeId   tinyint          NOT NULL,

    ExternalKey       nvarchar(1000)   NOT NULL,
    DisplayName       nvarchar(500)    NOT NULL,

    CodeSymbolId      uniqueidentifier NULL,
    CodeFileId        uniqueidentifier NULL,

    IsParsed          bit              NOT NULL DEFAULT 1,
    IsPartial         bit              NOT NULL DEFAULT 0,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_GraphNode_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_GraphNode PRIMARY KEY (GraphNodeId),

    CONSTRAINT UQ_GraphNode_Snapshot_ExternalKey
        UNIQUE (GraphSnapshotId, ExternalKey),

    CONSTRAINT FK_GraphNode_Snapshot
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshot(GraphSnapshotId),

    CONSTRAINT FK_GraphNode_ComponentType
        FOREIGN KEY (ComponentTypeId) REFERENCES dbo.ComponentType(ComponentTypeId),

    CONSTRAINT FK_GraphNode_CodeSymbol
        FOREIGN KEY (CodeSymbolId) REFERENCES dbo.CodeSymbol(CodeSymbolId),

    CONSTRAINT FK_GraphNode_CodeFile
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId)
);
GO
```

## 10.3 GraphEdge

```sql
CREATE TABLE dbo.GraphEdge
(
    GraphEdgeId       uniqueidentifier NOT NULL
        CONSTRAINT DF_GraphEdge_Id DEFAULT NEWSEQUENTIALID(),

    GraphSnapshotId   uniqueidentifier NOT NULL,
    GraphEdgeTypeId   tinyint          NOT NULL,

    SourceNodeId      uniqueidentifier NOT NULL,
    TargetNodeId      uniqueidentifier NOT NULL,

    Confidence        decimal(5,4)     NOT NULL,
    TraversalCost     decimal(10,4)    NOT NULL,

    IsRuntimeResolved bit              NOT NULL DEFAULT 0,

    ParserRule        varchar(200)     NULL,
    SourceCodeFileId  uniqueidentifier NULL,
    SourceLine        int              NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_GraphEdge_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_GraphEdge PRIMARY KEY (GraphEdgeId),

    CONSTRAINT UQ_GraphEdge_Identity
        UNIQUE
        (
            GraphSnapshotId,
            SourceNodeId,
            TargetNodeId,
            GraphEdgeTypeId
        ),

    CONSTRAINT FK_GraphEdge_Snapshot
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshot(GraphSnapshotId),

    CONSTRAINT FK_GraphEdge_Type
        FOREIGN KEY (GraphEdgeTypeId) REFERENCES dbo.GraphEdgeType(GraphEdgeTypeId),

    CONSTRAINT FK_GraphEdge_Source
        FOREIGN KEY (SourceNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_GraphEdge_Target
        FOREIGN KEY (TargetNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_GraphEdge_SourceFile
        FOREIGN KEY (SourceCodeFileId) REFERENCES dbo.CodeFile(CodeFileId),

    CONSTRAINT CK_GraphEdge_Confidence
        CHECK (Confidence >= 0 AND Confidence <= 1),

    CONSTRAINT CK_GraphEdge_Cost
        CHECK (TraversalCost >= 0),

    CONSTRAINT CK_GraphEdge_NotSelfReferential
        CHECK (SourceNodeId <> TargetNodeId)
);
GO

CREATE INDEX IX_GraphEdge_Source
    ON dbo.GraphEdge(SourceNodeId);

CREATE INDEX IX_GraphEdge_Target
    ON dbo.GraphEdge(TargetNodeId);

CREATE INDEX IX_GraphEdge_Snapshot_Type
    ON dbo.GraphEdge(GraphSnapshotId, GraphEdgeTypeId);
GO
```

---

# 11. Change Management

## 11.1 Change

Common normalized identity for all change sources.

```sql
CREATE TABLE dbo.Change
(
    ChangeId           uniqueidentifier NOT NULL
        CONSTRAINT DF_Change_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId     uniqueidentifier NOT NULL,
    ProjectId          uniqueidentifier NOT NULL,
    ChangeTypeId       tinyint          NOT NULL,

    Title              nvarchar(500)    NOT NULL,
    Description        nvarchar(max)    NULL,

    ExternalChangeId   nvarchar(300)    NULL,
    ExternalUrl        nvarchar(2000)   NULL,

    AuthorUserId       uniqueidentifier NULL,

    CreatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Change_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Change_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Change PRIMARY KEY (ChangeId),

    CONSTRAINT FK_Change_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_Change_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_Change_Type
        FOREIGN KEY (ChangeTypeId) REFERENCES dbo.ChangeType(ChangeTypeId),

    CONSTRAINT FK_Change_Author
        FOREIGN KEY (AuthorUserId) REFERENCES dbo.UserAccount(UserId)
);
GO

CREATE INDEX IX_Change_Project_Created
    ON dbo.Change(ProjectId, CreatedAtUtc DESC);

CREATE INDEX IX_Change_Project_Type
    ON dbo.Change(ProjectId, ChangeTypeId);
GO
```

## 11.2 PullRequest

```sql
CREATE TABLE dbo.PullRequest
(
    PullRequestId       uniqueidentifier NOT NULL
        CONSTRAINT DF_PullRequest_Id DEFAULT NEWSEQUENTIALID(),

    ChangeId            uniqueidentifier NOT NULL,
    RepositoryId        uniqueidentifier NOT NULL,

    ProviderPullRequestId nvarchar(300) NOT NULL,

    SourceBranchName    nvarchar(300) NULL,
    TargetBranchName    nvarchar(300) NULL,

    ProviderStatusCode  varchar(50) NULL,
    FilesChangedCount   int NULL,
    LinesAdded          int NULL,
    LinesDeleted        int NULL,

    CreatedAtUtc        datetime2(3) NOT NULL
        CONSTRAINT DF_PullRequest_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PullRequest PRIMARY KEY (PullRequestId),
    CONSTRAINT UQ_PullRequest_Provider
        UNIQUE (RepositoryId, ProviderPullRequestId),

    CONSTRAINT FK_PullRequest_Change
        FOREIGN KEY (ChangeId) REFERENCES dbo.Change(ChangeId),

    CONSTRAINT FK_PullRequest_Repository
        FOREIGN KEY (RepositoryId) REFERENCES dbo.Repository(RepositoryId)
);
GO
```

## 11.3 WorkItem

```sql
CREATE TABLE dbo.WorkItem
(
    WorkItemId             uniqueidentifier NOT NULL
        CONSTRAINT DF_WorkItem_Id DEFAULT NEWSEQUENTIALID(),

    ChangeId               uniqueidentifier NOT NULL,
    ExternalWorkItemId     nvarchar(300)    NOT NULL,

    Priority               nvarchar(100)    NULL,
    AssignedTo             nvarchar(320)    NULL,
    State                  nvarchar(100)    NULL,

    ProviderUpdatedAtUtc   datetime2(3)     NULL,

    CONSTRAINT PK_WorkItem PRIMARY KEY (WorkItemId),
    CONSTRAINT UQ_WorkItem_Provider
        UNIQUE (ChangeId, ExternalWorkItemId),

    CONSTRAINT FK_WorkItem_Change
        FOREIGN KEY (ChangeId) REFERENCES dbo.Change(ChangeId)
);
GO
```

## 11.4 Commit

```sql
CREATE TABLE dbo.Commit
(
    CommitId          uniqueidentifier NOT NULL
        CONSTRAINT DF_Commit_Id DEFAULT NEWSEQUENTIALID(),

    RepositoryId      uniqueidentifier NOT NULL,
    ChangeId          uniqueidentifier NULL,

    CommitHash        varchar(128)     NOT NULL,
    ParentHash        varchar(128)     NULL,
    Message           nvarchar(2000)   NULL,
    AuthorName        nvarchar(300)    NULL,
    AuthorEmail       nvarchar(320)    NULL,

    CommittedAtUtc    datetime2(3)     NULL,

    CONSTRAINT PK_Commit PRIMARY KEY (CommitId),
    CONSTRAINT UQ_Commit_Repository_Hash UNIQUE (RepositoryId, CommitHash),

    CONSTRAINT FK_Commit_Repository
        FOREIGN KEY (RepositoryId) REFERENCES dbo.Repository(RepositoryId),

    CONSTRAINT FK_Commit_Change
        FOREIGN KEY (ChangeId) REFERENCES dbo.Change(ChangeId)
);
GO
```

## 11.5 ChangeFile

```sql
CREATE TABLE dbo.ChangeFile
(
    ChangeFileId     uniqueidentifier NOT NULL
        CONSTRAINT DF_ChangeFile_Id DEFAULT NEWSEQUENTIALID(),

    ChangeId         uniqueidentifier NOT NULL,
    CodeFileId       uniqueidentifier NULL,

    RelativePath     nvarchar(1000)    NOT NULL,

    ChangeKindCode   varchar(30)       NOT NULL,
    OldPath          nvarchar(1000)    NULL,
    NewPath          nvarchar(1000)    NULL,

    LinesAdded       int               NULL,
    LinesDeleted     int               NULL,

    CONSTRAINT PK_ChangeFile PRIMARY KEY (ChangeFileId),

    CONSTRAINT UQ_ChangeFile_Path
        UNIQUE (ChangeId, RelativePath),

    CONSTRAINT FK_ChangeFile_Change
        FOREIGN KEY (ChangeId) REFERENCES dbo.Change(ChangeId),

    CONSTRAINT FK_ChangeFile_CodeFile
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId)
);
GO
```

---

# 12. Analysis

## 12.1 Analysis

```sql
CREATE TABLE dbo.Analysis
(
    AnalysisId             uniqueidentifier NOT NULL
        CONSTRAINT DF_Analysis_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId         uniqueidentifier NOT NULL,
    ProjectId              uniqueidentifier NOT NULL,
    ChangeId               uniqueidentifier NULL,

    GraphSnapshotId        uniqueidentifier NULL,
    EnvironmentId          uniqueidentifier NULL,

    AnalysisStatusId       tinyint          NOT NULL,
    OverallRiskStateId     tinyint          NULL,

    MaxDepth               smallint         NOT NULL DEFAULT 6,
    ConfidenceThreshold    decimal(5,4)     NOT NULL DEFAULT 0.7000,

    IncludeSecurityScan    bit              NOT NULL DEFAULT 1,
    IncludeAIExplanation   bit              NOT NULL DEFAULT 1,
    IncludeExternalApis    bit              NOT NULL DEFAULT 1,
    TraceTransitive        bit              NOT NULL DEFAULT 1,

    IsEvidenceComplete     bit              NOT NULL DEFAULT 0,
    IsDeterministicComplete bit             NOT NULL DEFAULT 0,

    StartedAtUtc           datetime2(3)     NULL,
    CompletedAtUtc         datetime2(3)     NULL,

    RequestedByUserId      uniqueidentifier NOT NULL,

    CreatedAtUtc           datetime2(3)     NOT NULL
        CONSTRAINT DF_Analysis_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    RowVersion             rowversion       NOT NULL,

    CONSTRAINT PK_Analysis PRIMARY KEY (AnalysisId),

    CONSTRAINT FK_Analysis_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_Analysis_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_Analysis_Change
        FOREIGN KEY (ChangeId) REFERENCES dbo.Change(ChangeId),

    CONSTRAINT FK_Analysis_Graph
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshot(GraphSnapshotId),

    CONSTRAINT FK_Analysis_Environment
        FOREIGN KEY (EnvironmentId) REFERENCES dbo.Environment(EnvironmentId),

    CONSTRAINT FK_Analysis_Status
        FOREIGN KEY (AnalysisStatusId) REFERENCES dbo.AnalysisStatus(AnalysisStatusId),

    CONSTRAINT FK_Analysis_Risk
        FOREIGN KEY (OverallRiskStateId) REFERENCES dbo.RiskState(RiskStateId),

    CONSTRAINT FK_Analysis_RequestedBy
        FOREIGN KEY (RequestedByUserId) REFERENCES dbo.UserAccount(UserId),

    CONSTRAINT CK_Analysis_Confidence
        CHECK (ConfidenceThreshold >= 0 AND ConfidenceThreshold <= 1),

    CONSTRAINT CK_Analysis_MaxDepth
        CHECK (MaxDepth > 0)
);
GO

CREATE INDEX IX_Analysis_Project_Date
    ON dbo.Analysis(ProjectId, CreatedAtUtc DESC);

CREATE INDEX IX_Analysis_Project_Risk
    ON dbo.Analysis(ProjectId, OverallRiskStateId);

CREATE INDEX IX_Analysis_Status
    ON dbo.Analysis(AnalysisStatusId);
GO
```

## 12.2 AnalysisTarget

Separates what is being analyzed from the analysis configuration.

```sql
CREATE TABLE dbo.AnalysisTarget
(
    AnalysisTargetId uniqueidentifier NOT NULL
        CONSTRAINT DF_AnalysisTarget_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId       uniqueidentifier NOT NULL,
    CodeFileId       uniqueidentifier NULL,
    GraphNodeId      uniqueidentifier NULL,

    TargetReasonCode varchar(50)       NULL,

    CONSTRAINT PK_AnalysisTarget PRIMARY KEY (AnalysisTargetId),

    CONSTRAINT UQ_AnalysisTarget
        UNIQUE (AnalysisId, CodeFileId, GraphNodeId),

    CONSTRAINT FK_AnalysisTarget_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_AnalysisTarget_File
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId),

    CONSTRAINT FK_AnalysisTarget_Node
        FOREIGN KEY (GraphNodeId) REFERENCES dbo.GraphNode(GraphNodeId)
);
GO
```

## 12.3 AnalysisScope

```sql
CREATE TABLE dbo.AnalysisScope
(
    AnalysisScopeId             uniqueidentifier NOT NULL
        CONSTRAINT DF_AnalysisScope_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId                 uniqueidentifier NOT NULL,

    ScanDirectDependencies     bit NOT NULL,
    TraceTransitiveDependencies bit NOT NULL,
    IncludeExternalBindings    bit NOT NULL,
    RunStaticSecurityAnalysis  bit NOT NULL,

    CreatedAtUtc               datetime2(3) NOT NULL
        CONSTRAINT DF_AnalysisScope_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AnalysisScope PRIMARY KEY (AnalysisScopeId),
    CONSTRAINT UQ_AnalysisScope_Analysis UNIQUE (AnalysisId),

    CONSTRAINT FK_AnalysisScope_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId)
);
GO
```

---

# 13. Analysis Progress

## 13.1 AnalysisStage

```sql
CREATE TABLE dbo.AnalysisStage
(
    AnalysisStageId tinyint       NOT NULL,
    Code            varchar(60)   NOT NULL,
    DisplayName     nvarchar(200) NOT NULL,
    SortOrder       tinyint       NOT NULL,

    CONSTRAINT PK_AnalysisStage PRIMARY KEY (AnalysisStageId),
    CONSTRAINT UQ_AnalysisStage_Code UNIQUE (Code)
);
GO
```

Seed:

```text
READING_CHANGES
IDENTIFYING_TARGETS
BUILDING_BLAST_RADIUS
SECURITY_SCAN
RETRIEVING_PROJECT_CONTEXT
VALIDATING_DEPENDENCIES
GENERATING_REPORT
```

## 13.2 AnalysisProgress

```sql
CREATE TABLE dbo.AnalysisProgress
(
    AnalysisProgressId uniqueidentifier NOT NULL
        CONSTRAINT DF_AnalysisProgress_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId         uniqueidentifier NOT NULL,
    AnalysisStageId    tinyint          NOT NULL,

    StatusCode         varchar(30)      NOT NULL,
    PercentComplete    decimal(5,2)     NOT NULL DEFAULT 0,

    StartedAtUtc       datetime2(3)     NULL,
    CompletedAtUtc     datetime2(3)     NULL,

    ComponentsTraced   int              NULL,
    EstimatedChains    int              NULL,

    ErrorMessage       nvarchar(max)    NULL,

    CONSTRAINT PK_AnalysisProgress PRIMARY KEY (AnalysisProgressId),

    CONSTRAINT UQ_AnalysisProgress
        UNIQUE (AnalysisId, AnalysisStageId),

    CONSTRAINT FK_AnalysisProgress_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_AnalysisProgress_Stage
        FOREIGN KEY (AnalysisStageId) REFERENCES dbo.AnalysisStage(AnalysisStageId),

    CONSTRAINT CK_AnalysisProgress_Percent
        CHECK (PercentComplete >= 0 AND PercentComplete <= 100)
);
GO
```

## 13.3 AnalysisTelemetry

```sql
CREATE TABLE dbo.AnalysisTelemetry
(
    AnalysisTelemetryId uniqueidentifier NOT NULL
        CONSTRAINT DF_AnalysisTelemetry_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId          uniqueidentifier NOT NULL,
    AnalysisStageId     tinyint          NULL,

    LogLevelCode        varchar(20)      NOT NULL,
    Message             nvarchar(max)    NOT NULL,

    CreatedAtUtc        datetime2(3)     NOT NULL
        CONSTRAINT DF_AnalysisTelemetry_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AnalysisTelemetry PRIMARY KEY (AnalysisTelemetryId),

    CONSTRAINT FK_AnalysisTelemetry_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_AnalysisTelemetry_Stage
        FOREIGN KEY (AnalysisStageId) REFERENCES dbo.AnalysisStage(AnalysisStageId)
);
GO

CREATE INDEX IX_AnalysisTelemetry_Analysis_Date
    ON dbo.AnalysisTelemetry(AnalysisId, CreatedAtUtc);
GO
```

---

# 14. Deterministic Analysis Results

## 14.1 AnalysisNodeResult

One result per graph node in an analysis.

```sql
CREATE TABLE dbo.AnalysisNodeResult
(
    AnalysisNodeResultId uniqueidentifier NOT NULL
        CONSTRAINT DF_AnalysisNodeResult_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId           uniqueidentifier NOT NULL,
    GraphNodeId          uniqueidentifier NOT NULL,

    RiskStateId          tinyint          NOT NULL,

    Distance             int              NULL,
    MinPathConfidence    decimal(5,4)     NULL,

    RuleCode             varchar(30)      NULL,
    ReasonCode           varchar(100)     NULL,

    IsDirectlyChanged    bit              NOT NULL DEFAULT 0,
    IsSecurityAffected   bit              NOT NULL DEFAULT 0,
    IsRuntimeResolved   bit              NOT NULL DEFAULT 0,
    IsBeyondDepth        bit              NOT NULL DEFAULT 0,

    CreatedAtUtc         datetime2(3)     NOT NULL
        CONSTRAINT DF_AnalysisNodeResult_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AnalysisNodeResult PRIMARY KEY (AnalysisNodeResultId),

    CONSTRAINT UQ_AnalysisNodeResult
        UNIQUE (AnalysisId, GraphNodeId),

    CONSTRAINT FK_AnalysisNodeResult_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_AnalysisNodeResult_Node
        FOREIGN KEY (GraphNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_AnalysisNodeResult_Risk
        FOREIGN KEY (RiskStateId) REFERENCES dbo.RiskState(RiskStateId),

    CONSTRAINT CK_AnalysisNodeResult_Confidence
        CHECK
        (
            MinPathConfidence IS NULL
            OR (MinPathConfidence >= 0 AND MinPathConfidence <= 1)
        )
);
GO

CREATE INDEX IX_AnalysisNodeResult_Risk
    ON dbo.AnalysisNodeResult(AnalysisId, RiskStateId);

CREATE INDEX IX_AnalysisNodeResult_Distance
    ON dbo.AnalysisNodeResult(AnalysisId, Distance);
GO
```

---

# 15. Impact Paths

A path is a deterministic sequence of graph edges.

## 15.1 ImpactPath

```sql
CREATE TABLE dbo.ImpactPath
(
    ImpactPathId        uniqueidentifier NOT NULL
        CONSTRAINT DF_ImpactPath_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId          uniqueidentifier NOT NULL,
    TargetNodeId        uniqueidentifier NOT NULL,
    ResultNodeId        uniqueidentifier NOT NULL,

    PathDistance        int              NOT NULL,
    PathConfidence      decimal(5,4)     NOT NULL,

    ContainsRuntimeEdge bit              NOT NULL DEFAULT 0,

    CreatedAtUtc        datetime2(3)     NOT NULL
        CONSTRAINT DF_ImpactPath_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ImpactPath PRIMARY KEY (ImpactPathId),

    CONSTRAINT FK_ImpactPath_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_ImpactPath_Target
        FOREIGN KEY (TargetNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_ImpactPath_Result
        FOREIGN KEY (ResultNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT CK_ImpactPath_Distance CHECK (PathDistance >= 0),
    CONSTRAINT CK_ImpactPath_Confidence
        CHECK (PathConfidence >= 0 AND PathConfidence <= 1)
);
GO
```

## 15.2 ImpactPathEdge

```sql
CREATE TABLE dbo.ImpactPathEdge
(
    ImpactPathId  uniqueidentifier NOT NULL,
    SequenceNo    int              NOT NULL,
    GraphEdgeId   uniqueidentifier NOT NULL,

    CONSTRAINT PK_ImpactPathEdge
        PRIMARY KEY (ImpactPathId, SequenceNo),

    CONSTRAINT UQ_ImpactPathEdge
        UNIQUE (ImpactPathId, GraphEdgeId),

    CONSTRAINT FK_ImpactPathEdge_Path
        FOREIGN KEY (ImpactPathId) REFERENCES dbo.ImpactPath(ImpactPathId),

    CONSTRAINT FK_ImpactPathEdge_Edge
        FOREIGN KEY (GraphEdgeId) REFERENCES dbo.GraphEdge(GraphEdgeId),

    CONSTRAINT CK_ImpactPathEdge_Sequence
        CHECK (SequenceNo >= 1)
);
GO
```

This preserves the complete evidence path without storing a comma-separated list of IDs.

---

# 16. Evidence

## 16.1 Evidence

```sql
CREATE TABLE dbo.Evidence
(
    EvidenceId         uniqueidentifier NOT NULL
        CONSTRAINT DF_Evidence_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId         uniqueidentifier NOT NULL,
    EvidenceTypeId     tinyint          NOT NULL,

    GraphNodeId        uniqueidentifier NULL,
    GraphEdgeId        uniqueidentifier NULL,
    ImpactPathId       uniqueidentifier NULL,

    CodeFileId         uniqueidentifier NULL,
    CodeFileVersionId  uniqueidentifier NULL,

    Title              nvarchar(500)    NOT NULL,
    Description        nvarchar(max)    NULL,

    SourceLineStart    int              NULL,
    SourceLineEnd      int              NULL,

    ParserRule         varchar(300)     NULL,
    Confidence         decimal(5,4)     NULL,

    SourceSnippet      nvarchar(max)    NULL,

    CreatedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Evidence_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Evidence PRIMARY KEY (EvidenceId),

    CONSTRAINT FK_Evidence_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_Evidence_Type
        FOREIGN KEY (EvidenceTypeId) REFERENCES dbo.EvidenceType(EvidenceTypeId),

    CONSTRAINT FK_Evidence_Node
        FOREIGN KEY (GraphNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_Evidence_Edge
        FOREIGN KEY (GraphEdgeId) REFERENCES dbo.GraphEdge(GraphEdgeId),

    CONSTRAINT FK_Evidence_Path
        FOREIGN KEY (ImpactPathId) REFERENCES dbo.ImpactPath(ImpactPathId),

    CONSTRAINT FK_Evidence_File
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId),

    CONSTRAINT FK_Evidence_FileVersion
        FOREIGN KEY (CodeFileVersionId) REFERENCES dbo.CodeFileVersion(CodeFileVersionId),

    CONSTRAINT CK_Evidence_Confidence
        CHECK
        (
            Confidence IS NULL
            OR (Confidence >= 0 AND Confidence <= 1)
        )
);
GO

CREATE INDEX IX_Evidence_Analysis
    ON dbo.Evidence(AnalysisId);

CREATE INDEX IX_Evidence_Node
    ON dbo.Evidence(GraphNodeId);

CREATE INDEX IX_Evidence_Type
    ON dbo.Evidence(AnalysisId, EvidenceTypeId);
GO
```

---

# 17. Security Domain

## 17.1 SecurityScanner

```sql
CREATE TABLE dbo.SecurityScanner
(
    SecurityScannerId uniqueidentifier NOT NULL
        CONSTRAINT DF_SecurityScanner_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId    uniqueidentifier NOT NULL,

    Name              nvarchar(200)    NOT NULL,
    Code              varchar(100)     NOT NULL,
    Version           nvarchar(100)    NULL,

    IsAvailable       bit              NOT NULL DEFAULT 1,

    CONSTRAINT PK_SecurityScanner PRIMARY KEY (SecurityScannerId),
    CONSTRAINT UQ_SecurityScanner_Org_Code UNIQUE (OrganizationId, Code),

    CONSTRAINT FK_SecurityScanner_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO
```

## 17.2 SecurityRule

```sql
CREATE TABLE dbo.SecurityRule
(
    SecurityRuleId     uniqueidentifier NOT NULL
        CONSTRAINT DF_SecurityRule_Id DEFAULT NEWSEQUENTIALID(),

    SecurityScannerId   uniqueidentifier NOT NULL,
    RuleCode            varchar(200)     NOT NULL,
    Name                nvarchar(300)    NOT NULL,

    Description         nvarchar(max)    NULL,
    CategoryCode        varchar(100)     NULL,

    SecuritySeverityId  tinyint          NOT NULL,

    IsActive            bit              NOT NULL DEFAULT 1,

    CONSTRAINT PK_SecurityRule PRIMARY KEY (SecurityRuleId),
    CONSTRAINT UQ_SecurityRule_Scanner_Code
        UNIQUE (SecurityScannerId, RuleCode),

    CONSTRAINT FK_SecurityRule_Scanner
        FOREIGN KEY (SecurityScannerId)
        REFERENCES dbo.SecurityScanner(SecurityScannerId),

    CONSTRAINT FK_SecurityRule_Severity
        FOREIGN KEY (SecuritySeverityId)
        REFERENCES dbo.SecuritySeverity(SecuritySeverityId)
);
GO
```

## 17.3 SecurityScan

```sql
CREATE TABLE dbo.SecurityScan
(
    SecurityScanId   uniqueidentifier NOT NULL
        CONSTRAINT DF_SecurityScan_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId       uniqueidentifier NOT NULL,
    SecurityScannerId uniqueidentifier NOT NULL,

    StatusCode       varchar(30)      NOT NULL,
    StartedAtUtc     datetime2(3)     NULL,
    CompletedAtUtc   datetime2(3)     NULL,

    FindingCount     int              NOT NULL DEFAULT 0,
    ErrorMessage     nvarchar(max)    NULL,

    CONSTRAINT PK_SecurityScan PRIMARY KEY (SecurityScanId),

    CONSTRAINT FK_SecurityScan_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_SecurityScan_Scanner
        FOREIGN KEY (SecurityScannerId)
        REFERENCES dbo.SecurityScanner(SecurityScannerId)
);
GO

CREATE INDEX IX_SecurityScan_Analysis
    ON dbo.SecurityScan(AnalysisId);
GO
```

## 17.4 SecurityFinding

```sql
CREATE TABLE dbo.SecurityFinding
(
    SecurityFindingId uniqueidentifier NOT NULL
        CONSTRAINT DF_SecurityFinding_Id DEFAULT NEWSEQUENTIALID(),

    SecurityScanId    uniqueidentifier NOT NULL,
    SecurityRuleId    uniqueidentifier NOT NULL,

    GraphNodeId       uniqueidentifier NULL,
    CodeFileId        uniqueidentifier NULL,

    Title             nvarchar(500)    NOT NULL,
    Description       nvarchar(max)    NULL,

    StatusCode        varchar(30)     NOT NULL
        CONSTRAINT DF_SecurityFinding_Status DEFAULT ('OPEN'),

    SourceLineStart   int              NULL,
    SourceLineEnd     int              NULL,

    Remediation       nvarchar(max)    NULL,

    Fingerprint       varchar(128)     NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_SecurityFinding_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    ResolvedAtUtc     datetime2(3)     NULL,

    CONSTRAINT PK_SecurityFinding PRIMARY KEY (SecurityFindingId),

    CONSTRAINT FK_SecurityFinding_Scan
        FOREIGN KEY (SecurityScanId) REFERENCES dbo.SecurityScan(SecurityScanId),

    CONSTRAINT FK_SecurityFinding_Rule
        FOREIGN KEY (SecurityRuleId) REFERENCES dbo.SecurityRule(SecurityRuleId),

    CONSTRAINT FK_SecurityFinding_Node
        FOREIGN KEY (GraphNodeId) REFERENCES dbo.GraphNode(GraphNodeId),

    CONSTRAINT FK_SecurityFinding_File
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId)
);
GO

CREATE INDEX IX_SecurityFinding_Scan
    ON dbo.SecurityFinding(SecurityScanId);

CREATE INDEX IX_SecurityFinding_Node
    ON dbo.SecurityFinding(GraphNodeId);

CREATE INDEX IX_SecurityFinding_Status
    ON dbo.SecurityFinding(StatusCode);
GO
```

## 17.5 SecurityFindingEvidence

```sql
CREATE TABLE dbo.SecurityFindingEvidence
(
    SecurityFindingId uniqueidentifier NOT NULL,
    EvidenceId        uniqueidentifier NOT NULL,

    CONSTRAINT PK_SecurityFindingEvidence
        PRIMARY KEY (SecurityFindingId, EvidenceId),

    CONSTRAINT FK_SecurityFindingEvidence_Finding
        FOREIGN KEY (SecurityFindingId)
        REFERENCES dbo.SecurityFinding(SecurityFindingId),

    CONSTRAINT FK_SecurityFindingEvidence_Evidence
        FOREIGN KEY (EvidenceId)
        REFERENCES dbo.Evidence(EvidenceId)
);
GO
```

---

# 18. Tests

The analysis UI includes failed tests and recommended tests, so test observations should be separate from recommendations.

## 18.1 TestRun

```sql
CREATE TABLE dbo.TestRun
(
    TestRunId       uniqueidentifier NOT NULL
        CONSTRAINT DF_TestRun_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId      uniqueidentifier NOT NULL,

    FrameworkCode   varchar(100)     NULL,
    StatusCode      varchar(30)      NOT NULL,

    StartedAtUtc    datetime2(3)     NULL,
    CompletedAtUtc  datetime2(3)     NULL,

    PassedCount     int              NOT NULL DEFAULT 0,
    FailedCount     int              NOT NULL DEFAULT 0,
    SkippedCount    int              NOT NULL DEFAULT 0,

    ErrorMessage    nvarchar(max)    NULL,

    CONSTRAINT PK_TestRun PRIMARY KEY (TestRunId),

    CONSTRAINT FK_TestRun_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId)
);
GO
```

## 18.2 TestResult

```sql
CREATE TABLE dbo.TestResult
(
    TestResultId    uniqueidentifier NOT NULL
        CONSTRAINT DF_TestResult_Id DEFAULT NEWSEQUENTIALID(),

    TestRunId       uniqueidentifier NOT NULL,
    TestName        nvarchar(500)    NOT NULL,

    StatusCode      varchar(30)      NOT NULL,
    DurationMs      bigint           NULL,
    FailureMessage  nvarchar(max)    NULL,

    CodeFileId      uniqueidentifier NULL,

    CONSTRAINT PK_TestResult PRIMARY KEY (TestResultId),

    CONSTRAINT FK_TestResult_Run
        FOREIGN KEY (TestRunId) REFERENCES dbo.TestRun(TestRunId),

    CONSTRAINT FK_TestResult_File
        FOREIGN KEY (CodeFileId) REFERENCES dbo.CodeFile(CodeFileId)
);
GO
```

---

# 19. Recommendations

## 19.1 Recommendation

```sql
CREATE TABLE dbo.Recommendation
(
    RecommendationId uniqueidentifier NOT NULL
        CONSTRAINT DF_Recommendation_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId       uniqueidentifier NOT NULL,
    SecurityFindingId uniqueidentifier NULL,
    GraphNodeId      uniqueidentifier NULL,

    Title            nvarchar(500)    NOT NULL,
    Description      nvarchar(max)    NOT NULL,

    PriorityCode     varchar(30)      NULL,
    SourceCode       varchar(50)      NOT NULL,

    SortOrder        int              NOT NULL DEFAULT 0,

    CreatedAtUtc     datetime2(3)     NOT NULL
        CONSTRAINT DF_Recommendation_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Recommendation PRIMARY KEY (RecommendationId),

    CONSTRAINT FK_Recommendation_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_Recommendation_Finding
        FOREIGN KEY (SecurityFindingId)
        REFERENCES dbo.SecurityFinding(SecurityFindingId),

    CONSTRAINT FK_Recommendation_Node
        FOREIGN KEY (GraphNodeId) REFERENCES dbo.GraphNode(GraphNodeId)
);
GO
```

`SourceCode` should distinguish:

```text
RULE_ENGINE
SECURITY_RULE
AI_ASSISTED
SYSTEM
```

AI recommendations must still reference verified analysis/finding/node context.

---

# 20. AI / Microsoft Foundry

## 20.1 AIProvider

```sql
CREATE TABLE dbo.AIProvider
(
    AIProviderId uniqueidentifier NOT NULL
        CONSTRAINT DF_AIProvider_Id DEFAULT NEWSEQUENTIALID(),

    Code         varchar(100)     NOT NULL,
    DisplayName  nvarchar(200)    NOT NULL,

    IsActive     bit              NOT NULL DEFAULT 1,

    CONSTRAINT PK_AIProvider PRIMARY KEY (AIProviderId),
    CONSTRAINT UQ_AIProvider_Code UNIQUE (Code)
);
GO
```

Possible providers:

```text
MICROSOFT_FOUNDRY
OPENAI_COMPATIBLE
MOCK
```

## 20.2 AIConfiguration

```sql
CREATE TABLE dbo.AIConfiguration
(
    AIConfigurationId uniqueidentifier NOT NULL
        CONSTRAINT DF_AIConfiguration_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId    uniqueidentifier NOT NULL,
    ProjectId         uniqueidentifier NULL,

    AIProviderId      uniqueidentifier NOT NULL,

    DeploymentName   nvarchar(300)    NULL,
    ModelName        nvarchar(300)    NULL,

    Temperature       decimal(4,3)     NULL,
    MaxTokens         int              NULL,

    IsEnabled         bit              NOT NULL DEFAULT 1,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_AIConfiguration_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_AIConfiguration_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AIConfiguration PRIMARY KEY (AIConfigurationId),

    CONSTRAINT FK_AIConfiguration_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_AIConfiguration_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_AIConfiguration_Provider
        FOREIGN KEY (AIProviderId) REFERENCES dbo.AIProvider(AIProviderId),

    CONSTRAINT CK_AIConfiguration_Temperature
        CHECK (Temperature IS NULL OR Temperature >= 0 AND Temperature <= 2),

    CONSTRAINT CK_AIConfiguration_MaxTokens
        CHECK (MaxTokens IS NULL OR MaxTokens > 0)
);
GO
```

## 20.3 PromptTemplate

```sql
CREATE TABLE dbo.PromptTemplate
(
    PromptTemplateId uniqueidentifier NOT NULL
        CONSTRAINT DF_PromptTemplate_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId   uniqueidentifier NOT NULL,
    ProjectId        uniqueidentifier NULL,

    Name             nvarchar(200)    NOT NULL,
    TemplateTypeCode varchar(50)      NOT NULL,

    SystemPrompt     nvarchar(max)    NULL,
    UserPrompt       nvarchar(max)    NOT NULL,

    VersionNo        int              NOT NULL DEFAULT 1,
    IsActive         bit              NOT NULL DEFAULT 1,

    CreatedByUserId  uniqueidentifier NOT NULL,
    CreatedAtUtc     datetime2(3)     NOT NULL
        CONSTRAINT DF_PromptTemplate_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_PromptTemplate PRIMARY KEY (PromptTemplateId),

    CONSTRAINT FK_PromptTemplate_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_PromptTemplate_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_PromptTemplate_User
        FOREIGN KEY (CreatedByUserId) REFERENCES dbo.UserAccount(UserId)
);
GO
```

## 20.4 AIGeneration

```sql
CREATE TABLE dbo.AIGeneration
(
    AIGenerationId    uniqueidentifier NOT NULL
        CONSTRAINT DF_AIGeneration_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId        uniqueidentifier NOT NULL,
    AIConfigurationId uniqueidentifier NOT NULL,
    PromptTemplateId  uniqueidentifier NULL,

    PurposeCode       varchar(50)      NOT NULL,
    StatusCode        varchar(30)      NOT NULL,

    InputHash         varchar(128)     NULL,

    OutputText       nvarchar(max)    NULL,
    ValidatedOutput  nvarchar(max)    NULL,

    InputTokens      int              NULL,
    OutputTokens     int              NULL,
    LatencyMs        bigint           NULL,

    Degraded         bit              NOT NULL DEFAULT 0,
    ErrorMessage     nvarchar(max)    NULL,

    CreatedAtUtc     datetime2(3)     NOT NULL
        CONSTRAINT DF_AIGeneration_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AIGeneration PRIMARY KEY (AIGenerationId),

    CONSTRAINT FK_AIGeneration_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_AIGeneration_Config
        FOREIGN KEY (AIConfigurationId)
        REFERENCES dbo.AIConfiguration(AIConfigurationId),

    CONSTRAINT FK_AIGeneration_Prompt
        FOREIGN KEY (PromptTemplateId)
        REFERENCES dbo.PromptTemplate(PromptTemplateId)
);
GO
```

## 20.5 AIOutputReference

This prevents an AI response from becoming an independent source of truth.

```sql
CREATE TABLE dbo.AIOutputReference
(
    AIGenerationId    uniqueidentifier NOT NULL,

    EvidenceId        uniqueidentifier NULL,
    AnalysisNodeResultId uniqueidentifier NULL,
    SecurityFindingId uniqueidentifier NULL,
    RecommendationId  uniqueidentifier NULL,

    CONSTRAINT FK_AIOutputReference_Generation
        FOREIGN KEY (AIGenerationId)
        REFERENCES dbo.AIGeneration(AIGenerationId),

    CONSTRAINT FK_AIOutputReference_Evidence
        FOREIGN KEY (EvidenceId) REFERENCES dbo.Evidence(EvidenceId),

    CONSTRAINT FK_AIOutputReference_NodeResult
        FOREIGN KEY (AnalysisNodeResultId)
        REFERENCES dbo.AnalysisNodeResult(AnalysisNodeResultId),

    CONSTRAINT FK_AIOutputReference_Finding
        FOREIGN KEY (SecurityFindingId)
        REFERENCES dbo.SecurityFinding(SecurityFindingId),

    CONSTRAINT FK_AIOutputReference_Recommendation
        FOREIGN KEY (RecommendationId)
        REFERENCES dbo.Recommendation(RecommendationId)
);
GO
```

Application logic must validate all referenced IDs against the same analysis before accepting AI output.

---

# 21. Integrations

## 21.1 SecretReference

```sql
CREATE TABLE dbo.SecretReference
(
    SecretReferenceId uniqueidentifier NOT NULL
        CONSTRAINT DF_SecretReference_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId    uniqueidentifier NOT NULL,

    ProviderCode      varchar(100)     NOT NULL,
    SecretUri         nvarchar(2000)   NOT NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_SecretReference_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SecretReference PRIMARY KEY (SecretReferenceId),

    CONSTRAINT FK_SecretReference_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId)
);
GO
```

## 21.2 IntegrationConnection

```sql
CREATE TABLE dbo.IntegrationConnection
(
    IntegrationConnectionId uniqueidentifier NOT NULL
        CONSTRAINT DF_IntegrationConnection_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId         uniqueidentifier NOT NULL,
    ProjectId              uniqueidentifier NULL,

    IntegrationProviderId  tinyint          NOT NULL,

    DisplayName            nvarchar(200)    NOT NULL,
    ExternalTenantId       nvarchar(300)    NULL,
    ExternalBaseUrl        nvarchar(2000)   NULL,

    SecretReferenceId      uniqueidentifier NULL,

    StatusCode             varchar(30)      NOT NULL,
    LastTestedAtUtc        datetime2(3)     NULL,
    LastError              nvarchar(max)    NULL,

    IsEnabled              bit              NOT NULL DEFAULT 1,

    CreatedAtUtc           datetime2(3)     NOT NULL
        CONSTRAINT DF_IntegrationConnection_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc           datetime2(3)     NOT NULL
        CONSTRAINT DF_IntegrationConnection_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_IntegrationConnection PRIMARY KEY (IntegrationConnectionId),

    CONSTRAINT FK_IntegrationConnection_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_IntegrationConnection_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_IntegrationConnection_Provider
        FOREIGN KEY (IntegrationProviderId)
        REFERENCES dbo.IntegrationProvider(IntegrationProviderId),

    CONSTRAINT FK_IntegrationConnection_Secret
        FOREIGN KEY (SecretReferenceId)
        REFERENCES dbo.SecretReference(SecretReferenceId)
);
GO

CREATE INDEX IX_IntegrationConnection_Organization
    ON dbo.IntegrationConnection(OrganizationId);

CREATE INDEX IX_IntegrationConnection_Project
    ON dbo.IntegrationConnection(ProjectId);
GO
```

---

# 22. SQL Server Metadata

The application needs database/schema dependency information.

## 22.1 DatabaseConnection

```sql
CREATE TABLE dbo.DatabaseConnection
(
    DatabaseConnectionId   uniqueidentifier NOT NULL
        CONSTRAINT DF_DatabaseConnection_Id DEFAULT NEWSEQUENTIALID(),

    IntegrationConnectionId uniqueidentifier NOT NULL,
    DatabaseName            nvarchar(200)    NOT NULL,
    SchemaFilter            nvarchar(200)    NULL,

    IsReadOnly              bit              NOT NULL DEFAULT 1,

    CONSTRAINT PK_DatabaseConnection PRIMARY KEY (DatabaseConnectionId),

    CONSTRAINT FK_DatabaseConnection_Integration
        FOREIGN KEY (IntegrationConnectionId)
        REFERENCES dbo.IntegrationConnection(IntegrationConnectionId)
);
GO
```

## 22.2 DatabaseSchema

```sql
CREATE TABLE dbo.DatabaseSchema
(
    DatabaseSchemaId    uniqueidentifier NOT NULL
        CONSTRAINT DF_DatabaseSchema_Id DEFAULT NEWSEQUENTIALID(),

    DatabaseConnectionId uniqueidentifier NOT NULL,
    SchemaName           nvarchar(128)    NOT NULL,

    CONSTRAINT PK_DatabaseSchema PRIMARY KEY (DatabaseSchemaId),

    CONSTRAINT UQ_DatabaseSchema
        UNIQUE (DatabaseConnectionId, SchemaName),

    CONSTRAINT FK_DatabaseSchema_Connection
        FOREIGN KEY (DatabaseConnectionId)
        REFERENCES dbo.DatabaseConnection(DatabaseConnectionId)
);
GO
```

## 22.3 DatabaseObject

```sql
CREATE TABLE dbo.DatabaseObject
(
    DatabaseObjectId uniqueidentifier NOT NULL
        CONSTRAINT DF_DatabaseObject_Id DEFAULT NEWSEQUENTIALID(),

    DatabaseSchemaId uniqueidentifier NOT NULL,

    ObjectTypeCode   varchar(50)      NOT NULL,
    ObjectName       nvarchar(256)    NOT NULL,

    DefinitionHash   varchar(128)     NULL,

    CONSTRAINT PK_DatabaseObject PRIMARY KEY (DatabaseObjectId),

    CONSTRAINT UQ_DatabaseObject
        UNIQUE (DatabaseSchemaId, ObjectTypeCode, ObjectName),

    CONSTRAINT FK_DatabaseObject_Schema
        FOREIGN KEY (DatabaseSchemaId)
        REFERENCES dbo.DatabaseSchema(DatabaseSchemaId)
);
GO
```

## 22.4 DatabaseColumn

```sql
CREATE TABLE dbo.DatabaseColumn
(
    DatabaseColumnId uniqueidentifier NOT NULL
        CONSTRAINT DF_DatabaseColumn_Id DEFAULT NEWSEQUENTIALID(),

    DatabaseObjectId  uniqueidentifier NOT NULL,

    ColumnName       nvarchar(128)    NOT NULL,
    DataType         nvarchar(128)    NOT NULL,
    IsNullable       bit              NOT NULL,
    OrdinalPosition  int              NULL,

    CONSTRAINT PK_DatabaseColumn PRIMARY KEY (DatabaseColumnId),

    CONSTRAINT UQ_DatabaseColumn
        UNIQUE (DatabaseObjectId, ColumnName),

    CONSTRAINT FK_DatabaseColumn_Object
        FOREIGN KEY (DatabaseObjectId)
        REFERENCES dbo.DatabaseObject(DatabaseObjectId)
);
GO
```

## 22.5 DatabaseForeignKey

```sql
CREATE TABLE dbo.DatabaseForeignKey
(
    DatabaseForeignKeyId uniqueidentifier NOT NULL
        CONSTRAINT DF_DatabaseForeignKey_Id DEFAULT NEWSEQUENTIALID(),

    SourceObjectId        uniqueidentifier NOT NULL,
    TargetObjectId        uniqueidentifier NOT NULL,

    ConstraintName        nvarchar(256)    NOT NULL,

    CONSTRAINT PK_DatabaseForeignKey PRIMARY KEY (DatabaseForeignKeyId),

    CONSTRAINT UQ_DatabaseForeignKey
        UNIQUE (SourceObjectId, TargetObjectId, ConstraintName),

    CONSTRAINT FK_DatabaseForeignKey_Source
        FOREIGN KEY (SourceObjectId)
        REFERENCES dbo.DatabaseObject(DatabaseObjectId),

    CONSTRAINT FK_DatabaseForeignKey_Target
        FOREIGN KEY (TargetObjectId)
        REFERENCES dbo.DatabaseObject(DatabaseObjectId)
);
GO
```

---

# 23. Reporting

## 23.1 Report

```sql
CREATE TABLE dbo.Report
(
    ReportId          uniqueidentifier NOT NULL
        CONSTRAINT DF_Report_Id DEFAULT NEWSEQUENTIALID(),

    AnalysisId        uniqueidentifier NOT NULL,

    Title             nvarchar(500)    NOT NULL,
    FormatCode        varchar(30)      NOT NULL,

    GeneratedByUserId uniqueidentifier NULL,

    StorageUri        nvarchar(2000)   NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_Report_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Report PRIMARY KEY (ReportId),

    CONSTRAINT FK_Report_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_Report_User
        FOREIGN KEY (GeneratedByUserId)
        REFERENCES dbo.UserAccount(UserId)
);
GO
```

## 23.2 ReportSection

```sql
CREATE TABLE dbo.ReportSection
(
    ReportSectionId uniqueidentifier NOT NULL
        CONSTRAINT DF_ReportSection_Id DEFAULT NEWSEQUENTIALID(),

    ReportId        uniqueidentifier NOT NULL,

    SectionTypeCode varchar(50)      NOT NULL,
    Title           nvarchar(300)    NOT NULL,
    Content         nvarchar(max)    NULL,

    SortOrder       int              NOT NULL,

    CONSTRAINT PK_ReportSection PRIMARY KEY (ReportSectionId),

    CONSTRAINT UQ_ReportSection_Order
        UNIQUE (ReportId, SortOrder),

    CONSTRAINT FK_ReportSection_Report
        FOREIGN KEY (ReportId) REFERENCES dbo.Report(ReportId)
);
GO
```

---

# 24. Notification System

## 24.1 NotificationChannel

```sql
CREATE TABLE dbo.NotificationChannel
(
    NotificationChannelId uniqueidentifier NOT NULL
        CONSTRAINT DF_NotificationChannel_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId        uniqueidentifier NOT NULL,
    IntegrationConnectionId uniqueidentifier NULL,

    ChannelTypeCode       varchar(30)      NOT NULL,
    DisplayName           nvarchar(200)    NOT NULL,

    IsEnabled             bit              NOT NULL DEFAULT 1,

    CreatedAtUtc          datetime2(3)     NOT NULL
        CONSTRAINT DF_NotificationChannel_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_NotificationChannel PRIMARY KEY (NotificationChannelId),

    CONSTRAINT FK_NotificationChannel_Organization
        FOREIGN KEY (OrganizationId)
        REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_NotificationChannel_Integration
        FOREIGN KEY (IntegrationConnectionId)
        REFERENCES dbo.IntegrationConnection(IntegrationConnectionId)
);
GO
```

Channel types:

```text
TEAMS
AZURE_DEVOPS
EMAIL
```

## 24.2 NotificationEventType

```sql
CREATE TABLE dbo.NotificationEventType
(
    NotificationEventTypeId tinyint       NOT NULL,
    Code                    varchar(60)   NOT NULL,
    DisplayName             nvarchar(150) NOT NULL,

    CONSTRAINT PK_NotificationEventType PRIMARY KEY (NotificationEventTypeId),
    CONSTRAINT UQ_NotificationEventType_Code UNIQUE (Code)
);
GO
```

Seed:

```text
HIGH_RISK_CHANGE
CRITICAL_SECURITY_FINDING
ANALYSIS_COMPLETED
ANALYSIS_FAILED
NEW_PULL_REQUEST
```

## 24.3 NotificationRule

```sql
CREATE TABLE dbo.NotificationRule
(
    NotificationRuleId      uniqueidentifier NOT NULL
        CONSTRAINT DF_NotificationRule_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId          uniqueidentifier NOT NULL,
    ProjectId               uniqueidentifier NULL,

    NotificationEventTypeId tinyint          NOT NULL,
    NotificationChannelId   uniqueidentifier NOT NULL,

    IsEnabled               bit              NOT NULL DEFAULT 1,

    CreatedAtUtc            datetime2(3)     NOT NULL
        CONSTRAINT DF_NotificationRule_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc            datetime2(3)     NOT NULL
        CONSTRAINT DF_NotificationRule_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_NotificationRule PRIMARY KEY (NotificationRuleId),

    CONSTRAINT UQ_NotificationRule
        UNIQUE
        (
            OrganizationId,
            ProjectId,
            NotificationEventTypeId,
            NotificationChannelId
        ),

    CONSTRAINT FK_NotificationRule_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_NotificationRule_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId),

    CONSTRAINT FK_NotificationRule_Event
        FOREIGN KEY (NotificationEventTypeId)
        REFERENCES dbo.NotificationEventType(NotificationEventTypeId),

    CONSTRAINT FK_NotificationRule_Channel
        FOREIGN KEY (NotificationChannelId)
        REFERENCES dbo.NotificationChannel(NotificationChannelId)
);
GO
```

## 24.4 NotificationDelivery

```sql
CREATE TABLE dbo.NotificationDelivery
(
    NotificationDeliveryId uniqueidentifier NOT NULL
        CONSTRAINT DF_NotificationDelivery_Id DEFAULT NEWSEQUENTIALID(),

    NotificationRuleId     uniqueidentifier NOT NULL,

    AnalysisId             uniqueidentifier NULL,
    SecurityFindingId      uniqueidentifier NULL,
    PullRequestId          uniqueidentifier NULL,

    StatusCode             varchar(30)      NOT NULL,
    AttemptCount           int              NOT NULL DEFAULT 0,

    SentAtUtc              datetime2(3)     NULL,
    LastAttemptAtUtc       datetime2(3)     NULL,
    ErrorMessage           nvarchar(max)    NULL,

    CreatedAtUtc           datetime2(3)     NOT NULL
        CONSTRAINT DF_NotificationDelivery_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_NotificationDelivery PRIMARY KEY (NotificationDeliveryId),

    CONSTRAINT FK_NotificationDelivery_Rule
        FOREIGN KEY (NotificationRuleId)
        REFERENCES dbo.NotificationRule(NotificationRuleId),

    CONSTRAINT FK_NotificationDelivery_Analysis
        FOREIGN KEY (AnalysisId) REFERENCES dbo.Analysis(AnalysisId),

    CONSTRAINT FK_NotificationDelivery_Finding
        FOREIGN KEY (SecurityFindingId)
        REFERENCES dbo.SecurityFinding(SecurityFindingId),

    CONSTRAINT FK_NotificationDelivery_PR
        FOREIGN KEY (PullRequestId) REFERENCES dbo.PullRequest(PullRequestId)
);
GO
```

---

# 25. Application Settings

## 25.1 OrganizationSetting

Store typed settings rather than creating one giant JSON blob.

```sql
CREATE TABLE dbo.OrganizationSetting
(
    OrganizationSettingId uniqueidentifier NOT NULL
        CONSTRAINT DF_OrganizationSetting_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId        uniqueidentifier NOT NULL,

    SettingKey            varchar(150)     NOT NULL,

    StringValue           nvarchar(2000)   NULL,
    IntValue              int              NULL,
    DecimalValue          decimal(18,6)    NULL,
    BoolValue             bit              NULL,

    UpdatedByUserId       uniqueidentifier NOT NULL,
    UpdatedAtUtc          datetime2(3)     NOT NULL
        CONSTRAINT DF_OrganizationSetting_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OrganizationSetting PRIMARY KEY (OrganizationSettingId),
    CONSTRAINT UQ_OrganizationSetting_Key UNIQUE (OrganizationId, SettingKey),

    CONSTRAINT FK_OrganizationSetting_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_OrganizationSetting_User
        FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.UserAccount(UserId)
);
GO
```

Expected keys can include:

```text
DEFAULT_TARGET_PROJECT
INCLUDE_SECURITY_SCAN
INCLUDE_AI_EXPLANATION
AUTO_ANALYZE_PULL_REQUESTS
MAX_ANALYSIS_DEPTH
CONFIDENCE_THRESHOLD
```

---

# 26. API / Workflow Operations

The database should support the application's operations without coupling business workflows into the schema.

Recommended service boundaries:

```text
ProjectService
RepositoryService
IngestionService
GraphService
ChangeService
AnalysisService
SecurityService
EvidenceService
AIService
ReportService
IntegrationService
NotificationService
SettingsService
AuditService
```

Each service owns its business logic and repositories.

The database provides:

- persistence;
- integrity;
- indexes;
- transactional boundaries;
- optimistic concurrency.

It should not become the place where complex business rules are hidden in hundreds of triggers.

---

# 27. Background Jobs

Long-running repository indexing, security scans, analysis, AI generation, and notifications should be asynchronous.

## 27.1 Job

```sql
CREATE TABLE dbo.Job
(
    JobId             uniqueidentifier NOT NULL
        CONSTRAINT DF_Job_Id DEFAULT NEWSEQUENTIALID(),

    OrganizationId    uniqueidentifier NOT NULL,
    ProjectId         uniqueidentifier NULL,

    JobTypeCode       varchar(60)      NOT NULL,
    StatusCode        varchar(30)      NOT NULL,

    CorrelationId     uniqueidentifier NULL,

    Attempts          int              NOT NULL DEFAULT 0,
    MaxAttempts       int              NOT NULL DEFAULT 3,

    QueuedAtUtc       datetime2(3)     NOT NULL
        CONSTRAINT DF_Job_QueuedAtUtc DEFAULT SYSUTCDATETIME(),
    StartedAtUtc      datetime2(3)     NULL,
    CompletedAtUtc    datetime2(3)     NULL,

    ErrorMessage      nvarchar(max)    NULL,

    CONSTRAINT PK_Job PRIMARY KEY (JobId),

    CONSTRAINT FK_Job_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_Job_Project
        FOREIGN KEY (ProjectId) REFERENCES dbo.Project(ProjectId)
);
GO

CREATE INDEX IX_Job_Status_Queue
    ON dbo.Job(StatusCode, QueuedAtUtc);
GO
```

Application-level workers should use row-level claiming/locking patterns rather than multiple workers processing the same job.

---

# 28. Audit

Enterprise settings, integrations, analysis actions, and security operations should be auditable.

## 28.1 AuditEvent

```sql
CREATE TABLE dbo.AuditEvent
(
    AuditEventId      bigint IDENTITY(1,1) NOT NULL,

    OrganizationId    uniqueidentifier NOT NULL,
    UserId            uniqueidentifier NULL,

    EntityTypeCode    varchar(100)     NOT NULL,
    EntityId          uniqueidentifier NULL,

    ActionCode        varchar(100)     NOT NULL,

    CorrelationId     uniqueidentifier NULL,

    MetadataJson      nvarchar(max)    NULL,

    CreatedAtUtc      datetime2(3)     NOT NULL
        CONSTRAINT DF_AuditEvent_CreatedAtUtc DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AuditEvent PRIMARY KEY (AuditEventId),

    CONSTRAINT FK_AuditEvent_Organization
        FOREIGN KEY (OrganizationId) REFERENCES dbo.Organization(OrganizationId),

    CONSTRAINT FK_AuditEvent_User
        FOREIGN KEY (UserId) REFERENCES dbo.UserAccount(UserId)
);
GO

CREATE INDEX IX_AuditEvent_Organization_Date
    ON dbo.AuditEvent(OrganizationId, CreatedAtUtc DESC);

CREATE INDEX IX_AuditEvent_Entity
    ON dbo.AuditEvent(EntityTypeCode, EntityId);
GO
```

---

# 29. Dashboard Queries

Do not permanently store dashboard totals such as:

```text
Critical Changes = 4
Risky Changes = 7
Safe Changes = 31
```

as independent facts.

They should be derived from `Analysis`, `AnalysisNodeResult`, and `SecurityFinding`.

Example:

```sql
SELECT
    rs.Code,
    COUNT(*) AS AnalysisCount
FROM dbo.Analysis a
JOIN dbo.RiskState rs
    ON rs.RiskStateId = a.OverallRiskStateId
WHERE a.ProjectId = @ProjectId
GROUP BY rs.Code;
```

For very large installations, introduce a separate reporting/materialized projection rather than corrupting the transactional model.

---

# 30. Critical Index Strategy

The highest-value indexes are:

```text
Project
  (OrganizationId, Name)

Repository
  (ProjectId, Name)

CodeFile
  (RepositoryId, RelativePath)

GraphNode
  (GraphSnapshotId, ExternalKey)

GraphEdge
  (SourceNodeId)
  (TargetNodeId)
  (GraphSnapshotId, GraphEdgeTypeId)

Analysis
  (ProjectId, CreatedAtUtc DESC)
  (ProjectId, OverallRiskStateId)

AnalysisNodeResult
  (AnalysisId, RiskStateId)
  (AnalysisId, Distance)

Evidence
  (AnalysisId)
  (GraphNodeId)

SecurityFinding
  (SecurityScanId)
  (GraphNodeId)
  (StatusCode)

Change
  (ProjectId, CreatedAtUtc DESC)

NotificationDelivery
  (NotificationRuleId, StatusCode)

Job
  (StatusCode, QueuedAtUtc)

AuditEvent
  (OrganizationId, CreatedAtUtc DESC)
```

Add indexes based on real query plans rather than indexing every column.

---

# 31. Concurrency

Use `rowversion` on mutable aggregate roots:

```text
Organization
UserAccount
Project
Repository
Analysis
```

The API should return HTTP `409 Conflict` when an update uses a stale `rowversion`.

Example:

```text
GET resource
    -> rowversion = X

UPDATE resource WHERE RowVersion = X

If zero rows affected:
    -> someone changed it
    -> return conflict
```

---

# 32. Transaction Boundaries

Use transactions around logically atomic operations.

## Example: Complete analysis result

A transaction may commit:

```text
Analysis status
      +
AnalysisNodeResult
      +
ImpactPath
      +
ImpactPathEdge
      +
Evidence
      +
Recommendations
      +
Report metadata
```

AI generation should generally be a separate asynchronous transaction.

## Example: Integration test

```text
Create/update connection test status
+
Audit event
```

## Example: Security finding

```text
SecurityFinding
+
SecurityFindingEvidence
```

must be committed together.

---

# 33. What Should NOT Be Stored as Raw JSON

Avoid storing core relationships as JSON such as:

```json
{
  "dependencies": [
    "...",
    "..."
  ]
}
```

Instead use:

```text
GraphNode
GraphEdge
ImpactPath
ImpactPathEdge
```

Avoid:

```json
{
  "critical": 4,
  "risky": 7,
  "safe": 31
}
```

for authoritative values.

Use normalized tables and aggregate them.

JSON is appropriate for:

- provider-specific metadata;
- external API payload snapshots when required;
- audit metadata;
- flexible AI output that is separately validated;
- integration-specific settings that cannot reasonably be normalized.

---

# 34. Data Retention

Recommended configurable retention categories:

```text
Graph snapshots       -> long-term / configurable
Analyses              -> long-term
Security findings     -> long-term
Reports               -> long-term
Audit events          -> organization policy
Telemetry             -> short/medium term
Job records            -> short term
Notification deliveries -> short term
AI raw output          -> organization policy
```

Retention must be implemented by background cleanup jobs and organization policy, not by ad-hoc application deletes.

---

# 35. Deletion Strategy

Prefer soft deletion for user-visible business entities:

```text
Organization
Project
Repository
Branch
IntegrationConnection
```

Use hard deletion or retention cleanup for high-volume operational data:

```text
AnalysisTelemetry
Job
NotificationDelivery
```

When deleting an organization, execute an explicit deletion workflow in the application/service layer and respect audit/retention requirements.

Do not rely on unrestricted `ON DELETE CASCADE` across the entire schema.

---

# 36. Security Boundaries

## Tenant boundary

Every query involving organization-owned data should have:

```sql
WHERE OrganizationId = @OrganizationId
```

or derive the organization through a project/parent relationship that has already been authorization-checked.

## Integration credentials

Never:

```text
SecretValue nvarchar(max)
```

in `IntegrationConnection`.

Use:

```text
SecretReference -> external secret manager
```

## Source code

If source code is persisted:

- mask secrets during ingestion;
- encrypt object storage;
- use least-privilege access;
- avoid logging source contents;
- never send unmasked secrets to Foundry.

---

# 37. Stored Procedures vs Application Services

Use stored procedures selectively for:

- reporting queries;
- high-volume batch operations;
- carefully controlled job claiming;
- maintenance/retention.

Keep business decisions such as:

```text
"Distance <= 1 means Critical"
"Runtime edge means Unknown"
"AI cannot change risk"
```

in the deterministic analysis service/domain layer, with configuration stored in SQL where appropriate.

This keeps business policy testable and avoids putting the application domain inside SQL triggers.

---

# 38. Recommended Seed Data

At deployment time seed at least:

### Risk states

```text
CRITICAL
RISKY
SAFE
UNKNOWN
```

### Component types

```text
FRONTEND
FRONTEND_MODULE
FRONTEND_COMPONENT
API
CONTROLLER
SERVICE
INTERFACE
REPOSITORY
DATABASE
DATABASE_TABLE
DATABASE_COLUMN
STORED_PROCEDURE
EXTERNAL
UNKNOWN
```

### Graph edges

```text
CALLS
DEPENDS_ON
IMPORTS
EXPOSES
READS
WRITES
BINDS
INHERITS
REFERENCES
USES
```

### Evidence

```text
DIRECT_CHANGE
STRUCTURAL_DEPENDENCY
DATABASE_ACCESS
SECURITY_ALERT
PARSER_EVIDENCE
TEST_EVIDENCE
INTEGRATION_EVIDENCE
GRAPH_PATH
```

### Change types

```text
PULL_REQUEST
WORK_ITEM
COMMIT
MANUAL
```

### Analysis status

```text
QUEUED
RUNNING
PARTIAL
COMPLETED
FAILED
CANCELLED
```

### Integration providers

```text
AZURE_DEVOPS
SQL_SERVER
MICROSOFT_FOUNDRY
AZURE_AI_SEARCH
MICROSOFT_TEAMS
POWER_AUTOMATE
```

---

# 39. Core Relationship Summary

```text
Organization
 |
 +--< OrganizationMembership >-- UserAccount
 |
 +--< Project
       |
       +--< ProjectMember >-- UserAccount
       |
       +--< Repository
       |      |
       |      +--< Branch
       |      +--< CodeFile
       |             |
       |             +--< CodeFileVersion
       |                    |
       |                    +--< CodeSymbol
       |
       +--< Environment
       |
       +--< GraphSnapshot
       |      |
       |      +--< GraphNode
       |      +--< GraphEdge
       |
       +--< Change
       |      |
       |      +-- PullRequest
       |      +-- WorkItem
       |      +-- Commit
       |      +--< ChangeFile
       |
       +--< Analysis
              |
              +-- AnalysisScope
              +--< AnalysisTarget
              +--< AnalysisProgress
              +--< AnalysisTelemetry
              +--< AnalysisNodeResult
              +--< ImpactPath
              |      +--< ImpactPathEdge
              |
              +--< Evidence
              +--< SecurityScan
              |      +--< SecurityFinding
              |
              +--< TestRun
              |      +--< TestResult
              |
              +--< Recommendation
              +--< AIGeneration
              +--< Report
                     +--< ReportSection
```

---

# 40. Feature Coverage Matrix

| Application feature | Primary tables |
|---|---|
| Login / enterprise identity | `UserAccount`, `OrganizationMembership` |
| Organizations | `Organization` |
| Projects | `Project`, `ProjectMember` |
| Repository connection | `Repository`, `IntegrationConnection` |
| Branch selection | `Branch` |
| Environment selection | `Environment` |
| Repository indexing | `IngestionRun`, `CodeFile`, `CodeFileVersion` |
| Code parsing | `CodeSymbol` |
| Dependency graph | `GraphSnapshot`, `GraphNode`, `GraphEdge` |
| Graph evidence | `GraphEdge`, `Evidence` |
| Work Items | `Change`, `WorkItem` |
| Pull Requests | `Change`, `PullRequest`, `ChangeFile` |
| Commits | `Commit` |
| Manual changes | `Change` |
| Analysis configuration | `Analysis`, `AnalysisScope` |
| Analysis progress | `AnalysisProgress`, `AnalysisTelemetry` |
| Blast radius | `AnalysisNodeResult`, `ImpactPath`, `ImpactPathEdge` |
| Risk classification | `RiskState`, `AnalysisNodeResult` |
| Traceable evidence | `Evidence` |
| Security scans | `SecurityScanner`, `SecurityRule`, `SecurityScan` |
| Security findings | `SecurityFinding`, `SecurityFindingEvidence` |
| Failed tests | `TestRun`, `TestResult` |
| Recommendations | `Recommendation` |
| Foundry configuration | `AIProvider`, `AIConfiguration` |
| Prompt templates | `PromptTemplate` |
| AI explanations | `AIGeneration`, `AIOutputReference` |
| Azure DevOps | `IntegrationProvider`, `IntegrationConnection` |
| SQL Server | `DatabaseConnection`, `DatabaseSchema`, `DatabaseObject`, `DatabaseColumn`, `DatabaseForeignKey` |
| Azure AI Search | `IntegrationConnection` |
| Teams | `IntegrationConnection`, `NotificationChannel` |
| Power Automate | `IntegrationConnection` |
| Reports | `Report`, `ReportSection` |
| Notification channels | `NotificationChannel` |
| Notification rules | `NotificationRule` |
| Notification delivery | `NotificationDelivery` |
| Organization settings | `OrganizationSetting` |
| Long-running jobs | `Job` |
| Audit | `AuditEvent` |

---

# 41. Recommended API-to-Database Mapping

```text
POST /projects
    -> Project

GET /projects
    -> Project + Repository summary

POST /projects/{id}/reindex
    -> IngestionRun + Job

GET /projects/{id}/graph
    -> current GraphSnapshot + GraphNode + GraphEdge

POST /analyses
    -> Analysis + AnalysisScope + AnalysisTarget + Job

GET /analyses/{id}/progress
    -> Analysis + AnalysisProgress + AnalysisTelemetry

GET /analyses/{id}
    -> Analysis + AnalysisNodeResult + SecurityFinding summary

GET /analyses/{id}/graph
    -> ImpactPath + ImpactPathEdge + GraphNode + GraphEdge

GET /analyses/{id}/evidence
    -> Evidence + source metadata

GET /security/findings
    -> SecurityFinding + SecurityRule + Severity + GraphNode

GET /changes
    -> Change + PullRequest/WorkItem metadata

GET /reports
    -> Report + Analysis summary

GET /integrations
    -> IntegrationConnection + IntegrationProvider

POST /integrations/{id}/test
    -> IntegrationConnection + AuditEvent

GET /settings
    -> OrganizationSetting

PUT /settings
    -> OrganizationSetting + AuditEvent

GET /notifications/settings
    -> NotificationChannel + NotificationRule

PUT /notifications/settings
    -> NotificationRule + AuditEvent
```

---

# 42. Important Implementation Notes

### Do not make `GraphNode` the source of all code information.

Code metadata belongs in:

```text
CodeFile
CodeFileVersion
CodeSymbol
```

Graph nodes reference those entities.

### Do not overwrite graph snapshots.

An analysis must be reproducible against the graph snapshot that existed when the analysis ran.

### Do not overwrite risk results.

Each `Analysis` has its own `AnalysisNodeResult` records.

### Do not store AI conclusions as authoritative risk.

AI output should reference deterministic evidence.

### Do not store dashboard counters as truth.

Calculate them from analysis/finding data or maintain a clearly labelled reporting projection.

### Do not store credentials directly.

Use external secret references.

### Do not use unrestricted cascade deletion.

Enterprise analysis history and evidence require deliberate retention/deletion behavior.

---

# 43. Final Architecture Principle

The database should preserve this chain:

```text
Repository
    |
    v
Ingestion
    |
    v
Code / DB Metadata
    |
    v
Graph Snapshot
    |
    v
Change
    |
    v
Analysis
    |
    +----> Deterministic Node Results
    |
    +----> Impact Paths
    |
    +----> Evidence
    |
    +----> Security Findings
    |
    +----> Test Results
    |
    +----> Recommendations
    |
    +----> AI Explanation
    |
    v
Report
```

The most important invariant is:

> **A report can be regenerated from an analysis, an analysis can be traced to a graph snapshot and change, and every non-safe conclusion can be traced to deterministic evidence.**

That is the database-level foundation for making Project X-Ray explainable, auditable, multi-tenant, and suitable for enterprise integration.
