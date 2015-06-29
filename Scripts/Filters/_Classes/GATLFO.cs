//--------------------------------
//   G-Audio: 2D Audio Framework
// Copyright © 2014 Gregorio Zanon
//--------------------------------

using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATLFO
	{
		[ FloatPropertyRange( .25f, 20f ) ]
		float Frequency{ get; set; }
		[ FloatPropertyRange( 0f, 1f ) ]
		float Strength{ get; set; } 
		
		void SetInitParams( float frequency, float strength );
	}
	
	/// <summary>
	/// A simple sine LFO for gain control.
	/// Must be initialized with SetInitParams.
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATLFO : AGATMonoFilter, IGATLFO
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATLFO ); } }
		#region private members
		[ SerializeField ]
		float _frequency = 4f;
		float _phase;
		[ SerializeField ]
		float _strength = .5f;
		float _phaseIncrement;
		#endregion
		
		/// <summary>
		/// Frequency in hz of the sine
		/// </summary>
		public float Frequency
		{
			get{ return _frequency; }
			set
			{
				_phaseIncrement = ( Mathf.PI * 2 * value ) / GATInfo.OutputSampleRate;
				_frequency = value;
			}
		}
		
		/// <summary>
		/// The attenuation of the processed signal:
		/// 1f is maximal( gain will oscillate between 1f and 0f )
		/// </summary>
		public float Strength
		{
			get{ return _strength; }
			set
			{
				_strength = value;
			}
		}
		
		public override int NbOfFilterableChannels{ get{ return 999; } }//GZNote: not implemented MultiChannelWrapper yet. LFO will be very strange on multichannel streams...
		
		public void SetInitParams( float frequency, float strength )
		{
			_strength = strength;
			Frequency = frequency;
		}
		
		protected virtual void OnEnable()
		{
			SetInitParams( _frequency, _strength );
		}
		
		public override void ProcessChunk( float[] data, int sourceIndex, int length, int stride )
		{
			length += sourceIndex;
			int i;
			float gain;
			
			for( i = sourceIndex; i < length; i+= stride )
			{
				gain = Mathf.Sin( _phase );
				gain = ( gain + 1f ) / 2;
				gain = gain * _strength + ( 1f - _strength );
				
				_phase += _phaseIncrement;
				
				if( _phase > Mathf.PI * 2 )
				{
					_phase -= Mathf.PI * 2;
				}
				
				data[i] *= gain;
			}
		}
		
		public override bool ProcessChunk( float[] data, int sourceIndex, int length, bool emptyData )
		{
			if( emptyData )
				return false;
			
			length += sourceIndex;
			int i;
			float gain;
			
			for( i = sourceIndex; i < length; i++ )
			{
				gain = Mathf.Sin( _phase );
				gain = ( gain + 1f ) / 2;
				gain = gain * _strength + ( 1f - _strength );
				
				_phase += _phaseIncrement;
				
				if( _phase > Mathf.PI * 2 )
				{
					_phase -= Mathf.PI * 2;
				}
				
				data[i] *= gain;
			}
			
			return true;
		}
		
		public override void ResetFilter()
		{
			_phase = 0f;
		}
		
		/// <summary>
		/// Not implemented yet.
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels )
		{
			throw new GATException( "Not implemented yet" );
		}
		
		static GATLFO()
		{
			AGATMonoFilter.RegisterMonoFilter( "LFO", typeof( GATLFO ) );
		}
	}
}


