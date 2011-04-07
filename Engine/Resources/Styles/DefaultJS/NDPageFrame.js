﻿/*
	Include in output:

	This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
	Natural Docs is licensed under version 3 of the GNU Affero General Public
	License (AGPL).  Refer to License.txt for the complete details.

	This file may be distributed with documentation files generated by Natural Docs.
	Such documentation is not covered by Natural Docs' copyright and licensing,
	and may have its own copyright and distribution terms as decided by its author.

	This file includes code derived from jQuery HashChange Event, which is 
	Copyright © 2010 "Cowboy" Ben Alman.  jQuery HashChange Event may be 
	obtained separately under the MIT license or the GNU General Public License (GPL).  
	However, this combined product is still licensed under the terms of the AGPLv3.

*/


/* Class: NDPageFrame
	 _____________________________________________________________________________

*/
var NDPageFrame = new function ()
	{

	// Group: Functions
	// ________________________________________________________________________


	/* Function: Start
	*/
	this.Start = function ()
		{
		// The default title of the page is the project title.  Save a copy before we mess with it.
		this.projectTitle = document.title;

		// this.hashChangePoller = undefined;

		if (navigator.userAgent.indexOf("MSIE 6") != -1)
			{
			// IE 6 doesn't like fixed positioning the way other browsers do.

			document.getElementsByTagName("html")[0].style.overflow = "hidden";  

			document.getElementById("NDHeader").style.position = "absolute";
			document.getElementById("NDFooter").style.position = "absolute";
			document.getElementById("NDMenu").style.position = "absolute";
			document.getElementById("NDContent").style.position = "absolute";
			document.getElementById("NDMessages").style.position = "absolute";
			}

		this.OnResize();
		window.onresize = this.OnResize;

		NDMenu.Start();

		this.AddHashChangeHandler();
		this.OnHashChange();
		};


	/* Function: OnResize
	*/
	this.OnResize = function ()
		{
		var width = NDCore.WindowClientWidth();
		var height = NDCore.WindowClientHeight();

		var header = document.getElementById("NDHeader");
		var footer = document.getElementById("NDFooter");
		var menu = document.getElementById("NDMenu");
		var content = document.getElementById("NDContent");
		var messages = document.getElementById("NDMessages");

		NDCore.SetToAbsolutePosition(header, 0, 0, width, undefined);
		NDCore.SetToAbsolutePosition(footer, 0, undefined, width, undefined);

		var headerHeight = header.offsetHeight;
		var footerHeight = footer.offsetHeight;

		// We needed separate calls to set the footer's Y position and width since wrapping may change its height.
		NDCore.SetToAbsolutePosition(footer, undefined, height - footerHeight, undefined, undefined);

		var menuWidth = menu.offsetWidth;

		NDCore.SetToAbsolutePosition(menu, 0, headerHeight, menuWidth, height - headerHeight - footerHeight);
		NDCore.SetToAbsolutePosition(content, menuWidth, headerHeight, width - menuWidth, height - headerHeight - footerHeight);

		NDCore.SetToAbsolutePosition(messages, menuWidth, 0, width - menuWidth, undefined);

		content.innerHTML = 
			"Page Width: " + width + "<br>" +
			"header Width (style): " + header.style.width + "<br>" +
			"<b>header Width (offsetWidth): " + header.offsetWidth + "</b><br>" +
			"header Width (clientWidth): " + header.clientWidth + "<br>" +
			"header Width (scrollWidth): " + header.scrollWidth + "<br>";

		};


	/* Function: Message
		Posts a message on the screen.
	*/
	this.Message = function (message)
		{
		var htmlEntry = document.createElement("div");
		htmlEntry.className = "MsgMessage";

		var htmlMessage = document.createTextNode(message);
		htmlEntry.appendChild(htmlMessage);

		document.getElementById("MsgContent").appendChild(htmlEntry);
		document.getElementById("NDMessages").style.display = "block";
		this.OnResize();
		};

	
	/* Function: CloseMessages
	*/
	this.CloseMessages = function ()
		{
		document.getElementById("NDMessages").style.display = "none";
		document.getElementById("MsgContent").innerHTML = "";
		};


	/* Function: OnHashChange
	*/
	this.OnHashChange = function ()
		{
		// Strip the hash symbol and everything before.  If there's no hash symbol, indexOf returning -1 means substr
		// will return the whole thing.
		var hash = location.hash.substr(location.hash.indexOf("#") + 1);
		
		NDMenu.GoToFilePath(hash);
		};


	/* Function: UpdatePageTitle
	*/
	this.UpdatePageTitle = function (pageTitle)
		{
		if (pageTitle)
			{  document.title = pageTitle + " - " + this.projectTitle;  }
		else
			{  document.title = this.projectTitle;  }
		};


	/* Function: AddHashChangeHandler
		Sets up <OnHashChange()> to be called whenever a hash change event occurs.  Based on jQuery HashChange
		Event because not all browsers support window.onhashchange.
	*/
	this.AddHashChangeHandler = function ()
		{
		// If the browser supports onhashchange...

		// Note that IE8 running in IE7 compatibility mode reports true for "onhashchange" in window even
		// though the event isn't supported, so also test document.documentMode.
		if ("onhashchange" in window && (document.documentMode === undefined || document.documentMode > 7))
			{
			// If we don't do it this way the "this" parameter doesn't get set.
			window.onhashchange = function () {  NDPageFrame.OnHashChange();  };
			}

		// If browser doesn't support onhashchange...
		else
			{
			this.hashChangePoller = {
				// timeoutID: undefined,
				timeoutLength: 200,  // Every fifth of a second

				// Remember the initial hash so it doesn't get triggered immediately.
				lastHash: location.hash
				};

			// Non-IE browsers that don't support onhashchange can use a straightforward polling loop of the hash.
			if (navigator.userAgent.indexOf("MSIE") == -1)
				{
				this.hashChangePoller.Start = function ()
					{  
					this.Poll();  
					};

				this.hashChangePoller.Stop = function ()
					{
					if (this.timeoutID != undefined)
						{  
						clearTimeout(this.timeoutID);  
						this.timeoutID = undefined;
						}
					};

				this.hashChangePoller.Poll = function ()
					{
					if (!NDCore.SameHash(location.hash, this.lastHash))
						{  
						this.lastHash = location.hash;
						NDPageFrame.OnHashChange();  
						}

					this.timeoutID = setTimeout("NDPageFrame.hashChangePoller.Poll()", this.timeoutLength);
					};
				}

			else  // IE
				{
				// Not only do IE6/7 need the "magical" iframe treatment, but so does IE8
				// when running in IE7 compatibility mode.

				this.hashChangePoller.Start = function ()
					{
					// Create a hidden iframe for history handling.
					var iframeElement = document.createElement("iframe");

					// Attempt to make it as hidden as possible by using techniques from 
					// http://www.paciellogroup.com/blog/?p=604
					with (iframeElement)
						{
						title = "empty";
						tabindex = -1;
						style.display = "none";
						width = 0;
						height = 0;
						src = "javascript:0";
						}

					this.firstRun = true;

					iframeElement.attachEvent("onload", 
						function () 
							{
							if (NDPageFrame.hashChangePoller.firstRun)
								{
								NDPageFrame.hashChangePoller.firstRun = false;
								NDPageFrame.hashChangePoller.SetHistory(location.hash);  

								NDPageFrame.hashChangePoller.Poll();
								}
							}
						);

					// jQuery HashChange Event does some stuff I'm not 100% clear on to "append iframe after 
					// the end of the body to prevent unnecessary initial page scrolling (yes, this works)."  Bah, 
					// screw it, let's just go with straightforward.
					document.body.appendChild(iframeElement);

					this.iframe = iframeElement.contentWindow;

					// Whenever the document.title changes, update the iframe's title to
					// prettify the back/next history menu entries.  Since IE sometimes
					// errors with "Unspecified error" the very first time this is set
					// (yes, very useful) wrap this with a try/catch block.
					document.onpropertychange = function () 
						{
						if (event.propertyName == "title") 
							{  
							try 
								{  NDPageFrame.hashChangePoller.iframe.document.title = document.title;  }
							catch(e)
								{  }
							}
						};
					};

				// No Stop method since an IE6/7 iframe was created.  Even
				// without an event handler the polling loop would still be necessary 
				// for back/next to work at all!
				this.hashChangePoller.Stop = function () { };


				this.hashChangePoller.Poll = function ()
					{
					var hash = location.hash;
					var historyHash = this.GetHistory();

					// If location.hash changed, which covers mouse clicks and manual editing
					if (!NDCore.SameHash(hash, this.lastHash))
						{
						this.lastHash = location.hash;
						this.SetHistory(hash, historyHash);
						NDPageFrame.OnHashChange();  
						}

					// If historyHash changed, which covers back and forward buttons
					else if (!NDCore.SameHash(historyHash, this.lastHash))
						{
						location.href = location.href.replace( /#.*/, '' ) + historyHash;
						}

					this.timeoutID = setTimeout("NDPageFrame.hashChangePoller.Poll()", this.timeoutLength);
					};

				this.hashChangePoller.GetHistory = function ()
					{
					return this.iframe.location.hash;
					};

				this.hashChangePoller.SetHistory = function (hash, historyHash)
					{
					if (!NDCore.SameHash(hash, historyHash))
						{
						with (this.iframe.document)
							{
							// Update iframe with any initial document.title that might be set.
							title = document.title;

							// Opening the iframe's document after it has been closed is what
							// actually adds a history entry.
							open();

							close();
							}

						// Update the iframe's hash, for great justice.
						this.iframe.location.hash = hash;
						}
					};
				}

			this.hashChangePoller.Start();
			}
		};



	// Group: Variables
	// ________________________________________________________________________

	/* var: projectTitle
		The project title in HTML.
	*/

	/* var: hashChangePoller
		An object to assist with hash change polling on browsers that don't support onhashchange.  Only used in
		<AddHashChangeHandler()>.
	*/

	};