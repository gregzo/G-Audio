using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	//Toggles the specified pattern step on click
	public class TogglePatternStepOnClick : MonoBehaviour {
		
		public int step;
		public PulsedPatternModule pattern;
		
		void OnMouseUp()
		{
			pattern.SubscribedSteps[ step ] = !pattern.SubscribedSteps[ step ];
		}
	}
}

