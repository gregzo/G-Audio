using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using GAudio;
using GAudio.Editor;

[ CustomEditor( typeof( GATPlayer) ) ]
[ InitializeOnLoad ]
public class GATPlayerInspector : GATBaseInspector, IGATAudioThreadStreamClient
{
	GATPlayer _player;
	
	float[] _trackLevels;
	float[] _playerChannelsLevels;

	IGATAudioThreadStream[] _trackStreams;
	IGATAudioThreadStream   _playerStream;

	TrackFiltersInfo[] _trackFiltersInfo;
	TrackFiltersInfo   _playerFiltersInfo;

	volatile bool _shouldRepaint;

	const float SPACE_BETWEEN_TRACKS = 5f;
	
	void OnEnable()
	{
		_player = target as GATPlayer;
		SetupTracksInfo();
	}

	void OnDisable()
	{
		UnregisterFromStreams();
	
		if( _player != null )
			EditorUtility.SetDirty( _player );
	}
	
	void UnregisterFromStreams()
	{
		if( _trackStreams == null )
			return;
		
		foreach( IGATAudioThreadStream stream in _trackStreams )
		{
			if( stream != null )
			{
				stream.RemoveAudioThreadStreamClient( this );
			}
		}
	}

	void SetupTracksInfo()
	{
		GATTrack track;
		int nbOfTracks;

		nbOfTracks 			= _player.NbOfTracks;
		_trackStreams 		= new IGATAudioThreadStream[ nbOfTracks ];
		_trackLevels  		= new float[ nbOfTracks ];
		_trackFiltersInfo	= new TrackFiltersInfo[ nbOfTracks ];
		
		for( int i = 0; i < nbOfTracks; i++ )
		{
			track = _player.GetTrack( i );

			_trackStreams[ i ] = track.GetAudioThreadStream( 0 );

			if( _trackStreams[ i ] != null )
			{
				_trackStreams[ i ].AddAudioThreadStreamClient( this );
			}
			
			if( track != null )
			{
				_trackFiltersInfo[ i ] = new TrackFiltersInfo( track.FiltersHandler );
			}
		}

		_playerStream 			= ( ( IGATAudioThreadStreamOwner )_player ).GetAudioThreadStream( 0 );
		_playerChannelsLevels 	= new float[ GATInfo.NbOfChannels ];
		_playerFiltersInfo 		= new TrackFiltersInfo( _player.FiltersHandler );

		if( _playerStream != null )
		{
			_playerStream.AddAudioThreadStreamClient( this );
		}
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		int   i;
		Rect  boxRect;
		float level;

		GUILayout.Space( 10f );

		#if GAT_DEBUG
		GUILayout.Label( _player.audio.isPlaying ? "Playing" : "Stopped" );
		#endif

//***********************************************************************
//************************* Master Box **********************************
		GUI.color = Color.white;
		boxRect = EditorGUILayout.BeginVertical();
		GUI.Box( boxRect, "Master", __boxStyle );

		GUILayout.Space( 25f );

		_player.Gain = ResettableSlider( "Gain: ", _player.Gain, 0f, 2f, 1f );
		_player.Clip = GUILayout.Toggle( _player.Clip, "Clip Mix" );

		if( _player.Clip )
		{
			_player.ClipThreshold = ResettableSlider( "Max: ", _player.ClipThreshold, 0f, 1f, 1f );
		}

		GUILayout.Space( 10f );

		level = 0f;

		//******************** Master Levels ****************************
		for( i = 0; i < GATInfo.NbOfChannels; i++ )
		{
			if( _playerStream != null )
			{
				level = _playerChannelsLevels[i];
			}
			Rect r = EditorGUILayout.BeginVertical( );
			EditorGUI.ProgressBar( new Rect( r.x + 10f, r.y, r.width - 20f, r.height ),level, "Channel " + i.ToString() );
			GUILayout.Space(16);
			EditorGUILayout.EndVertical();
		}

		GUILayout.Space( 10f );
		EditorGUILayout.EndVertical();

		//******************** Master Filters ***************************
		_playerFiltersInfo.DrawFiltersToolbar();

		GUILayout.Space( 30f );

//***********************************************************************
//*************************** Tracks ************************************

		GATTrack track;
		int nbOfTracks;
		Rect deleteButtonRect;

		nbOfTracks = _player.NbOfTracks;

		for( i = 0; i < nbOfTracks; i++ )
		{
			track = _player.GetTrack( i );

			//************** Track Box **********************************
			boxRect = EditorGUILayout.BeginVertical();

			GUI.Box( boxRect, "Track " + track.TrackNb, __boxStyle );

			//************** Delete Button ******************************
			deleteButtonRect = new Rect( boxRect.x + 3f, boxRect.y + 3f, 20f, 20f );
		
			GUI.backgroundColor = Color.black;
			if( GUI.Button( deleteButtonRect, "x", __xButtonStyle ) )
			{
				UnregisterFromStreams();
				_player.DeleteTrack( track );
				SetupTracksInfo();
				return;
			}
			GUI.backgroundColor = Color.white;

			GUILayout.Space( 30f );

			//************** Gain and Pan Controls **********************
			if( GATInfo.NbOfChannels == 2 && track.ForcePerChannelControl == false )
			{
				track.StereoGain = ResettableSlider( "Gain: ", track.StereoGain, 0f, 2f, 1f );
				EditorGUILayout.Space();
				track.StereoPan = ResettableSlider( "Pan: ", track.StereoPan, 0f, 1f, .5f );
				EditorGUILayout.Space();
			}
			else
			{
				for( int j = 0; j < GATInfo.NbOfChannels; j++ )
				{
					track.SetGainForChannel( ResettableSlider( "ch" + j + ": ", track.GetGainForChannel( j ), 0f, 2f, .5f ), j );
					EditorGUILayout.Space();
				}
			}

			GUILayout.BeginHorizontal();

			track.Mute = GUILayout.Toggle( track.Mute, "Mute" );

			if( GATInfo.NbOfChannels == 2 )
			{
				track.ForcePerChannelControl = GUILayout.Toggle( track.ForcePerChannelControl, "Per Channel Control" );
			}

			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			GUI.enabled = !track.Mute;

			//******************** Track Level ************************
			Rect r = EditorGUILayout.BeginVertical( );
			EditorGUI.ProgressBar( new Rect( r.x + 10f, r.y, r.width - 20f, r.height ), _trackLevels[i], "Pre-mix level" );
			GUILayout.Space(20);
			EditorGUILayout.EndVertical();

			GUILayout.Space( 15f );

			EditorGUILayout.EndVertical();

			//******************** Track Filters *********************
			GUI.enabled = true;
			_trackFiltersInfo[ i ].DrawFiltersToolbar();

			GUILayout.Space( SPACE_BETWEEN_TRACKS );
		}

		GUILayout.Space( 10f );

		if( GUILayout.Button( "AddTrack" ) )
		{
			UnregisterFromStreams();
			_player.AddTrack < GATTrack >();
			SetupTracksInfo();
		}

		GUILayout.Space( 10f );
		
		if( _shouldRepaint )
		{
			Repaint();
		}
	}

