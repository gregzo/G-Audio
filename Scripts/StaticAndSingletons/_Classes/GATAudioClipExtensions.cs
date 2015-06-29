//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Helper methods to extract audio data from AudioClips
	/// </summary>
	public static class GATAudioClipExtensions  
	{
		/// <summary>
		/// Extracts the audio data from an AudioClip and sets it in a GATData object.
		/// Memory is allocated according to the specified GATDataAllocationMode.
		/// </summary>
		public static GATData ToGATData( this AudioClip clip, GATDataAllocationMode mode )
		{
			GATData data;
			float[] tempArray;
			
			tempArray = new float[ clip.samples ];
			
			clip.GetData( tempArray, 0 );
			
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
			{
				mode = GATDataAllocationMode.Unmanaged;
			}
			#endif
			
			if( mode == GATDataAllocationMode.Managed )
			{
				data = GATManager.GetDataContainer( clip.samples );
				data.CopyFrom( tempArray, 0, 0, clip.samples );
			}
			else if( mode == GATDataAllocationMode.Fixed )
			{
				data = GATManager.GetFixedDataContainer( clip.samples, "ClipData: "+clip.name );
				data.CopyFrom( tempArray, 0, 0, clip.samples );
			}
			else 
			{
				data = new GATData( tempArray );
			}
			
			return data;
		}
		
		/// <summary>
		/// Performs the same operation as ToGATData, but splits interleaved channels in seperate GATData 
		/// instances.
		/// </summary>
		public static GATData[] ExtractChannels( this AudioClip clip, GATDataAllocationMode mode )
		{
			GATData[] channelsData;
			float[] tempArray;
			int i;
			int length;
			
			tempArray = new float[ clip.samples * clip.channels ];
			
			clip.GetData( tempArray, 0 );
			
			channelsData = new GATData[ clip.channels ];
			
			length = clip.samples;
			
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
			{
				mode = GATDataAllocationMode.Unmanaged;
			}
			#endif
			
			for( i = 0; i < clip.channels; i++ )
			{
				
				if( mode == GATDataAllocationMode.Managed )
				{
					channelsData[ i ] = GATManager.GetDataContainer( length );
				}
				else if( mode == GATDataAllocationMode.Fixed )
				{
					channelsData[ i ] = GATManager.GetFixedDataContainer( length, clip.name+" channel"+i+" data" );
				}
				else
				{
					channelsData[ i ] = new GATData( new float[ length ] );
				}
				
				channelsData[ i ].CopyFromInterlaced( tempArray, length, i, clip.channels );
			}
			
			return channelsData;
		}
	}
}

