//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Component for caching a stream to memory.
	/// Multichannel streams will be cached in seperate
	/// mono caches. Supports sample accurate start and
	/// stop.
	/// </summary>
	public class StreamToCacheModule : AGATStreamObserver
	{
		/// <summary>
		/// If true, caches will be allocated
		/// in start.
		/// </summary>
		public bool allocateCacheInStart;
		
		/// <summary>
		/// Duration of the cache to allocate in Start
		/// </summary>
		public double cacheDuration;
		
		/// <summary>
		/// Should the cache be allocated using
		/// G-Audio's allocator? Make sure 
		/// GATManager.DefaultAllocator.LargestAllocatableChunk
		/// is large enough if you use managed data.
		/// </summary>
		public bool useManagedData;
		
		/// <summary>
		/// Retrieve caches here.
		/// Caches can also be set after allocating them 
		/// manually. Alternatively, calling AllocateCaches
		/// will both allocate and set.
		/// Note that caches are retained and released automatically:
		/// if you set a new cache and need to keep the previous one alive,
		/// you should first retain the previous cache ( call Retain() on 
		/// all of it's GATData objects ).
		/// </summary>
		public GATData[] Caches
		{
			get
			{
				if( _streamToCache == null )
					return null;
				
				return _streamToCache.Caches;
			}
			set
			{
				if( _streamToCache == null )
					return;
				
				_streamToCache.Caches = value;
			}
		}
		
		/// <summary>
		/// If true, data will be additively mixed to the cache.
		/// </summary>
		public bool Overdub
		{
			get{ return _streamToCache.Overdub; }
			set
			{ 
				if( _streamToCache.Overdub == value )
					return;
				
				_streamToCache.Overdub = value; 
			}
		}
		
		/// <summary>
		/// Current position in the cache.
		/// </summary>
		public int RecPosition
		{
			get
			{
				if( _streamToCache == null )
					return 0;
				
				return _streamToCache.Position;
			}
		}
		
		/// <summary>
		/// If true, caching will not stop
		/// when the end of the cache is reached, but wrap.
		/// </summary>
		public bool LoopedRec
		{
			get{ return _loopedCaching; }
			set
			{ 
				_loopedCaching = value;
				
				if( _streamToCache != null )
				{
					_streamToCache.Loop = value;
				}
			}
		}
		
		#region Private
		
		GATAudioThreadStreamToCache _streamToCache;
		
		int  _cacheNumFrames;
		bool _loopedCaching;
		bool _isInited;
		
		protected override void Start()
		{
			if( _isInited )
				return;
			
			base.Start();
			
			if( _stream == null )
			{
				this.enabled = false;
				return;
			}
			
			_streamToCache = new GATAudioThreadStreamToCache( _stream, null );
			
			_isInited = true;
			
			if( allocateCacheInStart )
			{
				AllocateCaches( cacheDuration, useManagedData );
			}
		}
		
		void OnDestroy()
		{
			ReleaseCache();
		}
		
		#endregion
		
		/// <summary>
		/// Allocates caches retrievable in the Caches property.
		/// </summary>
		public void AllocateCaches( double duration, bool managedData )
		{
			if( !_isInited )
				Start ();
			
			if( _streamToCache != null && Caches != null )
			{
				_streamToCache.ReleaseCache();
			}
			
			cacheDuration = duration;
			
			_cacheNumFrames = ( int )( cacheDuration * GATInfo.OutputSampleRate );
			
			useManagedData = managedData;
			
			GATData[] caches = new GATData[ _stream.NbOfChannels ];
			
			int i;
			
			for( i = 0; i < caches.Length; i++ )
			{
				if( useManagedData )
				{
					if( _cacheNumFrames > GATManager.DefaultDataAllocator.LargestFreeChunkSize )
					{
						int j;
						for( j = 0; j < i; j++ )
						{
							caches[ i ].Release();
						}
						throw new GATException( "Chunk is too large to be allocated in managed memory, consider using unmanaged setting" );
					}
					
					caches[ i ] = GATManager.GetDataContainer( _cacheNumFrames );
				}
				else
				{
					caches[ i ] = new GATData( new float[ _cacheNumFrames ] );
				}
			}
			
			_streamToCache.Loop   = _loopedCaching;
			_streamToCache.Caches = caches;
		}
		
		/// <summary>
		/// Starts caching the stream at a precise dspTime, 
		/// or asap if no dspTime is provided or if the provided
		/// dspTime is too soon. onAtEnd callback is fired when the cache is full,
		/// wether looping or not.
		/// </summary>
		public void StartCaching( double dspTime = 0d, GATAudioThreadStreamToCache.AtEndHandler onAtEnd = null )
		{
			if( _streamToCache == null || Caches == null )
			{
				throw new GATException( "No cache is setup." );
			}
			
			_streamToCache.Start( dspTime, onAtEnd );
		}
		
		/// <summary>
		/// Immediately stops caching the stream.
		/// </summary>
		public void StopCaching()
		{
			if( _streamToCache == null )
			{
				return;
			}
			
			_streamToCache.Stop();
		}
		
		/// <summary>
		/// If allocated caches use managed data,
		/// calling this method makes sure that caches
		/// will be returned to the memory pool when 
		/// possible. 
		/// </summary>
		public void ReleaseCache()
		{
			if( _streamToCache == null )
				return;
			
			_streamToCache.ReleaseCache();
		}
		
		
	}
}

