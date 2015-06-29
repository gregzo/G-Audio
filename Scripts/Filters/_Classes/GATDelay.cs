using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATDelay
	{
		[ FloatPropertyRange( 0.001f, 1f ) ]
		float Delay{ get; set; }
		[ FloatPropertyRange( 0f, 1f ) ]
		float Feedback{ get; set; }
	}
	
	/// <summary>
	/// simple fold back distortion filterfound here:
	/// http://musicdsp.org/archive.php?classid=4#203
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATDelay : AGATMonoFilter, IGATDelay
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATDelay ); } }
		
		public float Delay
		{ 
			get
			{ 
				return _delay; 
			} 
			set
			{
				if( value <= 0f )
				{
					_delay = .001f;
				}
				else _delay = value;

				_delaySamples = ( double )_delay * GATInfo.OutputSampleRate;
			}
		}
		[ SerializeField ]
		float _delay = 1f;

		double _delaySamples;
		
		public float Feedback
		{
			get
			{
				return _feedback;
			}
			set
			{
				_feedback = value;
			}
		}
		[ SerializeField ]
		float _feedback = .5f;

		int _counter;

		float[] _buffer;

		static int MAX_DELAY_SAMPLES;

		protected virtual void OnEnable()
		{
			Debug.Log( "Delay OnEnable" );
			if( MAX_DELAY_SAMPLES == 0 )
				MAX_DELAY_SAMPLES = GATInfo.OutputSampleRate * 1;

			_buffer = new float[ MAX_DELAY_SAMPLES ]; 
			Delay = _delay;
		}
		
		public override int NbOfFilterableChannels{ get{ return 999; } }//Any, filter is pure linear processing
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			int counter = _counter;
			int i;

			double 	back;
			int    	index_1,
					index0,
					index1,
					index2;

			float   y_1,
					y0,
					y1,
					y2;

			float	x;

			float   c0,
					c1,
					c2,
					c3;

			float output;

			double delaySamples = _delaySamples;

			length += fromIndex;

			for( i = fromIndex; i < length; i++ )
			{
				// calculate delay offset
				back = ( double )counter - delaySamples;
				
				// clip lookback buffer-bound
				if( back < 0.0 )
					back = ( double )MAX_DELAY_SAMPLES + back;
				
				// compute interpolation left-floor
				index0 = ( int )back;
				
				// compute interpolation right-floor
				index_1 = index0 - 1;
				index1  = index0 + 1;
				index2  = index0 + 2;
				
				// clip interp. buffer-bound
				if( index_1 < 0 ) 
					index_1 = MAX_DELAY_SAMPLES - 1;

				if( index1 >= MAX_DELAY_SAMPLES ) 
					index1 = 0;

				if( index2 >= MAX_DELAY_SAMPLES )
					index2 = 0;
				
				// get neighbourgh samples
				y_1	= _buffer[ index_1 ];
				y0 	= _buffer[ index0 ];
				y1 	= _buffer[ index1 ];
				y2 	= _buffer[ index2 ];
				
				// compute interpolation x
				x 	= ( float )back - ( float )index0;
				
				// calculate
				c0 	= y0;
				c1	= 0.5f * ( y1 - y_1 );
				c2 	= y_1 - ( 2.5f * y0 ) + ( 2.0f * y1 ) - ( 0.5f * y2 );
				c3 	= 0.5f * ( y2 - y_1 ) + 1.5f * ( y0 - y1 );
				
				output = ( ( c3 * x + c2 ) * x + c1 ) * x + c0;
				
				// add to delay buffer
				_buffer[ counter ] = data[ i ] + output * _feedback;
				
				// increment delay counter
				counter++;
				
				// clip delay counter
				if( counter >= MAX_DELAY_SAMPLES )
					counter = 0;

				data[ i ] = output;
			}

			_counter = counter;
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{

		}
		
		public override void ResetFilter()
		{
			System.Array.Clear( _buffer, 0, _buffer.Length );
			_counter = 0;
		}
		
		/// <summary>
		/// Not applicable: the filter is stateless and may filter any number of channels already.
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels )
		{
			throw new GATException( "Distortion does not need multi channel wrappers - it can be applied safely to interlaced audio data" );
		}
		
		static GATDelay()
		{
			AGATMonoFilter.RegisterMonoFilter( "Delay", typeof( GATDelay ) );
		}
	}
}
