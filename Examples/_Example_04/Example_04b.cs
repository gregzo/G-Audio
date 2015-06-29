using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// In this more advanced example, we record the player's stereo output to cache
	// and play it back. In order to get access to the player's output, we implement 
	// IGATAudioThreadStreamClient and subscribe to the stream.
	public class Example_04b : ExamplesBase 
	{
		public StreamToCacheModule streamToCache;
		
		// enum for a simple Finite State Machine. 
		enum State{ IdleNoRec, IdleRecInMemory, Recording, PlayingBack }
		
		//IdleNoRec is the initial state before any data has been cached.
		State _state = State.IdleNoRec;
		
		// The number of samples we'll record - adjustable via a slider, inits at 200000 ( roughly 5 seconds at 44.1 khz )
		int _recNumSamples = 200000;
		
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
				GUILayout.HorizontalSlider( ( float )streamToCache.RecPosition, 0f, ( float )_recNumSamples );
			}
			
			GUILayout.Space( 10f );
			
			bool loop = streamToCache.LoopedRec;
			
			GUI.changed = false;
			
			loop = GUILayout.Toggle( loop, "Loop Rec" );
			
			if( GUI.changed )
			{
				streamToCache.LoopedRec = loop;
			}
		}
		
		// Prepares for recording: the StreamToCacheModule will take care of allocating caches and monitoring the audio thread stream.
		void StartRecording( int numSamples )
		{
			if( streamToCache.Caches != null ) // We've already recorded something, let's free the data
			{
				streamToCache.ReleaseCache();
			}
			
			streamToCache.AllocateCaches( ( double )numSamples / GATInfo.OutputSampleRate, _useManagedMemory );
			
			streamToCache.StartCaching( 0d, OnRecEnd );
			
			_state = State.Recording; // update state
			
			_didFade = false;
		}
		
		void OnRecEnd( GATData[] caches, bool willLoop )
		{
			if( willLoop == false )
			{
				_state = State.IdleRecInMemory;
			}
			
		}
		
		void StartPlaying()
		{
			int fadeInLength  = 1024;
			int fadeOutLength = 22050; //longer fade out
			int i;
			
			if( _didFade == false ) // We haven't played the recording yet, let's fade it for popless playback
			{
				for( i = 0; i < streamToCache.Caches.Length; i++ )
				{
					streamToCache.Caches[ i ].FadeIn( fadeInLength );
					streamToCache.Caches[ i ].FadeOut( fadeOutLength );
				}
				
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
			
			GATManager.DefaultPlayer.PlayData( streamToCache.Caches[ 0 ], leftPan ); // we pass the pan objects to the player.
			GATManager.DefaultPlayer.PlayData( streamToCache.Caches[ 1 ], rightPan );
			
			//Final note: pan objects can handle playback of multiple simultaneous samples.
		}
	}

}
