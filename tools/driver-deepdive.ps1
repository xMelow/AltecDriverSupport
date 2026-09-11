#requires -Version 5.1
param(
    [string[]]$DriverName = @('Altec ATP-300 Pro', 'HP Color LaserJet Pro M453-4 PCL-6 (V4)')
)
$ErrorActionPreference = 'SilentlyContinue'

function H($t){ "`n" + ('=' * 78) + "`n  $t`n" + ('=' * 78) }

foreach ($name in $DriverName) {

    H "DRIVER: $name"

    # ---- 1. Get-PrinterDriver (spooler view) -------------------------------
    $d = Get-PrinterDriver -Name $name
    H "1) Get-PrinterDriver"
    $d | Format-List *

    # ---- 2. Win32_PrinterDriver (adds real DriverPath / monitor) ----------
    $w = Get-CimInstance Win32_PrinterDriver | Where-Object { $_.Name -like "$name,*" }
    H "2) Win32_PrinterDriver"
    $w | Format-List Name, Version, SupportedPlatform, DriverPath, ConfigFile, DataFile, MonitorName, OEMUrl

    # ---- 3. Registry (the goldmine: isolation, attrs, core deps, hw ids) --
    $verKey = if ($d.MajorVersion -eq 4) { 'Version-4' } else { 'Version-3' }
    $rk = "HKLM:\SYSTEM\CurrentControlSet\Control\Print\Environments\Windows x64\Drivers\$verKey\$name"
    H "3) Registry  ($rk)"
    if (Test-Path $rk) {
        Get-ItemProperty $rk | Format-List *
    } else { "  (key not found)" }

    # ---- 4. INF [Version] section ---------------------------------------
    H "4) INF  ($($d.InfPath))"
    if ($d.InfPath -and (Test-Path $d.InfPath)) {
        Get-Content $d.InfPath | Select-String -Pattern '^\s*(Provider|CatalogFile|DriverVer|Class|ClassGUID|Signature)\s*=' |
            ForEach-Object { "  " + $_.Line.Trim() }
        $store = Split-Path $d.InfPath
        $files = Get-ChildItem $store -Recurse -File
        "  --"
        "  DriverStore folder : $store"
        "  Package files      : $($files.Count)  ($([math]::Round(($files | Measure-Object Length -Sum).Sum/1MB,2)) MB)"
        "  Catalog(s)         : " + (($files | Where-Object Extension -eq '.cat').Name -join ', ')
    } else { "  (INF not accessible)" }

    # ---- 5. Binary file version info + signature -------------------------
    $bin = $w.DriverPath
    H "5) Driver binary  ($bin)"
    if ($bin -and (Test-Path $bin)) {
        $fi  = (Get-Item $bin).VersionInfo
        $fi | Format-List FileName, CompanyName, FileDescription, FileVersion, ProductVersion, ProductName, LegalCopyright, OriginalFilename, Language
        $sig = Get-AuthenticodeSignature $bin
        H "   Authenticode"
        $sig | Format-List Status, StatusMessage, SignatureType
        if ($sig.SignerCertificate) {
            [pscustomobject]@{
                Subject     = $sig.SignerCertificate.Subject
                Issuer      = $sig.SignerCertificate.Issuer
                NotBefore   = $sig.SignerCertificate.NotBefore
                NotAfter    = $sig.SignerCertificate.NotAfter
                Thumbprint  = $sig.SignerCertificate.Thumbprint
            } | Format-List
        }
        if ($sig.TimeStamperCertificate) {
            "   Timestamped by : $($sig.TimeStamperCertificate.Subject)"
        }
    } else { "  (binary not accessible)" }

    # ---- 6. What the driver/queue can do (capabilities) -----------------
    $q = Get-Printer | Where-Object DriverName -eq $name | Select-Object -First 1
    if ($q) {
        H "6) Capabilities via queue '$($q.Name)'  (Get-PrintConfiguration / PrinterProperty)"
        Get-PrintConfiguration -PrinterName $q.Name | Format-List Color, DuplexingMode, PaperSize, PrintQuality, Collate, PagesPerSheet
        "  -- driver-exposed properties (first 25) --"
        Get-PrinterProperty -PrinterName $q.Name | Select-Object PropertyName, Type, Value -First 25 | Format-Table -AutoSize
    }
}

# ---- 7. Driver-related event log ---------------------------------------
H "7) PrintService / driver events (last 15)"
Get-WinEvent -FilterHashtable @{ LogName='Microsoft-Windows-PrintService/Admin' } -MaxEvents 15 -ErrorAction SilentlyContinue |
    Select-Object TimeCreated, Id, LevelDisplayName, Message | Format-Table -AutoSize -Wrap
Get-WinEvent -FilterHashtable @{ LogName='Microsoft-Windows-PrintService/Operational'; Id=300,301,302,215 } -MaxEvents 15 -ErrorAction SilentlyContinue |
    Select-Object TimeCreated, Id, Message | Format-Table -AutoSize -Wrap
