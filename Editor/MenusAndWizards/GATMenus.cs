using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class GATMenus 
{
	#region Hierarchy Create Menu
	[ MenuItem( "GameObject/Create Other/G-Audio/Manager", false, 1100 ) ]
	static void CreateManager()
	{
		if( GATManager.UniqueInstance == null )
		{
			GameObject go = new GameObject( "G-Audio Manager" );
			go.AddComponent < GATManager >();
		}
		else
		{
			Debug.LogError( "Only one GATManager may exist per scene. Manager found on go: " + GATManager.UniqueInstance.gameObject.name );
		}
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Sample Bank", false, 1101 ) ]
	static void CreateSampleBankWizard() 
	{
		EditorWindow.GetWindowWithRect<SampleBankWizard>( new Rect( 300f, 300f, 250f, 200f ), true, "Sample Bank Wizard" );
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Envelope", false, 1102 ) ]
	static void CreateEnvelope() 
	{
		GATEditorUtilities.CheckManager();
		
		GATEditorUtilities.NewChildGO<EnvelopeModule>( "Envelope" );
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Master Pulse", false, 1150 ) ]
	static void CreateMasterPulse() 
	{
		GATEditorUtilities.CheckManager();

		GATEditorUtilities.NewChildGO<MasterPulseModule>( "Master Pulse" );
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Sub Pulse", false, 1151) ]
	static void CreateSubPulse() 
	{
		GATEditorUtilities.CheckManager();
		
		GATEditorUtilities.NewChildGO<SubPulseModule>( "Sub Pulse" );
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Pulsed Pattern", false, 1152 ) ]
	static void CreatePulsedSamples() 
	{
		GATEditorUtilities.CheckManager();
		
		GATEditorUtilities.NewChildGO<PulsedPatternModule>( "Pulsed Pattern" );
	}

	[ MenuItem( "GameObject/Create Other/G-Audio/Music System", false, 1200 ) ]
	static void CreateMusicSystem() 
	{
		GATEditorUtilities.CheckManager();
		
		EditorWindow.GetWindowWithRect<MusicSystemWizard>( new Rect( 300f, 300f, 300f, 120f ), true, "Music System Wizard" );
	}

	#endregion

	#region Project Create Menu
	
	[ MenuItem( "Assets/Create/G-Audio/Sound Bank" ) ]
	public static void CreateSoundBankAsset ()
	{
		EditorWindow.GetWindowWithRect<SoundBankWizard>( new Rect( 400f, 400f, 200f, 100f ), true, "Sound Bank Wizard" );
	}
	#endregion

	#region G-Audio Windows Menu

	[ MenuItem ( "Window/G-Audio Memory" ) ]
	static void CreateMemoryWindow()
	{
		EditorWindow.GetWindow( typeof( GATMemoryWindow ), false );
	}

	#endregion
	
}
