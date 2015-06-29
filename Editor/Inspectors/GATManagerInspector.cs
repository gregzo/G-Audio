using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

[ CustomEditor( typeof( GATManager ) ) ]
public class GATManagerInspector : GATBaseInspector 
{
	GATManager _manager;
	
	void OnEnable()
	{
		_manager = target as GATManager;
	}

	public override void OnInspectorGUI()
	{
		GUILayout.Space( 10f );

		GUI.color = __blueColor;

		if( GUILayout.Button( "Memory Config Wizard", GUILayout.Width( 150f ) ) )
		{
			DataAllocatorConfigWindow window = EditorWindow.GetWindow< DataAllocatorConfigWindow >( true );

			window.SetAllocatorInfo( GATManager.UniqueInstance.AllocatorInitSettings );
		}

		GUILayout.Space( 5f );

		GUI.enabled = Application.isPlaying;

		if( GUILayout.Button( "Memory Status Window", GUILayout.Width( 150f )  ) )
		{
			EditorWindow.GetWindow( typeof( GATMemoryWindow ), false );
		}

		GUI.enabled = true;

		GUILayout.Space( 5f );

		GUI.color = Color.white;
		EditorGUIUtility.labelWidth = 150f;

#if !UNITY_5
		_manager.SupportedSampleRates = ( GATManager.SampleRatesSupport )EditorGUILayout.EnumPopup( "Supported Sample Rates", _manager.SupportedSampleRates, GUILayout.Width( 260f ) );
		_manager.SpeakerModeInit 	  = ( GATManager.SpeakerModeBehaviour )EditorGUILayout.EnumPopup( "Speaker Mode Initialization", _manager.SpeakerModeInit, GUILayout.Width( 260f ) );
#endif
		//_manager.MaxIOChannels 	  = EditorGUILayout.IntSlider( "Max I/O Channels", _manager.MaxIOChannels, 2, 8, GUILayout.ExpandWidth( false ) ); //G-Audio 1.2
		_manager.PulseLatency  	      = ( double )EditorGUILayout.FloatField( "Pulse Latency", ( float )_manager.PulseLatency, GUILayout.ExpandWidth( false ) );
	}

	void OnDisable()
	{
		if( _manager != null )
			EditorUtility.SetDirty( _manager );
	}
}
