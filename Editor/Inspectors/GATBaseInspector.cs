using UnityEngine;
using System.Collections;
using UnityEditor;

public class GATBaseInspector : Editor 
{
	protected static GUILayoutOption[] __largeButtonOptions 	= new GUILayoutOption[]{ GUILayout.Width( 130f ), GUILayout.ExpandWidth( false ) };
	protected static GUILayoutOption[] __buttonOptions 			= new GUILayoutOption[]{ GUILayout.Width( 70f  ), GUILayout.ExpandWidth( false ) };
	protected static GUILayoutOption[] __sliderOptions 			= new GUILayoutOption[]{ GUILayout.Width( 300f ), GUILayout.ExpandWidth( false ) };
	protected static GUILayoutOption[] __closeTogglesOptions 	= new GUILayoutOption[]{ GUILayout.Width( 20f  ), GUILayout.ExpandWidth( false ) };
	protected static GUILayoutOption[] __closeButtonsOptions 	= new GUILayoutOption[]{ GUILayout.Width( 20f  ), GUILayout.ExpandWidth( false ) };

	protected static GUIStyle __boxStyle;
	protected static GUIStyle __xButtonStyle;

	protected static Color __blueColor 		= new Color( .7f, .7f, 1f );
	protected static Color __purpleColor 	= new Color( .8f, .6f, 1f ); 

	public override void OnInspectorGUI()
	{
		if( __boxStyle == null )
		{
			__boxStyle = new GUIStyle( GUI.skin.box );
			__boxStyle.fontSize = 15;
			__boxStyle.fontStyle = FontStyle.Bold;
			
			__xButtonStyle = new GUIStyle( GUI.skin.button );
			__xButtonStyle.normal.textColor = Color.white;
		}
	}

	protected float ResettableSlider( string valString, float val, float min, float max, float resetValue )
	{
		float ret;

		EditorGUILayout.BeginHorizontal();

		valString = ( valString + val.ToString( "0.00" ) );

		if( GUILayout.Button( valString, __buttonOptions ) )
		{
			ret = resetValue;
			val = resetValue;
		}

		GUILayout.Space( 30f );

		ret = GUILayout.HorizontalSlider( val , min, max, __sliderOptions );

		EditorGUILayout.EndHorizontal();

		return ret;
	}

}
