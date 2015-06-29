using UnityEngine;
using System.Collections;
using System;
using System.IO;
using NVorbis;

namespace GAudio
{
	/// <summary>
	/// Base class for reading ogg and uncompressed wav files,
	/// from disk or memory streams. Use OpenAudioFileAtPath to
	/// retrieve an instance. GATPathsHelper provides handy methods
	/// for converting relative paths to absolute paths.
	/// Don't forget to call Dispose() to close the file and
	/// release resources!
	/// </summary>
	public abstract class AGATAudioFile : IDisposable
	{
		/// <summary>
		/// The file path.
		/// </summary>
		public readonly string filePath;

		/// <summary>
		/// The file's name, including it's extension
		/// </summary>
		public 	string FileName{ get{ return Path.GetFileName( filePath ); } }

		/// <summary>
		/// The number of channels.
		/// </summary>
		public abstract int Channels{ get; }

		/// <summary>
		/// The file's sample rate.
		/// </summary>
		public abstract int SampleRate{ get; }

		/// <summary>
		/// The number of audio frames in the file.
		/// One frame contains Channels samples.
		/// </summary>
		public abstract int NumFrames{ get; }

		/// <summary>
		/// Gets or sets the read position in the audio data,
		/// in frames.
		/// </summary>
		public abstract int ReadPosition{ get; set; }

		protected int _readChunkSize;

		/// <summary>
		/// Creates and returns a ready to use AGATAudioFile object.
		/// Wrap in a try catch block if you are not sure that the 
		/// file type is supported.
		/// </summary>
		public static AGATAudioFile OpenAudioFileAtPath( string path )
		{
			string ext = Path.GetExtension( path ).ToLower();

			if( ext != ".wav" && ext != ".ogg" )
			{
				throw new GATException( "Unrecognized extension: " + ext );
			}

			if( !File.Exists( path ) )
			{
				throw new GATException( "No such file!" );
			}

			if( ext == ".wav" )
				return new WavFile( path );

			#if GAT_NO_THREADING
			throw new GATException("NVorbis ogg decoder not compatible with GAT_NO_THREADING flag" );
			#else

			FileStream stream = File.OpenRead( path );

			return new OggFile( stream );
			#endif
		}

		/// <summary>
		/// Creates and returns a ready to use AGATAudioFile object.
		/// Wrap in a try catch block if you are not sure that the 
		/// file type is supported.
		/// </summary>
		public static AGATAudioFile OpenAudioFileFromStream( Stream stream, string format )
		{
			if( format != "wav" && format != "ogg" )
			{
				throw new GATException( "Unrecognized format: " + format );
			}
			
			if( format == "wav" )
				return new WavFile( stream );
			
			#if GAT_NO_THREADING
			throw new GATException("NVorbis ogg decoder not compatible with GAT_NO_THREADING flag" );
			#else
			return new OggFile( stream );
			#endif
		}

		/// <summary>
		/// Reads the next chunk into the target array at offset. 
		/// Multichannel data is always interleaved.
		/// </summary>
		public abstract int ReadNextChunk( float[] target, int offset, int numFrames );

		protected AGATAudioFile( string path )
		{
			filePath = path;
		}

		protected AGATAudioFile(){}

		#region IDisposable Implementation
		bool _disposed;
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

		void Dispose( bool explicitly )
		{
			if( _disposed )
				return;

			if( explicitly )
			{
				FreeResources();
			}
			
			_disposed = true;
		}
		
		~AGATAudioFile()
		{
			Dispose( false );
		}

		protected abstract void FreeResources();
		
		#endregion

		#if !GAT_NO_THREADING
		class OggFile : AGATAudioFile //Simply wrapping NVorbis reader
		{
			VorbisReader _reader;

			public OggFile( Stream stream ) : base()
			{
				_reader = new VorbisReader( stream, true );
			}

			protected override void FreeResources()
			{
				_reader.Dispose();
			}

			public override int   Channels{ get{ return _reader.Channels;     } }
			public override int SampleRate{ get{ return _reader.SampleRate;   } }
			public override int  NumFrames{ get{ return ( int )_reader.TotalSamples; } }

			public override int ReadPosition
			{
				get
				{
					return ( int )_reader.DecodedPosition;
				}

				set
				{
					_reader.DecodedPosition = ( long )value;
				}
			}

			public override int ReadNextChunk( float[] target, int offset, int numFrames )
			{
				if( _reader.DecodedPosition  + numFrames > _reader.TotalSamples ) //Double check eof, some ogg files seem to have padding
				{
					numFrames = ( int )( _reader.TotalSamples - _reader.DecodedPosition );
				}

				int readFrames = _reader.ReadSamples( target, offset, numFrames * Channels ) / Channels; //readsamples returns samples and not frames...

				return readFrames;
			}
		}
		#endif

		class WavFile : AGATAudioFile
		{
			const int BUFFER_LENGTH 		= 16384;
			const int BYTES_BUFFER_LENGTH 	= 32768;
			const int CONVERSION_FACTOR     = 32767;

			static Int16[] __intBuf;
			static byte[]  __bytesBuf;

			int _eofPosition;

