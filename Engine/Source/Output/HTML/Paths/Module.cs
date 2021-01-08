﻿/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Paths.Class
 * ____________________________________________________________________________
 * 
 * Path functions relating to classes in HTML output.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Paths
	{
	static public class Module
		{

		/* Function: OutputFile
		 * Returns the output file for a class.
		 */
		static public Path OutputFile (Path targetOutputFolder, string simpleLanguageID, SymbolString classSymbol)
			{
			SymbolString qualifier = classSymbol.WithoutLastSegment;
			string endingSymbol = classSymbol.LastSegment;

			return OutputFolder(targetOutputFolder, simpleLanguageID, qualifier) + '/' +
					  Utilities.Sanitize(endingSymbol, replaceDots: true) + ".html";
			}


		/* Function: OutputFolder
		 * 
		 * Returns the output folder for class files, optionally for the passed language and qualifier within it.
		 * 
		 * - If language isn't specified, it returns the output folder for all class files.
		 * - If language is specified but the qualifier is not, it returns the output folder for all class files of that language.
		 * - If language and qualifier are specified, it returns the output folder for that qualifier.
		 * 
		 * Examples:
		 * 
		 *		targetOutputFolder - C:\Project\Documentation\classes
		 *		targetOutputFolder + simpleLanguageID - C:\Project\Documentation\classes\CSharp
		 *		targetOutputFolder + simpleLanguageID + qualifier - C:\Project\Documentation\classes\CSharp\Namespace1\Namespace2
		 */
		static public Path OutputFolder (Path targetOutputFolder, string simpleLanguageID = null,
													  SymbolString qualifier = default(SymbolString))
			{
			StringBuilder result = new StringBuilder(targetOutputFolder);
			result.Append("/modules");  

			if (simpleLanguageID != null)
				{
				result.Append('/');
				result.Append(simpleLanguageID);
					
				if (qualifier != null)
					{
					result.Append('/');
					string pathString = qualifier.FormatWithSeparator('/');
					result.Append(Utilities.Sanitize(pathString));
					}
				}

			return result.ToString();
			}


		/* Function: HashPath
		 * Returns the hash path for the class.
		 */
		static public string HashPath (string simpleLanguageID, SymbolString classSymbol)
			{
			SymbolString qualifier = classSymbol.WithoutLastSegment;
			string endingSymbol = classSymbol.LastSegment;

			return QualifierHashPath(simpleLanguageID, qualifier) + Utilities.Sanitize(endingSymbol);
			}


		/* Function: QualifierHashPath
		 * 
		 * Returns the qualifier part of the hash path for classes.  If the qualifier symbol is specified it will include a 
		 * trailing member operator so that the last segment can simply be concatenated.
		 * 
		 * - If language ID is specified but the qualifier is not, it returns the prefix for all class paths of that language.
		 * - If language ID and qualifier are specified, it returns the prefix plus the hash path for that qualifier6, including
		 *   a trailing separator.
		 * 
		 * Examples:
		 * 
		 *		simpleLanguageID - CSharpClass:
		 *		simpleLanguageID + qualifier - CSharpClass:Namespace1.Namespace2.
		 */
		static public string QualifierHashPath (string simpleLanguageID, SymbolString qualifier = default(SymbolString))
			{
			StringBuilder result = new StringBuilder();

			result.Append(simpleLanguageID);
			result.Append("Module:");

			if (qualifier != null)
				{
				string pathString = qualifier.FormatWithSeparator('.');
				result.Append(Utilities.Sanitize(pathString));
				result.Append('.');
				}

			return result.ToString();
			}

		}
	}
