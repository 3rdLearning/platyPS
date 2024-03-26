<#
.SYNOPSIS
    Builds the MarkDown/MAML DLL and assembles the final package in out\platyPS.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    $Configuration = "Debug",
    [switch]$SkipDocs,
    [string]$DotnetCli,
    [switch]$Publish
)

function Find-DotnetCli()
{
    $dotnetCmd = Get-Command dotnet
    return $dotnetCmd.Path
}


if (-not $DotnetCli) {
    $DotnetCli = Find-DotnetCli
}

if (-not $DotnetCli) {
    throw "dotnet cli is not found in PATH, install it from https://docs.microsoft.com/en-us/dotnet/core/tools"
} else {
    Write-Host "Using dotnet from $DotnetCli"
}

if (Get-Variable -Name IsCoreClr -ValueOnly -ErrorAction SilentlyContinue) {
    $framework = 'netstandard1.6'
} else {
    $framework = 'net451'
}

& $DotnetCli publish ./src/Markdown.MAML -f $framework --output=$pwd/publish /p:Configuration=$Configuration

$assemblyPaths = (
    (Resolve-Path "publish/Markdown.MAML.dll").Path,
    (Resolve-Path "publish/YamlDotNet.dll").Path
)

# copy artifacts
New-Item -Type Directory out -ErrorAction SilentlyContinue > $null
Copy-Item -Rec -Force src\platyPS out
foreach($assemblyPath in $assemblyPaths)
{
	$assemblyFileName = [System.IO.Path]::GetFileName($assemblyPath)
	$outputPath = "out\platyPS\$assemblyFileName"
	if ((-not (Test-Path $outputPath)) -or
		(Test-Path $outputPath -OlderThan (Get-ChildItem $assemblyPath).LastWriteTime))
	{
		Copy-Item $assemblyPath out\platyPS
	} else {
		Write-Host -Foreground Yellow "Skip $assemblyFileName copying"
	}
}

# copy schema file and docs
Copy-Item .\platyPS.schema.md out\platyPS
New-Item -Type Directory out\platyPS\docs -ErrorAction SilentlyContinue > $null
Copy-Item .\docs\* out\platyPS\docs\

# copy template files
New-Item -Type Directory out\platyPS\templates -ErrorAction SilentlyContinue > $null
Copy-Item .\templates\* out\platyPS\templates\

# put the right module version
if ($env:APPVEYOR_REPO_TAG_NAME)
{
    $manifest = Get-Content -raw out\platyPS\platyPS.psd1
    $manifest = $manifest -replace "ModuleVersion = '0.0.1'", "ModuleVersion = '$($env:APPVEYOR_REPO_TAG_NAME)'"
    Set-Content -Value $manifest -Path out\platyPS\platyPS.psd1 -Encoding Ascii
}

# dogfooding: generate help for the module
Remove-Module platyPS -ErrorAction SilentlyContinue
Import-Module $pwd\out\platyPS

if (-not $SkipDocs) {
    New-ExternalHelp docs -OutputPath out\platyPS\en-US -Force
    # reload module, to apply generated help
    Import-Module $pwd\out\platyPS -Force
}

function Publish {
    [Diagnostics.CodeAnalysis.SuppressMessage("PSProvideDefaultParameterValue", '')]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $PublishDir,

        [Parameter(Mandatory)]
        [string]
        $PublishRepository
    )

    $EncryptedApiKeyPath = "$env:LOCALAPPDATA\vscode-powershell\NuGetApiKey.clixml"

    if (Test-Path -LiteralPath $EncryptedApiKeyPath) {
        $NuGetApiKey = LoadAndUnencryptNuGetApiKey $EncryptedApiKeyPath
        "Using stored NuGetApiKey"
    }
    else {
        $cred = PromptUserForNuGetApiKeyCredential -DestinationPath $EncryptedApiKeyPath
        $NuGetApiKey = $cred.GetNetworkCredential().Password
        "The NuGetApiKey has been stored in $EncryptedApiKeyPath"
    }

    $publishParams = @{
        Path        = $PublishDir
        NuGetApiKey = $NuGetApiKey
    }

    if ($PublishRepository) {
        $publishParams['Repository'] = $PublishRepository
    }

    "Calling Publish-Module..."
    Publish-Module @publishParams
}

function PromptUserForNuGetApiKeyCredential {
    [Diagnostics.CodeAnalysis.SuppressMessage("PSProvideDefaultParameterValue", '')]
    param(
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $DestinationPath
    )

    $message = "Enter your NuGet API Key in the password field (or nothing, this isn't used yet in the preview)"
    $nuGetApiKeyCred = Get-Credential -Message $message -UserName "ignored"

    if ($DestinationPath) {
        EncryptAndSaveNuGetApiKey -NuGetApiKeySecureString $nuGetApiKeyCred.Password -Path $DestinationPath
    }

    $nuGetApiKeyCred
}

function EncryptAndSaveNuGetApiKey {
    [Diagnostics.CodeAnalysis.SuppressMessage("PSAvoidUsingConvertToSecureStringWithPlainText", '')]
    [Diagnostics.CodeAnalysis.SuppressMessage("PSProvideDefaultParameterValue", '')]
    param(
        [Parameter(Mandatory, ParameterSetName='SecureString')]
        [ValidateNotNull()]
        [SecureString]
        $NuGetApiKeySecureString,

        [Parameter(Mandatory, ParameterSetName='PlainText')]
        [ValidateNotNullOrEmpty()]
        [string]
        $NuGetApiKey,

        [Parameter(Mandatory)]
        $Path
    )

    if ($PSCmdlet.ParameterSetName -eq 'PlainText') {
        $NuGetApiKeySecureString = ConvertTo-SecureString -String $NuGetApiKey -AsPlainText -Force
    }

    $parentDir = Split-Path $Path -Parent
    if (!(Test-Path -LiteralPath $parentDir)) {
        $null = New-Item -Path $parentDir -ItemType Directory
    }
    elseif (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path
    }

    $NuGetApiKeySecureString | ConvertFrom-SecureString | Export-Clixml $Path
    Write-Verbose "The NuGetApiKey has been encrypted and saved to $Path"
}

function LoadAndUnencryptNuGetApiKey {
    [Diagnostics.CodeAnalysis.SuppressMessage("PSProvideDefaultParameterValue", '')]
    param(
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path
    )

    $storedKey = Import-Clixml $Path | ConvertTo-SecureString
    $cred = New-Object -TypeName PSCredential -ArgumentList 'jpgr',$storedKey
    $cred.GetNetworkCredential().Password
    Write-Verbose "The NuGetApiKey has been loaded and unencrypted from $Path"
}

if ($Publish) {
    Publish -PublishDir "$pwd\out\platyPS" -PublishRepository "PLS3rdLearning"
}