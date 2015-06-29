using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

[ CustomEditor( typeof( SubPulseModule ) ) ]
public class SubPulseInspector : PulseBaseInspector 
{
	protected SubPulseModule _subPulse;

	protected override void OnEnable()
	{
		base.OnEnable();
		_subPulse = target as SubPulseModule;
	}

	void OnDisable()
	{
		if( _subPulse != null )
			EditorUtility.SetDirty( _subPulse );
	}

	public override void OnInspectorGUI()
	{
		int i;
		Rect boxRect;

//********************************************************************
//******************* Parent Pulse Box *******************************
		GUILayout.Space( 5f );

		boxRect = EditorGUILayout.BeginVertical();
		GUI.Box( boxRect, "Parent Pulse", __boxStyle );
		
		GUILayout.Space( 25f );

		GUILayout.BeginHorizontal();
		GUILayout.Space( 5f );
		_subPulse.ParentPulse = ( PulseModule )EditorGUILayout.ObjectField( _subPulse.ParentPulse, typeof( PulseModule ), true, GUILayout.Width( 150f ) );
		GUILayout.EndHorizontal();

		GUILayout.Space( 5f );

		if( _subPulse.ParentPulse != null )
		{
			//*********************************************************
			//**************** Parent Pulse Steps *********************
			bool[] subscribedSteps = _subPulse.SubscribedSteps;

			GUILayout.BeginHorizontal();
			GUILayout.Space( 80f );
			for( i = 0; i < subscribedSteps.Length; i++ )
			{
				GUI.enabled = _subPulse.ParentPulse.Steps[ i ];
				GUILayout.Label(  i.ToString(), GUILayout.Width( 15f ) );
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Space( 5f );
			GUILayout.Label( "Parent Steps:" , GUILayout.Width( 70f ) );
			
			for( i = 0; i < subscribedSteps.Length; i++ )
			{
				GUI.enabled = _subPulse.ParentPulse.Steps[ i ];
				subscribedSteps[ i ] = GUILayout.Toggle( subscribedSteps[i], "", GUILayout.Width( 15f ) );
			}
			GUILayout.EndHorizontal();

			GUILayout.Space( 5f );

			//*********************************************************
			//**************** Parent Pulse Bypass ********************
			GUI.enabled = true;

			GUILayout.BeginHorizontal();
			_subPulse.RandomBypassParentPulse = GUILayout.Toggle( _subPulse.RandomBypassParentPulse, "Random Bypass:", GUILayout.ExpandWidth( false ), GUILayout.Width( 110f ) );
			
			if( _subPulse.RandomBypassParentPulse )
			{
				GUILayout.Label( ( _subPulse.ParentPulseBypassChance * 100 ).ToString("0.##\\%"), GUILayout.Width( 40f ) );
				_subPulse.ParentPulseBypassChance = GUILayout.HorizontalSlider( _subPulse.ParentPulseBypassChance, 0f, 1f, GUILayout.Width( 100f ) );
			}
			GUILayout.EndHorizontal();

			GUILayout.Space( 5f );

			GUI.color = __purpleColor;

			if( _subPulse.RootPulse == null || _subPulse.RootPulse.IsPulsing == false )
			{
				GUI.enabled = false;
			}

			if( GUILayout.Button( "OneShot Next Step", __largeButtonOptions ) )
			{
				_subPulse.OneShotNextStep();
			}
			GUI.color = Color.white;
			GUI.enabled = true;

			GUILayout.Space( 5f );
		}
		else
		{
			GUILayout.Space( 30f );
		}

		GUILayout.EndVertical();

		//*********************************************************
		//**************** Common Pulse Properties ****************
		base.OnInspectorGUI();

		//*********************************************************
		//**************** Sub Pulse Mode *************************
		EditorGUIUtility.labelWidth = 60f;
		EditorGUIUtility.fieldWidth = 100f;
		_subPulse.SubPulseMode 		= ( SubPulseModule.PeriodMode )EditorGUILayout.EnumPopup( "Mode:", _subPulse.SubPulseMode, GUILayout.ExpandWidth( false ) );

		if( _subPulse.SubPulseMode == SubPulseModule.PeriodMode.AbsolutePeriod )
		{
			DrawPeriodSlider();
		}
		else if( _subPulse.SubPulseMode == SubPulseModule.PeriodMode.RatioOfParent )
		{
			string ratioString = string.Format( "Ratio:1/{0} ", _subPulse.RatioOfParentPeriod );
			_subPulse.RatioOfParentPeriod = EditorGUILayout.IntSlider( ratioString, _subPulse.RatioOfParentPeriod, 1, 16 );
		}
	}
}
