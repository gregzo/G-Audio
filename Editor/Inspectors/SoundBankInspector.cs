using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using GAudio;

[ CustomEditor( typeof( GATSoundBank ) ) ]
public class SoundBankInspector : Editor
{
	GATSoundBank 		_soundBank;
	List< AudioClip > 	_draggedClips;
	
	void OnEnable()
	{
		_soundBank = target as GATSoundBank;
	}

	void OnDisable()
	{
		if( _soundBank != null )
			EditorUtility.SetDirty( _soundBank );
	}

	static string 			 __helpBoxMessage = "Drag and drop AudioClips here. Clips must be placed in the resources folder before being added to a SoundBank.";
	static GUILayoutOption[] __smallerButtons = new GUILayoutOption[]{ GUILayout.Width( 55f ), GUILayout.ExpandWidth( false ) };

	public override void OnInspectorGUI()
	{
		int i;
		GATSampleInfo info;
		Rect boxRect;

		EditorGUIUtility.labelWidth = 80f;
		EditorGUIUtility.fieldWidth = 70f;

		EditorGUILayout.HelpBox( __helpBoxMessage, MessageType.Info );

		GUILayout.BeginHorizontal();
		GUILayout.Label( "Sample Rate: " + _soundBank.SampleRate );
		GUILayout.Label( "Total uncompressed size: " + _soundBank.HumanReadableUncompressedSize ); 
		if( GATManager.DefaultDataAllocator != null )
		{
			float percent = ( ( float )( _soundBank.TotalUncompressedBytes / 4 ) / GATManager.DefaultDataAllocator.TotalNonFixedSize ) * 100;

			GUILayout.Label( percent.ToString( "0.##\\%" ) );
		}
		GUILayout.EndHorizontal();

		GUI.color = Color.white;

		if( _soundBank.SampleInfos != null && _soundBank.SampleInfos.Count != 0 )
		{
			GUILayout.BeginHorizontal();
			if( GUILayout.Button( "Check Bank" ) )
			{
				CheckBankIntegrity( _soundBank.SampleInfos );
			}

			if( GUILayout.Button( "Compute Midi Codes(beta)" ) )
			{
				_soundBank.GetMidiCodes( 44100, 8192, 2500 );
				EditorUtility.SetDirty( _soundBank );
			}

			if( GUILayout.Button( "Sort" ) )
			{
				_soundBank.SortByMidiCode();
				EditorUtility.SetDirty( _soundBank );
			}

			GUILayout.EndHorizontal();
		}

		for( i = 0; i < _soundBank.SampleInfos.Count; i++ )
		{
			info    = _soundBank.SampleInfos[ i ];

			GUI.backgroundColor = Color.white;
			boxRect 			= EditorGUILayout.BeginVertical();

			if( info.clipStatus != SoundBankClipStatus.Ok )
			{
				GUI.backgroundColor = Color.red;
			}

			GUI.Box( boxRect, string.Format( "- {0} -\nUncompressed Size in Memory: {1}", info.FileName, info.HumanReadableSizeInMemory ) );

			GUILayout.Space( 40f );

			if( info.clipStatus == SoundBankClipStatus.Ok )
			{
				if( DrawOkSample( info ) == false ) //remove clicked
					break;
			}
			else if( info.clipStatus == SoundBankClipStatus.NotFound )
			{
				if( DrawNotFoundSample( info ) == false ) //remove clicked
					break;
			}
			else if( info.clipStatus == SoundBankClipStatus.NotInResources )
			{
				if( DrawNotInResourcesSample( info ) == false ) //remove clicked
					break;
			}
			else if( info.clipStatus == SoundBankClipStatus.Moved )
			{
				if( DrawMovedSample( info ) == false ) //remove clicked
					break;
			}

			GUILayout.Space( 5f );

			EditorGUILayout.EndVertical();

			GUILayout.Space( 5f );
		}

		CheckDragAndDrop ();
	}

	bool DrawOkSample( GATSampleInfo info )
	{
		bool remove = false;

		GUILayout.BeginHorizontal();
		info.Name = EditorGUILayout.TextField( "Name in Bank:", info.Name, GUILayout.ExpandWidth( false ) );
		
		if( GUILayout.Button( "Select", __smallerButtons ) )
		{
			Selection.activeObject = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( info.GUID ), typeof( AudioClip ) );
		}	
		
		GUI.color = Color.red;
		if( GUILayout.Button( "Remove", __smallerButtons ) )
		{
			_soundBank.RemoveSample( info );
			EditorUtility.SetDirty( _soundBank );
			remove = true;
		}	

		GUILayout.EndHorizontal();

		GUI.color = Color.white;

		GUILayout.BeginHorizontal();

