using UnityEngine;
using UnityEditor;
using System.Collections;

[InitializeOnLoad]
public class GATScriptExecutionOrder : Editor 
{
	const string 	GAT_MANAGER_PATH	= "Assets/G-Audio/Scripts/StaticAndSingletons/GATManager.cs",
					GAT_PLAYER_PATH  	= "Assets/G-Audio/Scripts/Mixing/GATPlayer.cs",
					GAT_BUFFER_PATH		= "Assets/G-Audio/Scripts/IO/_Classes/GATAudioBuffer.cs";
	
	const int	 GAT_MANAGER_T		= -20,
				 GAT_BUFFER_T		= -15,
				 GAT_PLAYER_T		= -10;
	
	static GATScriptExecutionOrder()
	{
		if( SetScriptExecutionOrder( GAT_MANAGER_PATH, GAT_MANAGER_T ) )
		{
			#if GAT_DEBUG
			Debug.Log( "GATManager.cs script execution order set to " + GAT_MANAGER_T );
			#endif
		}

		if( SetScriptExecutionOrder( GAT_BUFFER_PATH, GAT_BUFFER_T ) )
		{
			#if GAT_DEBUG
			Debug.Log( "GATAudioBuffer.cs script execution order set to "  + GAT_BUFFER_T  );
			#endif
		}

		if( SetScriptExecutionOrder( GAT_PLAYER_PATH,  GAT_PLAYER_T  ) )
		{
			#if GAT_DEBUG
			Debug.Log( "GATPlayer.cs script execution order set to "  + GAT_PLAYER_T  );
			#endif
		}
	}
	
	static bool SetScriptExecutionOrder( string scriptPath, int tDelta )
	{
		AssetImporter importer = MonoImporter.GetAtPath( scriptPath );
		
		if( importer == null )
		{
			Debug.LogError( "GATScriptExecutionOrder could not find script at path " + scriptPath );
			return false;
		}
		
		MonoImporter monoI = importer as MonoImporter;
		
		MonoScript script = monoI.GetScript();
		
		if( MonoImporter.GetExecutionOrder( script ) != tDelta )
		{
			MonoImporter.SetExecutionOrder( script, tDelta );
			return true;
		}
		
		return false;
	}
}

