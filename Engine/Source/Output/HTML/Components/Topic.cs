﻿/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.Topic
 * ____________________________________________________________________________
 * 
 * A reusable class for building HTML topics.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Links;
using CodeClear.NaturalDocs.Engine.Prototypes;
using CodeClear.NaturalDocs.Engine.Tokenization;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Topic : HTML.Components.FormattedText
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Topic
		 */
		public Topic (Context context) : base (context)
			{
			// These are created on first use since they may not be needed
			prototypeBuilder = null;
			classPrototypeBuilder = null;
			}


		/* Function: AppendTopic
		 * 
		 * Builds the HTML for the topic and appends it to the passed StringBuilder.
		 * 
		 * Parameters:
		 * 
		 *		topic - The topic to build.
		 *		context - The <Context> the topic appears in.  The topic will automatically replace the context's topic, so you can just
		 *					  pass the context of the topic page.
		 *		links - A list of <Links> that must contain any links found in the topic.
		 *		linkTargets - A list of topics that must contain any topics used as targets in the links.
		 *		output - The StringBuilder that the output will be appended to.
		 *		embeddedTopics - A list of topics that contains any embedded topics contained in this one.
		 *		embeddedTopicIndex - The index into embeddedTopics to start at.
		 *		extraClass - If specified, this string will be added to the CTopic div as an extra CSS class.
		 */
		public void AppendTopic (Topics.Topic topic, Context context, IList<Link> links, IList<Topics.Topic> linkTargets, StringBuilder output, 
											IList<Topics.Topic> embeddedTopics = null, int embeddedTopicIndex = 0, string extraClass = null)
			{
			try
				{

				// Setup

				context.Topic = topic;

				this.context = context;
				this.links = links;
				this.linkTargets = linkTargets;
				this.embeddedTopics = embeddedTopics;
				this.embeddedTopicIndex = embeddedTopicIndex;


				// Core

				string simpleCommentTypeName = EngineInstance.CommentTypes.FromID(topic.CommentTypeID).SimpleIdentifier;
				string simpleLanguageName = EngineInstance.Languages.FromID(topic.LanguageID).SimpleIdentifier;
				string topicHashPath = context.TopicOnlyHashPath;

				if (topicHashPath != null)
					{  output.Append("<a name=\"" + topicHashPath.EntityEncode() + "\"></a>");  }

				output.Append(
					"<a name=\"Topic" + topic.TopicID + "\"></a>" +
					"<div class=\"CTopic T" + simpleCommentTypeName + " L" + simpleLanguageName + 
										(extraClass == null ? "" : ' ' + extraClass) + "\">" +

						"\r\n ");
						AppendTitle(output);

						#if SHOW_NDMARKUP
							if (topic.Body != null)
								{
								htmlOutput.Append(
								"\r\n " +
								"<div class=\"CBodyNDMarkup\">" +
									topic.Body.ToHTML() +
								"</div>");
								}
						#endif

						if (topic.Prototype != null)
							{
							output.Append("\r\n ");
							AppendPrototype(output);
							}

						if (topic.Body != null)
							{
							output.Append("\r\n ");
							AppendBody(output);
							}

					output.Append(
					"\r\n" +
					"</div>"
					);
				}

			catch (Exception e)
				{
				// Build a message to show the topic we crashed on
				if (topic != null)
					{
					StringBuilder task = new StringBuilder();

					// Topic name
					if (string.IsNullOrEmpty(topic.Title) == false)
						{  task.Append("Building topic \"" + topic.Title + "\"");  }
					else
						{
						task.Append("Building topic ID " + topic.TopicID);

						if (topic.Title == null)
							{  task.Append(" (null title)");  }
						else // empty
							{  task.Append(" (empty string title)");  }
						}

					// File name
					if (topic.FileID > 0)
						{
						var file = EngineInstance.Files.FromID(topic.FileID);

						if (file != null)
							{  task.Append(" from file \"" + file.FileName + "\"");  }
						else
							{  task.Append(" from file ID " + topic.FileID);  }

						// Line number
						if (topic.CommentLineNumber > 0)
							{
							if (topic.CodeLineNumber != topic.CommentLineNumber && topic.CodeLineNumber > 0)
								{  task.Append(" lines " + topic.CommentLineNumber + " and " + topic.CodeLineNumber);  }
							else
								{  task.Append(" line " + topic.CommentLineNumber);  }
							}
						else if (topic.CodeLineNumber > 0)
							{  task.Append(" line " + topic.CodeLineNumber);  }
						}

					e.AddNaturalDocsTask(task.ToString());
					}

				throw;
				}

			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: AppendTitle
		 */
		protected void AppendTitle (StringBuilder output)
			{
			var commentType = EngineInstance.CommentTypes.FromID(context.Topic.CommentTypeID);

			WrappedTitleMode mode;
			
			if (commentType.Flags.File)
				{  mode = WrappedTitleMode.File;  }
			else if (commentType.Flags.Code)
				{  mode = WrappedTitleMode.Code;  }
			else
				{  mode = WrappedTitleMode.None;  }

			output.Append("<div class=\"CTitle\">");
			AppendWrappedTitle(context.Topic.Title, mode, output);
			output.Append("</div>");
			}


		/* Function: AppendPrototype
		 * Covers both prototypes and class prototypes.
		 */
		protected void AppendPrototype (StringBuilder output)
			{
			var commentType = EngineInstance.CommentTypes.FromID(context.Topic.CommentTypeID);
			bool builtPrototype = false;

			if (commentType.Flags.ClassHierarchy)
				{
				ParsedClassPrototype parsedClassPrototype = context.Topic.ParsedClassPrototype;

				if (parsedClassPrototype != null)
					{
					if (classPrototypeBuilder == null)
						{  classPrototypeBuilder = new HTML.Components.ClassPrototype(context);  }

					classPrototypeBuilder.AppendClassPrototype(parsedClassPrototype, context, false, output, links, linkTargets);

					builtPrototype = true;
					}
				}

			if (builtPrototype == false)
				{
				if (prototypeBuilder == null)
					{  prototypeBuilder = new HTML.Components.Prototype(context);  }

				prototypeBuilder.AppendPrototype(context.Topic.ParsedPrototype, context, output, links, linkTargets);
				}
			}


		/* Function: AppendBody
		 */
		protected void AppendBody (StringBuilder output)
			{
			output.Append("<div class=\"CBody\">");

			string body = context.Topic.Body;
			NDMarkup.Iterator iterator = new NDMarkup.Iterator(body);

			bool underParameterHeading = false;
			string parameterListSymbol = null;
			string altParameterListSymbol = null;

			while (iterator.IsInBounds)
				{
				switch (iterator.Type)
					{
					case NDMarkup.Iterator.ElementType.Text:

						// Preserve multiple whitespace chars, but skip the extra processing if there aren't any
						if (body.IndexOf("  ", iterator.RawTextIndex, iterator.Length) != -1)
							{  output.Append( iterator.String.ConvertMultipleWhitespaceChars() );  }
						else
							{  iterator.AppendTo(output);  }
						break;


					case NDMarkup.Iterator.ElementType.ParagraphTag:
					case NDMarkup.Iterator.ElementType.BulletListTag:
					case NDMarkup.Iterator.ElementType.BulletListItemTag:
					case NDMarkup.Iterator.ElementType.BoldTag:
					case NDMarkup.Iterator.ElementType.ItalicsTag:
					case NDMarkup.Iterator.ElementType.UnderlineTag:
					case NDMarkup.Iterator.ElementType.LTEntityChar:
					case NDMarkup.Iterator.ElementType.GTEntityChar:
					case NDMarkup.Iterator.ElementType.AmpEntityChar:
					case NDMarkup.Iterator.ElementType.QuoteEntityChar:

						// These the NDMarkup directly matches the HTML tags
						iterator.AppendTo(output);
						break;


					case NDMarkup.Iterator.ElementType.HeadingTag:

						if (iterator.IsOpeningTag)
							{  
							output.Append("<div class=\"CHeading\">");
							underParameterHeading = (iterator.Property("type") == "parameters");
							}
						else
							{  output.Append("</div>");  }
						break;


					case NDMarkup.Iterator.ElementType.PreTag:

						string preType = iterator.Property("type");
						string preLanguageName = iterator.Property("language");

						iterator.Next();
						NDMarkup.Iterator startOfCode = iterator;

						// Because we can assume the NDMarkup is valid, we can assume we were on an opening tag and that we will 
						// run into a closing tag before the end of the text.  We can also assume the next pre tag is a closing tag.

						while (iterator.Type != NDMarkup.Iterator.ElementType.PreTag)
							{  iterator.Next();  }

						string ndMarkupCode = body.Substring(startOfCode.RawTextIndex, iterator.RawTextIndex - startOfCode.RawTextIndex);
						string textCode = NDMarkupCodeToText(ndMarkupCode);

						output.Append("<pre>");

						if (preType == "code")
							{
							Languages.Language preLanguage = null;

							if (preLanguageName != null)
								{  
								// This can return null if the language name is unrecognized.
								preLanguage = EngineInstance.Languages.FromName(preLanguageName);  
								}

							if (preLanguage == null)
								{  preLanguage = EngineInstance.Languages.FromID(context.Topic.LanguageID);  }

							Tokenizer code = new Tokenizer(textCode, tabWidth: EngineInstance.Config.TabWidth);
							preLanguage.SyntaxHighlight(code);
							AppendSyntaxHighlightedText(code.FirstToken, code.LastToken, output);
							}
						else
							{  
							string htmlCode = textCode.EntityEncode();
							htmlCode = StringExtensions.LineBreakRegex.Replace(htmlCode, "<br />");
							output.Append(htmlCode);
							}

						output.Append("</pre>");
						break;


					case NDMarkup.Iterator.ElementType.DefinitionListTag:

						if (iterator.IsOpeningTag)
							{  output.Append("<table class=\"CDefinitionList\">");  }
						else
							{  output.Append("</table>");  }
						break;


					case NDMarkup.Iterator.ElementType.DefinitionListEntryTag:
					case NDMarkup.Iterator.ElementType.DefinitionListSymbolTag:

						if (iterator.IsOpeningTag)
							{  
							output.Append("<tr><td class=\"CDLEntry\">");
							parameterListSymbol = null;

							// Create anchors for symbols.  We are assuming there are enough embedded topics for each <ds>
							// tag and that they follow their parent topic in order.
							if (iterator.Type == NDMarkup.Iterator.ElementType.DefinitionListSymbolTag)
								{
								#if DEBUG
									if (embeddedTopics == null || embeddedTopicIndex >= embeddedTopics.Count ||
										 embeddedTopics[embeddedTopicIndex].IsEmbedded == false)
										{  throw new Exception ("There are not enough embedded topics to build the definition list.");  }
								#endif

								var embeddedTopic = embeddedTopics[embeddedTopicIndex];

								Context embeddedTopicContext = context;
								embeddedTopicContext.Topic = embeddedTopic;

								string embeddedTopicHashPath = embeddedTopicContext.TopicOnlyHashPath;

								if (embeddedTopicHashPath != null)
									{  output.Append("<a name=\"" + embeddedTopicHashPath.EntityEncode() + "\"></a>");  }

								output.Append("<a name=\"Topic" + embeddedTopic.TopicID + "\"></a>");

								embeddedTopicIndex++;
								}

							// If we're using a Parameters: heading, store the entry symbol in parameterListSymbol
							if (underParameterHeading)
								{
								NDMarkup.Iterator temp = iterator;
								temp.Next();

								StringBuilder symbol = new StringBuilder();

								while (temp.IsInBounds && 
											temp.Type != NDMarkup.Iterator.ElementType.DefinitionListEntryTag &&
											temp.Type != NDMarkup.Iterator.ElementType.DefinitionListSymbolTag)
									{
									if (temp.Type == NDMarkup.Iterator.ElementType.Text)
										{  temp.AppendTo(symbol);  }

									temp.Next();  
									}

								// If the entry name starts with any combination of $, @, or % characters, strip them off.
								int firstNonSymbolIndex = 0;
								while (firstNonSymbolIndex < symbol.Length)
									{
									char charAtIndex = symbol[firstNonSymbolIndex];

									if (charAtIndex != '$' && charAtIndex != '@' && charAtIndex != '%')
										{  break;  }

									firstNonSymbolIndex++;
									}

								if (symbol.Length > 0)
									{  parameterListSymbol = symbol.ToString();  }
								else
									{  parameterListSymbol = null;  }

								if (firstNonSymbolIndex > 0)
									{  
									symbol.Remove(0, firstNonSymbolIndex);
									altParameterListSymbol = symbol.ToString();
									}
								else
									{  altParameterListSymbol = null;  }

								}
							}
						else // closing tag
							{  
							// See if parameterListSymbol matches any of the prototype parameter names
							if ( (parameterListSymbol != null || altParameterListSymbol != null) && context.Topic.Prototype != null)
								{
								var parsedPrototype = context.Topic.ParsedPrototype;
								TokenIterator start, end;
								int matchedParameter = -1;

								for (int i = 0; i < parsedPrototype.NumberOfParameters; i++)	
									{
									parsedPrototype.GetParameterName(i, out start, out end);

									if ( (parameterListSymbol != null && parsedPrototype.Tokenizer.EqualsTextBetween(parameterListSymbol, true, start, end)) ||
										 (altParameterListSymbol != null && parsedPrototype.Tokenizer.EqualsTextBetween(altParameterListSymbol, true, start, end)) )
										{
										matchedParameter = i;
										break;
										}
									}

								// If so, include the type under the entry in the HTML
								if (matchedParameter != -1)
									{
									parsedPrototype.BuildFullParameterType(matchedParameter, out start, out end);

									if (start < end && 
										// Don't include single symbol types
										 !(end.RawTextIndex - start.RawTextIndex == 1 &&
										   (start.Character == '$' || start.Character == '@' || start.Character == '%')) )
										{
										output.Append("<div class=\"CDLParameterType\">");
										AppendSyntaxHighlightedTextWithTypeLinks(start, end, output, links, linkTargets);
										output.Append("</div>");
										}
									}
								}

							output.Append("</td>");
							}
						break;


					case NDMarkup.Iterator.ElementType.DefinitionListDefinitionTag:

						if (iterator.IsOpeningTag)
							{  output.Append("<td class=\"CDLDefinition\">");  }
						else
							{  output.Append("</td></tr>");  }
						break;


					case NDMarkup.Iterator.ElementType.LinkTag:

						string linkType = iterator.Property("type");

						if (linkType == "email")
							{  AppendEMailLink(iterator, output);  }
						else if (linkType == "url")
							{  AppendURLLink(iterator, output);  }
						else // type == "naturaldocs"
							{  AppendNaturalDocsLink(iterator, output);  }

						break;


					case NDMarkup.Iterator.ElementType.ImageTag: // xxx

						if (iterator.Property("type") == "standalone")
							{  output.Append("<p>");  }

						output.Append(iterator.Property("originaltext").ToHTML());

						if (iterator.Property("type") == "standalone")
							{  output.Append("</p>");  }
						break;
					}

				iterator.Next();
				}

			output.Append("</div>");
			}


		/* Function: AppendEMailLink
		 */
		protected void AppendEMailLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			string address = iterator.Property("target");
			int atIndex = address.IndexOf('@');
			int cutPoint1 = atIndex / 2;
			int cutPoint2 = (atIndex+1) + ((address.Length - (atIndex+1)) / 2);
			
			output.Append("<a href=\"#\" onclick=\"javascript:location.href='ma\\u0069'+'lto\\u003a'+'");
			output.Append( EMailSegmentForJavaScriptString( address.Substring(0, cutPoint1) ));
			output.Append("'+'");
			output.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint1, atIndex - cutPoint1) ));
			output.Append("'+'\\u0040'+'");
			output.Append( EMailSegmentForJavaScriptString( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
			output.Append("'+'");
			output.Append( EMailSegmentForJavaScriptString( address.Substring(cutPoint2, address.Length - cutPoint2) ));
			output.Append("';return false;\">");

			string text = iterator.Property("text");

			if (text != null)
				{  output.EntityEncodeAndAppend(text);  }
			else
				{
				output.Append( EMailSegmentForHTML( address.Substring(0, cutPoint1) ));
				output.Append("<span style=\"display: none\">[xxx]</span>");
				output.Append( EMailSegmentForHTML( address.Substring(cutPoint1, atIndex - cutPoint1) ));
				output.Append("<span>&#64;</span>");
				output.Append( EMailSegmentForHTML( address.Substring(atIndex + 1, cutPoint2 - (atIndex + 1)) ));
				output.Append("<span style=\"display: none\">[xxx]</span>");
				output.Append( EMailSegmentForHTML( address.Substring(cutPoint2, address.Length - cutPoint2) ));
				}

			output.Append("</a>");
			}

		/* Function: EMailSegmentForJavaScriptString
		 */
		protected string EMailSegmentForJavaScriptString (string segment)
			{
			segment = segment.StringEscape();
			segment = segment.Replace(".", "\\u002e");
			return segment;
			}

		/* Function: EMailSegmentForHTML
		 */
		protected string EMailSegmentForHTML (string segment)
			{
			segment = segment.EntityEncode();
			segment = segment.Replace(".", "&#46;");
			return segment;
			}

		/* Function: AppendURLLink
		 */
		protected void AppendURLLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			string target = iterator.Property("target");

			output.Append("<a href=\"");
				output.EntityEncodeAndAppend(target);
			output.Append("\" target=\"_top\">");

			string text = iterator.Property("text");

			if (text != null)
				{  output.EntityEncodeAndAppend(text);  }
			else
				{
				int startIndex = 0;
				int breakIndex;

				// Skip the protocol and any following slashes since we don't want a break after every slash in http:// or
				// file:///.

				int endOfProtocolIndex = target.IndexOf(':');

				if (endOfProtocolIndex != -1)
					{
					do
						{  endOfProtocolIndex++;  }
					while (endOfProtocolIndex < target.Length && target[endOfProtocolIndex] == '/');

					output.EntityEncodeAndAppend( target.Substring(0, endOfProtocolIndex) );
					output.Append("&#8203;");  // Zero width space
					startIndex = endOfProtocolIndex;
					}

				for (;;)
					{
					breakIndex = target.IndexOfAny(BreakURLCharacters, startIndex);

					if (breakIndex == -1)
						{
						if (target.Length - startIndex > MaxUnbrokenURLCharacters)
							{  breakIndex = startIndex + MaxUnbrokenURLCharacters;  }
						else
							{  break;  }
						}
					else if (breakIndex - startIndex > MaxUnbrokenURLCharacters)
						{  breakIndex = startIndex + MaxUnbrokenURLCharacters;  }

					output.EntityEncodeAndAppend( target.Substring(startIndex, breakIndex - startIndex) );
					output.Append("&#8203;");  // Zero width space
					output.EntityEncodeAndAppend(target[breakIndex]);

					startIndex = breakIndex + 1;
					}

				output.EntityEncodeAndAppend( target.Substring(startIndex) );
				}

			output.Append("</a>");
			}


		/* Function: AppendNaturalDocsLink
		 */
		protected void AppendNaturalDocsLink (NDMarkup.Iterator iterator, StringBuilder output)
			{
			// Create a link object with the identifying properties needed to look it up in the list of links.

			Link linkStub = new Link();
			linkStub.Type = LinkType.NaturalDocs;
			linkStub.Text = iterator.Property("originaltext");
			linkStub.Context = context.Topic.BodyContext;
			linkStub.ContextID = context.Topic.BodyContextID;
			linkStub.FileID = context.Topic.FileID;
			linkStub.ClassString = context.Topic.ClassString;
			linkStub.ClassID = context.Topic.ClassID;
			linkStub.LanguageID = context.Topic.LanguageID;


			// Find the actual link so we know if it resolved to anything.

			Link fullLink = null;

			foreach (Link link in links)
				{
				if (link.SameIdentifyingPropertiesAs(linkStub))
					{
					fullLink = link;
					break;
					}
				}

			#if DEBUG
			if (fullLink == null)
				{  throw new Exception("All links in a topic must be in the list passed to HTMLTopic.");  }
			#endif


			// If it didn't resolve, we just output the original text and we're done.

			if (!fullLink.IsResolved)
				{
				output.EntityEncodeAndAppend(iterator.Property("originaltext"));
				return;
				}


			// If it did resolve, find the interpretation that was used.  If it was a named link it would affect the link text.

			LinkInterpretation linkInterpretation = null;

			string ignore;
			List<LinkInterpretation> linkInterpretations = EngineInstance.Comments.NaturalDocsParser.LinkInterpretations(fullLink.Text,
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowNamedLinks |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.AllowPluralsAndPossessives |
																					  Comments.Parsers.NaturalDocs.LinkInterpretationFlags.FromOriginalText,
																					  out ignore);

			linkInterpretation = linkInterpretations[ fullLink.TargetInterpretationIndex ];


			// Find the Topic it resolved to.

			Topics.Topic targetTopic = null;

			foreach (var linkTarget in linkTargets)
				{
				if (linkTarget.TopicID == fullLink.TargetTopicID)
					{
					targetTopic = linkTarget;
					break;
					}
				}

			#if DEBUG
			if (targetTopic == null)
				{  throw new Exception("All links targets for a topic must be in the list passed to HTMLTopic.");  }
			#endif

			AppendOpeningLinkTag(targetTopic, output);
			output.EntityEncodeAndAppend(linkInterpretation.Text);
			output.Append("</a>");
			}


		/* Function: NDMarkupCodeToText
		 * Converts code sections in <NDMarkup> back to plain text, decoding entity chars and converting line breaks
		 * to \n.
		 */
		protected string NDMarkupCodeToText (string input)
			{
			string output = input.Replace("<br>", "\n");
			output = output.EntityDecode();
			return output;
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: links
		 * A list of <Links> that contain any which will appear in the prototype, or null if links aren't needed.
		 */
		protected IList<Link> links;

		/* var: linkTargets
		 * A list of topics that contain the targets of any resolved links appearing in <links>, or null if links aren't needed.
		 */
		protected IList<Topics.Topic> linkTargets;

		/* var: embeddedTopics
		 * A list of topics that contains any that are embedded in the one we are building.
		 */
		protected IList<Topics.Topic> embeddedTopics;

		/* var: embeddedTopicIndex
		 * The entry in <embeddedTopics> where the next one resides.
		 */
		protected int embeddedTopicIndex;

		/* var: prototypeBuilder
		 * An object for building prototypes, or null if one hasn't been created yet.  Since this
		 * class can be reused to build multiple topics, and these objects can be reused to build
		 * multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected HTML.Components.Prototype prototypeBuilder;

		/* var: classPrototypeBuilder
		 * An object for building class prototypes, or null if one hasn't been created yet.  Since this
		 * class can be reused to build multiple topics, and these objects can be reused to build
		 * multiple prototypes, one is stored with the class so it can be reused between runs.
		 */
		protected HTML.Components.ClassPrototype classPrototypeBuilder;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: breakURLCharacters
		 * An array of characters that cause an inline URL to wrap.
		 */
		static protected char[] BreakURLCharacters = { '.', '/', '#', '?', '&' };

		/* var: maxUnbrokenURLCharacters
		 * The longest stretch between <breakURLCharacters> that can occur unbroken in an inline URL.  Formatting attempts
		 * to break on those characters as it looks cleaner, but this limit forces it to happen if they don't occur.
		 */
		protected const int MaxUnbrokenURLCharacters = 35;

		}
	}

