function Get-AllItems {
	function Drill($item) {
		foreach ($child in $item.Children) {
			Drill $child
		}
		$item
	}
	Drill $root
}

function Find-By {
<#
.SYNOPSIS
	Recursively finds an element contained in the visual tree matched using a predicate.
.PARAMETER predicate
	The script block which filters on items.
.PARAMETER select
	If enabled, selects the first match.
#>
	param([parameter(mandatory=$true)] [scriptblock] $predicate, [switch] $select)
	foreach ($i in (Get-AllItems | ? $predicate)) {
		$i
		if ($select) {
			$i.IsSelected = $true
			break
		}
	}
}

function Find-ByName {
<#
.SYNOPSIS
	Recursively finds an element contained in the visual tree matched by name.
.PARAMETER name
	The regular expression to match on the element's x:Name.
.PARAMETER select
	If enabled, selects the first match.
#>
	param([parameter(mandatory=$true)] [string] $name, [switch] $select)
	Find-By { $_.Target.Name -match $name } -select:$select
}

<#
.SYNOPSIS
	Recursively finds an element contained in the visual tree matched by name.
.PARAMETER type
	The regular expression to match on the element's type.
.PARAMETER select
	If enabled, selects the first match.
#>
function Find-ByType {
	param([parameter(mandatory=$true)] [string] $type, [switch]$select)
	Find-By { $_.Target.GetType().Name -match $type } -select:$select
}

<#
.SYNOPSIS
	Gets the currently selected tree item's data context.
#>
function Get-SelectedDataContext {
	$selected.Target.DataContext
}

Export-ModuleMember Find-By,Find-ByType,Find-ByName,Get-SelectedDataContext