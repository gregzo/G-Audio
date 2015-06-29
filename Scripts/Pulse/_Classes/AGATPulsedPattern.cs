//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Base class for pulsed patterns.
	/// Provides playing order options and 
	/// automatically handles OnPulse() so 
	/// that you only need to override PlaySample.
	/// </summary>
	[ ExecuteInEditMode ]
	public abstract class AGATPulsedPattern : AGATPulseClient 
	{
		/// <summary>
		/// Fired when a sample in the pattern is about to play.
		/// </summary>
		public delegate void OnPatternWillPlay( PatternSample sample, int indexInPattern, double dspTime );
		
		/// <summary>
		/// The delegate you may subscribe to to be notified when a pattern's sample is about to play.
		/// </summary>
		public OnPatternWillPlay onPatternWillPlay;
		
		/// <summary>
		/// The GATPlayer instance through which playback will be routed.
		/// By default, it is set to GATManager.DefaultPlayer
		/// </summary>
		/// <value>The player.</value>
		public GATPlayer Player
		{
			get{ return _player; }
			set
			{
				if( _player == value )
					return;
				
				_player = value;
				
				if( _player != null )
				{
					SubscribeToPulseIfNeeded();
				}
				else
				{
					UnsubscribeToPulse();
				}
			}
		}
		[ SerializeField ]
		protected GATPlayer _player;
		
		/// <summary>
		/// The sample bank from which to pull samples.
		/// </summary>
		public GATActiveSampleBank SampleBank
		{
			get{ return _sampleBank; }
			set
			{
				if( _sampleBank == value )
					return;
				
				_sampleBank = value;
				
				if( _sampleBank != null )
				{
					SubscribeToPulseIfNeeded();
				}
				else
				{
					UnsubscribeToPulse();
				}
			}
		}
		[ SerializeField ]
		protected GATActiveSampleBank _sampleBank;
		
		protected override bool CanSubscribeToPulse()
		{
			if( base.CanSubscribeToPulse() == false || _sampleBank == false || _player == null )
			{
				return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Which track in the player should playback be routed through
		/// by default.
		/// </summary>
		public int TrackNb
		{ 
			get{ return _trackNb; }
			set
			{
				if( _trackNb == value )
					return;
				
				if( _player == null || ( _player.NbOfTracks <= value ) )
					return;
				
				_trackNb = value;
			}
		}
		[ SerializeField ]
		protected int _trackNb;
		
		public enum PlayingOrder{ MapToPulseIndex, Sequential, Randomized, MapToMasterPulseIndex, Together }
		
		/// <summary>
		/// The order in which samples
		/// should be played on pulse.
		/// MapToPulseIndex maps to the
		/// MasterPulse even when subscribing
		/// to a subpulse.
		/// </summary>
		public PlayingOrder SamplesOrdering
		{
			get{ return _playingOrder; }
			set
			{
				if( _playingOrder == value )
					return;
				
				_playingOrder = value;
			}
		}
		[ SerializeField ]
		protected PlayingOrder _playingOrder;
		
		/// <summary>
		/// If true, a random delay will be added to
		/// pulses. Useful for humanizing, or to avoid 
		/// virtual frequencies at granular synthesis speeds.
		/// </summary>
		/// <value><c>true</c> if add random delay; otherwise, <c>false</c>.</value>
		public bool AddRandomDelay
		{
			get{ return _randomDelay; }
			set
			{
				if( _randomDelay == value )
					return;
				
				_randomDelay = value;
			}
		}
		[ SerializeField ]
		protected bool _randomDelay;
		
		/// <summary>
		/// The ratio of the pulse duration of the maximum random delay.
		/// Example: a value of .1 will cause a maximum delay of .1 seconds 
		/// at 60 bpm.
		/// </summary>
		/// <value>The random delay max ratio.</value>
		public float RandomDelayMaxRatio
		{
			get{ return _randomDelayMaxRatio; }
			set
			{
				if( _randomDelayMaxRatio == value )
					return;
				
				_randomDelayMaxRatio = value;
			}
		}
		[ SerializeField ]
		protected float _randomDelayMaxRatio;
		
		/// <summary>
		/// Should some pulses be randomly bypassed?
		/// </summary>
		public bool RandomBypass
		{
			get{ return _randomBypass; }
			set
			{
				if( _randomBypass == value )
					return;
				
				_randomBypass = value;
			}
		}
		[ SerializeField ]
		protected bool _randomBypass;
		
		/// <summary>
		/// The chance that an pulse may be bypassed if RandomBypass is true.
		/// 1f is 100%.
		/// </summary>
		/// <value>The random bypass chance.</value>
		public float RandomBypassChance
		{
			get{ return _randomBypassChance; }
			set
			{
				if( _randomBypassChance == value )
					return;
				
				_randomBypassChance = value;
			}
		}
		[ SerializeField ]
		protected float _randomBypassChance;
		
		private   int _sampleIndex = -1;
		protected int _sampleCount;
		
		public override void OnPulse( IGATPulseInfo pulseInfo )
		{
			if( _subscribedSteps[ pulseInfo.StepIndex ] == false )
				return;
			
			if( _randomBypass )
			{
				if( Random.value < _randomBypassChance )
					return;
			}
			
			UpdateIndex( pulseInfo );
			
			double dspTime = pulseInfo.PulseDspTime;
			
			if( _randomDelay )
				dspTime += ( double )Random.Range( 0f, _randomDelayMaxRatio ) * pulseInfo.PulseDuration;
			
			if( _playingOrder != PlayingOrder.Together )
			{
				PlaySample( _sampleIndex, dspTime );
			}
			else
			{
				int i;
				
				for( i = 0; i < _sampleCount; i++ ) //ToDo : Update sample count
				{
					PlaySample( i, dspTime );
				}
			}
		}
		
		protected override void Awake()
		{
			base.Awake();
			
			_sampleCount = UpdatedSampleCount();
			
			if( _player == null )
			{
				_player = GATManager.DefaultPlayer;
			}
		}
		
		/// <summary>
		/// All conditions for playing a sample have been met, you may do so here.
		/// </summary>
		public abstract void PlaySample( int index, double dspTime );
		
		/// <summary>
		/// How many samples in the pattern?
		/// </summary>
		protected abstract int UpdatedSampleCount();
		
		void UpdateIndex( IGATPulseInfo pulseInfo )
		{
			switch( _playingOrder )
			{
			case PlayingOrder.MapToPulseIndex:
				_sampleIndex = pulseInfo.StepIndex % _sampleCount;
				break;
				
			case PlayingOrder.Sequential:
				_sampleIndex = ( _sampleIndex + 1 ) % _sampleCount;
				break;
				
			case PlayingOrder.Randomized:
				_sampleIndex = Random.Range( 0, _sampleCount );
				break;
				
			case PlayingOrder.MapToMasterPulseIndex:
				_sampleIndex = pulseInfo.PulseSender.MasterPulseInfo.StepIndex;
				break;
			}
		}
	}
}

