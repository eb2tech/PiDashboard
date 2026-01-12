$publishDir = "bin\Debug\net10.0\linux-arm64\publish\"
$targetDir = "pi@pikiosk.local:/home/pi/dev/dashagent/"

if (-Not (Test-Path $publishDir)) {
    Write-Host "Publish directory not found: $publishDir"
    exit 1
}

Write-Host "Copying files to Raspberry Pi..."
# ssh pi@pikiosk.local "sudo systemctl stop dashagent"
ssh pi@pikiosk.local "rm -rf /home/pi/dev/dashagent/"
scp -r "$publishDir\*" $targetDir
# ssh pi@pikiosk.local "sudo systemctl start dashagent"

Write-Host "Done."

