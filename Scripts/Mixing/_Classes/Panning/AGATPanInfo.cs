//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GAudio
{
	/// <summary>
	/// Base class for handling
	/// panning of mono samples
	/// to interlaced multi channel
	/// data.
	/// </summary>
	public abstract class AGATPanInfo
	{
		/// <summary>
		/// Pans and adds the mono data of the sample to the provided buffer.
		/// </summary>
		public abstract void PanMixSample( IGATBufferedSample sample, int length, float[] audioBuffer, float gain = 1f );
		
		/// <summary>
		/// Pans and adds the mono data of the processing buffer to the provided buffer.
		/// </summary>
		public abstract void PanMixProcessingBuffer( IGATBufferedSample sample, int length, float[] audioBuffer, float gain = 1f );
		
		/// <summary>
		/// Sets all channels' gains simultaneously.
		/// The array of gains provided should have as many elements
		/// as there are channels.
		/// </summary>
		public abstract void SetGains( float[] gains );
		
		/// <summary>
		/// Convenience method for stereo panning
		/// Standard panning with gain split between channels
		/// </summary>
		/// <param name='pan'>
		/// Pan value between 0f and 1f.
		/// </param>
		public void SetStereoPan( float pan )
		{
			SetGains( new float[]{ 1f - pan, pan } ); 
		}
		
		/// <summary>
		/// overload which adds gain control.
		/// </summary>
		public void SetStereoPan( float pan, float gain )
		{
			SetGains( new float[]{ ( 1f - pan ) * gain, pan * gain } ); 
		}
	}
}


