#requires -Version 5.1
<#
    printer-inventory.ps1
    Dumps everything Windows will readily tell you about installed printers and their drivers.
    Read-only. No admin strictly required, but run elevated for the most complete data.
#>

$ErrorActionPreference = 'SilentlyContinue'

function Section($t) {
    Write-Host ""
    Write-Host ("=" * 78) -ForegroundColor DarkGray
    Write-Host "  $t" -ForegroundColor Cyan
    Write-Host ("=" * 78) -ForegroundColor DarkGray
}

$report = [ordered]@{}

# ---------------------------------------------------------------------------
Section "SPOOLER SERVICE"
$spooler = Get-Service -Name Spooler
$spooler | Format-Table Status, Name, DisplayName, StartType -AutoSize
$report.Spooler = $spooler | Select-Object Status, Name, StartType

# ---------------------------------------------------------------------------
Section "PRINTERS  (Get-Printer)"
$printers = Get-Printer | Sort-Object Name
$printers | Format-Table Name, DriverName, PortName, Shared, Published, Type, PrinterStatus -AutoSize
$report.Printers = $printers | Select-Object Name, DriverName, PortName, Shared, Published, Type,
    PrinterStatus, Location, Comment, DeviceType, RenderingMode

# ---------------------------------------------------------------------------
Section "PRINTER DRIVERS  (Get-PrinterDriver)"
$drivers = Get-PrinterDriver | Sort-Object Name
foreach ($d in $drivers) {
    Write-Host ""
    Write-Host "  $($d.Name)" -ForegroundColor Yellow
    $d | Format-List Name, PrinterEnvironment, MajorVersion, DriverVersion,
        Manufacturer, ProviderName, InfPath, ConfigFile, DataFile, DriverPath,
        HelpFile, PrintProcessor, DependentFiles
}
$report.Drivers = $drivers | Select-Object Name, PrinterEnvironment, MajorVersion, DriverVersion,
    Manufacturer, ProviderName, InfPath, ConfigFile, DataFile, DriverPath, HelpFile,
    PrintProcessor, @{n='DependentFiles';e={$_.DependentFiles -join '; '}}

# ---------------------------------------------------------------------------
Section "DRIVER VERSION DECODED  (DriverVersion is a packed 64-bit int)"
$decoded = foreach ($d in $drivers) {
    $v = [uint64]$d.DriverVersion
    if ($null -ne $v -and $v -gt 0) {
        $p1 = ($v -shr 48) -band 0xFFFF
        $p2 = ($v -shr 32) -band 0xFFFF
        $p3 = ($v -shr 16) -band 0xFFFF
        $p4 =  $v          -band 0xFFFF
        $ver = "$p1.$p2.$p3.$p4"
    } else { $ver = "" }
    [pscustomobject]@{
        Driver        = $d.Name
        Type          = if ($d.MajorVersion -eq 4) { 'v4 (packaged)' } elseif ($d.MajorVersion -eq 3) { 'v3' } else { "v$($d.MajorVersion)" }
        FileVersion   = $ver
        RawVersion    = $d.DriverVersion
        Environment   = $d.PrinterEnvironment
    }
}
$decoded | Format-Table -AutoSize
$report.DriverVersionsDecoded = $decoded

# ---------------------------------------------------------------------------
Section "DRIVER FILE DETAIL  (on-disk version / signing where resolvable)"
$fileDetail = foreach ($d in $drivers) {
    $path = $d.DriverPath
    if ($path -and (Test-Path $path)) {
        $fi  = Get-Item $path
        $sig = Get-AuthenticodeSignature $path
        [pscustomobject]@{
            Driver         = $d.Name
            File           = Split-Path $path -Leaf
            ProductVersion = $fi.VersionInfo.ProductVersion
            FileVersion    = $fi.VersionInfo.FileVersion
            LastWrite      = $fi.LastWriteTime
            SizeKB         = [math]::Round($fi.Length / 1KB, 1)
            SignStatus     = $sig.Status
            Signer         = $sig.SignerCertificate.Subject -replace '^CN=([^,]+).*','$1'
        }
    }
}
$fileDetail | Format-Table -AutoSize
$report.DriverFileDetail = $fileDetail

