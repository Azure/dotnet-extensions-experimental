#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

usage()
{
  echo "Custom settings:"
  echo "  --testCoverage             Run unit tests and capture code coverage information"
  echo "  --mutationTesting          Run mutation testing"
  echo ""
}

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
REPO_ROOT=$(realpath $DIR/../)

hasWarnAsError=false
configuration=''
testCoverage=false
mutationTesting=false

properties=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -help|-h)
      usage
      "$DIR/common/build.sh" --help
      exit 0
      ;;
    -warnaserror)
      hasWarnAsError=true
      # Pass through converting to boolean
      value=false
      if [[ "${2,,}" == "true" || "$2" == "1" ]]; then
        value=true
      fi
      properties="$properties $1 $value"
      shift
      ;;
    -configuration|-c)
      configuration=$2
      properties="$properties $1 $2"
      shift
      ;;
    -testcoverage)
      testCoverage=true
      ;;
    -mutationtesting)
      mutationTesting=true
      properties="$properties /p:TestRunnerName=StrykerNET"
      ;;
    *)
      properties="$properties $1"
      ;;
  esac

  shift
done

# The Arcade's default is "warnAsError=true", we want the opposite by default.
if [[ "$hasWarnAsError" == false ]]; then
  properties="$properties --warnAsError false"
fi

# If mutation testing is requested, ensure no incompatible switches supplied
if [[ "$mutationTesting" == true ]]; then
  unsupportedSwitches=('restore' 'build' 'deploy' 'deploydeps' 'integrationtest' 'performancetest' 'sign' 'pack' 'testcoverage')
  for switch in "${unsupportedSwitches[@]}"; do
    if echo $properties | grep -cswi $switch > /dev/null; then
      echo "\e[31m[ERROR] Mutation testing is incompatible with '$switch' switch.\e[0m"
      echo "    Incompatible switches: ${unsupportedSwitches[*]// /|}"
      exit -1
    fi
  done

  requiredSwitches=('test')
  for switch in "${requiredSwitches[@]}"; do
    if echo $properties | grep -cswi $switch > /dev/null; then
      # switch is supplied
      echo "'$switch' switch is supplied" > /dev/null
    else
      properties="$properties --$switch"
    fi
  done

  # Set envvars so that Stryker can locate the .NET SDK
  export DOTNET_ROOT=$REPO_ROOT/.dotnet
  export DOTNET_MULTILEVEL_LOOKUP=0
  export PATH=$DOTNET_ROOT:$PATH

  # Create a marker file
  touch "$REPO_ROOT/.mutationtesting"

  # Remove the marker upon failure
  trap 'rm "$REPO_ROOT/.mutationtesting"' EXIT
fi

"$DIR/common/build.sh" $properties

# Remove the marker when we're done
if [[ "$mutationTesting" == true ]]; then
  [ -e "$REPO_ROOT/.mutationtesting" ] && rm -- "$REPO_ROOT/.mutationtesting"

  testResultsPath="$REPO_ROOT/artifacts/TestResults/$configuration/MutationTestingResults";

  # Merge mutation reports
  $REPO_ROOT/.dotnet/dotnet pwsh collect $REPO_ROOT/eng/StrykerNET/MergeMutationReports.ps1 $testResultsPath
  echo ""
  echo -e "\e[32mCode coverage results:\e[0m $testResultPath/mutation-report-merged.html"
  echo ""
fi

# Perform code coverage as the last operation, this enables the following scenarios:
#   .\build.sh --restore --build --c Release --testCoverage
if [[ "$testCoverage" == true ]]; then
  # Install required toolset
  . "$DIR/common/tools.sh"
  InitializeDotNetCli true > /dev/null

  testResultPath="$REPO_ROOT/artifacts/TestResults/$configuration"

  # Run tests and collect code coverage
  $REPO_ROOT/.dotnet/dotnet 'dotnet-coverage' collect --settings $REPO_ROOT/eng/CodeCoverage.config --output $testResultPath/local.cobertura.xml "$REPO_ROOT/build.sh --test --configuration $configuration"

  # Generate the code coverage report and open it in the browser
  $REPO_ROOT/.dotnet/dotnet reportgenerator -reports:$testResultPath/*.cobertura.xml -targetdir:$testResultPath/CoverageResultsHtml -reporttypes:HtmlInline_AzurePipelines
  echo ""
  echo -e "\e[32mCode coverage results:\e[0m $testResultPath/CoverageResultsHtml/index.html"
  echo ""
fi