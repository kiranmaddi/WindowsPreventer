Set-StrictMode -Version Latest

function Test-WindowsPreventerAdministrator {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Sid
    )

    $administrators = Get-LocalGroupMember -Group 'Administrators' -ErrorAction Stop

    foreach ($administrator in $administrators) {
        if ($administrator.SID.Value -eq $Sid) {
            return $true
        }
    }

    return $false
}

function Resolve-WindowsPreventerAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $UserName
    )

    $account = New-Object System.Security.Principal.NTAccount($UserName)
    $sid = $account.Translate([System.Security.Principal.SecurityIdentifier]).Value

    [pscustomobject]@{
        UserName = $UserName
        Sid = $sid
        HivePath = "Registry::HKEY_USERS\$sid"
    }
}

function Test-WindowsPreventerElevated {
    [CmdletBinding()]
    param()

    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object System.Security.Principal.WindowsPrincipal($identity)

    return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-WindowsPreventerLoadedUserAccount {
    [CmdletBinding()]
    param()

    $loadedSids = [Microsoft.Win32.Registry]::Users.GetSubKeyNames() |
        Where-Object { $_ -match '^S-1-5-21-.+' -and $_ -notmatch '_Classes$' }

    foreach ($sid in $loadedSids) {
        try {
            $securityIdentifier = New-Object System.Security.Principal.SecurityIdentifier($sid)
            $userName = $securityIdentifier.Translate([System.Security.Principal.NTAccount]).Value

            [pscustomobject]@{
                UserName = $userName
                Sid = $sid
                HivePath = "Registry::HKEY_USERS\$sid"
            }
        } catch {
            Write-Warning "Skipping loaded profile $sid because it could not be resolved to an account."
        }
    }
}

function Get-WindowsPreventerPolicyValues {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [pscustomobject] $Controls
    )

    $values = New-Object System.Collections.Generic.List[object]

    if ($Controls.disableRightClick) {
        $values.Add([pscustomobject]@{
            Path = 'Software\Microsoft\Windows\CurrentVersion\Policies\Explorer'
            Name = 'NoViewContextMenu'
            Type = 'DWord'
            Value = 1
            Scope = 'User'
        })
    }

    if ($Controls.hideRestartAndShutdown) {
        $values.Add([pscustomobject]@{
            Path = 'Software\Microsoft\Windows\CurrentVersion\Policies\Explorer'
            Name = 'NoClose'
            Type = 'DWord'
            Value = 1
            Scope = 'User'
        })
    }

    if ($Controls.disableClipboardHistory) {
        $values.Add([pscustomobject]@{
            Path = 'SOFTWARE\Policies\Microsoft\Windows\System'
            Name = 'AllowClipboardHistory'
            Type = 'DWord'
            Value = 0
            Scope = 'Machine'
        })

        $values.Add([pscustomobject]@{
            Path = 'SOFTWARE\Policies\Microsoft\Windows\System'
            Name = 'AllowCrossDeviceClipboard'
            Type = 'DWord'
            Value = 0
            Scope = 'Machine'
        })
    }

    if ($Controls.disableCopyPaste) {
        throw 'disableCopyPaste is not supported in Phase 1. Use kiosk mode, app-level controls, or a managed endpoint agent design.'
    }

    return $values
}

function Set-WindowsPreventerRegistryValue {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [ValidateSet('DWord')]
        [string] $Type,

        [Parameter(Mandatory)]
        [int] $Value
    )

    if ($PSCmdlet.ShouldProcess("$Path\$Name", "Set $Type value to $Value")) {
        if (-not (Test-Path -Path $Path)) {
            New-Item -Path $Path -Force | Out-Null
        }

        New-ItemProperty -Path $Path -Name $Name -PropertyType $Type -Value $Value -Force | Out-Null
    }
}

function Remove-WindowsPreventerRegistryValue {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [Parameter(Mandatory)]
        [string] $Name
    )

    if ($PSCmdlet.ShouldProcess("$Path\$Name", 'Remove policy value')) {
        if (Test-Path -Path $Path) {
            Remove-ItemProperty -Path $Path -Name $Name -ErrorAction SilentlyContinue
        }
    }
}

function Invoke-WindowsPreventerPolicy {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string] $ConfigPath,

        [ValidateSet('Apply', 'Remove')]
        [string] $Mode = 'Apply'
    )

    if (-not (Test-Path -Path $ConfigPath)) {
        throw "Config file was not found: $ConfigPath"
    }

    $config = Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json
    $policyValues = Get-WindowsPreventerPolicyValues -Controls $config.controls
    $targetAccounts = New-Object System.Collections.Generic.List[object]
    $targetUsers = @($config.targetUsers)

    if (-not $WhatIfPreference -and -not (Test-WindowsPreventerElevated)) {
        throw 'Apply and remove operations must be run from an elevated PowerShell session. Re-run with -WhatIf to preview without elevation.'
    }

    if ($targetUsers.Count -eq 0) {
        foreach ($loadedAccount in Get-WindowsPreventerLoadedUserAccount) {
            $targetAccounts.Add($loadedAccount)
        }
    } else {
        foreach ($targetUser in $targetUsers) {
            $targetAccounts.Add((Resolve-WindowsPreventerAccount -UserName $targetUser))
        }
    }

    foreach ($policyValue in $policyValues | Where-Object { $_.Scope -eq 'Machine' }) {
        $registryPath = "Registry::HKEY_LOCAL_MACHINE\$($policyValue.Path)"

        if ($Mode -eq 'Apply') {
            Set-WindowsPreventerRegistryValue -Path $registryPath -Name $policyValue.Name -Type $policyValue.Type -Value $policyValue.Value -WhatIf:$WhatIfPreference
        } else {
            Remove-WindowsPreventerRegistryValue -Path $registryPath -Name $policyValue.Name -WhatIf:$WhatIfPreference
        }
    }

    foreach ($account in $targetAccounts) {
        if ($config.exemptAdministrators -and (Test-WindowsPreventerAdministrator -Sid $account.Sid)) {
            Write-Information "Skipping administrator account: $($account.UserName)" -InformationAction Continue
            continue
        }

        if (-not (Test-Path -Path $account.HivePath)) {
            Write-Warning "Skipping $($account.UserName) because $($account.HivePath) is not loaded. Sign in as that user or load NTUSER.DAT before applying per-user policies."
            continue
        }

        foreach ($policyValue in $policyValues | Where-Object { $_.Scope -eq 'User' }) {
            $registryPath = Join-Path -Path $account.HivePath -ChildPath $policyValue.Path

            if ($Mode -eq 'Apply') {
                Set-WindowsPreventerRegistryValue -Path $registryPath -Name $policyValue.Name -Type $policyValue.Type -Value $policyValue.Value -WhatIf:$WhatIfPreference
            } else {
                Remove-WindowsPreventerRegistryValue -Path $registryPath -Name $policyValue.Name -WhatIf:$WhatIfPreference
            }
        }
    }
}