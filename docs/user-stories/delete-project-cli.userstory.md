# Add delete-project command to AzdoGenCli

## User Story

**As a** DevOps Engineer or CLI user,  
**I want** to delete an Azure DevOps project directly from the command line,  
**So that** I can easily clean up test projects, reset my demo environment, or automate tear-down scripts without navigating the Azure DevOps web interface.

## Acceptance Criteria

### Scenario 1: Successful Project Deletion (Interactive)
- **Given** I have a valid PAT with permissions to delete projects
- **And** a project named "DemoProject" exists in my organization
- **When** I run `AzdoGenCli --delete-project --project "DemoProject" --org "MyOrg"`
- **Then** the CLI should display a warning "Are you sure you want to delete project 'DemoProject' in organization 'MyOrg'? This action cannot be undone. (y/N)"
- **And** if I enter "y", the project is deleted, and the CLI outputs "✓ Project 'DemoProject' deleted successfully."
- **And** the operation handles the 202 Accepted response gracefully (e.g., "Deletion initiated").

### Scenario 2: Successful Project Deletion (Force)
- **Given** I have a valid PAT with permissions
- **And** a project named "DemoProject" exists in my organization
- **When** I run `AzdoGenCli --delete-project --project "DemoProject" --org "MyOrg" --force`
- **Then** the CLI should **not** prompt for confirmation
- **And** the project is deleted immediately.

### Scenario 3: Project Not Found
- **Given** the project "NonExistentProject" does not exist
- **When** I run the delete command
- **Then** the CLI should output an error "✗ Project 'NonExistentProject' not found." and exit with a non-zero status code.

### Scenario 4: Insufficient Permissions
- **Given** my PAT does not have "Delete" permissions on the project
- **When** I run the delete command
- **Then** the CLI should output an error "✗ Failed to delete project: Unauthorized / Insufficient permissions."

## Technical Notes

### 1. Extend VstsRestAPI
- **File**: `src/VstsRestAPI/ProjectsAndTeams/Projects.cs`
- **Task**: Add a `DeleteProject(string projectId)` method.
- **Implementation Details**:
  - Endpoint: `DELETE https://dev.azure.com/{organization}/_apis/projects/{projectId}?api-version=6.0`
  - The generic API requires the Project ID (GUID), not the name.
  - The implementation should first call the existing `GetProjectIdByName(string projectName)` to resolve the ID.
  - Return the API response status (typically `202 Accepted` for deletion).

### 2. Update CliArgs
- **File**: `src/AzdoGenCli/CliArgs.cs`
- **Task**: Add new properties and flags.
  - `public bool DeleteProject { get; set; }`
  - `public bool Force { get; set; }`
  - Update `Parse` method to handle `--delete-project` and `--force` (alias `-f`).

### 3. Implement CLI Logic
- **File**: `src/AzdoGenCli/Program.cs`
- **Task**: Add command execution logic in `Main`.
  - Condition: `if (cliArgs.DeleteProject)`
  - **Flow**:
    1. Validate authentication (Token/Org provided).
    2. Check if `--force` is present. If not, prompt user: `Console.ReadLine()`.
    3. Initialize `VstsRestAPI.ProjectsAndTeams.Projects` with configuration.
    4. Call `GetProjectIdByName` -> if null, error "Project not found".
    5. Call `DeleteProject` -> handle result.
