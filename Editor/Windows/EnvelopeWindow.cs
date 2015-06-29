//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------

using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

[ System.Serializable ]
public class EnvelopeWindow : EditorWindow 
{
	enum ControlType{ AttackStart, AttackEnd, ReleaseStart, ReleaseEnd, Position }
	enum DurationUnit{ Seconds, Samples }
	
	EnvelopeModule _envelopeModule;

	EnvelopeHandle[] _handles;
	EnvelopeHandle   _posHandle;
	EnvelopeHandle   _draggedHandle;

	DurationUnit _durationUnit;

	float _scrollViewWidth;
	int   _samplesPerPixel = 1000;
	
	Vector2 _start, _end;
	Vector2 _scrollPos;

	Vector3[] _linePoints;
	
	GATSoundBank    _selectedBank;
	int 			_selectedBankMin; 
	int 			_selectedBankMax;
	
	const float HANDLE_RADIUS 	= 6f;
	const float HEIGHT_TOP		= 45f;
	const float HEIGHT_BOTTOM	= 110f;
	const float LEFT_MARGIN		= 20f;

	static string __currentScene;
	
	void OnEnable()
	{
		if( _handles == null )
			SetupHandles();

		EditorApplication.playmodeStateChanged   += DidChangePlayMode;
		EditorApplication.hierarchyWindowChanged += OnHierarchyWindowChange;

		__currentScene = EditorApplication.currentScene;
	}

	void DidChangePlayMode()
	{
		if( Application.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode )
		{
			this.Close();
		}
	}

	void OnHierarchyWindowChange()
	{
		if( __currentScene != EditorApplication.currentScene )
		{
			this.Close();
		}
	}

	void OnLostFocus()
	{
		if( _envelopeModule != null )
			EditorUtility.SetDirty( _envelopeModule );
	}

	void SetupHandles()
	{
		EnvelopeHandle.HandleRadius = HANDLE_RADIUS;
		EnvelopeHandle.LeftMargin   = LEFT_MARGIN;

		_handles = new EnvelopeHandle[ 4 ];

		_handles[ 0 ] = new EnvelopeHandle( ControlType.AttackStart, 	HEIGHT_BOTTOM 	);
		_handles[ 1 ] = new EnvelopeHandle( ControlType.AttackEnd, 		HEIGHT_TOP 		);
		_handles[ 2 ] = new EnvelopeHandle( ControlType.ReleaseStart, 	HEIGHT_TOP 		);
		_handles[ 3 ] = new EnvelopeHandle( ControlType.ReleaseEnd,		HEIGHT_BOTTOM 	);

		_handles[ 0 ].SetClamps( 		  null, _handles[ 1 ] 	);
		_handles[ 1 ].SetClamps( _handles[ 0 ], _handles[ 2 ] 	);
		_handles[ 2 ].SetClamps( _handles[ 1 ], _handles[ 3 ] 	);
		_handles[ 3 ].SetClamps( _handles[ 2 ],  null 			);

		_posHandle = new EnvelopeHandle( ControlType.Position, HEIGHT_BOTTOM + 10f );

		_linePoints = new Vector3[ 4 ];

		this.maxSize = new Vector2( 4000f, 300f );
		this.minSize = new Vector2( 420f, 300f );
	}

	public void InitWithEnvelope( EnvelopeModule module )
	{
		if( module == _envelopeModule )
			return;

		if( _envelopeModule != null )
		{
			DiscardEnvelope();
		}

		if( _handles == null )
		{
			SetupHandles();
		}

		_envelopeModule  = module;
		_samplesPerPixel = module.SamplesPerPixel;
		_scrollPos 		 = new Vector2( module.ScrollXPosition, 0f );

		EnvelopeHandle.MaxSamples = module.MaxSamples;

		UpdateZoom();

		_handles[ 0 ].PosInSamples = module.Offset;
		_handles[ 1 ].PosInSamples = module.FadeIn + module.Offset;
		
		_handles[ 3 ].PosInSamples = module.Length + module.Offset;
		_handles[ 2 ].PosInSamples = _handles[ 3 ].PosInSamples - module.FadeOut;

		UpdatePosHandle();

		module.onLengthWasMapped += OnLengthWasMapped;
	}

	void OnDisable()
	{
		EditorApplication.playmodeStateChanged   -= DidChangePlayMode;
		EditorApplication.hierarchyWindowChanged -= OnHierarchyWindowChange;

		if( _envelopeModule == null )
			return;

		EditorUtility.SetDirty( _envelopeModule );

		DiscardEnvelope();


	}

