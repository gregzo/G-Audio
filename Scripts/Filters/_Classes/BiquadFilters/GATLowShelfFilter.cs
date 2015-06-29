using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Low Shelf biquad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATLowShelfFilter : AGATBiQuadPeak
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			if( _peakGain >= 0 ) // boost
			{    
				norm = 1d / ( 1d + GATMaths.SQRT2 * _K +_KSq );
				_a0 = ( 1 + _sqrt2V * _K + _V * _K * _K) * norm;
				_a1 = 2 * ( _V * _K * _K - 1) * norm;
				_a2 = ( 1 - _sqrt2V * _K + _V * _KSq ) * norm;
				_b1 = 2 * ( _KSq - 1 ) * norm;
				_b2 = ( 1 - GATMaths.SQRT2 * _K + _KSq ) * norm;
			}
			else 
			{   
				norm = 1d / ( 1 + _sqrt2V * _K + _V * _KSq );
				_a0 = ( 1 + GATMaths.SQRT2 * _K + _KSq ) * norm;
				_a1 = 2 * ( _K * _K - 1 ) * norm;
				_a2 = ( 1 - GATMaths.SQRT2 * _K + _KSq ) * norm;
				_b1 = 2 * ( _V * _K * _K - 1 ) * norm;
				_b2 = ( 1 - _sqrt2V * _K + _V * _KSq ) * norm;
			}
		}
		
		static GATLowShelfFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "Low Shelf", typeof( GATLowShelfFilter ) );
		}
	}
}

