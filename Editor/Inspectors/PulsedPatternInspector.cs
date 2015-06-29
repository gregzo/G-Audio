using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using GAudio;

[ CustomEditor( typeof( PulsedPatternModule ) ) ]
public class PulsedPatternInspector : GATBaseInspector 
{
	PulsedPatternModule   _pulsedPattern;
	PatternSample[] 	  _samplesInfo;

	string[] _sampleOptions;
	int 	 _selectedSampleName;
	int 	 _editedSampleIndex  = -1;
	int 	 _changedSampleIndex = -1;

	int[]    _playerTrackNumbers;
	string[] _playerTrackStrings;

	protected virtual void OnEnable()
	{
		_pulsedPattern = target as PulsedPatternModule;
		_samplesInfo   = _pulsedPattern.Samples;

		UpdateSampleOptions();

		_editedSampleIndex  = -1;
		_changedSampleIndex = -1;

		if( _pulsedPattern.Player != null )
		{
			_playerTrackNumbers = new    int[ _pulsedPattern.Player.NbOfTracks ];
			_playerTrackStrings = new string[ _playerTrackNumbers.Length       ];

			for( int i = 0; i < _playerTrackNumbers.Length; i++ )
			{
				_playerTrackNumbers[ i ] = i;
				_playerTrackStrings[ i ] = i.ToString();
			}
		}

		if( _pulsedPattern.SampleBank != null && _pulsedPattern.SampleBank.SoundBank != null )
		{
			CheckSamples();
		}
	}

	void OnDisable()
	{
		if( _pulsedPattern != null )
			EditorUtility.SetDirty( _pulsedPattern );
	}

	void CheckSamples( )
	{
		GATSoundBank soundBank = _pulsedPattern.SampleBank.SoundBank;

		foreach( PatternSample info in _samplesInfo )
		{
			if( soundBank.ContainsSampleNamed( info.SampleName ) == false )
			{
				Debug.LogError("Sample not found in bank! " + info.SampleName );
			}
		}
	}

	void UpdateSampleOptions()
	{
		if( _pulsedPattern.SampleBank != null && _pulsedPattern.SampleBank.IsLoaded != false )
		{
			string[] allSampleNames = _pulsedPattern.SampleBank.AllSampleNames;
			_sampleOptions = new string[ allSampleNames.Length ];
			allSampleNames.CopyTo( _sampleOptions, 0 );
		}
		else _sampleOptions = null;
	}

	static GUIContent __stepsFieldContent 	= new GUIContent( "Steps:"			, "Subscribe to specific steps. Disabled steps are steps the pulse bypasses." );
	static GUIContent __playerFieldContent 	= new GUIContent( "Player:"			, "If you have multiple GATPlayers, you can route playback to custom players." );
	static GUIContent __trackFieldContent 	= new GUIContent( "Track:"			, "The track in the player through which to play ." );
	static GUIContent __randomDelayContent	= new GUIContent( "Random Delay"	, "Samples playback will be randomly delayed by a duration of up to 1 pulse, adjustable with the slider below." );

	static string[] __orderHelpStrings = new string[]
	{ 
		"Samples are triggered according to the index of the pulse's current step. If this is greater than the number of samples, wrapping occurs.", 
		"Samples are played one after the other, regardless of step index.", 
		"Any sample may be played on any step.", 
		"Samples are triggered according to the index of the master pulse's current step, bypassing any sub-pulse step.", 
		"Samples are played synchronously ( chord ). " 
	};