	void DiscardEnvelope()
	{
		_envelopeModule.ScrollXPosition = _scrollPos.x;
		_envelopeModule.MaxSamples 		= EnvelopeHandle.MaxSamples;
		_envelopeModule.SamplesPerPixel = _samplesPerPixel;
		
		_envelopeModule.onLengthWasMapped -= OnLengthWasMapped;
		
		_envelopeModule = null;
	}

	void OnLengthWasMapped( bool didAdaptFades )
	{
		if( _envelopeModule.MapLengthToPulse == false )
			return;

		if( didAdaptFades )
		{
			_handles[ 1 ].PosInSamples = _envelopeModule.Offset + _envelopeModule.FadeIn;
		}

		_handles[ 2 ].PosInSamples = _envelopeModule.Offset + _envelopeModule.Length - _envelopeModule.FadeOut;
		_handles[ 3 ].PosInSamples = _envelopeModule.Offset + _envelopeModule.Length;

		UpdatePosHandle();

		Repaint();
	}

	void OnGUI()
	{
		int i;
		bool shouldRepaint = false;

		//*************************************************************
		//******************* Zoom slider *****************************
		GUILayout.Space( 10f );
		EditorGUIUtility.labelWidth = 55f;

		int samplesPerPixel = _samplesPerPixel;

		_samplesPerPixel = EditorGUILayout.IntSlider( "Zoom: ", _samplesPerPixel, 2, 1000 );

		if( samplesPerPixel != _samplesPerPixel )
		{
			UpdateZoom();
		}

		//*************************************************************
		//******************* ScrollView  *****************************
		Vector2 prevScroll = _scrollPos;

		_scrollPos = GUI.BeginScrollView( new Rect( 0f, 0f, this.position.width, this.position.height ), _scrollPos, new Rect( 0, 0, _scrollViewWidth ,200f ));

		if( prevScroll != _scrollPos )
			shouldRepaint = true;

		//*************************************************************
		//******************* Mouse Events ****************************
		if( Event.current.isMouse )
		{
			EventType eventType = Event.current.type;

			Vector2 mousePos = Event.current.mousePosition;

			if( eventType == EventType.MouseDown )
			{
				_draggedHandle = null;
				foreach( EnvelopeHandle handle in _handles )
				{
					if( handle.HitTest( mousePos ) )
					{
						_draggedHandle = handle;
						break;
					}
				}

				if( _draggedHandle == null && _posHandle.HitTest( mousePos ) )
				{
					_draggedHandle = _posHandle;
				}
			}
			else if( eventType == EventType.MouseUp )
			{
				_draggedHandle = null;
			}
			else if( eventType == EventType.MouseDrag && _draggedHandle != null )
			{
				if( _draggedHandle.Control == ControlType.Position )
				{
					int posInSamples = _draggedHandle.EvaluatePosInSamples( mousePos.x );
					int halfLength   = _envelopeModule.Length / 2;

					if( posInSamples - halfLength < 0 )
					{
						posInSamples = halfLength;
					}
					else if( posInSamples + halfLength > EnvelopeHandle.MaxSamples )
					{
						posInSamples = EnvelopeHandle.MaxSamples - halfLength;
					}

					_handles[ 0 ].PosInSamples = posInSamples - halfLength;
					_handles[ 1 ].PosInSamples = _handles[ 0 ].PosInSamples + _envelopeModule.FadeIn;

					_handles[ 3 ].PosInSamples = posInSamples + halfLength;
					_handles[ 2 ].PosInSamples = _handles[ 3 ].PosInSamples - _envelopeModule.FadeOut;

					_posHandle.PosInSamples = posInSamples;

					_envelopeModule.Offset = _handles[0].PosInSamples;
				}
				else
				{
					_draggedHandle.Drag( mousePos.x );

					if( _draggedHandle.Control == ControlType.AttackStart )
					{
						_envelopeModule.Offset = _draggedHandle.PosInSamples;
						_envelopeModule.FadeIn = _handles[ 1 ].PosInSamples - _handles[ 0 ].PosInSamples;
						_envelopeModule.Length = _handles[ 3 ].PosInSamples - _handles[ 0 ].PosInSamples;
						UpdatePosHandle();
					}
					else if( _draggedHandle.Control == ControlType.AttackEnd )
					{
						_envelopeModule.FadeIn = _handles[ 1 ].PosInSamples - _handles[ 0 ].PosInSamples;
					}
					else if( _draggedHandle.Control == ControlType.ReleaseStart )
					{
						_envelopeModule.FadeOut = _handles[ 3 ].PosInSamples - _handles[ 2 ].PosInSamples;
					}
					else //ReleaseEnd
					{
						_envelopeModule.FadeOut = _handles[ 3 ].PosInSamples - _handles[ 2 ].PosInSamples;
						_envelopeModule.Length = _handles[ 3 ].PosInSamples - _handles[ 0 ].PosInSamples;
						UpdatePosHandle();
					}
				}

				shouldRepaint = true;
			}
		}	

		//*************************************************************
		//******************* Drawing *********************************
		for( i = 0; i < 4; i++ )
		{
			_handles[ i ].DrawHandle();
		}

		_posHandle.DrawHandle();

		for( i = 0; i < 4; i++ )
		{
			_linePoints[ i ] = _handles[ i ].Position;
		}

		Handles.color = Color.blue;
		Handles.DrawAAPolyLine( 2f, _linePoints );
		Handles.color = Color.grey;
		Handles.DrawLine( _start, _end );

		GUI.EndScrollView();

		GUILayout.Space( 110f );

		//*************************************************************
		//******************* Info ************************************
		_durationUnit = ( DurationUnit )EditorGUILayout.EnumPopup( _durationUnit, GUILayout.Width( 60f ) );
		GUILayoutOption labelWidth = GUILayout.Width( 90f );

		GUILayout.BeginHorizontal();
		GUILayout.Label( "Length: " + GetLengthStringForSamples( _envelopeModule.Length ), labelWidth );
		GUILayout.Label( "Offset: " + GetLengthStringForSamples( _envelopeModule.Offset ), labelWidth );
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label( "Fade In: "  + GetLengthStringForSamples( _envelopeModule.FadeIn  ), labelWidth );
		GUILayout.Label( "Fade Out: " + GetLengthStringForSamples( _envelopeModule.FadeOut ), labelWidth );
		GUILayout.EndHorizontal();

		GUILayout.Space( 10f );
		_envelopeModule.Pulse = ( PulseModule )EditorGUILayout.ObjectField( _envelopeModule.Pulse, typeof( PulseModule ), true, GUILayout.Width( 130f ) );

		GUI.enabled = _envelopeModule.Pulse != null;

		_envelopeModule.MapLengthToPulse = GUILayout.Toggle( _envelopeModule.MapLengthToPulse, "Map Length To Pulse", GUILayout.ExpandWidth( false ) );

		GUI.enabled = true;
		if( _envelopeModule.Pulse != null && _envelopeModule.MapLengthToPulse )
		{
			GUILayout.Label( "Length to Pulse ratio: " + _envelopeModule.LengthToPulseRatio.ToString( "0.00" ) );
			_envelopeModule.LengthToPulseRatio = GUILayout.HorizontalSlider( _envelopeModule.LengthToPulseRatio, .1f, 8f, GUILayout.Width( 190f ) );
		}

		//************************************************************************************
		//*********************** ObjectPicker Messages Handling *****************************

		if (Event.current.commandName == "ObjectSelectorUpdated")
		{
			GATSoundBank bank = EditorGUIUtility.GetObjectPickerObject() as GATSoundBank;

			if( bank != null )
			{
				_selectedBankMax = bank.SizeOfLongestSample();
				_selectedBankMin = bank.SizeOfShortestSample();

				shouldRepaint = true;
				_selectedBank = bank;
			}
		}

		//************************************************************************************
		//*********************** SoundBank Clamping *****************************
		GUILayout.BeginArea( new Rect( 200f, 130f, 200f, 150f ) );
		GUILayout.Label( "Max Length: " + GetLengthStringForSamples( EnvelopeHandle.MaxSamples ) );

		EditorGUIUtility.labelWidth = 70f;
		_selectedBank = ( GATSoundBank )EditorGUILayout.ObjectField( "SoundBank:", _selectedBank, typeof( GATSoundBank ), false );

		if( _selectedBank != null )
		{
			if( GUILayout.Button( "Shortest sample:" + GetLengthStringForSamples( _selectedBankMin ), GUILayout.Width( 140f ) ) )
			{
				EnvelopeHandle.MaxSamples = _selectedBankMin;
				UpdateZoom();
			}

			if( GUILayout.Button( "Longest sample:" + GetLengthStringForSamples( _selectedBankMax ), GUILayout.Width( 140f ) ) )
			{
				EnvelopeHandle.MaxSamples = _selectedBankMax;
				UpdateZoom();
			}
		}
		else
		{
			EditorGUILayout.HelpBox( "Select a sound bank to easily map this envelope's max length to the bank's shortest or longest sample.", MessageType.Info );
		}

		//************************************************************************************
		//*********************** Reverse and Normalize **************************************

		_envelopeModule.Reverse = GUILayout.Toggle( _envelopeModule.Reverse, "Reverse" );
		GUILayout.BeginHorizontal();
		_envelopeModule.Normalize = GUILayout.Toggle( _envelopeModule.Normalize, "Normalize", GUILayout.Width( 75f ) );
		if( _envelopeModule.Normalize )
		{
			GUILayout.Label( _envelopeModule.NormalizeValue.ToString("0.00") );
			GUILayout.EndHorizontal();
			_envelopeModule.NormalizeValue = GUILayout.HorizontalSlider( _envelopeModule.NormalizeValue, 0f, 1f );
		}
		else GUILayout.EndHorizontal();


		GUILayout.EndArea();

		if( shouldRepaint )
			Repaint();
	}

