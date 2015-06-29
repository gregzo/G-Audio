using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Low Pass biquad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATLowPassFilter : AGATBiQuad
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			norm 	= 1d / ( 1 + _K / _Q + _KSq );
			_a0  	= _KSq * norm;
			_a1  	= 2 * _a0;
			_a2  	= _a0;
			_b1  	= 2 * ( _KSq - 1 ) * norm;
			_b2  	= ( 1 - _K / _Q + _KSq ) * norm;
		}
		
		protected override void OnEnable()
		{
			base.OnEnable();
		}
		
		static GATLowPassFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "Low Pass", typeof( GATLowPassFilter ) );
		}
	}
}

