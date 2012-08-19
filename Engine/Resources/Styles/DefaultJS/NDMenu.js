﻿/*
	Include in output:

	This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
	Natural Docs is licensed under version 3 of the GNU Affero General Public
	License (AGPL).  Refer to License.txt or www.naturaldocs.org for the
	complete details.

	This file may be distributed with documentation files generated by Natural Docs.
	Such documentation is not covered by Natural Docs' copyright and licensing,
	and may have its own copyright and distribution terms as decided by its author.

	
	Substitutions:

		Entry Types:

		`RootFolder = 0
		`LocalFolder = 1
		`DynamicFolder = 2
		`ExplicitFile = 3
		`ImplicitFile = 4

		Member Indexes:

		`Type = 0
		`ID = 1
		`Name = 1
		`HashPath = 2
		`Members = 3

		Constants:

		`MaxFileMenuSections = 10
*/

"use strict";


/* Class: NDMenu
	___________________________________________________________________________

	Loading Process:

		- When <OnLocationChange()> or <GoToFileOffsets()> need the menu to be updated, they call <Build()> with the path to 
		  the current selection.
		
		- If all the data needed exists in <fileMenuSections> a new menu is generated.

		- If some of the data is missing <Build()> displays what it can, stores the selection path in <pathBeingBuilt>, requests to 
		  load additional menu sections, and returns.  When the data comes back via <OnFileMenuSectionLoaded()> that function
		  will call <Build()> again.  <Build()> will either finish the update, request more parts of the menu, or just wait for previously
		  requested data to come in.

		- This system allows the user to click a different file before everything finishes loading.  <Build()> will be called again and
		  <pathBeingBuilt> will be replaced or set back to undefined.  If the previous click's data comes in after the new navigation 
		  has been completed, <Build()> won't do anything because it cleared <pathBeingBuilt> already.  If the previous click's data
		  comes back but isn't relevant to the new click which is still loading, <Build()> will just rebuild the partial menu with the new
		  click's path to no detrimental effect.

	Caching:

		The class stores loaded sections in <fileMenuSections>, hanging on to them until it grows to `MaxFileMenuSections, at which
		point unused entries may be pruned.  The array may grow larger than `MaxFileMenuSections if required to display everything.

		Which entries are in use is tracked by the <firstUnusedFileMenuSection> index.  This is reset when <Build()> starts, and every
		call to <GetFileMenuSection()> rearranges the array and advances it so that it serves as a dividing line.  If the path being built
		is walked start to finish, everything in use will be in indexes below <firstUnusedFileMenuSection>.

		The array is rearranged so that it gets put in MRU order.  When the array grows too large unused entries can be plucked off the 
		back end and the least recently used ones will go first.

		There's another trick to it though.  When <GetFileMenuSection()> returns an entry that was past <firstUnusedFileMenuSection>,
		it doesn't move it to the head of the array, it moves it to the back of the used list.  Why?  Because the paths are walked from
		root to leaf, meaning if you had path A > B > C > D and you just moved entries to the head, they would end up in the cache in 
		this order:

		> [ D, C, B, A ]

		So then let's say you navigated from that to A > B > C2 > D2.  The cache now looks like this, with | representing the 
		used/unused divider:

		> [ D2, C2, B, A | D, C ]

		C would get ejected from the cache before D, but since D is below C in the hierarchy its presence in the cache is not especially
		useful.  So instead we move entries to the end of the used list, which keeps them in their proper order.  Going to A > B > C > D 
		results in this cache:

		> [ A, B, C, D ]

		and then navigating to A > B > C2 > D2 results in this cache:

		> [ A, B, C2, D2 | C, D ]

		D would get ejected before C.  We now have a MRU ordering that also implicitly favors higher hierarchy entries instead of lower
		ones which aren't valuable without their parents.

*/
var NDMenu = new function ()
	{ 

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Start
	*/
	this.Start = function ()
		{
		// this.pathBeingBuilt = undefined;

		this.fileMenuSections = [ ];
		this.firstUnusedFileMenuSection = 0;
		};


	/* Function: OnLocationChange

		Called by <NDFramePage> whenever the location hash changes.

		Parameters:

			oldLocation - The <NDLocation> we were previously at, or undefined if there is none because this is the
							   first call after the page is loaded.
			newLocation - The <NDLocation> we are navigating to.
	*/
	this.OnLocationChange = function (oldLocation, newLocation)
		{
		if (newLocation.type == "File")
			{
			// It's possible we've navigated to a different member of the same file, so check to see if we need to do anything.
			if (oldLocation == undefined || oldLocation.type != "File" || oldLocation.path != newLocation.path)
				{
				this.Build( new NDFileMenuHashPath(newLocation.path) );
				}
			}

		// Any non-file path goes to the root menu.  Therefore we only need to rebuild the menu if we're displaying it
		// for the very first time or we're navigating away from a file path.  Switching between two non-file paths doesn't
		// require an update.
		else if (oldLocation == undefined || oldLocation.type == "File")
			{
			this.Build( new NDFileMenuOffsetPath() );
			}
		};


	/* Function: GoToFileOffsets
		Changes the current page in the file menu to the passed array of offsets, which should be in the format used by
		<NDFileMenuOffsetPath>.
	*/
	this.GoToFileOffsets = function (offsets)
		{
		this.Build( new NDFileMenuOffsetPath(offsets) );
		};


	/* Function: Build

		Generates the HTML for the menu.
		
		Parameters:
		
			path - The path to the selected file or folder.  Can be set to a <NDFileMenuOffsetPath> or <NDFileMenuHashPath>.
					  If there's no selection, supply an empty path object.

					  If the previous attempt to build the menu only partially completed because there was not enough data
					  loaded, calling this again with path undefined will make another attempt at building it.
	*/
	this.Build = function (path)
		{
		if (path != undefined)
			{  this.pathBeingBuilt = path;  }

		// This needs to be checked because the user may navigate somewhere that requires another menu section to be
		// loaded, navigate somewhere else before it finishes, and then the menu data comes in.  Build() would be called
		// even though the menu is complete.
		if (this.pathBeingBuilt == undefined)
			{  return;  }

		// Reset.  Calls to GetFileMenuSection() will recalculate this.
		this.firstUnusedFileMenuSection = 0;

		var newMenuContent = document.createElement("div");
		newMenuContent.id = "MContent";

		var result = this.BuildEntries(newMenuContent, this.pathBeingBuilt);

		if (!result.completed)
			{
			var htmlEntry = document.createElement("div");
			htmlEntry.className = "MLoadingNotice";
			newMenuContent.appendChild(htmlEntry);
			}

		var menuContainer = document.getElementById("NDMenu");
		var oldMenuContent = document.getElementById("MContent");

		if (oldMenuContent != undefined)
			{  menuContainer.replaceChild(newMenuContent, oldMenuContent);  }
		else
			{  menuContainer.appendChild(newMenuContent);  }

		if (result.completed)
			{
			if (result.selectedFile)
				{  
				// Scroll the selected file into view.  Check if it's necessary first because scrollIntoView() may change the scroll 
				// position even if the element is already visible and we don't want it to be jumpy.

				// If the element's bottom is lower than the extent of the visible menu...
				if (result.selectedFile.offsetTop + result.selectedFile.offsetHeight > menuContainer.scrollTop + menuContainer.clientHeight)
					{  result.selectedFile.scrollIntoView(false);  }

				// If the element's top is higher than the extent of the visible menu...
				else if (result.selectedFile.offsetTop < menuContainer.scrollTop)
					{  result.selectedFile.scrollIntoView(true);  }
				}


			this.pathBeingBuilt = undefined;
			this.CleanUpFileMenuSections();
			}
		else if (result.needToLoad != undefined)
			{  this.LoadFileMenuSection(result.needToLoad);  }
		};

	
	/* Function: BuildEntries

		Generates the HTML menu entries and appends them to the passed element.
		
		Returns:
		
			Returns an object with the following members:

			completed - Bool.  Whether it was able to build the complete menu as opposed to just part.
			needToLoad - The ID of the menu section that needs to be loaded, or undefined if none.
			selectedFile - The DOM element of the selected file, or undefined if none.

	*/
	this.BuildEntries = function (htmlMenu, path)
		{
		var result = 
			{ 
			completed: false
			// needToLoad: undefined
			// selectedFile: undefined
			};

		var pathSoFar = [ ];
		var iterator = path.GetIterator();
		var navigationType;


		// Generate the list of folders up to and including the selected one.

		for (;;)
			{	
			navigationType = iterator.NavigationType();

			if (navigationType == `Nav_RootFolder)
				{  
				var htmlEntry = document.createElement("a");
				htmlEntry.className = "MEntry MFolder Parent Root";
				htmlEntry.setAttribute("href", "javascript:NDMenu.GoToFileOffsets([])");

				var name = document.createTextNode(`Locale{HTML.RootFolderName});
				htmlEntry.appendChild(name);

				htmlMenu.appendChild(htmlEntry);
				iterator.Next();  
				}

			else if (navigationType == `Nav_SelectedRootFolder)
				{  
				var htmlEntry = document.createElement("div");
				htmlEntry.className = "MEntry MFolder Parent Root Selected";

				var name = document.createTextNode(`Locale{HTML.RootFolderName});
				htmlEntry.appendChild(name);

				htmlMenu.appendChild(htmlEntry);
				break;  
				}
			
			else if (navigationType == `Nav_ParentFolder || navigationType == `Nav_SelectedParentFolder)
				{
				pathSoFar.push(iterator.offsetFromParent);

				var name;

				// If there's multiple names, create all but the last as empty parent folders

				if (typeof(iterator.currentEntry[`Name]) == "object")
					{
					for (var i = 0; i < iterator.currentEntry[`Name].length - 1; i++)
						{
						var htmlEntry = document.createElement("div");
						htmlEntry.className = "MEntry MFolder Parent Empty";
						htmlEntry.innerHTML = iterator.currentEntry[`Name][i];

						htmlMenu.appendChild(htmlEntry);
						}

					name = iterator.currentEntry[`Name][ iterator.currentEntry[`Name].length - 1 ];
					}
				else
					{  name = iterator.currentEntry[`Name];  }

				if (navigationType == `Nav_SelectedParentFolder)
					{
					var htmlEntry = document.createElement("div");
					htmlEntry.className = "MEntry MFolder Selected";
					htmlEntry.innerHTML = name;

					htmlMenu.appendChild(htmlEntry);
					break;
					}
				else
					{
					var htmlEntry = document.createElement("a");
					htmlEntry.className = "MEntry MFolder Parent";
					htmlEntry.setAttribute("href", "javascript:NDMenu.GoToFileOffsets([" + pathSoFar.join(",") + "])");
					htmlEntry.innerHTML = name;

					htmlMenu.appendChild(htmlEntry);
					iterator.Next();
					}
				}

			else if (navigationType == `Nav_NeedToLoad)
				{  
				result.needToLoad = iterator.needToLoad;  
				return result;
				}

			else
				{  throw "Unexpected navigation type " + navigationType;  }
			}


		// Generate the list of files in the selected folder

		var selectedFolder = iterator.currentEntry;

		if (selectedFolder[`Type] == `DynamicFolder)
			{
			var membersID = selectedFolder[`Members];
			selectedFolder = this.GetFileMenuSection(membersID);  

			if (selectedFolder == undefined)
				{  
				result.needToLoad = membersID;
				return result;
				}
			}
		
		var selectedFileIndex = -1;

		if (iterator.Next())
			{  selectedFileIndex = iterator.offsetFromParent;  }

		for (var i = 0; i < selectedFolder[`Members].length; i++)
			{
			var member = selectedFolder[`Members][i];

			if (member[`Type] == `ImplicitFile || member[`Type] == `ExplicitFile)
				{
				if (i == selectedFileIndex)
					{
					var htmlEntry = document.createElement("div");
					htmlEntry.className = "MEntry MFile Selected";
					htmlEntry.innerHTML = member[`Name];

					htmlMenu.appendChild(htmlEntry);
					result.selectedFile = htmlEntry;
					}
				else
					{
					var hashPath = selectedFolder[`HashPath];

					if (member[`Type] == `ImplicitFile)
						{  hashPath += member[`Name];  }
					else
						{  hashPath += member[`HashPath];  }

					var htmlEntry = document.createElement("a");
					htmlEntry.className = "MEntry MFile";
					htmlEntry.setAttribute("href", "#" + hashPath);
					htmlEntry.innerHTML = member[`Name];

					htmlMenu.appendChild(htmlEntry);
					}
				}
			else  // `InlineFolder, `DynamicFolder.  `RootFolder won't be in a member list.
				{
				var name = "<div class=\"MFolderIcon\"></div>";
				
				if (typeof(member[`Name]) == "object")
					{  name += member[`Name][0];  }
				else
					{  name += member[`Name];  }

				var targetPath = (pathSoFar.length == 0 ? i : pathSoFar.join(",") + "," + i);

				var htmlEntry = document.createElement("a");
				htmlEntry.className = "MEntry MFolder Child";
				htmlEntry.setAttribute("href", "javascript:NDMenu.GoToFileOffsets([" + targetPath + "])");
				htmlEntry.innerHTML = name;

				htmlMenu.appendChild(htmlEntry);
				}
			}

		result.completed = true;
		return result;
		};

	

	// Group: Support Functions
	// ________________________________________________________________________


	/* Function: GetFileMenuSection
		Returns the file root folder with the passed number, or undefined if it isn't loaded yet.
	*/
	this.GetFileMenuSection = function (id)
		{
		for (var i = 0; i < this.fileMenuSections.length; i++)
			{
			if (this.fileMenuSections[i].ID == id)
				{
				var section = this.fileMenuSections[i];

				// Move to the end of the used sections list.  It doesn't matter if it's ready.
				if (i >= this.firstUnusedFileMenuSection)
					{
					if (i > this.firstUnusedFileMenuSection)
						{
						this.fileMenuSections.splice(i, 1);
						this.fileMenuSections.splice(this.firstUnusedFileMenuSection, 0, section);
						}

					this.firstUnusedFileMenuSection++;
					}

				if (section.Ready == true)
					{  return section.RootFolder;  }
				else
					{  return undefined;  }
				}
			}

		return undefined;
		};


	/* Function: LoadFileMenuSection
		Starts loading the file root folder with the passed ID if it isn't already loaded or in the process of loading.
	*/
	this.LoadFileMenuSection = function (id)
		{
		for (var i = 0; i < this.fileMenuSections.length; i++)
			{
			if (this.fileMenuSections[i].ID == id)
				{  
				// If it has an entry, it's either already loaded or in the process of loading.
				return;
				}
			}

		// If we're here, there was no entry for it.
		this.fileMenuSections.push({
			ID: id,
			RootFolder: undefined,
			Ready: false
			});

		var script = document.createElement("script");
		script.src = "menu/files" + (id == 1 ? "" : id) + ".js";
		script.type = "text/javascript";
		script.id = "NDFileMenuLoader" + id;

		document.getElementsByTagName("head")[0].appendChild(script);
		};


	/* Function: OnFileMenuSectionLoaded
		Called by the menu data file when it has finished loading, passing its contents as a parameter.
	*/
	this.OnFileMenuSectionLoaded = function (id, rootFolder)
		{
		for (var i = 0; i < this.fileMenuSections.length; i++)
			{
			if (this.fileMenuSections[i].ID == id)
				{
				this.fileMenuSections[i].RootFolder = rootFolder;
				this.fileMenuSections[i].Ready = true;
				break;
				}
			}

		//	To simulate latency for testing, replace this line with setTimeout("NDMenu.Build()", 1500);
		this.Build();
		};


	/* Function: CleanUpFileMenuSections
		Goes through <fileMenuSections> and if there's more than `MaxFileMenuSections, removes the least recently accessed
		entries that aren't being used.
	*/
	this.CleanUpFileMenuSections = function ()
		{
		if (this.fileMenuSections.length > `MaxFileMenuSections)
			{
			var head = document.getElementsByTagName("head")[0];

			for (var i = this.fileMenuSections.length - 1; 
				  i >= this.firstUnusedFileMenuSection && this.fileMenuSections.length > `MaxFileMenuSections; 
				  i--)
				{
				// We don't want to remove an entry if data's being loaded for it.  The event handler could reasonably expect it 
				// to exist.
				if (this.fileMenuSections[i].Ready == false)
					{  break;  }

				// Remove the loader tag too so it can be recreated if we ever need this section again.
				head.removeChild(document.getElementById("NDFileMenuLoader" + this.fileMenuSections[i].ID));

				this.fileMenuSections.pop();
				}
			}
		};



	// Group: Variables
	// ________________________________________________________________________


	/* var: pathBeingBuilt
		If we're attempting to update the menu, this will be an object that represents the navigation path from the root folder 
		to the selected folder or file.  It can be either a <NDFileMenuOffsetPath> or <NDFileMenuHashPath>.  Once the menu
		has been completely built this will return to undefined.
	*/

	/* var: fileMenuSections
		An array of <NDMenuSections> that have been loaded for the file menu or are in the process of being loaded.
		The array is ordered from the most recently accessed to the least.
	*/

	/* var: firstUnusedFileMenuSection
		An index into <fileMenuSections> of the first entry that was not accessed via <GetFileMenuSection()> in the
		last call to <Build()>.
	*/

	};






/* Class: NDMenuSection
	___________________________________________________________________________

	An object representing part of the menu structure.

		var: ID
		The root folder ID number.

		var: RootFolder
		A reference to the root folder entry itself.

		var: Ready
		True if the data has been loaded and is ready to use.  False if the data has been
		requested but is not ready yet.  If the data has not been requested it simply would
		not have a MenuSection object for it.

*/


/* Class: NDFileMenuOffsetPath
	___________________________________________________________________________

	A path through <NDMenu's> hierarchy using array offsets, which is more efficient than using folder names.
	This has the same interface as <NDFileMenuHashPath> so they can be used interchangeably.

	You can pass an array of file offsets to the constructor, or leave it undefined to refer to the root folder.

*/
function NDFileMenuOffsetPath (offsets)
	{

	// Group: Functions
	// ________________________________________________________________________

	/* Function: GetIterator
		Creates and returns a new <iterator: NDFileMenuOffsetIterator> positioned at the beginning of the path.
	*/
	this.GetIterator = function ()
		{
		return new NDFileMenuOffsetIterator(this);
		};


	// Group: Properties
	// ________________________________________________________________________

	/* Property: path
		An array of offsets.  An empty array means the root folder is selected.  A first entry would be the index of the root folder 
		member selected, and further entries would be indexes into subfolders.  If the last entry points to a folder, it means that 
		folder is selected but not a file within it.  If it points to a file, it means that file is selected.
	*/
	if (offsets == undefined)
		{  this.path = [ ];  }
	else
		{  this.path = offsets;  }

	};


/* Class: NDFileMenuOffsetIterator
	___________________________________________________________________________

	A class that can walk through <NDFileMenuOffsetPath>.

	Limitations:

		- The iterator can only walk forward.  Walking backwards wasn't needed when it was written so it doesn't exist.
		- The iterator must be recreated when another section of the menu is loaded.  An iterator created before the load is not
		   guaranteed to notice the new data.

*/
function NDFileMenuOffsetIterator (pathObject)
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Next
		Moves the iterator forward one place in the path, returning whether it's still in bounds.
	*/
	this.Next = function ()
		{
		// If we're past the end of the path...
		if (this.nextIndex > this.pathObject.path.length)
			{
			this.nextIndex++;
			this.currentEntry = undefined;
			this.offsetFromParent = -1;
			}

		// If we're in the path but past what's loaded...
		else if (this.currentEntry == undefined)
			{
			this.offsetFromParent = this.pathObject.path[this.nextIndex];
			this.nextIndex++;
			}

		// If we're moving into a folder with local members...
		else if (this.currentEntry[`Type] == `LocalFolder ||
				  this.currentEntry[`Type] == `RootFolder)
			{
			this.offsetFromParent = this.pathObject.path[this.nextIndex];
			this.currentEntry = this.currentEntry[`Members][this.offsetFromParent];
			this.nextIndex++;
			}

		// If we're moving into a folder with dynamic members...
		else if (this.currentEntry[`Type] == `DynamicFolder)
			{
			this.offsetFromParent = this.pathObject.path[this.nextIndex];

			var membersID = this.currentEntry[`Members];
			this.currentEntry = NDMenu.GetFileMenuSection(membersID);

			if (this.currentEntry == undefined)
				{  this.needToLoad = membersID;  }
			else
				{  this.currentEntry = this.currentEntry[`Members][this.offsetFromParent];  }

			this.nextIndex++;
			}

		// If we're on a file entry...
		else
			{
			// ...jump to the end of the path.  In most cases this will be the same as nextIndex++, but on the off chance
			// that we have an invalid path that extends beyond the file, just ignore the extra.
			this.nextIndex = this.pathObject.path.length + 1;
			this.currentEntry = undefined;
			this.offsetFromParent = -1;
			}

		return (this.nextIndex <= this.pathObject.path.length);
		};


	/* Function: NavigationType
		Returns the type of navigation entry the current position represents, which is different from currentEntry[`Type].
		It will return one of these values:

		`Nav_RootFolder - The topmost root folder.
		`Nav_SelectedRootFolder - The topmost root folder which is selected.
		`Nav_ParentFolder - A parent folder, but above the one that's selected.
		`Nav_SelectedParentFolder - The parent folder which is selected.
		`Nav_SelectedFile - The file which is selected.
		`Nav_NeedToLoad - This section of the menu hasn't been loaded yet.  The section to load will be stored in <needToLoad>.
		`Nav_OutOfBounds - The iterator is past the end of the path.
	*/
	this.NavigationType = function ()
		{
		/* Substitutions:
			`Nav_RootFolder = 0
			`Nav_SelectedRootFolder = 1
			`Nav_ParentFolder = 2
			`Nav_SelectedParentFolder = 3
			`Nav_SelectedFile = 4
			`Nav_NeedToLoad = 9
			`Nav_OutOfBounds = -1
		*/

		if (this.nextIndex > this.pathObject.path.length)
			{  return `Nav_OutOfBounds;  }

		else if (this.currentEntry == undefined)
			{  return `Nav_NeedToLoad;  }

		else if (this.currentEntry[`Type] == `ImplicitFile ||
				  this.currentEntry[`Type] == `ExplicitFile)
			{  return `Nav_SelectedFile;  }

		// So we're at a folder.  If it's the last one we know it's selected.
		else if (this.nextIndex == this.pathObject.path.length)
			{
			if (this.nextIndex == 0)
				{  return `Nav_SelectedRootFolder;  }
			else
				{  return `Nav_SelectedParentFolder;  }
			}

		// and if there's more than one past it, we know it's not selected.
		else if (this.nextIndex <= this.pathObject.path.length - 2)
			{
			if (this.nextIndex == 0)
				{  return `Nav_RootFolder;  }
			else
				{  return `Nav_ParentFolder;  }
			}

		// but if there's only one past it, we need to know whether it's a file or a folder to know whether it's selected
		// or not.
		else
			{
			var lookahead = this.Duplicate();
			lookahead.Next();

			if (lookahead.currentEntry == undefined)
				{  
				this.needToLoad = lookahead.needToLoad;
				return `Nav_NeedToLoad;  
				}
			else if (lookahead.currentEntry[`Type] == `ImplicitFile ||
					  lookahead.currentEntry[`Type] == `ExplicitFile)
				{  
				if (this.nextIndex == 0)
					{  return `Nav_SelectedRootFolder;  }
				else
					{  return `Nav_SelectedParentFolder;  }
				}
			else // on a folder
				{  
				if (this.nextIndex == 0)
					{  return `Nav_RootFolder;  }
				else
					{  return `Nav_ParentFolder;  }
				}
			}
		};


	/* Function: AtEndOfPath
		Returns whether the iterator is at the end of the path.
	*/
	this.AtEndOfPath = function ()
		{
		return (this.nextIndex == this.pathObject.path.length);
		};


	/* Function: Duplicate
		Creates and returns a new iterator at the same position as this one.
	*/
	this.Duplicate = function ()
		{
		var newObject = new NDFileMenuOffsetIterator (this.pathObject);
		newObject.currentEntry = this.currentEntry;
		newObject.offsetFromParent = this.offsetFromParent;
		newObject.needToLoad = this.needToLoad;
		newObject.nextIndex = this.nextIndex;

		return newObject;
		};



	// Group: Properties
	// ________________________________________________________________________


	/* Property: currentEntry
		A reference to the entry in <NDMenu.fileMenuSections> the iterator is currently on.  This will be undefined if
		the entry is not loaded yet.
	*/
	this.currentEntry = NDMenu.GetFileMenuSection(1);  // Will return undefined if not loaded yet.

	/* Property: offsetFromParent
		The location of the current entry in its parent's member list.
	*/
	this.offsetFromParent = -1;

	/* Property: needToLoad
		If <currentEntry> is undefined while the iterator is still in bounds or <NavigationType> returns `Nav_NeedToLoad,
		this will hold the ID of the menu section that needs to be loaded.  The value is not relevant otherwise and is not 
		guaranteed to be undefined.

		Remember that you need to create a new iterator after loading a section of the menu.  Existing ones are not
		guaranteed to notice the new addition.
	*/
	// this.needToLoad = undefined;

	if (this.currentEntry == undefined)
		{  this.needToLoad = 1;  }



	// Group: Variables
	// ________________________________________________________________________


	/* var: pathObject
		A reference to the <NDFileMenuOffsetPath> object this iterator works on.
	*/
	this.pathObject = pathObject;

	/* var: nextIndex
		An index into <NDFileMenuOffsetPath.path> of the *next* position, not what the current position is.  
		This means an index of zero refers to the root folder even though entry zero in the path refers to the root folder's 
		member.
	*/
	this.nextIndex = 0;

	};



/* Class: NDFileMenuHashPath
	___________________________________________________________________________

	A path through <NDMenu's> hierarchy using a hash path string such as "File2:folder/folder/source.cs".  All paths are
	assumed to terminate on a file name instead of a folder.  	This has the same interface as <NDFileMenuOffsetPath> so 
	they can be used interchangeably.

*/
function NDFileMenuHashPath (hashPath)
	{

	// Group: Functions
	// ________________________________________________________________________

	/* Function: GetIterator
		Creates and returns a new <iterator: NDFileMenuOffsetIterator> positioned at the beginning of the path.
	*/
	this.GetIterator = function ()
		{
		// We generate a new offset path for every iterator created because a path can persist between menu section
		// loads, but an iterator should not.
		return new NDFileMenuOffsetIterator(this.MakeOffsetPath());
		};


	/* Function: MakeOffsetPath
		Generates and returns a <NDFileMenuOffsetPath> from the hash path string.
		
		If there are not enough menu sections loaded to fully resolve it, it will generate what it can and put an extra -1 
		offset on the end to indicate  that there's more.  The extra entry prevents things from rendering as selected 
		when they may not be.  <NDFileMenuOffsetIterator> shouldn't have to worry about handling the -1 
		because it would stop with `Nav_NeedToLoad before using it, and after the section is loaded new iterators will 
		have to be created which will cause this function to generate an updated offset path.

		If there are invalid sections of the hash path, such as a folder name that doesn't exist, this will generate as much
		as it can from the valid section and ignore the rest.
	*/
	this.MakeOffsetPath = function ()
		{
		var offsets = [ ];

		if (this.hashPathString == "" || this.hashPathString == undefined)
			{  return new NDFileMenuOffsetPath(offsets);  }

		var section = NDMenu.GetFileMenuSection(1);

		if (section === undefined)
			{
			offsets.push(-1);
			return new NDFileMenuOffsetPath(offsets);
			}

		// If you don't test for undefined the !StartsWith will work.
		if (this.hashPathString == section[`HashPath] || 
			(section[`HashPath] !== undefined && !this.hashPathString.StartsWith(section[`HashPath])) )
			{  return new NDFileMenuOffsetPath(offsets);  }

		do
			{
			var found = false;

			for (var i = 0; i < section[`Members].length; i++)
				{
				var member = section[`Members][i];

				if (member[`Type] == `ExplicitFile || member[`Type] == `ImplicitFile)
					{
					var hashPath = section[`HashPath];

					if (member[`Type] == `ExplicitFile)
						{  hashPath += member[`HashPath];  }
					else
						{  hashPath += member[`Name];  }

					if (hashPath == this.hashPathString)
						{
						offsets.push(i);
						return new NDFileMenuOffsetPath(offsets);
						}
					}
				else // folder
					{
					if (this.hashPathString == member[`HashPath])
						{
						offsets.push(i);
						return new NDFileMenuOffsetPath(offsets);
						}
					else if (this.hashPathString.StartsWith(member[`HashPath]))
						{
						offsets.push(i);
						section = member;
						found = true;

						if (section[`Type] == `DynamicFolder)	
							{
							section = NDMenu.GetFileMenuSection(section[`Members]);
							if (section === undefined)
								{
								offsets.push(-1);
								return new NDFileMenuOffsetPath(offsets);
								}
							}

						break;
						}
					}
				}
			}
		while (found == true);

		return new NDFileMenuOffsetPath(offsets);
		};



	// Group: Properties
	// ________________________________________________________________________

	/* Property: hashPathString
		The hash path string such as "File2:folder/folder/source.cs".
	*/
	this.hashPathString = hashPath;

	};
