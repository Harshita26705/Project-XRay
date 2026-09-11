using Microsoft.EntityFrameworkCore;
using XRay.Domain.Reference;

namespace XRay.Infrastructure.Persistence;

/// <summary>Idempotent seed data for schema.md's reference/lookup tables (section 4 + stage/notification seeds).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (!await db.RiskStates.AnyAsync(ct))
        {
            db.RiskStates.AddRange(
                new RiskState { Id = 1, Code = "CRITICAL", DisplayName = "Critical", Description = "Strong evidence of direct or high-impact change.", SortOrder = 1 },
                new RiskState { Id = 2, Code = "RISKY", DisplayName = "Risky", Description = "Potential downstream or transitive impact.", SortOrder = 2 },
                new RiskState { Id = 3, Code = "SAFE", DisplayName = "Safe", Description = "No impact detected with currently available evidence.", SortOrder = 3 },
                new RiskState { Id = 4, Code = "UNKNOWN", DisplayName = "Unknown", Description = "Insufficient evidence for a trustworthy conclusion.", SortOrder = 4 }
            );
        }

        if (!await db.ComponentTypes.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[]
                     {
                         "FRONTEND", "FRONTEND_MODULE", "FRONTEND_COMPONENT", "API", "CONTROLLER", "SERVICE",
                         "INTERFACE", "REPOSITORY", "DATABASE", "DATABASE_TABLE", "DATABASE_COLUMN",
                         "STORED_PROCEDURE", "EXTERNAL", "UNKNOWN"
                     })
            {
                db.ComponentTypes.Add(new ComponentType { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        if (!await db.GraphEdgeTypes.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var (code, cost) in new[]
                     {
                         ("CALLS", 1m), ("DEPENDS_ON", 1m), ("IMPORTS", 1m), ("EXPOSES", 1m), ("READS", 1m),
                         ("WRITES", 1m), ("BINDS", 0m), ("INHERITS", 1m), ("REFERENCES", 1m), ("USES", 1m)
                     })
            {
                db.GraphEdgeTypes.Add(new GraphEdgeType { Id = i++, Code = code, DisplayName = ToDisplay(code), DefaultCost = cost });
            }
        }

        if (!await db.EvidenceTypes.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[]
                     {
                         "DIRECT_CHANGE", "STRUCTURAL_DEPENDENCY", "DATABASE_ACCESS", "SECURITY_ALERT",
                         "PARSER_EVIDENCE", "TEST_EVIDENCE", "INTEGRATION_EVIDENCE", "GRAPH_PATH"
                     })
            {
                db.EvidenceTypes.Add(new EvidenceType { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        if (!await db.ChangeTypes.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[] { "PULL_REQUEST", "WORK_ITEM", "COMMIT", "MANUAL" })
            {
                db.ChangeTypes.Add(new ChangeType { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        if (!await db.AnalysisStatuses.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[] { "QUEUED", "RUNNING", "PARTIAL", "COMPLETED", "FAILED", "CANCELLED" })
            {
                db.AnalysisStatuses.Add(new AnalysisStatus { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        if (!await db.IntegrationProviders.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[]
                     {
                         "AZURE_DEVOPS", "SQL_SERVER", "MICROSOFT_FOUNDRY", "AZURE_AI_SEARCH",
                         "MICROSOFT_TEAMS", "POWER_AUTOMATE"
                     })
            {
                db.IntegrationProviders.Add(new IntegrationProvider { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        if (!await db.SecuritySeverities.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[] { "CRITICAL", "HIGH", "MEDIUM", "LOW", "INFO" })
            {
                db.SecuritySeverities.Add(new SecuritySeverity { Id = i, Code = code, DisplayName = ToDisplay(code), SortOrder = i });
                i++;
            }
        }

        if (!await db.AnalysisStages.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[]
                     {
                         "READING_CHANGES", "IDENTIFYING_TARGETS", "BUILDING_BLAST_RADIUS", "SECURITY_SCAN",
                         "RETRIEVING_PROJECT_CONTEXT", "VALIDATING_DEPENDENCIES", "GENERATING_REPORT"
                     })
            {
                db.AnalysisStages.Add(new AnalysisStage { Id = i, Code = code, DisplayName = ToDisplay(code), SortOrder = i });
                i++;
            }
        }

        if (!await db.NotificationEventTypes.AnyAsync(ct))
        {
            byte i = 1;
            foreach (var code in new[]
                     {
                         "HIGH_RISK_CHANGE", "CRITICAL_SECURITY_FINDING", "ANALYSIS_COMPLETED",
                         "ANALYSIS_FAILED", "NEW_PULL_REQUEST"
                     })
            {
                db.NotificationEventTypes.Add(new NotificationEventType { Id = i++, Code = code, DisplayName = ToDisplay(code) });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static string ToDisplay(string code)
    {
        var words = code.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));
    }
}
