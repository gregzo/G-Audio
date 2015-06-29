//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GAudio
{
	/// <summary>
	/// Component for broadcasting as many 
	/// mono streams as the source stream's
	/// number of channels.
	/// </summary>
	public class StreamSplitterModule : AGATStreamObserver, IGATAudioThreadStreamOwner
	{
		GATAudioThreadStreamSplitter  _splitter;
		bool _initialized;
		// Use this for initialization
		protected override void Start () 
		{
			if( _initialized )
				return;
			
			base.Start ();
			
			if( _stream == null )
			{
				this.enabled = false;
			}
			
			_splitter = new GATAudioThreadStreamSplitter( _stream, GATDataAllocationMode.Fixed );
			
			_initialized = true;
		}
		
		void OnDestroy()
		{
			_splitter.Dispose();
		}
		
		IGATAudioThreadStream IGATAudioThreadStreamOwner.GetAudioThreadStream( int index )
		{
			if( !_initialized )
				Start();
			
			return _splitter.GetAudioThreadStream( index );
		}
		
		int IGATAudioThreadStreamOwner.NbOfStreams{ get{ return _splitter.NbOfStreams; } }
	}

}
