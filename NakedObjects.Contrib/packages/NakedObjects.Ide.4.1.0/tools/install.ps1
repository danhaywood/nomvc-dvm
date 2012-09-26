param($installPath, $toolsPath, $package, $project)
	
	$nakedObjectsFolder = "Naked Objects"
	$CSSnippets = "$toolsPath\C#\*.snippet"
	$VBSnippets = "$toolsPath\VB\*.snippet"

	$CSTemplates = "$toolsPath\C#\*.zip"
	$VBTemplates = "$toolsPath\VB\*.zip"
	$vsVersions = @("2010")

	Function copyFiles ($toParent, $toDir,  $files) {

		if (Test-Path $toParent) {
			$destination = "$toParent$toDir"
			
			if (!(Test-Path $destination)) {			
				New-Item $destination -itemType "directory"	
			}	

			"Installing to $destination for Visual Studio $vsVersion"
			Copy-Item $files $destination			
		}		
	}


	Foreach ($vsVersion in $vsVersions) {
		
		$docRoot = [System.Environment]::GetFolderPath("MyDocuments")

		$CSSnippetsFolder = "$docRoot\Visual Studio $vsVersion\Code Snippets\Visual C#\My Code Snippets\"
		$VBSnippetsFolder = "$docRoot\Visual Studio $vsVersion\Code Snippets\Visual Basic\My Code Snippets\"

		$CSItemsFolder = "$docRoot\Visual Studio $vsVersion\Templates\ItemTemplates\Visual C#\"
		$VBItemsFolder = "$docRoot\Visual Studio $vsVersion\Templates\ItemTemplates\Visual Basic\"

		copyFiles $CSSnippetsFolder $nakedObjectsFolder $CSSnippets
		copyFiles $VBSnippetsFolder $nakedObjectsFolder $VBSnippets
	    copyFiles $CSItemsFolder $nakedObjectsFolder $CSTemplates
		copyFiles $VBItemsFolder $nakedObjectsFolder $VBTemplates
	}