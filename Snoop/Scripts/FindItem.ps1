function Find-Item {
    param(
        [string] $name = '',
        [System.Type] $type = $null)

	if ($name -eq '' -and $type -eq $null) {
		throw "Either -name or -type must be specified."
	}

    function Recurse($item) {
        foreach ($child in $item.Children) {
            Recurse $child
        }

		$match = $false

		if (![string]::IsNullOrEmpty($name)) {
			if ($item.Target.Name -match $name) {
				$match = $true
			}
		}

		if ($type -ne $null) {
			$match = $item.Target.GetType() -eq $type
		}

		if ($match) {
			$item
		}
    }
    
	Recurse $root
}
