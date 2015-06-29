//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Static class which provides a few audio related methods and constants.
	/// Methods that handle the actual sample processing will be found
	/// in the GATData class or in AGATFilter derived classes.
	/// </summary>
	public static class GATMaths  
	{
		/// <summary>
		/// Square root of 2.
		/// </summary>
		public const double SQRT2 = 1.414213562373095d;
		
		/// <summary>
		/// Gets the max and min value in the array.
		/// </summary>
		public static void GetMaxAndMin ( float[] data, int fromIndex, int length, out float min, out float max )
		{
			int i;
			max = 0;
			min = 0;
			
			for ( i = fromIndex; i < fromIndex+length; i++ )
			{
				if ( data[i] > max ) 
				{
					max = data[i];
				}
				else if ( data[i] < min )
				{
					min = data[i];
				}
			}
		}
		
		/// <summary>
		/// Gets the max and min values in the interleaved array.
		/// </summary>
		public static void GetMaxAndMin ( float[] data, int fromIndex, int length, out float min, out float max, int stride )
		{
			int i;
			max = 0;
			min = 0;
			length += fromIndex;
			
			for ( i = fromIndex; i < length; i+= stride )
			{
				if ( data[i] > max ) 
				{
					max = data[i];
				}
				else if ( data[i] < min )
				{
					min = data[i];
				}
			}
		}
		
		/// <summary>
		/// Gets the absolute max value in the array
		/// </summary>
		/// <returns>The abs max value.</returns>
		public static float GetAbsMaxValue( float[] data, int fromIndex, int length )
		{
			float min, max;
			GetMaxAndMin( data, fromIndex, length, out min, out max );
			min = Mathf.Abs( min );
			if( min > max )
				return min;
			
			return max;
		}
		
		/// <summary>
		/// Gets the absolute max value of the specified channel in an interleaved array.
		/// </summary>
		public static float GetAbsMaxValueFromInterleaved( float[] data, int fromIndex, int length, int channelNb, int nbOfChannels )
		{
			float min, max;
			fromIndex += channelNb;
			GetMaxAndMin( data, fromIndex, length, out min, out max, nbOfChannels );
			min = Mathf.Abs( min );
			if( min > max )
				return min;
			
			return max;
		}

		public static void ClampData ( float[] data, float minValue, float maxValue, out int nbOfClippedFloats )
		{
			int i;
			nbOfClippedFloats = 0;
			
			for ( i = 0; i < data.Length; i++ )
			{
				if ( data[i] > maxValue )
				{
					data[i] = maxValue;
					nbOfClippedFloats++;
				}
				else if ( data[i] < minValue )
				{
					data[i] = minValue;
					nbOfClippedFloats++;
				}
			}
		}
		
		public static int GetIndexOfMaxValue ( float[] data )
		{
			int i;
			int maxIndex = 0;
			float max = 0;
			for ( i = 0; i < data.Length; i++ )
			{
				if ( data[i] > max )
				{
					maxIndex = i;
					max = data[i];
				}
			}
			
			return maxIndex;
		}
		
		public static int GetIndexOfMaxValue( float[] data, int first, int toIndex )
		{
			int i;
			int maxIndex = 0;
			float max = 0;
			for ( i = first; i < toIndex; i++ )
			{
				if ( data[i] > max )
				{
					maxIndex = i;
					max = data[i];
				}
			}
			
			return maxIndex;
		}
		
		//Use when downsampling to avoid out of range if the source sample is not long enough
		public static int ClampedResampledLength( int sourceLength, int targetLength, double resamplingFactor )
		{
			int neededSamples = ( int ) ( targetLength * resamplingFactor );
			if( sourceLength < neededSamples ) 
			{
				targetLength = ( int ) ( sourceLength / resamplingFactor );
			}
			
			return targetLength;
		}
		
		public static int ResampledLength( int sourceLength, double resamplingFactor )
		{
			return ( int )( sourceLength / resamplingFactor );
		}
		
		public static float GetRatioForInterval( float semiTones )
		{
			if( semiTones == 0f )
				return 1f;
			
			float pitch = Mathf.Pow( 2f, semiTones / 12 );
			
			return pitch; 
		}
		
		public static float GetSemiTonesForRatio( float ratio )
		{
			return 12f * Mathf.Log( ratio, 2f );
		}
		
		public static bool IsPrime( int number )
		{
			if( ( number & 1 ) == 1 ) 
			{
				int upto = ( int )Mathf.Sqrt( number );
				for ( int i = 3; i <= upto; i += 2 ) 
				{
					if( number % i == 0 )
					{
						return false;
					}
				}
				
				return true;
			} 
			else 
			{
				return ( number == 2 );
			}
		}

		/// <summary>
		/// Fills the provided array with normalized Hanning Window data
		/// </summary>
		public static void MakeHanningWindow( float[] data )
		{
			int i;
			int size = data.Length;
			for ( i = 0; i < size; i++ )
			{
				data[i] = .5f * ( 1f - Mathf.Cos( 2 * Mathf.PI * i / size ) );
			}
		}
		
		/// <summary>
		/// Fills the provided array with normalized Hamming Window data
		/// </summary>
		public static void MakeHammingWindow( float[] data )
		{
			int i;
			int size = data.Length;
			for ( i = 0; i < size; i++ )
			{
				data[i] = .54f + .46f * Mathf.Cos( 2 * Mathf.PI * i / size );
			}
		}
	}
}

