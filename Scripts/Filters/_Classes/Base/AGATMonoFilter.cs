//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base class for mono filters and stateless filters which are channel agnostic.
/// </summary>
public abstract class AGATMonoFilter : AGATFilter
{
	/// <summary>
	/// Overload which lets clients specify a stride to filter a single channel in an interleaved stream.
	/// </summary>
	public abstract void ProcessChunk( float[] data, int fromIndex, int length, int stride );

	/// <summary>
	/// If the filter can only filter a single channel, this method may be called to request a wrapper for multiple 
	/// mono filters to filter a multichannel audio stream. 
	/// </summary>
	public abstract AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels ) where T : AGATMonoFilter;

	protected static void RegisterMonoFilter( string filterName, Type filterType )
	{
		__filterTypesForNames.Add( filterName, filterType );
		__filterNamesForTypes.Add( filterType, filterName );

		if( __allFilterNames != null ) //Lazily constructed, if not null it has been accessed already in a not up to date state. 
			__allFilterNames = null; //force rebuild in getter method
	}

	static Dictionary< string, Type > __filterTypesForNames = new Dictionary< string, Type >(); 
	static Dictionary< Type, string > __filterNamesForTypes = new Dictionary< Type, string >(); 	
	static string[] __allFilterNames;

	/// <summary>
	/// Returns the name of a registered filter type.
	/// </summary>
	public static string FilterNameForType( Type t )
	{
		if( __filterNamesForTypes.ContainsKey( t ) == false )
			return null;

		return __filterNamesForTypes[ t ];
	}

	/// <summary>
	/// Returns the type for a registered filter name.
	/// </summary>
	public static Type FilterTyperForName( string filterName )
	{
		if( __filterTypesForNames.ContainsKey( filterName ) == false )
			return null;

		return __filterTypesForNames[ filterName ];
	}

	/// <summary>
	/// Gets all registers filters' names.
	/// </summary>
	public static string[] GetAllFilterNames() //Don't cache AllFilterNames: new filters might register
	{
		if( __allFilterNames == null )
		{
			__allFilterNames = new string[ __filterTypesForNames.Count ]; 
			__filterTypesForNames.Keys.CopyTo( __allFilterNames, 0 );
		}

		return __allFilterNames;
	}
}
