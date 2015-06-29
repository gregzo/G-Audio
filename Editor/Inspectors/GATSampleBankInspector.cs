using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using GAudio;

[ CustomEditor( typeof( GATSampleBank ), true ) ]
public class GATSampleBankInspector : GATBaseInspector 
{
	GATSampleBank _sampleBank;

	void OnEnable()
	{
		_sampleBank = target as GATSampleBank;
	}

	void OnDisable()
	{
		if( _sampleBank != null )
			EditorUtility.SetDirty( _sampleBank );
	}

	public override void OnInspectorGUI()
	{
		int i;
		GATSoundBank soundBank;

		base.OnInspectorGUI();

		if( _sampleBank == null )
			return;

		GUILayout.Space( 5f );

		EditorGUIUtility.fieldWidth = 70f;

		for( i = 0; i < _sampleBank.SoundBanks.Count; i++ )
		{
			soundBank = _sampleBank.SoundBanks[ i ];
			GUILayout.BeginHorizontal();

			_sampleBank.SoundBanks[ i ] = EditorGUILayout.ObjectField( soundBank, typeof( GATSoundBank ), false, GUILayout.ExpandWidth( false ) ) as GATSoundBank;

			if( GUI.changed )
			{
				CheckBanks( _sampleBank.SoundBanks[ i ], i );
			}

			if( soundBank != null )
			{
				GUILayout.Label( soundBank.SampleRate.ToString() + " khz", GUILayout.Width( 60f ) );
			}
			else GUILayout.Space( 64f );

			if( i > 0 )
			{
				GUI.color = Color.red;
				if( GUILayout.Button( "X", EditorStyles.miniButton, GUILayout.Width( 20f ) ) )
				{
					_sampleBank.SoundBanks.RemoveAt( i );
					_sampleBank.EditorUpdateSoundBank();
					break;
				}
				GUI.color = Color.white;
			}

			GUILayout.EndHorizontal();
		}

		if( GUILayout.Button( "Add Bank", GUILayout.Width( 65f ) ) )
		{
			_sampleBank.SoundBanks.Add ( null );
		}

		if( _sampleBank.IsLoaded == false )
		{
			_sampleBank.extraCapacity = EditorGUILayout.IntField( "Extra Capacity", _sampleBank.extraCapacity, GUILayout.ExpandWidth( false ) );
		}


		if( _sampleBank.SoundBank != null )
		{
			if( _sampleBank.IsLoaded )
			{
				GUI.enabled = false;
			}

			EditorGUIUtility.labelWidth = 100f;
			EditorGUIUtility.fieldWidth = 80f;

			GUILayout.BeginHorizontal();
			_sampleBank.AllocationMode = ( GATDataAllocationMode )EditorGUILayout.EnumPopup( "Allocation Mode:", _sampleBank.AllocationMode, GUILayout.ExpandWidth( false ) );

			GUI.enabled = true;

			if( _sampleBank.IsLoaded == false )
			{
				GUI.color = Color.green;
				if( GUILayout.Button( "Load", __buttonOptions ) )
				{
					_sampleBank.LoadAll();
				}
			}
			else
			{
				GUI.color = Color.red;
				if( GUILayout.Button( "Unload", __buttonOptions ) )
				{
					_sampleBank.UnloadAll();

					_sampleBank.AutoLoadInEditMode = false;
				}
			}

			GUILayout.EndHorizontal();

			GUI.color = Color.white;

			_sampleBank.LoadInAwake 		= GUILayout.Toggle( _sampleBank.LoadInAwake, "Load in Awake", GUILayout.Width( 120f ) );

			bool autoLoad = _sampleBank.AutoLoadInEditMode;
			_sampleBank.AutoLoadInEditMode	= GUILayout.Toggle( _sampleBank.AutoLoadInEditMode, "Auto Load in Edit Mode", GUILayout.Width( 150f ) );

			if( Application.isPlaying == false && autoLoad != _sampleBank.AutoLoadInEditMode )
			{
				if( autoLoad )
				{
					if( _sampleBank.IsLoaded )
						_sampleBank.UnloadAll();
				}
				else
				{
					if( _sampleBank.IsLoaded == false )
					{
						_sampleBank.LoadAll();
					}
				}
			}

			if( _sampleBank.IsLoaded == false )
				return;

			GUI.color = __purpleColor;

			string[] allNames = _sampleBank.AllSampleNames;
			foreach( string sampleName in allNames )
			{
				if( GUILayout.Button( sampleName, __largeButtonOptions ) )
				{
					GATData data = _sampleBank.GetAudioData( sampleName );
					GATManager.DefaultPlayer.PlayData( data, 0 );
				}
			}
		}
		else
		{
			EditorGUILayout.HelpBox( string.Format( "None of the specified SoundBanks match your current {0}kHz sample rate.", GATInfo.OutputSampleRate ), MessageType.Warning );
		}
	}

	protected void CheckBanks( GATSoundBank newBank, int index )
	{
		if( newBank == null )
		{
			_sampleBank.EditorUpdateSoundBank();
			return;
		}
			
		GATSoundBank soundBank;

		for( int i = 0; i < _sampleBank.SoundBanks.Count; i++ )
		{
			if( i == index )
				continue;

			soundBank = _sampleBank.SoundBanks[ i ];

			if( soundBank == newBank )
			{
				EditorUtility.DisplayDialog( "Duplicate SoundBank Error", string.Format( "SoundBank {0} is already referenced .", newBank.name ), "OK" );
				_sampleBank.SoundBanks[ index ] = null;   
				break;
			}
			else if( soundBank.SampleRate == newBank.SampleRate )
			{
				EditorUtility.DisplayDialog( "Duplicate Sample Rate Error", string.Format( "{0}khz sample rate is already covered by SoundBank {1}  .", soundBank.SampleRate.ToString() ,soundBank.name ), "OK" );
				_sampleBank.SoundBanks[ index ] = null; 
				break;
			}
			else if( soundBank.SampleInfos.Count != newBank.SampleInfos.Count )
			{
				EditorUtility.DisplayDialog( "SoundBank Mismatch Error", string.Format( "SoundBank {0} does not have the same number of sounds as SoundBank {1}. Check your SoundBanks and make sure they correspond.", newBank.name ,soundBank.name ), "OK" );
				_sampleBank.SoundBanks[ index ] = null;  
				break;
			}
		}

		_sampleBank.EditorUpdateSoundBank();
	}
}
