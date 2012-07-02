new-psdrive tree treescriptprovider -root / -moduleinfo $(new-module -name tree {

$items = @{}
$reverse = @{}

function Get-TreeItem([string]$path) {
    if ($items.Count -eq 0) {
        function recurse($element, [string]$p) {
            $names = @{}
            foreach ($c in $element.Children) {
                $n = $c.Target.GetType().Name
                $names[$n]++
            }
            foreach ($c in $element.Children) {
                $n = $c.Target.GetType().Name
                if ($names[$n] -gt 1) {
                    recurse $c ($p + $n + $names[$n] + '\')
                } else {
                    recurse $c ($p + $n + '\')
                }
            }
        }
        recurse $root $path
    }
    return $items[$path]
}

function Get-ValidPath([string]$path) {
    $path = $path.Replace('/','\')
    if (-not $path.EndsWith('\')) {
        $path += '\'
    }
    return $path
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

    $path = Get-ValidPath $path
    $item = Get-TreeItem $path
    if ($item) {
        foreach ($c in $item.Children) {
            $p = $reverse[$c]
            $psprovider.WriteItemObject((Split-Path -Leaf $p), $p, $true)
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
}
function GetItem {
    [cmdletbinding()]
    param(
		[string]$path
    )

    $psprovider.writewarning("GetItem:$path")
    $path = Get-ValidPath $path
    return (Get-TreeItem $path)
}
function HasChildItems {
    [cmdletbinding()]
	[outputtype('bool')]
    param(
		[string]$path
    )

    $psprovider.writewarning("HasChildItems:$path")
    $path = Get-ValidPath $path
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

    $psprovider.writewarning("IsItemContainer:$path")
    return $true
}
function IsValidPath {
    [cmdletbinding()]
	[outputtype('bool')]
    param(
		[string]$path
    )

    $psprovider.writewarning("IsValidPath:$path")
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

    $psprovider.writewarning("ItemExists:$path")
    $path = Get-ValidPath $path
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

    $psprovider.writewarning("RenameItem:$path")
    # ...
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