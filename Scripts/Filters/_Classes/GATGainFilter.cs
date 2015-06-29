using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATGainFilter
	{
		[ FloatPropertyRange( 0f, 5f ) ]
		float          Gain{ get; set; }
		
		[ ToggleGroupProperty( 1 ) ]
		bool           Clip{ get; set; }
		
		[ FloatPropertyRange( 0f, 1f ) ]
		float Threshold{ get; set; }
	}
	
	/// <summary>
	/// Gain and clip filter with adjustable clipping threshold
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATGainFilter : AGATMonoFilter, IGATGainFilter
	{
		static GATGainFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "Gain and Clip", typeof( GATGainFilter ) );
		}
		
		public float Gain{ get{ return _gain; } set{ _gain = value; } }
		public bool Clip
		{ 
			get{ return _clip; } 
			set
			{ 
				if( value == _clip )
					return;
				
				_clip = value;
				if( !value )
					_nbOfClippedSamples = 0;
			} 
		}
		public float Threshold
		{ 
			get{ return _clipThreshold; } 
			set
			{ 
				if( value == _clipThreshold )
					return;
				
				_clipThreshold = value;
				_negThreshold = -value;
			} 
		}
		
		public int NbOfClippedSamples{ get{ return _nbOfClippedSamples; } }
		
		[ SerializeField ]
		protected float _gain = 1f;
		[ SerializeField ]
		protected bool  _clip = false;
		[ SerializeField ]
		protected float _clipThreshold = 1f;
		
		protected int _nbOfClippedSamples;
		
		protected float _negThreshold;
		
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATGainFilter ); } }
		
		public override void ResetFilter()
		{
			_nbOfClippedSamples = 0;
		}
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			if( emptyData )
				return false;
			
			int i;
			int clipped = 0;
			
			if( _gain != 1f )
			{
				for( i = fromIndex; i < data.Length; i ++ )
				{
					data[ i ] *= _gain;
				}
			}
			
			if( _clip )
			{
				for( i = fromIndex; i < data.Length; i ++ )
				{
					if( data[ i ] > _clipThreshold )
					{
						data[ i ] = _clipThreshold;
						clipped ++;
					}
					else if( data[ i ] < _negThreshold )
					{
						data[ i ] = _negThreshold;
						clipped ++;
					}
				}
				
				_nbOfClippedSamples = clipped;
			}
			
			return true;
		}
		
		/// <summary>
		/// Not implemented.
		/// </summary>
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			throw new GATException("Not implemented" );
		}
		
		/// <summary>
		/// Not applicable: the filter is stateless and may filter any number of channels already.
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels ) 
		{
			throw new GATException("Not implemented and not needed" );
		}
		
		protected virtual void OnEnable()
		{
			_negThreshold = -_clipThreshold;
		}
		
		public override int NbOfFilterableChannels{ get{ return 1000; } }
	}
}

