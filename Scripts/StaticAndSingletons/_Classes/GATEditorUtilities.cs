#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;

namespace GAudio
{
	public static class GATEditorUtilities
	{
		const string RESOURCES_STRING 		= "/Resources/";
		const int    RESOURCES_CHAR_LENGTH 	= 11;
		const string STREAMING_ASSETS		= "Assets/StreamingAssets/";
		
		public static T NewChildGO<T>( string name, GameObject parentObj = null ) where T : Component
		{
			if( parentObj == null )
			{
				parentObj = Selection.activeGameObject;
			}
			
			GameObject go = new GameObject( name );
			
			if( parentObj != null )
			{
				go.transform.parent = parentObj.transform;
				go.transform.localPosition = Vector3.zero;
				go.transform.localScale    = Vector3.one;
			}
			
			T comp = go.AddComponent<T>();
			
			return comp;
		}
		
		public static void CheckManager()
		{
			if( GATManager.UniqueInstance == null )
			{
				EditorApplication.ExecuteMenuItem( "GameObject/Create Other/G-Audio/Manager" );
			}
		}
		
		public static T CreateAsset<T> ( string assetName = null ) where T : ScriptableObject
		{
			T asset = ScriptableObject.CreateInstance< T >();
			
			string path = AssetDatabase.GetAssetPath( Selection.activeObject );
			
			if( path == "" ) 
			{
				path = "Assets";
			} 
			else if( Path.GetExtension( path ) != "" ) 
			{
				path = path.Replace( Path.GetFileName( AssetDatabase.GetAssetPath( Selection.activeObject ) ), "" );
			}
			
			string name = ( assetName == null ) ? ( string.Format( "/New {0}.asset", typeof(T).ToString() ) ) 
				: ( string.Format( "/{0}.asset", assetName ) );
			
			string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath( path + name );
			
			AssetDatabase.CreateAsset( asset, assetPathAndName );
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
			
			return asset;
		}
		
		public static bool IsInResources( string assetPath )
		{
			while( assetPath != "Assets" )
			{
				assetPath = Path.GetDirectoryName( assetPath );
				
				if( Path.GetFileName( assetPath ) == "Resources" )
				{
					return true;
				}
			}
			
			return false;
		}
		
		public static bool IsInStreamingAssets( string assetPath )
		{
			while( assetPath != "Assets" )
			{
				assetPath = Path.GetDirectoryName( assetPath );
				
				if( Path.GetFileName( assetPath ) == "StreamingAssets" )
				{
					return true;
				}
			}
			
			return false;
		}
		
		public static string PathInResources( string assetPath )
		{
			int index;
			string pathInResources;
			
			index = assetPath.IndexOf( RESOURCES_STRING );
			
			if( index == -1 )
				return null;
			
			pathInResources = assetPath.Substring( index + RESOURCES_CHAR_LENGTH );
			pathInResources = pathInResources.Substring( 0, pathInResources.Length - Path.GetExtension( pathInResources ).Length );
			
			return pathInResources;
			
		}
		
		public static string PathInStreamingAssets( string assetPath )
		{	
			if (!assetPath.StartsWith( STREAMING_ASSETS  ) )
			{
				Debug.LogError("Not in streaming assets! ");
				return null;
			}
			else
			{
				int index = assetPath.IndexOf( STREAMING_ASSETS );
				string res;
				res = assetPath.Substring( index + STREAMING_ASSETS.Length );
				return res;
			}
		}
	}
}

#endif

