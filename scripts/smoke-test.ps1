# Smoke-test the running X-Ray API against the demo application.
#   .\scripts\smoke-test.ps1
$ErrorActionPreference = 'Stop'
$base = 'http://127.0.0.1:8000'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\demo-app')).Path

Write-Host '--- health ---'
Invoke-RestMethod "$base/health" | ConvertTo-Json -Compress

Write-Host '--- create project ---'
$project = @{ id = 'cgone-demo'; name = 'CGOne Demo'; root = $root; include = @(); critical = @() } | ConvertTo-Json
try {
    Invoke-RestMethod -Method Post "$base/projects" -ContentType 'application/json' -Body $project | ConvertTo-Json -Compress
} catch {
    Write-Host 'project already exists, continuing'
}

Write-Host '--- ingest ---'
Invoke-RestMethod -Method Post "$base/projects/cgone-demo/ingest" | ConvertTo-Json -Compress

Write-Host '--- analyze ---'
$change = @{
    project_id    = 'cgone-demo'
    run_security  = $true
    change        = @{
        source = 'manual'
        title  = 'Change payment validation'
        files  = @(
            @{ path = 'CgOne.Demo.Api/Services/PaymentValidator.cs'; added_lines = 12; removed_lines = 3 },
            @{ path = 'CgOne.Demo.Api/Services/PaymentService.cs'; added_lines = 8; removed_lines = 1 }
        )
    }
} | ConvertTo-Json -Depth 6

$result = Invoke-RestMethod -Method Post "$base/analyze/change" -ContentType 'application/json' -Body $change
Write-Host "overall=$($result.overall_state) score=$($result.risk.score) confidence=$($result.confidence)"
$result.nodes | Where-Object { $_.state -ne 'GREEN' } |
    Select-Object state, name, distance, rule_applied |
    Format-Table -AutoSize | Out-String -Width 140 | Write-Host

Write-Host '--- report (first 20 lines) ---'
$report = Invoke-RestMethod "$base/reports/$($result.analysis_id)"
($report.markdown -split "`n" | Select-Object -First 20) -join "`n" | Write-Host