	string GetLengthStringForSamples( int samples )
	{
		if( _durationUnit == DurationUnit.Seconds )
		{
			return ( ( float )samples / GATInfo.OutputSampleRate ).ToString( "0.00s" );
		}
		else return samples.ToString( "N0" );
	}

	void UpdatePosHandle()
	{
		_posHandle.PosInSamples = _envelopeModule.Length / 2 + _envelopeModule.Offset;
	}

	void UpdateZoom()
	{
		float startPosX = _handles[ 0 ].Position.x;
		EnvelopeHandle.TotalLength  = ( float )EnvelopeHandle.MaxSamples / _samplesPerPixel;
		_scrollViewWidth = EnvelopeHandle.TotalLength + LEFT_MARGIN * 2;
		
		_start = new Vector2( LEFT_MARGIN, HEIGHT_BOTTOM );
		_end   = new Vector2( LEFT_MARGIN + EnvelopeHandle.TotalLength, HEIGHT_BOTTOM );

		foreach( EnvelopeHandle handle in _handles )
			handle.UpdatePosition();

		UpdatePosHandle();
		_posHandle.UpdatePosition();

		float deltaX = _handles[ 0 ].Position.x - startPosX;

		_scrollPos = new Vector2( _scrollPos.x + deltaX, 0f );
	}