# ---------------------------------------------------------------------------
Section "PORTS  (Get-PrinterPort)"
$ports = Get-PrinterPort | Sort-Object Name
$ports | Format-Table Name, Description, PrinterHostAddress, PortNumber, Protocol -AutoSize
$report.Ports = $ports | Select-Object Name, Description, PrinterHostAddress, PortNumber, Protocol, SNMPEnabled

# ---------------------------------------------------------------------------
Section "PRINT PROCESSORS"
$procs = Get-PrinterDriver | ForEach-Object { $_.PrintProcessor } | Sort-Object -Unique
$procs | ForEach-Object { Write-Host "  $_" }
$report.PrintProcessors = $procs

# ---------------------------------------------------------------------------
Section "WMI VIEW  (Win32_Printer  -  extra fields not in Get-Printer)"
$wmi = Get-CimInstance Win32_Printer | Sort-Object Name
$wmi | Format-Table Name, DriverName, PortName, Default, Network, Local, WorkOffline, PrinterState, PrinterStatus -AutoSize
$report.WmiPrinters = $wmi | Select-Object Name, DriverName, PortName, Default, Network, Local,
    Shared, ShareName, ServerName, WorkOffline, PrinterState, PrinterStatus, Attributes,
    CapabilityDescriptions, PrintJobDataType

# ---------------------------------------------------------------------------
Section "WMI VIEW  (Win32_PrinterDriver)"
$wmiDrv = Get-CimInstance Win32_PrinterDriver | Sort-Object Name
foreach ($d in $wmiDrv) {
    Write-Host ""
    Write-Host "  $($d.Name)" -ForegroundColor Yellow
    $d | Format-List Name, Version, SupportedPlatform, DriverPath, ConfigFile, DataFile,
        InfName, FilePath, MonitorName, OEMUrl
}
$report.WmiDrivers = $wmiDrv | Select-Object Name, Version, SupportedPlatform, DriverPath,
    ConfigFile, DataFile, InfName, MonitorName, OEMUrl

# ---------------------------------------------------------------------------
Section "PnP / DRIVER STORE VIEW  (pnputil - printer class)"
Write-Host "  (published INFs in the driver store for class 'Printer')"
$pnp = & pnputil.exe /enum-drivers /class Printer 2>$null
$pnp | Out-String | Write-Host
$report.PnpUtilPrinterDrivers = ($pnp -join "`n")

# ---------------------------------------------------------------------------
Section "3rd-PARTY PRINT DRIVER PACKAGES  (Win32_PnPSignedDriver)"
$signed = Get-CimInstance Win32_PnPSignedDriver |
    Where-Object { $_.DeviceClass -eq 'PRINTER' } |
    Select-Object DeviceName, DriverVersion, DriverDate, DriverProviderName, InfName, IsSigned, Signer
$signed | Format-Table -AutoSize
$report.SignedPrinterDrivers = $signed

# ---------------------------------------------------------------------------
Section "PENDING PRINT JOBS"
$jobs = $printers | ForEach-Object { Get-PrintJob -PrinterName $_.Name }
if ($jobs) { $jobs | Format-Table PrinterName, Id, DocumentName, UserName, JobStatus, Size, SubmittedTime -AutoSize }
else       { Write-Host "  (none)" }
$report.PendingJobs = $jobs | Select-Object PrinterName, Id, DocumentName, UserName, JobStatus, Size

# ---------------------------------------------------------------------------
# Machine-readable dump
$outJson = Join-Path $PSScriptRoot 'printer-inventory.json'
$report | ConvertTo-Json -Depth 6 | Out-File $outJson -Encoding UTF8
Section "DONE"
Write-Host "  JSON written to: $outJson" -ForegroundColor Green
