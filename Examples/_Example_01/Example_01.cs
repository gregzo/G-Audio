using UnityEngine;
using System.Collections;

// In this first example, we see how to retrieve all sample names from a sample bank 
// and play them through the default player at a user specified gain. 
// Playback is routed through one of 4 mono tracks, each filtered differently.
// Tracks are configured through GATPlayer's mixer inspector - see the G-Audio Manager/ DefaultPlayer GameObject.

namespace GAudio.Examples
{
	public class Example_01 : ExamplesBase //We extend TutorialsBase which handles drawing of buttons and sliders, and track selection
	{
		protected override string         Title{ get{ return "Example_01 : Retrieving & Playing Samples"; } }
		protected override string SlidersHeader{ get{ return "Gain"; } }
		
		protected override string[] GetButtonLabels() //one button and one slider per element will be drawn
		{
			return sampleBank.AllSampleNames; //We grab all sample names from the bank to draw one button per sample
		}
		
		protected override void SetSlidersRange( out float min, out float max, out float init ) //this override sets the range for sliders.
		{
			//We will use the sliders for gain control, range 0-2
			min = 0f;
			max = 2f;
			init = 1f;
		}
		
		protected override void ButtonClicked( int buttonIndex ) 
		{
			string sampleName 	= ButtonLabels[ buttonIndex ]; //since the button labels are the sample names, we can just grab the string at buttonIndex
			float  gain 		= SliderValues[ buttonIndex ]; //and the gain value according to the corresponding slider's value
			
			//Get the audio data from the bank
			GATData sampleData = sampleBank.GetAudioData( sampleName );
			
			// And play it through the default player, at track TrackNb, and specified gain.
			GATManager.DefaultPlayer.PlayData( sampleData, TrackNb, gain );  
		}
	}
}

