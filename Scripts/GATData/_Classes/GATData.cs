//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System;

namespace GAudio
{
	// Convenience interface which allows
	// wrapping of both GATData and IGATProcessedSample 
	// objects in GATRealTimeSample and GATRealTimeADSR
	public interface IGATDataOwner : IRetainable
	{
		GATData AudioData{ get; }
	}
	
	/// <summary>
	/// Unmanaged: normal, garbage collected memory
	/// Managed: virtual allocation of a chunk from a larger float[] pre-allocated by a GATDataAllocator instance 
	/// Fixed: like Managed, but may not be freed and is not constrained to GATDataAllocator bin sizes.
	/// </summary>
	public enum GATDataAllocationMode{ Unmanaged, Managed, Fixed };
	
	/// <summary>
	/// A wrapper for audio data, with a suite of
	/// audio processing methods. Used throughout
	/// G-Audio.
	/// Note: When requesting GATData instances from
	/// GATManager.GetDataContainer( int size ), you
	/// will in fact be handed an instance of a managed subclass
	/// ( GATDataAllocator.GATManagedData ) which responds
	/// to Retain() and Release() calls. See the example
	/// scenes that comes with G-Audio for examples of typical use.
	/// </summary>
	public class GATData : RetainableObject, IGATDataOwner
	{
		/// <summary>
		/// Offset in the parent array if using managed data.
		/// </summary>
		public int MemOffset{ get{ return _offset; } }
		
		/// <summary>
		/// Gets the parent array.
		/// Use with caution!
		/// </summary>
		public float[] ParentArray{ get{ return _parentArray; } }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="GATData"/> class.
		/// If you need a managed data chunk, use GATManager.GetDataContainer instead
		/// </summary>
		public GATData( float[] parentArray )
		{
			_parentArray = parentArray;
		}
		
		/// <summary>
		/// The allocated number of samples.
		/// </summary>
		public virtual int Count{ get{ return _parentArray.Length - _offset; } } 
		
		/// <summary>
		/// Gets or sets the float in the parent array at the specified index.
		/// The indexer should only be used for quick prototyping, as it is roughly
		/// 30% slower than caching ParentArray for direct access in an extension method.
		/// </summary>
		public float this[ int index ]
		{	
			get{ return _parentArray[ _offset + index ]; 		  }
			set{	    _parentArray[ _offset + index ] = value; }
		}
		
		/// <summary>
		/// IGATDataOwner explicit implementation.
		/// Useful to reference both processed samples and GATData objects.
		/// In the case of GATData, it returns itself.
		/// </summary>
		GATData IGATDataOwner.AudioData{ get{ return this; } }
		
		/// <summary>
		/// Gets an IntPtr pointer to the parent array.
		/// Only valid for GATData instances obtained through
		/// GATManager.GetDataContainer calls.
		/// </summary>
		public virtual System.IntPtr GetPointer()
		{
			return IntPtr.Zero;
		}
		
		#region Copying, Mixing and Processing ( Public )
		
		/// <summary>
		/// Copies data from a GATData instance to a float array.
		/// </summary>
		public void CopyTo( float[] destinationArray, int destinationIndex, int sourceIndex, int length )
		{
			System.Array.Copy( _parentArray, sourceIndex + _offset, destinationArray, destinationIndex, length );
		}
		
		/// <summary>
		/// Copies data from a GATData instance to another.
		/// </summary>
		public void CopyTo( GATData destinationData, int destinationIndex, int sourceIndex, int length )
		{
			System.Array.Copy( _parentArray, sourceIndex + _offset, destinationData._parentArray, destinationIndex + destinationData._offset, length );
		}
		
		/// <summary>
		/// Copies data from a float[] to a GATData instance.
		/// </summary>
		public void  CopyFrom( float[] sourceArray, int destinationIndex, int sourceIndex, int length )
		{	
			System.Array.Copy( sourceArray, sourceIndex, _parentArray, _offset + destinationIndex, length );
		}
		
