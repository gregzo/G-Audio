----------------------------------------------
                 G-Audio 
       Copyright © 2014-2015 Gregorio Zanon
               Version 1.36
          www.G-Audio-Unity.com
        support@g-audio-unity.com
----------------------------------------------

Thank you for buying G-Audio!

Head to www.G-Audio-Unity.com for documentation, video tutorials and forums.

---------------------------------------
		 Release Notes
---------------------------------------

*** 1.36 ***
- Fixed mangled preprocessor directives

*** 1.35 ***

- Fixed GATPlayer edit mode errors when recompiling in Unity 5
- Updated asset description to discose lack of Windows Store compatibility

*** 1.34 ***

- Unity 5.0 related fixes

- Mobile platforms in Unity 5 no longer need the InitScene: sample rate can be set in the project settings.
The InitScene can still be used to request mic authorization.

- In Unity 5, compiling breaks audio in edit mode. Enter and exit play mode to restore. Proper fix coming soon.

*** 1.33 ***
- Fixed corrupted Demo Scene ( corrupted in 1.32 )
- Improved parsing of less common wav files following user request
- User request: added a method to schedule fade outs in GATRealTimeSample.

void ScheduleFadeOut( double fadeStartDspTime, double fadeDuration )
- schedule a fade before playback, or during playback.
- interrupt a fade early by calling FadeOutAndStop or ElegantStop.
- a scheduled fade is cancelled when playback ends.
- scheduled fades should be sample accurate, both in duration and scheduled start time.


*** 1.32 ***

- Compatibility fixes for Unity 5 beta:
	G-Audio Manager's "Supported Sample Rates" and "Speaker Mode Initialization" settings
	are no longer necessary and have been removed from the inspector.
	
*** 1.31 ***

Small release focusing on bug fixes.

- Fixed padding issue in some ogg files( OggFile.ReadNextChunk )

- Tracks now broadcast their stream even when muted.

- Fixed accessing streaming assets on iOS

- New speaker mode setting( GATManager inspector ): force stereo or align with the platform's 
driver caps. Quad and more should now work properly, both in the editor and in builds.

- New class: GATFilerParam - facilitates access in code to filter parameters.

- Reworked LFOFilterParam to make use of GATFilterParam. 
Also added comments to LFOFilterParam to better demonstrate G-Audio's fun custom attributes.

- Maybe fixed a rare editor bug where GATPlayer would lock in an irreversible fail state.

*** 1.3 ***

Lots of goodies in this version, mostly responding to user requests.
New methods and classes are fully documented, please take a look at the code
for more details.

___________________________
*--- GATRealtimeSample ---*

- Automatically fade in and out with sample accurate timing:
	FadesIn bool property and FadeInDuration double
	FadeOutAndStop method
	
- Now supports per sample filters:
	AddFilter, GetFilter and RemoveFilter methods
	ResetFilters to reset active filter states
	
- Now implements IDisposable
	Call Dispose() when you are done with an instance.
	If you plan to recycle, call SetData( null ) to make
	sure audio data is released.
	
- Optimized when pitch is 1d or -1d:
	Minimized resampling overhead for these cases.
	
______________________
*--- Sample Banks ---*

GATSampleBank and GATActiveSampleBank have been completely reworked 
to enable dynamic loading of samples, synchronously or asynchronously.
GATResamplingSampleBank is now obsolete as GATActiveSampleBank provides
the same functionnality. GATResamplingSampleBank will stay around as an empty
class extending GATActiveSampleBank for compatibility.

Processed samples caching has been decoupled from sample banks 
and is now handled by GATProcessedSamplesCache. 
GATActiveSampleBank automatically manages a cache.

New methods and properties( GATSampleBank ):
	int  NumberOfSamplesInBank
	void LoadSamplesNamed
	void LoadStreamingAssetsAsync
	void AddSample
	void RemoveSample
	
Async loading requires audio files to be placed in the StreamingAssets folder.
Only ogg and wav files are supported.

____________________
*--- Sound Bank ---*

Sound Banks now support files placed in the StreamingAssets folder.
Place your audio files in StreamingAssets if you would like to load them 
asynchronously. Loading from StreamingAssets is slower, but generates
less garbage than loading from resources as audio data is piped straight to
GATData, bypassing AudioClip completely. 

