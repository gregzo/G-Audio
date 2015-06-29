using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATDistortion
	{
		[ FloatPropertyRange( 0.001f, 1f ) ]
		float Threshold{ get; set; }
		[ FloatPropertyRange( 0f, 1f ) ]
		float Mix{ get; set; }
	}
	
	/// <summary>
	/// simple fold back distortion filterfound here:
	/// http://musicdsp.org/archive.php?classid=4#203
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATDistortion : AGATMonoFilter, IGATDistortion
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATDistortion ); } }
		
		public float Threshold
		{ 
			get
			{ 
				return _threshold; 
			} 
			set
			{
				if( value <= 0f )
				{
					_threshold = .001f;
				}
				else _threshold = value;
			}
		}
		[ SerializeField ]
		float _threshold = .1f;
		
		public float Mix
		{
			get
			{
				return _mix;
			}
			set
			{
				_mix = value;
			}
		}
		[ SerializeField ]
		float _mix = 1f;
		
		public override int NbOfFilterableChannels{ get{ return 999; } }//Any, filter is pure linear processing
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			if( emptyData )
				return false;
			
			length += fromIndex;
			int i;
			float input;
			
			for( i = fromIndex; i < length; i++ )
			{
				input = data[i];
				
				if ( input > Threshold || input < -Threshold )
				{
					data[i] = input * ( 1f - _mix ) + _mix * ( Mathf.Abs( Mathf.Abs( ( input - Threshold ) % ( Threshold * 4 ) ) - Threshold * 2 ) - Threshold );
				}
			}
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			length += fromIndex;
			int i;
			float input;
			
			for( i = fromIndex; i < length; i+= stride )
			{
				input = data[i];
				
				if ( input > Threshold || input < -Threshold )
				{
					data[i] = input * ( 1f - _mix ) + _mix * ( Mathf.Abs( Mathf.Abs( ( input - Threshold ) % ( Threshold * 4 ) ) - Threshold * 2 ) - Threshold );
				}
			}
		}
		
		public override void ResetFilter()
		{
			//Don't do anything, filter is passive
		}
		
		/// <summary>
		/// Not applicable: the filter is stateless and may filter any number of channels already.
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels )
		{
			throw new GATException( "Distortion does not need multi channel wrappers - it can be applied safely to interlaced audio data" );
		}
		
		static GATDistortion()
		{
			AGATMonoFilter.RegisterMonoFilter( "Distortion", typeof( GATDistortion ) );
		}
	}
}
