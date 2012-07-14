function Get-AllItems {
	function Drill($item) {
		foreach ($child in $item.Children) {
			Drill $child
		}
		$item
	}
	Drill $root
}

function Find-ByCondition {
	param([parameter(mandatory=$true)] [scriptblock] $condition)
	foreach ($i in (Get-AllItems | ? $condition)) {
		if ($select) {
			$i.IsSelected = $true
			break
		} else {
			$i
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
	If provided, selects the first match.
#>
	param([parameter(mandatory=$true)] [string] $name, [switch] $select)
	Find-ByCondition { $_.Target.Name -match $name }
}

function Find-ByType {
<#
.SYNOPSIS
	Recursively finds an element contained in the visual tree matched by name.
.PARAMETER type
	The regular expression to match on the element's type.
.PARAMETER select
	If provided, selects the first match.
#>
	param([parameter(mandatory=$true)] [string] $type, [switch]$select)
	Find-ByCondition { $_.Target.GetType().Name -match $type }
}

Export-ModuleMember Find-ByType,Find-ByName