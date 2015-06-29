using UnityEngine;
using System.Collections;
using GAudio;

namespace GAudio.Examples
{
	//Base class to pulse a single float parameter
	//Pulse in is a squared lerp, out is linear
	public abstract class FloatPulser : MonoBehaviour 
	{
		public float min, max;
		
		public PulsedPatternModule pattern;
		
		public int observedPatternIndex;
		
		public float pulseOutDuration;

		public float additionalLatency;
		
		enum State{ Idle, LerpIn, LerpOut };
		State _state;
		
		float _lerpVal;
		float _lerpFactor;
		
		void OnEnable()
		{
			pattern.onPatternWillPlay += OnPAtternWillPlay;
		}
		
		void OnDisable()
		{
			pattern.onPatternWillPlay -= OnPAtternWillPlay;
		}
		
		void OnPAtternWillPlay( PatternSample info, int indexInPattern, double dspTime )
		{
			if( indexInPattern == observedPatternIndex )
			{
				_lerpFactor = 1f / ( float )( dspTime + ( double )additionalLatency - AudioSettings.dspTime );
				_lerpVal 	= 0f;
				_state 		= State.LerpIn;
			}
		}
		
		void Update()
		{
			if( _state == State.Idle )
				return;
			
			float lerpedValue;
			
			_lerpVal += Time.deltaTime * _lerpFactor;
			
			if( _lerpVal > 1f )
			{
				_lerpVal = 1f;
			}
			
			if( _state == State.LerpIn )
			{
				lerpedValue = Mathf.Lerp( min, max, _lerpVal * _lerpVal ); //Square for pulseIn
			}
			else
			{
				lerpedValue = Mathf.Lerp( max, min, _lerpVal );
			}
			
			DidLerp( lerpedValue );
			
			if( _lerpVal == 1f )
			{
				if( _state == State.LerpIn )
				{
					_state = State.LerpOut;
					_lerpVal = 0f;
					_lerpFactor = 1f / pulseOutDuration;
				}
				else 
				{
					_state = State.Idle;
				}
			}
		}
		
		protected abstract void DidLerp( float newVal );
	}
}

