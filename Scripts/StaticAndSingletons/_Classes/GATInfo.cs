//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Suite of static properties for
	/// handy access to audio related information.
	/// Initialized by GATManager.
	/// </summary>
	public class GATInfo
	{
		public static GATInfo 		 UniqueInstance{ get; private set; }
		
		/// <summary>
		/// Gets the nb of channels the Unity project is setup to output.
		/// This is deduced from AudioSettings.AudioSpeakerMode, and might not reflect
		/// the actual number of channels being outputted. In case of discrepancy, GATPlayer instances 
		/// will be disabled and warnings will appear in the console if the GAT_DEBUG flag is set.
		/// </summary>
		public static int 			   NbOfChannels{ get; private set; }
		
		/// <summary>
		/// The length of the audio buffer for one channel.
		/// </summary>
		public static int AudioBufferSizePerChannel{ get; private set; }
		
		/// <summary>
		/// The duration in seconds of the audio buffer.
		/// </summary>
		public static double 	AudioBufferDuration{ get; private set; }
		
		/// <summary>
		/// The output sample rate.
		/// </summary>
		public static int 		   OutputSampleRate{ get; private set; }
		
		/// <summary>
		/// The maximum delta in gain from one buffer to another before 
		/// gain should be interpolated.
		/// </summary>
		public static float 		   MaxGainDelta{ get; private set; }
		
		/// <summary>
		/// Reset OnLevelWasLoaded, used by pulses to sync.
		/// </summary>
		public static double	        SyncDspTime{ get; private set; }
		
		/// <summary>
		/// The latency applied by PulseModule components.
		/// </summary>
		public static double		   PulseLatency{ get; private set; }
		
		/// <summary>
		/// The sample rate requested in GATAudioInit. May not match OutputSampleRate if the requested sample rate isn't supported.
		/// </summary>
		public static int		RequestedSampleRate{ get; private set; }
		
		public static int 		      MaxIOChannels{ get; private set; }
		
		private GATInfo( int inbOfChannels, int iaudioBufferSizePerChannel, double iaudioBufferDuration )
		{
			NbOfChannels 				= inbOfChannels;
			AudioBufferSizePerChannel 	= iaudioBufferSizePerChannel;
			AudioBufferDuration 		= iaudioBufferDuration;
			OutputSampleRate			= AudioSettings.outputSampleRate;
			MaxGainDelta 				= .005f;
			UniqueInstance	 			= this;
		}
		
		public static void Init()
		{
			if( UniqueInstance != null )
			{
				Debug.LogWarning( "GATInfo can only be initialized once!" );
				return;
			}
			
			int nbOfChannels;
			switch( AudioSettings.speakerMode )
			{
			case AudioSpeakerMode.Mono:
				nbOfChannels = 1;
				break;
				
			case AudioSpeakerMode.Stereo:
				nbOfChannels = 2;
				break;
				
			case AudioSpeakerMode.Quad:
				nbOfChannels = 4;
				break;
				
			case AudioSpeakerMode.Surround:
				nbOfChannels = 5;
				break;
				
			case AudioSpeakerMode.Mode5point1:
				nbOfChannels = 6;
				break;
				
			case AudioSpeakerMode.Mode7point1:
				nbOfChannels = 8;
				break;
				
			default:
				nbOfChannels = 2;
				break;
			}
			
			int bufferSize;
			int numBuffers;
			
			AudioSettings.GetDSPBufferSize( out bufferSize, out numBuffers );
			
			double dspBufferDuration = ( ( double )( bufferSize ) ) / AudioSettings.outputSampleRate;
			
			UniqueInstance = new GATInfo( nbOfChannels, bufferSize, dspBufferDuration );
			
			if( RequestedSampleRate != 0 && OutputSampleRate != RequestedSampleRate )
			{
				Debug.LogWarning( "Requested sample rate of " + RequestedSampleRate + " is not available on this platform." );
			}
			
			#if GAT_DEBUG
			Debug.Log( "Number of channels: "+nbOfChannels );
			Debug.Log( "dsp buffer size: " + bufferSize + " duration: " + dspBufferDuration + "sample rate: "+OutputSampleRate );
			#endif
		}
		
		public void SetSyncDspTime( double dspTime )
		{
			SyncDspTime = dspTime;
		}
		
		public void SetPulseLatency( double pulseLatency )
		{
			PulseLatency = pulseLatency;
		}
		
		public void SetMaxIOChannels( int maxChannels )
		{
			MaxIOChannels = maxChannels;
		}
		
		/// <summary>
		/// Called by GATAudioInit before
		/// GATManager has been initialized to register
		/// a user sample rate request. 
		/// </summary
		public static void SetRequestedSampleRate( int sampleRate )
		{
			RequestedSampleRate = sampleRate;
		}
	}
}

