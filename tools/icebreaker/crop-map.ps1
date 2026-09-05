param([Parameter(Mandatory=$true)][string]$SourceImage)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$source=(Resolve-Path -LiteralPath $SourceImage).Path
$out=Join-Path $PSScriptRoot '..\..\Plugin\Resources\Maps\Icebreaker\Layers'
New-Item -ItemType Directory -Force $out | Out-Null
$image=[Drawing.Bitmap]::new($source)
try {
    for($i=0;$i -lt 14;$i++) {
        $left=[Math]::Round($i*$image.Width/14)
        $right=[Math]::Round(($i+1)*$image.Width/14)
        $width=$right-$left
        $layer=[Drawing.Bitmap]::new($width,2600,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $graphics=[Drawing.Graphics]::FromImage($layer)
            try {
                $graphics.DrawImage($image,[Drawing.Rectangle]::new(0,0,$width,2600),[Drawing.Rectangle]::new($left,100,$width,2600),[Drawing.GraphicsUnit]::Pixel)
                if($i -in @(0,1,13)) {$graphics.FillRectangle([Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(255,31,31,31)),0,0,$width,430)}
            } finally {$graphics.Dispose()}
            $level=10-$i
            $layer.Save((Join-Path $out "Icebreaker_L$level.png"),[Drawing.Imaging.ImageFormat]::Png)
        } finally {$layer.Dispose()}
    }
} finally {$image.Dispose()}
Write-Output 'Cropped 14 deck layers.'
