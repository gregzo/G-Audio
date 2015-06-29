//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GAudio
{
	public interface IGATDataAllocatorOwner 
	{
		GATDataAllocator DataAllocator{ get; }
		GATDataAllocator.InitializationSettings AllocatorInitSettings{ get; }
	}
	
	/// <summary>
	/// Pre-allocates a large float[] to use
	/// as virtual memory and avoid any garbage
	/// collection. All calls to GATActiveSampleBank 
	/// and GATReamplingSampleBank GetProcessedSample methods
	/// result in a virtual allocation from GATManager's default
	/// GATDataAllocator instance.
	/// </summary>
	public sealed class GATDataAllocator : IDisposable
	{
		/// <summary>
		/// Gets the size of the largest free chunk( read only ).
		/// Note that this value cannot be greater than binWidth * nbOfBins, 
		/// the largest bin.
		/// </summary>
		public int LargestFreeChunkSize
		{
			get
			{
				if( _unallocatedCursor.MaxSize >= _maxBinSize )
				{
					return _maxBinSize;
				}
				else
				{
					int i;
					for( i = _nbOfBins - 1; i >= 0; i-- )
					{
						if( _freeChunksBins[i].Count > 0 )
						{
							return ( _binWidth + i * _binWidth );
						}
					}
				}
				
				return 0;
			}
		}
		
		/// <summary>
		/// The length of the pre-allocated array
		/// </summary>
		public int TotalSize{ get{ return _totalSize; } }
		
		/// <summary>
		/// The width multiplier for the Stacks holding
		/// free data chunks
		/// </summary>
		public int BinWidth{ get{ return _binWidth; } }
		
		/// <summary>
		/// The number of bins
		/// </summary>
		public int NbOfBins{ get{ return _freeChunksBins.Length; } }
		
		/// <summary>
		/// Size available to be
		/// split in chunks if no
		/// previously allocated chunk can be recycled.
		/// </summary>
		public int UnfragmentedSize
		{
			get
			{
				return _unallocatedCursor.MaxSize;
			}
		}
		
		/// <summary>
		/// A conservative estimate of the
		/// available number of chunks. Takes
		/// into account both unfragmented memory
		/// and recyclable previously allocated chunks.
		/// </summary>
		public int NbOfAvailableChunksForSize( int size )
		{
			int binIndex = GetBinIndexForSize( size );
			int total = 0;
			while( binIndex < _nbOfBins )
			{
				total += _freeChunksBins[ binIndex ].Count;
				binIndex++;
			}
			
			total += ( _unallocatedCursor.MaxSize / size );
			
			return total;
		}
		
		/// <summary>
		/// The size of the pre-allocated array reserved by fixed allocations
		/// </summary>
		public int FixedAllocationsSize{ get{ return _fixedAllocationsSize; } }
		int _fixedAllocationsSize;
		
		/// <summary>
		/// The size of available memory afer substracting fixed allocations
		/// </summary>
		public int TotalNonFixedSize{ get{ return _totalSize - _fixedAllocationsSize; } }
		
		#region Private Members
		private readonly float[] _mainBuffer;
		
		private GATManagedData 	_firstCursor, 
		_unallocatedCursor, 
		_endCursor;
		
		private readonly Stack< GATManagedData > 	_pool;
		private readonly Stack< GATManagedData >[] 	_freeChunksBins;
		
		private readonly int 	_binWidth,
		_nbOfBins,
		_maxBinSize,
		_totalSize;
		
		
		private GCHandle _mainBufferHandle;
		private System.IntPtr _mainBufferPointer;
		
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="GATDataAllocator"/> class.
		/// In most cases, only the default allocator initialized by GATManager is
		/// required.
		/// </summary>
		public GATDataAllocator( InitializationSettings initSettings )
		{
			int i;
			
			_totalSize	 = ( int )( initSettings.preAllocatedAudioDuration * GATInfo.OutputSampleRate );
			_mainBuffer  = new float[ _totalSize ];
			_pool		 = new Stack< GATManagedData >( initSettings.maxConcurrentSamples );
			_binWidth    = initSettings.binWidth;
			_nbOfBins	 = initSettings.nbOfBins;
			
			
			for( i = 0; i < initSettings.maxConcurrentSamples; i++ )
			{
				GATManagedData data = new GATManagedData( this );
				_pool.Push( data );
			}
			
			_freeChunksBins = new Stack< GATManagedData >[ _nbOfBins ];
			
			for( i = 0; i < _nbOfBins; i++ )
			{
				_freeChunksBins[i] = new Stack< GATManagedData >(20);
			}
			
			InitCursors();
			
			_maxBinSize = _nbOfBins * _binWidth;
			
			#if !UNITY_WEBPLAYER
			_mainBufferHandle = GCHandle.Alloc( _mainBuffer, GCHandleType.Pinned );
			_mainBufferPointer = _mainBufferHandle.AddrOfPinnedObject();
			#endif
			
			
			#if GAT_DEBUG
			
			Debug.Log( "GATDataAllocator initialized, total size: " + _totalSize + " largest allocatable chunk: " + _maxBinSize );
			
			#endif
		}
		
		/// <summary>
		/// Finds and virtually allocates a GATManagedData instance before
		/// returning it as a GATData reference.
		/// First, the algorithm looks at the free chunks bin of appropriate size.
		/// If the bin doesn't hold any chunk, it will check if there is enough
		/// unfragmented space. If there isn't, it will look at bins holding larger chunks
		/// and fragment one if found. Finally, it will attempt defragmenting and run again,
		/// before logging an out of memory error.
		/// </summary>
		/// <returns>
		/// A GATData reference to a GATManagedData instance.
		/// </returns>
		/// <param name='size'>
		/// Size of the chunk to virtually allocate.
		/// </param>
		public GATData GetDataContainer( int size )
		{
			GATManagedData chunk = null;
			int binIndex;
			int binSize;
			
			binIndex = GetBinIndexForSize( size );
			binSize  = _binWidth + binIndex * _binWidth;
			
			if( _freeChunksBins[ binIndex ].Count != 0 )
			{
				chunk = _freeChunksBins[ binIndex ].Pop ();
			}
			else
			{
				if( _unallocatedCursor.MaxSize >= binSize )
				{
					chunk = _unallocatedCursor;
					chunk.allocatedSize = size;
					_unallocatedCursor = GetOrMakeChunk();
					_unallocatedCursor.AllocateFree( chunk.MemOffset + binSize, _endCursor );
					chunk.next = _unallocatedCursor;
				}
				else 
				{
					if( TryFragmentBins( binIndex + 1, binSize, ref chunk ) == false )
					{
						Defragment();
						
						if( _freeChunksBins[ binIndex ].Count != 0 )
						{
							chunk = _freeChunksBins[ binIndex ].Pop ();
						}
						else
						{
							if( _unallocatedCursor.MaxSize >= binSize )
							{
								chunk = _unallocatedCursor;
								chunk.allocatedSize = size;
								_unallocatedCursor = GetOrMakeChunk();
								_unallocatedCursor.AllocateFree( chunk.MemOffset + binSize, _endCursor );
								chunk.next = _unallocatedCursor;
							}
							else if( TryFragmentBins( binIndex + 1, binSize, ref chunk ) == false )
							{
								throw new GATException( "Out of memory!" );
							}
						}
					}
				}
			}
			
			chunk.allocatedSize = size;
			
			return chunk;
		}
		
		/// <summary>
		/// Allocates a fixed chunk of memory.
		/// Fixed chunks cannot be freed, but may be of any size.
		/// For debugging ease, a description is required when requesting
		/// a fixed chunk.
		/// </summary>
		public GATData GetFixedDataContainer( int size, string description )
		{
			if( _unallocatedCursor.MaxSize < size )
			{
				throw new GATException( "Out of fixed memory!" );
			}
			
			int offset;
			GATFixedData ret;
			
			offset = _endCursor.MemOffset - size;
			ret = new GATFixedData( this, description );
			ret.AllocateFree( offset, _endCursor.next );
			ret.allocatedSize = size;
			_endCursor.AllocateFree( offset, ret );
			
			_fixedAllocationsSize += size;
			return ret;
		}
		
		/// <summary>
		/// Defragments contiguous free chunks.
		/// Automatically called when a chunk of appropriate size
		/// cannot be recycled.
		/// </summary>
		public void Defragment()
		{
			GATManagedData chunk;
			GATManagedData nextChunk;
			int i;
			
			chunk = _firstCursor.next;
			
			if( chunk == _unallocatedCursor )
			{
				return; //Nothing to defrag
			}
			
			nextChunk = chunk.next;
			
			for( i = 0; i < _freeChunksBins.Length; i++ )
			{
				_freeChunksBins[i].Clear();
			}
			
			while( nextChunk != _unallocatedCursor )
			{
				if( chunk.allocatedSize == 0 )
				{
					if( nextChunk.allocatedSize == 0  )
					{
						chunk.next = nextChunk.next;
						_pool.Push( nextChunk );
						nextChunk = chunk.next;
					}
					else
					{
						while( chunk.MaxSize > _maxBinSize ) // fragment and bin
						{
							GATManagedData subChunk;
							subChunk = GetOrMakeChunk();
							subChunk.AllocateFree( chunk.MemOffset + _maxBinSize, nextChunk );
							chunk.next = subChunk;
							AddToFreeChunksBins( chunk );
							chunk = subChunk;
						}
						
						if( chunk.MaxSize != 0 )
						{
							AddToFreeChunksBins( chunk );
						}
						else
						{
							_pool.Push( chunk );
						}
						
						chunk = nextChunk;
						nextChunk = nextChunk.next;
					}
				}
				else
				{
					chunk = nextChunk;
					nextChunk = nextChunk.next;
				}
			}
			
			//Now chunk.next is _unallocatedChunk, concatenate if free
			if( chunk.allocatedSize == 0 )
			{
				_pool.Push( _unallocatedCursor );
				_unallocatedCursor = chunk;
				_unallocatedCursor.next = _endCursor;
			}	
		}
		
		
		public void CleanUp()
		{
			if( _mainBufferHandle.IsAllocated )
			{
				_mainBufferHandle.Free();
				_mainBufferPointer = System.IntPtr.Zero;
			}
		}
		
		#region Debug Methods
		
		/// <summary>
		/// Gets information regarding each allocated chunk.
		/// </summary>
		public List< GATMemDebugInfo > GetDebugInfo()
		{
			GATManagedData chunk;
			int counter = 0;
			List<GATMemDebugInfo> debugInfo = new List<GATMemDebugInfo>();
			
			chunk = _firstCursor.next;
			
			while( chunk.next != _endCursor )
			{
				debugInfo.Add ( new GATMemDebugInfo( counter, chunk.allocatedSize, chunk.MaxSize ) );
				counter++;
				chunk = chunk.next;
			}
			
			return debugInfo;
		}
		
		/// <summary>
		/// Gets information regarding fixed allocations.
		/// </summary>
		public List<GATFixedMemDebugInfo> GetFixedDebugInfo()
		{
			GATManagedData chunk;
			GATFixedData fixedData;
			int counter = 0;
			List<GATFixedMemDebugInfo> debugInfo = new List<GATFixedMemDebugInfo>();
			
			chunk = _endCursor.next;
			
			
			while( chunk != null )
			{
				fixedData = chunk as GATFixedData;
				debugInfo.Add ( new GATFixedMemDebugInfo( counter, fixedData.allocatedSize, fixedData.Description ) );
				counter++;
				chunk = chunk.next;
			}
			
			return debugInfo;
		}
		
		#endregion
		
		#region Private Helper Methods
		
		private void InitCursors()
		{
			_endCursor = new GATFixedData( this, "" );
			_endCursor.AllocateFree( _mainBuffer.Length, null );
			
			_unallocatedCursor = new GATManagedData( this );
			_unallocatedCursor.AllocateFree( 0, _endCursor );
			
			_firstCursor = new GATManagedData( this );
			_firstCursor.AllocateFree( 0, _unallocatedCursor );
		}
		
		private GATManagedData GetOrMakeChunk()
		{	
			if( _pool.Count != 0 )
			{
				return _pool.Pop();
			}
			else 
			{
				GATManagedData data = new GATManagedData( this );
				return data;
			}
		}
		
		private int GetBinIndexForSize( int size )
		{
			if( size <= _binWidth )
			{
				return 0;
			}
			
			int binIndex = ( ( size - _binWidth - 1 ) / _binWidth ) + 1;
			
			if( binIndex >= _nbOfBins )
			{
				Debug.LogError( "no such bin" );
				return -1;
			}
			
			return binIndex;
		}
		
		private void AddToFreeChunksBins( GATManagedData chunk )
		{
			int binIndex;
			int size;
			
			size = chunk.MaxSize;
			
			binIndex = ( size - _binWidth ) / _binWidth;
			
			_freeChunksBins[ binIndex ].Push( chunk );
		}
		
		bool TryFragmentBins( int fromBinIndex, int binSize, ref GATManagedData chunk )
		{
			for( int i = fromBinIndex; i < _nbOfBins; i++ )
			{
				if( _freeChunksBins[ i ].Count != 0 )
				{
					chunk = _freeChunksBins[ i ].Pop ();
					GATManagedData subChunk = GetOrMakeChunk();
					subChunk.AllocateFree( chunk.MemOffset + binSize, chunk.next );
					chunk.next = subChunk;
					AddToFreeChunksBins( subChunk );
					return true;
				}
			}
			return false;
		}
		
		#endregion
		
		public class GATManagedData : GATData
		{	
			protected readonly GATDataAllocator _manager;
			
			public GATManagedData next;
			
			public int allocatedSize;
			
			public virtual int MaxSize{ get{ return next.MemOffset - _offset; } }
			
			public GATManagedData( GATDataAllocator manager ) : base( manager._mainBuffer )
			{
				_manager = manager;
			}
			
			protected override void Discard ()
			{
				allocatedSize = 0;
				_manager.AddToFreeChunksBins( this );
			}
			
			public void AllocateFree( int offset, GATManagedData inext )
			{
				next 	= inext;
				_offset = offset;
				
				allocatedSize 	= 0; 
				_retainCount 	= 0;
			}
			
			public override int Count{ get{ return allocatedSize; } }
			
			public override System.IntPtr GetPointer()
			{
				return _manager._mainBufferPointer;
			}
		}
		
		public class GATFixedData : GATManagedData
		{
			public override int MaxSize{ get{ return allocatedSize; } }
			
			public string Description{ get; protected set; }
			
			public GATFixedData( GATDataAllocator manager, string description ) : base( manager )
			{
				Description = description;
			}
			
			protected override void Discard ()
			{
				//Do nothing, fixed data!
			}
		}
		
		[ System.Serializable ]
		public class InitializationSettings
		{
			public float 	preAllocatedAudioDuration 		= 100;
			public int 		maxConcurrentSamples			= 50;
			public int 		binWidth						= 16384;
			public int 		nbOfBins						= 20;
		}
		
		#region IDisposable Implementation
		
		bool _disposed;
		
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		void Dispose( bool explicitly )
		{
			if ( _disposed )
				return;
			
			CleanUp();
			
			_disposed = true;
		}
		
		~GATDataAllocator()
		{
			Dispose( false );
		}
		
		#endregion
	}
}
