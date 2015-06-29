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
	/// Use this class when you do not
	/// need a sample's gains per channel to
	/// change during playback. If you do need
	/// live pan control, use GATDynamicPanInfo instead.
	/// Note: GATPanInfo objects are only required when not
	/// routing playback through a GATTrack.
	/// Note 2: a single instance may be used to
	/// pan multiple concurrent samples.
	/// </summary>
	public class GATFixedPanInfo : AGATPanInfo
	{
		public readonly List< GATChannelGain > channelGains;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="GATFixedPanInfo"/> class.
		/// </summary>
		/// <param name='gains'>
		/// An array of gains indexed per channel. Silent channels
		/// should be represented by 0f gain values.
		/// </param>
		/// <exception cref='GATException'>
		/// Is thrown when the passed array's
		/// length does not match the number of 
		/// channels your project is setup to output.
		/// </exception>
		public GATFixedPanInfo( float[] gains )
		{
			if( gains.Length != GATInfo.NbOfChannels )
			{
				throw new GATException( "The array of gains should have as many items as there are channels. Use 0f gain in indexes where you need a silent channel." );
			}
			channelGains = new List<GATChannelGain>();
			SetGains( gains );
		}
		
		public GATFixedPanInfo()
		{
			channelGains = new List<GATChannelGain>();
		}
		
		/// <summary>
		/// Sets the gains for all
		/// channels. Can only be 
		/// set once.
		/// </summary>
		/// <param name='gains'>
		/// Gains per channel. Use 0f for
		/// silent channels.
		/// </param>
		/// <exception cref='GATException'>
		/// Is thrown when attempting to
		/// set gains more than once, or if
		/// the provided gains array length 
		/// does not match the number of channels 
		/// your project is setup to output.
		/// </exception>
		public override void SetGains( float[] gains )
		{
			if( channelGains.Count != 0 )
			{
				throw new GATException( "GATFixedPanInfo gains per channel can only be set once. Use GATDynamicPanInfo for dynamic panning." ); 
			}
			
			if( gains.Length != GATInfo.NbOfChannels )
			{
				throw new GATException( "The array of gains should have as many items as there are channels. Use 0f gain in indexes where you need a silent channel." );
			}
			
			for( int i = 0; i < GATInfo.NbOfChannels; i++ )
			{
				if( gains[i] != 0f )
				{
					channelGains.Add( new GATChannelGain( i, gains[i] ) );
				}
			}	
		}
		
		/// <summary>
		/// Called by GATPlayer to 
		/// mix to interlaced data 
		/// according to specified gains
		/// per channel.
		/// This method may be useful if
		/// you subscribe to GATPlayer's 
		/// mixing delegate and choose to
		/// override default mixing.
		/// </summary>
		public override void PanMixSample( IGATBufferedSample sample, int length, float[] audioBuffer, float gain = 1f )
		{
			int i;
			
			if( gain == 1f )
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					sample.AudioData.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i] );
				}
			}
			else
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					sample.AudioData.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i], gain );
				}
			}
		}
		
		/// <summary>
		/// This method may be used if
		/// you subscribe to GATPlayer's 
		/// mixing delegate and choose to
		/// override default mixing.
		/// </summary>
		public override void PanMixProcessingBuffer( IGATBufferedSample sample, int length, float[] audioBuffer, float gain = 1f )
		{
			int i;
			
			if( gain == 1f )
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					sample.ProcessingBuffer.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i] );
				}
			}
			else
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					sample.ProcessingBuffer.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i], gain );
				}
			}
		}
	}
}





