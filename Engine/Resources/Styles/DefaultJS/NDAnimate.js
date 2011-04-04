/*
   Include in output:

   This file is part of Natural Docs, which is Copyright � 2003-2011 Greg Valure.
   Natural Docs is licensed under version 3 of the GNU Affero General Public
   License (AGPL).  Refer to License.txt for the complete details.

   This file may be distributed with documentation files generated by Natural Docs.
   Such documentation is not covered by Natural Docs' copyright and licensing,
   and may have its own copyright and distribution terms as decided by its author.

*/


/* Class: NDAnimate
	___________________________________________________________________________

    A set of functions for simple animations.
*/
var NDAnimate = new function ()
	{

	// Group: Properties
	// ________________________________________________________________________


	/* Property: enabled
		If set to false, no animations will be performed.  Any call to <Do()> will result in the progress handler
		being called immediately at 100% and then the result handler.
	*/
	this.enabled = true;

	/* Property: framesPerSecond
		How smoothly the animation should perform, ideally.  Whether this FPS is actually achieved depends on the
		speed of the browser and the complexity of the animation.
	*/
	this.framesPerSecond = 30;



	// Group: Functions
	// ________________________________________________________________________


	/* Function: FadeIn
	*/
	this.FadeIn = function (element, duration)
		{
		var state = new NDAnimate_AnimationState();

		state.targetElement = element;
		state.duration = duration;
		state.progressFunction = function (progress) { NDAnimate.SetOpacity(element, progress); };

		this.Start(state);
		};

	/* Function: FadeOut
	*/
	this.FadeOut = function (element, duration)
		{
		var state = new NDAnimate_AnimationState();

		state.targetElement = element;
		state.duration = duration;
		state.countDown = true;
		state.progressFunction = function (progress) { NDAnimate.SetOpacity(element, progress); };

		this.Start(state);
		};



	// Group: General Support Functions
	// ________________________________________________________________________


	/* Function: Start
		Begins an arbitrary <NDAnimate_AnimationState>.
	*/
	this.Start = function (state)
		{
		if (this.enabled)
			{
			var animationIndex = -1;

			for (var i = 0; i < this.animations.length; i++)
				{
				if (this.animations[i] === undefined)
					{
					animationIndex = i;
					break;
					}
				}

			if (animationIndex == -1)
				{  this.animations.push(state);  }
			else
				{  this.animations[animationIndex] = state;  }

			this.SetTimeout();
			}

		else // animations disabled
			{
			if (state.countDown)
				{  state.progressFunction(0);  }
			else
				{  state.progressFunction(1);  }
			}
		};


	/* Function: SetTimeout
		Sets the animation timeout if one is not already running.
	*/
	this.SetTimeout = function ()
		{
		if (this.timeoutID === undefined)
			{
			var runningAnimations = false;

			for (var i = 0; i < this.animations.length; i++)
				{
				if (this.animations[i] !== undefined)
					{
					runningAnimations = true;
					break;
					}
				}

			if (runningAnimations == false)
				{  return;  }

			var ms = Math.ceil(1000 / this.framesPerSecond);

			// Sanity check
			if (ms < 10)
				{  ms = 10;  }

			this.timeoutID = setInterval("NDAnimate.Update()", ms);
			}
		};


	/* Function: Update
		Called periodically via timeout to update all animations.
	*/
	this.Update = function ()
		{
		var time = new Date().valueOf();

		for (var i = 0; i < this.animations.length; i++)
			{
			if (this.animations[i] !== undefined)
				{
				var state = this.animations[i];
				var progress;

				if (time - state.startTime >= state.duration)
					{  progress = 1;  }
				else
					{  progress = (time - state.startTime) / state.duration;  }

				var done = (progress == 1);

				if (state.countDown == true)
					{  progress = 1 - progress;  }

				state.progressFunction(progress);

				if (done)
					{  this.animations[i] = undefined;  }
				}
			}

		clearTimeout(this.timeoutID);
		this.timeoutID = undefined;
		this.SetTimeout();
		};


	/* Function: SetOpacity
		Sets the opacity of the passed element in a browser-independent way.  Opacity is a float that ranges from
		0 for completely transparent to 1 for completely opaque.
	*/
	this.SetOpacity = function (targetElement, opacity) 
		{ 
		var ieVersion = NDCore.IEVersion();

		if (ieVersion === undefined || ieVersion >= 9)
			{
			targetElement.style.opacity = opacity;
			}
		else if (ieVersion == 8)
			{
			targetElement.style.filter = "progid:DXImageTransform.Microsoft.Alpha(Opacity=" + Math.ceil(opacity * 100) + ")";
			}
		else  // (ieVersion <= 7)  This covers IE8 running in IE7 compatibility mode
			{
			if (ieVersion == 6 && opacity == 1)
				{
				// When the opacity filter is set on IE6 and ClearType is on, it renders text against a black background
				// giving it annoying fringes.  When the animation is done, remove the filter to make it render properly
				// again.  It's still distracting during the animation but the end result looks decent at least.

				targetElement.style.filter = undefined;

				// We don't do this for IE 7 and 8 because they instead render the text without ClearType on.  That kind of
				// sucks, especially since they render large fonts with non-ClearType anti-aliasing so they should be
				// capable of doing it.  Anyway, the lack of fringing means you get a smooth animation and end point, and
				// it's turning ClearType back on at the end that makes it distracting.  Since we care about IE8 we'll sacrifice
				// ideal text clarity for not having a broken-looking jump at the end of the animation.  IE9 doesn't have any
				// of these issues.
				}
			else
				{
				// Forces hasLayout, which allows this to work.
				// http://www.satzansatz.de/cssd/onhavinglayout.html
				targetElement.style.zoom = 1;

				targetElement.style.filter = "alpha(opacity=" + Math.ceil(opacity * 100) + ")";
				}
			}
		};


	
	// Group: Variables
	// ________________________________________________________________________


	/* var: animations
		An array of <NDAnimate_AnimationStates> currently in progress.  They are referred to by their index, 
		and after they are completed their entry reverts to undefined.  This is for internal use only.
	*/
	this.animations = [ ];

	/* var: timeoutID
	*/
	// this.timeoutID = undefined;
	
	};



/* Class: NDAnimate_AnimationState
	___________________________________________________________________________

    A class containing the state data of an in-progress animation.
*/
function NDAnimate_AnimationState ()
	{

	// Group: Properties
	// ________________________________________________________________________

	/* Property: duration
		The amount of time the animation should run in milliseconds.
	*/
	this.duration = 1000;

	/* Property: progressFunction
		The function that will be called periodically to update DOM elements to correspond to the animation's
		progress.

		Parameters:

			progress - A floating point value from 0 to 1 representing the animation's progress.  This function is
							guaranteed to be called with 1 as the value.  There is no guarantee on how often it will be
							called for intermediate states, and may not be called at all for anything but 1.

							If <countDown> is set, it will be called from 1 to 0 with 0 being the only guaranteed call.
	*/

	/* Property: countDown
		If true, <progressFunction> counts from 1 to 0 instead of 0 to 1.
	*/
	this.countDown = false;

	/* Property: targetElement
		The DOM element the animation is acting upon.  This is only available as an efficiency for <progressFunction>
		and <completionFunction> so it does not have to be set if they don't use it.
	*/

	/* Property: startTime
		The start time in milliseconds as returned by Date.valueOf().  Defaults to when this class was created.
	*/
	this.startTime = new Date().valueOf();

	};