using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XRay.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIProviders",
                columns: table => new
                {
                    AIProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIProviders", x => x.AIProviderId);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisStages",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisStatuses",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChangeTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComponentTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComponentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraphEdgeTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    DefaultCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphEdgeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationProviders",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationEventTypes",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationEventTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.OrganizationId);
                });

            migrationBuilder.CreateTable(
                name: "RiskStates",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecuritySeverities",
                columns: table => new
                {
                    Id = table.Column<byte>(type: "tinyint", nullable: false),
                    SortOrder = table.Column<byte>(type: "tinyint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritySeverities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccounts",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalIdentityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccounts", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "SecretReferences",
                columns: table => new
                {
                    SecretReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretUri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretReferences", x => x.SecretReferenceId);
                    table.ForeignKey(
                        name: "FK_SecretReferences_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityScanners",
                columns: table => new
                {
                    SecurityScannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityScanners", x => x.SecurityScannerId);
                    table.ForeignKey(
                        name: "FK_SecurityScanners_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    AuditEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.AuditEventId);
                    table.ForeignKey(
                        name: "FK_AuditEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvents_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    OrganizationMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.OrganizationMembershipId);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationSettings",
                columns: table => new
                {
                    OrganizationSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StringValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IntValue = table.Column<int>(type: "int", nullable: true),
                    DecimalValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationSettings", x => x.OrganizationSettingId);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationSettings_UserAccounts_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalProjectId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalProjectUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_Projects_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_UserAccounts_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityRules",
                columns: table => new
                {
                    SecurityRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityScannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecuritySeverityId = table.Column<byte>(type: "tinyint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityRules", x => x.SecurityRuleId);
                    table.ForeignKey(
                        name: "FK_SecurityRules_SecurityScanners_SecurityScannerId",
                        column: x => x.SecurityScannerId,
                        principalTable: "SecurityScanners",
                        principalColumn: "SecurityScannerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityRules_SecuritySeverities_SecuritySeverityId",
                        column: x => x.SecuritySeverityId,
                        principalTable: "SecuritySeverities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIConfigurations",
                columns: table => new
                {
                    AIConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AIProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeploymentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MaxTokens = table.Column<int>(type: "int", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConfigurations", x => x.AIConfigurationId);
                    table.ForeignKey(
                        name: "FK_AIConfigurations_AIProviders_AIProviderId",
                        column: x => x.AIProviderId,
                        principalTable: "AIProviders",
                        principalColumn: "AIProviderId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIConfigurations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIConfigurations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Changes",
                columns: table => new
                {
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalChangeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Changes", x => x.ChangeId);
                    table.ForeignKey(
                        name: "FK_Changes_ChangeTypes_ChangeTypeId",
                        column: x => x.ChangeTypeId,
                        principalTable: "ChangeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Changes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Changes_UserAccounts_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnections",
                columns: table => new
                {
                    IntegrationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IntegrationProviderId = table.Column<byte>(type: "tinyint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalTenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalBaseUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastTestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnections", x => x.IntegrationConnectionId);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_IntegrationProviders_IntegrationProviderId",
                        column: x => x.IntegrationProviderId,
                        principalTable: "IntegrationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationConnections_SecretReferences_SecretReferenceId",
                        column: x => x.SecretReferenceId,
                        principalTable: "SecretReferences",
                        principalColumn: "SecretReferenceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.JobId);
                    table.ForeignKey(
                        name: "FK_Jobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jobs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectEnvironments",
                columns: table => new
                {
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEnvironments", x => x.EnvironmentId);
                    table.ForeignKey(
                        name: "FK_ProjectEnvironments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => x.ProjectMemberId);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_UserAccounts_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    PromptTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.PromptTemplateId);
                    table.ForeignKey(
                        name: "FK_PromptTemplates_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromptTemplates_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromptTemplates_UserAccounts_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationProviderId = table.Column<byte>(type: "tinyint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderRepositoryId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CloneUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultBranchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastIndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.RepositoryId);
                    table.ForeignKey(
                        name: "FK_Repositories_IntegrationProviders_IntegrationProviderId",
                        column: x => x.IntegrationProviderId,
                        principalTable: "IntegrationProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Repositories_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkItems",
                columns: table => new
                {
                    WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalWorkItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedTo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItems", x => x.WorkItemId);
                    table.ForeignKey(
                        name: "FK_WorkItems_Changes_ChangeId",
                        column: x => x.ChangeId,
                        principalTable: "Changes",
                        principalColumn: "ChangeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    DatabaseConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SchemaFilter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReadOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.DatabaseConnectionId);
                    table.ForeignKey(
                        name: "FK_DatabaseConnections_IntegrationConnections_IntegrationConnectionId",
                        column: x => x.IntegrationConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "IntegrationConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationChannels",
                columns: table => new
                {
                    NotificationChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChannelTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationChannels", x => x.NotificationChannelId);
                    table.ForeignKey(
                        name: "FK_NotificationChannels_IntegrationConnections_IntegrationConnectionId",
                        column: x => x.IntegrationConnectionId,
                        principalTable: "IntegrationConnections",
                        principalColumn: "IntegrationConnectionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationChannels_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderBranchId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastIndexedCommitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK_Branches_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeFiles",
                columns: table => new
                {
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LanguageCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileExtension = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CurrentContentHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeFiles", x => x.CodeFileId);
                    table.ForeignKey(
                        name: "FK_CodeFiles_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Commits",
                columns: table => new
                {
                    CommitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CommitHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParentHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AuthorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CommittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commits", x => x.CommitId);
                    table.ForeignKey(
                        name: "FK_Commits_Changes_ChangeId",
                        column: x => x.ChangeId,
                        principalTable: "Changes",
                        principalColumn: "ChangeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Commits_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PullRequests",
                columns: table => new
                {
                    PullRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderPullRequestId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceBranchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetBranchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilesChangedCount = table.Column<int>(type: "int", nullable: true),
                    LinesAdded = table.Column<int>(type: "int", nullable: true),
                    LinesDeleted = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequests", x => x.PullRequestId);
                    table.ForeignKey(
                        name: "FK_PullRequests_Changes_ChangeId",
                        column: x => x.ChangeId,
                        principalTable: "Changes",
                        principalColumn: "ChangeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PullRequests_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseSchemas",
                columns: table => new
                {
                    DatabaseSchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchemaName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseSchemas", x => x.DatabaseSchemaId);
                    table.ForeignKey(
                        name: "FK_DatabaseSchemas_DatabaseConnections_DatabaseConnectionId",
                        column: x => x.DatabaseConnectionId,
                        principalTable: "DatabaseConnections",
                        principalColumn: "DatabaseConnectionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRules",
                columns: table => new
                {
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NotificationEventTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    NotificationChannelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRules", x => x.NotificationRuleId);
                    table.ForeignKey(
                        name: "FK_NotificationRules_NotificationChannels_NotificationChannelId",
                        column: x => x.NotificationChannelId,
                        principalTable: "NotificationChannels",
                        principalColumn: "NotificationChannelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRules_NotificationEventTypes_NotificationEventTypeId",
                        column: x => x.NotificationEventTypeId,
                        principalTable: "NotificationEventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRules_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "OrganizationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationRules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngestionRuns",
                columns: table => new
                {
                    IngestionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TriggerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilesDiscovered = table.Column<int>(type: "int", nullable: false),
                    FilesParsed = table.Column<int>(type: "int", nullable: false),
                    FilesFailed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionRuns", x => x.IngestionRunId);
                    table.ForeignKey(
                        name: "FK_IngestionRuns_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngestionRuns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngestionRuns_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "RepositoryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChangeFiles",
                columns: table => new
                {
                    ChangeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangeKindCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LinesAdded = table.Column<int>(type: "int", nullable: true),
                    LinesDeleted = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeFiles", x => x.ChangeFileId);
                    table.ForeignKey(
                        name: "FK_ChangeFiles_Changes_ChangeId",
                        column: x => x.ChangeId,
                        principalTable: "Changes",
                        principalColumn: "ChangeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChangeFiles_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseObjects",
                columns: table => new
                {
                    DatabaseObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseSchemaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectTypeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ObjectName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DefinitionHash = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseObjects", x => x.DatabaseObjectId);
                    table.ForeignKey(
                        name: "FK_DatabaseObjects_DatabaseSchemas_DatabaseSchemaId",
                        column: x => x.DatabaseSchemaId,
                        principalTable: "DatabaseSchemas",
                        principalColumn: "DatabaseSchemaId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeFileVersions",
                columns: table => new
                {
                    CodeFileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngestionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaskedContentUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParserStatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParserError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeFileVersions", x => x.CodeFileVersionId);
                    table.ForeignKey(
                        name: "FK_CodeFileVersions_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeFileVersions_IngestionRuns_IngestionRunId",
                        column: x => x.IngestionRunId,
                        principalTable: "IngestionRuns",
                        principalColumn: "IngestionRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GraphSnapshots",
                columns: table => new
                {
                    GraphSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IngestionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphSnapshots", x => x.GraphSnapshotId);
                    table.ForeignKey(
                        name: "FK_GraphSnapshots_IngestionRuns_IngestionRunId",
                        column: x => x.IngestionRunId,
                        principalTable: "IngestionRuns",
                        principalColumn: "IngestionRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseColumns",
                columns: table => new
                {
                    DatabaseColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatabaseObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColumnName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsNullable = table.Column<bool>(type: "bit", nullable: false),
                    OrdinalPosition = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseColumns", x => x.DatabaseColumnId);
                    table.ForeignKey(
                        name: "FK_DatabaseColumns_DatabaseObjects_DatabaseObjectId",
                        column: x => x.DatabaseObjectId,
                        principalTable: "DatabaseObjects",
                        principalColumn: "DatabaseObjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseForeignKeys",
                columns: table => new
                {
                    DatabaseForeignKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConstraintName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseForeignKeys", x => x.DatabaseForeignKeyId);
                    table.ForeignKey(
                        name: "FK_DatabaseForeignKeys_DatabaseObjects_SourceObjectId",
                        column: x => x.SourceObjectId,
                        principalTable: "DatabaseObjects",
                        principalColumn: "DatabaseObjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DatabaseForeignKeys_DatabaseObjects_TargetObjectId",
                        column: x => x.TargetObjectId,
                        principalTable: "DatabaseObjects",
                        principalColumn: "DatabaseObjectId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeSymbols",
                columns: table => new
                {
                    CodeSymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeFileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    SymbolName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullyQualifiedName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartLine = table.Column<int>(type: "int", nullable: true),
                    StartColumn = table.Column<int>(type: "int", nullable: true),
                    EndLine = table.Column<int>(type: "int", nullable: true),
                    EndColumn = table.Column<int>(type: "int", nullable: true),
                    IsPartial = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSymbols", x => x.CodeSymbolId);
                    table.ForeignKey(
                        name: "FK_CodeSymbols_CodeFileVersions_CodeFileVersionId",
                        column: x => x.CodeFileVersionId,
                        principalTable: "CodeFileVersions",
                        principalColumn: "CodeFileVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeSymbols_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GraphSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnalysisStatusId = table.Column<byte>(type: "tinyint", nullable: false),
                    OverallRiskStateId = table.Column<byte>(type: "tinyint", nullable: true),
                    MaxDepth = table.Column<short>(type: "smallint", nullable: false),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IncludeSecurityScan = table.Column<bool>(type: "bit", nullable: false),
                    IncludeAIExplanation = table.Column<bool>(type: "bit", nullable: false),
                    IncludeExternalApis = table.Column<bool>(type: "bit", nullable: false),
                    TraceTransitive = table.Column<bool>(type: "bit", nullable: false),
                    IsEvidenceComplete = table.Column<bool>(type: "bit", nullable: false),
                    IsDeterministicComplete = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.AnalysisId);
                    table.ForeignKey(
                        name: "FK_Analyses_AnalysisStatuses_AnalysisStatusId",
                        column: x => x.AnalysisStatusId,
                        principalTable: "AnalysisStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_Changes_ChangeId",
                        column: x => x.ChangeId,
                        principalTable: "Changes",
                        principalColumn: "ChangeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_GraphSnapshots_GraphSnapshotId",
                        column: x => x.GraphSnapshotId,
                        principalTable: "GraphSnapshots",
                        principalColumn: "GraphSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_ProjectEnvironments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "ProjectEnvironments",
                        principalColumn: "EnvironmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_RiskStates_OverallRiskStateId",
                        column: x => x.OverallRiskStateId,
                        principalTable: "RiskStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Analyses_UserAccounts_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GraphNodes",
                columns: table => new
                {
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComponentTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodeSymbolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsParsed = table.Column<bool>(type: "bit", nullable: false),
                    IsPartial = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphNodes", x => x.GraphNodeId);
                    table.ForeignKey(
                        name: "FK_GraphNodes_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphNodes_CodeSymbols_CodeSymbolId",
                        column: x => x.CodeSymbolId,
                        principalTable: "CodeSymbols",
                        principalColumn: "CodeSymbolId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphNodes_ComponentTypes_ComponentTypeId",
                        column: x => x.ComponentTypeId,
                        principalTable: "ComponentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphNodes_GraphSnapshots_GraphSnapshotId",
                        column: x => x.GraphSnapshotId,
                        principalTable: "GraphSnapshots",
                        principalColumn: "GraphSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIGenerations",
                columns: table => new
                {
                    AIGenerationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PurposeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidatedOutput = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: true),
                    Degraded = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIGenerations", x => x.AIGenerationId);
                    table.ForeignKey(
                        name: "FK_AIGenerations_AIConfigurations_AIConfigurationId",
                        column: x => x.AIConfigurationId,
                        principalTable: "AIConfigurations",
                        principalColumn: "AIConfigurationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIGenerations_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIGenerations_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "PromptTemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisProgresses",
                columns: table => new
                {
                    AnalysisProgressId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisStageId = table.Column<byte>(type: "tinyint", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PercentComplete = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComponentsTraced = table.Column<int>(type: "int", nullable: true),
                    EstimatedChains = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisProgresses", x => x.AnalysisProgressId);
                    table.ForeignKey(
                        name: "FK_AnalysisProgresses_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisProgresses_AnalysisStages_AnalysisStageId",
                        column: x => x.AnalysisStageId,
                        principalTable: "AnalysisStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisScopes",
                columns: table => new
                {
                    AnalysisScopeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanDirectDependencies = table.Column<bool>(type: "bit", nullable: false),
                    TraceTransitiveDependencies = table.Column<bool>(type: "bit", nullable: false),
                    IncludeExternalBindings = table.Column<bool>(type: "bit", nullable: false),
                    RunStaticSecurityAnalysis = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisScopes", x => x.AnalysisScopeId);
                    table.ForeignKey(
                        name: "FK_AnalysisScopes_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisTelemetries",
                columns: table => new
                {
                    AnalysisTelemetryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisStageId = table.Column<byte>(type: "tinyint", nullable: true),
                    LogLevelCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisTelemetries", x => x.AnalysisTelemetryId);
                    table.ForeignKey(
                        name: "FK_AnalysisTelemetries_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisTelemetries_AnalysisStages_AnalysisStageId",
                        column: x => x.AnalysisStageId,
                        principalTable: "AnalysisStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormatCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StorageUri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.ReportId);
                    table.ForeignKey(
                        name: "FK_Reports_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reports_UserAccounts_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "UserAccounts",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityScans",
                columns: table => new
                {
                    SecurityScanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityScannerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FindingCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityScans", x => x.SecurityScanId);
                    table.ForeignKey(
                        name: "FK_SecurityScans_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityScans_SecurityScanners_SecurityScannerId",
                        column: x => x.SecurityScannerId,
                        principalTable: "SecurityScanners",
                        principalColumn: "SecurityScannerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestRuns",
                columns: table => new
                {
                    TestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PassedCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRuns", x => x.TestRunId);
                    table.ForeignKey(
                        name: "FK_TestRuns_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisNodeResults",
                columns: table => new
                {
                    AnalysisNodeResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RiskStateId = table.Column<byte>(type: "tinyint", nullable: false),
                    Distance = table.Column<int>(type: "int", nullable: true),
                    MinPathConfidence = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    RuleCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDirectlyChanged = table.Column<bool>(type: "bit", nullable: false),
                    IsSecurityAffected = table.Column<bool>(type: "bit", nullable: false),
                    IsRuntimeResolved = table.Column<bool>(type: "bit", nullable: false),
                    IsBeyondDepth = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisNodeResults", x => x.AnalysisNodeResultId);
                    table.ForeignKey(
                        name: "FK_AnalysisNodeResults_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisNodeResults_GraphNodes_GraphNodeId",
                        column: x => x.GraphNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisNodeResults_RiskStates_RiskStateId",
                        column: x => x.RiskStateId,
                        principalTable: "RiskStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisTargets",
                columns: table => new
                {
                    AnalysisTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetReasonCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisTargets", x => x.AnalysisTargetId);
                    table.ForeignKey(
                        name: "FK_AnalysisTargets_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisTargets_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AnalysisTargets_GraphNodes_GraphNodeId",
                        column: x => x.GraphNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GraphEdges",
                columns: table => new
                {
                    GraphEdgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphSnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphEdgeTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TraversalCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsRuntimeResolved = table.Column<bool>(type: "bit", nullable: false),
                    ParserRule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceCodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceLine = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphEdges", x => x.GraphEdgeId);
                    table.ForeignKey(
                        name: "FK_GraphEdges_CodeFiles_SourceCodeFileId",
                        column: x => x.SourceCodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphEdges_GraphEdgeTypes_GraphEdgeTypeId",
                        column: x => x.GraphEdgeTypeId,
                        principalTable: "GraphEdgeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphEdges_GraphNodes_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphEdges_GraphNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GraphEdges_GraphSnapshots_GraphSnapshotId",
                        column: x => x.GraphSnapshotId,
                        principalTable: "GraphSnapshots",
                        principalColumn: "GraphSnapshotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImpactPaths",
                columns: table => new
                {
                    ImpactPathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResultNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PathDistance = table.Column<int>(type: "int", nullable: false),
                    PathConfidence = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ContainsRuntimeEdge = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactPaths", x => x.ImpactPathId);
                    table.ForeignKey(
                        name: "FK_ImpactPaths_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactPaths_GraphNodes_ResultNodeId",
                        column: x => x.ResultNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactPaths_GraphNodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportSections",
                columns: table => new
                {
                    ReportSectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SectionTypeCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSections", x => x.ReportSectionId);
                    table.ForeignKey(
                        name: "FK_ReportSections_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "ReportId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityFindings",
                columns: table => new
                {
                    SecurityFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityScanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SourceLineStart = table.Column<int>(type: "int", nullable: true),
                    SourceLineEnd = table.Column<int>(type: "int", nullable: true),
                    Remediation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fingerprint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityFindings", x => x.SecurityFindingId);
                    table.ForeignKey(
                        name: "FK_SecurityFindings_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityFindings_GraphNodes_GraphNodeId",
                        column: x => x.GraphNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityFindings_SecurityRules_SecurityRuleId",
                        column: x => x.SecurityRuleId,
                        principalTable: "SecurityRules",
                        principalColumn: "SecurityRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityFindings_SecurityScans_SecurityScanId",
                        column: x => x.SecurityScanId,
                        principalTable: "SecurityScans",
                        principalColumn: "SecurityScanId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestResults",
                columns: table => new
                {
                    TestResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestResults", x => x.TestResultId);
                    table.ForeignKey(
                        name: "FK_TestResults_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TestResults_TestRuns_TestRunId",
                        column: x => x.TestRunId,
                        principalTable: "TestRuns",
                        principalColumn: "TestRunId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evidences",
                columns: table => new
                {
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceTypeId = table.Column<byte>(type: "tinyint", nullable: false),
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GraphEdgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImpactPathId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeFileVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceLineStart = table.Column<int>(type: "int", nullable: true),
                    SourceLineEnd = table.Column<int>(type: "int", nullable: true),
                    ParserRule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SourceSnippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidences", x => x.EvidenceId);
                    table.ForeignKey(
                        name: "FK_Evidences_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_CodeFileVersions_CodeFileVersionId",
                        column: x => x.CodeFileVersionId,
                        principalTable: "CodeFileVersions",
                        principalColumn: "CodeFileVersionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_CodeFiles_CodeFileId",
                        column: x => x.CodeFileId,
                        principalTable: "CodeFiles",
                        principalColumn: "CodeFileId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_EvidenceTypes_EvidenceTypeId",
                        column: x => x.EvidenceTypeId,
                        principalTable: "EvidenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_GraphEdges_GraphEdgeId",
                        column: x => x.GraphEdgeId,
                        principalTable: "GraphEdges",
                        principalColumn: "GraphEdgeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_GraphNodes_GraphNodeId",
                        column: x => x.GraphNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidences_ImpactPaths_ImpactPathId",
                        column: x => x.ImpactPathId,
                        principalTable: "ImpactPaths",
                        principalColumn: "ImpactPathId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImpactPathEdges",
                columns: table => new
                {
                    ImpactPathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNo = table.Column<int>(type: "int", nullable: false),
                    GraphEdgeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpactPathEdges", x => new { x.ImpactPathId, x.SequenceNo });
                    table.ForeignKey(
                        name: "FK_ImpactPathEdges_GraphEdges_GraphEdgeId",
                        column: x => x.GraphEdgeId,
                        principalTable: "GraphEdges",
                        principalColumn: "GraphEdgeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImpactPathEdges_ImpactPaths_ImpactPathId",
                        column: x => x.ImpactPathId,
                        principalTable: "ImpactPaths",
                        principalColumn: "ImpactPathId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    NotificationDeliveryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NotificationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecurityFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PullRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StatusCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.NotificationDeliveryId);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_NotificationRules_NotificationRuleId",
                        column: x => x.NotificationRuleId,
                        principalTable: "NotificationRules",
                        principalColumn: "NotificationRuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_PullRequests_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "PullRequests",
                        principalColumn: "PullRequestId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_SecurityFindings_SecurityFindingId",
                        column: x => x.SecurityFindingId,
                        principalTable: "SecurityFindings",
                        principalColumn: "SecurityFindingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recommendations",
                columns: table => new
                {
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GraphNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PriorityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recommendations", x => x.RecommendationId);
                    table.ForeignKey(
                        name: "FK_Recommendations_Analyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "Analyses",
                        principalColumn: "AnalysisId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_GraphNodes_GraphNodeId",
                        column: x => x.GraphNodeId,
                        principalTable: "GraphNodes",
                        principalColumn: "GraphNodeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recommendations_SecurityFindings_SecurityFindingId",
                        column: x => x.SecurityFindingId,
                        principalTable: "SecurityFindings",
                        principalColumn: "SecurityFindingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityFindingEvidences",
                columns: table => new
                {
                    SecurityFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityFindingEvidences", x => new { x.SecurityFindingId, x.EvidenceId });
                    table.ForeignKey(
                        name: "FK_SecurityFindingEvidences_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "EvidenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityFindingEvidences_SecurityFindings_SecurityFindingId",
                        column: x => x.SecurityFindingId,
                        principalTable: "SecurityFindings",
                        principalColumn: "SecurityFindingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIOutputReferences",
                columns: table => new
                {
                    AIOutputReferenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIGenerationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnalysisNodeResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecurityFindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIOutputReferences", x => x.AIOutputReferenceId);
                    table.ForeignKey(
                        name: "FK_AIOutputReferences_AIGenerations_AIGenerationId",
                        column: x => x.AIGenerationId,
                        principalTable: "AIGenerations",
                        principalColumn: "AIGenerationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIOutputReferences_AnalysisNodeResults_AnalysisNodeResultId",
                        column: x => x.AnalysisNodeResultId,
                        principalTable: "AnalysisNodeResults",
                        principalColumn: "AnalysisNodeResultId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIOutputReferences_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "EvidenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIOutputReferences_Recommendations_RecommendationId",
                        column: x => x.RecommendationId,
                        principalTable: "Recommendations",
                        principalColumn: "RecommendationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIOutputReferences_SecurityFindings_SecurityFindingId",
                        column: x => x.SecurityFindingId,
                        principalTable: "SecurityFindings",
                        principalColumn: "SecurityFindingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIConfigurations_AIProviderId",
                table: "AIConfigurations",
                column: "AIProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConfigurations_OrganizationId",
                table: "AIConfigurations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIConfigurations_ProjectId",
                table: "AIConfigurations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGenerations_AIConfigurationId",
                table: "AIGenerations",
                column: "AIConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGenerations_AnalysisId",
                table: "AIGenerations",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AIGenerations_PromptTemplateId",
                table: "AIGenerations",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputReferences_AIGenerationId",
                table: "AIOutputReferences",
                column: "AIGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputReferences_AnalysisNodeResultId",
                table: "AIOutputReferences",
                column: "AnalysisNodeResultId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputReferences_EvidenceId",
                table: "AIOutputReferences",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputReferences_RecommendationId",
                table: "AIOutputReferences",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AIOutputReferences_SecurityFindingId",
                table: "AIOutputReferences",
                column: "SecurityFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_AIProviders_Code",
                table: "AIProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_AnalysisStatusId",
                table: "Analyses",
                column: "AnalysisStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_ChangeId",
                table: "Analyses",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_EnvironmentId",
                table: "Analyses",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_GraphSnapshotId",
                table: "Analyses",
                column: "GraphSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_OverallRiskStateId",
                table: "Analyses",
                column: "OverallRiskStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_ProjectId_CreatedAtUtc",
                table: "Analyses",
                columns: new[] { "ProjectId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_ProjectId_OverallRiskStateId",
                table: "Analyses",
                columns: new[] { "ProjectId", "OverallRiskStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_RequestedByUserId",
                table: "Analyses",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNodeResults_AnalysisId_Distance",
                table: "AnalysisNodeResults",
                columns: new[] { "AnalysisId", "Distance" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNodeResults_AnalysisId_GraphNodeId",
                table: "AnalysisNodeResults",
                columns: new[] { "AnalysisId", "GraphNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNodeResults_AnalysisId_RiskStateId",
                table: "AnalysisNodeResults",
                columns: new[] { "AnalysisId", "RiskStateId" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNodeResults_GraphNodeId",
                table: "AnalysisNodeResults",
                column: "GraphNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNodeResults_RiskStateId",
                table: "AnalysisNodeResults",
                column: "RiskStateId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisProgresses_AnalysisId_AnalysisStageId",
                table: "AnalysisProgresses",
                columns: new[] { "AnalysisId", "AnalysisStageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisProgresses_AnalysisStageId",
                table: "AnalysisProgresses",
                column: "AnalysisStageId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisScopes_AnalysisId",
                table: "AnalysisScopes",
                column: "AnalysisId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisStages_Code",
                table: "AnalysisStages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisStatuses_Code",
                table: "AnalysisStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTargets_AnalysisId",
                table: "AnalysisTargets",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTargets_CodeFileId",
                table: "AnalysisTargets",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTargets_GraphNodeId",
                table: "AnalysisTargets",
                column: "GraphNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTelemetries_AnalysisId_CreatedAtUtc",
                table: "AnalysisTelemetries",
                columns: new[] { "AnalysisId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisTelemetries_AnalysisStageId",
                table: "AnalysisTelemetries",
                column: "AnalysisStageId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_OrganizationId",
                table: "AuditEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_UserId",
                table: "AuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_RepositoryId_Name",
                table: "Branches",
                columns: new[] { "RepositoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFiles_ChangeId_RelativePath",
                table: "ChangeFiles",
                columns: new[] { "ChangeId", "RelativePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChangeFiles_CodeFileId",
                table: "ChangeFiles",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Changes_AuthorUserId",
                table: "Changes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Changes_ChangeTypeId",
                table: "Changes",
                column: "ChangeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Changes_ProjectId_ChangeTypeId",
                table: "Changes",
                columns: new[] { "ProjectId", "ChangeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Changes_ProjectId_CreatedAtUtc",
                table: "Changes",
                columns: new[] { "ProjectId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeTypes_Code",
                table: "ChangeTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeFiles_RepositoryId_RelativePath",
                table: "CodeFiles",
                columns: new[] { "RepositoryId", "RelativePath" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeFileVersions_CodeFileId_IngestionRunId",
                table: "CodeFileVersions",
                columns: new[] { "CodeFileId", "IngestionRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeFileVersions_IngestionRunId",
                table: "CodeFileVersions",
                column: "IngestionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_CodeFileVersionId",
                table: "CodeSymbols",
                column: "CodeFileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_ComponentTypeId",
                table: "CodeSymbols",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_SymbolName",
                table: "CodeSymbols",
                column: "SymbolName");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_ChangeId",
                table: "Commits",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Commits_RepositoryId_CommitHash",
                table: "Commits",
                columns: new[] { "RepositoryId", "CommitHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypes_Code",
                table: "ComponentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseColumns_DatabaseObjectId_ColumnName",
                table: "DatabaseColumns",
                columns: new[] { "DatabaseObjectId", "ColumnName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_IntegrationConnectionId",
                table: "DatabaseConnections",
                column: "IntegrationConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseForeignKeys_SourceObjectId_TargetObjectId_ConstraintName",
                table: "DatabaseForeignKeys",
                columns: new[] { "SourceObjectId", "TargetObjectId", "ConstraintName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseForeignKeys_TargetObjectId",
                table: "DatabaseForeignKeys",
                column: "TargetObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseObjects_DatabaseSchemaId_ObjectTypeCode_ObjectName",
                table: "DatabaseObjects",
                columns: new[] { "DatabaseSchemaId", "ObjectTypeCode", "ObjectName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseSchemas_DatabaseConnectionId_SchemaName",
                table: "DatabaseSchemas",
                columns: new[] { "DatabaseConnectionId", "SchemaName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_AnalysisId",
                table: "Evidences",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_AnalysisId_EvidenceTypeId",
                table: "Evidences",
                columns: new[] { "AnalysisId", "EvidenceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CodeFileId",
                table: "Evidences",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_CodeFileVersionId",
                table: "Evidences",
                column: "CodeFileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_EvidenceTypeId",
                table: "Evidences",
                column: "EvidenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_GraphEdgeId",
                table: "Evidences",
                column: "GraphEdgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_GraphNodeId",
                table: "Evidences",
                column: "GraphNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Evidences_ImpactPathId",
                table: "Evidences",
                column: "ImpactPathId");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTypes_Code",
                table: "EvidenceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_GraphEdgeTypeId",
                table: "GraphEdges",
                column: "GraphEdgeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_GraphSnapshotId_GraphEdgeTypeId",
                table: "GraphEdges",
                columns: new[] { "GraphSnapshotId", "GraphEdgeTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_GraphSnapshotId_SourceNodeId_TargetNodeId_GraphEdgeTypeId",
                table: "GraphEdges",
                columns: new[] { "GraphSnapshotId", "SourceNodeId", "TargetNodeId", "GraphEdgeTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_SourceCodeFileId",
                table: "GraphEdges",
                column: "SourceCodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_SourceNodeId",
                table: "GraphEdges",
                column: "SourceNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdges_TargetNodeId",
                table: "GraphEdges",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdgeTypes_Code",
                table: "GraphEdgeTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraphNodes_CodeFileId",
                table: "GraphNodes",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphNodes_CodeSymbolId",
                table: "GraphNodes",
                column: "CodeSymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphNodes_ComponentTypeId",
                table: "GraphNodes",
                column: "ComponentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphNodes_GraphSnapshotId_ExternalKey",
                table: "GraphNodes",
                columns: new[] { "GraphSnapshotId", "ExternalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GraphSnapshots_IngestionRunId",
                table: "GraphSnapshots",
                column: "IngestionRunId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphSnapshots_ProjectId",
                table: "GraphSnapshots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactPathEdges_GraphEdgeId",
                table: "ImpactPathEdges",
                column: "GraphEdgeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactPathEdges_ImpactPathId_GraphEdgeId",
                table: "ImpactPathEdges",
                columns: new[] { "ImpactPathId", "GraphEdgeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpactPaths_AnalysisId",
                table: "ImpactPaths",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactPaths_ResultNodeId",
                table: "ImpactPaths",
                column: "ResultNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactPaths_TargetNodeId",
                table: "ImpactPaths",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_BranchId",
                table: "IngestionRuns",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_ProjectId",
                table: "IngestionRuns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionRuns_RepositoryId",
                table: "IngestionRuns",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_IntegrationProviderId",
                table: "IntegrationConnections",
                column: "IntegrationProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_OrganizationId",
                table: "IntegrationConnections",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_ProjectId",
                table: "IntegrationConnections",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnections_SecretReferenceId",
                table: "IntegrationConnections",
                column: "SecretReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationProviders_Code",
                table: "IntegrationProviders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_OrganizationId",
                table: "Jobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ProjectId",
                table: "Jobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_IntegrationConnectionId",
                table: "NotificationChannels",
                column: "IntegrationConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationChannels_OrganizationId",
                table: "NotificationChannels",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_AnalysisId",
                table: "NotificationDeliveries",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationRuleId_StatusCode",
                table: "NotificationDeliveries",
                columns: new[] { "NotificationRuleId", "StatusCode" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_PullRequestId",
                table: "NotificationDeliveries",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_SecurityFindingId",
                table: "NotificationDeliveries",
                column: "SecurityFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationEventTypes_Code",
                table: "NotificationEventTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_NotificationChannelId",
                table: "NotificationRules",
                column: "NotificationChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_NotificationEventTypeId",
                table: "NotificationRules",
                column: "NotificationEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_OrganizationId_ProjectId_NotificationEventTypeId_NotificationChannelId",
                table: "NotificationRules",
                columns: new[] { "OrganizationId", "ProjectId", "NotificationEventTypeId", "NotificationChannelId" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_ProjectId",
                table: "NotificationRules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_UserId",
                table: "OrganizationMemberships",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId",
                table: "OrganizationMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_OrganizationId_SettingKey",
                table: "OrganizationSettings",
                columns: new[] { "OrganizationId", "SettingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationSettings_UpdatedByUserId",
                table: "OrganizationSettings",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEnvironments_ProjectId_Name",
                table: "ProjectEnvironments",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_ProjectId_UserId",
                table: "ProjectMembers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedByUserId",
                table: "Projects",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId_Name",
                table: "Projects",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_CreatedByUserId",
                table: "PromptTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_OrganizationId",
                table: "PromptTemplates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_ProjectId",
                table: "PromptTemplates",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_ChangeId",
                table: "PullRequests",
                column: "ChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequests_RepositoryId_ProviderPullRequestId",
                table: "PullRequests",
                columns: new[] { "RepositoryId", "ProviderPullRequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_AnalysisId",
                table: "Recommendations",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_GraphNodeId",
                table: "Recommendations",
                column: "GraphNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Recommendations_SecurityFindingId",
                table: "Recommendations",
                column: "SecurityFindingId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_AnalysisId",
                table: "Reports",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_GeneratedByUserId",
                table: "Reports",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSections_ReportId_SortOrder",
                table: "ReportSections",
                columns: new[] { "ReportId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_IntegrationProviderId",
                table: "Repositories",
                column: "IntegrationProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_ProjectId_Name",
                table: "Repositories",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskStates_Code",
                table: "RiskStates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretReferences_OrganizationId",
                table: "SecretReferences",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindingEvidences_EvidenceId",
                table: "SecurityFindingEvidences",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindings_CodeFileId",
                table: "SecurityFindings",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindings_GraphNodeId",
                table: "SecurityFindings",
                column: "GraphNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindings_SecurityRuleId",
                table: "SecurityFindings",
                column: "SecurityRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindings_SecurityScanId",
                table: "SecurityFindings",
                column: "SecurityScanId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityFindings_StatusCode",
                table: "SecurityFindings",
                column: "StatusCode");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityRules_SecurityScannerId_RuleCode",
                table: "SecurityRules",
                columns: new[] { "SecurityScannerId", "RuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityRules_SecuritySeverityId",
                table: "SecurityRules",
                column: "SecuritySeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityScanners_OrganizationId_Code",
                table: "SecurityScanners",
                columns: new[] { "OrganizationId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityScans_AnalysisId",
                table: "SecurityScans",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityScans_SecurityScannerId",
                table: "SecurityScans",
                column: "SecurityScannerId");

            migrationBuilder.CreateIndex(
                name: "IX_SecuritySeverities_Code",
                table: "SecuritySeverities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_CodeFileId",
                table: "TestResults",
                column: "CodeFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TestResults_TestRunId",
                table: "TestResults",
                column: "TestRunId");

            migrationBuilder.CreateIndex(
                name: "IX_TestRuns_AnalysisId",
                table: "TestRuns",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_ExternalIdentityId",
                table: "UserAccounts",
                column: "ExternalIdentityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_ChangeId_ExternalWorkItemId",
                table: "WorkItems",
                columns: new[] { "ChangeId", "ExternalWorkItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIOutputReferences");

            migrationBuilder.DropTable(
                name: "AnalysisProgresses");

            migrationBuilder.DropTable(
                name: "AnalysisScopes");

            migrationBuilder.DropTable(
                name: "AnalysisTargets");

            migrationBuilder.DropTable(
                name: "AnalysisTelemetries");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "ChangeFiles");

            migrationBuilder.DropTable(
                name: "Commits");

            migrationBuilder.DropTable(
                name: "DatabaseColumns");

            migrationBuilder.DropTable(
                name: "DatabaseForeignKeys");

            migrationBuilder.DropTable(
                name: "ImpactPathEdges");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships");

            migrationBuilder.DropTable(
                name: "OrganizationSettings");

            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "ReportSections");

            migrationBuilder.DropTable(
                name: "SecurityFindingEvidences");

            migrationBuilder.DropTable(
                name: "TestResults");

            migrationBuilder.DropTable(
                name: "WorkItems");

            migrationBuilder.DropTable(
                name: "AIGenerations");

            migrationBuilder.DropTable(
                name: "AnalysisNodeResults");

            migrationBuilder.DropTable(
                name: "Recommendations");

            migrationBuilder.DropTable(
                name: "AnalysisStages");

            migrationBuilder.DropTable(
                name: "DatabaseObjects");

            migrationBuilder.DropTable(
                name: "NotificationRules");

            migrationBuilder.DropTable(
                name: "PullRequests");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "Evidences");

            migrationBuilder.DropTable(
                name: "TestRuns");

            migrationBuilder.DropTable(
                name: "AIConfigurations");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "SecurityFindings");

            migrationBuilder.DropTable(
                name: "DatabaseSchemas");

            migrationBuilder.DropTable(
                name: "NotificationChannels");

            migrationBuilder.DropTable(
                name: "NotificationEventTypes");

            migrationBuilder.DropTable(
                name: "EvidenceTypes");

            migrationBuilder.DropTable(
                name: "GraphEdges");

            migrationBuilder.DropTable(
                name: "ImpactPaths");

            migrationBuilder.DropTable(
                name: "AIProviders");

            migrationBuilder.DropTable(
                name: "SecurityRules");

            migrationBuilder.DropTable(
                name: "SecurityScans");

            migrationBuilder.DropTable(
                name: "DatabaseConnections");

            migrationBuilder.DropTable(
                name: "GraphEdgeTypes");

            migrationBuilder.DropTable(
                name: "GraphNodes");

            migrationBuilder.DropTable(
                name: "SecuritySeverities");

            migrationBuilder.DropTable(
                name: "Analyses");

            migrationBuilder.DropTable(
                name: "SecurityScanners");

            migrationBuilder.DropTable(
                name: "IntegrationConnections");

            migrationBuilder.DropTable(
                name: "CodeSymbols");

            migrationBuilder.DropTable(
                name: "AnalysisStatuses");

            migrationBuilder.DropTable(
                name: "Changes");

            migrationBuilder.DropTable(
                name: "GraphSnapshots");

            migrationBuilder.DropTable(
                name: "ProjectEnvironments");

            migrationBuilder.DropTable(
                name: "RiskStates");

            migrationBuilder.DropTable(
                name: "SecretReferences");

            migrationBuilder.DropTable(
                name: "CodeFileVersions");

            migrationBuilder.DropTable(
                name: "ComponentTypes");

            migrationBuilder.DropTable(
                name: "ChangeTypes");

            migrationBuilder.DropTable(
                name: "CodeFiles");

            migrationBuilder.DropTable(
                name: "IngestionRuns");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropTable(
                name: "IntegrationProviders");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "UserAccounts");
        }
    }
}
