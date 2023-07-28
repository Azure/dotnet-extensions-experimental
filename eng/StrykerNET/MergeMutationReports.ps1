param(
  [Parameter(Mandatory=$true)]
  [string]$ReportsRoot
)

$jsonSample = Get-Content $PSScriptRoot\report-sample.json
$htmlSample = Get-Content $PSScriptRoot\report-sample.html
$jsonReportPath = Join-Path -Path $ReportsRoot -ChildPath "final-mutation-report.json"
$htmlReportPath = Join-Path -Path $ReportsRoot -ChildPath "final-mutation-report.html"
$reportNamePattern = 'mutation-report.json'

function Get-FileStats ($jsonFile) {
    $json = Get-Content $jsonFile | ConvertFrom-Json

    return $json.files.PSObject.Properties | ForEach-Object {
        $filePath = $_.Name.Replace("\", "\\")
        $fileData = $_.Value | ConvertTo-Json -Depth 20
        return "`"$filePath`": $fileData"
    }
}

# cleanup
Remove-Item $jsonReportPath -ErrorAction SilentlyContinue
Remove-Item $htmlReportPath -ErrorAction SilentlyContinue

# create reports
$fileStats =
    Get-ChildItem -Path $ReportsRoot -Filter $reportNamePattern -Recurse -File | 
    ForEach-Object { $_.FullName } | 
    ForEach-Object { Get-FileStats $_ }

$mergedJson = $fileStats -join ','
$jsonReport = $jsonSample.Replace("{}", "{${mergedJson}}")
$htmlReport = $htmlSample.Replace('REPORT_PLACEHOLDER', $jsonReport)

# save
$jsonReport | Set-Content $jsonReportPath
$htmlReport | Set-Content $htmlReportPath
