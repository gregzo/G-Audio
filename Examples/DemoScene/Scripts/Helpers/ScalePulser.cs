using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	//pulses transform.localScale on all 3 axis
	public class ScalePulser : FloatPulser {
		
		
		protected override void DidLerp( float val )
		{
			transform.localScale = new Vector3( val, val, val );
		}
	}
}