		info.MidiCode = EditorGUILayout.IntField( "MidiCode: ", info.MidiCode, GUILayout.Width( 110f ) );
		GUILayout.Label( GATMidiHelper.MidiCodeToString( info.MidiCode ) );
		GUILayout.EndHorizontal();
		return !remove;
	}

	bool DrawNotFoundSample( GATSampleInfo info )
	{
		bool remove = false;

		GUILayout.Label( string.Format( "Sample {0} not found!", info.Name ) );

		GUILayout.BeginHorizontal();
		if( GUILayout.Button( "Remove", __smallerButtons ) )
		{
			_soundBank.RemoveSample( info );
			EditorUtility.SetDirty( _soundBank );
			remove = true;
		}	
		GUILayout.EndHorizontal();

		return !remove;
	}

	bool DrawNotInResourcesSample( GATSampleInfo info )
	{
		bool remove = false;

		GUILayout.Label( string.Format( "Sample {0} not in a Resources folder!", info.Name ) );

		GUILayout.BeginHorizontal();
		if( GUILayout.Button( "Select", __smallerButtons ) )
		{
			Selection.activeObject = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( info.GUID ), typeof( AudioClip ) );
		}	

		if( GUILayout.Button( "Remove", __smallerButtons ) )
		{
			_soundBank.RemoveSample( info );
			EditorUtility.SetDirty( _soundBank );
			remove = true;
		}	
		GUILayout.EndHorizontal();
		
		return !remove;
	}

	bool DrawMovedSample( GATSampleInfo info )
	{
		bool remove = false;

		GUILayout.Label( string.Format( "Sample {0}'s path has changed.", info.Name ) );

		GUILayout.BeginHorizontal();
		if( GUILayout.Button( "Select", __smallerButtons ) )
		{
			Selection.activeObject = AssetDatabase.LoadAssetAtPath( AssetDatabase.GUIDToAssetPath( info.GUID ), typeof( AudioClip ) );
		}	

		GUI.backgroundColor = Color.green;
		if( GUILayout.Button( "Update", __smallerButtons ) )
		{
			info.UpdatePathInResources();
			EditorUtility.SetDirty( _soundBank );
		}

		GUI.backgroundColor = Color.red;
		if( GUILayout.Button( "Remove", __smallerButtons ) )
		{
			_soundBank.RemoveSample( info );
			EditorUtility.SetDirty( _soundBank );
			remove = true;
		}	

		GUILayout.EndHorizontal();
		
		return !remove;
	}

	void CheckDragAndDrop()
	{
		EventType eventType = Event.current.type;
		
		if( eventType == EventType.DragUpdated || eventType == EventType.dragPerform )
		{
			if( _draggedClips == null )
			{
				_draggedClips = GetClips( DragAndDrop.objectReferences );
			}
			
			if( _draggedClips.Count != 0 )
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}
			
			if( eventType == EventType.dragPerform )
			{
				DragAndDrop.AcceptDrag();
				TryImportDraggedClips();
			}
			
		}
		else if( eventType == EventType.DragExited )
		{
			_draggedClips = null;
		}
	}

	List< AudioClip > GetClips( Object[] draggedObjects )
	{
		List< AudioClip > clips = new List< AudioClip >();
		AudioClip 		  clip;
		int 		      i;

		if( draggedObjects == null )
			return null;

		for( i = 0; i < draggedObjects.Length; i++ )
		{
			clip = draggedObjects[ i ] as AudioClip;

			if( clip != null )
			{
				clips.Add( clip );
			}
		}

		return clips;
	}

	void TryImportDraggedClips()
	{
		int 		i;
		ImportInfo 	info;

		for( i = 0; i < _draggedClips.Count; i++ )
		{
			info = new ImportInfo( _draggedClips[ i ] );
			
			if( _soundBank.Contains( info.GUID ) )
			{
				EditorGUIUtility.PingObject( info.Clip );
				if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "AudioClip {0} is already in Sound Bank!", info.Clip.name ), "Skip", "Cancel" ) )
				{
					continue;
				}
				else return;
			}
			else
			{
				if( info.PathInResources == null )
				{
					EditorGUIUtility.PingObject( info.Clip );
					if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "Cannot import AudioClip {0}, it is not in a Resources folder.", info.Clip.name ), "Skip", "Cancel" ) )
					{
						continue;
					}
					else return;
				}

#if UNITY_5
				AudioImporterSampleSettings sampleSettings = info.Importer.defaultSampleSettings;
				if( sampleSettings.loadType != AudioClipLoadType.DecompressOnLoad )
				{
					EditorGUIUtility.PingObject( info.Clip );
					if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "AudioClip {0}'s load type import settings should be set to * load into memory *.", info.Clip.name ), "Fix", "Cancel" ) )
					{
						sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
						info.Importer.defaultSampleSettings = sampleSettings;
					}
					else return;

				}
#else
				if( info.Importer.format == AudioImporterFormat.Native )
				{
					if( info.Importer.loadType == AudioImporterLoadType.StreamFromDisc )
					{
						EditorGUIUtility.PingObject( info.Clip );
						if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "AudioClip {0}'s load type import settings should be set to * load into memory *.", info.Clip.name ), "Fix", "Cancel" ) )
						{
							info.Importer.loadType = AudioImporterLoadType.DecompressOnLoad;
						}
						else return;
					}
				}
				else if( info.Importer.loadType != AudioImporterLoadType.DecompressOnLoad )
				{
					EditorGUIUtility.PingObject( info.Clip );
					if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "AudioClip {0}'s load type import settings should be set to * decompress on load *.", info.Clip.name ), "Fix", "Cancel" ) )
					{
						info.Importer.loadType = AudioImporterLoadType.DecompressOnLoad;
					}
					else return;
				}
