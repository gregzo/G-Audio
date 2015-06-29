//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GAudio
{
	[ RequireComponent( typeof( AudioSource ) ) ]
	/// <summary>
	/// Monobehaviour component for handling
	/// microphone input. Only allocates a one
	/// second AudioClip for minimal memory impact.
	/// </summary>
	public class MicrophoneModule : SourceToStreamModule
	{
		/// <summary>
		/// Should the default microphone be primed
		/// in Start()?
		/// </summary>
		public bool initInStart = true;
		
		/// <summary>
		/// A list of available microphones.
		/// </summary>
		public static GATMicInfo[] Microphones{ get; protected set; }
		
		/// <summary>
		/// The current active device.
		/// </summary>
		public GATMicInfo CurrentMic{ get; protected set; }
		
		/// <summary>
		/// If true, the mic is hot and an udio stream is
		/// being broadcasted.
		/// </summary>
		public bool    IsActive{ get; protected set; }
		
		/// <summary>
		/// Subscribe to this delegate to be notified 
		/// of interruptions, for example when the user 
		/// plugs or unplugs headphones. You can immediately
		/// call StartMicrophone() to resume recording.
		/// </summary>
		public OnMicFailed onMicFailed;
		
		public delegate void OnMicFailed( MicrophoneModule micModule );
		public delegate void ReadyToRecordHandler();
		
		#region Private and Protected
		protected ReadyToRecordHandler _onReadyToRecord;
		protected bool _initialized;
		
		protected override void Awake()
		{
			base.Awake();
			
			_source.playOnAwake = false;
			_source.loop = true;
			
			if( Microphones == null )
			{
				InitDevices();
			}
		}
		
		void InitDevices()
		{
			string[] devices = Microphone.devices;

			int minFreq, maxFreq;
			Microphones = new GATMicInfo[ devices.Length ];
			
			int i;
			
			for( i = 0; i < devices.Length; i++ )
			{
				Microphone.GetDeviceCaps( devices[i], out minFreq, out maxFreq );
				Microphones[i] = new GATMicInfo( devices[i], minFreq, maxFreq );
			}
		}
		
		void OnEnable()
		{
			if( _initialized && _source.clip != null )
			{
				_source.timeSamples = CurrentMic.GetPosition();
				_source.Play();
				IsActive = true;
			}
		}
		
		IEnumerator Start()
		{
			yield return null;
			if( initInStart )
				StartMicrophone();
		}
		
		void OnDisable()
		{
			if( _initialized )
			{
				IsActive = false;
				_source.Stop();
			}
			else
			{
				StopAllCoroutines();
			}
		}
		
		void Update()
		{
			// This monitors microphone failure when audio output routing changes
			if( IsActive && CurrentMic.IsRecording == false )
			{
				IsActive = false;
				_source.Stop ();
				Destroy( _source.clip );
				
				if( onMicFailed != null )
				{
					onMicFailed( this );
				}
			}
		}
		
		void OnDestroy()
		{
			_source.Stop();
			Destroy( _source.clip );
			Destroy ( _source );
		}
		
		IEnumerator InitMicrophonedRoutine() 
		{
			int micPos;
			
			_source.clip = Microphone.Start( CurrentMic.name, true, 1, GATInfo.OutputSampleRate );
			
			micPos = 0;
			
			while( micPos == 0 )
			{
				yield return null;
				micPos = CurrentMic.GetPosition();
			}
			
			_source.timeSamples = micPos;
			_source.Play();
			
			while( _source.timeSamples == 0 )
			{
				yield return null;
			}
			
			IsActive = true;
			_initialized    = true;
			
			if( _onReadyToRecord != null )
			{
				_onReadyToRecord();
			}
		}
		
		protected override void OnAudioFilterRead( float[] data, int numChannels )
		{
			if( IsActive == false )
			{
				return;
			}
			
			base.OnAudioFilterRead( data, numChannels );
		}
		
		#endregion
		
		/// <summary>
		/// Call to refresh the list of
		/// available microphones ( Microphones static property ).
		/// This stops the current microphone. 
		/// </summary>
		public void ResetDevices()
		{
			IsActive = false;
			
			_source.Stop ();
			
			if( _source.clip != null )
				Destroy( _source.clip );
			
			InitDevices();
			
			if( Microphones.Length > 0 )
			{
				CurrentMic = Microphones[ 0 ];
			}
		}
		
		/// <summary>
		/// Prepares and starts a stream with 
		/// mic input. This method starts a coroutine:
		/// when mic input goes live, callback will be fired.
		/// </summary>
		public void StartMicrophone( GATMicInfo micInfo, ReadyToRecordHandler callback = null )
		{
			if( IsActive )
				return;
			
			CurrentMic = micInfo;
			
			if( CurrentMic.SupportsRate( GATInfo.OutputSampleRate ) == false )
			{
				throw new GATException( "The current microphone does not support output sample rate of " + GATInfo.OutputSampleRate );
			}
			
			_onReadyToRecord = callback;
			
			StartCoroutine( InitMicrophonedRoutine() );
		}
		
		/// <summary>
		/// Prepares and starts a stream with 
		/// CurrentMic as input. This method starts a coroutine:
		/// when mic input goes live, callback will be fired.
		/// </summary>
		public void StartMicrophone( ReadyToRecordHandler callback = null )
		{
			if( IsActive )
				return;
			
			if( Microphones.Length == 0 )
			{
				throw new GATException( "No microphone found. Check available mics with GATMicrophone.Microphone before calling PrepareToRecord. " );
			}
			
			StartMicrophone( Microphones[ 0 ], callback );
		}
		
		
	}
	
	/// <summary>
	/// Convenience class for wrapping
	/// microphone information.
	/// </summary>
	public class GATMicInfo
	{
		/// <summary>
		/// The name of the device
		/// </summary>
		public readonly string name;
		
		/// <summary>
		/// The minimum supported sample rate 
		/// </summary>
		public readonly int minFrequency;
		
		/// <summary>
		/// The maximum supported sample rate
		/// </summary>
		public readonly int maxFrequency;
		
		
		public GATMicInfo( string iname, int minFreq, int maxFreq )
		{
			name 		 = iname;
			minFrequency = minFreq;
			maxFrequency = maxFreq;
		}
		
		public bool IsRecording
		{
			get
			{
				return Microphone.IsRecording( name );
			}
		}
		
		public int GetPosition()
		{
			return Microphone.GetPosition( name );
		}
		
		public bool SupportsRate( int sampleRate )
		{
			if( minFrequency != 0 && sampleRate < minFrequency )
				return false;
			
			if( maxFrequency != 0 && sampleRate > maxFrequency )
				return false;
			
			return true;
		}
		
		public void Log()
		{
			Debug.Log( string.Format( "Mic: {0} maxFreq: {1} minFreq: {2}", name, minFrequency.ToString(), maxFrequency.ToString() ) );
		}
	}
}
