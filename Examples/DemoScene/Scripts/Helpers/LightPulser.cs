using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	//Pulses the intensity of a light
	public class LightPulser : FloatPulser {
		
		Light _light;
		
		void Awake()
		{
			_light = GetComponent<Light>();
		}
		
		protected override void DidLerp( float newVal )
		{
			_light.intensity = newVal;
		}
	}
}