#endif

				if( info.Clip.frequency != _soundBank.SampleRate )
				{
					EditorGUIUtility.PingObject( info.Clip );
					if( EditorUtility.DisplayDialog( "Cannot Import", string.Format( "AudioClip {0}'s sample rate does not match the Sound Bank's.", info.Clip.name ), "Skip", "Cancel" ) )
					{
						continue;
					}
					else return;
				}
			}

			_soundBank.AddSample( info.PathInResources, info.GUID, info.Clip.channels, info.Clip.samples, info.IsStreamingAsset );
			//AssetDatabase.ImportAsset( info.Path );
		}

		EditorUtility.SetDirty( _soundBank );
		_draggedClips.Clear();
	}

	public void CheckBankIntegrity( List< GATSampleInfo > sampleInfos )
	{
		if( Application.isPlaying )
			throw new GATException( "Check integrity cannot be called in play mode!" );
		
		string 		path;
		string 		pathInResources;
		AudioClip 	clip;

		bool bankHasErrors = false;

		List< GATSampleInfo > samplesToRemove = new List< GATSampleInfo >();

		foreach( GATSampleInfo info in sampleInfos )
		{
			path = AssetDatabase.GUIDToAssetPath( info.GUID );

			if( path == "" )
			{
				info.clipStatus = SoundBankClipStatus.NotFound;
				if( ClipNotFoundDialog( info.Name ) )
				{
					samplesToRemove.Add( info );
				}
				else bankHasErrors = true;

				continue;
			}

			clip = ( AudioClip )AssetDatabase.LoadAssetAtPath( path, typeof( AudioClip ) );

			if( clip == null )
			{
				info.clipStatus = SoundBankClipStatus.NotFound;
				if( ClipNotFoundDialog( info.Name ) )
				{
					samplesToRemove.Add( info );
				}
				else bankHasErrors = true;

				continue;
			}

			pathInResources = GATEditorUtilities.PathInResources( path );
			if( pathInResources == null )
				pathInResources = GATEditorUtilities.PathInStreamingAssets( path );

			if( pathInResources == null )
			{
				info.clipStatus = SoundBankClipStatus.NotInResources;

				if( EditorUtility.DisplayDialog( "AudioClip not in Resources", string.Format( "clip {0} was found, but is not in a Resources or StreamingAssets folder. Remove it from the SoundBank?", info.Name ), "Remove", "Skip" ) )
				{
					samplesToRemove.Add( info );
				}
				else bankHasErrors = true;

				continue;
			}
			else if( pathInResources != info.PathInResources )
			{
				info.clipStatus = SoundBankClipStatus.Moved;
				Debug.Log( string.Format( "moved form {0} to {1}", info.PathInResources, pathInResources ) );

				if( EditorUtility.DisplayDialog( "AudioClip moved", string.Format( "clip {0} was found but has moved since it was added to the SoundBank. Update path info?", info.Name ), "Update", "Skip" ) )
				{
					info.UpdatePathInResources();
				}
				else bankHasErrors = true;

				continue;
			}
		}

		foreach( GATSampleInfo info in samplesToRemove )
		{
			_soundBank.RemoveSample( info );
		}

		EditorUtility.SetDirty( _soundBank );

		string endDialog = bankHasErrors ? "This Sound Bank contains errors that need to be fixed before it is loaded."
										 : "No errors!";

		EditorUtility.DisplayDialog( "Check Completed", endDialog, "OK" );
	}

	bool ClipNotFoundDialog( string clipName )
	{
		return EditorUtility.DisplayDialog( "AudioClip not found", string.Format( "clip {0} cannot be located in your project. Remove it from the SoundBank?", clipName ), "Remove", "Skip" );
	}

	class ImportInfo
	{
		public readonly AudioClip 		Clip;
		public readonly AudioImporter 	Importer;
		public readonly string 			Path;
		public readonly string 			PathInResources;
		public readonly string			GUID;
		public readonly bool			IsStreamingAsset;
		
		public ImportInfo( AudioClip clip )
		{
			Clip			= clip;
			Path 			= AssetDatabase.GetAssetPath( clip.GetInstanceID() );
			GUID			= AssetDatabase.AssetPathToGUID( Path );
			Importer		= AssetImporter.GetAtPath( Path ) as AudioImporter;

			if( GATEditorUtilities.IsInResources( Path ) )
			{
				PathInResources = GATEditorUtilities.PathInResources( Path ); 

			}
			else if( GATEditorUtilities.IsInStreamingAssets( Path ) )
			{
				PathInResources = GATEditorUtilities.PathInStreamingAssets( Path );
				System.IO.Path.ChangeExtension( PathInResources, System.IO.Path.GetExtension( Importer.assetPath ) );
				IsStreamingAsset = true;
			}
		}
	}
}
