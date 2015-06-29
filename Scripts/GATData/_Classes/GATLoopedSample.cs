//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// A wrapper for GATData or IGATProcessedSample objects.
	/// Adds gapless looping and smooth stop behaviour.
	/// </summary>
	public class GATLoopedSample : AGATWrappedSample 
	{
		/// <summary>
		/// True by default. Set to false to stop playback 
		/// at the end of the current loop.
		/// </summary>
		/// <value><c>true</c> if loop; otherwise, <c>false</c>.</value>
		public bool Loop{ get; set; }
		
		/// <summary>
		/// Set to -1 for infinite looping.
		/// </summary>
		public int NumberOfLoops{ get; set; }
		
		/// <summary>
		/// How many times have we looped already?
		/// </summary>
		public int CurrentLoop{ get; protected set; }
		
		/// <summary>
		/// Wraps audio data in a looping container. Set NumberOfLoops to -1 for infinite looping.
		/// 0 for no loop. Pass a GATFixedPanInfo or GATDynamicPanInfo object if you intend to route
		/// playback directly to the player ( as opposed to through a track ).
		/// </summary>
		public GATLoopedSample( IGATDataOwner dataOwner, int numberOfLoops = -1, AGATPanInfo panInfo = null ) : base( dataOwner, panInfo )
		{
			if( numberOfLoops != 0 )
			{
				Loop = true;
			}
			
			NumberOfLoops = numberOfLoops;
		}
		
		protected override bool PlayerWillMixSample( IGATBufferedSample sample, int length, float[] audioBuffer )
		{	
			if( sample.IsFirstChunk )
			{
				PlayingStatus = Status.Playing;
			}
			
			if( StopsEarly )
			{
				if( AudioSettings.dspTime >= _endDspTime )
					_shouldStop = true;
			}
			
			if( _shouldStop )
			{
				sample.CacheToProcessingBuffer( length );
				sample.ProcessingBuffer.FadeOut( 0, length );
				sample.IsLastChunk = true;
				
				if( ReferenceEquals( sample.Track, null ) == false ) //sample is played in a track, which will handle Panning via it's own processing buffer. Copy data to the tracks buffer:
				{
					sample.Track.MixFrom( sample.ProcessingBuffer, 0, sample.OffsetInBuffer, length, sample.PlayingGain );
				}
				else
				{
					sample.PanInfo.PanMixProcessingBuffer( sample, length, audioBuffer, sample.PlayingGain );
				}
				
				PlayingStatus = Status.ReadyToPlay;
				CurrentLoop = 0;
				_shouldStop = false;
				
				return false;
			}
			
			if( sample.IsLastChunk )
			{
				if( Loop && ( NumberOfLoops == -1 || CurrentLoop < NumberOfLoops ) )
				{
					int extraSamples = GATInfo.AudioBufferSizePerChannel - length;
					sample.IsLastChunk = false;
					sample.AudioData.CopyTo( sample.ProcessingBuffer, 0, sample.NextIndex, length );
					sample.AudioData.CopyTo( sample.ProcessingBuffer, length, 0, extraSamples );
					sample.NextIndex = extraSamples;
					
					if( ReferenceEquals( sample.Track, null ) == false ) //sample is played in a track, which will handle Panning via it's own processing buffer. Copy data to the tracks buffer:
					{
						sample.Track.MixFrom( sample.ProcessingBuffer, 0, sample.OffsetInBuffer, GATInfo.AudioBufferSizePerChannel, sample.PlayingGain );
					}
					else
					{
						sample.PanInfo.PanMixProcessingBuffer( sample, GATInfo.AudioBufferSizePerChannel, audioBuffer, sample.PlayingGain );
					}
					
					CurrentLoop++;
					
					return false;
				}
				else
				{
					PlayingStatus = Status.ReadyToPlay;
					CurrentLoop = 0;
				}
			}
			
			return true;
		}
	}
}

