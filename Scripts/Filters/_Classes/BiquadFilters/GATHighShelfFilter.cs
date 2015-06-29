using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// High Shelf biquad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATHighShelfFilter : AGATBiQuadPeak
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			if( _peakGain >= 0 ) // boost
			{    
				norm = 1d / ( 1 + GATMaths.SQRT2 * _K + _KSq );
				_a0 = ( _V + _sqrt2V * _K + _KSq ) * norm;
				_a1 = 2 * ( _KSq - _V ) * norm;
				_a2 = ( _V - _sqrt2V * _K + _KSq ) * norm;
				_b1 = 2 * ( _KSq - 1 ) * norm;
				_b2 = ( 1 - GATMaths.SQRT2 * _K + _KSq ) * norm;
			}
			else 
			{   
				norm = 1 / ( _V + _sqrt2V * _K + _KSq);
				_a0 = ( 1 + GATMaths.SQRT2 * _K + _KSq ) * norm;
				_a1 = 2 * ( _KSq - 1 ) * norm;
				_a2 = ( 1 - GATMaths.SQRT2 * _K + _KSq ) * norm;
				_b1 = 2 * ( _KSq - _V ) * norm;
				_b2 = ( _V - _sqrt2V * _K + _KSq ) * norm;
			}
		}
		
		static GATHighShelfFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "High Shelf", typeof( GATHighShelfFilter ) );
		}
	}
}