	void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
	{
		int trackIndex = -1;
		int i;

		if( stream == _playerStream )
		{
			for( i = 0; i < _playerChannelsLevels.Length; i++ )
			{
				_playerChannelsLevels[ i ] = GATMaths.GetAbsMaxValueFromInterleaved( data, offset, stream.BufferSizePerChannel * stream.NbOfChannels, i, stream.NbOfChannels );
			}
			_shouldRepaint = true;
			return;
		}
		
		for( i = 0; i < _trackStreams.Length; i++ )
		{
			if( stream == _trackStreams[ i ] )
			{
				trackIndex = i;
				break;
			}
		}
		
		if( trackIndex == -1 )
			return;
		
		if( emptyData )
		{
			_trackLevels[ trackIndex ] = 0f;
			_shouldRepaint = true;
			return;
		}
		
		_trackLevels[ trackIndex ] = GATMaths.GetAbsMaxValue( data, offset, GATInfo.AudioBufferSizePerChannel );
		_shouldRepaint = true;
	}
	
	class TrackFiltersInfo
	{
		const int MONOFILTERS_COUNT   = 4;

		int 			        _selectedFilterIndexInOptions;
		
		AGATFilter[] 			_filters;
		AGATFilter 				_selectedFilter;

		AGATFilter.FilterProperty[] _selectedFilterProperties;

		string[]	 			_filterNames;
		
		GATFiltersHandler 		_handler;

		static GUILayoutOption[] __filtersButtonOptions = new GUILayoutOption[]{ GUILayout.Width( 90f  ), GUILayout.Height( 15f ), GUILayout.ExpandWidth( false ) };
		static GUILayoutOption[] __filtersTypeOptions 	= new GUILayoutOption[]{ GUILayout.Width( 110f ), GUILayout.ExpandWidth( false ) };
		static GUILayoutOption[] __filtersLabelOptions 	= new GUILayoutOption[]{ GUILayout.Width( 100f ), GUILayout.ExpandWidth( false ) };

		static Color filterBgdColor = new Color( .96f, .96f, 96f );

