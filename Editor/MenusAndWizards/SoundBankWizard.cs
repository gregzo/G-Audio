using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class SoundBankWizard : EditorWindow {

	public enum SampleRates{ _24000, _44100, _48000, _88200, _96000 };

	SampleRates _sampleRate = SampleRates._44100;

	void OnEnable()
	{
		this.maxSize = new Vector2( 200, 100 );
		this.minSize = new Vector2( 200, 100 );
	}

	void OnGUI()
	{
		EditorGUIUtility.fieldWidth = 60f;
		EditorGUIUtility.labelWidth = 80f;
		_sampleRate = ( SampleRates )EditorGUILayout.EnumPopup( "Sample Rate:", _sampleRate, GUILayout.ExpandWidth( false ) );

		GUILayout.Space( 10f );

		GUI.color = Color.green;
		if( GUILayout.Button( "Create", GUILayout.Width( 70f ) ) )
		{
			GATSoundBank soundBank = GATEditorUtilities.CreateAsset< GATSoundBank >("New SoundBank" );

			switch( _sampleRate )
			{
				case SampleRates._24000:
				soundBank.Init( 24000 );
				break;

				case SampleRates._44100:
				soundBank.Init( 44100 );
				break;

				case SampleRates._48000:
				soundBank.Init( 48000 );
				break;

				case SampleRates._88200:
				soundBank.Init( 88200 );
				break;

				case SampleRates._96000:
				soundBank.Init( 96000 );
				break;

			}

			this.Close();
		}
	}
}
