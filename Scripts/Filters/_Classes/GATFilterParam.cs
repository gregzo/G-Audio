using UnityEngine;
using System.Collections;
using System.Reflection;
using System;

namespace GAudio
{
	/// <summary>
	/// Helper class to easily control filter parameters.
	/// Reflection only occurs in the constructor: overhead
	/// when getting / setting the parameter is minimal.
	/// </summary>
	public class GATFilterParam {

		/// <summary>
		/// Name of the parameter property.
		/// </summary>
		public string ParamName{ get; private set; }

		/// <summary>
		/// Instance of the filter being tweaked.
		/// </summary>
		public AGATMonoFilter Filter{ get; private set; }

		/// <summary>
		/// Parameter value: uses a cached PropertyInfo object - 
		/// no reflection occurs when getting / setting the value.
		/// </summary>
		public float ParamValue
		{
			get
			{
				return ( float )_propInfo.GetValue( Filter, null );
			}
			set
			{
				_propInfo.SetValue( Filter, value, null );
			}
		}

		/// <summary>
		/// Initialize a filter parameter handler for the given filter.
		/// paramName should be the exact name of the property
		/// to tweak.
		/// </summary>
		public GATFilterParam( AGATMonoFilter filter, string paramName )
		{
			Type t = filter.GetType();
			
			_propInfo = t.GetProperty( paramName, BindingFlags.Public | BindingFlags.Instance );

			if( _propInfo == null )
			{
				throw new GATException( "No such filter!" );
			}
			
			Filter = filter;
		}

		/// <summary>
		/// Initialize a filter parameter handler for a track filter. 
		/// paramName should be the exact name of the property
		/// to tweak. If player is null, the default player is used instead.
		/// </summary>
		public GATFilterParam( int trackNb, int slotNb, string paramName, GATPlayer player = null )
		{
			Type t;
			GATTrack track;

			if( player == null )
				player = GATManager.DefaultPlayer;

			track  = player.GetTrack( trackNb );

			if( track == null )
			{
				throw new GATException( "Track " + trackNb + " does not exist." );
			}

			Filter = track.FiltersHandler.GetFilterAtSlot( slotNb );

			if( Filter == null )
			{
				throw new GATException( "No filter found in slot " + slotNb + " of track " + trackNb );
			}
			
			t = Filter.GetType();
			
			_propInfo = t.GetProperty( paramName, BindingFlags.Public | BindingFlags.Instance );

			if( _propInfo == null )
			{
				throw new GATException( "No such filter!" );
			}
		}

		/// <summary>
		/// Initialize a filter parameter handler for a player filter.
		/// If player is null, the default player is used instead.
		/// paramName should be the exact name of the property
		/// to tweak.
		/// </summary>
		public GATFilterParam( int slotNb, string paramName, GATPlayer player = null )
		{
			Type t;
			
			if( player == null )
				player = GATManager.DefaultPlayer;

			
			Filter = player.FiltersHandler.GetFilterAtSlot( slotNb );
			
			if( Filter == null )
			{
				throw new GATException( "No filter found in slot " + slotNb + " of player." );
			}
			
			t = Filter.GetType();
			
			_propInfo = t.GetProperty( paramName, BindingFlags.Public | BindingFlags.Instance );
			
			if( _propInfo == null )
			{
				throw new GATException( "No such filter!" );
			}
		}

		private PropertyInfo _propInfo;
	}
}
