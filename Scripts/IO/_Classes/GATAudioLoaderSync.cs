using UnityEngine;
using System.Collections;

namespace GAudio
{
	public partial class GATAudioLoader
	{
		/// <summary>
		/// Blocking load of wav and ogg files.
		/// </summary>
		public GATData[] LoadSync( AGATAudioFile file, GATDataAllocationMode allocationMode )
		{
			GATData[] loadedChannels;
			int i;
			bool didFail = false;
			
			loadedChannels = new GATData[ file.Channels ];
			
			for( i = 0; i < file.Channels; i++ )
			{
				if( allocationMode == GATDataAllocationMode.Fixed ) //GZComment: allocation fail leaks
				{
					try{ loadedChannels[ i ] = GATManager.GetFixedDataContainer( file.NumFrames, file.FileName ); }
					catch( System.Exception ex )
					{ 
						didFail = true;
						#if UNITY_EDITOR
						Debug.LogException( ex );
						#endif
					}
				}
				else if( allocationMode == GATDataAllocationMode.Managed ) //GZComment: allocation fail leaks
				{
					try
					{ 
						loadedChannels[ i ] = GATManager.GetDataContainer( file.NumFrames ); 
					}
					catch( System.Exception ex )
					{ 
						didFail = true;
						#if UNITY_EDITOR
						Debug.LogException( ex );
						#endif
					}
				}
				else 
				{
					loadedChannels[ i ] = new GATData( new float[ file.NumFrames ] );
				}
			}
			
			if( didFail )
			{
				for( i = 0; i < loadedChannels.Length; i++ )
				{
					if( loadedChannels[ i ] != null )
						loadedChannels[ i ].Release();
				}
				
				return null;
			}

			if( file.Channels == 1 )
			{
				file.ReadNextChunk( loadedChannels[ 0 ].ParentArray, loadedChannels[ 0 ].MemOffset, file.NumFrames );
				return loadedChannels;
			}

			int framesPerRead;
			int framesRead;
			int totalFramesRead = 0; 

			framesPerRead = _buffer.Length / file.Channels;

			while( true )
			{
				framesRead = file.ReadNextChunk( _buffer, 0, framesPerRead );

				for( i = 0; i < file.Channels; i++ )
				{
					loadedChannels[ i ].CopyFromInterlaced( _buffer, framesRead, totalFramesRead, i, file.Channels ); 
				}

				totalFramesRead += framesRead;
				
				if( framesRead < framesPerRead )
					break;
			}
			
			return loadedChannels;
		}
	}
}