		public void DrawFiltersToolbar()
		{
			int  i;
			int  slot;
			Rect boxRect;
			string[] allFilterNames;

			boxRect = EditorGUILayout.BeginVertical();

			GUI.backgroundColor = filterBgdColor;

			GUI.Box( boxRect, "" );

			GUI.backgroundColor = Color.white;

			EditorGUILayout.Space();

			_handler.SelectedFilterSlot = GUILayout.Toolbar( _handler.SelectedFilterSlot, _filterNames );

			slot = _handler.SelectedFilterSlot;

			EditorGUILayout.Space();
			
			if( _filters[ slot ] != null )
			{
				GUI.color = Color.red;

				if( GUILayout.Button( "Remove Filter", __filtersButtonOptions ) )
				{
					RemoveFilter( slot );

				}

				GUI.color = Color.white;

				if( _selectedFilter != _filters[ slot ] )
				{
					_selectedFilter = _filters[ slot ];
		
					_selectedFilterProperties = _selectedFilter.GetFilterProperties();
				}

				if( _selectedFilter != null )
				{
					AGATFilter.FilterProperty filterProp;

					GUI.enabled = ! _selectedFilter.Bypass;
					for( i = 0; i < _selectedFilterProperties.Length; i++ )
					{
						filterProp = _selectedFilterProperties[ i ];
						if( filterProp.IsGroupToggle )
						{
							filterProp.SetToggleValue( GUILayout.Toggle( filterProp.GroupToggleState, filterProp.LabelString ) );
							if( filterProp.GroupToggleState == false )
							{
								i++;
							}

							continue;
						}
						GUILayout.BeginHorizontal();
						GUILayout.Label( filterProp.LabelString, __filtersLabelOptions );
						filterProp.SetValue( GUILayout.HorizontalSlider( filterProp.CurrentValue, filterProp.Range.Min, filterProp.Range.Max, __sliderOptions ) );
						GUILayout.EndHorizontal();
					}

					GUI.enabled = true;

					_selectedFilter.Bypass = GUILayout.Toggle( _selectedFilter.Bypass, "Bypass" );
				}
			}
			else
			{
				allFilterNames = AGATMonoFilter.GetAllFilterNames();
				EditorGUILayout.BeginHorizontal();

				_selectedFilterIndexInOptions = EditorGUILayout.Popup( _selectedFilterIndexInOptions, allFilterNames, __filtersTypeOptions );
				
				GUI.color = Color.green;
				if( GUILayout.Button( "Add Filter", __filtersButtonOptions ) )
				{
					Type filterType = AGATMonoFilter.FilterTyperForName( allFilterNames[ _selectedFilterIndexInOptions ] );
					AddFilter( filterType, slot );
				}

				GUI.color = Color.white;
				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

		}
		
		public TrackFiltersInfo( GATFiltersHandler handler )
		{
			int i;
			AGATMonoFilter monoFilter;

			_filters 			= new AGATFilter[ MONOFILTERS_COUNT ];
			_filterNames 		= new string[ MONOFILTERS_COUNT ];
			_handler 			= handler;

			for( i = 0; i < MONOFILTERS_COUNT; i++ )
			{
				monoFilter = handler.GetFilterAtSlot( i );

				if( monoFilter != null )
				{
					_filters[ i ] 		= monoFilter;
					_filterNames[ i ] 	= AGATMonoFilter.FilterNameForType( monoFilter.GetType() );
				}
				else
				{
					_filterNames[ i ] = "Slot "+i;
				}
			}
		}

		static MethodInfo __addFilterMethod;
		static TrackFiltersInfo()
		{
			__addFilterMethod = typeof( GATFiltersHandler ).GetMethod( "AddFilter" );
		}
		
		/*void AddFilter( GATFilterType type, int slot )
		{
			switch( type )
			{
				case GATFilterType.Distortion:
				_filters[ slot ] = _handler.AddFilter < GATDistortion >( slot );
				break;

				case GATFilterType.LFO:
				_filters[ slot ] = _handler.AddFilter < GATLFO >( slot );
				break;
				
				case GATFilterType.LowPass:
				_filters[ slot ] = _handler.AddFilter < GATLowPassFilter >( slot );
				break;

				case GATFilterType.HighPass:
				_filters[ slot ] = _handler.AddFilter < GATHighPassFilter >( slot );
				break;

				case GATFilterType.Notch:
				_filters[ slot ] = _handler.AddFilter < GATNotchFilter >( slot );
				break;

				case GATFilterType.Peak:
				_filters[ slot ] = _handler.AddFilter < GATPeakFilter >( slot );
				break;

				case GATFilterType.LowShelf:
				_filters[ slot ] = _handler.AddFilter < GATLowShelfFilter >( slot );
				break;

				case GATFilterType.HighShelf:
				_filters[ slot ] = _handler.AddFilter < GATHighShelfFilter >( slot );
				break;

				case GATFilterType.GainAndClip:
				_filters[ slot ] = _handler.AddFilter < GATGainFilter >( slot );
				break;
			}
			
			_filterTypes[ slot ] = type;
			_filterNames[ slot ] = type.ToString();
		}*/

		void AddFilter( System.Type type, int slot )
		{
			MethodInfo generic	 = __addFilterMethod.MakeGenericMethod( type );
			_filters[ slot ] 	 = ( AGATFilter )generic.Invoke( _handler, new object[]{ slot } ); 
			_filterNames[ slot ] = AGATMonoFilter.FilterNameForType( type ); 
		}
		
		void RemoveFilter( int slot )
		{
			_handler.RemoveFilterAtSlot( slot );

			_filters[ slot ] 		= null;
			_filterNames[ slot ] 	= "Slot " + slot;
		}
	}
}
