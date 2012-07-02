new-psdrive tree treescriptprovider -root / -moduleinfo $(new-module -name tree {

$items = @{}

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

    $psprovider.WriteWarning("Clear-Item is not supported.")
}
function GetChildItems {
    [cmdletbinding()]
    param(
		[string]$path, 
		[bool]$recurse
    )

    $psprovider.writewarning("GetChildItems:$path")
    # ...
}
function GetChildNames {
    [cmdletbinding()]
    param(
		[string]$path, 
		[Management.Automation.ReturnContainers]$returnContainers
    )

    $psprovider.writewarning("GetChildNames:$path")
    # ...
}
function GetItem {
    [cmdletbinding()]
    param(
		[string]$path
    )

    $psprovider.writewarning("GetItem:$path")
    # ...
}
function HasChildItems {
    [cmdletbinding()]
	[outputtype('bool')]
    param(
		[string]$path
    )

    $psprovider.writewarning("HasChildItems:$path")
    # ...
}
function InvokeDefaultAction {
    [cmdletbinding()]
    param(
		[string]$path
    )

    $psprovider.writewarning("InvokeDefaultAction:$path")
    # ...
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
    # ...
}
function ItemExists {
    [cmdletbinding()]
	[outputtype('bool')]
    param(
		[string]$path
    )

    $psprovider.writewarning("ItemExists:$path")
    return $true
}
function MoveItem {
    [cmdletbinding()]
    param(
		[string]$path, 
		[string]$destination
    )

    $psprovider.writewarning("MoveItem:$path -> $destination")
    # ...
}
function NewDrive {
    [cmdletbinding()]
	[outputtype('Management.Automation.PSDriveInfo')]
    param(
		[Management.Automation.PSDriveInfo]$drive
    )

    $psprovider.writewarning("NewDrive")
    # ...
}
function NewItem {
    [cmdletbinding()]
    param(
		[string]$path, 
		[string]$itemTypeName, 
		[Object]$newItemValue
    )

    $psprovider.writewarning("NewItem:$path")
    # ...
}
function RemoveItem {
    [cmdletbinding()]
    param(
		[string]$path, 
		[bool]$recurse
    )

    $psprovider.writewarning("RemoveItem:$path")
    # ...
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

    $psprovider.writewarning("SetItem:$path")
    # ...
}
})