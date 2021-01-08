/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.Menu
 * ____________________________________________________________________________
 * 
 * A class for generating a menu tree.
 * 
 * Usage:
 * 
 *		- Add files with <AddFile()>.
 *		- Add classes with <AddClass()>.
 *		- If desired, condense unnecessary folder levels with <Condense()>.  You cannot add more entries after calling
 *		  this.
 *		- Sort the members with <Sort()>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Menu : Component
		{

		// Group: Functions
		// __________________________________________________________________________

		public Menu (Context context) : base (context)
			{
			rootFileMenu = null;
			rootClassMenu = null;
			rootInterfaceMenu = null;
			rootModuleMenu = null;
			rootDatabaseMenu = null;

			isCondensed = false;
			}


		/* Function: AddFile
		 * Adds a file to the menu tree.
		 */
		public void AddFile (Files.File file)
			{
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a file to the menu once it's been condensed.");  }
			#endif


			// Find which file source owns this file and generate a relative path to it.

			MenuEntries.FileSource fileSourceEntry = FindOrCreateFileSourceEntryOf(file);
			Path relativePath = fileSourceEntry.WrappedFileSource.MakeRelative(file.FileName);


			// Split off the file name and split the rest into individual folder names.

			string prefix;
			List<string> pathSegments;
			relativePath.Split(out prefix, out pathSegments);

			string fileName = pathSegments[pathSegments.Count - 1];
			pathSegments.RemoveAt(pathSegments.Count - 1);


			// Create the file entry and find out where it goes.  Create new folder levels as necessary.

			MenuEntries.File fileEntry = new MenuEntries.File(file);
			MenuEntries.Container container = fileSourceEntry;

			foreach (string pathSegment in pathSegments)
				{
				Path pathFromFileSource;

				if (container == fileSourceEntry)
					{  pathFromFileSource = pathSegment;  }
				else
					{  pathFromFileSource = (container as MenuEntries.Folder).PathFromFileSource + '/' + pathSegment;  }

				MenuEntries.Folder folderEntry = null;

				foreach (var member in container.Members)
					{
					if (member is MenuEntries.Folder && 
						(member as MenuEntries.Folder).PathFromFileSource == pathFromFileSource)
						{  
						folderEntry = (MenuEntries.Folder)member;
						break;
						}
					}

				if (folderEntry == null)
					{
					folderEntry = new MenuEntries.Folder(pathFromFileSource);
					folderEntry.Parent = container;
					container.Members.Add(folderEntry);
					}

				container = folderEntry;
				}

			fileEntry.Parent = container;
			container.Members.Add(fileEntry);
			}


		/* Function: AddClass
		 * Adds a class to the menu tree.
		 */
		public void AddClass (Symbols.ClassString classString)
		   {
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a class to the menu once it's been condensed.");  }
			#endif


			string[] classSegments = classString.Symbol.SplitSegments();

			MenuEntries.Container container;
			bool ignoreCase;

			if (classString.Hierarchy == Hierarchy.Class)
				{
				MenuEntries.Language languageEntry = FindOrCreateLanguageEntryOf(classString);

				container = languageEntry;
				ignoreCase = (languageEntry.WrappedLanguage.CaseSensitive == false);
				}

			else if (classString.Hierarchy == Hierarchy.Interface)
			{
				MenuEntries.Language languageEntry = FindOrCreateLanguageEntryOf(classString);

				container = languageEntry;
				ignoreCase = (languageEntry.WrappedLanguage.CaseSensitive == false);
			}

			else if (classString.Hierarchy == Hierarchy.Module)
			{
				MenuEntries.Language languageEntry = FindOrCreateLanguageEntryOf(classString);

				container = languageEntry;
				ignoreCase = (languageEntry.WrappedLanguage.CaseSensitive == false);
			}

			else if (classString.Hierarchy == Hierarchy.Database)
				{
				if (rootDatabaseMenu == null)
					{
					rootDatabaseMenu = new MenuEntries.Container(Hierarchy.Database);
					rootDatabaseMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Database");
					}

				container = rootDatabaseMenu;
				ignoreCase = true;
				}

			else
				{  throw new NotImplementedException();  }


			// Create the class and find out where it goes.  Create new scope containers as necessary.

			MenuEntries.Class classEntry = new MenuEntries.Class(classString);
			string scopeSoFar = null;

			// We only want to walk through the scope levels so we use length - 1 to ignore the last segment, which is the class name.
			for (int i = 0; i < classSegments.Length - 1; i++)
				{
				string classSegment = classSegments[i];

				if (scopeSoFar == null)
					{  scopeSoFar = classSegment;  }
				else
					{  scopeSoFar += Symbols.SymbolString.SeparatorChar + classSegment;  }

				MenuEntries.Scope scopeEntry = null;

				foreach (var member in container.Members)
					{
					if (member is MenuEntries.Scope && 
						string.Compare((member as MenuEntries.Scope).WrappedScopeString, scopeSoFar, ignoreCase) == 0)
						{  
						scopeEntry = (MenuEntries.Scope)member;
						break;
						}
					}

				if (scopeEntry == null)
					{
					scopeEntry = new MenuEntries.Scope(Symbols.SymbolString.FromExportedString(scopeSoFar), classString.Hierarchy);
					scopeEntry.Parent = container;
					container.Members.Add(scopeEntry);
					}

				container = scopeEntry;
				}

			classEntry.Parent = container;
			container.Members.Add(classEntry);
			}


		/* Function: Condense
		 *	 Removes unnecessary levels in the menu.  Only call this function after everything has been added.
		 */
		public void Condense ()
			{
			if (rootFileMenu != null)
				{
				rootFileMenu.Condense();

				// If there's only one file source we can remove the top level container.
				if (rootFileMenu.Members.Count == 1)
					{  
					MenuEntries.FileSource fileSourceEntry = (MenuEntries.FileSource)rootFileMenu.Members[0];

					// Overwrite the file source name with the tab title, especially since it might not be defined if there was only one.
					// We don't need an unnecessary level for a single file source.
					fileSourceEntry.Title = rootFileMenu.Title;

					// Get rid of unnecessary levels as there's no point in displaying them.
					fileSourceEntry.CondensedTitles = null;

					rootFileMenu = fileSourceEntry;
					}
				}

			if (rootClassMenu != null)
				{
				rootClassMenu.Condense();

				// If there's only one language we can remove the top level container.
				if (rootClassMenu.Members.Count == 1)
					{  
					MenuEntries.Language languageEntry = (MenuEntries.Language)rootClassMenu.Members[0];

					// We can overwrite the language name with the tab title.  We're not going to preserve an unnecessary level
					// for the language.
					languageEntry.Title = rootClassMenu.Title;

					// However, we are going to keep CondensedTitles because we want all scope levels to be visible, even if
					// they're empty.

					rootClassMenu = languageEntry;
					}
				}

			if (rootInterfaceMenu != null)
				{
				rootInterfaceMenu.Condense();

				// If there's only one language we can remove the top level container.
				if (rootInterfaceMenu.Members.Count == 1)
					{
					MenuEntries.Language languageEntry = (MenuEntries.Language)rootInterfaceMenu.Members[0];

					// We can overwrite the language name with the tab title.  We're not going to preserve an unnecessary level
					// for the language.
					languageEntry.Title = rootInterfaceMenu.Title;

					// However, we are going to keep CondensedTitles because we want all scope levels to be visible, even if
					// they're empty.

					rootInterfaceMenu = languageEntry;
					}
				}

			if (rootModuleMenu != null)
			{
				rootModuleMenu.Condense();

				// If there's only one language we can remove the top level container.
				if (rootModuleMenu.Members.Count == 1)
				{
					MenuEntries.Language languageEntry = (MenuEntries.Language)rootModuleMenu.Members[0];

					// We can overwrite the language name with the tab title.  We're not going to preserve an unnecessary level
					// for the language.
					languageEntry.Title = rootModuleMenu.Title;

					// However, we are going to keep CondensedTitles because we want all scope levels to be visible, even if
					// they're empty.

					rootModuleMenu = languageEntry;
				}
			}

			if (rootDatabaseMenu != null)
			   {
			   rootDatabaseMenu.Condense();

				// If the only top level entry is a scope we can merge it
				if (rootDatabaseMenu.Members.Count == 1 && rootDatabaseMenu.Members[0] is MenuEntries.Scope)
					{  
					MenuEntries.Scope scopeEntry = (MenuEntries.Scope)rootDatabaseMenu.Members[0];

					// Move the scope title into CondensedTitles since we want it to be visible.
					if (scopeEntry.CondensedTitles == null)
						{
						scopeEntry.CondensedTitles = new List<string>(1);
						scopeEntry.CondensedTitles.Add(scopeEntry.Title);
						}
					else
						{
						scopeEntry.CondensedTitles.Insert(0, scopeEntry.Title);
						}

					// Now overwrite the original title with the tab title.
					scopeEntry.Title = rootDatabaseMenu.Title;

					rootDatabaseMenu = scopeEntry;
					}
			   }

			isCondensed = true;
			}


		/* Function: Sort
		 * Sorts the menu entries.  Should only be done after everything is added to the menu.
		 */
		public void Sort ()
			{
			if (rootFileMenu != null)
				{  rootFileMenu.Sort();  }

			if (rootClassMenu != null)
				{  rootClassMenu.Sort();  }

			if (rootInterfaceMenu != null)
				{  rootInterfaceMenu.Sort();  }

			if (rootModuleMenu != null)
				{  rootModuleMenu.Sort();  }

			if (rootDatabaseMenu != null)
				{  rootDatabaseMenu.Sort();  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindOrCreateFileSourceEntryOf
		 * Finds or creates the file source entry associated with the passed file.
		 */
		protected MenuEntries.FileSource FindOrCreateFileSourceEntryOf (Files.File file)
			{
			var fileSource = EngineInstance.Files.FileSourceOf(file);
			var fileSourceEntry = FindFileSourceEntry(fileSource);

			if (fileSourceEntry == null)
				{  fileSourceEntry = CreateFileSourceEntry(fileSource);  }

			return fileSourceEntry;
			}


		/* Function: CreateFileSourceEntry
		 * Creates an entry for the file source, adds it to the menu, and returns it.  It will also create the <rootFileMenu> 
		 * container if necessary.
		 */
		protected MenuEntries.FileSource CreateFileSourceEntry (Files.FileSource fileSource)
			{
			#if DEBUG
			if (FindFileSourceEntry(fileSource) != null)
				{  throw new Exception ("Tried to create a file source entry that already existed in the menu.");  }
			#endif

			if (rootFileMenu == null)
				{
				rootFileMenu = new MenuEntries.Container(Hierarchy.File);
				rootFileMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Files");
				}

			MenuEntries.FileSource fileSourceEntry = new MenuEntries.FileSource(fileSource);
			fileSourceEntry.Parent = rootFileMenu;
			rootFileMenu.Members.Add(fileSourceEntry);

			return fileSourceEntry;
			}


		/* Function: FindFileSourceEntry
		 * Returns the menu entry that contains the passed file source, or null if there isn't one yet.
		 */
		protected MenuEntries.FileSource FindFileSourceEntry (Files.FileSource fileSource)
			{
			if (rootFileMenu == null)
				{  return null;  }

			// If the menu only had one file source and it was condensed, the root file entry may have been replaced
			// by that file source.
			else if (rootFileMenu is MenuEntries.FileSource)
				{
				MenuEntries.FileSource fileSourceEntry = (MenuEntries.FileSource)rootFileMenu;

				if (fileSourceEntry.WrappedFileSource == fileSource)
					{  return fileSourceEntry;  }
				else
					{  return null;  }
				}

			// We're assuming that the only other possibility is a container with a flat list of FileSources.  If we later allow 
			// FileSources to be put in nested groups this will need to be updated.
			else
				{
				foreach (var member in rootFileMenu.Members)
					{
					if (member is MenuEntries.FileSource)
						{
						MenuEntries.FileSource fileSourceEntry = (MenuEntries.FileSource)member;
						
						if (fileSourceEntry.WrappedFileSource == fileSource)
							{  return fileSourceEntry;  }
						}
					}

				return null;
				}
			}


		/* Function: FindOrCreateLanguageEntryOf
		 * Finds or creates the language entry associated with the passed ClassString.
		 */
		protected MenuEntries.Language FindOrCreateLanguageEntryOf (Symbols.ClassString classString)
			{
			var languageEntry = FindLanguageEntry(classString);

			if (languageEntry == null)
				{  languageEntry = CreateLanguageEntry(classString);  }

			return languageEntry;
			}


		/* Function: CreateLanguageEntry
		 * Creates an entry for the language, adds it to the class menu, and returns it.  It will also create the <rootClassMenu> 
		 * container if necessary.
		 * TODO: Support for separating SystemVerilog, Verilog, VHDL???
		 */
		protected MenuEntries.Language CreateLanguageEntry (Symbols.ClassString classString)
			{
			var language = EngineInstance.Languages.FromID(classString.LanguageID);
			var hierarchy = classString.Hierarchy;
			MenuEntries.Language languageEntry = null;
			#if DEBUG
			if (FindLanguageEntry(classString) != null)
				{  throw new Exception ("Tried to create a language entry that already existed in the menu.");  }
			#endif

			if (rootClassMenu == null)
				{
				//TODO - temp
				rootClassMenu = new MenuEntries.Container(Hierarchy.Class);
				rootClassMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Classes");
				}
			if (rootInterfaceMenu == null)
			{
				//TODO - temp
				rootInterfaceMenu = new MenuEntries.Container(Hierarchy.Interface);
				rootInterfaceMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Interfaces");
			}
			if (rootModuleMenu == null)
			{
				//TODO - temp
				rootModuleMenu = new MenuEntries.Container(Hierarchy.Module);
				rootModuleMenu.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Modules");
			}

			// TODO - temp
			if (hierarchy == Hierarchy.Class)
			{
				languageEntry = new MenuEntries.Language(language, Hierarchy.Class);
				languageEntry.Parent = rootClassMenu;
				rootClassMenu.Members.Add(languageEntry);
			}
			else if (hierarchy == Hierarchy.Module)
			{
				languageEntry = new MenuEntries.Language(language, Hierarchy.Module);
				languageEntry.Parent = rootModuleMenu;
				rootModuleMenu.Members.Add(languageEntry);
			}
			else if (hierarchy == Hierarchy.Interface)
			{
				languageEntry = new MenuEntries.Language(language, Hierarchy.Interface);
				languageEntry.Parent = rootInterfaceMenu;
				rootInterfaceMenu.Members.Add(languageEntry);
			}
			else
            {
				// TODO - throw 'non implementation' error
            }


			return languageEntry;
			}


		/* Function: FindLanguageEntry
		 * Returns the entry that contains the passed language, or null if there isn't one yet.
		 */
		protected MenuEntries.Language FindLanguageEntry (Symbols.ClassString classString)
			{
			var language = EngineInstance.Languages.FromID(classString.LanguageID);
			MenuEntries.Container rootMenu;
			if (classString.Hierarchy == Hierarchy.Class)
            {
				rootMenu = rootClassMenu;
            }
			else if (classString.Hierarchy == Hierarchy.Module)
            {
				rootMenu = rootModuleMenu;
            }
			else
            {
				rootMenu = rootInterfaceMenu;
            }
			if (rootMenu == null)
				{  return null;  }

			// If the menu only had one language and it was condensed, the root class entry may have been replaced
			// by that language.
			else if (rootMenu is MenuEntries.Language)
				{
				MenuEntries.Language languageEntry = (MenuEntries.Language)rootMenu;

				if (languageEntry.WrappedLanguage == language)
					{  return languageEntry;  }
				else
					{  return null;  }
				}

			// The only other possibility is a container with a flat list of languages.
			else
				{
				foreach (var member in rootMenu.Members)
					{
					if (member is MenuEntries.Language)
						{
						MenuEntries.Language languageEntry = (MenuEntries.Language)member;
						
						if (languageEntry.WrappedLanguage == language)
							{  return languageEntry;  }
						}
					}

				return null;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: RootFileMenu
		 * 
		 * The root container of all file-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.FileSources> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		public MenuEntries.Container RootFileMenu
			{
			get
				{  return rootFileMenu;  }
			}
			

		/* Property: RootClassMenu
		 * 
		 * The root container of all class-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		public MenuEntries.Container RootClassMenu
			{
			get
				{  return rootClassMenu;  }
			}


		/* Property: RootInterfaceMenu
		 * 
		 * The root container of all interface-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		public MenuEntries.Container RootInterfaceMenu
			{
			get
				{  return rootInterfaceMenu;  }
			}


		/* Property: RootModule
		 * 
		 * The root container of all module-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		public MenuEntries.Container RootModuleMenu
			{
			get
				{  return rootModuleMenu;  }
			}

		/* Property: RootDatabaseMenu
		 * 
		 * The root container of all database-based menu entries, or null if none.
		 */
		public MenuEntries.Container RootDatabaseMenu
			{
			get
				{  return rootDatabaseMenu;  }
			}
			


		// Group: Variables
		// __________________________________________________________________________


		/* var: rootFileMenu
		 * 
		 * The root container of all file-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.FileSources> as its members.  However,
		 * after condensation it may be a file source if there was only one.
		 */
		protected MenuEntries.Container rootFileMenu;

		/* var: rootClassMenu
		 * 
		 * The root container of all class-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a language if there was only one.
		 */
		protected MenuEntries.Container rootClassMenu;

		/* var: rootInterfaceMenu
		 * 
		 * The root container of all interface-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a language if there was only one.
		 */
		protected MenuEntries.Container rootInterfaceMenu;

		/* var: rootModuleMenu
		 * 
		 * The root container of all module-based menu entries, or null if none.
		 * 
		 * Before condensation this will be a container with only <MenuEntries.Languages> as its members.  However,
		 * after condensation it may be a language if there was only one.
		 */
		protected MenuEntries.Container rootModuleMenu;

		/* var: rootDatabaseMenu
		 * 
		 * The root container of all database-based menu entries, or null if none.
		 */
		protected MenuEntries.Container rootDatabaseMenu;

		/* var: isCondensed
		 * Whether the menu tree has been condensed.
		 */
		protected bool isCondensed;

		}
	}