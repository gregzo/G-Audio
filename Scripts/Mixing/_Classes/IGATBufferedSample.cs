//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Interface through which currently
	/// playing samples may be accessed
	/// when subscribing to the player's
	/// OnShouldMixSample callback.
	/// </summary>
	public interface IGATBufferedSample
	{
		/// <summary>
		/// True if very first chunk of
		/// data is about to be mixed.
		/// </summary>
		bool IsFirstChunk{ get;}
		
		/// <summary>
		/// True if the last chunk of data is about
		/// to be mixed. Can be manually set to
		/// delay or advance stopping of playback
		/// when overriding default mixing.
		/// </summary>
		bool IsLastChunk{ get; set; }
		
		/// <summary>
		/// Use when overriding mixing to benefit
		/// from the class's convenient PanMix methods.
		/// </summary>
		AGATPanInfo PanInfo{ get; }
		
		/// <summary>
		/// The actual audio data being played
		/// </summary>
		GATData AudioData{ get; }
		
		/// <summary>
		/// The index in AudioData from which data
		/// is about to be mixed. May be set for
		/// seeking / resampling. Note that when
		/// subscribing to GATPlayer's OnShouldMixData
		/// delegate, if your callback returns false,
		/// it is your responsibility to move the playhead.
		/// </summary>
		int NextIndex{ get; set; }
		
		/// <summary>
		/// The offset in the audio buffer. In most cases,
		/// 0 whenever IsFirstChunk is false. Used for sample 
		/// accurate playback( a sample may start in the 
		/// middle of the audio buffer ).
		/// </summary>
		int	OffsetInBuffer{ get; }
		
		/// <summary>
		/// A reference to a shared processing buffer,
		/// useful for non destructive processing: cache
		/// the chunk, process, mix from processing buffer.
		/// </summary>
		GATData ProcessingBuffer{ get; }
		
		/// <summary>
		/// If not null, mixing should be
		/// routed through Track. Must be checked
		/// when overriding default mixing: if not
		/// null, mix using Track.MixFrom, do not
		/// directly mix to the audio buffer.
		/// </summary>
		GATTrack Track{ get; }
		
		/// <summary>
		/// The gain specific to the sample - to be multiplied 
		/// by the track or the player's gain level.
		/// </summary>
		float PlayingGain{ get; }
		
		/// <summary>
		/// Caches the current chunk to the 
		/// processing buffer.
		/// </summary>
		void CacheToProcessingBuffer( int length );
	}
	
	public interface IGATBufferedSampleOptions
	{
		void SetEnd( int numSamples, int fadeLength );
	}
}

