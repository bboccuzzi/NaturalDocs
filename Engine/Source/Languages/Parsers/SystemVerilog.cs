/* 
 * Class: CodeClear.NaturalDocs.Engine.Languages.Parsers.SystemVerilog
 * ____________________________________________________________________________
 * 
 * Additional language support for SystemVerilog.
 * TODO: Currently this code is forked from the Python parser and therefore may have remnants of Python parsing
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Languages.Parsers
{
	public class SystemVerilog : Language
	{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemVerilog
		 */
		public SystemVerilog(Languages.Manager manager) : base(manager, "SystemVerilog")
		{
		}


		/* Function: SyntaxHighlight
		 */
		override public void SyntaxHighlight(Tokenizer source)
		{
			TokenIterator iterator = source.FirstToken;

			while (iterator.IsInBounds)
			{
				if (TryToSkipComment(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipString(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipNumber(ref iterator, ParseMode.SyntaxHighlight) ||
					TryToSkipDecorator(ref iterator, ParseMode.SyntaxHighlight))
				{
				}
				else if (iterator.FundamentalType == FundamentalType.Text)
				{
					TokenIterator endOfIdentifier = iterator;

					TryToSkipUnqualifiedIdentifier(ref endOfIdentifier);
					string identifier = source.TextBetween(iterator, endOfIdentifier);

					if (verilog1995keywords.Contains(identifier) || systemverilogkeywords.Contains(identifier))
					{ iterator.SetSyntaxHighlightingTypeByCharacters(SyntaxHighlightingType.Keyword, identifier.Length); }

					iterator = endOfIdentifier;
				}
				else
				{ iterator.Next(); }
			}
		}


		/* Function: ParsePrototype
		 * Converts a raw text prototype into a <ParsedPrototype>.
		 */

		// ANSI-C Style Port List (Verilog-2001) https://sutherland-hdl.com/pdfs/verilog_2001_ref_guide.pdf pg 12 of PDF
		// module module_name #(parameter_declaration, ...) (port_declaration, port_name, ...); ... endmodule
		// Two styles of "prototype" - In one situation there is the declaration of module - another instance is instantiation
		// Instantiation assigns parameters and ports by name, association, or positional. Current parser cannot differentiate these
		override public ParsedPrototype ParsePrototype(string stringPrototype, int commentTypeID)
		{
			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			ParsedPrototype parsedPrototype;


			// Mark any leading decorators.

			TokenIterator iterator = tokenizedPrototype.FirstToken;

			TryToSkipWhitespace(ref iterator, true, ParseMode.ParsePrototype);

			if (TryToSkipDecorators(ref iterator, ParseMode.ParsePrototype))
			{ TryToSkipWhitespace(ref iterator, true, ParseMode.ParsePrototype); }


			// Search for the first opening bracket or brace.

			char closingBracket = '\0';
			bool noparam = true;
			bool trueClose = false;

			// With Verilog, must parse parameter list and 
			//iterator.MatchesAcrossTokens("#(")
			while (!trueClose)
			{
				trueClose = true;
				while (iterator.IsInBounds)
				{
					if (iterator.Character == '(')
					{
						closingBracket = ')';
						trueClose = true;
						break;
					}
					else if (iterator.Character == '[')
					{
						// Only treat brackets as parameters if it's following "this", meaning it's an iterator.  Ignore all others so we
						// don't get tripped up on metadata or array brackets on return values.

						TokenIterator lookbehind = iterator;
						lookbehind.Previous();
						lookbehind.PreviousPastWhitespace(PreviousPastWhitespaceMode.Iterator);

						if (lookbehind.MatchesToken("this"))
						{
							closingBracket = ']';
							break;
						}
						else
						{ iterator.Next(); }
					}
					else if (iterator.Character == '{')
					{
						closingBracket = '}';
						break;
					}

					// Parameter list, useful for modules, interfaces, etc...
					else if (iterator.Character == '#')
					{
						// Found a parameter list, need to find opening bracket for parameters
						while (iterator.IsInBounds)
						{
							if (iterator.Character == '(') { break; }
							else { iterator.Next(); }
						}
						TokenIterator lookahead = iterator;
						trueClose = true;
						lookahead.Next();
						while (lookahead.IsInBounds)
                        {
							if (lookahead.Character == '(') {
								trueClose = false;
							}
							lookahead.Next();
                        }
						closingBracket = ')';
						break;
					}
					else if (TryToSkipComment(ref iterator) ||
							   TryToSkipString(ref iterator))
					{ }
					else
					{ iterator.Next(); }
				}


				// If we found brackets, it's either a function prototype or a class prototype that includes members.  
				// Mark the delimiters.
				if (closingBracket != '\0')
				{
					noparam = false;
					iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
					if (!trueClose)
					{
						iterator.PrototypeParsingType = PrototypeParsingType.StartOfParams;
					}
					iterator.Next();

					while (iterator.IsInBounds) 
					{
						if (iterator.Character == ',')
						{
							iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
							iterator.Next();
						}

						else if (iterator.Character == closingBracket)
						{
							iterator.Previous();
							iterator.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
							iterator.Next();
							if (trueClose)
							{
								iterator.PrototypeParsingType = PrototypeParsingType.EndOfParams;
							}
							break;
						}

						// Unlike prototype detection, here we treat < as an opening bracket.  Since we're already in the parameter list
						// we shouldn't run into it as part of an operator overload, and we need it to not treat the comma in "template<a,b>"
						// as a parameter divider.
						else if (TryToSkipComment(ref iterator) ||
								   TryToSkipString(ref iterator) ||
								   TryToSkipBlock(ref iterator, false))
						{ }

						else
						{ iterator.Next(); }
	
					}

				}

			}

			// If there's no brackets, it's a variable, property, or class.
			if (noparam)
			{
				parsedPrototype = new ParsedPrototype(tokenizedPrototype);
				TokenIterator start = tokenizedPrototype.FirstToken;
				TokenIterator end = tokenizedPrototype.LastToken;

				MarkVerilogParameter(start, end);
			}
			else
			{
				// We have enough tokens marked to create the parsed prototype.  This will also let us iterate through the parameters
				// easily.

				parsedPrototype = new ParsedPrototype(tokenizedPrototype, supportsImpliedTypes: false);


				// Set the main section to the last one, since any decorators present will each be in their own section.  Some can have
				// parameter lists and we don't want those confused for the actual parameter list.

				parsedPrototype.MainSectionIndex = parsedPrototype.Sections.Count - 1;


				// Mark the part before the parameters, which includes the name and return value.

				TokenIterator start, end;
				parsedPrototype.GetBeforeParameters(out start, out end);

				// Exclude the opening bracket
				end.Previous();
				end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < end)
				{ MarkVerilogParameter(start, end); }


				// If there are any parameters, mark the tokens in them.

				if (parsedPrototype.NumberOfParameters > 0)
				{
					for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)
					{
						parsedPrototype.GetParameter(i, out start, out end);
						MarkVerilogParameter(start, end);
					}
				}
			}
			return parsedPrototype;
		}


		/*
		 /* Function: ParseClassPrototype
		 * Converts a raw text prototype into a <ParsedClassPrototype>.  Will return null if it is not an appropriate prototype.
		 //
		override public ParsedClassPrototype ParseClassPrototype(string stringPrototype, int commentTypeID)
		{
			if (EngineInstance.CommentTypes.FromID(commentTypeID).Flags.ClassHierarchy == false)
			{ return null; }

			Tokenizer tokenizedPrototype = new Tokenizer(stringPrototype, tabWidth: EngineInstance.Config.TabWidth);
			TokenIterator startOfPrototype = tokenizedPrototype.FirstToken;
			ParsedClassPrototype parsedPrototype = new ParsedClassPrototype(tokenizedPrototype);
			bool success = false;

			success = TryToSkipClassDeclarationLine(ref startOfPrototype, ParseMode.ParseClassPrototype);

			if (success)
			{ return parsedPrototype; }
			else
			{ return base.ParseClassPrototype(stringPrototype, commentTypeID); }
		}
		*/


		// Group: Parsing Functions
		// __________________________________________________________________________


		/* Function: TryToSkipClassDeclarationLine
		 * 
		 * If the iterator is on a class's declaration line, moves it past it and returns true.  It does not handle the class body.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassDeclarationLine(ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			TokenIterator lookahead = iterator;


			// Decorators

			if (TryToSkipDecorators(ref lookahead, mode))
			{ TryToSkipWhitespace(ref lookahead); }


			// Keyword

			if (lookahead.MatchesToken("class") == false)
			{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
			}

			if (mode == ParseMode.ParseClassPrototype)
			{ lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.Keyword; }

			lookahead.Next();
			TryToSkipWhitespace(ref lookahead);


			// Name

			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
			{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
			}

			if (mode == ParseMode.ParseClassPrototype)
			{ startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name); }

			TryToSkipWhitespace(ref lookahead);


			// Base classes

			if (lookahead.Character == '(')
			{
				if (mode == ParseMode.ParseClassPrototype)
				{ lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfParents; }

				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				for (; ; )
				{
					if (lookahead.Character == ')')
					{
						if (mode == ParseMode.ParseClassPrototype)
						{ lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.EndOfParents; }

						break;
					}

					if (TryToSkipClassParent(ref lookahead, mode) == false)
					{
						ResetTokensBetween(iterator, lookahead, mode);
						return false;
					}

					TryToSkipWhitespace(ref lookahead);

					if (lookahead.Character == ',')
					{
						if (mode == ParseMode.ParseClassPrototype)
						{ lookahead.ClassPrototypeParsingType = ClassPrototypeParsingType.ParentSeparator; }

						lookahead.Next();
						TryToSkipWhitespace(ref lookahead);
					}
				}
			}


			iterator = lookahead;
			return true;
		}


		/* Function: TryToSkipDecorators
		 * 
		 * Tries to move the iterator past a group of decorators.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark each decorator with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorators(ref TokenIterator iterator, ParseMode mode = ParseMode.ParseClassPrototype)
		{
			if (TryToSkipDecorator(ref iterator, mode) == false)
			{ return false; }

			for (; ; )
			{
				TokenIterator lookahead = iterator;
				TryToSkipWhitespace(ref lookahead);

				if (TryToSkipDecorator(ref lookahead, mode) == true)
				{ iterator = lookahead; }
				else
				{ break; }
			}

			return true;
		}


		/* Function: TryToSkipDecorator
		 * 
		 * Tries to move the iterator past a single decorator.  Note that there may be more than one decorator in a row, so use <TryToSkipDecorators()>
		 * if you need to move past all of them.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParsePrototype>
		 *			- Each decorator will create a new prototype section.
		 *		- <ParseMode.ParseClassPrototype>
		 *			- Will mark the first token with <ClassPrototypeParsingType.StartOfPrePrototypeLine> and the rest with <ClassPrototypeParsingType.PrePrototypeLine>.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecorator(ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			if (iterator.Character != '@')
			{ return false; }

			TokenIterator lookahead = iterator;
			lookahead.Next();

			if (TryToSkipIdentifier(ref lookahead) == false)
			{ return false; }

			TokenIterator decoratorStart = iterator;
			TokenIterator decoratorEnd = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
			{ decoratorStart.SetSyntaxHighlightingTypeBetween(decoratorEnd, SyntaxHighlightingType.Metadata); }

			TryToSkipWhitespace(ref lookahead);

			if (TryToSkipDecoratorParameters(ref lookahead, mode))
			{ decoratorEnd = lookahead; }

			if (mode == ParseMode.ParsePrototype)
			{
				decoratorStart.PrototypeParsingType = PrototypeParsingType.StartOfPrototypeSection;
				decoratorEnd.PrototypeParsingType = PrototypeParsingType.EndOfPrototypeSection;
			}
			else if (mode == ParseMode.ParseClassPrototype)
			{
				iterator.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.PrePrototypeLine);
				iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.StartOfPrePrototypeLine;
			}

			iterator = decoratorEnd;
			return true;
		}


		/* Function: TryToSkipDecoratorParameters
		 * 
		 * Tries to move the iterator past a decorator parameter section, such as "("String")" in "@Copynight("String")".
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.SyntaxHighlight>
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipDecoratorParameters(ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			if (iterator.Character != '(')
			{ return false; }

			TokenIterator lookahead = iterator;

			if (!TryToSkipBlock(ref lookahead, false))
			{ return false; }

			TokenIterator end = lookahead;

			if (mode == ParseMode.SyntaxHighlight)
			{
				iterator.SetSyntaxHighlightingTypeBetween(end, SyntaxHighlightingType.Metadata);
			}

			else if (mode == ParseMode.ParsePrototype)
			{
				TokenIterator openingParen = iterator;

				TokenIterator closingParen = lookahead;
				closingParen.Previous();

				openingParen.PrototypeParsingType = PrototypeParsingType.StartOfParams;
				closingParen.PrototypeParsingType = PrototypeParsingType.EndOfParams;

				lookahead = openingParen;
				lookahead.Next();

				TokenIterator startOfParam = lookahead;

				while (lookahead < closingParen)
				{
					if (lookahead.Character == ',')
					{
						MarkDecoratorParameter(startOfParam, lookahead, mode);

						lookahead.PrototypeParsingType = PrototypeParsingType.ParamSeparator;
						lookahead.Next();

						startOfParam = lookahead;
					}

					else if (TryToSkipComment(ref lookahead) ||
							   TryToSkipString(ref lookahead) ||
							   TryToSkipBlock(ref lookahead, true))
					{ }

					else
					{ lookahead.Next(); }
				}

				MarkDecoratorParameter(startOfParam, lookahead, mode);
			}

			iterator = end;
			return true;
		}


		/* Function: MarkDecoratorParameter
		 * 
		 * Applies types to an decorator parameter, such as ""String"" in "@Copynight("String")" or "id = 12" in 
		 * "@RequestForEnhancement(id = 12, engineer = "String")".
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.ParsePrototype>
		 *			- The contents will be marked with parameter tokens.
		 *		- Everything else has no effect.
		 */
		protected void MarkDecoratorParameter(TokenIterator start, TokenIterator end, ParseMode mode = ParseMode.IterateOnly)
		{
			if (mode != ParseMode.ParsePrototype)
			{ return; }

			start.NextPastWhitespace(end);
			end.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

			if (start >= end)
			{ return; }


			// Find and mark the equals sign, if there is one

			TokenIterator equals = start;

			while (equals < end)
			{
				if (equals.Character == '=')
				{
					equals.PrototypeParsingType = PrototypeParsingType.PropertyValueSeparator;
					break;
				}
				else if (TryToSkipComment(ref equals) ||
							TryToSkipString(ref equals) ||
							TryToSkipBlock(ref equals, true))
				{ }
				else
				{ equals.Next(); }
			}


			// The equals sign will be at or past the end if it doesn't exist.

			if (equals >= end)
			{
				start.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue);
			}
			else
			{
				TokenIterator iterator = equals;
				iterator.PreviousPastWhitespace(PreviousPastWhitespaceMode.EndingBounds, start);

				if (start < iterator)
				{ start.SetPrototypeParsingTypeBetween(iterator, PrototypeParsingType.Name); }

				iterator = equals;
				iterator.Next();
				iterator.NextPastWhitespace(end);

				if (iterator < end)
				{ iterator.SetPrototypeParsingTypeBetween(end, PrototypeParsingType.PropertyValue); }
			}
		}


		/* Function: TryToSkipClassParent
		 * 
		 * Tries to move the iterator past a single class parent declaration.
		 * 
		 * Supported Modes:
		 * 
		 *		- <ParseMode.IterateOnly>
		 *		- <ParseMode.ParseClassPrototype>
		 *		- Everything else is treated as <ParseMode.IterateOnly>.
		 */
		protected bool TryToSkipClassParent(ref TokenIterator iterator, ParseMode mode = ParseMode.IterateOnly)
		{
			TokenIterator lookahead = iterator;

			if (lookahead.MatchesToken("metaclass"))
			{
				lookahead.Next();
				TryToSkipWhitespace(ref lookahead);

				if (lookahead.Character == '=')
				{
					if (mode == ParseMode.ParseClassPrototype)
					{ iterator.ClassPrototypeParsingType = ClassPrototypeParsingType.Modifier; }

					lookahead.Next();
					TryToSkipWhitespace(ref lookahead);
				}
				else
				{
					// Nevermind, reset
					lookahead = iterator;
				}
			}


			TokenIterator startOfIdentifier = lookahead;

			if (TryToSkipIdentifier(ref lookahead) == false)
			{
				ResetTokensBetween(iterator, lookahead, mode);
				return false;
			}

			if (mode == ParseMode.ParseClassPrototype)
			{ startOfIdentifier.SetClassPrototypeParsingTypeBetween(lookahead, ClassPrototypeParsingType.Name); }

			iterator = lookahead;
			return true;
		}


		/* Function: TryToSkipIdentifier
		 * Tries to move the iterator past a qualified identifier, such as "X.Y.Z".  Use <TryToSkipUnqualifiedIdentifier()> if you only want
		 * to skip a single segment.
		 */
		protected bool TryToSkipIdentifier(ref TokenIterator iterator)
		{
			TokenIterator lookahead = iterator;

			for (; ; )
			{
				if (TryToSkipUnqualifiedIdentifier(ref lookahead) == false)
				{ return false; }

				if (lookahead.Character == '.')
				{ lookahead.Next(); }
				else
				{ break; }
			}

			iterator = lookahead;
			return true;
		}


		/* Function: TryToSkipUnqualifiedIdentifier
		 * Tries to move the iterator past a single unqualified identifier, which means only "X" in "X.Y.Z".
		 */
		protected bool TryToSkipUnqualifiedIdentifier(ref TokenIterator iterator)
		{
			if (iterator.FundamentalType == FundamentalType.Text)
			{
				if (iterator.Character >= '0' && iterator.Character <= '9')
				{ return false; }
			}
			else if (iterator.FundamentalType == FundamentalType.Symbol)
			{
				if (iterator.Character != '_')
				{ return false; }
			}
			else
			{ return false; }

			do
			{ iterator.Next(); }
			while (iterator.FundamentalType == FundamentalType.Text || iterator.Character == '_');

			return true;
		}



		// Group: Static Variables
		// __________________________________________________________________________

		//https://www.intel.com/content/www/us/en/programmable/quartushelp/17.0/mapIdTopics/jka1465580561132.htm

		// TODO - Expand on keywords per specification.


		/* var: systemverilogkeywords
		 */
		static protected StringSet systemverilogkeywords = new StringSet(KeySettings.Literal, new string[] {
			"accept_on", "export", "ref", "alias", "extends", "restrict", "always_comb", "extern", "return", "always_ff",
			"final", "s_always", "always_latch", "first_match", "s_eventually", "assert", "foreach", "s_nexttime", "assume",
			"forkjoin", "s_until", "before", "global", "s_until_with", "bind", "iff", "sequence", "bins", "ignore_bins",
			"shortint", "binsof", "illegal_bins", "shortreal", "bit", "implies", "solve", "break", "import", "static",
			"byte", "inside", "string", "chandle", "int", "strong", "checker", "interface", "struct", "class", "intersect",
			"super", "clocking", "join_any", "sync_accept_on", "const", "join_none", "sync_reject_on", "constraint", "let",
			"tagged", "context", "local", "this", "continue", "logic", "throughout", "cover", "longint", "timeprecision",
			"covergroup", "matches", "timeunit", "coverpoint", "modport", "type", "cross", "new", "typedef", "dist", "nexttime",
			"union", "do", "null", "unique", "endchecker", "package", "unique0", "endclass", "packed", "until", "endclocking",
			"priority", "until_with", "endgroup", "program", "untypted", "endinterface", "property", "var", "endpackage",
			"protected", "virtual", "endprogram", "pure", "void", "endproperty", "rand", "wait_order", "endsequence",
			"randc", "weak", "enum", "randcase", "wildcard", "eventually", "randsequence", "with", "expect", "reject_on", "within"
		});
		/* var: verilog1995keywords
		 */

		//https://www.globalspec.com/reference/79171/203279/appendix-b-verilog-and-systemverilog-reserved-keywords
		static protected StringSet verilog1995keywords = new StringSet(KeySettings.Literal, new string[] {
			"function", "endfunction", "task", "endtask", "case", "endcase", "begin", "end", "initial", "always",
			"ifnon", "and", "rtran", "assign", "inout", "input", "buf", "integer", "scalared", "bufif0", "join", "small", "bufif1",
			"large", "specify", "macromodule", "specparam", "casex", "medium", "strong0", "casez", "module", "strong1", "nand", "supply0", "deassign", "negedge", "supply1", "default",
			"table", "defparam", "nor", "disable", "not", "edge ", "notif0 ", "else ", "notif1 ", "or", "output", "tri", "endmodule",
			"parameter", "tri0", "tri1", "endprimitive", "posedge", "triand", "endspecify", "primitive", "trior", "endtable", "pull0", "trireg", "pull1", "vectored", "pullup", "wait", "for",
			"pulldown", "wand", "force", "weak0", "forever", "real", "weak1", "realtime", "while", "reg", "wire", "highz0", "release", "wor", "highz1", "repeat", "xnor", "if", "xor",

			// VLSI, gate level primitives
			"rnmos", "rcmos", "rpmos", "pmos", "cmos", "nmos", "rtanif0", "rtranif1", "tranif0", "tranif1", "tran", "rtran",

			// Sim
			"time", "fork", "event"
			});

	}
}
