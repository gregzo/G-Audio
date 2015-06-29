using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	// In this example, we wrap samples in a GATRealTimeSample object before playback.
	// This gives us control over pitch, as well as the ablity to stop playback smoothly in
	// less than a frame. We use both these functionalities to cut playback of samples as soon as a new one is played.
	public class Example_02 : ExamplesBase 
	{
		// We need a reference to the last played GATRealTimeSample so that we may stop or pitch shift it.
		GATRealTimeSample _realTimeSample;
		
		// The index of the last played sample / clicked button
		int _playingSampleIndex;
		
		protected override string         Title{ get{ return "Example_02: Wrapping Samples for Pitch Shifting & Smooth Stopping"; } }
		protected override string SlidersHeader{ get{ return "Pitch"; } }
		
		protected override string[] GetButtonLabels()
		{
			return sampleBank.AllSampleNames; // Just as in the previous tutorial, we return all the sample names from the bank.
		}
		
		protected override void SetSlidersRange( out float min, out float max, out float init )
		{
			// Our sliders will affect pitch. We'd like to be able to pitch shift a semi-tone down, and 2 semi-tones up ( arbitrary ).
			// Pitch is expressed as a frequency factor ( 2 is double frequency ). GATMaths provides a handy method to translate semi-tone 
			// transposition to pitch factor.
			min 	= -2f;
			max 	= 2f;
			init 	= 1f;
		}
		
		protected override void ButtonClicked( int buttonIndex )
		{
			if( _realTimeSample != null ) 
			{
				if( _realTimeSample.PlayingStatus == GATRealTimeSample.Status.Playing ) // If a sample is already playing, we stop it
				{
					_realTimeSample.FadeOutAndStop( .5d ); //fade occurs at buffer level, no coroutines, no pops.
				}
			}
			
			string sampleName = ButtonLabels[ buttonIndex ]; // get the name of the clicked sample
			float  pitch      = SliderValues[ buttonIndex ]; // and the value of the corresponding slider
			
			GATData sampleData = sampleBank.GetAudioData( sampleName ); // grab the audio data from the bank
			
			_realTimeSample = new GATRealTimeSample( sampleData ); // wrap in a GATRealTimeSample for realtime monitoring and control
			_realTimeSample.Loop = true;
			_realTimeSample.Pitch = pitch; // set pitch
			
			_realTimeSample.PlayThroughTrack( TrackNb ); // and play. Note that we ask the GATRealTimeInstance to play directly. There are many overloaded
			// Play methods - this is the simplest and routes playback through the default player.
			
			_playingSampleIndex = buttonIndex; // cache the index of the sample for easy monitoring of sliders ( see below )
		}
		
		protected override void SliderValueDidChange( int valueIndex, float newValue )
		{
			if( valueIndex != _playingSampleIndex ) // if the slider isn't the currently playing sample's, ignore.
				return;
			
			if( _realTimeSample == null ) // if we haven't played a sample yet, ignore
				return;
			
			if( _realTimeSample.PlayingStatus != GATRealTimeSample.Status.Playing ) // if the last played sample is done playing, ignore.
				return;
			
			_realTimeSample.Pitch = newValue; // update pitch.
		}
	}
}

