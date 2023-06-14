[CmdletBinding(PositionalBinding=$false, DefaultParameterSetName = 'CommandLine')]
Param(
  [Parameter(ParameterSetName='CommandLine')]
  [string][Alias('c')]$configuration = "Debug",
  [Parameter(ParameterSetName='CommandLine')]
  [string]$platform = $null,
  [Parameter(ParameterSetName='CommandLine')]
  [string] $projects,
  [Parameter(ParameterSetName='CommandLine')]
  [string][Alias('v')]$verbosity = "minimal",
  [Parameter(ParameterSetName='CommandLine')]
  [string] $msbuildEngine = $null,
  [Parameter(ParameterSetName='CommandLine')]
  [boolean] $warnAsError = $false,        # NOTE: inverted the Arcade's default
  [Parameter(ParameterSetName='CommandLine')]
  [boolean] $nodeReuse = $true,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('r')]$restore,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $deployDeps,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('b')]$build,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $rebuild,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $deploy,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('t')]$test,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $integrationTest,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $performanceTest,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $sign,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $pack,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $publish,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $clean,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('bl')]$binaryLog,
  [Parameter(ParameterSetName='CommandLine')]
  [switch][Alias('nobl')]$excludeCIBinarylog,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $ci,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $prepareMachine,
  [Parameter(ParameterSetName='CommandLine')]
  [string] $runtimeSourceFeed = '',
  [Parameter(ParameterSetName='CommandLine')]
  [string] $runtimeSourceFeedKey = '',
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $excludePrereleaseVS,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $nativeToolsOnMachine,
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $help,

  # Run tests with code coverage
  [Parameter(ParameterSetName='CommandLine')]
  [switch] $testCoverage,

  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

function Print-Usage() {
  Write-Host "Custom settings:"
  Write-Host "  -testCoverage           Run unit tests and capture code coverage information."
  Write-Host ""
}

if ($help) {
  Get-Help $PSCommandPath

  Print-Usage;
  . $PSScriptRoot/common/build.ps1 -help
  exit 0
}

. $PSScriptRoot/common/build.ps1 `
       -configuration $configuration `
       -platform $platform `
       -projects $projects `
       -verbosity $verbosity `
       -msbuildEngine $msbuildEngine `
       -warnAsError $([boolean]::Parse("$warnAsError")) `
       -nodeReuse $nodeReuse `
       -restore:$restore `
       -deployDeps:$deployDeps `
       -build:$build `
       -rebuild:$rebuild `
       -deploy:$deploy `
       -test:$test `
       -integrationTest:$integrationTest `
       -performanceTest:$performanceTest `
       -sign:$sign `
       -pack:$pack `
       -publish:$publish `
       -clean:$clean `
       -binaryLog:$binaryLog `
       -excludeCIBinarylog:$excludeCIBinarylog `
       -ci:$ci `
       -prepareMachine:$prepareMachine `
       -runtimeSourceFeed $runtimeSourceFeed `
       -runtimeSourceFeedKey $runtimeSourceFeedKey `
       -excludePrereleaseVS:$excludePrereleaseVS `
       -nativeToolsOnMachine:$nativeToolsOnMachine `
       -help:$help `
       @properties


# Perform code coverage as the last operation, this enables the following scenarios:
#   .\build.cmd -restore -build -c Release -testCoverage
if ($testCoverage) {
  try {
    # Install required toolset
    . $PSScriptRoot/common/tools.ps1
    InitializeDotNetCli -install $true | Out-Null

    Push-Location $PSScriptRoot/../

    $testResultPath = "./artifacts/TestResults/$configuration";

    # Run tests and collect code coverage
    ./.dotnet/dotnet dotnet-coverage collect --settings ./eng/CodeCoverage.config --output $testResultPath/local.cobertura.xml "build.cmd -test -configuration $configuration -bl:`$$binaryLog"

    # Generate the code coverage report and open it in the browser
    ./.dotnet/dotnet reportgenerator -reports:$testResultPath/*.cobertura.xml -targetdir:$testResultPath/CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
    Start-Process $testResultPath/CoverageResultsHtml/index.html
  }
  catch {
    Write-Host $_.Exception.Message -Foreground "Red"
    Write-Host $_.ScriptStackTrace -Foreground "DarkGray"
    exit $global:LASTEXITCODE;
  }
  finally {
    Pop-Location
  }
}
