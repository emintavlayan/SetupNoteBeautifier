# Static deployment

## Local development

Run the SAFE app in watch mode:

```bash
dotnet run
```

## Tests

Run all xUnit tests:

```bash
dotnet test Application.sln --configuration Release
```

## Production build

Build and bundle the client assets:

```bash
dotnet build Application.sln --configuration Release
dotnet run -- Bundle
```

## Output folder

The deployable static files are generated in:

`deploy/public`

## Deployment copy example

```powershell
$source = ".\deploy\public\*"
$target = "C:\inetpub\wwwroot\SetupNoteBeautifier\"
Copy-Item -Path $source -Destination $target -Recurse -Force
```

## Rollback idea

Before overwriting production files, rename the current deployed folder (for example to `SetupNoteBeautifier_backup_YYYYMMDD_HHMM`) so you can quickly roll back if needed.
