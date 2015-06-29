using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// Base class for tutorials 1 to 4
	// Handles GUI controls so that derived classes
	// may focus on audio code.
	public abstract class ExamplesBase : MonoBehaviour 
	{
		//public fields
		public GATResamplingSampleBank sampleBank;
		
		//Protected Properties
		protected string[] 	ButtonLabels{ get{ return _buttonLabels; } } // The text for each button
		protected float[]  	SliderValues{ get{ return _sliderValues; } } // The current value for each slider
		protected int 		     TrackNb{ get{ return _trackNb;      } } // The currently selected track nb
		
		//Compulsory overrides
		protected abstract string[] GetButtonLabels(); //override to provide button count and text
		protected abstract void 	SetSlidersRange( out float min, out float max, out float init ); //override to set the range and init values for the sliders
		protected abstract void 	ButtonClicked( int buttonIndex ); //override to handle button clicks
		//Labels 
		protected virtual string    AreaOneHeader{ get{ return "Samples";      } }
		protected virtual  string   AreaTwoHeader{ get{ return "Track Number"; } }
		protected abstract string   SlidersHeader{ get; }
		protected abstract string           Title{ get; }
		
		//Optional overrides
		protected virtual void SliderValueDidChange( int valueIndex, float newValue ){} //override to handle slider update( optional )
		protected virtual void AreaOneExtraGUI(){} //override to draw extra GUI below the buttons
		protected virtual void AreaTwoExtraGUI(){} //override to draw extra GUI below the track selection grid
		
		
		//****************************************
		//************* Private ******************
		
		private float 	 _slidersMin, 
		_slidersMax, 
		_slidersInit;
		
		private int 	 _trackNb;
		
		private  float[] _sliderValues;
		private string[] _buttonLabels;
		
		static Rect __guiArea1 = new Rect( 20f,  30f, 250f, 400f );
		static Rect __guiArea2 = new Rect( 300f, 30f, 100f, 150f );
		
		private void Start()
		{
			// Make sure you're sample bank is set to LoadInAwake!
			// We start using it in Start(), not Awake(), to be sure it's already loaded.
			if( sampleBank.IsLoaded == false )
			{
				Debug.LogWarning( "Sample bank is not loaded!" );
				Destroy( this );
			}
			
			_buttonLabels = GetButtonLabels();
			_sliderValues = new float[ ButtonLabels.Length ];
			
			SetSlidersRange( out _slidersMin, out _slidersMax, out _slidersInit );
			
			for( int i = 0; i < SliderValues.Length; i++ )
			{
				SliderValues[ i ] = _slidersInit;
			}
		}
		
		private void OnGUI()
		{
			int i;
			
			//*************************************************************************
			//************** Title **************
			GUILayout.Label( this.Title );
			
			//*************************************************************************
			//************** First area: Sample buttons and gain sliders **************
			
			GUILayout.BeginArea( __guiArea1 );
			
			//Headers
			GUILayout.BeginHorizontal();
			GUILayout.Label( AreaOneHeader, GUILayout.Width( 150f ) );
			GUILayout.Label( this.SlidersHeader );
			GUILayout.EndHorizontal();
			
			// Iterate through the sample names
			for( i = 0; i < ButtonLabels.Length; i++ )
			{
				GUILayout.BeginHorizontal();
				
				// Draw the button
				if( GUILayout.Button( ButtonLabels[ i ], GUILayout.Width( 150f ) ) )
				{
					// If it is pressed, play it with appropriate gain
					ButtonClicked( i );
				}
				
				//Draw the gain sliders for each sample
				UpdateSliderValue( i, GUILayout.HorizontalSlider( SliderValues[ i ], _slidersMin, _slidersMax ) );
				
				GUILayout.EndHorizontal();
			}
			
			AreaOneExtraGUI();
			
			GUILayout.EndArea();
			
			
			//*************************************************************************
			//************** Second area: Track Selection *****************************
			
			
			
			GUILayout.BeginArea( __guiArea2 );
			
			//Header
			GUILayout.Label( this.AreaTwoHeader );
			
			// Let the user select a track, from 0 to 3.
			_trackNb = GUILayout.SelectionGrid( TrackNb, new string[]{ "0", "1", "2", "3" }, 1 );
			
			//If the selected track doesn't exist, select the last track
			if( _trackNb > GATManager.DefaultPlayer.NbOfTracks - 1 )
			{
				_trackNb = GATManager.DefaultPlayer.NbOfTracks - 1;
			}
			
			AreaTwoExtraGUI();
			
			GUILayout.EndArea();
		}
		
		private void UpdateSliderValue( int valueIndex, float newValue )
		{
			if( SliderValues[ valueIndex ] == newValue )
				return;
			
			SliderValues[ valueIndex ] = newValue;
			SliderValueDidChange( valueIndex, newValue );
		}
	}
}
