param($installPath, $toolsPath, $package, $project)

	$nakedObjectsFolder = "Naked Objects"
	$vsVersions = @("2010")

	Function removeFiles($parent, $dir) {
		if (Test-Path $parent) {
			$destination = "$parent$dir"     
			if (Test-Path $destination) {
				"Uninstalling from $destination for Visual Studio $vsVersion"
				Remove-Item $destination -recurse -force
			}	       
		}	
	}


	Foreach ($vsVersion in $vsVersions){
		$docRoot = [System.Environment]::GetFolderPath("MyDocuments")

		$CSSnippetsFolder = "$docRoot\Visual Studio $vsVersion\Code Snippets\Visual C#\My Code Snippets\"
		$VBSnippetsFolder = "$docRoot\Visual Studio $vsVersion\Code Snippets\Visual Basic\My Code Snippets\"

		$CSItemsFolder = "$docRoot\Visual Studio $vsVersion\Templates\ItemTemplates\Visual C#\"
		$VBItemsFolder = "$docRoot\Visual Studio $vsVersion\Templates\ItemTemplates\Visual Basic\"

		removeFiles $CSSnippetsFolder $nakedObjectsFolder
		removeFiles $VBSnippetsFolder $nakedObjectsFolder
	    removeFiles $CSItemsFolder $nakedObjectsFolder
		removeFiles $VBItemsFolder $nakedObjectsFolder
	}