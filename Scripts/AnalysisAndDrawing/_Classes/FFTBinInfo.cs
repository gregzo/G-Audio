using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GAudio
{
	public class FFTBinInfo
	{
		public readonly int   BinIndex;
		public readonly float InterpolatedBinIndex;
		public readonly float InterpolatedFrequency;
		public readonly float InterpolatedMagnitude;
		
		public FFTBinInfo( float[] magnitudes, int index, float binFreqWidth )
		{
			float mag1, mag2, mag3, d;
			
			mag1 = magnitudes[ index - 1 ];
			mag2 = magnitudes[ index     ];
			mag3 = magnitudes[ index + 1 ];
			
			d = ( mag3 - mag1 ) / ( 2 * ( 2 * mag2 - mag1 - mag3 ) ); //Quadratic interpolation
			
			InterpolatedMagnitude = mag2 - ( ( mag1 - mag3 ) * d ) / 4;
			
			InterpolatedBinIndex = index + d;
			
			InterpolatedFrequency = InterpolatedBinIndex * binFreqWidth;
			
			BinIndex = index;
		}
		
		public FFTBinInfo( float[] magnitudes, int index, int sampleRate, int fftSize ) : this( magnitudes, index, ( float )sampleRate / fftSize )
		{
		}
		
		
		public int GetMidiCode()
		{
			return GATMidiHelper.FrequencyToClosestMidiCode( InterpolatedFrequency );
		}
		
		public string Description
		{
			get
			{
				return string.Format( "Interpolated frequency: {0}, magnitude: {1}, midicode: {2}", InterpolatedFrequency, InterpolatedMagnitude, GetMidiCode() );
			}
		}
		
		public static List< FFTBinInfo > GetLowerMaxBins( float[] magnitudes, int fromIndex, int toIndex, float binFrequencyWidth, float magThresholdRatio )
		{
			List< FFTBinInfo > maxBins = new List< FFTBinInfo >();
			FFTBinInfo 		   binInfo;
			int 			   maxIndex;
			float 			   magThreshold;
			
			fromIndex ++;
			toIndex --;
			
			maxIndex = GATMaths.GetIndexOfMaxValue( magnitudes, fromIndex, toIndex );
			
			binInfo = new FFTBinInfo( magnitudes, maxIndex, binFrequencyWidth );
			
			maxBins.Add( binInfo );
			
			magThreshold = binInfo.InterpolatedMagnitude * magThresholdRatio;
			
			while( true )
			{
				toIndex  = binInfo.BinIndex - 1;
				
				if( toIndex - fromIndex < 3 )
					break;
				
				maxIndex = GATMaths.GetIndexOfMaxValue( magnitudes, fromIndex, toIndex );
				binInfo  = new FFTBinInfo( magnitudes, maxIndex, binFrequencyWidth );
				
				if( binInfo.InterpolatedMagnitude < magThreshold )
					break;
				
				maxBins.Add( binInfo );
			}
			
			return maxBins;
		}
	}
}

