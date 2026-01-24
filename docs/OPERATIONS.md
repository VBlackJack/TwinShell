# Operations Runbook

## Overview

This document provides operational procedures for deploying, maintaining, and troubleshooting TwinShell.

## Table of Contents

1. [Deployment](#deployment)
2. [Monitoring](#monitoring)
3. [Troubleshooting](#troubleshooting)
4. [Maintenance](#maintenance)
5. [Performance Tuning](#performance-tuning)

---

## Deployment

### Pre-Deployment Checklist

- [ ] All tests pass (`dotnet test`)
- [ ] Build succeeds in Release mode
- [ ] Version number updated
- [ ] CHANGELOG updated
- [ ] Smoke tests pass

### Release Process

#### 1. Create Release Build

```powershell
# Build and publish
dotnet publish src/TwinShell.App/TwinShell.App.csproj `
    --configuration Release `
    --output ./publish `
    --self-contained true `
    -r win-x64

# Create portable ZIP
Compress-Archive -Path ./publish/* -DestinationPath ./TwinShell-Portable-win-x64.zip
```

#### 2. Create Git Tag

```bash
# Tag the release
git tag -a v1.6.0 -m "Release v1.6.0"
git push origin v1.6.0
```

#### 3. Verify Release

```powershell
# Run smoke tests
./publish/TwinShell.App.exe --health-check

# Verify required files
$requiredFiles = @(
    "TwinShell.App.exe",
    "TwinShell.Core.dll",
    "TwinShell.Infrastructure.dll",
    "TwinShell.Persistence.dll"
)

foreach ($file in $requiredFiles) {
    if (!(Test-Path "./publish/$file")) {
        Write-Error "Missing: $file"
        exit 1
    }
}
Write-Host "All files present"
```

### Rollback Procedure

If issues are discovered after deployment:

1. **Immediate rollback** (< 5 minutes):
   ```powershell
   # Download previous version
   Invoke-WebRequest -Uri "https://github.com/.../releases/download/v1.5.1/TwinShell-Portable-win-x64.zip" -OutFile "rollback.zip"

   # Extract and replace
   Expand-Archive -Path "rollback.zip" -DestinationPath "./rollback" -Force
   ```

2. **Restore user data** (if needed):
   - User data is preserved in `%LOCALAPPDATA%\TwinShell\`
   - Database and settings are version-independent

---

## Monitoring

### Health Checks

TwinShell provides built-in health checks:

| Check | Description | Severity |
|-------|-------------|----------|
| Database | SQLite connectivity and query performance | Critical |
| Configuration | Settings file validity | Warning |
| FileSystem | Write permissions and disk space | Critical |
| GitSync | Repository accessibility | Warning |
| External | PowerShell Gallery connectivity | Info |

### Interpreting Health Status

- **Healthy** ✅: Component functioning normally
- **Degraded** ⚠️: Component working with issues (app usable)
- **Unhealthy** ❌: Component failed (may block functionality)

### Log Files

| File | Location | Contents |
|------|----------|----------|
| Startup log | `%LOCALAPPDATA%\TwinShell\startup.log` | Application startup events |
| Error log | `%LOCALAPPDATA%\TwinShell\startup-error.log` | Startup errors only |
| Audit log | Database (`AuditLogs` table) | Command execution history |

### Key Metrics to Monitor

1. **Command execution time** (p50, p95, p99)
2. **Backup success rate**
3. **Git sync success rate**
4. **Database query latency**

---

## Troubleshooting

### Common Issues

#### Issue: Application won't start

**Symptoms**: Crash on launch, no window appears

**Diagnosis**:
```powershell
# Check error log
Get-Content "$env:LOCALAPPDATA\TwinShell\startup-error.log" -Tail 50
```

**Solutions**:
1. Delete corrupted database: `Remove-Item "$env:LOCALAPPDATA\TwinShell\twinshell.db"`
2. Reset configuration: `Remove-Item "$env:LOCALAPPDATA\TwinShell\config.json"`
3. Reinstall application

#### Issue: Git sync failing

**Symptoms**: "Failed to pull/push" errors

**Diagnosis**:
```powershell
# Test Git connectivity
cd "$env:LOCALAPPDATA\TwinShell\repo"
git remote -v
git fetch origin
```

**Solutions**:
1. Verify network connectivity
2. Check Git credentials
3. Resolve merge conflicts manually
4. Re-clone repository if corrupted

#### Issue: Commands timing out

**Symptoms**: "Operation timed out" after 30 seconds

**Diagnosis**:
- Check system resources (CPU, memory)
- Verify target system responsiveness
- Check for network issues (remote commands)

**Solutions**:
1. Increase timeout in Settings
2. Break command into smaller batches
3. Check target system availability

#### Issue: High memory usage

**Symptoms**: Application using >500MB RAM

**Diagnosis**:
```powershell
# Check process memory
Get-Process TwinShell.App | Select-Object WorkingSet64
```

**Solutions**:
1. Restart application
2. Clear execution history
3. Reduce batch size

### Diagnostic Commands

```powershell
# Full health check
$health = Invoke-RestMethod -Uri "http://localhost:5000/health"
$health | ConvertTo-Json

# Database statistics
$stats = @{
    Actions = (sqlite3 twinshell.db "SELECT COUNT(*) FROM Actions")
    History = (sqlite3 twinshell.db "SELECT COUNT(*) FROM ExecutionHistory")
    AuditLogs = (sqlite3 twinshell.db "SELECT COUNT(*) FROM AuditLogs")
}
$stats
```

---

## Maintenance

### Daily Tasks

1. **Verify backups**: Check that automatic backup completed
2. **Monitor logs**: Review startup-error.log for issues
3. **Check disk space**: Ensure >1GB free

### Weekly Tasks

1. **Test Git sync**: Perform manual sync
2. **Review audit logs**: Check for unusual patterns
3. **Cleanup old backups**: Run backup retention

### Monthly Tasks

1. **Full backup test**: Restore to test environment
2. **Performance review**: Check execution times
3. **Update dependencies**: Check for security updates

### Database Maintenance

```sql
-- Vacuum database (reclaim space)
VACUUM;

-- Analyze for query optimization
ANALYZE;

-- Check integrity
PRAGMA integrity_check;
```

---

## Performance Tuning

### Recommended Settings

| Setting | Development | Production |
|---------|-------------|------------|
| Command Timeout | 30s | 60s |
| Batch Size | 10 | 50 |
| Log Retention | 30 days | 365 days |
| Backup Frequency | Manual | 24h |

### Optimization Tips

1. **Large action lists**: Enable pagination (future feature)
2. **Slow Git sync**: Use shallow clones
3. **Database performance**: Run VACUUM monthly

### Resource Limits

| Resource | Recommended | Maximum |
|----------|-------------|---------|
| RAM | 200MB | 500MB |
| Disk (app) | 100MB | 500MB |
| Disk (data) | 50MB | 1GB |
| CPU | 5% idle | 50% during execution |

---

## Appendix

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `TWINSHELL_DATA_PATH` | Data directory | `%LOCALAPPDATA%\TwinShell` |
| `TWINSHELL_LOG_LEVEL` | Log verbosity | `Information` |

### Configuration Files

| File | Purpose |
|------|---------|
| `config.json` | User settings |
| `twinshell.db` | SQLite database |
| `backup-meta.json` | Backup tracking |

### Support Contacts

- **GitHub Issues**: https://github.com/jbombled/TwinShell/issues
- **Documentation**: https://github.com/jbombled/TwinShell/docs
