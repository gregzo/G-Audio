using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class PulseBaseInspector : GATBaseInspector 
{
	protected int 		  _numSteps;

	private   PulseModule _basePulse;

	protected virtual void OnEnable()
	{
		_basePulse = target as PulseModule;	
		_numSteps  = _basePulse.Steps.Length;
	}
	
	public override void OnInspectorGUI()
	{
		int i;
		bool[] steps;

		base.OnInspectorGUI();

		steps = _basePulse.Steps;
		
		GUILayout.Space( 5f );

		_basePulse.Bypass = GUILayout.Toggle( _basePulse.Bypass, "Bypass", GUILayout.ExpandWidth( false ), GUILayout.Width( 60f ) );

		GUILayout.Space( 5f );
		
		GUILayout.BeginHorizontal();
		GUI.skin.label.fontSize = 10;
		GUILayout.Label( "Number Of Steps:", GUILayout.Width( 90f ) );
		_numSteps = EditorGUILayout.IntField( _numSteps, GUILayout.Width( 25f ) );
		GUILayout.EndHorizontal();
		
		if( Event.current.isKey && Event.current.keyCode == KeyCode.Return ) 
		{
			if( _numSteps != steps.Length )
			{
				UpdateSteps( steps );
			}
		}
		
		GUILayout.Space( 5f );
		
		GUILayout.BeginHorizontal();
		GUI.color = __purpleColor;
		for( i = 0; i < steps.Length; i++ )
		{
			if( GUILayout.Button( i.ToString(), __closeButtonsOptions ) )
			{
				_basePulse.PulseOneShot( i );
			}
		}
		GUI.color = Color.white;
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		
		GUILayout.Space( 3f );
		
		for( i = 0; i < steps.Length; i++ )
		{
			steps[ i ] = GUILayout.Toggle( steps[ i ], "", __closeTogglesOptions );
		}
		
		GUILayout.EndHorizontal();
		
		GUILayout.Space( 5f );

		GUILayout.BeginHorizontal();
		_basePulse.RandomBypassStep = GUILayout.Toggle( _basePulse.RandomBypassStep, "Random Bypass:", GUILayout.ExpandWidth( false ), GUILayout.Width( 110f ) );
		
		if( _basePulse.RandomBypassStep )
		{
			GUILayout.Label( ( _basePulse.StepBypassChance * 100 ).ToString("0.##\\%"), GUILayout.Width( 40f ) );
			_basePulse.StepBypassChance = GUILayout.HorizontalSlider( _basePulse.StepBypassChance, 0f, 1f, GUILayout.Width( 100f ) );
		}
		GUILayout.EndHorizontal();
		GUILayout.Space( 5f );
	}
	
	void UpdateSteps( bool[] prevSteps )
	{
		bool[] newSteps = new bool[ _numSteps ];
		int i;
		
		for( i = 0; i < _numSteps; i++ )
		{
			if( i < prevSteps.Length )
			{
				newSteps[ i ] = prevSteps[ i ];
			}
			else
			{
				newSteps[ i ] = true;
			}
		}
		
		_basePulse.Steps = newSteps;
	}

	private float _floatFieldValue;
	protected void DrawPeriodSlider()
	{
		bool updateFloatField = false;

		GUILayout.BeginHorizontal();
		GUI.changed = false;
		_basePulse.PeriodDisplayUnit = ( PulseModule.PulseUnit )EditorGUILayout.EnumPopup( _basePulse.PeriodDisplayUnit, GUILayout.Width( 40f ) );
		if( GUI.changed || _floatFieldValue == 0f )
		{
			updateFloatField = true;
		}

		GUI.changed = false;

		switch( _basePulse.PeriodDisplayUnit )
		{

		case PulseModule.PulseUnit.BPM:
			float bpm = ( float )( 60d / _basePulse.Period );
			GUILayout.Label( bpm.ToString( "0.0" ), GUILayout.Width( 35f ) ); 


			_basePulse.Period =  ( double )( 60f / ( GUILayout.HorizontalSlider( bpm, 30f, 320f, GUILayout.Width( 200f ) ) ) );

			if( updateFloatField || GUI.changed )
			{
				_floatFieldValue = bpm;
			}
		
			_floatFieldValue = EditorGUILayout.FloatField( _floatFieldValue, GUILayout.Width( 50f ) );

			if( _floatFieldValue != bpm && Event.current.keyCode == KeyCode.Return )
			{
				_basePulse.Period =  ( double )( 60f / ( _floatFieldValue ) );
			}
			break;
			
		case PulseModule.PulseUnit.Sec:
			GUILayout.Label( _basePulse.Period.ToString( "0.00" ), GUILayout.Width( 35f ) ); 
			_basePulse.Period =  GUILayout.HorizontalSlider( ( float )_basePulse.Period, .1f, 4f, GUILayout.Width( 200f ) );

			if( updateFloatField || GUI.changed )
			{
				_floatFieldValue = ( float )_basePulse.Period;
			}
			
			_floatFieldValue = EditorGUILayout.FloatField( _floatFieldValue, GUILayout.Width( 50f ) );
			
			if( _floatFieldValue != _basePulse.Period && Event.current.keyCode == KeyCode.Return )
			{
				_basePulse.Period =  _floatFieldValue;
			}
			break;
			
		case PulseModule.PulseUnit.Ms:
			int ms = ( int )( _basePulse.Period * 1000 );
			GUILayout.Label( ms.ToString(), GUILayout.Width( 35f ) ); 
			_basePulse.Period =  ( double )( ( GUILayout.HorizontalSlider( ms, 5f, 125f, GUILayout.Width( 200f ) ) / 1000 ) );

			if( updateFloatField || GUI.changed )
			{
				_floatFieldValue = ( float )( _basePulse.Period * 1000 );
			}
			
			_floatFieldValue = EditorGUILayout.FloatField( _floatFieldValue, GUILayout.Width( 50f ) );
			
			if( _floatFieldValue != _basePulse.Period && Event.current.keyCode == KeyCode.Return )
			{
				_basePulse.Period =  ( double )_floatFieldValue / 1000;
			}

			break;
		}
		
		GUILayout.EndHorizontal();
	}
}
