//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Base class for playback of wrapped GATData and IGATProcessedSample objects
	/// </summary>
	public abstract class AGATWrappedSample 
	{
		public enum Status{ ReadyToPlay, Scheduled, Playing };
		
		/// <summary>
		/// The current playing status of the sample.
		/// </summary>
		public Status PlayingStatus{ get; protected set; }
		
		/// <summary>
		/// Use to manipulate gain per channel
		/// or stereo pan when not playing through 
		/// a track. Requires a GATDynamicPanInfo 
		/// reference to be passed when instantiating the class.
		/// </summary>
		public readonly AGATPanInfo panInfo;
		
		/// <summary>
		/// Set this flag to true to automatically stop playback 
		/// after MaxDuration seconds. Count starts at the precise
		/// dsp start time of the sample.
		public bool StopsEarly{ get; set; }
		
		/// <summary>
		/// If StopsEarly is true, the duration after which
		/// playback will automatically stop ( smoothly! ).
		/// </summary>
		public double MaxDuration
		{
			get;
			set;
		}
		
		protected bool _shouldStop;
		
		protected IGATDataOwner _dataOwner;
		protected double _endDspTime;
		
		/// <summary>
		/// Data owners include IGATProcessedSample and GATData obects.
		/// Specify a GATDynamicPanInfo or GATFixedPanInfo object to
		/// enable routing playback directly to the player ( not through tracks ).
		/// </summary>
		public AGATWrappedSample( IGATDataOwner dataOwner, AGATPanInfo ipaninfo = null )
		{
			panInfo 	= ipaninfo;
			_dataOwner 	= dataOwner;
		}
		
		/// <summary>
		/// Smoothly stops playback by fading out
		/// the audio data in audio buffer size
		/// ( non destructive, data is buffered
		/// before the fade ).
		/// </summary>
		public void ElegantStop()
		{
			_shouldStop = true;
		}
		
		/// <summary>
		/// Plays the sample directly through the Default Player.
		/// If no AGATPanInfo reference was specified when creating the instance,
		/// doesn't do anything.
		/// </summary>
		public void PlayPanned( float gain = 1f )
		{
			if( panInfo == null )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "No panInfo set!" );
				return;
				#endif
			}
			
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			
			GATManager.DefaultPlayer.PlayData( _dataOwner.AudioData, panInfo, gain, PlayerWillMixSample );
			_endDspTime = AudioSettings.dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample directly through the specified player
		/// If no AGATPanInfo reference was specified when creating the instance,
		/// doesn't do anything.
		/// </summary>
		public void PlayPanned( GATPlayer player, float gain = 1f )
		{
			if( panInfo == null )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "No panInfo set!" );
				return;
				#endif
			}
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			
			player.PlayData( _dataOwner.AudioData, panInfo, gain, PlayerWillMixSample );
			_endDspTime = AudioSettings.dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample through the default player's track trackNb
		/// </summary>
		public void PlayThroughTrack( int trackNb, float gain = 1f )
		{
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			GATManager.DefaultPlayer.PlayData( _dataOwner.AudioData, trackNb, gain, PlayerWillMixSample );
			_endDspTime = AudioSettings.dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample through the specified player's track trackNb
		/// </summary>
		public void PlayThroughTrack( GATPlayer player, int trackNb, float gain = 1f )
		{
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			player.PlayData( _dataOwner.AudioData, trackNb, gain, PlayerWillMixSample );
			_endDspTime = AudioSettings.dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample directly through the Default Player.
		/// If no AGATPanInfo reference was specified when creating the instance,
		/// doesn't do anything.
		/// </summary>
		public void PlayScheduled( double dspTime, float gain = 1f )
		{
			if( panInfo == null )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "No panInfo set!" );
				return;
				#endif
			}
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			GATManager.DefaultPlayer.PlayDataScheduled( _dataOwner.AudioData, dspTime, panInfo, gain, PlayerWillMixSample );
			_endDspTime = dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample directly through the specified player.
		/// If no AGATPanInfo reference was specified when creating the instance,
		/// doesn't do anything.
		/// </summary>
		public void PlayScheduled( GATPlayer player, double dspTime, float gain = 1f )
		{
			if( panInfo == null )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "No panInfo set!" );
				return;
				#endif
			}
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			player.PlayDataScheduled( _dataOwner.AudioData, dspTime, panInfo, gain, PlayerWillMixSample );
			_endDspTime = dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample through the default player's track trackNb
		/// </summary>
		public void PlayScheduledThroughTrack( double dspTime, int trackNb, float gain = 1f )
		{
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			GATManager.DefaultPlayer.PlayDataScheduled( _dataOwner.AudioData, dspTime, trackNb, gain, PlayerWillMixSample );
			_endDspTime = dspTime + MaxDuration;
		}
		
		/// <summary>
		/// Plays the sample through the specified player's track trackNb
		/// </summary>
		public void PlayScheduledThroughTrack( GATPlayer player, double dspTime, int trackNb, float gain = 1f )
		{
			if( PlayingStatus != Status.ReadyToPlay )
				return;
			
			PlayingStatus = Status.Scheduled;
			player.PlayDataScheduled( _dataOwner.AudioData, dspTime, trackNb, gain, PlayerWillMixSample );
			_endDspTime = dspTime + MaxDuration;
		}
		
		protected abstract bool PlayerWillMixSample( IGATBufferedSample sample, int length, float[] audioBuffer );
	}
}

