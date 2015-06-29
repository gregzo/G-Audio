using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio.Attributes;

namespace GAudio.Editor
{
	public abstract class BindedValueDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight( SerializedProperty prop, GUIContent label )
		{
			if( ( ( BindedValueProperty )attribute ).CheckToggle( prop.serializedObject.targetObject ) == false )
				return 0f;

			return EditorGUI.GetPropertyHeight( prop );
		}
		
		public override void OnGUI( Rect position, SerializedProperty prop, GUIContent label ) 
		{
			BindedValueProperty attr = attribute as BindedValueProperty;

			if( attr.CheckToggle( prop.serializedObject.targetObject ) == false )
				return;
			
			EditorGUI.BeginChangeCheck();
			
			DrawPropertyControl( position, prop, label );
			
			if( EditorGUI.EndChangeCheck() )
			{
				attr.SetValue( prop.serializedObject.targetObject, PropValue( prop ) );
				prop.serializedObject.Update();
			}
			else
			{
				UpdateOuterProperty( prop, attr );
			}
		}
		
		protected abstract void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr );
		
		protected abstract object PropValue( SerializedProperty prop );
		
		protected virtual void DrawPropertyControl( Rect position, SerializedProperty prop, GUIContent label )
		{
			EditorGUI.PropertyField( position, prop, label );
		}
	}
	
	[ CustomPropertyDrawer( typeof( BindedFloatProperty ) ) ]
	public class BindedFloatDrawer : BindedValueDrawer
	{
		protected override void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr )
		{
			float nestedValue = ( float )attr.GetValue( prop.serializedObject.targetObject );
			
			prop.floatValue = nestedValue;
		}
		
		protected override object PropValue( SerializedProperty prop )
		{
			return prop.floatValue;
		}
		
		/*protected override void DrawPropertyControl( Rect position, SerializedProperty prop, GUIContent label )
	{
		prop.floatValue = EditorGUI.FloatField( position, prop.floatValue );
	}*/
	}

	[ CustomPropertyDrawer( typeof( BindedBoolProperty ) ) ]
	public class BindedBoolDrawer : BindedValueDrawer
	{
		protected override void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr )
		{
			bool nestedValue = ( bool )attr.GetValue( prop.serializedObject.targetObject );
			
			prop.boolValue = nestedValue;
		}
		
		protected override object PropValue( SerializedProperty prop )
		{
			return prop.boolValue;
		}
	}

	[ CustomPropertyDrawer( typeof( BindedIntProperty ) ) ]
	public class BindedIntDrawer : BindedValueDrawer
	{
		protected override void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr )
		{
			int nestedValue = ( int )attr.GetValue( prop.serializedObject.targetObject );
			
			prop.intValue = nestedValue;
		}
		
		protected override object PropValue( SerializedProperty prop )
		{
			return prop.intValue;
		}
	}

	[ CustomPropertyDrawer( typeof( BindedDoubleProperty ) ) ]
	public class BindedDoubleDrawer : BindedValueDrawer
	{
		protected override void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr )
		{
			double nestedValue = ( double )attr.GetValue( prop.serializedObject.targetObject );
			
			prop.floatValue = ( float )nestedValue;
		}
		
		protected override object PropValue( SerializedProperty prop )
		{
			return prop.floatValue;
		}
	}

	[ CustomPropertyDrawer( typeof( BindedObjectProperty ) ) ]
	public class BindedObjectDrawer : BindedValueDrawer
	{
		protected override void UpdateOuterProperty( SerializedProperty prop, BindedValueProperty attr )
		{
			Object nestedValue = ( Object )attr.GetValue( prop.serializedObject.targetObject );
			
			prop.objectReferenceValue = nestedValue;
		}
		
		protected override object PropValue( SerializedProperty prop )
		{
			return prop.objectReferenceValue;
		}
	}
}
