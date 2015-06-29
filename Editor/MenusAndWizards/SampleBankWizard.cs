using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class SampleBankWizard : EditorWindow 
{
	public enum BankType{ Simple, Active }

	BankType 		_bankType = BankType.Active;
	GATSoundBank 	_soundBank;

	static string __soundBankHelp = "Sample Banks are scene objects which load SoundBank assets. " +
		"If you have not created a Sound Bank yet, you can do so using the Create menu in the Project Window ( Create/G-Audio/Sound Bank )";

	static string[] __bankTypesHelp = new string[]
	{
		"A simple sample bank used to load Sound Bank assets in memory. " +
		"Use it if you do not need the processed samples caching more advanced Sample Bank classes provide, " +
		"or if you intend to process samples yourself. Note that the Pulsed Samples component will not work with this type of bank.",

		"The Active Sample Bank adds processed samples caching functionnalities, enabling automatic sample processing and memory management. " +
		"Required to use Pulsed Samples components. Every call to GetProcessedSample() at a new pitch will result in a new " +
		"resampled version being cached, unless the processed sample object is released explicitly with the Release() method. The Pulse Samples component handles " +
		"this automatically."
	};

	void OnEnable()
	{
		this.maxSize = new Vector2( 250f, 200f );
		this.minSize = new Vector2( 250f, 200f );
	}

	void OnGUI()
	{
		EditorGUIUtility.fieldWidth = 150f;
		EditorGUIUtility.labelWidth = 80f;
		_soundBank = ( GATSoundBank )EditorGUILayout.ObjectField( "Sound Bank: ", _soundBank, typeof( GATSoundBank ), false, GUILayout.ExpandWidth( false ) );

		if( _soundBank == false )
		{
			EditorGUILayout.HelpBox( __soundBankHelp, MessageType.Info );
			GUI.enabled = false;
		}
		else
		{
			_bankType = ( BankType )EditorGUILayout.EnumPopup( "Bank Type: ", _bankType, GUILayout.ExpandWidth( false ) );
			EditorGUILayout.HelpBox( __bankTypesHelp[ ( int )_bankType ], MessageType.Info );
		}

		GUI.color = Color.green;
		if ( GUILayout.Button( "Create", GUILayout.Width( 70f ) ) )
		{
			GATEditorUtilities.CheckManager();

			GATSampleBank bank = null;

			switch( _bankType )
			{
			case BankType.Simple:
				bank = GATEditorUtilities.NewChildGO< GATSampleBank >( "Sample Bank" );
				break;

			case BankType.Active:
				bank = GATEditorUtilities.NewChildGO< GATActiveSampleBank >( "Sample Bank" );
				break;
			}

			bank.EditorUpdateSoundBank( _soundBank );

			this.Close();
		}
	}
}
