<#
    PhantomPrinter.ps1  -  create/remove "phantom" print queues so NiceLabel (or
    BarTender) label files that reference a printer you don't have will open
    against a matching driver instead of erroring / mangling the layout.

    A phantom printer = a real Windows print queue with the EXACT name the label
    expects, bound to a chosen driver, pointed at a null port so nothing prints.

    Every queue this tool creates is tagged in its Comment field with
    $script:Tag so "Remove all phantoms" can never touch a genuine printer.

    Usage:
        Right-click > Run with PowerShell   (or use PhantomPrinter.cmd for auto-elevate)
        GUI opens. Paste the name from the NiceLabel error, pick a driver, Create.

    CLI (no GUI):
        .\PhantomPrinter.ps1 -Name "cvxcvx" -Driver "Altec ATP-300 Pro"
        .\PhantomPrinter.ps1 -List
        .\PhantomPrinter.ps1 -RemoveAll
#>
[CmdletBinding()]
param(
    [string]$Name,
    [string]$Driver,
    [switch]$List,
    [switch]$RemoveAll,
    [switch]$NoGui
)

$script:Tag      = 'PHANTOM (PhantomPrinter tool)'
$script:PortName = 'PhantomPrinter_NUL:'
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Elevation
# ---------------------------------------------------------------------------
function Test-Admin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
}
if (-not (Test-Admin)) {
    $psi = @{
        FilePath     = (Get-Process -Id $PID).Path
        Verb         = 'RunAs'
        ArgumentList = @('-ExecutionPolicy','Bypass','-File',"`"$PSCommandPath`"") + `
                       ($MyInvocation.UnboundArguments)
    }
    try { Start-Process @psi } catch { Write-Warning 'Elevation cancelled.' }
    return
}

# ---------------------------------------------------------------------------
# Core operations
# ---------------------------------------------------------------------------
function Ensure-NullPort {
    if (-not (Get-PrinterPort -Name $script:PortName -ErrorAction SilentlyContinue)) {
        # LocalPort monitor; writes to a file literally named like the port -> harmless
        Add-PrinterPort -Name $script:PortName
    }
}

function Get-InstalledDrivers {
    # 'provider' (NiceLabel / Seagull / hp) distinguishes driver families better
    # than Manufacturer (often just "Altec" for both); fall back to Manufacturer.
    Get-PrinterDriver | ForEach-Object {
        $fam = if ($_.PSObject.Properties['provider'] -and $_.provider) { $_.provider }
               else { $_.Manufacturer }
        [pscustomobject]@{
            Name        = $_.Name
            Provider    = $fam
            Environment = $_.PrinterEnvironment
            Major       = $_.MajorVersion
        }
    } | Sort-Object Name
}

function Get-Phantoms {
    Get-Printer -ErrorAction SilentlyContinue |
        Where-Object { $_.Comment -eq $script:Tag } |
        Select-Object Name, DriverName, PortName
}

function New-Phantom {
    param([Parameter(Mandatory)][string]$PrinterName,
          [Parameter(Mandatory)][string]$DriverName)

    $existing = Get-Printer -Name $PrinterName -ErrorAction SilentlyContinue
    if ($existing) {
        if ($existing.Comment -eq $script:Tag) {
            throw "A phantom named '$PrinterName' already exists."
        }
        throw "'$PrinterName' already exists as a REAL printer. Refusing to touch it."
    }
    if (-not (Get-PrinterDriver -Name $DriverName -ErrorAction SilentlyContinue)) {
        throw "Driver '$DriverName' is not installed on this machine. Install it first."
    }
    Ensure-NullPort
    Add-Printer -Name $PrinterName -DriverName $DriverName -PortName $script:PortName -Comment $script:Tag
    "Created phantom '$PrinterName'  ->  $DriverName"
}

function Remove-AllPhantoms {
    $ph = Get-Phantoms
    if (-not $ph) { return 'No phantom printers to remove.' }
    $out = foreach ($p in $ph) {
        try { Remove-Printer -Name $p.Name; "Removed '$($p.Name)'" }
        catch { "FAILED to remove '$($p.Name)': $($_.Exception.Message)" }
    }
    # tidy the port if nothing else uses it
    if (-not (Get-Printer | Where-Object PortName -eq $script:PortName)) {
        try { Remove-PrinterPort -Name $script:PortName -ErrorAction SilentlyContinue } catch {}
    }
    $out
}

# ---------------------------------------------------------------------------
# CLI paths
# ---------------------------------------------------------------------------
if ($List)      { Get-Phantoms | Format-Table -AutoSize; return }
if ($RemoveAll) { Remove-AllPhantoms; return }
if ($Name -and $Driver) { New-Phantom -PrinterName $Name -DriverName $Driver; return }
if ($NoGui)     { Write-Host 'Nothing to do. Use -Name/-Driver, -List or -RemoveAll.'; return }

# ---------------------------------------------------------------------------
# GUI
# ---------------------------------------------------------------------------
Add-Type -AssemblyName PresentationFramework

[xml]$xaml = @"
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Phantom Printer" Height="460" Width="640"
        WindowStartupLocation="CenterScreen" ResizeMode="CanMinimize">
  <Grid Margin="12">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
      <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/><RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <StackPanel Grid.Row="0" Margin="0,0,0,8">
      <TextBlock Text="Printer name (paste exactly from the NiceLabel error):" FontWeight="Bold"/>
      <TextBox x:Name="TxtName" Height="26" Margin="0,4,0,0"/>
    </StackPanel>

    <StackPanel Grid.Row="1" Margin="0,0,0,8">
      <TextBlock Text="Driver:" FontWeight="Bold"/>
      <ComboBox x:Name="CboDriver" Height="26" Margin="0,4,0,0" IsEditable="False"/>
    </StackPanel>

    <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="0,0,0,8">
      <Button x:Name="BtnCreate" Content="Create phantom printer" Width="180" Height="30" Margin="0,0,8,0"/>
      <Button x:Name="BtnRemove" Content="Remove ALL phantoms"   Width="160" Height="30" Margin="0,0,8,0"/>
      <Button x:Name="BtnRefresh" Content="Refresh"              Width="80"  Height="30"/>
    </StackPanel>

    <TextBlock Grid.Row="3" Text="Current phantom printers:" FontWeight="Bold" Margin="0,4,0,4"/>
    <DataGrid Grid.Row="4" x:Name="GridPh" AutoGenerateColumns="False" IsReadOnly="True"
              HeadersVisibility="Column" GridLinesVisibility="Horizontal">
      <DataGrid.Columns>
        <DataGridTextColumn Header="Name"   Binding="{Binding Name}"       Width="2*"/>
        <DataGridTextColumn Header="Driver" Binding="{Binding DriverName}" Width="3*"/>
        <DataGridTextColumn Header="Port"   Binding="{Binding PortName}"   Width="*"/>
      </DataGrid.Columns>
    </DataGrid>

    <TextBox Grid.Row="5" x:Name="TxtLog" Height="90" Margin="0,8,0,0" IsReadOnly="True"
             VerticalScrollBarVisibility="Auto" TextWrapping="Wrap" FontFamily="Consolas" FontSize="11"/>
  </Grid>
</Window>
"@

$reader = New-Object System.Xml.XmlNodeReader $xaml
$win    = [Windows.Markup.XamlReader]::Load($reader)

$TxtName   = $win.FindName('TxtName')
$CboDriver = $win.FindName('CboDriver')
$BtnCreate = $win.FindName('BtnCreate')
$BtnRemove = $win.FindName('BtnRemove')
$BtnRefresh= $win.FindName('BtnRefresh')
$GridPh    = $win.FindName('GridPh')
$TxtLog    = $win.FindName('TxtLog')

function Log($m) {
    $TxtLog.AppendText(('[{0:HH:mm:ss}] {1}{2}' -f (Get-Date), $m, [Environment]::NewLine))
    $TxtLog.ScrollToEnd()
}
function Refresh {
    $CboDriver.Items.Clear()
    foreach ($d in Get-InstalledDrivers) {
        $CboDriver.Items.Add(('{0}   [{1}, v{2}, {3}]' -f $d.Name, $d.Provider, $d.Major, $d.Environment)) | Out-Null
    }
    $GridPh.ItemsSource = @(Get-Phantoms)
}
function Selected-DriverName {
    if ($CboDriver.SelectedItem) { ($CboDriver.SelectedItem -split '   \[')[0].Trim() }
}

$BtnRefresh.Add_Click({ Refresh; Log 'Refreshed.' })
$BtnCreate.Add_Click({
    $n = $TxtName.Text.Trim()
    $d = Selected-DriverName
    if (-not $n) { Log 'Enter a printer name.'; return }
    if (-not $d) { Log 'Pick a driver.'; return }
    try { Log (New-Phantom -PrinterName $n -DriverName $d); Refresh }
    catch { Log ("ERROR: " + $_.Exception.Message) }
})
$BtnRemove.Add_Click({
    if ([Windows.MessageBox]::Show('Remove every phantom printer this tool created?',
        'Confirm', 'YesNo', 'Question') -ne 'Yes') { return }
    try { Remove-AllPhantoms | ForEach-Object { Log $_ }; Refresh }
    catch { Log ("ERROR: " + $_.Exception.Message) }
})

Refresh
Log 'Ready. Paste the printer name from the NiceLabel error, choose a driver, Create.'
$win.ShowDialog() | Out-Null
