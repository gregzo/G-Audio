using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	//Base class for dragging a GameOject's x position to a float parameter
	public abstract class SliderObj : MonoBehaviour {
		
		public float minVal, maxVal;
		
		public float minX, maxX;
		
		public float yPos, zPos;
		
		public float initValue;
		
		void Awake()
		{
			UpdateSlider( initValue );
		}
		
		void OnMouseDrag()
		{
			Vector3 mousePoint = Camera.main.ScreenToWorldPoint( new Vector3( Input.mousePosition.x, Input.mousePosition.y, zPos ) );
			float sliderX = mousePoint.x;
			
			if( sliderX > maxX )
			{
				sliderX = maxX;
			}
			else if( sliderX < minX )
			{
				sliderX = minX;
			}
			
			transform.localPosition = new Vector3( sliderX, yPos, zPos );
			
			float newValue = ( ( sliderX - minX ) / ( maxX - minX ) ) * ( maxVal - minVal ) + minVal;
			
			SliderDidChange( newValue );
		}
		
		protected abstract void SliderDidChange( float newValue );
		
		protected void UpdateSlider( float newVal )
		{
			float normalized = ( newVal - minVal ) / ( maxVal - minVal );
			float sliderX = normalized * ( maxX - minX ) + minX;
			transform.localPosition = new Vector3( sliderX, yPos, zPos );
		}
	}
}