________________________
*--- GATAudioLoader ---*

A new singleton class which handles async or sync
loading of wav and ogg files to GATData objects.

If your samples are in a SoundBank, the Samplebank classes
have convenience methods for loading asynchronously. Otherwise,
you should interact directly with the GATAudioLoader singleton -
see GATAudioLoader.cs for documentation. 

_______________________
*--- AGATAudioFile ---*

Opening and reading user or StreamingAssets audio files has
never been easier! Only ogg and wav files are supported for now.

________________________
*--- LFOFilterParam ---*

Fun with filters! LFO any filter parameter with this easy to use
component.

_______________
*--- Other ---*

- The default GATDataAllocator is now instantiated only in play mode.
This should solve editor memory leaks. 

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

---------------------------------------
			Credits
---------------------------------------

- Vorbis decoding is possible thanks to NVorbis, many thanks
to all contributors of this project! Please credit appropriately
if you use ogg decoding features in your application.

------------ NVorbis ------------------
 		A Vorbis decoder in C#
  	Copyright © Andrew Ward 2014
  	http://nvorbis.codeplex.com
  	----------------------------

NVorbis is licensed under the MS-PL license, see __License.txt in the NVorbis folder.

	***

-The biquad filters in GATBiQuad.cs are 
heavily inspired by Nigel Redmon's 
C++ implementation which can be found here: 
http://www.earlevel.com/main/2012/11/26/biquad-c-source-code/

	***
	
-The FFT algorithm in FloatFFT.cs is by Gerry Beauregard, source code here:
http://gerrybeauregard.wordpress.com/2011/04/01/an-fft-in-c/

	***
	
-The GATSoundBank gizmo icon is by momentumdesignlab.com ( Creative Commons Attribution License )

	***
	
-The 3 Audial Manipulators filters are kindly provided by Atmospherium.

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

--------------------------------------
			Disclaimer
--------------------------------------

G-Audio's copyright holder and contributors decline all responsibility in any kind of damage,
material, financial, or auditive, resulting from appropriate or inappropriate use of the present product.

G-Audio requires one license per seat.

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

---------------------------------------
		Version History
---------------------------------------

'''''''''''''''''''''''
	*** 1.26 ***
'''''''''''''''''''''''

'''''''''''''''''''''''''''''''''''''''
	!!! IMPORTANT NOTICE !!!
'''''''''''''''''''''''''''''''''''''''
G-Audio now lives in the GAudio namespace.
Simply add "using G-Audio;" to all of your classes which reference G-Audio classes to fix errors.

As G-Audio grows and now counts more than a 100 classes, moving everything to namespaces is the best way
to avoid spamming intelisense, and to keep things tidy and organized.

Over time, specialized classes will move to nested namespaces ( filters to the GAudio.Filtering namespace, 
for example ). This will happen gradually, so that users are impacted as little as possible.

2 Nested namespaces are already present in this release:

1) GAudio.Examples, where all the scripts in the Examples folder live.
2) GAudio.Attributes, where you'll find nifty custom attributes, more below. 

Note that in order to preserve compatibility with Audial Fiters, the base classes for filtering 
and some attributes still are namespace orphans.

'''''''''''''''''''''''''''''''''''''''

_____________________
*--- G-Audio iOS ---*

- G-Audio is getting an iOS specific add-on, G-Audio iOS Toolkit.
It will kick off very cheap to thank G-Audio early adopters.
The initial release focuses on Dirac support: true pitch shifting and 
time stretching with one of the top algorithms out there.

____________________
*--- Attributes ---*

- 1.26 brings a set of custom attributes which enable exposing properties in the inspector.
The implementation leverages PropertyDrawer and PropertyAttribute to draw default Unity 
inspector controls, but re-routes getting and setting to the specified property or field.
Nesting is also supported.

The attributes are:
- BindedBoolProperty
- BindedIntProperty
- BindedFloatProperty
- BindedDoubleProperty

Classes which extend PropertyAttribute cannot be generic, hence one attribute per value type.

Example:

public class LovelyAttributes : Monobehaviour
{
	public float MyValue
	{ 
		get
		{
			Debug.Log( "Getting _myValue" );
			return _myValue;	
		} 
		set
		{
			Debug.Log( "Setting _myValue" );
			_myValue = value;
		}
	}
	
	[ SerializeField ]
	[ BindedFloatProperty( "MyValue", typeof( LovelyAttributes ) ) ] //pass target member path as string, and outer type.
	private float _myValue = 42f;
}

Nested example:

public class LovelyNest : MonoBehaviour
{	
	[ SerializeField ]
	[ BindedIntProperty( "_intOwner.MyInt", typeof( LovelyNest ) ) ] //this works too! 
	private int _myNestedInt;
	
	[ SerializeField ]
	[ HideInInspector ]
	private ProudIntOwner _intOwner;
	
	[ System.Serializable ]
	class ProudIntOwner
	{
		public int MyInt
		{
			get
			{ 
				Debug.Log( "Look at _myInt!" );
				return _myInt;
			}
			set
			{
				Debug.Log( "What a nice int! " );
				_myInt = value;
			}
		}
		
		[ SerializeField ]
		int _myInt = 42;
	}
}

Note that class ProudIntOwner does not need to be a nested class,
and that the binded property's target can be a field as well as a property.


_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

'''''''''''''''''''''''
	*** 1.25 ***
