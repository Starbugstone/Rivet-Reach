param(
    [string]$CaptureDirectory = "$PSScriptRoot\..\Logs\ItemAppearance\RuntimeVerified",
    [string]$IconDirectory = "$PSScriptRoot\..\Logs\ItemAppearance\VerifiedIcons",
    [string]$OutputDirectory = "$PSScriptRoot\..\.docs\verification\item-appearance-2026-09-12"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$report = Get-Content "$CaptureDirectory\report.json" -Raw | ConvertFrom-Json
if ($report.result -ne 'PASS') { throw 'Resolve the native appearance audit failures before publishing its sheets.' }
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$font = [System.Drawing.Font]::new('Segoe UI', 11)
$brush = [System.Drawing.Brushes]::White
$items = @($report.items)
try {
    for ($start = 0; $start -lt $items.Count; $start += 30) {
        $count = [Math]::Min(30, $items.Count - $start)
        $sheet = [System.Drawing.Bitmap]::new(1800, ([int][Math]::Ceiling($count / 5.0) * 280))
        $graphics = [System.Drawing.Graphics]::FromImage($sheet)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(25, 30, 35))
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            for ($i = 0; $i -lt $count; $i++) {
                $item = $items[$start + $i]
                $x = ($i % 5) * 360; $y = [int][Math]::Floor($i / 5.0) * 280
                $held = [System.Drawing.Image]::FromFile("$CaptureDirectory\held-$($item.id).png")
                $icon = [System.Drawing.Image]::FromFile("$IconDirectory\$($item.id).png")
                try {
                    $destination = [System.Drawing.RectangleF]::new($x + 2, $y + 29, 356, 249)
                    $crop = [System.Drawing.RectangleF]::new(($held.Width * .38), ($held.Height * .18), ($held.Width * .62), ($held.Height * .76))
                    $graphics.DrawImage($held, $destination, $crop, [System.Drawing.GraphicsUnit]::Pixel)
                    $graphics.FillRectangle([System.Drawing.Brushes]::Black, $x + 4, $y + 31, 68, 68)
                    $graphics.DrawImage($icon, $x + 6, $y + 33, 64, 64)
                    $graphics.DrawString("$($item.id)  $($item.name)", $font, $brush, [single]($x + 6), [single]($y + 4))
                } finally { $held.Dispose(); $icon.Dispose() }
            }
            $page = [int]($start / 30) + 1
            $sheet.Save("$OutputDirectory\catalog-$page.png", [System.Drawing.Imaging.ImageFormat]::Png)
        } finally { $graphics.Dispose(); $sheet.Dispose() }
    }
} finally { $font.Dispose() }
Copy-Item "$CaptureDirectory\report.json" "$OutputDirectory\report.json" -Force
Write-Output "Created comparison sheets for $($items.Count) registered items."
