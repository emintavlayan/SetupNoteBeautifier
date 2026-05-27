# SetupNoteBeautifier

## Project purpose

SetupNoteBeautifier is a client-side SAFE Stack app that formats radiotherapy setup note text into a cleaner key/value output for manual copy/paste into clinical systems.

## Local development

From the repository root:

```bash
dotnet run
```

Then open `http://localhost:8080`.

## Build and test

From the repository root:

```bash
dotnet build Application.sln --configuration Release
dotnet test Application.sln --configuration Release
```

## Static deployment

Production build command:

```bash
dotnet run -- Bundle
```

Deployable static output folder:

`deploy/public`

Example PowerShell copy command:

```powershell
$source = ".\deploy\public\*"
$target = "C:\inetpub\wwwroot\SetupNoteBeautifier\"
Copy-Item -Path $source -Destination $target -Recurse -Force
```

## Clinical trimming assumptions

- This tool is a text formatter, not a clinical validator.
- It preserves parsed values unless shortening options are explicitly enabled.
- Extreme key shortening is optional.
- Users must review copied output before pasting into the clinical system.

## Privacy note

Processing is client-side in the current version. Pasted setup text is not sent to the server.
