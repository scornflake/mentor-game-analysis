# Get-Version.ps1
# Calculates the application version from git commit count
# Returns: Major.Minor.BuildNumber (e.g., 1.0.39)

param(
    [Parameter()]
    [switch]$VersionOnly,
    
    [Parameter()]
    [switch]$BuildNumberOnly
)

$ErrorActionPreference = "Stop"

# Get script directory and project root
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir

# Path to Directory.Build.props
$BuildPropsPath = Join-Path $ProjectRoot "Directory.Build.props"

# Function to extract VersionPrefix from Directory.Build.props
function Get-VersionPrefix {
    if (-not (Test-Path $BuildPropsPath)) {
        Write-Error "Directory.Build.props not found at: $BuildPropsPath"
        exit 1
    }
    
    $xml = [xml](Get-Content $BuildPropsPath)
    $versionPrefix = $xml.Project.PropertyGroup.VersionPrefix
    
    if ([string]::IsNullOrWhiteSpace($versionPrefix)) {
        Write-Error "VersionPrefix not found in Directory.Build.props"
        exit 1
    }
    
    return $versionPrefix.Trim()
}

# Function to get build number from various sources
function Get-BuildNumber {
    # 1. Check for CI/CD environment variables
    if ($env:GITHUB_RUN_NUMBER) {
        Write-Verbose "Using GitHub Actions run number: $env:GITHUB_RUN_NUMBER"
        return $env:GITHUB_RUN_NUMBER
    }
    
    if ($env:BUILD_BUILDNUMBER) {
        Write-Verbose "Using Azure DevOps build number: $env:BUILD_BUILDNUMBER"
        return $env:BUILD_BUILDNUMBER
    }
    
    # 2. Try to get git commit count
    try {
        Push-Location $ProjectRoot
        
        # Check if we're in a git repository
        $gitDir = git rev-parse --git-dir 2>$null
        if ($LASTEXITCODE -eq 0) {
            $commitCount = git rev-list --count HEAD 2>$null
            if ($LASTEXITCODE -eq 0 -and $commitCount) {
                Write-Verbose "Using git commit count: $commitCount"
                return $commitCount
            }
        }
    }
    catch {
        Write-Verbose "Git not available or not in a git repository"
    }
    finally {
        Pop-Location
    }
    
    # 3. Fallback to 0
    Write-Warning "Could not determine build number from git or CI/CD. Using 0."
    return "0"
}

# Main execution
try {
    $versionPrefix = Get-VersionPrefix
    $buildNumber = Get-BuildNumber
    $fullVersion = "$versionPrefix.$buildNumber"
    
    if ($BuildNumberOnly) {
        Write-Output $buildNumber
    }
    elseif ($VersionOnly) {
        Write-Output $fullVersion
    }
    else {
        # Default: output structured information
        Write-Host "Version Information:" -ForegroundColor Cyan
        Write-Host "  Version Prefix: $versionPrefix" -ForegroundColor White
        Write-Host "  Build Number:   $buildNumber" -ForegroundColor White
        Write-Host "  Full Version:   $fullVersion" -ForegroundColor Green
        Write-Output $fullVersion
    }
}
catch {
    Write-Error "Failed to determine version: $_"
    exit 1
}