'''''''''''''''''''''''

- Fixed a crash when allocating caches in start in StreamToCacheModule
- Fixed out of range exceptions when using GATRealTimeSample with unmanaged data
- Fixed a bug in FFTModule where if output was set to real, the imaginary part of the fft wasn't cleared.
_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

'''''''''''''''''''''''
	*** 1.24 ***
'''''''''''''''''''''''

__________________
*--- Playback ---*

- All Play methods of GATPlayer and IGATProcessedSample now return an IGATBufferedSampleOptions
object you can use to schedule an early, faded stop of the sample. This is especially useful in
sequencer like applications: no extra data is allocated, fading occurs at buffer level.
Example : GATManager.DefaultPlayer.PlayData( data, 0 ).SetEnd( 44100, 10000 ); //will only play the first 44100 samples, and fade the last 10000.

- GATRealTimeSample has been completely reworked:
	* Supports negative pitches for reverse playback
	* Is now loopable, whatever the pitch
	* Supports filters
	* Supports scheduled end via the MaxDuration parameter
	* Fires a loop callback you may subscribe to via the SetLoopCallback method
	* Is recyclable ( use the SetData method to change the data it wraps )
	* Supports Seeking ( Seek method )
	* Implements IDisposable - call Dispose when you're sure you don't need the instance anymore,
	  or suffer the overhead of having the finalizer call it for you.
	  
	In addition, the AGATWrappedSample.Status enum has been changed. States are now ReadyToPlay, Scheduled and Playing. 
	State can be queried via the PlayingStatus property.
	
	Note that negative pitches require the StartPosition property to be set at a non 0 value, or playback will stop immediately.
	Looping negative pitches should work fine. 
	
	If you do not need pitch shifting or filtering of single samples, you should use the GATLoopedSample class which is more 
	optimized for simple uses.
	
- Fix: GATLoopedSample wasn't looping infinitely when loop was set to -1.

- Mix parameter added to all biquad filters

__________________
*--- Analysis ---*

- New: FFTBinInfo class helps interpolating FFT results.

- Generation of Window Functions data has been moved to the GATMaths class and is now public ( MakeHammingWindow / MakeHanningWindow methods )

- DrawAudioModule and DrawFFTModule now reference components instead of GameObjects to conform to the audio stream scheme introduced with the
I/O system in 1.2

- Beta: Sound Banks can now try to detect midi codes of samples. This is still very much a beta functionnality and will be more fully featured
in 1.3. Note that midi codes can be inputted manually, and samples sorted by midi code by clicking sort.

________________
*--- Memory ---*

- Fix: Fixed an important memory leak in the editor.


_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------
'''''''''''''''''''''''
	*** 1.2 ***
