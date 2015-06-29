//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.IO;

namespace GAudio
{
	public enum PathRelativeType{ ApplicationDataPath, ApplicationPersistentDataPath, StreamingAssets, Absolute }
	
	/// <summary>
	/// Paths and URL convenience 
	/// methods
	/// </summary>
	public static class GATPathsHelper 
	{
		public static string PathForWavFile( string relativePath, PathRelativeType type, bool createDirectory )
		{
			string extension = Path.GetExtension( relativePath );

			if( extension != ".wav" )
			{
				relativePath = Path.ChangeExtension( relativePath, ".wav" );
			}

			return GetAbsolutePath( relativePath, type, createDirectory );
		}

		public static string GetAbsolutePath( string relativePath, PathRelativeType type, bool createDirectory )
		{

			switch( type )
			{
			case PathRelativeType.ApplicationDataPath:
				relativePath = Path.Combine( Application.dataPath, relativePath );
				break;
				
			case PathRelativeType.ApplicationPersistentDataPath:
				relativePath = Path.Combine( Application.persistentDataPath, relativePath );
				break;

			case PathRelativeType.StreamingAssets:
				relativePath = Path.Combine( Application.streamingAssetsPath, relativePath );
				break;

			default:
				
				break;
			}
			
			if( createDirectory )
			{
				Directory.CreateDirectory( Path.GetDirectoryName( relativePath ) );
			}
			
			return relativePath;
		}
		
		public static string URLFromFilePath( string path )
		{
			#if UNITY_STANDALONE_WIN || UNITY_METRO
			return "file:///" + path;
			#else
			
			return "file://" + path;	
			#endif
		}
	}
}