	public override void OnInspectorGUI()
	{
		int i;
		Rect boxRect;
		bool bankIsLoaded = false;

		base.OnInspectorGUI();

		GUILayout.Space( 10f );

//**********************************************************************
//********************** Pulse Box *************************************
		boxRect = EditorGUILayout.BeginVertical();

		GUI.Box( boxRect, "Pulse", __boxStyle );

		if( _pulsedPattern.Pulse != null ) //Pulse is of type MasterPulseModule, let's put a shortcut to it's Start/Stop methods
		{
			MasterPulseModule masterPulse = _pulsedPattern.Pulse as MasterPulseModule;
			GUILayout.Space( 5f );

			if( masterPulse != null )
			{
				if( masterPulse.IsPulsing == false )
				{
					GUI.color = Color.green;
					if( GUILayout.Button( "Start", __buttonOptions ) )
					{
						masterPulse.StartPulsing( 0 );
					}
				}
				else
				{
					GUI.color = Color.red;
					if( GUILayout.Button( "Stop", __buttonOptions ) )
					{
						masterPulse.Stop();
					}
				}
			}
	
			GUI.color = Color.white;
		}

		GUILayout.Space( 5f );

		//***************** Pulse Steps *******************************
		if( _pulsedPattern.Pulse != null )
		{
			bool[] subscribedSteps = _pulsedPattern.SubscribedSteps;

			GUILayout.BeginHorizontal();
			GUILayout.Space( 45f );
			for( i = 0; i < subscribedSteps.Length; i++ )
			{
				GUI.enabled = _pulsedPattern.Pulse.Steps[ i ];
				GUILayout.Label(  i.ToString(), GUILayout.Width( 15f ) );
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Space( 5f );
			GUILayout.Label( __stepsFieldContent, GUILayout.Width( 35f ) );

			for( i = 0; i < subscribedSteps.Length; i++ )
			{
				GUI.enabled 		 = _pulsedPattern.Pulse.Steps[ i ];
				subscribedSteps[ i ] = GUILayout.Toggle( subscribedSteps[i], "", GUILayout.Width( 15f ) );
			}
			GUILayout.EndHorizontal();

			GUI.enabled = true;
		}
		else
		{
			GUILayout.Space( 30f );
		}

		GUILayout.Space( 5f );
		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f );

		_pulsedPattern.Pulse = ( PulseModule )EditorGUILayout.ObjectField( _pulsedPattern.Pulse, typeof( PulseModule ), true, GUILayout.MaxWidth( 125f ) );

		if( _pulsedPattern.Pulse != null )
		{
			_pulsedPattern.RandomBypass = GUILayout.Toggle( _pulsedPattern.RandomBypass, "Random Bypass" );
		}

		GUILayout.EndHorizontal();

		if( _pulsedPattern.RandomBypass )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space( 90f );
			GUILayout.Label( ( _pulsedPattern.RandomBypassChance * 100 ).ToString("0.##\\%"), GUILayout.Width( 40f ) );
			_pulsedPattern.RandomBypassChance = GUILayout.HorizontalSlider( _pulsedPattern.RandomBypassChance, 0f, 1f, GUILayout.Width( 100f ) );
			GUILayout.EndHorizontal();
		}

		GUILayout.Space( 10f );
		EditorGUILayout.EndVertical();

//********************************************************************

		GUILayout.Space( 10f );

//********************************************************************
//***************** SampleBank Box ***********************************
		boxRect = EditorGUILayout.BeginVertical();
		GUI.Box( boxRect, "Sample Bank", __boxStyle );

		GUILayout.Space( 5f );

		if( _pulsedPattern.SampleBank != null )
		{
			string label;

			bankIsLoaded = _pulsedPattern.SampleBank.IsLoaded;

			if( bankIsLoaded )
			{
				label = "Loaded";
				GUI.enabled = false;
			}
			else
			{
				label = "Load";
				GUI.color = Color.green;
			}
			
			if( GUILayout.Button( new GUIContent( label, "In edit mode, SampleBanks do not automatically reload to preserve memory." ), __buttonOptions ) )
			{
				_pulsedPattern.SampleBank.LoadAll();
				UpdateSampleOptions();
			}
			GUI.enabled = true;
			GUI.color   = Color.white;
		}
		GUILayout.Space( 5f );

		GATActiveSampleBank cachedBank = _pulsedPattern.SampleBank;

		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f );
		_pulsedPattern.SampleBank = ( GATActiveSampleBank )EditorGUILayout.ObjectField( _pulsedPattern.SampleBank, typeof( GATActiveSampleBank ), true, GUILayout.MaxWidth( 125f ) );

		GUILayout.EndHorizontal();

		if( _pulsedPattern.SampleBank != null && bankIsLoaded == false )
		{
			EditorGUILayout.HelpBox( "The selected bank is not loaded yet. Click Load to enable previewing.", MessageType.Warning );
		}

		if( _pulsedPattern.SampleBank != cachedBank )
		{
			UpdateSampleOptions();
		}

		GUILayout.Space( 10f );
		EditorGUILayout.EndVertical();

