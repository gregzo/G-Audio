using UnityEngine;
using System.Collections;
using UnityEditor;

[ InitializeOnLoad ]
public class GATStartup 
{
	static string[] __oldGizmosPaths	   = new string[]{ "Assets/Gizmos/GATSoundBank icon.png"  };

	static string[] __newGizmosSourcePaths = new string[]{ "Assets/G-Audio/GATSoundBank icon.png" };
	static string[] __newGizmosTargetPaths = new string[]{ "Assets/Gizmos/GATSoundBank icon.png"  };

	static GATStartup()
	{
		Object o;
		//First, check that we haven't moved the gizmos already:
		o = AssetDatabase.LoadAssetAtPath( __newGizmosSourcePaths[ 0 ], typeof( Object ) );

		if( o == null )
		{
			return;
		}

		o = null;
		o = AssetDatabase.LoadAssetAtPath( "Assets/Gizmos", typeof( Object ) );
		if( o == null ) //Make sure we have a Gizmos folder
		{
			AssetDatabase.CreateFolder( "Assets", "Gizmos" );
		}
		else //If we already do, check for and delete old G-Audio gizmos
		{
			CheckAndDeleteOldGizmos();
		}

		MoveNewGizmos(); //Move our new gizmos to the Gizmos folder
	}

	static void CheckAndDeleteOldGizmos()
	{
		Object o;

		foreach( string path in __oldGizmosPaths )
		{
			o = AssetDatabase.LoadAssetAtPath( path, typeof( Object ) );
			if( o != null )
			{
				AssetDatabase.DeleteAsset( path );
			}
		}
	}

	static void MoveNewGizmos()
	{
		for( int i = 0; i < __newGizmosSourcePaths.Length; i++ )
		{
			AssetDatabase.MoveAsset( __newGizmosSourcePaths[ i ], __newGizmosTargetPaths[ i ] );
		}
	}
}
