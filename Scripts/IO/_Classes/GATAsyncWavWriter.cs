//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
#if !GAT_NO_THREADING
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Threading;

namespace GAudio
{
	/// <summary>
	/// Creates or overwrites a wav file.
	/// Writing to the file and finalizing the header 
	/// is done on a seperate thread. Input is cached in
	/// a ring buffer, and written in chunks of size
	/// AudioSettings.dspBufferSize. Note: this class is
	/// lockless.
	/// </summary>
	public class GATAsyncWavWriter : IDisposable
	{
		/// <summary>
		/// How many frames maximum are written at a time?
		/// </summary>
		public readonly int WriteChunkFrames;
		
		#region Private Members
		const int RESCALE_FACTOR = 32767;
		
		bool _disposed;
		
		float[] _inputBuffer;
		Int16[] _intBuffer;
		byte[]  _bytesBuffer;
		
		volatile bool _vDoWrite;
		volatile int  _vReceivedFrames;
		int 		  _writtenFrames;
		
		Thread 		_thread;
		FileStream 	_fs;
		
		int 	_numChannels;
		string 	_path;
		
		int _nextInputOffset;
		int _nextWriteOffset;
		int _inputBufferSize;
		
		#endregion
		
		/// <summary>
		/// filePath should be absolute and valid. If the file doesn't exist,
		/// it will be immediately created.
		/// </summary>
		public GATAsyncWavWriter( string filePath, int numChannels, bool overwrite )
		{
			int bufferSize = GATInfo.AudioBufferSizePerChannel * numChannels;
			
			_inputBufferSize = bufferSize * 4;
			
			_inputBuffer = new float[ _inputBufferSize ];
			_intBuffer   = new Int16[ bufferSize ];
			_bytesBuffer = new  byte[ bufferSize * 2 ];
			
			_numChannels 		= numChannels;
			WriteChunkFrames   = GATInfo.AudioBufferSizePerChannel;
			
			_path = filePath;
			
			_fs = new FileStream( _path, FileMode.Create, FileAccess.Write );	
		}
		
		/// <summary>
		/// You must call PrepareToWrite before the first 
		/// WriteStreamAsync call. This starts a thread which
		/// waits for input and writes to disk when enough data
		/// has been received.
		/// </summary>
		public void PrepareToWrite()
		{
			_vDoWrite = true;
			_thread = new Thread( AsyncWriteLoop );
			_thread.Start();
		}
		
		/// <summary>
		/// Hand out data here sequentially.
		/// If PrepareToWrite has not been called before, this method will
		/// have no effect.
		/// Note that numFrames should not exceed WriteChunkFrames.
		/// </summary>
		public void WriteStreamAsync( float[] data, int offset, int numFrames )
		{
			if( !_vDoWrite )
				return;
			
			int length = numFrames * _numChannels;
			
			int appliedLength = length;
			
			if( offset + appliedLength > data.Length )
			{
				throw new GATException( "Cannot write, out of range!" );
			}
			
			if( appliedLength + _nextInputOffset >= _inputBufferSize )
			{
				appliedLength = _inputBufferSize - _nextInputOffset;
				System.Array.Copy( data, offset, _inputBuffer, _nextInputOffset, appliedLength );
				
				offset += appliedLength;
				appliedLength = length - appliedLength;
				_nextInputOffset = 0;
			}
			
			System.Array.Copy( data, offset, _inputBuffer, _nextInputOffset, appliedLength );
			_vReceivedFrames   += numFrames;
			_nextInputOffset   += appliedLength;
		}
		
		/// <summary>
		/// Blocks further input, flushes what is left 
		/// in the ring buffer to disk, and finalizes the header.
		/// As these operations are async, the file will not be immediately ready
		/// to be opened.
		/// </summary>
		public void StopAndFinalize()
		{
			_vDoWrite = false;
		}
		
		#region IDisposable Implementation
		
		/// <summary>
		/// Releases all resource used by the <see cref="GATAsyncWavWriter"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="GATAsyncWavWriter"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="GATAsyncWavWriter"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="GATAsyncWavWriter"/> so the garbage
		/// collector can reclaim the memory that the <see cref="GATAsyncWavWriter"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		protected virtual void Dispose( bool explicitly )
		{
			if (_disposed)
				return;
			
			_vDoWrite = false;
			
			_disposed = true;
		}
		
		~GATAsyncWavWriter()
		{
			Dispose( false );
		}
		
		#endregion
		
		#region Private Methods
		void AsyncWriteLoop()
		{
			_fs.Seek( GATWavHelper.headerSize, SeekOrigin.Begin );
			
			int sleepMS;
			int receivedFrames = 0; 
			
			sleepMS = ( int )( GATInfo.AudioBufferDuration * 2000 / 3 );
			
			while( _vDoWrite )
			{
				receivedFrames = _vReceivedFrames; // cache volatile locally
				if( receivedFrames >= _writtenFrames + WriteChunkFrames )
				{
					ConvertAndWriteChunk( WriteChunkFrames );
				}
				else
				{
					Thread.Sleep( sleepMS );
				}
			}
			
			while( receivedFrames > _writtenFrames )
			{
				if( receivedFrames < WriteChunkFrames )
				{
					ConvertAndWriteChunk( receivedFrames - _writtenFrames );
				}
				else
				{
					ConvertAndWriteChunk( WriteChunkFrames );
				}
			}
			
			byte[] header = null;
			
			header = GATWavHelper.GetHeader( _numChannels, GATInfo.OutputSampleRate, ( int )_fs.Length );
			
			_fs.Seek( 0, SeekOrigin.Begin );
			
			_fs.Write( header, 0, header.Length );
			
			_fs.Close();
		}
		
		void ConvertAndWriteChunk( int numFrames )
		{
			int i;
			int j;
			int length = numFrames * _numChannels;
			
			for( i = 0, j = _nextWriteOffset; i < length; i++, j++ )
			{
				_intBuffer[ i ] = ( Int16 )( _inputBuffer[ j ] * RESCALE_FACTOR );
			} 
			
			Buffer.BlockCopy( _intBuffer, 0, _bytesBuffer, 0, length * 2 );
			
			_fs.Write( _bytesBuffer, 0, length * 2 );
			
			_nextWriteOffset = ( _nextWriteOffset + length ) % _inputBufferSize; 
			_writtenFrames  += numFrames;
		}
		
		#endregion
	}
}
#endif
