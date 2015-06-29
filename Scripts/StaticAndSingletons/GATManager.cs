//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if GAT_IOS
using GAudio.iOS;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Interaction with GATManager is mostly for convenience:
	/// GATManager.DefaultPlayer to access the player, 
	/// GATManager.GetDataContainer to request a free GATData object 
	/// from the default GATDataAllocator.
	/// 
	/// Only one GATManager should exist per scene.
	/// </summary>
	[ ExecuteInEditMode ]
	public class GATManager : MonoBehaviour, IGATDataAllocatorOwner
	{
		public GATDataAllocator.InitializationSettings AllocatorInitSettings
		{
			get{ return _AllocatorInitSettings; }
		}
		[ SerializeField ]
		GATDataAllocator.InitializationSettings _AllocatorInitSettings = new GATDataAllocator.InitializationSettings();
		
		public double PulseLatency
		{
			get{ return _PulseLatency; }
			set
			{
				if( _PulseLatency == value )
					return;
				
				_PulseLatency = value;
			}
		}
		[ SerializeField ]
		double 	_PulseLatency = .1d;
		
		public int MaxIOChannels
		{
			get{ return _MaxIOChannels; }
			set
			{
				if( _MaxIOChannels == value )
					return;
				
				_MaxIOChannels = value;
			}
		}
		[ SerializeField ]
		int _MaxIOChannels = 2;
		
		public enum SampleRatesSupport{ All, Only44100 }
		public SampleRatesSupport SupportedSampleRates
		{
			get{ return _supportedSampleRates;  }
			set{ _supportedSampleRates = value; }
		}
		[ SerializeField ]
		SampleRatesSupport _supportedSampleRates = SampleRatesSupport.All;

		public enum SpeakerModeBehaviour{ Stereo, PlatformMax }
		public SpeakerModeBehaviour SpeakerModeInit
		{
			get{ return _speakerModeInit; } 
			set{ _speakerModeInit = value; }
		}
		[ SerializeField ]
		SpeakerModeBehaviour _speakerModeInit = SpeakerModeBehaviour.Stereo;
		
		[ SerializeField ] 
		private GATPlayer _defaultPlayer;
		
		/// <summary>
		/// A convenience reference to the defult player.
		/// </summary>
		public static GATPlayer DefaultPlayer{ get; private set; }
		
		static GATDataAllocator __allocator;
		/// <summary>
		/// A convenience reference to the defult allocator.
		/// Whilst it is possible to have multiple GATDataAllocator instances in a scene, 
		/// all G-Audio higher level classes which need to allocate data do so through the default allocator.
		/// </summary>
		public static GATDataAllocator DefaultDataAllocator{ get{ return __allocator; } }
		
		public static GATManager UniqueInstance{ get{ return __uniqueInstance; } }
		static GATManager __uniqueInstance;
		
		#if UNITY_EDITOR
		private bool _willEnterPlaymode;
		
		public bool popEnvelopeWindow = true;
		#endif
		
		//The following members and delegate are used to detect audio thread/main thread discrepancies
		//in the editor - the main thread may pause whilst the audio thread keeps running.
		//This typically happens when accessing drop down menus ( also in inspectors ).
		double _dspTimeInUpdate;
		public delegate void OnMainThreadResumed( double dspTimeDelta );
		public static OnMainThreadResumed onMainThreadResumed;
		
		
		#region IGATDataAllocatorOwner explicit implementation
		GATDataAllocator IGATDataAllocatorOwner.DataAllocator{ get{ return __allocator; } }
		#endregion
		
		void Awake()
		{
			if( __uniqueInstance != null )
			{
				Debug.LogError( "Only one GATManager may exist per scene! Manager found on go: " + __uniqueInstance.gameObject.name );
				DestroyImmediate( this );
				return;
			}
			
			InitManager();
		}
		
		void OnEnable()
		{
			#if UNITY_EDITOR
			if( _willEnterPlaymode )
				return;
			#endif

			AudioSource audio = _defaultPlayer.GetComponent< AudioSource >();

			if( __uniqueInstance == null )
			{
				InitManager ();
			}
			
			if( audio.isPlaying == false )
				audio.Play();
			
			#if UNITY_EDITOR
			EditorApplication.playmodeStateChanged += OnPlaymodeChange;
			
			//For main thread suspension detection
			if( Application.isPlaying == false )
			{
				EditorApplication.update += Update;
			}
			#endif
		}
		
		void OnDisable()
		{
			#if UNITY_EDITOR
			EditorApplication.playmodeStateChanged -= OnPlaymodeChange;
			
			//For main thread suspension detection
			EditorApplication.update -= Update;
			#endif
		}
		
		void Start()
		{
			_dspTimeInUpdate = AudioSettings.dspTime;
		}
		
		#if UNITY_EDITOR
		void OnPlaymodeChange()
		{
			if( EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying == false )
			{
				_willEnterPlaymode = true;
			}
		}
		#endif
		
		void InitManager()
		{
			__uniqueInstance = this;

#if !UNITY_5
			if( _speakerModeInit == SpeakerModeBehaviour.Stereo )
			{
				AudioSettings.speakerMode = AudioSpeakerMode.Stereo;
			}
			else
			{
				AudioSettings.speakerMode = AudioSettings.driverCaps;
			}
#endif
			
			if( GATInfo.UniqueInstance == null )
			{
				GATInfo.Init();
			}
			
			GATInfo.UniqueInstance.SetSyncDspTime( AudioSettings.dspTime );
			GATInfo.UniqueInstance.SetPulseLatency( _PulseLatency );
			GATInfo.UniqueInstance.SetMaxIOChannels( _MaxIOChannels );
			
			
			if( __allocator == null )
			{
#if UNITY_EDITOR
				if( Application.isPlaying )
#endif
				{
					__allocator = new GATDataAllocator( _AllocatorInitSettings );
				}
			}
			
			//Static initializers
			GATPlayer.InitStatics();
			
			if( _defaultPlayer == null )
			{
				GameObject go = new GameObject( "DefaultPlayer" );
				go.transform.parent = transform;
				go.AddComponent< AudioSource >();
				_defaultPlayer = go.AddComponent< GATPlayer >();
				_defaultPlayer.AddTrack< GATTrack >();
			}
			
			DefaultPlayer = _defaultPlayer;
			
			#if GAT_IOS && !UNITY_EDITOR
			GATiOS.InitializeNativeGAudio( GATInfo.OutputSampleRate, GATInfo.AudioBufferSizePerChannel, ( byte )GATInfo.NbOfChannels );
			#endif
		}
		
		void OnDestroy()
		{
			if( __uniqueInstance != this ) //Destroying an illegal duplicate
			{
				return;
			}
			//Call static cleaners first
			GATPlayer.CleanUpStatics();
			
			if( __allocator != null )
			{
				__allocator.Dispose();
				__allocator = null;
			}
			
			DefaultPlayer = null;
			
			__uniqueInstance = null;
			
			onMainThreadResumed = null;

			//System.GC.Collect();
		}
		
		void OnLevelWasLoaded()
		{
			GATInfo.UniqueInstance.SetSyncDspTime( AudioSettings.dspTime );
		}
		
		/// <summary>
		/// Convenience method to request a chunk of virtual memory from the default 
		/// GATDataAllocator instance.
		/// </summary>
		public static GATData GetDataContainer( int length )
		{
			return __allocator.GetDataContainer( length );
		}
		
		/// <summary>
		/// Convenience method to request a chunk of fixed virtual memory from the default 
		/// GATDataAllocator instance.
		/// </summary>
		public static GATData GetFixedDataContainer( int length, string description )
		{
#if UNITY_EDITOR
			if( Application.isPlaying == false )
				return new GATData( new float[ length ] );
#endif
			return __allocator.GetFixedDataContainer( length, description );
		}
		
		//Suspension of main thread detection
		void Update()
		{
			if( onMainThreadResumed == null )
				return;
			
			double prevDspTime = _dspTimeInUpdate;
			_dspTimeInUpdate = AudioSettings.dspTime;
			
			double dspDelta = _dspTimeInUpdate - prevDspTime;
			
			if( Application.isPlaying )
			{
				if( Time.frameCount > 100 && dspDelta > .1d )
				{
					if( onMainThreadResumed != null )
						onMainThreadResumed( dspDelta );
					
					if( Debug.isDebugBuild )
					{
						Debug.LogWarning( "The main thread was paused while the audio thread was still running, delta dspTime between the last 2 frames: "+ ( _dspTimeInUpdate - prevDspTime ) );
					}
				}
			}
			else
			{
				if( dspDelta > .1d )
				{
					if( onMainThreadResumed != null )
						onMainThreadResumed( dspDelta );
				}
			}
		}
	}
}

