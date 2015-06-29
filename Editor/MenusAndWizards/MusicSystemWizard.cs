using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class MusicSystemWizard : EditorWindow 
{
	GATActiveSampleBank _sampleBank;
	
	void OnGUI()
	{
		bool canCreate = false;

		EditorGUIUtility.fieldWidth = 70f;
		EditorGUIUtility.labelWidth = 80f;

		_sampleBank = ( GATActiveSampleBank )EditorGUILayout.ObjectField( "Sample Bank:", _sampleBank, typeof( GATSampleBank ), true );

		if( _sampleBank != null )
		{
			if( _sampleBank.SoundBank == null )
			{
				EditorGUILayout.HelpBox( "Your SampleBank must refer to a SoundBank!", MessageType.Error );
			}
			else canCreate = true;
		}
		else
		{
			EditorGUILayout.HelpBox( "Creating a Music System is a shortcut to create and link a MasterPulse, an Envelope, and a Pattern. Creating a Music System requires a SampleBank object in your scene.", MessageType.Info );
		}

		GUI.enabled = canCreate;

		GUI.color = Color.green;

		if( GUILayout.Button( "Create", GUILayout.ExpandWidth( false ) ) )
		{
			MasterPulseModule pulse 	= GATEditorUtilities.NewChildGO< MasterPulseModule >( "Music System" );

			EnvelopeModule envelope 	= GATEditorUtilities.NewChildGO< EnvelopeModule >( "Envelope", pulse.gameObject );
			envelope.Pulse				= pulse;
			envelope.MaxSamples			= _sampleBank.SoundBank.SizeOfShortestSample();

			PulsedPatternModule pattern = GATEditorUtilities.NewChildGO< PulsedPatternModule >( "Pattern", pulse.gameObject );
			pattern.Envelope			= envelope;
			pattern.SampleBank			= _sampleBank;
			pattern.Pulse				= pulse;

			pattern.AddSample( _sampleBank.SoundBank.SampleInfos[ 0 ].Name );

			this.Close();
		}
	}
}
