using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace GAudio.Attributes
{
	public abstract class BindedValueProperty : PropertyAttribute
	{
		protected MemberInfo[] 	_memberInfos;
		protected bool[]     	_fieldFlags;
		protected string[] 	 	_pathComponents;
		protected FieldInfo     _toggleInfo;
		
		public BindedValueProperty( string propertyPath, Type outerType, string toggleField = null )
		{
			_pathComponents = propertyPath.Split( '.' );
			int i;
			PropertyInfo propInfo;
			FieldInfo    fieldInfo;
			
			_memberInfos = new MemberInfo[ _pathComponents.Length ];
			_fieldFlags  = new bool[ _pathComponents.Length ];

			if( toggleField != null )
			{
				_toggleInfo = outerType.GetField( toggleField, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
			}
			
			
			for( i = 0; i < _pathComponents.Length; i++ )
			{
				propInfo = outerType.GetProperty( _pathComponents[ i ], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
				
				if( propInfo == null )
				{
					fieldInfo = outerType.GetField( _pathComponents[ i ], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance );
					_fieldFlags[ i ] = true;
					_memberInfos[ i ] = fieldInfo;
					
					outerType = fieldInfo.FieldType;
				}
				else
				{
					_memberInfos[ i ] = propInfo;
					outerType = propInfo.PropertyType;
				}
			}
		}
		
		public virtual void SetValue( object owner, object value )
		{
			object o = GetTargetObj( owner );
			
			if( _fieldFlags[ _fieldFlags.Length - 1 ] )
			{
				( ( FieldInfo )_memberInfos[ _memberInfos.Length - 1 ] ).SetValue( o, value );
			}
			else
			{
				( ( PropertyInfo )_memberInfos[ _memberInfos.Length - 1 ] ).SetValue( o, value, null );
			}
		}
		
		public object GetValue( object owner )
		{
			object o = GetTargetObj( owner );
			
			if( _fieldFlags[ _fieldFlags.Length - 1 ] )
			{
				return ( ( FieldInfo )_memberInfos[ _memberInfos.Length - 1 ] ).GetValue( o );
			}
			else
			{
				return ( ( PropertyInfo )_memberInfos[ _memberInfos.Length - 1 ] ).GetValue( o, null );
			}
		}

		public object GetTargetObj( object outerObj )
		{
			int i;
			for( i = 0; i < _memberInfos.Length - 1; i++ )
			{
				if( _fieldFlags[ i ] )
				{
					outerObj = ( ( FieldInfo )_memberInfos[ i ] ).GetValue( outerObj );
				}
				else
				{
					outerObj = ( ( PropertyInfo )_memberInfos[ i ] ).GetValue( outerObj, null );
				}
			}
			
			return outerObj;
		}

		public bool CheckToggle( object owner )
		{
			if( _toggleInfo == null )
				return true;

			bool toggleValue = ( bool )_toggleInfo.GetValue( owner );

			return toggleValue;
		}
	}

	public class BindedBoolProperty : BindedValueProperty
	{
		public BindedBoolProperty( string propertyPath, Type outerType, string toggleName = null ) : base( propertyPath, outerType, toggleName )
		{
			
		}
	}

	public class BindedIntProperty : BindedValueProperty
	{
		public BindedIntProperty( string propertyPath, Type outerType, string toggleName = null ) : base( propertyPath, outerType, toggleName )
		{
			
		}
	}

	public class BindedFloatProperty : BindedValueProperty
	{
		public BindedFloatProperty( string propertyPath, Type outerType, string toggleName = null ) : base( propertyPath, outerType, toggleName )
		{
			
		}
	}

	public class BindedDoubleProperty : BindedValueProperty
	{
		public BindedDoubleProperty( string propertyPath, Type outerType, string toggleName = null ) : base( propertyPath, outerType, toggleName )
		{
			
		}

		public override void SetValue( object owner, object value )
		{
			object o    = GetTargetObj( owner );
			float val   = ( float )value;
			double dVal = ( double )val; // must cast as inspector will set floats. ( no doubleValue on SerializedProperty...)
			
			if( _fieldFlags[ _fieldFlags.Length - 1 ] )
			{
				( ( FieldInfo )_memberInfos[ _memberInfos.Length - 1 ] ).SetValue( o, dVal );
			}
			else
			{
				( ( PropertyInfo )_memberInfos[ _memberInfos.Length - 1 ] ).SetValue( o, dVal, null );
			}
		}
	}

	public class BindedObjectProperty : BindedValueProperty
	{
		public BindedObjectProperty( string propertyPath, Type outerType, string toggleName = null ) : base( propertyPath, outerType, toggleName )
		{
			
		}
	}
}
