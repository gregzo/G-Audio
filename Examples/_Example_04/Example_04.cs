using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// In this more advanced example, we record the player's stereo output to cache
	// and play it back. In order to get access to the player's output, we implement 
	// IGATAudioThreadStreamClient and subscribe to the stream.
	public class Example_04 : ExamplesBase, IGATAudioThreadStreamClient 
	{
		// enum for a simple Finite State Machine. 
		enum State{ IdleNoRec, IdleRecInMemory, Recording, PlayingBack }
		
		//IdleNoRec is the initial state before any data has been cached.
		State _state = State.IdleNoRec;
		
		// We need a reference to the observed stream so that we may unsubscribe
		IGATAudioThreadStream _observedStream;
		
		// The number of samples we'll record - adjustable via a slider, inits at 200000 ( roughly 5 seconds at 44.1 khz )
		int _recNumSamples = 200000;
		
		// The two GATData instances which will hold left and right channel recorded data
		GATData _leftData, _rightData;
		
		// the current offset in the GATData object whilst recording
		int _recOffset;
		
		// Has the recorded data been faded yet to prevent pops?
		bool _didFade;
		
		// Should we record to G-Audio pre-allocated memory?
		bool _useManagedMemory;
		
		const int MAX_UNMANGED_REC_SIZE = 1000000;
		
		protected override string         Title{ get{ return "Example_04: Recording an Audio Stream to Cache & Playing It Back ( advanced )"; } }
		protected override string SlidersHeader{ get{ return "Gain";       } }
		
		void Awake()
		{
			if( GATInfo.NbOfChannels != 2 )
			{
				Debug.LogError( "This tutorial only works with stereo ouptut!" );
				Destroy( this );
			}
		}
		
		void OnEnable() // We subscribe to the player's stream in OnEnable, and unsubscribe in OnDisable
		{
			// audio thread stream access is protected behind an explicit implementation of IGATAudioThreadStreamOwner.GetAudioThreadStream()
			// Handling of streams is delicate: the callback they provide is on the audio thread and needs special care.
			// We first cast the default player to IGATAudioThreadStreamOwner in order to get access to audio stream getter methods.
			IGATAudioThreadStreamOwner streamOwner = ( IGATAudioThreadStreamOwner )GATManager.DefaultPlayer;
			_observedStream = streamOwner.GetAudioThreadStream( 0 ); 
			
			// The point of this tutorial is to demonstrate stereo capture!
			if( _observedStream.NbOfChannels != 2 )
			{
				Debug.LogError( "This tutorial only works with stereo ouptut!" );
				Destroy( this );
				return;
			}
			_observedStream.AddAudioThreadStreamClient( this ); //Subscribe to the stream: we will now receive the HandleAudioThreadStream callback.
		}
		
		void OnDisable()
		{
			_observedStream.RemoveAudioThreadStreamClient( this ); // Unsubscribing is vital!
			_observedStream = null;
		}
		
		protected override string[] GetButtonLabels()
		{
			return sampleBank.AllSampleNames; // Just as in previous tutorials, we want one button per sample in the bank.
		}
		
		protected override void SetSlidersRange( out float min, out float max, out float init )
		{
			// We will use the sliders for gain.
			min = 0f;
			max = 2f;
			init = 1f;
		}
		
		protected override void ButtonClicked( int buttonIndex )
		{
			//Simple, unprocessed playback through the selected track. We'll focus on recording, not processing.
			
			string sampleName 	= ButtonLabels[ buttonIndex ];
			float gain 			= SliderValues[ buttonIndex ];
			
			//Grab the audio data from the bank
			GATData sampleData = sampleBank.GetAudioData( sampleName );
			
			// And play it through the default player, at track _trackNb, and specified gain.
			GATManager.DefaultPlayer.PlayData( sampleData, TrackNb, gain );  
		}
		
		protected override void AreaOneExtraGUI() // We need to draw extra controls to record
		{
			GUILayout.Space( 20f );
			
			_useManagedMemory = GUILayout.Toggle( _useManagedMemory, "Use managed memory" ); // should we allocate the cached recording using G-Audio's virtual memory?
			
			if( _state == State.IdleNoRec || _state == State.IdleRecInMemory ) // We're not recording or playing, draw rec controls
			{
				GUILayout.BeginHorizontal();
				
				// What is the longest possible recording length? 
				int maxSamples = _useManagedMemory ? GATManager.DefaultDataAllocator.LargestFreeChunkSize : MAX_UNMANGED_REC_SIZE;
				
				// Clamp _recNumSamples when switching back and forth from managed to unmanaged
				if( _useManagedMemory && _recNumSamples > maxSamples )
					_recNumSamples = maxSamples;
				
				// Label for the slider, adjusted to display seconds and not samples
				GUILayout.Label( "Rec duration:" + ( ( float )_recNumSamples / GATInfo.OutputSampleRate ).ToString( "0.00" ), GUILayout.Width( 80f ) );
				
				// Recording duration slider, from 1 second to maxSamples
				_recNumSamples = ( int )GUILayout.HorizontalSlider( ( float )_recNumSamples, 44100f, ( float )maxSamples );
				
				GUILayout.EndHorizontal();
				
				// Rec button
				if( GUILayout.Button( "Record" ) )
				{
					StartRecording( _recNumSamples );
				}
			}
			
			// If we have a recording in memory, draw a button to play it
			if( _state == State.IdleRecInMemory )
			{
				if( GUILayout.Button( "Play" ) )
				{
					StartPlaying();
				}
			}
			else if( _state == State.Recording ) // else if recording, draw a slider that will act as a progress bar
			{									 // ( the thumb is not adjustable, it's position is simply mapped to _recOffset )
				GUILayout.Label( "Recording" );
				GUILayout.HorizontalSlider( ( float )_recOffset, 0f, ( float )_recNumSamples );
			}
		}
		
		// Prepares for recording and sets the state that the audio thread stream callback is monitoring
		void StartRecording( int numSamples )
		{
			if( _leftData != null ) // We've already recorded something, let's free the data
			{
				_leftData.Release();  // Note that releasing data won't stop playback, as the player retains it too.
				_rightData.Release(); // Also note that Release() only has an effect if it was allocated in G-Audio's virtual memory.
			}
			
			if( _useManagedMemory )
			{
				_leftData = GATManager.GetDataContainer( numSamples );  //Ask the manager for an empty container of appropriate size
				_rightData = GATManager.GetDataContainer( numSamples );
			}
			else
			{
				_leftData = new GATData( new float[ numSamples ] ); // Make the container ourselves, allocating float arrays manually.
				_rightData = new GATData( new float[ numSamples ] );
			}
			
			
			_leftData.Retain();  // Retain the data, otherwise the player will free it at the end of playback. 
			_rightData.Retain(); // Just as Release, this has no effect on manually allocated arrays.
			
			_recOffset = 0;		 // reset the offset
			_state = State.Recording; // update state
		}
		
		public void HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
		{
			if( _state == State.Recording ) // Monitor the state
			{
				bool atEnd = false;
				int appliedLength = stream.BufferSizePerChannel; // at the end of the recording, the length we will copy will be
				// smaller than buffer size.
				
				if( appliedLength > _leftData.Count - _recOffset ) // We've reached the end of the recording, adjust applied length
				{
					atEnd = true;
					appliedLength = _leftData.Count - _recOffset;
				}
				
				_leftData.CopyFromInterlaced( data, appliedLength, _recOffset, 0, 2 ); // copy each channel to the GATData objects. CopyFromInterlaced handles de-interleaving.
				_rightData.CopyFromInterlaced( data, appliedLength, _recOffset, 1, 2 );
				
				if( atEnd ) // Done, reset the _didFade flag and change state.
				{
					_didFade = false; // We will fade the data to avoid pops, but not on the audio thread - there's no reason to do it before playback.
					_state = State.IdleRecInMemory;
					return;
				}
				
				_recOffset += appliedLength;
			}
		}
		
		void StartPlaying()
		{
			int fadeInLength  = 1024;
			int fadeOutLength = 22050; //longer fade out
			
			if( _didFade == false ) // We haven't played the recording yet, let's fade it for popless playback
			{
				_leftData.FadeIn( fadeInLength );
				_rightData.FadeIn( fadeInLength );
				
				_leftData.FadeOut( fadeOutLength );
				_rightData.FadeOut( fadeOutLength );
				
				_didFade = true;
			}
			
			// We are not going to route playback through tracks. In order for the player to
			// directly play data, it needs an AGATPanInfo object ( GATFixedPanInfo or GATDynamicPanInfo ).
			// As we won't dynamically pan, we'll use the fixed version.
			// Note that it would be better to cache these objects so as not to have to re-create them 
			// every time we play.
			GATFixedPanInfo leftPan  = new GATFixedPanInfo();
			GATFixedPanInfo rightPan = new GATFixedPanInfo();
			
			leftPan.SetStereoPan( 0f ); // pan on fixed pan objects can only be set once!
			rightPan.SetStereoPan( 1f );
			
			GATManager.DefaultPlayer.PlayData( _leftData, leftPan ); // we pass the pan objects to the player.
			GATManager.DefaultPlayer.PlayData( _rightData, rightPan );
			
			//Final note: pan objects can handle playback of multiple simultaneous samples.
		}
	}
}

