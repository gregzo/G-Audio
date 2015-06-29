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
	/// This class enables pre-mixing of samples
	/// in the track's buffer, where effects and
	/// panning will then be applied to the pre-mixed
	/// mono audio. Tracks can be added to GATPlayer 
	/// instances via the GATPlayer.AddTrack methods 
	/// and shouldn't be instantiated directly.
	/// </summary>
	[ System.Serializable ]
	public class GATTrack : ScriptableObject, IGATFilterableStream, IGATAudioThreadStreamOwner
	{
		/// <summary>
		/// The id of the track in it's parent GATPlayer
		/// </summary>
		public int TrackNb{ get{ return _trackNb; } }
		[ SerializeField ]
		protected int _trackNb;
		
		/// <summary>
		/// The object which manages dynamic panning for the track.
		/// In most cases, you can pan tracks using SetGainForChannel or
		/// SetStereoPan methods.
		/// </summary>
		public GATDynamicPanInfo PanInfo{ get{ return _panInfo; } }
		protected GATDynamicPanInfo _panInfo;
		
		/// <summary>
		/// The mono buffer where pre-mixing occurs.
		/// </summary>
		public GATData TrackBuffer{ get{ return _trackBuffer; } }
		protected GATData _trackBuffer;
		
		/// <summary>
		/// Helper object which manages filters applied to the track.
		/// </summary>
		public GATFiltersHandler FiltersHandler{ get{ return _filtersHandler; } }
		[ SerializeField ]
		protected GATFiltersHandler _filtersHandler;
		
		/// <summary>
		/// Muting is smoothed: no pops.
		/// </summary>
		public bool Mute
		{
			get{ return _nextMute; }
			set
			{ 
				if( _nextMute != value )
				{
					_nextMute = value;
					_shouldToggleMute = true;
				}
			}
		}
		
		/// <summary>
		/// Convenience method for stereo panning
		/// </summary>
		public float StereoGain
		{
			get{ return _stereoGain; }
			set
			{
				if( value == _stereoGain )
					return;
				
				_stereoGain = value;
				_gains[ 0 ] = ( 1f - _stereoPan ) * _stereoGain;
				_gains[ 1 ] = _stereoPan * _stereoGain; 
				
				_panInfo.SetGains( _gains );
			}
		}
		
		/// <summary>
		/// Convenience method for stereo panning
		/// 0f is left, 1f right.
		/// </summary>
		public float StereoPan
		{
			get{ return _stereoPan; }
			set
			{
				if( value == _stereoPan )
					return;
				
				_stereoPan = value;
				_gains[ 0 ] = ( 1f - _stereoPan ) * _stereoGain;
				_gains[ 1 ] = _stereoPan * _stereoGain; 
				
				_panInfo.SetGains( _gains );
			}
		}
		
		/// <summary>
		/// Current gain for the specified channel.
		/// Returns -1f if the channel doesn't exist.
		/// </summary>
		public float GetGainForChannel( int channel )
		{
			if( channel >= _gains.Length )
				return -1f;
			
			return _gains[ channel ];
		}
		
		/// <summary>
		/// Set gain for the specified channel.
		/// Does nothing if the channel doesn't exist.
		/// </summary>
		public void SetGainForChannel( float gain, int channel )
		{
			if( channel >= _gains.Length )
				return;
			
			if( _gains[ channel ] == gain )
				return;
			
			_gains[ channel ] = gain;
			
			_panInfo.SetGainForChannel( gain, channel );
		}
		
		/// <summary>
		/// A track may have one IGATTrackContributor
		/// which will get the opportunity to mix directly
		/// to the track buffer before filters are applied.
		/// This method returns false if another contributor is already set. 
		/// </summary>
		public bool SubscribeContributor( IGATTrackContributor contributor )
		{
			if( _contributor == contributor )
				return true;
			
			if( _contributor != null )
				return false;
			
			_contributor = contributor;
			
			return true;
		}
		
		/// <summary>
		/// Unsubscribes the contributor. Returns false if
		/// the current contributor is different from the one making the call.
		/// </summary>
		public bool UnsubscribeContributor( IGATTrackContributor contributor )
		{
			if( _contributor == null )
				return true;
			
			if( _contributor != contributor )
				return false;
			
			_contributor = null;
			
			return true;
		}
		protected IGATTrackContributor _contributor;
		
		#region IGATAudioThreadStreamOwner Implementation
		
		public IGATAudioThreadStream GetAudioThreadStream( int index = 0 )
		{
			return _audioThreadStreamProxy;
		}
		
		int IGATAudioThreadStreamOwner.NbOfStreams{ get{ return 1; } }
		
		#endregion
		
		#region public but undocumented
		
		public static int  NbOfMixedSamples{ get; protected set; }
		
		public virtual void OnDisable()
		{
			_active = false;
			if( _player != null )
				_player.onPlayerWillMix -= PlayerWillBeginMixing;
		}
		
		public virtual void InitTrack( GATPlayer parentPlayer, int trackNb )
		{
			int i;
			_player = parentPlayer;
			_trackNb = trackNb;
			_filtersHandler = ScriptableObject.CreateInstance< GATFiltersHandler >();
			_filtersHandler.InitFiltersHandler( 1 ); //GATTracks are mono, panning occurs after filtering.
			_gains = new float[ GATInfo.NbOfChannels ];
			for( i = 0; i < _gains.Length; i++ )
			{
				_gains[ i ] = .5f;
			}
			OnEnable();
		}
		
		public void TrackNbDidChange( int newNb )
		{
			_trackNb = newNb;
		}
		
		
		// Called by GATPlayer.
		// Applies effects, and pan-mixes
		// to the audio buffer.
		public bool FXAndMixTo( float[] audioBuffer )
		{
			if( !_active )
				return false;
			
			int i;
			bool isEmptyData = false;
			
			if( _hasData == false ) //no sample overwrote the buffer, let's clear it
			{
				if( _bufferIsDirty )
				{
					_trackBuffer.Clear();
					_bufferIsDirty = false;
				}
				
				isEmptyData = true;
			}
			
			if( _contributor != null )
			{
				isEmptyData = !( _contributor.MixToTrack( _trackBuffer, _trackNb ) );
			}
			
			if( _filtersHandler.HasFilters )
			{
				if( _filtersHandler.ApplyFilters( _trackBuffer.ParentArray, _trackBuffer.MemOffset, GATInfo.AudioBufferSizePerChannel, isEmptyData ) )
				{
					isEmptyData  = false;
				}
			}
			
			if( isEmptyData )
			{
				_audioThreadStreamProxy.BroadcastStream( _trackBuffer.ParentArray, _trackBuffer.MemOffset, isEmptyData );
				return false;
			}
			
			if( _shouldToggleMute )
			{
				if( _nextMute )
				{
					_trackBuffer.FadeOut( GATInfo.AudioBufferSizePerChannel );
				}
				else
				{
					_trackBuffer.FadeIn( GATInfo.AudioBufferSizePerChannel );
					_mute = false;
				}
				
				_shouldToggleMute = false;
			}
			
			_bufferIsDirty = true;
			
			_audioThreadStreamProxy.BroadcastStream( _trackBuffer.ParentArray, _trackBuffer.MemOffset, false );

			if( _mute )
				return false;
			
			GATDynamicChannelGain channelGain;
			
			for( i = 0; i < _panInfo.channelGains.Count; i++ )
			{
				channelGain = _panInfo.channelGains[i];
				if( channelGain.ShouldInterpolate )
				{
					_trackBuffer.SmoothedGainMixToInterlaced( audioBuffer, 0, 0, GATInfo.AudioBufferSizePerChannel, channelGain );
					continue;
				}
				
				if( channelGain.Gain != 0f )
				{
					_trackBuffer.GainMixToInterlaced( audioBuffer, 0, 0, GATInfo.AudioBufferSizePerChannel, channelGain );
				}
			}
			
			if( _nextMute ) //only set mute here to allow for mixing of faded data : elegant stop
				_mute = true;
			
			return !isEmptyData;
		}
		
		/// <summary>
		/// Called by GATPlayer.
		/// Needed when overriding default mixing:
		/// If a sample is routed through a track,
		/// it should be mixed to it.
		/// </summary>
		public void MixFrom( GATData data, int index, int offsetInBuffer, int length, float gain = 1f )
		{	
			if( !_active )
				return;
			
			if( _bufferIsDirty ) //Contains old data
			{
				int lastIndex = offsetInBuffer + length;
				
				if( lastIndex < GATInfo.AudioBufferSizePerChannel || offsetInBuffer > 0 ) //if what we want to add to the buffer doesn't fill it, let's clear it first
				{
					_trackBuffer.Clear();
				}
				
				if( gain == 1f )
				{
					data.CopyTo( _trackBuffer, offsetInBuffer, index, length ); // no need to mix on dirty or empty data, simply copy
				}
				else
				{
					data.CopyGainTo( _trackBuffer, index, offsetInBuffer, length, gain );
				}
				
				_bufferIsDirty = false;
			}
			else
			{
				if( gain == 1f )
				{
					data.MixTo( _trackBuffer, offsetInBuffer, index, length );
				}
				else
				{
					data.GainMixTo( _trackBuffer, index, offsetInBuffer, length, gain );
				}
			}
			
			NbOfMixedSamples++;	
			_hasData = true;
		}
		
		#endregion
		
		#region protected and private
		[ SerializeField ]
		protected bool 	_nextMute;
		
		protected bool	_mute,
		_shouldToggleMute,
		_hasData,
		_bufferIsDirty = true;
		
		[ SerializeField ]
		protected GATPlayer _player;
		
		protected volatile bool _active;
		
		[ SerializeField ]
		float[] _gains; //Serialize here to avoid serializing GATPanInfo
		
		[ SerializeField ]
		protected float _stereoGain = 1f;
		
		[ SerializeField ]
		protected float _stereoPan = .5f;
		
		protected GATAudioThreadStreamProxy _audioThreadStreamProxy;
		
		protected virtual void OnEnable()
		{	
			if( GATInfo.NbOfChannels == 0 )
				return;

			if( _player != null ) //Object has just been deserialized, only setup transient objects
			{
				_panInfo = new GATDynamicPanInfo( _player, true );
				_panInfo.SetGains( _gains );
				
				_trackBuffer = GATManager.GetFixedDataContainer( GATInfo.AudioBufferSizePerChannel, "track"+TrackNb+" buffer" );
				_audioThreadStreamProxy = new GATAudioThreadStreamProxy( GATInfo.AudioBufferSizePerChannel, 1, _trackBuffer.GetPointer(), _trackBuffer.MemOffset, ( "Track " + _trackNb + " stream" ) );
				_player.onPlayerWillMix += PlayerWillBeginMixing;
				_mute = _nextMute; //only _nextMute is serialized
				_active = true;
			}
		}
		
		/// <summary>
		/// Call before disposing of a track permanently.
		/// </summary>
		void OnDestroy()
		{
			if( _panInfo != null )
				_panInfo.CleanUp();
			
			if( _filtersHandler != null )
			{
				if( Application.isPlaying )
				{
					Destroy( _filtersHandler );
				}
				else
				{
					DestroyImmediate( _filtersHandler );
				}
			}
		}
		
		void PlayerWillBeginMixing()
		{
			_hasData = false;
			NbOfMixedSamples = 0;
		}
		
		#endregion
		
		#region EDITOR_MEMBERS
		#if UNITY_EDITOR
		[ SerializeField ]
		protected bool _forcePerChannelControl;
		public bool ForcePerChannelControl
		{ 
			get{ return _forcePerChannelControl; } 
			set
			{ 
				if( value == _forcePerChannelControl )
					return;
				_forcePerChannelControl = value; 
				
				if( value == false )
				{
					_gains[ 0 ] = ( 1f - _stereoPan ) * _stereoGain;
					_gains[ 1 ] = _stereoPan * _stereoGain; 
					
					_panInfo.SetGains( _gains );
				}
			} 
		}
		#endif
		#endregion
	}
}