//*******************************************************************

		GUILayout.Space( 10f );

//*******************************************************************
//***************** Envelope Box ************************************

		boxRect = EditorGUILayout.BeginVertical();
		GUI.Box( boxRect, "Envelope", __boxStyle );

		GUILayout.Space( 30f );

		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f );
		_pulsedPattern.Envelope = ( EnvelopeModule )EditorGUILayout.ObjectField( _pulsedPattern.Envelope, typeof( EnvelopeModule ), true, GUILayout.MaxWidth( 125f ) );

		if( _pulsedPattern.Envelope != null )
		{
			GUI.color = __blueColor;
			if( GUILayout.Button( "Envelope Window", __largeButtonOptions ) )
			{
				EnvelopeWindow window = EditorWindow.GetWindow< EnvelopeWindow >();
				window.InitWithEnvelope( _pulsedPattern.Envelope );
			}
			GUI.color = Color.white;
		}
		GUILayout.EndHorizontal();

		if( _pulsedPattern.Envelope == null )
			EditorGUILayout.HelpBox( "If no envelope is set, the entire samples will be played and pitch shifting is disabled.", MessageType.Info );

		GUILayout.Space( 10f );

		EditorGUILayout.EndVertical();

//*******************************************************************

		GUILayout.Space( 10f );

//*******************************************************************
//***************** Samples Box *************************************

		boxRect = EditorGUILayout.BeginVertical();
		GUI.Box( boxRect, "Samples", __boxStyle );
		
		GUILayout.Space( 30f );

		//******************* Player and track fields ***********************
		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f );
		GUILayout.Label( __playerFieldContent, GUILayout.Width( 40f ) );
		_pulsedPattern.Player = ( GATPlayer )EditorGUILayout.ObjectField( _pulsedPattern.Player, typeof( GATPlayer ), true, GUILayout.Width( 125f ) );
		GUILayout.Label( __trackFieldContent, GUILayout.Width( 40f ) );
		_pulsedPattern.TrackNb = EditorGUILayout.IntPopup( _pulsedPattern.TrackNb, _playerTrackStrings, _playerTrackNumbers, GUILayout.Width( 40f ) );
		GUILayout.Space( 5f );
		GUILayout.EndHorizontal();

		GUILayout.Space( 10f );

		//******************* Playing Order ***********************
		EditorGUILayout.HelpBox( __orderHelpStrings[ ( int )_pulsedPattern.SamplesOrdering ], MessageType.Info );

		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f ); 
		_pulsedPattern.SamplesOrdering = ( AGATPulsedPattern.PlayingOrder )EditorGUILayout.EnumPopup( _pulsedPattern.SamplesOrdering, GUILayout.MaxWidth( 145f ) );
		_pulsedPattern.AddRandomDelay  = GUILayout.Toggle( _pulsedPattern.AddRandomDelay, __randomDelayContent );
		GUILayout.EndHorizontal();

		if( _pulsedPattern.AddRandomDelay )
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space( 110f );
			GUILayout.Label( ( _pulsedPattern.RandomDelayMaxRatio * 100 ).ToString("0.##\\%"), GUILayout.Width( 40f ) );
			_pulsedPattern.RandomDelayMaxRatio = GUILayout.HorizontalSlider( _pulsedPattern.RandomDelayMaxRatio, 0f, 1f,  GUILayout.Width( 100f )  );
			GUILayout.EndHorizontal();
		}

//******************************************************************
		GUILayout.Space( 10f );

