//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System;

namespace GAudio
{
	public class GATAudioThreadStreamProxy : IGATAudioThreadStream 
	{
		OnAudioThreadStream _onAudioThreadStream;
		
		public bool HasClient{ get{ return _onAudioThreadStream != null; } }
		
		public GATAudioThreadStreamProxy( int bufferSizePerChannel, int nbOfChannels, IntPtr bufferPointer, int bufferOffset, string streamName = null )
		{
			_bufferSizePerChannel 	= bufferSizePerChannel;
			_nbOfChannels 			= nbOfChannels;
			_bufferPointer			= bufferPointer;
			_bufferOffset			= bufferOffset;
			_streamName				= streamName;
		}
		
		public void BroadcastStream( float[] data, int offset, bool isEmptyData )
		{
			if( _onAudioThreadStream != null )
			{
				_onAudioThreadStream( data, offset, isEmptyData, this );
			}
		}
		
		#region IGATAudioThreadStream explicit implementation
		public void AddAudioThreadStreamClient( IGATAudioThreadStreamClient client )
		{
			_onAudioThreadStream += client.HandleAudioThreadStream;
		}
		
		public void RemoveAudioThreadStreamClient( IGATAudioThreadStreamClient client )
		{
			if( _onAudioThreadStream != null )
			{
				_onAudioThreadStream -= client.HandleAudioThreadStream;
			}
		}
		
		int            IGATAudioThreadStream.NbOfChannels{ get{ return _nbOfChannels; 			} }
		int    IGATAudioThreadStream.BufferSizePerChannel{ get{ return _bufferSizePerChannel; 	} }
		System.IntPtr IGATAudioThreadStream.BufferPointer{ get{ return _bufferPointer; 			} }
		int			   IGATAudioThreadStream.BufferOffset{ get{ return _bufferOffset; 			} }
		string           IGATAudioThreadStream.StreamName{ get{ return _streamName; 			} }
		
		int 			_nbOfChannels;
		int 			_bufferSizePerChannel;
		System.IntPtr 	_bufferPointer;
		int				_bufferOffset;
		string 			_streamName;
		
		#endregion
		
		
	}
}

