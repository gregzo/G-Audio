using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Peak Frequency biquad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATPeakFilter : AGATBiQuadPeak
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			if( _peakGain >= 0 ) 
			{    // boost
				norm 	= 1d / ( 1 + 1 / _Q * _K + _KSq );
				_a0 	= ( 1 + _V / _Q * _K + _KSq ) * norm;
				_a1 	= 2 * ( _KSq - 1 ) * norm;
				_a2 	= ( 1 - _V / _Q * _K + _KSq ) * norm;
				_b1 	= _a1;
				_b2 	= ( 1 - 1d / _Q * _K + _KSq ) * norm;
			}
			else 
			{    // cut
				norm 	= 1d / ( 1 + _V / _Q * _K + _KSq );
				_a0 	= ( 1 + 1d / _Q * _K + _KSq ) * norm;
				_a1 	= 2 * ( _KSq - 1 ) * norm;
				_a2 	= ( 1 - 1d / _Q * _K + _KSq ) * norm;
				_b1 	= _a1;
				_b2 	= ( 1 - _V / _Q * _K + _KSq ) * norm;
			}
		}
		
		static GATPeakFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "Peak", typeof( GATPeakFilter ) );
		}
	}

}