	//******************************************************************
	//****************** Envelope Handles Helper Class *****************
	class EnvelopeHandle
	{
		public readonly ControlType Control;
		public static float HandleRadius;
		public static int   MaxSamples;
		public static float TotalLength;
		public static float LeftMargin;

		public Vector2 Position{ get; private set; }

		public EnvelopeHandle( ControlType control, float yPos )
		{
			Control 		= control;
			Position 		= new Vector2( 0f, yPos );
		}

		public void SetClamps( EnvelopeHandle leftClamp, EnvelopeHandle rightClamp )
		{
			_leftClamp 		= leftClamp;
			_rightClamp 	= rightClamp;
		}

		public int PosInSamples
		{ 
			get{ return _posInSamples; } 
			set 
			{
				_posInSamples = value;
				UpdatePosition();
			}
		}
		int _posInSamples;

		EnvelopeHandle _leftClamp;
		EnvelopeHandle _rightClamp;

		public void DrawHandle()
		{
			Handles.DrawSolidDisc( Position, Vector3.forward, 5f );
		}

		public bool HitTest( Vector2 pos )
		{
			return (   pos.x > Position.x - HandleRadius && pos.x < Position.x + HandleRadius 
			        && pos.y > Position.y - HandleRadius && pos.y < Position.y + HandleRadius );
		}

		public void Drag( float xPos )
		{
			int samplePos;
			int clampValue;

			samplePos = EvaluatePosInSamples( xPos );

			clampValue = _leftClamp == null ? 0 : _leftClamp.PosInSamples;

			if( samplePos < clampValue )
				samplePos = clampValue;

			clampValue = _rightClamp == null ? MaxSamples : _rightClamp.PosInSamples;

			if( samplePos > clampValue )
				samplePos = clampValue;

			PosInSamples = samplePos;

			UpdatePosition();
		}

		public int EvaluatePosInSamples( float xPos )
		{
			return ( int )( ( ( xPos - LeftMargin ) / TotalLength ) * MaxSamples );
		}

		public void UpdatePosition()
		{
			float normalizedPos = ( float )PosInSamples / MaxSamples;
			Position = new Vector2( LeftMargin + ( float )( normalizedPos * TotalLength ), Position.y );
		}
	}
}
