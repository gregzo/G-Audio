//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Caches an input stream in seperate mono GATData containers.
	/// </summary>
	public class GATAudioThreadStreamToCache : IGATAudioThreadStreamClient
	{
		public delegate void AtEndHandler( GATData[] caches, bool willLoop );
		
		/// <summary>
		/// Should we wrap and keep caching data once we've filled the buffers?
		/// </summary>
		public bool Loop{ get; set; }
		
		/// <summary>
		/// The write position in the cache.
		/// Getter for a volatile backing field,
		/// do cache!
		/// </summary>
		public int Position
		{ 
			get{ return _vPosition; } 
		}
		
		/// <summary>
		/// The caches we write to,
		/// one per input stream channel.
		/// </summary>
		public GATData[] Caches
		{ 
			get
			{ 
				return _caches; 
			} 
			
			set
			{
				if( _caches == value )
					return;
				
				int i;
				if( _caches != null )
				{
					if( _vDoCache )
					{
						_vDoCache = false;
						_stream.RemoveAudioThreadStreamClient( this );
					}
					
					ReleaseCache();
				}
				
				if( _stream.NbOfChannels != value.Length )
				{
					throw new GATException( "The number of caches must match the stream's number of channels ( caches are mono )" );
				}
				
				_cacheFrames = value[ 0 ].Count;
				
				for( i = 1; i < value.Length; i++ )
				{
					if( value[ i ].Count != _cacheFrames )
					{
						throw new GATException( "All caches must be of equal length!" );
					}
				}
				
				_caches = value;
				for( i = 0; i < _caches.Length; i++ )
				{
					_caches[ i ].Retain();
				}
			}
		}
		
		/// <summary>
		/// If true, data will be additively mixed to the cache.
		/// Else, the cache will be overwritten.
		/// </summary>
		public bool Overdub{ get{ return _overdub; } set{ _overdub = value; } }
		
		#region Private members
		AtEndHandler _onEnd;
		GATData[] _caches;
		IGATAudioThreadStream _stream;	
		
		volatile bool _vDoCache;
		volatile int _vPosition;
		
		bool 	_overdub;
		bool 	_atEnd;
		bool 	_waiting;
		
		int  	_numFramesPerRead;
		int  	_cacheFrames;
		
		double  _targetDspTime;
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="GATAudioThreadStreamToCache"/> class.
		/// </summary>
		/// <param name="stream">The observed multichannel or mono stream.</param>
		/// <param name="caches">The caches to record to, one per channel must be provided.</param>
		/// <param name="handler">Optional callback fired when the cache is full.</param>
		public GATAudioThreadStreamToCache( IGATAudioThreadStream stream, GATData[] caches, AtEndHandler handler = null )
		{
			_numFramesPerRead = stream.BufferSizePerChannel;
			
			Caches = caches;
			
			_stream = stream;
			
			_onEnd = handler;
		}
		
		/// <summary>
		/// Start caching the stream.
		/// </summary>
		/// <param name="targetDspTime">The dsp time at which caching should start. Pass 0 to start as soon as possible.</param>
		/// <param name="handler">Optional callback fired when the cache is full.</param>
		public void Start( double targetDspTime = 0d, AtEndHandler handler = null )
		{
			if( _vDoCache || _waiting )
				return;
			
			_waiting = true;
			_vPosition = 0;
			_onEnd = handler;
			_targetDspTime = targetDspTime;
			_stream.AddAudioThreadStreamClient( this );
		}
		
		/// <summary>
		/// Stop caching immediately.
		/// </summary>
		public void Stop()
		{
			if( _vDoCache == false )
				return;
			
			_vDoCache = false;
			_stream.RemoveAudioThreadStreamClient( this );
		}
		
		/// <summary>
		/// Releases the cache objects.
		/// </summary>
		public void ReleaseCache()
		{
			int i;
			if( _caches == null )
				return;
			
			for( i = 0; i < _caches.Length; i++ )
			{
				_caches[ i ].Release();
			}
			
			_caches = null;
		}
		
		
		void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
		{
			int pos = _vPosition;
			int framesToCopy = _numFramesPerRead;
			int i;
			int numCaches = _caches.Length;
			double dspTime = AudioSettings.dspTime;
			
			if( _vDoCache == false  )
			{
				if( _targetDspTime < dspTime )
				{
					_targetDspTime = dspTime;
				}
				
				if( _targetDspTime >= dspTime  && _targetDspTime < dspTime + GATInfo.AudioBufferDuration )
				{
					if( _waiting )
					{
						_waiting  = false;
						_vDoCache = true;
						
						int frameOffsetInBuffer = ( int )( ( _targetDspTime - dspTime ) * GATInfo.OutputSampleRate );
						
						framesToCopy = stream.BufferSizePerChannel - frameOffsetInBuffer;
						offset += frameOffsetInBuffer * stream.NbOfChannels;
					}
					else
					{
						return;
					}
				}
				else 
				{
					return;
				}
			}
			
			if( pos + _numFramesPerRead >= _cacheFrames )
			{
				framesToCopy = _cacheFrames - pos;
				
				if( Loop )
				{
					for( i = 0; i < numCaches; i++ )
					{
						if( _overdub )
						{
							_caches[ i ].MixFromInterlaced( data, offset, framesToCopy, pos, i, numCaches ); 
						}
						else
						{
							_caches[ i ].CopyFromInterlaced( data, offset, framesToCopy, pos, i, numCaches ); 
						}
						
					}
					
					pos = 0;
					offset += framesToCopy * stream.NbOfChannels;
					framesToCopy = _numFramesPerRead - framesToCopy;
				}
				else
				{
					_vDoCache = false;
					
					_stream.RemoveAudioThreadStreamClient( this );
				}
				
				if( _onEnd != null )
					_onEnd( _caches, Loop );
			}
			
			for( i = 0; i < numCaches; i++ )
			{
				if( _overdub )
				{
					_caches[ i ].MixFromInterlaced( data, offset, framesToCopy, pos, i, numCaches ); 
				}
				else
				{
					_caches[ i ].CopyFromInterlaced( data, offset, framesToCopy, pos, i, numCaches ); 
				}
			}
			
			pos += framesToCopy;
			
			_vPosition = pos;
		}
	}
}

