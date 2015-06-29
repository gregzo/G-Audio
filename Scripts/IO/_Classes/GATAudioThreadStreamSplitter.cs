//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace GAudio
{
	/// <summary>
	/// Splits a multi channel input stream in as many
	/// mono output streams.
	/// </summary>
	public class GATAudioThreadStreamSplitter : IGATAudioThreadStreamClient, IGATAudioThreadStreamOwner, IDisposable
	{
		GATAudioThreadStreamProxy[] _streamProxies;
		
		GATData _sharedBuffer;
		float[] _sharedBufferArray;
		IntPtr  _sharedBufferPointer;
		int 	_memOffset;
		int 	_sharedBufferSize;
		
		int _sourceStreamChannels;
		IGATAudioThreadStream _sourceStream;
		
		/// <summary>
		/// The splitter will begin broadcasting it's 
		/// sub streams immediately. 
		/// </summary>
		public GATAudioThreadStreamSplitter( IGATAudioThreadStream stream, GATDataAllocationMode bufferAllocationMode )
		{
			int i;
			
			_sourceStreamChannels = stream.NbOfChannels;
			if( _sourceStreamChannels < 2 )
			{
				Debug.LogWarning( "source stream is mono: " + stream.StreamName );
			}
			
			IntPtr outputBufferPointer = IntPtr.Zero;
			
			_sharedBufferSize	= stream.BufferSizePerChannel;
			
			
			if( bufferAllocationMode == GATDataAllocationMode.Unmanaged )
			{
				_sharedBufferArray = new float[ _sharedBufferSize ];
				_sharedBuffer = new GATData( _sharedBufferArray );
			}
			else 
			{
				if( bufferAllocationMode == GATDataAllocationMode.Fixed )
				{
					_sharedBuffer = GATManager.GetFixedDataContainer( _sharedBufferSize, "StreamSplitter buffer" ); 
				}
				else
				{
					_sharedBuffer = GATManager.GetDataContainer( _sharedBufferSize ); 
				}
				
				_sharedBufferArray 	= _sharedBuffer.ParentArray;
				outputBufferPointer = _sharedBuffer.GetPointer();
			}
			
			_memOffset			= _sharedBuffer.MemOffset;
			
			_streamProxies = new GATAudioThreadStreamProxy[ _sourceStreamChannels ];
			
			for( i = 0; i < _sourceStreamChannels; i++ )
			{
				_streamProxies[ i ] = new GATAudioThreadStreamProxy( _sharedBufferSize, 1, outputBufferPointer, _sharedBuffer.MemOffset, ( stream.StreamName + " split " + i ) );
			}
			
			stream.AddAudioThreadStreamClient( this );
			
			_sourceStream = stream;
		}
		
		void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool isEmptyData, IGATAudioThreadStream stream )
		{
			int i;
			int sourceIndex;
			int targetIndex;
			int toIndex;
			
			GATAudioThreadStreamProxy proxy;
			
			for( i = 0; i < _sourceStreamChannels; i++ )
			{
				proxy = _streamProxies[ i ];
				
				if( proxy.HasClient && isEmptyData == false ) //No need to de-interlace if the current channel has no listener or data is empty!
				{
					sourceIndex = offset + i;
					targetIndex = _memOffset;
					toIndex     = targetIndex + _sharedBufferSize;
					
					while( targetIndex < toIndex )
					{
						_sharedBufferArray[ targetIndex ] = data[ sourceIndex ];
						targetIndex++;
						sourceIndex += _sourceStreamChannels;
					}
				}	
				
				proxy.BroadcastStream( _sharedBufferArray, _memOffset, isEmptyData ); // no need to clear buffer if no data, let clients handle the flag
			}
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="GATAudioThreadStreamSplitter"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="GATAudioThreadStreamSplitter"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="GATAudioThreadStreamSplitter"/> in an unusable state. After
		/// calling <see cref="Dispose"/>, you must release all references to the <see cref="GATAudioThreadStreamSplitter"/> so
		/// the garbage collector can reclaim the memory that the <see cref="GATAudioThreadStreamSplitter"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose ( true );
			GC.SuppressFinalize( this );
		}
		
		bool _disposed;
		protected void Dispose( bool explicitely )
		{
			if( _disposed )
				return;
			
			_sourceStream.RemoveAudioThreadStreamClient( this );
			_sourceStream = null;
			_sharedBuffer.Release();
			_sharedBuffer = null;
			_disposed = true;
		}
		
		~GATAudioThreadStreamSplitter()
		{
			Dispose ( false );
		}
		
		#region IGATAudioThreadStreamOwner Implementation
		
		
		public IGATAudioThreadStream GetAudioThreadStream( int index = 0 )
		{
			if( index >= _sourceStreamChannels )
			{
				return null;
			}
			return _streamProxies[ index ];
		}
		
		public int NbOfStreams{ get{ return _sourceStreamChannels; } }
		#endregion
	}
}

