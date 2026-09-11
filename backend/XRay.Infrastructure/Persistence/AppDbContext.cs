using Microsoft.EntityFrameworkCore;
using XRay.Domain.Ai;
using XRay.Domain.Analysis;
using XRay.Domain.Audit;
using XRay.Domain.Changes;
using XRay.Domain.DatabaseMetadata;
using XRay.Domain.EvidenceModel;
using XRay.Domain.Graph;
using XRay.Domain.Identity;
using XRay.Domain.Ingestion;
using XRay.Domain.Integrations;
using XRay.Domain.Jobs;
using XRay.Domain.Notifications;
using XRay.Domain.Projects;
using XRay.Domain.Recommendations;
using XRay.Domain.Reference;
using XRay.Domain.Reporting;
using XRay.Domain.Security;
using XRay.Domain.Settings;
using XRay.Domain.Tests;

namespace XRay.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Reference / lookup tables
    public DbSet<RiskState> RiskStates => Set<RiskState>();
    public DbSet<ComponentType> ComponentTypes => Set<ComponentType>();
    public DbSet<GraphEdgeType> GraphEdgeTypes => Set<GraphEdgeType>();
    public DbSet<EvidenceType> EvidenceTypes => Set<EvidenceType>();
    public DbSet<ChangeType> ChangeTypes => Set<ChangeType>();
    public DbSet<AnalysisStatus> AnalysisStatuses => Set<AnalysisStatus>();
    public DbSet<IntegrationProvider> IntegrationProviders => Set<IntegrationProvider>();
    public DbSet<SecuritySeverity> SecuritySeverities => Set<SecuritySeverity>();
    public DbSet<AnalysisStage> AnalysisStages => Set<AnalysisStage>();
    public DbSet<NotificationEventType> NotificationEventTypes => Set<NotificationEventType>();

    // Identity
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<OrganizationMembership> OrganizationMemberships => Set<OrganizationMembership>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<ProjectEnvironment> ProjectEnvironments => Set<ProjectEnvironment>();

    // Ingestion
    public DbSet<IngestionRun> IngestionRuns => Set<IngestionRun>();
    public DbSet<CodeFile> CodeFiles => Set<CodeFile>();
    public DbSet<CodeFileVersion> CodeFileVersions => Set<CodeFileVersion>();
    public DbSet<CodeSymbol> CodeSymbols => Set<CodeSymbol>();

    // Graph
    public DbSet<GraphSnapshot> GraphSnapshots => Set<GraphSnapshot>();
    public DbSet<GraphNode> GraphNodes => Set<GraphNode>();
    public DbSet<GraphEdge> GraphEdges => Set<GraphEdge>();

    // Changes
    public DbSet<Change> Changes => Set<Change>();
    public DbSet<PullRequest> PullRequests => Set<PullRequest>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Commit> Commits => Set<Commit>();
    public DbSet<ChangeFile> ChangeFiles => Set<ChangeFile>();

    // Analysis
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AnalysisTarget> AnalysisTargets => Set<AnalysisTarget>();
    public DbSet<AnalysisScope> AnalysisScopes => Set<AnalysisScope>();
    public DbSet<AnalysisProgress> AnalysisProgresses => Set<AnalysisProgress>();
    public DbSet<AnalysisTelemetry> AnalysisTelemetries => Set<AnalysisTelemetry>();
    public DbSet<AnalysisNodeResult> AnalysisNodeResults => Set<AnalysisNodeResult>();
    public DbSet<ImpactPath> ImpactPaths => Set<ImpactPath>();
    public DbSet<ImpactPathEdge> ImpactPathEdges => Set<ImpactPathEdge>();

    // Evidence
    public DbSet<Evidence> Evidences => Set<Evidence>();

    // Security
    public DbSet<SecurityScanner> SecurityScanners => Set<SecurityScanner>();
    public DbSet<SecurityRule> SecurityRules => Set<SecurityRule>();
    public DbSet<SecurityScan> SecurityScans => Set<SecurityScan>();
    public DbSet<SecurityFinding> SecurityFindings => Set<SecurityFinding>();
    public DbSet<SecurityFindingEvidence> SecurityFindingEvidences => Set<SecurityFindingEvidence>();

    // Tests
    public DbSet<TestRun> TestRuns => Set<TestRun>();
    public DbSet<TestResult> TestResults => Set<TestResult>();

    // Recommendations
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    // AI
    public DbSet<AIProvider> AIProviders => Set<AIProvider>();
    public DbSet<AIConfiguration> AIConfigurations => Set<AIConfiguration>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AIGeneration> AIGenerations => Set<AIGeneration>();
    public DbSet<AIOutputReference> AIOutputReferences => Set<AIOutputReference>();

    // Integrations
    public DbSet<SecretReference> SecretReferences => Set<SecretReference>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();

    // Database metadata
    public DbSet<DatabaseConnection> DatabaseConnections => Set<DatabaseConnection>();
    public DbSet<DatabaseSchema> DatabaseSchemas => Set<DatabaseSchema>();
    public DbSet<DatabaseObject> DatabaseObjects => Set<DatabaseObject>();
    public DbSet<DatabaseColumn> DatabaseColumns => Set<DatabaseColumn>();
    public DbSet<DatabaseForeignKey> DatabaseForeignKeys => Set<DatabaseForeignKey>();

    // Reporting
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ReportSection> ReportSections => Set<ReportSection>();

    // Notifications
    public DbSet<NotificationChannel> NotificationChannels => Set<NotificationChannel>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    // Settings
    public DbSet<OrganizationSetting> OrganizationSettings => Set<OrganizationSetting>();

    // Jobs
    public DbSet<Job> Jobs => Set<Job>();

    // Audit
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ModelConfiguration.Configure(modelBuilder);
    }
}
