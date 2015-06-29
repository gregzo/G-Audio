----------------------------------------------
         G-Audio : 2D Audio Framework
       Copyright © 2014 Gregorio Zanon
            Version 1.0
     support: basak.gregorio@gmail.com
----------------------------------------------

----------------------------------------------
------------ Init Example Scene --------------
----------------------------------------------


***************************************
************ What's Happening? ********

-An empty scene that serves one purpose:
to set the output sample rate according
to the runtime platform. This needs to be 
done if requesting a custom sample rate,
for example 44.1khz on iOS. 


***************************************
*********** Things To Notice **********

-The scene only contains one script:
GATAudioInit.cs. It attempts to set the
sample rate at the requested value, and
updates GATInfo to register that a sample rate
change request has been made. Level 1 is then
loaded in Start().


**********************************************
*********** Things You Should Know ***********

-Not all platforms support all sample rates. You
should get a warning if the samplerate you request
using GATAudioInit.cs is not available. The warning
will happen once GATManager loads.
 







