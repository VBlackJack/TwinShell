# Disaster Recovery Plan

## Overview

This document defines the disaster recovery (DR) procedures for TwinShell, including Recovery Time Objectives (RTO), Recovery Point Objectives (RPO), and step-by-step recovery procedures.

## Recovery Objectives

| Metric | Target | Description |
|--------|--------|-------------|
| **RTO** | 1 hour | Maximum time to restore application functionality |
| **RPO** | 15 minutes | Maximum acceptable data loss (automatic backups every 24h, manual backups on-demand) |

## Backup Strategy

### Automatic Backups

TwinShell automatically creates backups:
- **Frequency**: Every 24 hours (configurable)
- **Location**: `%LOCALAPPDATA%\TwinShell\Backups\`
- **Retention**: 30 days (configurable)
- **Format**: `.twbak` (ZIP archive with JSON data)

### Backup Contents

Each backup includes:
- Actions (commands, templates, examples)
- Categories (custom categories)
- Batches (command sequences)
- Configuration (settings, preferences)
- History (execution logs)

### Manual Backup

To create a manual backup:
1. Open TwinShell
2. Go to **Settings** → **Backup & Restore**
3. Click **Create Backup Now**
4. Choose destination folder (optional)

Or via code:
```csharp
var backupService = serviceProvider.GetRequiredService<IBackupService>();
var result = await backupService.CreateBackupAsync();
```

## Recovery Procedures

### Scenario 1: Corrupted Database

**Symptoms**: Application crashes on startup, "Database error" messages

**Recovery Steps**:
1. Close TwinShell completely
2. Navigate to `%LOCALAPPDATA%\TwinShell\`
3. Rename `twinshell.db` to `twinshell.db.corrupt`
4. Restart TwinShell (creates new empty database)
5. Go to **Settings** → **Backup & Restore**
6. Click **Restore from Backup**
7. Select most recent valid backup
8. Verify data integrity

**Recovery Time**: ~15 minutes

### Scenario 2: Lost Configuration

**Symptoms**: Settings reset, missing preferences, Git sync not working

**Recovery Steps**:
1. Check for backup in `%LOCALAPPDATA%\TwinShell\Backups\`
2. Go to **Settings** → **Backup & Restore**
3. Click **Restore from Backup** → Select backup
4. Choose "Configuration only" option
5. Restart application

**Recovery Time**: ~5 minutes

### Scenario 3: Complete Data Loss

**Symptoms**: All actions, batches, and settings missing

**Recovery Steps**:
1. Locate backup (local or Git repository)
2. If using Git Sync:
   - Go to **Settings** → **Git Sync**
   - Click **Pull from Remote**
3. If using local backup:
   - Go to **Settings** → **Backup & Restore**
   - Click **Restore from Backup**
4. Verify restoration completed
5. Run health check

**Recovery Time**: ~30 minutes

### Scenario 4: Git Sync Conflicts

**Symptoms**: Merge conflicts, sync failures

**Recovery Steps**:
1. Note the conflicted files (shown in error message)
2. Navigate to repository folder
3. Use Git tool to resolve conflicts:
   ```bash
   git status
   git diff
   # Edit conflicted files
   git add .
   git commit -m "Resolve merge conflicts"
   ```
4. Return to TwinShell
5. Click **Full Sync** to complete

**Recovery Time**: ~15-30 minutes (depends on conflicts)

## Verification Procedures

### Post-Recovery Checklist

- [ ] Application starts without errors
- [ ] Actions list populated correctly
- [ ] Batches accessible and executable
- [ ] Settings restored (theme, language)
- [ ] Git sync functional (if configured)
- [ ] Health check passes

### Health Check Command

Run comprehensive health check:
```csharp
var healthService = serviceProvider.GetRequiredService<IHealthCheckService>();
var report = await healthService.CheckAllAsync();
Console.WriteLine($"Status: {report.OverallStatus}");
```

## Prevention Measures

### Daily Operations

1. **Verify automatic backups are running**
   - Check `%LOCALAPPDATA%\TwinShell\Backups\` for recent files
   - Ensure backup timestamp is within last 24 hours

2. **Monitor disk space**
   - Minimum 1 GB free space recommended
   - Cleanup old backups if needed

3. **Test Git sync regularly**
   - Perform manual sync weekly
   - Verify changes propagate correctly

### Weekly Tasks

1. Verify at least one backup exists
2. Test restore procedure (dry run)
3. Check audit logs for errors

### Monthly Tasks

1. Full backup restore test
2. Review and cleanup old backups
3. Verify Git repository integrity

## Contact Information

For assistance with disaster recovery:

- **Documentation**: https://github.com/jbombled/TwinShell/docs
- **Issues**: https://github.com/jbombled/TwinShell/issues
- **Author**: Julien Bombled

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-24 | Julien Bombled | Initial version |
