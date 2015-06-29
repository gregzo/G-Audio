using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	/// <summary>
	/// In this example, we use GATResamplingSampleBank's caching abilities
	/// to pre-process pitch shifted samples and build a custom scale. 
	/// It is advised to have the Memory Status window docked when running
	/// the scene to observe the effects on memory - the Memory Status window
	/// can be popped from G-Audio Manager's inspector.
	/// </summary>
	public class Example_03 : ExamplesBase
	{
		// This will give us the ability to request chunks of samples from sample banks
		public EnvelopeModule envelopeModule;
		
		// The unique sample we'll pull from the bank and build our scale with
		public string sampleName = "C5";
		
		// Semi-tones for an equal-tempered, occidental major scale.
		static int[]  scaleSemiTones = new int[]{ 0, 2, 4, 5, 7, 9, 11, 12 };
		
		// We'll need references to the processed samples so that we can free memory 
		// when pitch is adjusted.
		IGATProcessedSample[] _processedSamples;
		
		protected override string         Title{ get{ return "Example_03: Caching Pitch Shifted Samples for Building Scales";  } }
		protected override string AreaOneHeader{ get{ return "Note";        } }
		protected override string SlidersHeader{ get{ return "Pitch";       } }
		
		void Awake()
		{
			// Our scale has 8 notes, we'll need one processed sample per note.
			_processedSamples = new IGATProcessedSample[ 8 ];
		}
		
		protected override string[] GetButtonLabels()
		{
			// Note names for our buttons
			return new string[]{ "C", "D", "E", "F", "G", "A", "B", "C" };
		}
		
		protected override void SetSlidersRange( out float min, out float max, out float init )
		{
			//Sliders will control pitch deviation in semi-tones : +/- 1 semi tone range 
			min 	= -1f;
			max 	=  1f;
			init	=  0f;
		}
		
		protected override void ButtonClicked( int buttonIndex )
		{
			// local reference to the cached sample for readability
			IGATProcessedSample cachedSample = _processedSamples[ buttonIndex ];
			
			// pitch is scale semi tone plus corresponding slider value, translated as a frequency ratio
			double pitch = GATMaths.GetRatioForInterval( ( float )scaleSemiTones[ buttonIndex ] + SliderValues[ buttonIndex ] );
			
			// If we have already played this sample and pitch has changed, we call Release().
			// If we wouldn't, the sample bank would keep a cached version of the sample at the previous pitch,
			// Quickly saturating memory. 
			if( cachedSample != null && cachedSample.Pitch != pitch )
			{
				cachedSample.Release();
			}
			
			if( envelopeModule != null ) // If we have an envelope module, we just pass it's envelope to the bank
			{							 // when requesting a processed sample. All processing is automated.
				cachedSample = sampleBank.GetProcessedSample( sampleName, envelopeModule.Envelope, pitch );
			}
			else
			{							 // Else, we pass null: the entire sample will be pitch shifted and cached.
				cachedSample = sampleBank.GetProcessedSample( sampleName, null, pitch );
			}
			
			cachedSample.Play( TrackNb ); // IGATProcessedSample also has it's own set of Play methods. Here, playback
			// will be routed through the default player's track TrackNb.
			_processedSamples[ buttonIndex ] = cachedSample; // keep a reference of the processed sample to release later.
		}
		
		protected override void AreaOneExtraGUI() // Let's add a button below the scale buttons
		{
			GUILayout.Space( 10f );
			
			if( GUILayout.Button( "Flush Cache" ) ) // Observe the effects of this button in the Memory Status Window - 
			{										// Don't forget to click Refresh!
				if( envelopeModule != null )
				{
					sampleBank.FlushCacheForEnvelope( envelopeModule.Envelope ); // Will release all cached samples associated with the envelope
				}
				else
				{
					sampleBank.FlushCacheForEnvelope( null ); // Will release all cached samples associated with a null envelope
				}											  // ( resampled versions of the original sound )
			}
		}
	}
}

