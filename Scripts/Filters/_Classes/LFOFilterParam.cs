using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using GAudio.Attributes;

namespace GAudio
{
	/// <summary>
	/// Simple Monobehaviour component for LFOing 
	/// filter parameters. Uses GATFilterParam to
	/// conveniently tweak the requested parameter
	/// of a track or master filter. 
	/// 
	/// Also demonstrates use of custom attributes
	/// to get the inspector to display properties
	/// instead of fields, and to display a property
	/// only if a toggle is true.
	/// </summary>
	public class LFOFilterParam : MonoBehaviour {
		
		/// <summary>
		/// The GATPlayer to grab the filter from.
		/// If null, the default player will be used.
		/// </summary>
		public GATPlayer player;

		public bool isTrackFilter = true;

		// *** binded property, only displayed if isTrackFilter is true.
			/// <summary>
			/// The track nb.
			/// </summary>
			public int TrackNb
			{
				get{ return _trackNb; }
				set
				{
					_trackNb = value;
				}
			}

			[ SerializeField ]
			[ BindedIntProperty( "TrackNb", typeof( LFOFilterParam ), "isTrackFilter" ) ]
			private int _trackNb;
		// ***

		public int filterSlot;

		public string paramName;
		
		public float min, max;

		// *** binded property 
			public float Frequency
			{
				get{ return _frequency; }
				set
				{
					_phaseIncrement = ( Mathf.PI * 2 * value ) / GATInfo.OutputSampleRate;
					_frequency = value;
				}
			}
			[ SerializeField ]
			[ BindedFloatProperty( "Frequency", typeof( LFOFilterParam ) ) ]
			float _frequency = 1f;
		// ***


		GATFilterParam _filterParam;
		float _phase;
		float _phaseIncrement;
		
		void OnEnable () 
		{
			Frequency = _frequency;
			
			if( player == null )
				player = GATManager.DefaultPlayer;

			if( isTrackFilter )
			{
				_filterParam = new GATFilterParam( _trackNb, filterSlot, paramName, player );
			}
			else
			{
				_filterParam = new GATFilterParam( filterSlot, paramName, player );
			}
			
			player.onPlayerWillMix += OnPlayerWillMix;
		}
		
		void OnDisable()
		{
			player.onPlayerWillMix -= OnPlayerWillMix;
		}
		
		void OnPlayerWillMix()
		{
			float lerpVal = ( Mathf.Sin( _phase ) + 1f ) / 2;
			
			float paramVal = Mathf.Lerp( min, max, lerpVal );
			
			_filterParam.ParamValue = paramVal;
			
			_phase += _phaseIncrement * GATInfo.AudioBufferSizePerChannel;
			
			if( _phase > Mathf.PI * 2 )
			{
				_phase -= Mathf.PI * 2;
			}
		}
	}
}

