# SAFE Template

This template can be used to generate a full-stack web application using the [SAFE Stack](https://safe-stack.github.io/). It was created using the dotnet [SAFE Template](https://safe-stack.github.io/docs/template-overview/). If you want to learn more about the template why not start with the [quick start](https://safe-stack.github.io/docs/quickstart/) guide?

## Install pre-requisites

You'll need to install the following pre-requisites in order to build SAFE applications

* [.NET SDK](https://www.microsoft.com/net/download) 8.0 or higher
* [Node 18](https://nodejs.org/en/download/) or higher
* [NPM 9](https://www.npmjs.com/package/npm) or higher

## Starting the application

To concurrently run the server and the client components in watch mode use the following command:

```bash
dotnet run
```

Then open `http://localhost:8080` in your browser.

The build project in root directory contains a couple of different build targets. You can specify them after `--` (target name is case-insensitive).

To run concurrently server and client tests in watch mode (you can run this command in parallel to the previous one in new terminal):

```bash
dotnet run -- WatchRunTests
```

Client tests are available under `http://localhost:8081` in your browser and server tests are running in watch mode in console.

Finally, there are `Bundle` and `Azure` targets that you can use to package your app and deploy to Azure, respectively:

```bash
dotnet run -- Bundle
dotnet run -- Azure
```

## Build and test

Use these commands locally from the repository root:

```bash
dotnet tool restore
dotnet restore Application.sln
dotnet build Application.sln --configuration Release
dotnet test Application.sln --configuration Release
dotnet build src/Client/Client.fsproj --configuration Release
```

## Static deployment

Build the static client assets from the repository root:

```bash
dotnet build Application.sln --configuration Release
dotnet run -- Bundle
```

Deployable static files are written to:

`deploy/public`

Example PowerShell copy command:

```powershell
$source = ".\deploy\public\*"
$target = "C:\inetpub\wwwroot\SetupNoteBeautifier\"
Copy-Item -Path $source -Destination $target -Recurse -Force
```

The `src/Server` project is kept for future logging/statistics work but is not used by the current setup note trimmer flow.

In the current version, pasted setup text is processed in the browser and is not sent to a server.

## Clinical trimming assumptions

- This tool is a text formatter, not a clinical validator.
- It preserves parsed values unless shortening options are explicitly enabled.
- Extreme key shortening is optional.
- Users must review copied output before pasting into the clinical system.

## SAFE Stack Documentation

If you want to know more about the full Azure Stack and all of it's components (including Azure) visit the official [SAFE documentation](https://safe-stack.github.io/docs/).

You will find more documentation about the used F# components at the following places:

* [Saturn](https://saturnframework.org/)
* [Fable](https://fable.io/docs/)
* [Elmish](https://elmish.github.io/elmish/)
