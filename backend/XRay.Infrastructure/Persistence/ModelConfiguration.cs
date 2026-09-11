using Microsoft.EntityFrameworkCore;
using XRay.Domain.Ai;
using XRay.Domain.Analysis;
using XRay.Domain.Changes;
using XRay.Domain.DatabaseMetadata;
using XRay.Domain.EvidenceModel;
using XRay.Domain.Graph;
using XRay.Domain.Identity;
using XRay.Domain.Ingestion;
using XRay.Domain.Integrations;
using XRay.Domain.Notifications;
using XRay.Domain.Projects;
using XRay.Domain.Reference;
using XRay.Domain.Reporting;
using XRay.Domain.Security;
using XRay.Domain.Settings;

namespace XRay.Infrastructure.Persistence;

/// <summary>
/// All fluent-API model configuration in one place, organized by schema.md's domain boundaries.
/// Explicit config is added only where EF Core conventions cannot infer schema.md's intent
/// (composite/renamed keys, business-identity unique constraints, and lookup-table FKs).
/// String columns intentionally rely on EF's nvarchar(max) default rather than mirroring every
/// varchar(N) in schema.md, to keep this manageable; tighten later if storage size matters.
/// </summary>
public static class ModelConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        ConfigureReference(b);
        ConfigureIdentity(b);
        ConfigureProjects(b);
        ConfigureIngestion(b);
        ConfigureGraph(b);
        ConfigureChanges(b);
        ConfigureAnalysis(b);
        ConfigureEvidence(b);
        ConfigureSecurity(b);
        ConfigureTests(b);
        ConfigureAi(b);
        ConfigureIntegrations(b);
        ConfigureDatabaseMetadata(b);
        ConfigureReporting(b);
        ConfigureNotifications(b);
        ConfigureSettings(b);

        // Global conventions applied after explicit configuration above.
        foreach (var property in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(6);
        }

        // Avoid SQL Server "multiple cascade paths" migration failures across this densely
        // cross-referenced schema; deletes are handled explicitly in application services instead.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    private static void ConfigureReference(ModelBuilder b)
    {
        b.Entity<RiskState>(e => e.HasKey(x => x.Id));
        b.Entity<ComponentType>(e => e.HasKey(x => x.Id));
        b.Entity<GraphEdgeType>(e => e.HasKey(x => x.Id));
        b.Entity<EvidenceType>(e => e.HasKey(x => x.Id));
        b.Entity<ChangeType>(e => e.HasKey(x => x.Id));
        b.Entity<AnalysisStatus>(e => e.HasKey(x => x.Id));
        b.Entity<IntegrationProvider>(e => e.HasKey(x => x.Id));
        b.Entity<SecuritySeverity>(e => e.HasKey(x => x.Id));
        b.Entity<AnalysisStage>(e => e.HasKey(x => x.Id));
        b.Entity<NotificationEventType>(e => e.HasKey(x => x.Id));

        foreach (var type in new[]
                 {
                     typeof(RiskState), typeof(ComponentType), typeof(GraphEdgeType), typeof(EvidenceType),
                     typeof(ChangeType), typeof(AnalysisStatus), typeof(IntegrationProvider),
                     typeof(SecuritySeverity), typeof(AnalysisStage), typeof(NotificationEventType)
                 })
        {
            b.Entity(type).HasIndex(nameof(ReferenceEntity.Code)).IsUnique();
        }
    }

    private static void ConfigureIdentity(ModelBuilder b)
    {
        b.Entity<Organization>(e =>
        {
            e.HasKey(x => x.OrganizationId);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<UserAccount>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.ExternalIdentityId).IsUnique();
            e.HasIndex(x => x.Email);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<OrganizationMembership>(e =>
        {
            e.HasKey(x => x.OrganizationMembershipId);
            e.HasIndex(x => new { x.OrganizationId, x.UserId }).IsUnique();
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });
    }

    private static void ConfigureProjects(ModelBuilder b)
    {
        b.Entity<Project>(e =>
        {
            e.HasKey(x => x.ProjectId);
            e.HasIndex(x => new { x.OrganizationId, x.Name });
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<ProjectMember>(e =>
        {
            e.HasKey(x => x.ProjectMemberId);
            e.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<Repository>(e =>
        {
            e.HasKey(x => x.RepositoryId);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne<IntegrationProvider>().WithMany().HasForeignKey(x => x.IntegrationProviderId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Branch>(e =>
        {
            e.HasKey(x => x.BranchId);
            e.HasIndex(x => new { x.RepositoryId, x.Name }).IsUnique();
            e.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId);
        });

        b.Entity<ProjectEnvironment>(e =>
        {
            e.HasKey(x => x.EnvironmentId);
            e.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });
    }

    private static void ConfigureIngestion(ModelBuilder b)
    {
        b.Entity<IngestionRun>(e =>
        {
            e.HasKey(x => x.IngestionRunId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
        });

        b.Entity<CodeFile>(e =>
        {
            e.HasKey(x => x.CodeFileId);
            e.HasIndex(x => new { x.RepositoryId, x.RelativePath }).IsUnique();
            e.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId);
        });

        b.Entity<CodeFileVersion>(e =>
        {
            e.HasKey(x => x.CodeFileVersionId);
            e.HasIndex(x => new { x.CodeFileId, x.IngestionRunId });
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
            e.HasOne(x => x.IngestionRun).WithMany().HasForeignKey(x => x.IngestionRunId);
        });

        b.Entity<CodeSymbol>(e =>
        {
            e.HasKey(x => x.CodeSymbolId);
            e.HasIndex(x => x.CodeFileVersionId);
            e.HasIndex(x => x.SymbolName);
            e.HasOne(x => x.CodeFileVersion).WithMany().HasForeignKey(x => x.CodeFileVersionId);
            e.HasOne<ComponentType>().WithMany().HasForeignKey(x => x.ComponentTypeId);
        });
    }

    private static void ConfigureGraph(ModelBuilder b)
    {
        b.Entity<GraphSnapshot>(e =>
        {
            e.HasKey(x => x.GraphSnapshotId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.IngestionRun).WithMany().HasForeignKey(x => x.IngestionRunId);
        });

        b.Entity<GraphNode>(e =>
        {
            e.HasKey(x => x.GraphNodeId);
            e.HasIndex(x => new { x.GraphSnapshotId, x.ExternalKey }).IsUnique();
            e.HasOne(x => x.GraphSnapshot).WithMany().HasForeignKey(x => x.GraphSnapshotId);
            e.HasOne<ComponentType>().WithMany().HasForeignKey(x => x.ComponentTypeId);
            e.HasOne(x => x.CodeSymbol).WithMany().HasForeignKey(x => x.CodeSymbolId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
        });

        b.Entity<GraphEdge>(e =>
        {
            e.HasKey(x => x.GraphEdgeId);
            e.HasIndex(x => new { x.GraphSnapshotId, x.SourceNodeId, x.TargetNodeId, x.GraphEdgeTypeId }).IsUnique();
            e.HasIndex(x => x.SourceNodeId);
            e.HasIndex(x => x.TargetNodeId);
            e.HasIndex(x => new { x.GraphSnapshotId, x.GraphEdgeTypeId });
            e.HasOne(x => x.GraphSnapshot).WithMany().HasForeignKey(x => x.GraphSnapshotId);
            e.HasOne<GraphEdgeType>().WithMany().HasForeignKey(x => x.GraphEdgeTypeId);
            e.HasOne(x => x.SourceNode).WithMany().HasForeignKey(x => x.SourceNodeId);
            e.HasOne(x => x.TargetNode).WithMany().HasForeignKey(x => x.TargetNodeId);
            e.HasOne(x => x.SourceCodeFile).WithMany().HasForeignKey(x => x.SourceCodeFileId);
        });
    }

    private static void ConfigureChanges(ModelBuilder b)
    {
        b.Entity<Change>(e =>
        {
            e.HasKey(x => x.ChangeId);
            e.HasIndex(x => new { x.ProjectId, x.CreatedAtUtc });
            e.HasIndex(x => new { x.ProjectId, x.ChangeTypeId });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne<ChangeType>().WithMany().HasForeignKey(x => x.ChangeTypeId);
            e.HasOne(x => x.AuthorUser).WithMany().HasForeignKey(x => x.AuthorUserId);
        });

        b.Entity<PullRequest>(e =>
        {
            e.HasKey(x => x.PullRequestId);
            e.HasIndex(x => new { x.RepositoryId, x.ProviderPullRequestId }).IsUnique();
            e.HasOne(x => x.Change).WithMany().HasForeignKey(x => x.ChangeId);
            e.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId);
        });

        b.Entity<WorkItem>(e =>
        {
            e.HasKey(x => x.WorkItemId);
            e.HasIndex(x => new { x.ChangeId, x.ExternalWorkItemId }).IsUnique();
            e.HasOne(x => x.Change).WithMany().HasForeignKey(x => x.ChangeId);
        });

        b.Entity<Commit>(e =>
        {
            e.HasKey(x => x.CommitId);
            e.HasIndex(x => new { x.RepositoryId, x.CommitHash }).IsUnique();
            e.HasOne(x => x.Repository).WithMany().HasForeignKey(x => x.RepositoryId);
            e.HasOne(x => x.Change).WithMany().HasForeignKey(x => x.ChangeId);
        });

        b.Entity<ChangeFile>(e =>
        {
            e.HasKey(x => x.ChangeFileId);
            e.HasIndex(x => new { x.ChangeId, x.RelativePath }).IsUnique();
            e.HasOne(x => x.Change).WithMany().HasForeignKey(x => x.ChangeId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
        });
    }

    private static void ConfigureAnalysis(ModelBuilder b)
    {
        b.Entity<Analysis>(e =>
        {
            e.HasKey(x => x.AnalysisId);
            e.HasIndex(x => new { x.ProjectId, x.CreatedAtUtc });
            e.HasIndex(x => new { x.ProjectId, x.OverallRiskStateId });
            e.HasIndex(x => x.AnalysisStatusId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Change).WithMany().HasForeignKey(x => x.ChangeId);
            e.HasOne(x => x.GraphSnapshot).WithMany().HasForeignKey(x => x.GraphSnapshotId);
            e.HasOne(x => x.Environment).WithMany().HasForeignKey(x => x.EnvironmentId);
            e.HasOne<AnalysisStatus>().WithMany().HasForeignKey(x => x.AnalysisStatusId);
            e.HasOne<RiskState>().WithMany().HasForeignKey(x => x.OverallRiskStateId);
            e.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<AnalysisTarget>(e =>
        {
            e.HasKey(x => x.AnalysisTargetId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
            e.HasOne(x => x.GraphNode).WithMany().HasForeignKey(x => x.GraphNodeId);
        });

        b.Entity<AnalysisScope>(e =>
        {
            e.HasKey(x => x.AnalysisScopeId);
            e.HasIndex(x => x.AnalysisId).IsUnique();
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
        });

        b.Entity<AnalysisProgress>(e =>
        {
            e.HasKey(x => x.AnalysisProgressId);
            e.HasIndex(x => new { x.AnalysisId, x.AnalysisStageId }).IsUnique();
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne<AnalysisStage>().WithMany().HasForeignKey(x => x.AnalysisStageId);
        });

        b.Entity<AnalysisTelemetry>(e =>
        {
            e.HasKey(x => x.AnalysisTelemetryId);
            e.HasIndex(x => new { x.AnalysisId, x.CreatedAtUtc });
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne<AnalysisStage>().WithMany().HasForeignKey(x => x.AnalysisStageId);
        });

        b.Entity<AnalysisNodeResult>(e =>
        {
            e.HasKey(x => x.AnalysisNodeResultId);
            e.HasIndex(x => new { x.AnalysisId, x.GraphNodeId }).IsUnique();
            e.HasIndex(x => new { x.AnalysisId, x.RiskStateId });
            e.HasIndex(x => new { x.AnalysisId, x.Distance });
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.GraphNode).WithMany().HasForeignKey(x => x.GraphNodeId);
            e.HasOne<RiskState>().WithMany().HasForeignKey(x => x.RiskStateId);
        });

        b.Entity<ImpactPath>(e =>
        {
            e.HasKey(x => x.ImpactPathId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.TargetNode).WithMany().HasForeignKey(x => x.TargetNodeId);
            e.HasOne(x => x.ResultNode).WithMany().HasForeignKey(x => x.ResultNodeId);
        });

        b.Entity<ImpactPathEdge>(e =>
        {
            e.HasKey(x => new { x.ImpactPathId, x.SequenceNo });
            e.HasIndex(x => new { x.ImpactPathId, x.GraphEdgeId }).IsUnique();
            e.HasOne(x => x.ImpactPath).WithMany(p => p.Edges).HasForeignKey(x => x.ImpactPathId);
            e.HasOne(x => x.GraphEdge).WithMany().HasForeignKey(x => x.GraphEdgeId);
        });
    }

    private static void ConfigureEvidence(ModelBuilder b)
    {
        b.Entity<Evidence>(e =>
        {
            e.HasKey(x => x.EvidenceId);
            e.HasIndex(x => x.AnalysisId);
            e.HasIndex(x => x.GraphNodeId);
            e.HasIndex(x => new { x.AnalysisId, x.EvidenceTypeId });
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne<EvidenceType>().WithMany().HasForeignKey(x => x.EvidenceTypeId);
            e.HasOne(x => x.GraphNode).WithMany().HasForeignKey(x => x.GraphNodeId);
            e.HasOne(x => x.GraphEdge).WithMany().HasForeignKey(x => x.GraphEdgeId);
            e.HasOne(x => x.ImpactPath).WithMany().HasForeignKey(x => x.ImpactPathId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
            e.HasOne(x => x.CodeFileVersion).WithMany().HasForeignKey(x => x.CodeFileVersionId);
        });
    }

    private static void ConfigureSecurity(ModelBuilder b)
    {
        b.Entity<SecurityScanner>(e =>
        {
            e.HasKey(x => x.SecurityScannerId);
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
        });

        b.Entity<SecurityRule>(e =>
        {
            e.HasKey(x => x.SecurityRuleId);
            e.HasIndex(x => new { x.SecurityScannerId, x.RuleCode }).IsUnique();
            e.HasOne(x => x.SecurityScanner).WithMany().HasForeignKey(x => x.SecurityScannerId);
            e.HasOne<SecuritySeverity>().WithMany().HasForeignKey(x => x.SecuritySeverityId);
        });

        b.Entity<SecurityScan>(e =>
        {
            e.HasKey(x => x.SecurityScanId);
            e.HasIndex(x => x.AnalysisId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.SecurityScanner).WithMany().HasForeignKey(x => x.SecurityScannerId);
        });

        b.Entity<SecurityFinding>(e =>
        {
            e.HasKey(x => x.SecurityFindingId);
            e.HasIndex(x => x.SecurityScanId);
            e.HasIndex(x => x.GraphNodeId);
            e.HasIndex(x => x.StatusCode);
            e.HasOne(x => x.SecurityScan).WithMany().HasForeignKey(x => x.SecurityScanId);
            e.HasOne(x => x.SecurityRule).WithMany().HasForeignKey(x => x.SecurityRuleId);
            e.HasOne(x => x.GraphNode).WithMany().HasForeignKey(x => x.GraphNodeId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
        });

        b.Entity<SecurityFindingEvidence>(e =>
        {
            e.HasKey(x => new { x.SecurityFindingId, x.EvidenceId });
            e.HasOne(x => x.SecurityFinding).WithMany().HasForeignKey(x => x.SecurityFindingId);
            e.HasOne(x => x.Evidence).WithMany().HasForeignKey(x => x.EvidenceId);
        });
    }

    private static void ConfigureTests(ModelBuilder b)
    {
        b.Entity<XRay.Domain.Tests.TestRun>(e =>
        {
            e.HasKey(x => x.TestRunId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
        });

        b.Entity<XRay.Domain.Tests.TestResult>(e =>
        {
            e.HasKey(x => x.TestResultId);
            e.HasOne(x => x.TestRun).WithMany().HasForeignKey(x => x.TestRunId);
            e.HasOne(x => x.CodeFile).WithMany().HasForeignKey(x => x.CodeFileId);
        });

        b.Entity<XRay.Domain.Recommendations.Recommendation>(e =>
        {
            e.HasKey(x => x.RecommendationId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.SecurityFinding).WithMany().HasForeignKey(x => x.SecurityFindingId);
            e.HasOne(x => x.GraphNode).WithMany().HasForeignKey(x => x.GraphNodeId);
        });
    }

    private static void ConfigureAi(ModelBuilder b)
    {
        b.Entity<AIProvider>(e =>
        {
            e.HasKey(x => x.AIProviderId);
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<AIConfiguration>(e =>
        {
            e.HasKey(x => x.AIConfigurationId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.AIProvider).WithMany().HasForeignKey(x => x.AIProviderId);
        });

        b.Entity<PromptTemplate>(e =>
        {
            e.HasKey(x => x.PromptTemplateId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId);
        });

        b.Entity<AIGeneration>(e =>
        {
            e.HasKey(x => x.AIGenerationId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.AIConfiguration).WithMany().HasForeignKey(x => x.AIConfigurationId);
            e.HasOne(x => x.PromptTemplate).WithMany().HasForeignKey(x => x.PromptTemplateId);
        });

        b.Entity<AIOutputReference>(e =>
        {
            e.HasKey(x => x.AIOutputReferenceId);
            e.HasOne(x => x.AIGeneration).WithMany().HasForeignKey(x => x.AIGenerationId);
            e.HasOne(x => x.Evidence).WithMany().HasForeignKey(x => x.EvidenceId);
            e.HasOne(x => x.AnalysisNodeResult).WithMany().HasForeignKey(x => x.AnalysisNodeResultId);
            e.HasOne(x => x.SecurityFinding).WithMany().HasForeignKey(x => x.SecurityFindingId);
            e.HasOne(x => x.Recommendation).WithMany().HasForeignKey(x => x.RecommendationId);
        });
    }

    private static void ConfigureIntegrations(ModelBuilder b)
    {
        b.Entity<SecretReference>(e =>
        {
            e.HasKey(x => x.SecretReferenceId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
        });

        b.Entity<IntegrationConnection>(e =>
        {
            e.HasKey(x => x.IntegrationConnectionId);
            e.HasIndex(x => x.OrganizationId);
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne<IntegrationProvider>().WithMany().HasForeignKey(x => x.IntegrationProviderId);
            e.HasOne(x => x.SecretReference).WithMany().HasForeignKey(x => x.SecretReferenceId);
        });
    }

    private static void ConfigureDatabaseMetadata(ModelBuilder b)
    {
        b.Entity<DatabaseConnection>(e =>
        {
            e.HasKey(x => x.DatabaseConnectionId);
            e.HasOne(x => x.IntegrationConnection).WithMany().HasForeignKey(x => x.IntegrationConnectionId);
        });

        b.Entity<DatabaseSchema>(e =>
        {
            e.HasKey(x => x.DatabaseSchemaId);
            e.HasIndex(x => new { x.DatabaseConnectionId, x.SchemaName }).IsUnique();
            e.HasOne(x => x.DatabaseConnection).WithMany().HasForeignKey(x => x.DatabaseConnectionId);
        });

        b.Entity<DatabaseObject>(e =>
        {
            e.HasKey(x => x.DatabaseObjectId);
            e.HasIndex(x => new { x.DatabaseSchemaId, x.ObjectTypeCode, x.ObjectName }).IsUnique();
            e.HasOne(x => x.DatabaseSchema).WithMany().HasForeignKey(x => x.DatabaseSchemaId);
        });

        b.Entity<DatabaseColumn>(e =>
        {
            e.HasKey(x => x.DatabaseColumnId);
            e.HasIndex(x => new { x.DatabaseObjectId, x.ColumnName }).IsUnique();
            e.HasOne(x => x.DatabaseObject).WithMany().HasForeignKey(x => x.DatabaseObjectId);
        });

        b.Entity<DatabaseForeignKey>(e =>
        {
            e.HasKey(x => x.DatabaseForeignKeyId);
            e.HasIndex(x => new { x.SourceObjectId, x.TargetObjectId, x.ConstraintName }).IsUnique();
            e.HasOne(x => x.SourceObject).WithMany().HasForeignKey(x => x.SourceObjectId);
            e.HasOne(x => x.TargetObject).WithMany().HasForeignKey(x => x.TargetObjectId);
        });
    }

    private static void ConfigureReporting(ModelBuilder b)
    {
        b.Entity<Report>(e =>
        {
            e.HasKey(x => x.ReportId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId);
        });

        b.Entity<ReportSection>(e =>
        {
            e.HasKey(x => x.ReportSectionId);
            e.HasIndex(x => new { x.ReportId, x.SortOrder }).IsUnique();
            e.HasOne(x => x.Report).WithMany(r => r.Sections).HasForeignKey(x => x.ReportId);
        });
    }

    private static void ConfigureNotifications(ModelBuilder b)
    {
        b.Entity<NotificationChannel>(e =>
        {
            e.HasKey(x => x.NotificationChannelId);
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.IntegrationConnection).WithMany().HasForeignKey(x => x.IntegrationConnectionId);
        });

        b.Entity<NotificationRule>(e =>
        {
            e.HasKey(x => x.NotificationRuleId);
            e.HasIndex(x => new { x.OrganizationId, x.ProjectId, x.NotificationEventTypeId, x.NotificationChannelId }).IsUnique();
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne<NotificationEventType>().WithMany().HasForeignKey(x => x.NotificationEventTypeId);
            e.HasOne(x => x.NotificationChannel).WithMany().HasForeignKey(x => x.NotificationChannelId);
        });

        b.Entity<NotificationDelivery>(e =>
        {
            e.HasKey(x => x.NotificationDeliveryId);
            e.HasIndex(x => new { x.NotificationRuleId, x.StatusCode });
            e.HasOne(x => x.NotificationRule).WithMany().HasForeignKey(x => x.NotificationRuleId);
            e.HasOne(x => x.Analysis).WithMany().HasForeignKey(x => x.AnalysisId);
            e.HasOne(x => x.SecurityFinding).WithMany().HasForeignKey(x => x.SecurityFindingId);
            e.HasOne(x => x.PullRequest).WithMany().HasForeignKey(x => x.PullRequestId);
        });
    }

    private static void ConfigureSettings(ModelBuilder b)
    {
        b.Entity<OrganizationSetting>(e =>
        {
            e.HasKey(x => x.OrganizationSettingId);
            e.HasIndex(x => new { x.OrganizationId, x.SettingKey }).IsUnique();
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId);
        });
    }
}
