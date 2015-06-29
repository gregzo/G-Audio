using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

[ CustomEditor( typeof( EnvelopeModule ) ) ]
public class GATEnvelopeInspector : GATBaseInspector 
{
	EnvelopeModule _envelopeModule;

	void OnEnable()
	{
		if( GATManager.UniqueInstance != null && GATManager.UniqueInstance.popEnvelopeWindow )
			PopWindow();
	}

	void PopWindow()
	{
		_envelopeModule = target as EnvelopeModule;
		
		EnvelopeWindow window = EditorWindow.GetWindow< EnvelopeWindow >( true, "EnvelopeWindow" );
		
		window.InitWithEnvelope( _envelopeModule );
	}

	public override void OnInspectorGUI()
	{
		if( GATManager.UniqueInstance == null )
			return;

		GATManager.UniqueInstance.popEnvelopeWindow = GUILayout.Toggle( GATManager.UniqueInstance.popEnvelopeWindow, "Pop Window Auto" );

		GUI.color = __blueColor;

		if( GUILayout.Button( "Envelope Window", GUILayout.ExpandWidth( false ) ) )
		{
			PopWindow();
		}
	}
}
