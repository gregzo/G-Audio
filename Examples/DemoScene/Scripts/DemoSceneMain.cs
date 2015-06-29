using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// Controller class for the demo.
	//
	// Goal: add interactivity to a pattern composed and configured in the inspector.
	// The initial pattern can be heard in edit mode by starting both the GranularPulse and the MainPulse.
	//
	// Rules: When the user clicks on the center button, we want to:
	//		A) Double time the main pulse for 6 beats if on an even step index, or 7 beats if on an uneven step index.
	//		   To achieve that, we implement IGATPulseController and register this instance as the controller for the pulse:
	//		   the OnPulseControl callback we receive happens just before pulses are update and fire, giving us a chance to
	//		   adjust their period.         
	//
	//		B) End the double time streak with a long, distorted sound on the same note as the main pattern
	//         For that, we need to know which sample in the pattern ends the double time streak: subscribing to
	//		   PulsedPatternModule.onPatternWillPlay will enable us to retrieve that information.
	//
	//		C) At the end of the double time streak, randomly shift the harmony of the main pattern and the tone of the 
	//		   granular drone between 4 known states. 
	//		   We declare a simple serialized nested class, PatternPitches, in which we can specify semi-tone pitch shifts for the patterns.
	//		   The 4 harmonic states are stored in an array of PatternPitches, configured in the inspector.
	//
	//		D) When the harmony shifts, increase the main pulse's bpm.
	//		   We'll do that in the OnPulseControl callback. We won't limit bpm: let the user crash G-Audio, and see how far it can go
	//		   ( over 60'000 bpm on average desktop machines )
	//
	//		E) Trigger visual effects: the line renderer( PatternLine class ) for the double time streak,
	//		   the particle effect at the end of the streak, and the color tween of the DrawFFTModule's line renderer.
	//		   The PatternLine class will take care of drawing the double time streak line, we just need to enable it at the
	//		   right moment. Color tweens for the DrawFFTModule's line will be implemented here in the LerpFFTLineColorRoutine coroutine.
	//
	// Note that the draggable platform and the 2 clickable spheres are handled independently.
	public class DemoSceneMain : MonoBehaviour, IGATPulseController
	{
		//We need a ref. to the main pulse in order to manage double timing and increasing it's tempo gradually.
		public PulseModule pulse;
		
		//And a ref to the sample bank for playback of the longer sound at the end of a double time sequence.
		public GATResamplingSampleBank sampleBank;
		
		//The main pattern is made of 2 interlaced phasing patterns, A and B. We need references to them
		//in order to set their pitches when the harmony changes.
		public PulsedPatternModule patternA;
		public PulsedPatternModule patternB;
		
		//We also need a ref. to the granular pattern to pitch shift it
		public PulsedPatternModule granularPattern;
		
		//The particle system that we'll trigger at the end of the double time streak
		public ParticleSystem particles;
		
		//The class that handles drawing the double time streak
		public PatternLine patternLine;
		
		//We'll tween this line's color during double time streaks to enhance the effect
		public DrawFFTModule fftLine;
		//The color we'll lerp the fft line to
		public Color doubletimeLineColor;
		
		//The array holding harmonic states we'll randomly shift between 
		public PatternPitches[] chords;
		
		//The current index in chords 
		int   _chordIndex;
		
		//Size of the square button
		float _buttonSide;
		
		//Are we currently in a double time streak?
		bool  _inDoubleTime;
		
		//If the user pressed the button, we'll switch to double time streak at the next beat.
		bool  _shouldEnterDoubleTime;
		
		//Set to true at the end of the double time streak
		bool  _endOfStreak;
		
		//Hardcoded further down, 6 or 7
		int   _nbOfDoubleTimePulses;
		
		//We keep track of how many double time pulses have occurred to switch back to normal speed
		int   _counter;
		
		// We'll play a longer, distorted, sound at the end of a double time streak.
		// We0ll wrap it in a GATRealTimeSample to pitch shift it according to mouse y.
		GATRealTimeSample _rtSample;
		// How much are we pitch shifting per pixel?
		float _mouseToPitchCoeff;
		
		//Hard coded position of the particle effect at the end of the streak
		const float PARTICLES_Z 		= 5f;
		const float PARTICLES_X 		= 3.5f;
		const float PARTICLES_Y_FACTOR 	= .1f;
		
		[ System.Serializable ]
		public class PatternPitches
		{
			//Pitch shifts ( in semi tones ) for patterns A, B, and granular drone
			public float[] patternA;
			public float[] patternB;
			public float granularPitch;
		}
		
		void Start()
		{
			//pitch shift coeff will depend on screen size
			_mouseToPitchCoeff = .5f / Screen.height;
			//And so will the button's side: we don't want it pin sized on retina displays
			_buttonSide = Screen.height / 15;
		}
		
		void OnEnable()
		{
			//Subscribe and register, we need the info! Always in OnEnable
			pulse.RegisterPulseController( this );
			patternA.onPatternWillPlay += PatternAWillPlay;
		}
		
		void OnDisable()
		{
			//Always unregister in OnDisable
			pulse.UnregisterPulseController( this );
			patternA.onPatternWillPlay -= PatternAWillPlay;
		}
		
		void OnGUI()
		{
			// The button at the center of the screen simply sets the _shouldEnterDoubleTime flag to true: we want to actually start double time
			// just as the next pulse fires
			if( GUI.Button( new Rect( Screen.width / 2 - _buttonSide / 2, Screen.height / 2 - _buttonSide, _buttonSide, _buttonSide ),  "" ) )
			{
				if( _inDoubleTime == false )
					_shouldEnterDoubleTime = true;
			}
		}
		
		//The pulse control callback: we get information regarding the previous pulse, and a chance to change the next one.
		void IGATPulseController.OnPulseControl( IGATPulseInfo prevPulseInfo )
		{
			if( _shouldEnterDoubleTime ) // the user clicked the button since the last pulse
			{
				pulse.Period /= 2; //double bpm
				_counter	  = 0; // set the double time counter to 0
				
				_shouldEnterDoubleTime = false; 
				_inDoubleTime 			= true;
				patternLine.enabled 	= true; //Enabling the patternLine will make it subscribe to onPatternWillPlay delegates for A and B patterns. 
				
				if( prevPulseInfo.StepIndex % 2 == 0 ) // if the previous step index was even, we'll double time for 7 beats
				{									   // ( we want the double time streak to end on a Pattern A note, and pattern A plays on even steps )
					_nbOfDoubleTimePulses    = 7;
				}
				else
				{
					_nbOfDoubleTimePulses    = 6; // else for 6
					patternLine.SetFirstVertexIndex( 1 ); // and notify the line that we'll start at index 1 ( the line is setup to draw 7 steps, not 6 )
				}
				
				//Start the color tween: it should start at prev pulse dspTime + new period and last new period * double time pulses
				StartCoroutine( LerpFFTLineColorRoutine( prevPulseInfo.PulseDspTime + pulse.Period, ( float )pulse.Period * _nbOfDoubleTimePulses ) );
			}
			else if( _inDoubleTime ) // we're in the double time streak
			{
				_counter++;
				
				if( _counter == _nbOfDoubleTimePulses ) // end of the streak
				{
					pulse.Period   *= 2; //bpm back to where it was
					_inDoubleTime   = false;
					_endOfStreak = true; 
				}
			}
		}
		
		void PatternAWillPlay( PatternSample sampleInfo, int indexInPattern, double dspTime )
		{
			if( _endOfStreak ) //Time to maybe change harmonic state, and in all cases trigger the distorted longer sound
			{
				TryChangeChord(); 
				PlayStreakEnd( sampleInfo, dspTime ); //we'll use the same pitch shift and sample for the longer sound
				_endOfStreak = false;
			}
		}
		
		void PlayStreakEnd( PatternSample sampleInfo, double dspTime )
		{
			_rtSample = new GATRealTimeSample( sampleBank.GetAudioData( sampleInfo.SampleName ) ); // grab the sample and wrap it in GATRealtimeSample for realtime pitch shifting
			_rtSample.Pitch = sampleInfo.Pitch;  // set it's pitch to be the same as the end of streak pitch
			_rtSample.PlayScheduledThroughTrack( dspTime, 2 ); //play through track 2, which is panned to the right and distorted
			
			float deltaTime = ( float )( dspTime - AudioSettings.dspTime ); 
			particles.transform.localPosition = new Vector3( PARTICLES_X, PARTICLES_Y_FACTOR * sampleInfo.SemiTones, PARTICLES_Z ); //Adjust the particle system's y to reflect the pitch's height
			Invoke( "PlayParticles", deltaTime ); //Fire at appropriate time
		}
		
		void PlayParticles()
		{
			particles.Play();
		}
		
		void TryChangeChord() //try because there's a chance the chord will not change - we don't want the harmony to shift too systematically
		{
			float ran = Random.value;
			if( ran < .4f ) //40% chance of moving to the next harmonic state
			{
				_chordIndex = ( _chordIndex + 1 ) % chords.Length;
			}
			else if( ran < .7f ) // 30% chance of moving back to the previous harmonic state
			{
				_chordIndex--; 
				if( _chordIndex < 0 ) //wrap
					_chordIndex = chords.Length - 1;
			}
			else return; // 30% chance of not changing harmonic state at all - in that case, don't increase tempo: early return
			
			//Now apply the new pitches to patterns A and B
			int i;
			PatternPitches chord = chords[ _chordIndex ]; 
			
			for ( i = 0; i < chord.patternA.Length; i++ )
			{
				patternA.Samples[i].SemiTones = chord.patternA[i];
			}
			
			for ( i = 0; i < chord.patternB.Length; i++ )
			{
				patternB.Samples[i].SemiTones = chord.patternB[i];
			}
			
			//And to the granular drone, in a coroutine lerp to get glissandos
			StartCoroutine( TweenGranularPitch( ( float )pulse.Period * 6, chord.granularPitch ) );
			
			//Arbitrary rules for tempo increase
			if( pulse.Period > .15d )
			{
				pulse.Period *=  0.97d;
			}
			else if( pulse.Period > .02d )
			{
				pulse.Period *=  0.9d;
			}
			else
			{
				pulse.Period *=  0.95d;
			}
			
		}
		
		IEnumerator TweenGranularPitch( float duration, float targetPitch )
		{
			float factor = 1f / duration;
			
			float val = 0f;
			
			float fromPitch = granularPattern.Samples[0].SemiTones;
			
			while( val < 1f )
			{
				val += Time.deltaTime * factor;
				granularPattern.Samples[0].SemiTones = Mathf.Lerp( fromPitch, targetPitch, val * val );
				yield return null;
			}
		}
		
		float _prevMouseY;
		void Update() //We monitor the mouses deltaY in Update, and map it to the end of streak sound's pitch if it is playing.
		{
			float mouseDeltaY = Input.mousePosition.y - _prevMouseY;
			_prevMouseY 	  = Input.mousePosition.y;
			
			if( _rtSample == null )
				return;
			
			if( _rtSample.PlayingStatus == GATRealTimeSample.Status.Playing )
			{
				_rtSample.Pitch *= 1f + ( mouseDeltaY * _mouseToPitchCoeff );
			}
		}
		
		//Simple color lerp, back and forth
		IEnumerator LerpFFTLineColorRoutine( double startDspTime, float duration )
		{
			while( AudioSettings.dspTime < startDspTime )
				yield return null;
			
			yield return StartCoroutine( LerpFFTLineColor( duration, fftLine.startColor, doubletimeLineColor ) );
			yield return null;
			StartCoroutine( LerpFFTLineColor( .8f, doubletimeLineColor, fftLine.startColor ) );
		}
		
		IEnumerator LerpFFTLineColor( float duration, Color fromColor, Color targetColor )
		{
			float factor = 1f / duration;
			Color appliedColor;
			float lerpVal = 0f;
			
			while( lerpVal < 1f )
			{
				lerpVal 	+= Time.deltaTime * factor;
				appliedColor = Color.Lerp( fromColor, targetColor, lerpVal );
				
				fftLine.Line.SetColors( appliedColor, fftLine.endColor );
				
				yield return null;
			}
		}
	}
}