'''''''''''''''''''''''

___________________
*---I/O Classes---*

1.2 introduces a brand new modular I/O system which enables routing of audio streams
to tracks or to file. 

Stream broadcasters:

-GATPlayer and every track of a player
-MicrophoneModule
-SourceToStreamModule ( any normal Unity AudioSource can become a stream )
-StreamSplitterModule ( both a client and broadcaster )

Streams can have multiple interleaved channels, and can be de-interleaved in
as many mono streams using the StreamSplitterModule.

On the client side, the following components handle stream input:

-StreamToTrackModule mixes a mono stream to a single track in a player
-StreamToCacheModule caches a stream in memory 
-StreamToWavModule writes a mono or interleaved multichannel stream to disk 
-StreamSplitterModule outputs one mono stream per channel in the input stream

Both StreamToCacheModule and StreamToWavModule can be configured for sample accurate 
recording: they can start at a precise dspTime, and stop after having recorded a precise
number of samples. StreamToWav module uses the new GATAsyncWavWriter class to safely write
without burdening the audio thread.

As with many other G-Audio classes, new functionnalities are accessible through 
inspector friendly Monobehaviour components or by directly interacting with 
lower level classes:

           I/O Classes Summary 
           
* Monobehaviours *		* Non monob. equivalents *

MicrophoneModule		N.A.
SourceToStreamModule	N.A.
StreamToCacheModule		GATAudioThreadStreamToCache
StreamToTrackModule		GATAudioThreadStreamToTrack
StreamToWavModule		GATAsyncWavWriter
StreamSplitterModule	GATAudioThreadStreamSplitter

____________________
*--- New Scenes ---*

ExampleScene_04 ( recording to cache ) now has a
second scene demonstrating caching audio with the new IO 
components ( Example_04b ). Example_04 is more complex,
but has been left in to demonstrate direct handling of
G-Audio's audio streams.

-ExampleScene_05( Looper ) demonstrates configuration of stream components and
scripting.

_________________________
*--- Other additions ---*

-New: GATTrack now has SubscribeContributor and UnsubscribeContributor methods
which allow any object implementing IGATTrackContributor to directly mix to the track's buffer
before filters are applied. A track can only have one contributor. StreamToTrack module 
makes use of these methods, with the option to take over the track completely or to mix to it
( 'exclusive' bool parameter ).

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

'''''''''''''''''''''''
	*** 1.11 ***
'''''''''''''''''''''''

-New: Audial Manipulators compatibility: new filters for G-Audio! 
See AboutAudial.txt in G-Audio/Filters/Audial Manipulators for more
details.

-New: GATPlayer.ClearScheduledSamples() and GATPlayer.ClearPlayingSamples have been added for
safe, popless stop of an entire GATPlayer.
Note that if you called Retain() on the GATData objects before requesting playback, 
you will still need to call Release() to free memory if it is managed by G-Audio.

-New: GATLoopedSample
A new wrapper class for GATData or IAGTProcessedSample objects that enables gapless looping
of samples. Number of loops can be adjusted, or set to -1 for infinite looping.

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

'''''''''''''''''''''''
	*** 1.1 ***
