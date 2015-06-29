//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Reflection;
using System;

/// <summary>
/// Base class for filters.
/// GATFilter objects may be added to
/// GATTrack and GATPlayer through their GATFiltersHandler.
/// They may also process GATData objects directly.
/// ( GATData.ApplyFilter( GATFilter filter ) )
/// </summary>
[ System.Serializable ]
public abstract class AGATFilter : ScriptableObject
{
	/// <summary>
	/// If more than one filter should be applied, slot index
	/// can be used to define filtering order.
	/// </summary>
	public int SlotIndex{ get{ return _slotIndex; } }
	[ SerializeField ]
	private int _slotIndex = -1;

	/// <summary>
	/// Clients of the filter should check this value
	/// before calling ProcessChunk(...)
	/// </summary>
	public bool Bypass
	{ 
		get{ return _bypass; }
		set
		{
			if( value == _bypass )
				return;

			if( !value )
			{
				ResetFilter();
			}

			_bypass = value;
		}
	}
	[ SerializeField ]
	protected bool _bypass;


	/// <summary>
	/// The interface type for this filters adjustable parameters.
	/// </summary>
	public abstract System.Type ControlInterfaceType{ get; }

	/// <summary>
	/// Initializes the filter.
	/// </summary>
	public virtual void InitFilter( int slotIndex )
	{
		_slotIndex = slotIndex;
	}

	/// <summary>
	/// Resets the filter. If the filter is stateless,
	/// may do nothing.
	/// </summary>
	public abstract void ResetFilter();

	/// <summary>
	/// Apply the filter.
	/// </summary>
	/// <returns><c>true</c>, if chunk was processed, <c>false</c> otherwise.</returns>
	/// <param name="data">Data.</param>
	/// <param name="fromIndex">From index.</param>
	/// <param name="length">Length.</param>
	/// <param name="emptyData"> If data is empty, the filter may still need to add to it( reverbs, for example ), or to reset their state.</param>
	public abstract bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData );

	/// <summary>
	/// Specifies the number of interleaved channels a single filter can
	/// process at once.
	/// </summary>
	public abstract int NbOfFilterableChannels{ get; }

	public FilterProperty[] GetFilterProperties()
	{
		int i;
		
		Type filterControlType 		= this.ControlInterfaceType;
		PropertyInfo[] properties 	= filterControlType.GetProperties( BindingFlags.Public | BindingFlags.Instance );
		
		FilterProperty[] filterProperties = new FilterProperty[ properties.Length ];
		
		for( i = 0; i < filterProperties.Length; i++ )
		{
			filterProperties[i] = new FilterProperty( properties[i], this );
		}
		
		return filterProperties;
	}

	public class FilterProperty
	{
		public float CurrentValue{ get{ return _currentValue; } }
		
		public string LabelString{ get{ return _labelString; } }
		
		public FloatPropertyRange Range{ get{ return _range; } }
		
		public bool IsGroupToggle{ get{ return _isGroupToggle; } }
		
		public bool GroupToggleState{ get; set; }
		
		PropertyInfo 		_info;
		FloatPropertyRange 	_range;
		float 				_currentValue;
		string 				_labelString;
		bool				_isGroupToggle;
		AGATFilter			_filter;
		
		public void SetValue( float val )
		{
			if( val != _currentValue )
			{
				_currentValue = val;
				_info.SetValue( _filter, val, null );
				_labelString = _info.Name + ": " + _currentValue.ToString( "0.00" ); 
			}
		}
		
		public void SetToggleValue( bool val )
		{
			if( val == GroupToggleState )
				return;
			
			_info.SetValue( _filter, val, null );
			GroupToggleState = val;
		}
		
		
		public FilterProperty( PropertyInfo info, AGATFilter filterInstance )
		{
			_info = info;
			
			object[] attributes = info.GetCustomAttributes( typeof( FloatPropertyRange ), true );
			
			if( attributes.Length == 0 )
			{
				attributes = info.GetCustomAttributes( typeof( ToggleGroupProperty ), true );
				if( attributes.Length == 1 )
				{
					_isGroupToggle = true;
				}
			}
			else
			{
				_range = ( FloatPropertyRange )attributes[ 0 ];
			}
			
			
			object val = info.GetValue( filterInstance, null );
			
			if( val is float )
			{
				_currentValue = ( float )val;
				_labelString = _info.Name + ": " + _currentValue.ToString("0.00"); 
			}
			else if( val is double )
			{
				_currentValue = ( float )( ( double )val ); 
				_labelString = _info.Name + ": " + _currentValue.ToString("0.00"); 
			}
			else if( val is bool )
			{
				GroupToggleState = ( bool )val;
				_labelString = _info.Name; 
			}

			_filter = filterInstance;
		}
	}
}
