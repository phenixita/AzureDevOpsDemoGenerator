# AzureDevOpsDemoGenerator

CLI-first toolkit for generating Azure DevOps demo projects from curated templates.

This repository contains:
- `src/AzdoGenCli`: modern command-line experience for provisioning demo projects
- `src/VstsDemoBuilder` and `src/VstsRestAPI`: underlying provisioning engine used by the CLI

## AzdoGenCli Quick Start (GitHub Release EXE)

### Prerequisites
- Windows with PowerShell
- Access to an Azure DevOps organization where you can create projects
- `AzdoGenCli.exe` downloaded from GitHub Releases

`AzdoGenCli.exe` is distributed as a **self-contained single file**. No .NET runtime or SDK installation is required.

### Download and Run
```powershell
# From the folder where you downloaded AzdoGenCli.exe
.\AzdoGenCli.exe --help
```

### Optional: Add to PATH
```powershell
# Example: move AzdoGenCli.exe to C:\Tools\AzdoGenCli
# Then add C:\Tools\AzdoGenCli to your PATH
AzdoGenCli.exe --help
```

## AzdoGenCli Usage

### Command Format
```text
AzdoGenCli [OPTIONS]
```

### Options
- `--pat <token>`: Personal Access Token authentication
- `--org <organization>`: Azure DevOps organization name (for `https://dev.azure.com/<organization>`)
- `--project <name>`: project name to create
- `--template <name>`: template short name, display name, or template folder
- `--list-templates`: list all embedded templates and exit
- `--verbose`: enable debug file logging
- `--console`: print log output to console (in addition to file)
- `--dry-run`: collect/validate inputs and exit without provisioning
- `--help` or `-h`: show usage

Note: both `--flag` and `-flag` forms are accepted by the parser.

## Authentication Modes

## How Login Works (User Guide)

`AzdoGenCli.exe` always needs an Azure DevOps access token before it can create a project.

It chooses auth in this order:
1. `--pat` argument (if provided)
2. `AZURE_DEVOPS_PAT` environment variable (if set)
3. OAuth browser sign-in (default fallback)

### Option A: OAuth sign-in (default)
Use this when you do not want to pass/store a PAT.

What happens:
1. Run `.\AzdoGenCli.exe`
2. CLI prints a Microsoft login URL
3. Open the URL in your browser and sign in
4. After successful sign-in, return to console
5. CLI continues to organization/project/template prompts

Notes:
- OAuth wait timeout is about 2 minutes; if it expires, rerun the command.
- If OAuth later fails during project creation with `OAUTHACCESSDENIED`, switch to PAT mode.

### Option B: PAT sign-in (recommended for automation)
Use this for non-interactive runs, CI, or when OAuth permissions are insufficient.

PAT via argument:
```powershell
.\AzdoGenCli.exe --pat "<your-pat>"
```

PAT via environment variable:
```powershell
$env:AZURE_DEVOPS_PAT = "<your-pat>"
.\AzdoGenCli.exe --org myorg --project Demo01 --template SmartHotel360
```

PAT guidance:
- Use a PAT from the same Azure DevOps user/account that should own creation actions.
- If unsure, create a PAT with broad enough permissions to create projects and configure artifacts in your org.
- Avoid sharing PATs in chat, screenshots, or committed scripts.

### Which should I use?
- Use OAuth for quick interactive local usage.
- Use PAT for reliability, repeatability, and scripted/non-interactive provisioning.

## Common Workflows

### 1) Discover Templates
```powershell
.\AzdoGenCli.exe --list-templates
```

### 2) Fully Non-Interactive Provisioning
```powershell
.\AzdoGenCli.exe --org myorg --project Demo01 --template SmartHotel360 --pat "<your-pat>"
```

### 3) Semi-Interactive (Template Prompt)
```powershell
.\AzdoGenCli.exe --org myorg --project Demo01 --pat "<your-pat>"
```

### 4) Validate Inputs Only
```powershell
.\AzdoGenCli.exe --org myorg --project Demo01 --template TailwindTraders --dry-run
```

### 5) Debug Logging
```powershell
.\AzdoGenCli.exe --org myorg --project Demo01 --template eShopOnWeb --verbose --console
```

## Template Naming Guidance

`--template` accepts:
- short name (recommended): `SmartHotel360`, `TailwindTraders`, `eShopOnWeb`, `AKS`
- display name: `Tailwind Traders`
- template folder: `Gen-SmartHotel360`

Use `--list-templates` to see the exact currently embedded values.

## Project Name Rules

Interactive validation enforces:
- 1 to 64 characters
- letters, numbers, `_`, `-`
- no spaces

## Output and Logs

On success, CLI prints the created project URL:
- `https://dev.azure.com/<org>/<project>`

Log files are written to:
- `%USERPROFILE%\.azdo-gen\logs\azdo-gen-<date>.log`

## Troubleshooting

### OAuth access denied during project creation
If you see `OAUTHACCESSDENIED`, retry with PAT auth:
```powershell
.\AzdoGenCli.exe --pat "<your-pat>" --org myorg --project Demo01 --template SmartHotel360
```

### Template not found
Run:
```powershell
.\AzdoGenCli.exe --list-templates
```
Then copy one of the listed short names.

## For Contributors (Source Build)

If you are developing from source:

```powershell
dotnet restore src\VSTSDemoGeneratorV2.sln
dotnet build src\VSTSDemoGeneratorV2.sln
dotnet run --project src\AzdoGenCli -- --help
```

Builds from source can emit transitive dependency warnings such as `NU1902` / `NU1904`. Track and remediate via dependency updates.

- Main CLI entry point: `src/AzdoGenCli/Program.cs`
- CLI argument parser: `src/AzdoGenCli/CliArgs.cs`
- Interactive input and validation: `src/AzdoGenCli/InputCollector.cs`
- Template metadata source: `src/AzdoGenCli/Templates/TemplateSetting.json`
