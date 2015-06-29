//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GAudio.Examples
{
	// This example demonstrates a looper implementation using G-Audio's new I/O components.
	public class Example_05 : AGATPulseClient 
	{
		public MicrophoneModule 	mic; // ref to the component which manages the microphone
		public StreamToCacheModule 	streamToCache; // this component handles the actual recording to cache of unfiltered mic data 
		public StreamToTrackModule 	streamToTrack; // this one allows routing of mic data to tracks for live effects
		public PulsedPatternModule 	metronomePattern; // a ref is needed to toggle the metronome on and off

		public StreamToWavModule 	streamToWav; // handling of recording to disk

		enum RecState{ Idle, WillRec, InRec }; 
		RecState _recState = RecState.Idle;
		
		enum MicMode{ Track, PlayThrough, Muted }; // Mic data is always recorded unfiltered so that we may change filtering later.
		MicMode _micMode = MicMode.Track; // We can route the mic input through a track to preview effects in real time.
		
		RecInfo[] _recs; // helper objects to manage cached data per recorded track.
		
		int _numTracks = 4; 
		int _currentRecIndex; // stores the currently selected rec track
		
		GATTrack _currentTrack; // cache for convenience. 
		
		AGATFilter.FilterProperty[] _trackFilterProps; //helper class which performs reflection on filters to draw their properties easily
		
		AGATFilter 	_selectedFilter; //cache current filter to avoid fetching in OnGUI
		string 		_filterName; 
		
		int _lastPulseIndex; // We inherit from AGATPulseClient to receive pulses in OnPulse. 
		// The last pulse index is stored to display a rec count down in OnGUI.
		
		void Start()
		{
			// Compute how many samples are needed for one loop at current bpm and sample rate
			int numSamples = ( int )( GATInfo.OutputSampleRate * ( Pulse.Period * Pulse.Steps.Length ) );
			
			// Setup the Rec objects which will allocate caches
			_recs = new RecInfo[ _numTracks ];
			
			for( int i = 0; i < _numTracks; i++ )
			{
				_recs[ i ] = new RecInfo( numSamples, 1, i + 1 );
			}
			
			// Grab track and filter info, and re-route mic data if needed
			UpdateCurrentTrack();
		}
		
		// Don't forget base.OnEnable: it is where subscription to the pulse happens
		protected override void OnEnable()
		{
			base.OnEnable();
			mic.onMicFailed += OnMicFailed; // Mic fails when re-routing audio output( when plugging headphones, for example )
			// The MicrophoneModule can notify us when this happens, so that we may warn the user
			// or restart the mic.
		}
		
		protected override void OnDisable()
		{
			base.OnDisable();
			mic.onMicFailed -= OnMicFailed; // Don't forget to unsubscribe!
		}
		
		public override void OnPulse( IGATPulseInfo pulseInfo ) // Monitor pulse to handle recording and playback in time
		{
			if( pulseInfo.StepIndex == 0 ) // We're about to start the loop
			{
				int i;
				
				for( i = 0; i < _recs.Length; i++ )
				{
					if( _recs[ i ].shouldPlay ) // if there's some recorded data, play it
					{
						if( !streamToCache.Overdub && _recState == RecState.WillRec && _currentRecIndex == i ) // but don't if we are recording this very track, except when overdubbing
							continue;
						
						_recs[ i ].Play( pulseInfo.PulseDspTime ); // Play the cached data 
					}
				}
				
				// If the user clicked record, we set the appropriate cache in the streamToCache module and fire
				if( _recState == RecState.WillRec )
				{
					streamToCache.Caches = _recs[ _currentRecIndex ].data;
					streamToCache.StartCaching( pulseInfo.PulseDspTime, RecAtEnd ); // RecAtEnd will be called when the cache is full
					_recState = RecState.InRec;
					_recs[ _currentRecIndex ].shouldPlay = true; // next time setp 0 pulses, we'll have data that we should play
				}
			}
			
			_lastPulseIndex 	= pulseInfo.StepIndex; // store the pulse index for a rec count down
		}
		
		void RecAtEnd( GATData[] caches, bool willLoop ) // delegate method called by streamToCache
		{
			_recState = RecState.Idle;
		}
		
		// Update track and filter info when the user switches track
		void UpdateCurrentTrack()
		{
			_currentTrack = GATManager.DefaultPlayer.GetTrack( _recs[ _currentRecIndex ].trackNb ); 
			
			_selectedFilter = _currentTrack.FiltersHandler.GetFilterAtSlot( 0 );
			
			if( _selectedFilter != null )
			{
				_trackFilterProps = _selectedFilter.GetFilterProperties();
				_filterName = AGATMonoFilter.FilterNameForType( _selectedFilter.GetType() );
			}
			
			if( _micMode == MicMode.Track ) // if we're routing the mic through a track, we should update the streamToTrack object
			{
				streamToTrack.TargetTrack = _currentTrack;
			}
		}
		
		void OnMicFailed( MicrophoneModule micModule ) // Just reset the mic
		{
			micModule.StartMicrophone();
		}
		
		// Nested helper class to handle caches 
		class RecInfo
		{
			public GATData[] data;
			public bool shouldPlay;
			public int trackNb;
			public GATTrack Track{ get; protected set; }
			
			
			public RecInfo( int numSamples, int numChannels, int itrackNb )
			{
				int i;
				
				data = new GATData[ numChannels ];
				
				for( i = 0; i < numChannels; i++ )
				{
					data[ i ] = new GATData( new float[ numSamples ] );
				}
				
				trackNb = itrackNb;
				Track = GATManager.DefaultPlayer.GetTrack( trackNb );
			}
			
			public void Play( double dspTime )
			{
				GATManager.DefaultPlayer.PlayDataScheduled( data[ 0 ], dspTime, trackNb ); 
			}
		}
		
		// --------------------------------------------------------------
		// **************** Nasty GUI code ******************************
		
		static Rect AREA = new Rect( 20f, 20f, 310f, 700f );
		static Rect FILTER_AREA = new Rect( 360f, 150f, 310f, 400f );
		static string[] __trackSelection = new string[]{ "Track 1", "Track 2", "Track 3", "Track 4" };
		static string[] __micModes		 = new string[]{ "Mic To Track", "Mic Unfiltered", "Mic Muted" };
		
		void OnGUI()
		{
			GUILayout.BeginArea( AREA );
			
			GUILayout.Label( "G-Audio I/O examples: Looper" );
			GUILayout.Label( "www.G-Audio-Unity.com" );
			
			if( _recState == RecState.InRec )
				GUI.enabled = false;
			
			GUILayout.Label( "Warning! Live mic, plug headphones or bear the larsen." );
			
			_micMode = ( MicMode )GUILayout.SelectionGrid( ( int )_micMode, __micModes, 3 ); 
			
			if( GUI.changed )
			{
				switch( _micMode )
				{
				case MicMode.Muted:
					mic.playThrough = false;
					streamToTrack.enabled = false;
					break;
					
				case MicMode.PlayThrough:
					mic.playThrough = true;
					streamToTrack.enabled = false;
					break;
					
				case MicMode.Track:
					mic.playThrough = false;
					streamToTrack.enabled = true;
					streamToTrack.TargetTrack = _currentTrack;
					break;
				}
				
				GUI.changed = false;
			}
			
			GUILayout.Space( 5f );
			
			streamToCache.Overdub = GUILayout.Toggle( streamToCache.Overdub, "Overdub" );
			
			GUILayout.Space( 5f );
			
			metronomePattern.enabled = GUILayout.Toggle( metronomePattern.enabled, "Metronome" );
			
			GUILayout.Space( 5f );
			
			if( _recState == RecState.Idle )
			{
				if( GUILayout.Button( "Rec", GUILayout.Width( 310f ) ) && _recState != RecState.InRec )
				{
					_recState = RecState.WillRec;
				}
			}
			else if( _recState == RecState.WillRec )
			{
				GUILayout.Button( ( 4 - _lastPulseIndex ).ToString(), GUILayout.Width( 310f ) );
			}
			else
			{
				GUILayout.Button( "Recording", GUILayout.Width( 310f ) );
			}
			
			GUILayout.Space( 5f );
			
			GUI.changed = false;
			
			_currentRecIndex =  GUILayout.SelectionGrid( _currentRecIndex, __trackSelection, _numTracks );
			
			if( GUI.changed )
			{
				UpdateCurrentTrack();
			}
			
			GUILayout.Space( 10f );
			
			GUILayout.Label( "Track Gain: "+_currentTrack.StereoGain.ToString( "0.00" ) );
			_currentTrack.StereoGain = GUILayout.HorizontalSlider( _currentTrack.StereoGain, 0f, 2f );
			
			GUILayout.Space( 5f );
			GUILayout.Label( "Track Pan " );
			_currentTrack.StereoPan  = GUILayout.HorizontalSlider( _currentTrack.StereoPan, 0f, 1f );
			
			GUILayout.Space( 10f );
			
			if( GUILayout.Button( "Clear Track" ) )
			{
				_recs[ _currentRecIndex ].data[ 0 ].Clear();
				_recs[ _currentRecIndex ].shouldPlay = false;
			}
			
			GUILayout.Space( 10f );
			
			#if UNITY_WEBPLAYER
			GUI.enabled = false;
			#endif

			if( streamToWav.IsWriting == false )
			{
				if( GUILayout.Button( "Start recording to wav" ) )
				{
					streamToWav.StartWriting();
				}
			}
			else
			{
				if( GUILayout.Button( "Stop recording to wav" ) )
				{
					streamToWav.EndWriting();
				}
			}
			
			#if UNITY_WEBPLAYER
			GUILayout.Label( "Recording to disk is disabled in WebPlayers." );
			#endif
			
			GUILayout.EndArea();
			
			GUI.enabled = true;
			
			GUILayout.BeginArea( FILTER_AREA );
			
			GUILayout.Label( "Track " + _recs[ _currentRecIndex ].trackNb + " Filter " );
			
			if( _selectedFilter != null )
			{
				int i;
				
				GUILayout.Space( 10f );
				
				GUILayout.Label( _filterName );
				
				GUILayout.Space( 10f );
				
				AGATFilter.FilterProperty prop;
				
				for( i = 0; i < _trackFilterProps.Length; i++ )
				{
					prop = _trackFilterProps[ i ];
					if( prop.IsGroupToggle )
						continue;
					
					GUILayout.BeginHorizontal();
					GUILayout.Label( prop.LabelString, GUILayout.Width( 100f ) );
					prop.SetValue( GUILayout.HorizontalSlider( prop.CurrentValue, prop.Range.Min, prop.Range.Max, GUILayout.Width( 200f ) ) );
					GUILayout.EndHorizontal();
				}
				
				_selectedFilter.Bypass = GUILayout.Toggle( _selectedFilter.Bypass, "Bypass Filter" );
			}
			
			GUILayout.EndArea();
		}
	}
}

