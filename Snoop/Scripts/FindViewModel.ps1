function Find-ViewModel {
    param([System.Type] $type = $(throw "type is required"))

    function Recurse($item) {
        foreach ($child in $item.Children) {
            Recurse $child
        }

		if ($item.Target.GetType() -eq $type) {
			$item.Target.DataContext
		}
    }
    
	Recurse $root
}
