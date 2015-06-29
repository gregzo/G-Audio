using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// Subscribes to patterns A and B OnEnable, and uses a 
	// LineRenderer to draw a line mapped to A and B's pitch shifts
	// Helper class for the DemoScene, not re-usable
	public class PatternLine : MonoBehaviour {
		
		public PulsedPatternModule patternA, patternB;
		public Color lineStartColor;
		public Color lineEndColor;
		public float lineFadeDuration = .5f;
		
		public float xFactor, yFactor, zPos;
		
		int _vertexIndex;
		LineRenderer _line;
		bool _alphaIsZero;
		bool _isFading;
		int _firstVertexIndex;
		
		void Awake()
		{
			_line = GetComponent<LineRenderer>();
			
			this.enabled = false;
		}
		
		void OnEnable()
		{
			patternA.onPatternWillPlay += PatternWillPlay;
			patternB.onPatternWillPlay += PatternWillPlay;
			_vertexIndex = 0;
		}
		
		void OnDisable()
		{
			patternA.onPatternWillPlay -= PatternWillPlay;
			patternB.onPatternWillPlay -= PatternWillPlay;
		}
		
		public void SetFirstVertexIndex( int index )
		{
			_firstVertexIndex = index;
			_vertexIndex = index;
		}
		
		void PatternWillPlay( PatternSample sampleInfo, int indexInPattern, double dspTime )
		{
			if( _vertexIndex > 7 )
				return;
			
			StartCoroutine( PlaceVertex( _vertexIndex, sampleInfo.SemiTones, dspTime ) );
			_vertexIndex++;
		}
		
		IEnumerator PlaceVertex( int index, float semiTones, double dspTime )
		{
			while( AudioSettings.dspTime < dspTime )
			{
				yield return null;
			}
			
			int i;
			
			for( i = index; i < 8; i++ )
			{
				_line.SetPosition( i, new Vector3( index * xFactor, semiTones * yFactor, zPos ) );
			}
			
			if( index == _firstVertexIndex )
			{
				if( _isFading )
				{
					_isFading = false;
				}
				
				_line.SetColors( lineStartColor, lineEndColor );
			}
			
			if( index == 7 )
			{
				StartCoroutine( FadeOutLine( lineFadeDuration ) );
			}
		}
		
		IEnumerator FadeOutLine( float duration )
		{
			float factor 	= 1f / duration;
			float lerpVal 	= 0f; 
			float fromAlpha = lineEndColor.a;
			float alpha 	= fromAlpha;
			
			_isFading = true;
			while( alpha > 0f && _isFading )
			{
				lerpVal += Time.deltaTime * factor;
				alpha = Mathf.Lerp( fromAlpha, 0f, lerpVal );
				_line.SetColors( lineStartColor, new Color( lineEndColor.r, lineEndColor.g, lineEndColor.b, alpha ) );
				yield return null;
			}
			
			if( _isFading )
			{
				_isFading = false;
				this.enabled = false;
			}
		}
	}

}
