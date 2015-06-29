----------------------------------------------
                 G-Audio 
       Copyright © 2014 Gregorio Zanon
          www.G-Audio-Unity.com
        support@g-audio-unity.com
----------------------------------------------

---------------------------------------
		 Looper Scene Notes
---------------------------------------

This scene uses the new I/O components to
build a simple looper.
_____________________________
On the Microphone GameObject:

-MicrophoneModule handles mic configuration and potential
interruptions, and broadcasts a stereo audio stream 

-StreamSplitterModule de-interleaves the microphone stream
( Unity only supports mono microphone input, it seems... )
and broadcasts 2 mono streams

-StreamToTrackModule optionally routes one of the splitter's 
mono streams to the specified track ( if the user selects 
mic to track option ).

-StreamToCacheModule routes one of the splitter's 
mono streams to cache when the user presses rec. Caches are
updated in script( Example_05.cs ) when the user switches track.

___________________________
On the GATPlayer GameObject:

-StreamToWavModule writes the player's stereo stream to disk.