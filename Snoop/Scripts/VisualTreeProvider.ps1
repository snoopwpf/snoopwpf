ipmo -force (Join-Path (Split-Path $MyInvocation.MyCommand.Path) "PSProvider\PSProvider.psd1")

if (Test-Path tree:) {
    Remove-PSDrive tree
}

New-PSDrive tree TreeScriptProvider -root / -moduleinfo $(new-module -name tree {
    
    function Get-ValidPath([string]$path) {
        $path = $path.Replace('/','\')
        if (-not $path.EndsWith('\')) {
            $path += '\'
        }
        return $path
    }

    function Get-Path($treeItem) {
        $path = '\'
        $current = $treeItem
        while ($current.Parent) {
            $name = $current.Target.GetType().Name
            $path = "\$name" + $path
            $current = $current.Parent
        }
        return $path
    }

    function Get-TreeItem([string]$path) {
        $path = Get-ValidPath $path

        if ($path -eq '\') {
        return $root
        }

        $parts = $path.Split('\', [StringSplitOptions]::RemoveEmptyEntries)
        $current = $root
        $count = 0
        foreach ($part in $parts) {
            foreach ($c in $current.Children) {
                $name = $c.Target.GetType().Name
                if ($name -eq $part) {
                    $current = $c
                    $count++
                    break;
                }
            }
        }

        if ($count -eq $parts.Length) {
            return $current
        }

        return $null
    }

    function ClearItem {
        [cmdletbinding()]
        param(
            [string]$path
        )

        $psprovider.WriteWarning("Clear-Item is not supported.")
    }
    function CopyItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [string]$copyPath, 
            [bool]$recurse
        )

        $psprovider.WriteWarning("Copy-Item is not supported.")
    }
    function GetChildItems {
        [cmdletbinding()]
        param(
            [string]$path, 
            [bool]$recurse
        )

        $item = Get-TreeItem $path
        if ($item) {
            foreach ($c in $item.Children) {
                $p = Get-Path $c
                GetItem $p
            }
        } else {
            $psprovider.WriteWarning("$path was not found.")
        }
    }
    function GetChildNames {
        [cmdletbinding()]
        param(
            [string]$path, 
            [Management.Automation.ReturnContainers]$returnContainers
        )

        $psprovider.writewarning("GetChildNames:$path")
        $item = Get-TreeItem $path
        if ($item) {
            foreach ($c in $item.Children) {
                $psprovider.WriteItemObject($c.Target.GetType().Name, $p, $true)
            }
        } else {
            $psprovider.WriteWarning("$path was not found.")
        }
    }
    function GetItem {
        [cmdletbinding()]
        param(
            [string]$path
        )
        $item = Get-TreeItem $path
        $psprovider.WriteItemObject($item, $path, $true)
    }
    function HasChildItems {
        [cmdletbinding()]
        [outputtype('bool')]
        param(
            [string]$path
        )
        $item = Get-TreeItem $path
        return $item.Children.Count -gt 0
    }
    function InvokeDefaultAction {
        [cmdletbinding()]
        param(
            [string]$path
        )
        $psprovider.writewarning("InvokeDefaultAction:$path")
    }
    function IsItemContainer {
        [cmdletbinding()]
        [outputtype('bool')]
        param(
            [string]$path
        )
        return $true
    }
    function IsValidPath {
        [cmdletbinding()]
        [outputtype('bool')]
        param(
            [string]$path
        )

        $path = Get-ValidPath
        foreach ($c in $path) {
            if ($c -eq '/' -or $c -eq '\') {
                continue
            }
            if (-not [char]::IsLetter($c)) {
                return $false
            }
        }
        return $true
    }
    function ItemExists {
        [cmdletbinding()]
        [outputtype('bool')]
        param(
            [string]$path
        )
        return (Get-TreeItem $path)
    }
    function MoveItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [string]$destination
        )
        $psprovider.WriteWarning("Move-Item is not supported.")
    }
    function NewDrive {
        [cmdletbinding()]
        [outputtype('Management.Automation.PSDriveInfo')]
        param(
            [Management.Automation.PSDriveInfo]$drive
        )
        $psprovider.WriteWarning("New-Drive is not supported.")
    }
    function NewItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [string]$itemTypeName, 
            [Object]$newItemValue
        )
        $psprovider.WriteWarning("New-Item is not supported.")
    }
    function RemoveItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [bool]$recurse
        )
        $psprovider.WriteWarning("Remove-Item is not supported.")
    }
    function RenameItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [string]$newName
        )
        $psprovider.WriteWarning("Rename-Item is not supported.")
    }
    function SetItem {
        [cmdletbinding()]
        param(
            [string]$path, 
            [Object]$value
        )
        $psprovider.WriteWarning("Set-Item is not supported.")
    }
})
