using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

[ CustomEditor( typeof( MasterPulseModule ) ) ]
public class MasterPulseInspector : PulseBaseInspector 
{

	protected MasterPulseModule _masterPulse;

	protected override void OnEnable()
	{
		base.OnEnable();
		_masterPulse = target as MasterPulseModule;
	}

	void OnDisable()
	{
		if( _masterPulse != null )
			EditorUtility.SetDirty( _masterPulse );
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		DrawPeriodSlider();

		if( _masterPulse.IsPulsing == false )
		{
			GUI.color = Color.green;
			if( GUILayout.Button( "Start", __buttonOptions ) )
			{
				_masterPulse.StartPulsing( 0 );
			}
		}
		else
		{
			GUI.color = Color.red;
			if( GUILayout.Button( "Stop", __buttonOptions ) )
			{
				_masterPulse.Stop();
			}
		}
		
		GUI.color = Color.white;
		
		GUILayout.BeginHorizontal();

		_masterPulse.StartPulseAuto = GUILayout.Toggle( _masterPulse.StartPulseAuto, "Auto Start Pulse", GUILayout.Width( 110f ) );

		if( _masterPulse.StartPulseAuto )
		{
			EditorGUIUtility.labelWidth = 40f;
			EditorGUIUtility.fieldWidth = 35f;
			_masterPulse.StartDelay = ( double )EditorGUILayout.FloatField( "Delay:", ( float )_masterPulse.StartDelay, GUILayout.ExpandWidth( false ) );
		}

		GUILayout.EndHorizontal();
	}
}