'''''''''''''''''''''''

- - - NEW - - -

v1.1 is a huge update which focuses mainly on editor integration and laying ground for 
the upcoming I/O classes ( Microphone and Write to File classes ). It also marks the launch
of G-Audio-Unity.com, which is the hub for all things G-Audio. 
The site is developed by Anthony ( username: Anthony C. ) - who recently joined the G-Audio team and 
is helping with outreach, support and software design. A warm welcome to him!


-New: Audio in edit mode - player, tracks, envelopes, pulses, filters and patterns are now fully functional both in play and edit mode. 

-New: GATPlayer's inspector is now an in-editor mixer. StereoTrackModule and all filter components are gone:
tracks and filters can be directly managed in the mixer inspector.

-New: All sample rates are now supported. 
	  Important note: AudioReverbZone components are bypassed by G-Audio's output ( Unity Pro filters are supported ).
	                  Unity Free users who wish to take advantage of AudioReverbZones can do so at 44.1khz only.
	                  Set G-Audio Manager's Supported Sample Rates field to Only44100 and enter play mode for the change to apply.
	                  A side effect of this mode is distance attenuation of G-Audio output.

-New: Filters can now be added to a player's master output, not just to tracks.

-New: EnvelopeModule now has a custom window where you may visually edit the envelope.

-New: PulseSamplesModule is replaced by PulsedPatternModule which has it's own custom inspector

-New: Custom inspectors for MasterPulseModule and SubPulseModule

-New: Visualize the current state of G-Audio's pre-allocated memory in the Memory Status Window

-New: Simplified memory configuration with the Memory Config Wizard, accessible through GATManager's inspector

-New: Samples are now pre-organized in GATSoundBank custom assets. Create a sound bank by clicking Project/Create/G-Audio/SoundBank

-New: G-Audio main object types can be created via wizards accessible in the Hierarchy/Create/G-Audio menu:
Sample Banks, EnvelopeModules, Pulses, Patterns are now just a click away. 

-New: Add your custom filters and get them to show up in the mixer, like any built-in G-Audio filter( see tutorial on the website ).

-New: 4 brand new example scenes and a demo included in the project.

-New: GAT_DEBUG pre-compilation flag: add it to BuildSettings -> PlayerSettings -> Scripting Define Symbols in the editor to 
	  troubleshoot G-Audio 

- - - CHANGES - - -

-GATSampleBank classes don't directly reference AudioClips anymore. 
They load Sound Banks - no more tedious drag and dropping of clips every time you make a new bank. 

-GATActiveSampleBank's GetProcessedSample methods dropped the cacheData bool parameter. All requested chunks
are cached by default, and need to be explicitly released.

-Had to drop Unity 4.2 compatibility for serialization of abstract classes to work. 

-The 2 reverb classes are gone. Whilst benchmarking, it appeared they performed very poorly. Please be patient whilst we hunt down more effective ones.
Meanwhile, Unity's Reverb Filter and AudioReverbZone components perform well but cannot be applied to individual tracks
( they will filter the entire ouptut of a GATPlayer ).

-IGATImpulseClient is no more - PulseModule classes now only fires OnPulse. This greatly simplifies the pulse system.
A new subscribable delegate, onWillPulse, fires even when individual steps are bypassed. 

You may also implement IGATPulseController to receive 
a callback just before the next pulse is updated.

Summing up the order of pulse events:
1) OnPulseControl( PulseInfo previousPulseInfo ). There can be only one pulse controller, which should implement IGATPulseController.
2) PulseInfo is updated.
3) onWillPulse fires. The delegate is public, anyone may subscribe. Useful for envelopes, which might need to update ( if the pulse changes ) before the pulse event.
4) OnPulse fires, on checked steps only. The delegate is not public: implement IGATPulseClient to subscribe.

As a direct consequence of these changes,
AGATImpulseClient is now AGATPulseClient.

-GATRealtimeADSR has been renamed GATRealTimeADSR for consistency.

-DrawGATAudio has been renamed DrawAudioModule for consistency.

-GATEnvelope's constructor's 2 last optional parameters have been swapped
	* previously: float normalizeValue = .3f,  bool  normalize      = true
	* now:        bool  normalize      = true, float normalizeValue = .3f
	
	This prevents having to specify a normalize value even if normalize is false.

-Updated, better Doxygen documentation ( see website ).

-The F.A.Q. and GettingStarted txt files are no more. Please refer to G-Audio-Unity.com for similar material.

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

'''''''''''''''''''''''
	*** 1.02 ***
'''''''''''''''''''''''

-New: ReverbModule adds 2 reverbs to tracks( NReverg or PRCReverb )
-New: Added a gain parameter to all of GATPlayer, GATProcessedSample and 
GATRealTimeSample's Play methods.
-New: Stream mono wav files from disk with GATWavStreamer and GATWavFile classes
-New: multiple GATPlayer instances are now allowed. This enables different Unity filters to
affect different players. All Play, PlayScheduled and PlayThroughTrack are now overloaded to accept
a GATPlayer parameter.

-Fix: Processed samples obtained with GATResamplingSampleBank.GetProcessedSample were updating their audio data needlessly
-Fix: Stereo samples are now properly loaded by GATSampleBank classes
-Fix: GATPlayer now has a sampleRateFix field to properly playback audio in builds at sample rates different
from that of the editor. If no AudioClip is set in GATPlayer's AudioSource, 
Unity automatically resamples at 44.1 khz on iOS even if output sample rate is 44.1khz. 
The fix creates a 1 sample long clip of appropriate sample rate and assigns it to the AudioSource to
bypass resampling. The downside is that for some reason, AudioReverbZone components are bypassed.
Set to true if you wish to play at 24 khz on iOS!

-Fix: Readme introduction read "working on Unity" instead of "working on Unity audio projects." 
I did not intend to imply that I was working on the engine itself ;-)

_____________________________________________________________________________________________________________
-------------------------------------------------------------------------------------------------------------

*** 1.0 ***

Initial release.