			public WavFile( string path ) : base( path )
			{
				if( __intBuf == null )
				{
					__intBuf 	= new Int16[ BUFFER_LENGTH ];
					__bytesBuf 	= new byte[ BYTES_BUFFER_LENGTH ];
				}

				_stream = File.OpenRead( filePath );
				_headerSize = GATWavHelper.headerSize;
				ParseHeader();
			}

			public WavFile( Stream stream ) : base()
			{
				if( __intBuf == null )
				{
					__intBuf 	= new Int16[ BUFFER_LENGTH ];
					__bytesBuf 	= new byte[ BYTES_BUFFER_LENGTH ];
				}
				
				_stream = stream;
				_headerSize = GATWavHelper.headerSize;
				ParseHeader();
			}

			protected override void FreeResources()
			{
				_stream.Close();
				_stream.Dispose();
			}

			public override int   Channels{ get{ return _channels;   } }
			public override int SampleRate{ get{ return _sampleRate; } }
			public override int  NumFrames{ get{ return _numFrames;  } }
			public override int ReadPosition
			{
				get
				{
					return ( ( int )_stream.Position - _headerSize ) / ( _blockAlign );
				}
				
				set
				{
					value = ( value * _blockAlign ) + _headerSize;
					_stream.Seek( value, SeekOrigin.Begin );
				}
			}

			private Stream _stream;
			private int _channels, _sampleRate, _numFrames, _readPos, _blockAlign, _headerSize;

			void ParseHeader()
			{
				byte[] bytes;	

				BinaryReader br = new BinaryReader( _stream );

				bytes = br.ReadBytes( 4 ); //ChunkID
				
				if( bytes.IsEqualTo( GATWavHelper.riffBytes ) == false )
				{
					throw new GATException("File is not 'RIFF'" );
				}
				
				br.ReadInt32(); //fileSize
				
				bytes = br.ReadBytes( 4 ); //format
				
				if( bytes.IsEqualTo( GATWavHelper.waveBytes ) == false )
				{
					throw new GATException("File is not 'WAVE'" );
				}
				
				bytes = br.ReadBytes( 4 );
				
				if( bytes.IsEqualTo( GATWavHelper.fmtBytes ) == false )
				{
					throw new GATException( "Header error (subchunk1_ID is not 'fmt ' )" );
				}
				
				int fmtSize = br.ReadInt32();
				
				if ( fmtSize != 16 && fmtSize != 18 )
				{
					throw new GATException( "Header error: fmt size is not 16 or 18." );
				}
				
				Int16 format = br.ReadInt16();
				
				if( format != 1 )
				{
					throw new GATException( "Compressed wav files not supported." );
				}
				
				_channels = ( int )br.ReadInt16();
				
				if( _channels > GATInfo.MaxIOChannels )
				{
					throw new GATException( "File has more channels than than set in GATManager.MaxIOChannels" );
				}
				
				_sampleRate = br.ReadInt32();
				
				br.ReadInt32(); //byte rate
				
				_blockAlign = br.ReadInt16(); //block align
				
				Int16 bitDepth = br.ReadInt16();

				if( bitDepth != 16 )
				{
					throw new GATException( "Only 16 bit wav files are supported." );
				}

				if( fmtSize == 18 )
				{
					br.ReadInt16();
					_headerSize += 2;
				}

				bytes = br.ReadBytes( 4 );

				
				while( bytes.IsEqualTo( GATWavHelper.dataBytes ) == false )
				{
					int extraChunkSize = br.ReadInt32();
					_headerSize += ( 8 + extraChunkSize ); //identifier + size int + actual size

					_stream.Seek( extraChunkSize, SeekOrigin.Current );

					bytes = br.ReadBytes( 4 );
				}

				int numDataBytes = br.ReadInt32();
				_eofPosition = _headerSize + numDataBytes;
				_numFrames = numDataBytes / _blockAlign;
			}

			/// <summary>
			/// Returns number of frames read
			/// </summary>
			public override int ReadNextChunk( float[] target, int offset, int numFrames )
			{
				int numBytes = numFrames * _blockAlign;

				int totalReadBytes = 0;
				int readBytes = 0;
				int readSamples;
				int i;

				if( _stream.Position + numBytes > _eofPosition )
					numBytes = _eofPosition - ( int )_stream.Position;

				int requestedReadLength = numBytes < BYTES_BUFFER_LENGTH ? numBytes : BYTES_BUFFER_LENGTH;

				while( totalReadBytes < numBytes )
				{
					readBytes = _stream.Read( __bytesBuf, 0, requestedReadLength );
					totalReadBytes += readBytes;

					if( totalReadBytes > numBytes ) // there might be more data after the data chunk, stop here.
					{
						readBytes -= totalReadBytes - numBytes; 
						totalReadBytes = numBytes;
					}

					Buffer.BlockCopy( __bytesBuf, 0, __intBuf, 0, readBytes );

					readSamples = readBytes / 2;

					for( i = 0; i < readSamples; i++ )
					{
						target[ offset ] = ( float )__intBuf[ i ] / CONVERSION_FACTOR;
						offset ++;
					}

					if( readBytes < requestedReadLength ) //eof or end of requested samples
					{
						break;
					}
				}

				return totalReadBytes / _blockAlign;
			}
		}
	}


}


