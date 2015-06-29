//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System;

namespace GAudio
{
	/// <summary>
	/// Convenience helper methods 
	/// and constants for reading and writing wav 
	/// files.
	/// </summary>
	public static class GATWavHelper 
	{
		public static readonly byte[] riffBytes = new byte[]{ 0x52, 0x49, 0x46, 0x46 };
		public static readonly byte[] waveBytes = new byte[]{ 0x57, 0x41, 0x56, 0x45 };
		public static readonly byte[] fmtBytes  = new byte[]{ 0x66, 0x6d, 0x74, 0x20 };
		public static readonly byte[] dataBytes = new byte[]{ 0x64, 0x61, 0x74, 0x61 };
		
		/// <summary>
		/// The size of the header, in bytes.
		/// </summary>
		public static readonly int headerSize = 44;
		
		/// <summary>
		/// Multiplier for converting float32 to int16
		/// </summary>
		public static readonly int floatToInt16RescaleFactor = 32767;
		
		static readonly byte[] __canonicalHeader = new byte[]
		{
			//Offsets
			
			/* 00 */		0x52, 0x49, 0x46, 0x46, // 'RIFF'
			/* 04 */		0x00, 0x00, 0x00, 0x00, // Chunk Size: 36 + data bytes, or file bytes - 8
			/* 08 */		0x57, 0x41, 0x56, 0x45, // 'WAVE'
			
			// ************** Format SubChunk **********************
			// -----------------------------------------------------
			/* 12 */		0x66, 0x6d, 0x74, 0x20, // 'fmt ' 
			/* 16 */		0x00, 0x00, 0x00, 0x10, // SubChunk1 size: 16 for PCM data 
			/* 20 */					0x00, 0x00, // Audio format, 1 is PCM
			/* 22 */					0x00, 0x00, // NumChannels
			/* 24 */		0x00, 0x00, 0x00, 0x00, // Sample rate
			/* 28 */		0x00, 0x00, 0x00, 0x00, // Byte rate: SampleRate * NumChannels * BitsPerSample / 8
			/* 32 */					0x00, 0x00, // BlockAlign : NumChannels * BitsPerSample / 8
			/* 34 */					0x00, 0x00, // BitsPerSample: default at 16
			
			// ************** Data SubChunk **********************
			// -----------------------------------------------------
			/* 36 */		0x64, 0x61, 0x74, 0x61, // 'data'
			/* 40 */		0x00, 0x00, 0x00, 0x00  // SubChunk2 size: number of data bytes ( numSamples * NumChannels * bitsPerSample / 8
			/* 44 */		//The PCM interleaved data
			
		};
		
		/// <summary>
		/// Returns the header for a
		/// 16 bit uncompressed wav file.
		/// </summary>
		public static byte[] GetHeader( int numChannels, int sampleRate, int numBytes )
		{
			byte[] header;
			int val;
			int offset;
			
			header = new byte[ 44 ];
			Buffer.BlockCopy( __canonicalHeader, 0, header, 0, 44 );
			
			// Chunk size
			offset 	= 4;
			val 	= numBytes - 8; //Chunk size
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int32 )val ), 0, header, offset, 4 );
			
			// SubChunk1 size
			offset 	= 16;
			val 	= 16;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int32 )val ), 0, header, offset, 4 );
			
			// Format 1
			offset 	= 20;
			val 	= 1;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int16 )val ), 0, header, offset, 2 );
			
			// NumChannels
			offset 	= 22;
			val 	= numChannels;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int16 )val ), 0, header, offset, 2 );
			
			// SampleRate
			offset 	= 24;
			val 	= sampleRate;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int32 )val ), 0, header, offset, 4 );
			
			// ByteRate
			offset 	= 28;
			val 	= sampleRate * numChannels * 16 / 8;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int32 )val ), 0, header, offset,4 );
			
			// BlockAlign
			offset 	= 32;
			val 	= numChannels * 2;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int16 )val ), 0, header, offset, 2 );
			
			// BitDepth
			offset 	= 34;
			val 	= 16;
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int16 )val ), 0, header, offset, 2 );
			
			// Data size
			offset 	= 40;
			val 	= numBytes - headerSize; //Chunk size
			Buffer.BlockCopy( BitConverter.GetBytes( ( Int32 )val ), 0, header, offset, 4 );
			
			return header;
		}
		
		/// <summary>
		/// Compare byte arrays ( extension method )
		/// </summary>
		public static bool IsEqualTo( this byte[] bytes, byte[] comparand )
		{
			int  i;
			
			if( bytes.Length != comparand.Length )
			{
				throw new GATException( "Lengths don't match!");
			}
			
			for( i = 0; i < bytes.Length; i++ )
			{
				if( bytes[ i ] != comparand[ i ] )
				{
					return false;
				}
			}
			
			return true;
		}
	}
}

