using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// High pass bi-quad filter
	/// </summary>
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATHighPassFilter : AGATBiQuad
	{
		protected override void CalcBiquad()
		{
			double norm;
			
			norm 	= 1d / ( 1 + _K / _Q + _KSq );
			_a0  	= _K / _Q * norm;
			_a1  	= 0;
			_a2  	= -_a0;
			_b1  	= 2 * ( _KSq - 1 ) * norm;
			_b2  	= ( 1 - _K / _Q + _KSq ) * norm;
		}
		
		static GATHighPassFilter()
		{
			AGATMonoFilter.RegisterMonoFilter( "High Pass", typeof( GATHighPassFilter ) );
		}
	}
}

