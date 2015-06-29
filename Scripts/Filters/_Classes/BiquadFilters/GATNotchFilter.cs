using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Notch biquad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATNotchFilter : AGATBiQuad
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			norm 	= 1d / ( 1 + _K / _Q + _KSq );
			_a0   	= ( 1 + _KSq ) * norm;
			_a1   	= 2 * ( _KSq - 1 ) * norm;
			_a2 	= _a0;
			_b1 	= _a1;
			_b2 	= ( 1 - _K / _Q + _KSq ) * norm;
		}
		
		static GATNotchFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "Notch", typeof( GATNotchFilter ) );
		}
	}
}

