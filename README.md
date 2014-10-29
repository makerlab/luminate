====================
Painting With Light
====================
Oct 20 2014

===========================
Reaching me and helping out
===========================

I'm on twitter as http://twitter.com/anselm and anselm@hook.org and anselm@gmail.com. I'd like collaborators in this early stage - please feel free to contact me.

I'd like to see what you can draw with this and what improvements you'd like to make. I may hire a couple of people for short term contracts to improve this but the work will remain open source.

=========
Compiling
=========

You'll need Unity Pro (I believe). I build this under Unity on the Mac and then run it through XCode. There are some missing libraries in XCode so I tend to build on top of the supplied XCode project. You may be able to just build the working example from XCode without Unity.

=======
Usage
=======

On the left side is a palette of colors. You can pick a color with your finger.

Also on the left side but inwards a bit are two more buttons. These select which texture you are using. There are only two textures.

On the right side is a palette of brush tips and commands. Since I don't have custom icons yet you have to guess which one is which. This is the order from top to bottom:

	1) Swatch - select to start drawing a single quad
	2) Ribbon - select to start drawing with a pretty 3d ribbon
	3) Tube   - select to start drawing with a 3d tube like toothpaste
	4) Erase  - erase last (you can also shake the phone to erase)
	5) Sun    - move the sun in the sky
	6) Save   - does nothing
	7) Track  - set your 3d area of interest (first thing you should do).

The first thing you should do is select the Track button. The way I would do this is I would look around the real world and find something such as a book cover or a messy corner - something that has a lot of nice visual contrast. Then I would aim my phone at this such that the app camera view is centered on this real world area. Then I would push the track button. Now you should see a bunch of dots and lines appear. What you want to do is keep holding the phone steady for a second, giving the computer time to "see" that piece of the world. After a second or two you should slide the phone horizontally as if it was on a ruler. A good way to do this is to step sideways while holding the phone. You actually do not want to "rotate" the phone. If you do a good job the computer will draw horizontal lines that show how features moved across its field of view. By helping the computer get some "parallax" on what it is looking at it can now start to do a 3d reconstruction of that part of your real world area. After you fiddle with this a bit you'll figure out how to do it best. Sometimes I do it from the top - facing down at a book - sometimes I do it sideways looking out at something in the real world. Once this process is done you will see a tiny 3d box floating in the world. This is where the computer thinks the center of the drawing area is. You will *also* see a 3d ball that is always in the center of the screen. This is your 3d brush tip.

Once you have done tracking you can now "paint". There are two ways to paint:

1) You can paint by touching the screen with your finger and moving it around. You are painting on a virtual plane about a foot in front of your camera phone.

2) In the bottom right corner below the tracking icon is an invisible button. If you press there with your finger then I assume your finger is in the middle of the screen. This lets you paint without your finger obscuring your own painting actions (because your finger is not transparent). In this mode you paint by moving the phone in 3d.

Some general tips are that it is easier to paint by moving the phone, and it is hard to get back exactly to where you were before in 3d so some planning is required. You can get some hints about where you are by moving the pen tip in 3d space and seeing what it collides with. I also find that using the swatch to lay down a ground plane helps. As well in the current design the ribbon tip has a hard shadow and this can also help ground your work (there are two separate shadow systems right now - ordinary occlusion based shadows and then separately a hard shadow on the ribbon).

==========
Issues
==========

Small Bugs and things to fix up a bit better oct 2014

	- i noticed that i broke the uv somehow for the swatch - fix
	- finger free area needs to be more clearly deliniated
	- really need to closely verify that minimum point feature distance especially for tubes!!
	- almost certainly need do massively reduce point count in tubes and in general; things need to space out more
	- will defininitely need smoothing for tubes; some kind of bicubic or catmull
	- reintroduce idea of setting a tracking plane
	- make undo an active element
	- button better art for down
	- button art indicate role types
	- we need a main menu ui

One design challenge is having a sense of control and high fidelity - how can we improve that user experience so this is really useful and not a toy?

	- gridding or snap to nearest line?
	- arguably we could have a freezeable drawing plane not always orthogonal to the camera....? thoughts?	
	- maybe we could show the drawing plane as a semi-transparent div while drawing so that it helps you guage context?
	( i tried this before and it basically was just a box on the screen facing the camera so it was boring... try again?)			
	< having a ball shadow on the ground would help

Improve the drawing and aspects of drawing with more styles and more fidelity

	< test having delauny on every frame
	< test wider minimum gap between segments
	< line smoothing
	< multi segment swatches?
	< smaller swatches? somehow control the width?

	- Try a minecraft block style
	- Smoke, Fog, Lights, Sparklers
	- And ribbon width should vary by speed
	- should ribbon rotate to face direction of travel?
	- should i do 2d bevels? for example what if you want to draw ribbons on a plane?

Another design issue is simply having more colors

	- Are we happy with hard shadows on? should we turn on ssao etc?
	- maybe semi transparent paint color is a good idea? so we have a kind of neon tube spray paint or something?
	- maybe we can try textured paints?
	- can we get a real multi pass bloom shader effect working on mobile?
	- is vectrosity worth looking at again to see if they've dealt with 3d lines better that before?

Before release

	- actually make save button work
	- do we need a clear button? maybe not ( can just undo ? )
	- there is an idea of 5 second or 10 second drawing games
	- multiplayer support
	- being able to upload your things to the web or somewhere and share them with webgl on some kind of website i offer
	- actually having a main menu show everything

To release as an app:

	- do a better write up
	- make a video
	- bug tracker

