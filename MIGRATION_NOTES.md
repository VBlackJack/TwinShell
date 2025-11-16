# EF Core Migration Instructions

## Create Migration for Custom Categories

Since the dotnet CLI is not available in this environment, the migration needs to be created manually or when you have access to the dotnet tools.

### Command to Run:

```bash
cd src/TwinShell.Persistence
dotnet ef migrations add AddCustomCategories --startup-project ../TwinShell.App
```

### Migration Details:

This migration should create two new tables:

1. **CustomCategories**
   - Id (string, PK)
   - Name (string, required, max 100 chars, indexed)
   - IconKey (string, required, max 50 chars)
   - ColorHex (string, required, max 7 chars)
   - IsSystemCategory (bool, required)
   - DisplayOrder (int, required, indexed)
   - IsHidden (bool, required)
   - Description (string, nullable, max 500 chars)
   - CreatedAt (DateTime, required)
   - ModifiedAt (DateTime, nullable)

2. **ActionCategoryMappings**
   - Id (string, PK)
   - ActionId (string, required, FK to Actions)
   - CategoryId (string, required, FK to CustomCategories)
   - CreatedAt (DateTime, required)
   - Unique constraint on (ActionId, CategoryId)

### Database will be auto-migrated on application startup

The migration will be applied automatically when the application starts thanks to:
```csharp
await context.Database.MigrateAsync();
```

### Seed System Categories (Optional)

You may want to seed some default system categories:
- "Active Directory" (icon: "user", color: "#2196F3")
- "DNS" (icon: "network", color: "#4CAF50")
- "Security" (icon: "shield", color: "#F44336")
- "Backup" (icon: "archive", color: "#FF9800")
