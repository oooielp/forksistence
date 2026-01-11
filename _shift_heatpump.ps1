$offset = 7
$paths = @(
  @{Src='Resources/Textures/Structures/Piping/Atmospherics/pump.rsi/pumpHeat.png';Dest='Resources/Textures/Structures/Piping/Atmospherics/pump_alt1.rsi/pumpHeat.png';DX=$offset;DY=-$offset},
  @{Src='Resources/Textures/Structures/Piping/Atmospherics/pump.rsi/pumpHeatOn.png';Dest='Resources/Textures/Structures/Piping/Atmospherics/pump_alt1.rsi/pumpHeatOn.png';DX=$offset;DY=-$offset},
  @{Src='Resources/Textures/Structures/Piping/Atmospherics/pump.rsi/pumpHeat.png';Dest='Resources/Textures/Structures/Piping/Atmospherics/pump_alt2.rsi/pumpHeat.png';DX=-$offset;DY=$offset},
  @{Src='Resources/Textures/Structures/Piping/Atmospherics/pump.rsi/pumpHeatOn.png';Dest='Resources/Textures/Structures/Piping/Atmospherics/pump_alt2.rsi/pumpHeatOn.png';DX=-$offset;DY=$offset}
)
Add-Type -AssemblyName System.Drawing
foreach($p in $paths){
  $src = Join-Path $PWD $p.Src
  $dest = Join-Path $PWD $p.Dest
  $bmp = [System.Drawing.Bitmap]::new($src)
  $new = [System.Drawing.Bitmap]::new($bmp.Width, $bmp.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($new)
  $g.Clear([System.Drawing.Color]::Transparent)
  $g.DrawImage($bmp, $p.DX, $p.DY)
  $g.Dispose()
  $new.Save($dest, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()
  $new.Dispose()
}
