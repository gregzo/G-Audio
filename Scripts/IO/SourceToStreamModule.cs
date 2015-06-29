//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// This component broadcasts a
	/// G-Audio audio stream ( IGATAudioThreadStream )
	/// from a Unity AudioSource. The stream will have
	/// as many interleaved channels as Unity outputs
	/// ( GATInfo.NbOfChannels ). The stream should be de-interleaved
	/// with a StreamSplitterModule before it can be routed through
	/// G-Audio tracks.
	/// </summary>
	public class SourceToStreamModule : MonoBehaviour, IGATAudioThreadStreamOwner
	{
		/// <summary>
		/// If true, broadcasting the stream will
		/// not interrupt normal audio playback.
		/// </summary>
		public bool playThrough;
		
		protected AudioSource _source;
		
		protected GATAudioThreadStreamProxy _audioThreadStreamProxy;
		
		protected virtual void Awake()
		{
			_audioThreadStreamProxy = new GATAudioThreadStreamProxy( GATInfo.AudioBufferSizePerChannel, GATInfo.NbOfChannels, GATAudioBuffer.AudioBufferPointer, 0, "MicrophoneStream" );
			_source = this.GetComponent< AudioSource >();
		}
		
		#region GATOwner< IGATAudioThreadStream > implementation
		
		
		IGATAudioThreadStream IGATAudioThreadStreamOwner.GetAudioThreadStream( int index )
		{
			return ( _audioThreadStreamProxy );
		}
		
		int IGATAudioThreadStreamOwner.NbOfStreams{ get{ return 1; } }
		
		#endregion
		
		protected virtual void OnAudioFilterRead( float[] data, int numChannels )
		{
			int dataLength = data.Length;
			
			_audioThreadStreamProxy.BroadcastStream( data, 0, false );
			
			if( !playThrough )
			{
				System.Array.Clear( data, 0, dataLength );
			}
		}
	}
}

