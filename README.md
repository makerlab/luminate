This is a 3d drawing program for mobile by http://twitter.com/anselm

It is a project I started summer 2013. It is a work in progress - here are my notes at the moment:

Things to fix up a bit better oct 2014

<- really need to closely verify that minimum point feature distance especially for tubes!!
< - almost certainly need do massively reduce point count in tubes and in general; things need to space out more
<- will defininitely need smoothing for tubes; some kind of bicubic or catmull

<- revise ui

	[] swatch mode
	[] ribbon mode
	[] tube mode
	[] erase mode
	[] drawing plane set mode
	[] sun mode
	[] track
	() finger free down area

	< button animation improve
	< button indicate current mode
	< button color area scroll for more options?
	< button better art to clearly indicate role of each button

	< we need a main menu ui

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

	- actually make save work
	- do we need a clear button? maybe not ( can just undo ? )
	- there is an idea of 5 second or 10 second drawing games
	- multiplayer support
	- being able to upload your things to the web or somewhere and share them with webgl on some kind of website i offer
	- actually having a main menu show everything

To release

	- write up
	- make a video
	- bug tracker
	- clean up source
	- hashtag
	- domain


