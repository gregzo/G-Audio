using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	//Maps the x position of a GameObject to an Envelope's offset parameter
	public class EnvelopeOffsetSlider : SliderObj {
		
		public EnvelopeModule envelope;
		
		protected override void SliderDidChange( float newValue )
		{
			envelope.Offset = ( int )newValue;
		}
	}
}