		/// <summary>
		/// Copies and multuplies by gain data from one GATData instance to another.
		/// </summary>
		public void CopyGainTo( GATData destinationData, int sourceIndex, int destinationIndex, int length, float gain )
		{
			sourceIndex += _offset;
			length 		+= sourceIndex;
			
			float[] destinationArray = destinationData.ParentArray;
			
			destinationIndex += destinationData._offset;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationIndex ] = _parentArray[ sourceIndex ] * gain;
				sourceIndex++;
				destinationIndex++;
			}
		}
		
		/// <summary>
		/// Performs a copy of a single channel
		/// from an interlaced source array.
		/// </summary>
		public void CopyFromInterlaced( float[] sourceArray, int targetLength, int channelNb, int nbOfChannels )
		{
			int i;
			int sourceIndex = channelNb;
			targetLength += _offset;
			
			for( i = _offset; i < targetLength; i++ )
			{
				_parentArray[i] = sourceArray[ sourceIndex ];
				sourceIndex += nbOfChannels;
			}
		}
		
		/// <summary>
		/// Performs a copy of a single channel
		/// from an interlaced source array.
		/// </summary>
		public void CopyFromInterlaced( float[] sourceArray, int targetLength, int targetOffset, int channelNb, int nbOfChannels )
		{
			int i;
			int sourceIndex = channelNb;
			targetLength += _offset + targetOffset;
			
			for( i = _offset + targetOffset; i < targetLength; i++ )
			{
				_parentArray[i] = sourceArray[ sourceIndex ];
				sourceIndex += nbOfChannels;
			}
		}
		
		/// <summary>
		/// Performs a copy of a single channel
		/// from an interlaced source array.
		/// sourceOffset is samples offset for channel 0.
		/// </summary>
		public void CopyFromInterlaced( float[] sourceArray, int sourceOffset, int targetLength, int targetOffset, int channelNb, int nbOfChannels )
		{
			int i;
			int sourceIndex = sourceOffset + channelNb;
			targetLength += _offset + targetOffset;
			
			for( i = _offset + targetOffset; i < targetLength; i++ )
			{
				_parentArray[i] = sourceArray[ sourceIndex ];
				sourceIndex += nbOfChannels;
			}
		}
		
		/// <summary>
		/// Performs a copy of a single channel
		/// from an interlaced source array.
		/// </summary>
		public void MixFromInterlaced( float[] sourceArray, int sourceFrameOffset, int targetLength, int targetOffset, int channelNb, int nbOfChannels )
		{
			int i;
			int sourceIndex = sourceFrameOffset + channelNb;
			targetLength += _offset + targetOffset;
			
			for( i = _offset + targetOffset; i < targetLength; i++ )
			{
				_parentArray[i] += sourceArray[ sourceIndex ];
				sourceIndex += nbOfChannels;
			}
		}
		
		/// <summary>
		/// Copies data from a float[] to a GATData instance 
		/// and applies interpolated gain.
		/// </summary>
		public void CopySmoothedGainFrom( float[] sourceArray, int sourceIndex, int destinationIndex, int length, float fromGain, float toGain )
		{
			float interpolationDelta = ( toGain - fromGain ) / length;
			destinationIndex += _offset;
			length += sourceIndex;
			
			while( sourceIndex < length )
			{
				_parentArray[destinationIndex] = sourceArray[sourceIndex] * fromGain;
				fromGain += interpolationDelta;
				destinationIndex ++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Copies data from a GATData instance to another and 
		/// applies interpolated gain.
		/// </summary>
		public void CopySmoothedGainTo( int sourceIndex, GATData destination, int destinationIndex, int length, float fromGain, float toGain )
		{
			sourceIndex += _offset;
			destination.CopySmoothedGainFrom( _parentArray, sourceIndex, destinationIndex, length, fromGain, toGain ); 
		}
		
		/// <summary>
		/// Additive copy to a float[]
		/// </summary>
		public void MixTo( float[] destinationArray, int destinationIndex, int sourceIndex, int length )
		{
			sourceIndex += _offset;
			length 		+= sourceIndex;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationIndex ] += _parentArray[sourceIndex];
				destinationIndex++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy from a float[]
		/// </summary>
		public void MixFrom( float[] sourceArray, int destinationIndex, int sourceIndex, int length )
		{
			length += sourceIndex;
			destinationIndex += _offset;
			
			while( sourceIndex < length )
			{
				_parentArray[ destinationIndex ] += sourceArray[sourceIndex];
				destinationIndex ++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy to another GATData instance
		/// </summary>
		public void MixTo( GATData destination, int destinationIndex, int sourceIndex, int length )
		{	
			float[] destinationArray = destination._parentArray;
			sourceIndex += _offset;
			length += sourceIndex;
			destinationIndex += destination._offset;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationIndex ] += _parentArray[sourceIndex];
				destinationIndex ++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and gain to another GATData instance
		/// </summary>
		public void GainMixTo( GATData destinationData, int sourceIndex, int destinationIndex, int length, float gain )
		{
			sourceIndex += _offset;
			length 		+= sourceIndex;
			
			float[] destinationArray = destinationData.ParentArray;
			
			destinationIndex += destinationData._offset;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationIndex ] += _parentArray[ sourceIndex ] * gain;
				sourceIndex++;
				destinationIndex++;
			}	
		}
		
		/// <summary>
		/// Additive copy and gain to an interleaved float[]
		/// </summary>
		public void GainMixToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATChannelGain channelGain )
		{
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset += channelGain.ChannelNumber;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] += _parentArray[sourceIndex] * channelGain.Gain;
				destinationOffset += GATInfo.NbOfChannels;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and gain to an interleaved float[] with extra gain parameter.
		/// </summary>
		public void GainMixToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATChannelGain channelGain, float igain )
		{
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset += channelGain.ChannelNumber;
			float appliedGain = igain * channelGain.Gain;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] += _parentArray[sourceIndex] * appliedGain;
				destinationOffset += GATInfo.NbOfChannels;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Copy and gain to an interleaved float[]
		/// </summary>
		public void GainCopyToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATChannelGain channelGain )
		{
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset +=  channelGain.ChannelNumber;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] = _parentArray[sourceIndex] * channelGain.Gain;
				destinationOffset += GATInfo.NbOfChannels;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Copy to an interleaved float[]
		/// </summary>
		public void CopyToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, int channelNb )
		{
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset +=  channelNb;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] = _parentArray[sourceIndex];
				destinationOffset += GATInfo.NbOfChannels;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and interpolated gain to an interleaved float[]
		/// </summary>
		public void SmoothedGainMixToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATDynamicChannelGain channelGain )
		{
			float gain;
			float interpolationDelta;
			
			gain = channelGain.PrevGain;
			interpolationDelta = channelGain.InterpolationDelta;
			
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset += channelGain.ChannelNumber;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] += _parentArray[sourceIndex] * gain;
				destinationOffset += GATInfo.NbOfChannels;
				gain += interpolationDelta;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and interpolated gain to an interleaved float[] with extra gain parameter
		/// </summary>
		public void SmoothedGainMixToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATDynamicChannelGain channelGain, float igain )
		{
			float gain;
			float interpolationDelta;
			
			gain = channelGain.PrevGain;
			interpolationDelta = channelGain.InterpolationDelta;
			
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset += channelGain.ChannelNumber;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] += _parentArray[sourceIndex] * gain * igain;
				destinationOffset += GATInfo.NbOfChannels;
				gain += interpolationDelta;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Copy and interpolated gain to an interleaved float[]
		/// </summary>
		public void SmoothedGainCopyToInterlaced( float[] destinationArray, int destinationOffset, int sourceIndex, int length, GATDynamicChannelGain channelGain )
		{
			float gain;
			float interpolationDelta;
			
			gain = channelGain.PrevGain;
			interpolationDelta = channelGain.InterpolationDelta;
			
			sourceIndex = sourceIndex + _offset;
			length 		= sourceIndex + length;
			destinationOffset += channelGain.ChannelNumber;
			
			while( sourceIndex < length )
			{
				destinationArray[ destinationOffset ] = _parentArray[sourceIndex] * gain;
				destinationOffset += GATInfo.NbOfChannels;
				gain += interpolationDelta;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and interpolated gain from a float[]
		/// </summary>
		public void MixSmoothedGainFrom( float[] sourceArray, int sourceIndex, int destinationIndex, int length, float fromGain, float toGain )
		{
			float interpolationDelta = ( toGain - fromGain ) / length;
			destinationIndex += _offset;
			length += sourceIndex;
			
			while( sourceIndex < length )
			{
				_parentArray[destinationIndex] += sourceArray[sourceIndex] * fromGain;
				fromGain += interpolationDelta;
				destinationIndex ++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// Additive copy and interpolated gain to another GATData instance
		/// </summary>
		public void MixSmoothedGainTo( int sourceIndex, GATData destination, int destinationIndex, int length, float fromGain, float toGain )
		{
			float interpolationDelta;
			float[] destinationArray;
			interpolationDelta = ( toGain - fromGain ) / length;
			
			destinationIndex += destination._offset;
			sourceIndex += _offset;
			
			length += sourceIndex;
			destinationArray = destination._parentArray;
			
			while( sourceIndex < length )
			{
				destinationArray[destinationIndex] += _parentArray[sourceIndex] * fromGain;
				fromGain += interpolationDelta;
				destinationIndex ++;
				sourceIndex++;
			}
		}
		
		/// <summary>
		/// In-place Interpolated gain
		/// </summary>
		public void SmoothedGain( int fromIndex, int length, float fromGain, float toGain )
		{
			float rate = ( toGain - fromGain ) / length;
			fromIndex += _offset;
			
			length += fromIndex;
			
			while( fromIndex < length )
			{
				_parentArray[fromIndex] *= fromGain;
				fromGain += rate;
				fromIndex ++;
			}
		}
		
		public void Gain( int fromIndex, int length, float gain )
		{
			fromIndex += _offset;
			length 	  += _offset;
			
			while( fromIndex < length )
			{
				_parentArray[ fromIndex ] *= gain;
				fromIndex ++;
			}
		}
		
		/// <summary>
		/// In place fade-in from offset 0 to fadeLength
		/// </summary>
		public void FadeIn( int fadeLength)
		{
			if( fadeLength > Count )
			{
				fadeLength = Count;
			}
			
			float rate = 1f / fadeLength;
			
			int fadeIndex = _offset;
			int i;
			
			for( i = 0; i < fadeLength; i++ )
			{
				_parentArray[ fadeIndex ] *= ( i * rate );
				fadeIndex++;
			}
		}
		
		public void FadeInSquared( int fadeLength )
		{
			if( fadeLength > Count )
			{
				fadeLength = Count;
			}
			
			float rate = 1f / fadeLength;
			
			int fadeIndex = _offset;
			float gain;
			int i;
			
			for( i = 0; i < fadeLength; i++ )
			{
				gain = i * rate;
				gain *= gain;
				_parentArray[ fadeIndex ] *= gain;
				fadeIndex++;
			}
		}
		
		/// <summary>
		/// In-place fade-out from offset this.Count - fadeLength to
		/// this.Count - 1
		/// </summary>
		public void FadeOut( int fadeLength )
		{
			if( fadeLength > Count )
			{
				fadeLength = Count;
			}
			
			float rate = 1f / fadeLength;
			
			int fadeIndex = _offset + Count - fadeLength;
			int i;
			
			for( i = 0; i < fadeLength; i++ )
			{
				_parentArray[ fadeIndex ] *= ( 1 - i * rate );
				fadeIndex++;
			}
		}
		
		public void FadeOutSquared( int fadeLength )
		{
			if( fadeLength > Count )
			{
				fadeLength = Count;
			}
			
			float rate = 1f / fadeLength;
			float gain;
			int fadeIndex = _offset + Count - fadeLength;
			int i;
			
			for( i = 0; i < fadeLength; i++ )
			{
				gain = 1 - i * rate;
				gain *= gain;
				_parentArray[ fadeIndex ] *= gain;
				fadeIndex++;
			}
		}
		
		/// <summary>
		/// In-place fade out with offset control
		/// </summary>
		public void FadeOut( int offset, int fadeLength )
		{
			if( offset + fadeLength > Count )
			{
				fadeLength = Count - offset;
			}
			
			float rate = 1f / fadeLength;
			
			offset += _offset;
			int i;
			for( i = 0; i < fadeLength; i++ )
			{
				_parentArray[ offset ] *= ( 1 - i * rate );
				offset++;
			}
		}

		public void Fade( float fromGain, float toGain, int fadeLength, int offset = 0 )
		{
			if( offset + fadeLength > Count )
			{
				fadeLength = Count - offset;
			}

			offset += _offset;

			int i;

			float fadeRate = ( toGain - fromGain ) / fadeLength;
			float gain = fromGain;

			for( i = 0; i < fadeLength; i++ )
			{
				_parentArray[ offset ] *= gain;
				offset++;
				gain += fadeRate;
			}
		}
		
		/// <summary>
		/// Applies gain to the instance's data so that 
		/// the maximum value matches normalizedMax
		/// </summary>
		public void Normalize( float normalizedMax )
		{
			float max = 0f;
			int i;
			int maxIndex = _offset + Count;
			
			for( i = _offset; i < maxIndex; i++ )
			{
				if( _parentArray[ i ] > max )
				{
					max = _parentArray[ i ];
				}
			}
			
			float gain = normalizedMax / max;
			
			for( i = _offset; i < maxIndex; i++ )
			{
				_parentArray[ i ] *= gain;
			}
		}
		
		/// <summary>
		/// Performs a resampled
		/// copy to another 
		/// GATData instance
		/// The resampling algorithm
		/// performs naive linear interpolation.
		/// Warning: for resampling in chunks, use
		/// the method of the same name which returns
		/// a double interpolated index.
		/// </summary>
		public void ResampleCopyTo( int sourceIndex, GATData destinationSample, int targetLength, double resamplingFactor )
		{
			sourceIndex += _offset;
			destinationSample.ResampleCopyFrom( _parentArray, ( double )sourceIndex, targetLength, 0, resamplingFactor );
		}
		
		/// <summary>
		/// Performs a resampled
		/// copy from an array to
		/// the GATData instance it
		/// is called on.
		/// The resampling algorithm
		/// performs naive linear interpolation.
		/// </summary>
		/// <returns>
		/// The next interpolated index.
		/// </returns>
		public double ResampleCopyFrom( float[] sourceArray, double trueInterpolatedIndex, int targetLength, int targetOffset, double resamplingFactor )
		{
			int sourceIndex;
			int destinationIndex = _offset + targetOffset; 
			targetLength += destinationIndex;
			
			float sourceValue;
			
			double interpolation;
			
			
			
			while( destinationIndex < targetLength )
			{
				sourceIndex = ( int )trueInterpolatedIndex;
				interpolation = trueInterpolatedIndex - ( double )sourceIndex;
				sourceValue = sourceArray[ sourceIndex ];	
				
				_parentArray[ destinationIndex ] = sourceValue + ( sourceArray[ sourceIndex + 1 ] - sourceValue ) * ( float )interpolation;
				
				destinationIndex++;
				trueInterpolatedIndex += resamplingFactor;
			}
			
			return trueInterpolatedIndex;
		}
		
		/// <summary>
		/// Performs a resampled
		/// copy to another GATData
		/// instance.
		/// The resampling algorithm
		/// performs naive linear interpolation.
		/// </summary>
		/// <returns>
		/// The next interpolated index.
		/// </returns>
		public double ResampleCopyTo( double trueInterpolatedIndex, GATData destinationSample, int targetLength, double resamplingFactor )
		{
			return destinationSample.ResampleCopyFrom( _parentArray, trueInterpolatedIndex, targetLength, 0, resamplingFactor );
		}
		
		public void GlissCopyFrom( float[] sourceArray, int fromIndex, int targetLength, double fromPitch, double toPitch )
		{
			int 	sourceIndex;
			double 	interpolatedIndex;
			float 	sourceValue;
			double 	interpolation;
			int 	destinationIndex = _offset; 
			double  pitchDelta;
			
			targetLength += _offset;
			interpolatedIndex = ( double )fromIndex;
			pitchDelta = ( toPitch - fromPitch ) / targetLength;
			
			while( destinationIndex < targetLength )
			{
				sourceIndex = ( int )interpolatedIndex;
				interpolation = interpolatedIndex - ( double )sourceIndex;
				sourceValue = sourceArray[ sourceIndex ];	
				
				_parentArray[ destinationIndex ] = sourceValue + ( sourceArray[ sourceIndex + 1 ] - sourceValue ) * ( float )interpolation;
				
				destinationIndex++;
				interpolatedIndex += fromPitch;
				fromPitch 		  += pitchDelta;
			}
		}
		
		public void GlissCopyFrom( GATData sourceData, int fromIndex, double fromPitch, double toPitch )
		{
			fromIndex += sourceData.MemOffset;
			GlissCopyFrom( sourceData.ParentArray, fromIndex, this.Count, fromPitch, toPitch );
		}
		
		/// <summary>
		/// Applies a filter to the instance's audio data.
		/// </summary>
		public void ApplyFilter( AGATFilter filter, int fromIndex, int length, bool emptyData )
		{
			fromIndex += _offset;
			filter.ProcessChunk( _parentArray, fromIndex, length, emptyData );
		}
		
		/// <summary>
		/// Clears audio data at specified range 
		/// </summary>
		public void Clear( int fromIndex, int length )
		{
			System.Array.Clear( _parentArray, _offset + fromIndex, length );
		}
		
		/// <summary>
		/// Clears all audio data
		/// </summary>
		public void Clear()
		{
			System.Array.Clear( _parentArray, _offset, Count );
		}
		
		/// <summary>
		/// Clears a specific channel of interleaved data
		/// </summary>
		public void ClearInterlaced( int channelNb, int nbOfInterlacedChannels, int length )
		{
			int fromIndex = _offset + channelNb;
			length += fromIndex;
			
			while( fromIndex < length )
			{
				_parentArray[ fromIndex ] = 0f;
				fromIndex += nbOfInterlacedChannels;
			}
		}
		
		/// <summary>
		/// Reverses the entire audio data.
		/// </summary>
		public void Reverse()
		{
			System.Array.Reverse( _parentArray, _offset, Count );
		}

		public void Reverse( int offset, int length )
		{
			System.Array.Reverse( _parentArray, _offset + offset, length );
		}
		
		/// <summary>
		/// Finds the next zero crossing.
		/// </summary>
		/// <returns>
		/// The index at which zero crossing occured.
		/// </returns>
		/// <param name='positive'>
		/// true if the crossing
		/// is from a negative to a positive value.
		/// </param>
		public int NextZeroCrossing( int fromIndex, out bool positive )
		{
			fromIndex += _offset;
			
			if( _parentArray[ fromIndex ] < 0f )
			{
				while( _parentArray[ fromIndex ] < 0f )
				{
					fromIndex++;
				}
				positive = true;
				return fromIndex - _offset;
			}
			else if( _parentArray[ fromIndex ] > 0f )
			{
				while( _parentArray[ fromIndex ] > 0f )
				{
					fromIndex++;
				}
				positive = false;
				return fromIndex - _offset;
			}
			else //already at zero
			{
				while( _parentArray[ fromIndex ] == 0f )
				{
					fromIndex++;
				}
				
				positive = _parentArray[ fromIndex ] > 0f;
				return fromIndex - _offset;
			}
		}
		
		/// <summary>
		/// Converts an Int16 array and set it at targetOffset
		/// </summary>
		public void ConvertAndSetInt16( System.Int16[] source, int sourceOffset, int targetOffset, int length )
		{
			float sample;
			targetOffset += _offset;
			length 		 += sourceOffset;
			
			while( sourceOffset < length )
			{
				sample = ( float )source[ sourceOffset ] / GATWavHelper.floatToInt16RescaleFactor;
				_parentArray[ targetOffset ] = sample;
				sourceOffset++;
				targetOffset++;
			}
		}
		
		/// <summary>
		/// Checks that no data exceeds the -1f to 1f range.
		/// </summary>
		/// <returns><c>true</c>, if sample is in valid range, <c>false</c> otherwise.</returns>
		public bool CheckSampleBounds()
		{
			int i;
			for( i = _offset; i < Count; i++ )
			{
				if( _parentArray[ i ] > 1.0f || _parentArray[ i ] < -1.0f )
				{
					return false;
				}
			}
			
			return true;
		}
		
		#endregion
		
		#region Protected Members and Methods
		protected readonly float[] 	_parentArray;
		protected int 				_offset;
		
		protected override void 	Discard(){}
		
		#endregion
	}
}