//******************************************************************
//*********************** Drawing Samples **************************

		PatternSample info;

		for( i = 0; i < _samplesInfo.Length; i++ )
		{
			info = _samplesInfo[i];

			if( DrawSampleInfo( info, bankIsLoaded, i ) )
			{
				_pulsedPattern.PlaySample( i, AudioSettings.dspTime + .1d );
			}
		}

		if( _sampleOptions != null )
		{
			GUILayout.Space( 20f );

			GUILayout.BeginHorizontal();
			GUILayout.Space( 5f );
			GUI.color = Color.green;
			if( GUILayout.Button( "Add", __buttonOptions ) )
			{
				_pulsedPattern.AddSample( _sampleOptions[ _selectedSampleName ] );
				_samplesInfo = _pulsedPattern.Samples;
			}
			GUI.color = Color.white;
			GUILayout.Space( 5f );
			_selectedSampleName = EditorGUILayout.Popup( _selectedSampleName, _sampleOptions, GUILayout.Width( 100f ) );
			GUILayout.EndHorizontal();
		}

		GUILayout.Space( 10f );
		
		EditorGUILayout.EndVertical();
	}

	static GUIContent __transposeContent = new GUIContent( "Pitch:", "pitch shift in semi-tones, non-integer values are valid" );
	static Color __editedBoxColor = new Color( .94f, .96f, 94f );
	bool DrawSampleInfo( PatternSample info, bool canPreview, int index )
	{
		Rect boxRect;
		bool press = false;

		bool isEditing		 = index == _editedSampleIndex;
		bool shouldCollapse	 = isEditing;

		if( isEditing )
		{
			GUI.backgroundColor = __editedBoxColor;
			boxRect 			= EditorGUILayout.BeginVertical();

			GUI.Box( boxRect, "", __boxStyle );
			GUILayout.Space( 5f );
		}

		Rect foldRect = EditorGUILayout.BeginHorizontal();

		if( canPreview )
		{
			GUI.enabled = true;
		}
		else
		{
			GUI.enabled = false;
		}

		GUILayout.Space( 20f );
		GUILayout.Label( index.ToString(), GUILayout.Width( 20f ) );

		isEditing = EditorGUI.Foldout( new Rect( foldRect.x + 15f, foldRect.y, 15f, foldRect.height ), isEditing, "" );

		if( shouldCollapse && isEditing == false )
		{
			_editedSampleIndex = -1;
		}

		GUI.color = __purpleColor;
		if( GUILayout.Button( info.SampleName, GUILayout.Width( 100f ) ) )
		{
			press = true;
		}
		GUI.color = Color.white;

		GUI.enabled = true;

		EditorGUIUtility.labelWidth = 40f;
		EditorGUIUtility.fieldWidth = 25f;
		info.SemiTones = EditorGUILayout.FloatField( __transposeContent, info.SemiTones, GUILayout.ExpandWidth( false ) );
		GUILayout.Space( 5f );
		info.Gain = GUILayout.HorizontalSlider( info.Gain, 0f, 2f, GUILayout.Width( 100f ) );
		if( GUILayout.Button( info.Gain.ToString( "0.00" ), GUILayout.Width( 35f ) ) )
		{
			info.Gain = 1f;
		}
		  
		GUILayout.Space( 5f );

		EditorGUILayout.EndHorizontal();

		if( isEditing )
		{
			if( _editedSampleIndex != index )
			{
				_editedSampleIndex  = index;
				_changedSampleIndex = -1;
			}

			if( _changedSampleIndex == -1 )
				_changedSampleIndex = GetIndexOfSample( info.SampleName );

			GUILayout.Space( 10f );

			GUILayout.BeginHorizontal();
			int cached = _changedSampleIndex;
			_changedSampleIndex = EditorGUILayout.Popup( _changedSampleIndex, _sampleOptions, GUILayout.Width( 100f ) );

			if( _changedSampleIndex != cached )
			{
				info.SampleName = _sampleOptions[ _changedSampleIndex ];
			}

			GUILayout.Space( 10f );

			GUI.color = Color.red;
			if( GUILayout.Button( "Remove", __buttonOptions ) )
			{
				_pulsedPattern.RemoveSampleAt( index );
				_samplesInfo = _pulsedPattern.Samples;
				_changedSampleIndex = -1;
				_editedSampleIndex = -1;
			}
			GUI.color = Color.white;
			GUILayout.EndHorizontal();

			GUILayout.Space( 5f );

			EditorGUILayout.EndVertical();

			GUI.backgroundColor = Color.white;
		}

		GUILayout.Space( 5f );
			
		return press;
	}

	int GetIndexOfSample( string sampleName )
	{
		int i;
		for( i = 0; i < _sampleOptions.Length; i++ )
		{
			if( sampleName == _sampleOptions[ i ] )
				return i;
		}

		return -1;
	}
}
