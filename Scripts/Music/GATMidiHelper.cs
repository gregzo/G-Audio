using UnityEngine;
using System.Collections;

namespace GAudio
{
	public static class GATMidiHelper
	{
		static string[] __notesShatp = new string[]{ "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
		static string[] __notesFlat  = new string[]{ "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
		
		public static string MidiCodeToString( int midiCode, bool flats = false )
		{
			int octave = midiCode / 12 - 1;
			
			if( octave < 0 )
				return "";
			
			int absNote = midiCode % 12;
			
			
			if( flats )
			{
				return string.Format( "{0}-{1}", __notesFlat[ absNote ], octave.ToString() );
			}
			
			return string.Format( "{0}-{1}", __notesShatp[ absNote ], octave.ToString() );
		}
		
		public static int FrequencyToClosestMidiCode( float frequency )
		{
			float mid = 69 + ( 12 * ( Mathf.Log( frequency / 440, 2f ) ) );
			return Mathf.RoundToInt( mid );
		}
		
		public static float FrequencyToMidiCode( float frequency, float tuningA = 440f )
		{
			return 69 + ( 12 * ( Mathf.Log( frequency / tuningA, 2f ) ) );
		}
		
		public static float MidiCodeToFrequency( float midicode, float tuningA = 440f )
		{
			return ( tuningA / 32 ) * ( Mathf.Pow( 2f, ( midicode - 9f ) / 12 ) );
		}
	}
}

