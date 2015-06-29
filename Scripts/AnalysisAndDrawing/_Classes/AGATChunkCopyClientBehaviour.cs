//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Abstract implementation of IGATChunkCopyClient
	/// which provides overridable methods to handle
	/// from the main thread reception of audio data
	/// copied by the audio thread. 
	/// Warning: this implementation only requests
	/// copies when necessary: not all mixed audio data 
	/// chunks will be received!
	/// </summary>
	public abstract class AGATChunkCopyClientBehaviour : MonoBehaviour, IGATAudioThreadStreamClient
	{
		/// <summary>
		/// The component that owns the observed audio stream.
		/// </summary>
		public Component observedAudioStreamComp;
		
		/// <summary>
		/// Are we observing a player's track?
		/// </summary>
		public bool observeTrack;
		
		/// <summary>
		/// If the observed audio stream is multi channel,
		/// which channel should we de-interleave?
		/// </summary>
		public int observedChannel = 0;
		
		/// <summary>
		/// Holds a copy of the last received audio data.
		/// </summary>
		protected float[] _data;
		
		protected IGATAudioThreadStream _observedStream;
		
		/// <summary>
		/// If overriden in a derived class,
		/// base.Start() should be called first
		/// </summary>
		protected virtual void Start()
		{
			UpdateObservedStream();
		}
		
		/// <summary>
		/// Called from Update() whenever new data is
		/// available.
		/// </summary>
		protected abstract void HandleAudioDataUpdate();
		
		/// <summary>
		/// Called from Update() when the audio thread 
		/// has empty data only. Data is not copied to
		/// _data.
		/// </summary>
		protected abstract void HandleNoMoreData(); 
		
		
		#region Private
		
		private Component		_cachedStreamComp;
		
		private volatile bool 	_dataIsUpdated,
		_receivedZeroData,
		_needsData;
		
		private 		 bool   _inZeroState;
		
		private void Awake()
		{
			_data = new float[ GATInfo.AudioBufferSizePerChannel ];
			_needsData = true;
			_inZeroState = true;
		}
		
		void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
		{
			if( _needsData == false )
				return;
			
			if( !emptyData )
			{
				int sourceChannels = stream.NbOfChannels; //May change!
				int length 		   = stream.BufferSizePerChannel;
				
				if( sourceChannels == 1 )
				{
					System.Array.Copy ( data, offset, _data, 0, length );
				}
				else //deinterlace
				{
					int i = 0;
					length *= sourceChannels;
					offset += observedChannel;
					while( offset < length )
					{
						_data[i] = data[ offset ];
						offset += sourceChannels;
						i++;
					}
				}
			}
			
			_receivedZeroData = emptyData;
			_needsData		  = false;
			_dataIsUpdated    = true;
		}
		
		private void Update()
		{	
			if( _dataIsUpdated  )
			{
				if( _receivedZeroData == false )
				{
					HandleAudioDataUpdate();
					
					_inZeroState = false;
				}
				else
				{
					if( _inZeroState == false )
					{
						HandleNoMoreData();
						_inZeroState = true;
					}
				}
				
				_dataIsUpdated = false;
				_needsData = true;
			}
			
			if( observedAudioStreamComp != _cachedStreamComp )
			{
				UpdateObservedStream();
			}
		}
		
		private void UpdateObservedStream()
		{		
			IGATAudioThreadStream stream = null;
			
			if( observeTrack )
			{
				GATPlayer player = observedAudioStreamComp as GATPlayer;
				if( player == null )
				{
					Debug.LogWarning( "Could not find Player to observe track " + observedAudioStreamComp.name );
					return;
				}
				
				GATTrack track = player.GetTrack( observedChannel );
				
				stream = ( ( IGATAudioThreadStreamOwner )track ).GetAudioThreadStream( 0 );
			}
			else if( observedAudioStreamComp != null )
			{
				stream = observedAudioStreamComp as IGATAudioThreadStream;
				
				if( stream == null )
				{
					IGATAudioThreadStreamOwner streamOwner;
					streamOwner = observedAudioStreamComp as IGATAudioThreadStreamOwner;
					if( streamOwner != null )
					{
						stream = streamOwner.GetAudioThreadStream( 0 );
					}
					
					if( stream == null )
					{
						Debug.LogWarning( "Could not find IGATAudioThreadStream or IGATAudioThreadStreamOwner on GameObject " + observedAudioStreamComp.name );
						observedAudioStreamComp = _cachedStreamComp;
						return;
					}
				}
			}
			
			if( _observedStream != null )
			{
				_observedStream.RemoveAudioThreadStreamClient( this );
			}
			
			if( stream != null )
			{
				stream.AddAudioThreadStreamClient( this );
			}
			else
			{
				_dataIsUpdated = false;
				_needsData = true;
				HandleNoMoreData();
			}
			
			_observedStream = stream;
			_cachedStreamComp = observedAudioStreamComp;
		}
		
		#endregion
	}
}

