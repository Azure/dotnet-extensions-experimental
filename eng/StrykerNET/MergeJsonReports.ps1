# Define a list to store all the JSON objects.
$jsonObjects = @()

# Get all JSON files in the current directory.
$jsonFiles = Get-ChildItem -Path . -Filter *.json

# Iterate over each file.
foreach ($file in $jsonFiles)
{
    # Convert the contents of the file to a PowerShell object.
    $jsonObject = Get-Content $file.FullName | ConvertFrom-Json

    # Add the object to the list.
    $jsonObjects += $jsonObject
}

# Create an object for the result.
$result = New-Object PSObject
$result | Add-Member -Type NoteProperty -Name schemaVersion -Value $jsonObjects[0].schemaVersion
$result | Add-Member -Type NoteProperty -Name thresholds -Value $jsonObjects[0].thresholds
$result | Add-Member -Type NoteProperty -Name projectRoot -Value $jsonObjects[0].projectRoot
$result | Add-Member -Type NoteProperty -Name files -Value @{}

# Merge 'files' from all objects into the result.
foreach ($jsonObject in $jsonObjects)
{
    foreach ($file in $jsonObject.files.PSObject.Properties)
    {
        $result.files | Add-Member -Type NoteProperty -Name $file.Name -Value $file.Value
    }
}

# Convert the result to JSON and return.
$result | ConvertTo-Json -Depth 20 # -Depth probably not needed here
