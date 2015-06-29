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
	/// Use this class when you do
	/// need a sample's gains per channel to
	/// change during playback. If you do not need
	/// live pan control, use GATFixedPanInfo instead.
	/// Note: GATPanInfo objects are only required when not
	/// routing playback through a GATTrack.
	/// Note 2: a single instance may pan
	/// multiple concurrent samples.
	/// </summary>
	public class GATDynamicPanInfo : AGATPanInfo
	{
		/// <summary>
		/// Instances subscribe to GATPlayer delegates.
		/// You can deactivate an instance if you need
		/// to reuse it later: it will unsubscribe when
		/// not active.
		/// </summary>
		public bool Active
		{
			get{ return _active; }
			set
			{
				if( _active == value )
					return;
				
				if( value )
				{
					_player.onPlayerWillMix += PlayerWillMix;
					_player.onPlayerDidMix  += PlayerDidMix;
				}
				else
				{
					_player.onPlayerWillMix -= PlayerWillMix;
					_player.onPlayerDidMix  -= PlayerDidMix;
				}
				
				_active = value;
			}
		}
		
		public readonly List< GATDynamicChannelGain > channelGains;
		
		#region Private Members
		GATDynamicChannelGain[] _indexedChannelGains;
		GATPlayer				_player;
		
		bool _needsUpdate,
		_active;
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="GATDynamicPanInfo"/> class.
		/// </summary>
		/// <param name='player'>
		/// Instances need to subscribe to GATPlayer delegates in order to 
		/// pan multiple samples at once.
		/// </param>
		public GATDynamicPanInfo( GATPlayer player, bool startsActive = true )
		{
			channelGains = new List<GATDynamicChannelGain>( GATInfo.NbOfChannels );
			_indexedChannelGains = new GATDynamicChannelGain[ GATInfo.NbOfChannels ];
			
			_player = player;
			Active = startsActive;
		}
		
		/// <summary>
		/// Sets gain for specified channel.
		/// If the gain delta is too large, 
		/// gain will be smoothed over the
		/// length of the audio buffer to
		/// prevent crackling.
		/// Does nothing if the specified channel does not exist.
		/// </summary>
		public void SetGainForChannel( float gain, int channel )
		{
			if( channel >= _indexedChannelGains.Length ) 
			{
				return;
			}
			GATDynamicChannelGain channelGain = _indexedChannelGains[ channel ];
			
			if( channelGain == null )
			{
				_indexedChannelGains[ channel ] = new GATDynamicChannelGain( channel, gain );
				channelGains.Add ( _indexedChannelGains[ channel ] );
			}
			else
			{
				channelGain.NextGain = gain;
			}
			
			_needsUpdate = true;
		}
		
		/// <summary>
		/// Gets current gain for the specified channel.
		/// Returns -1f if the channel does not exist.
		/// </summary>
		public float GetGainForChannel( int channel )
		{
			if( _indexedChannelGains[ channel ] == null )
				return -1f;
			
			return _indexedChannelGains[ channel ].Gain;
		}
		
		/// <summary>
		/// Sets gains for all channels.
		/// If the gain delta is too large, 
		/// gain will be smoothed over the
		/// length of the audio buffer to
		/// prevent crackling.
		/// </summary>
		public override void SetGains( float[] gains )
		{
			int i;
			
			for( i = 0; i < gains.Length; i++ )
			{
				SetGainForChannel( gains[i], i );
			}
		}
		
		/// <summary>
		/// Call when you do not need the 
		/// object anymore. Will unsubscribe from
		/// GATPlayer delegates.
		/// </summary>
		public void CleanUp()
		{
			Active = false;
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
					if(  channelGains[i].ShouldInterpolate && !sample.IsFirstChunk )
					{
						sample.AudioData.SmoothedGainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i] );
						continue;
					}
					
					if( channelGains[i].Gain != 0f )
					{
						sample.AudioData.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i] );
					}
				}
			}
			else
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					if(  channelGains[i].ShouldInterpolate && !sample.IsFirstChunk )
					{
						sample.AudioData.SmoothedGainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i], gain );
						continue;
					}
					
					if( channelGains[i].Gain != 0f )
					{
						sample.AudioData.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, sample.NextIndex, length, channelGains[i], gain );
					}
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
					if( channelGains[i].ShouldInterpolate && !sample.IsFirstChunk )
					{
						sample.ProcessingBuffer.SmoothedGainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i] );
						continue;
					}
					
					if( channelGains[i].Gain != 0f )
					{
						sample.ProcessingBuffer.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i] );
					}
				}
			}
			else
			{
				for( i = 0; i < channelGains.Count; i++ )
				{
					if( channelGains[i].ShouldInterpolate && !sample.IsFirstChunk )
					{
						sample.ProcessingBuffer.SmoothedGainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i], gain );
						continue;
					}
					
					if( channelGains[i].Gain != 0f )
					{
						sample.ProcessingBuffer.GainMixToInterlaced( audioBuffer, sample.OffsetInBuffer * GATInfo.NbOfChannels, 0, length, channelGains[i], gain );
					}
				}
			}
		}
		
		#region Private Methods
		
		void PlayerWillMix()
		{
			int i;
			
			if( !_needsUpdate )
				return;
			
			for( i = 0; i < channelGains.Count; i++ )
			{
				channelGains[i].PlayerWillMix();
			}
			
			_needsUpdate = false;
		}
		
		void PlayerDidMix()
		{
			int i;
			
			for( i = 0; i < channelGains.Count; i++ )
			{
				channelGains[i].PlayerDidMix();
			}
		}
		
		#endregion
	}
}

