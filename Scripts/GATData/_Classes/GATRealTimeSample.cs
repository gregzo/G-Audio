using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GAudio
{
	/// <summary>
	/// Provides real time control over a playing sample.
	/// Filterable, resamplable, loopable.
	/// Negative pitches result in reverse in playback.
	/// </summary>
	public class GATRealTimeSample : AGATWrappedSample, IDisposable
	{
		public delegate bool SampleWillLoopHandler( GATRealTimeSample sample );
		
		protected SampleWillLoopHandler onSampleWillLoop;
		
		/// <summary>
		/// Get a callback whenever the sample is about to loop, wether in reverse or
		/// normal playback. Return false in the callback to stop looping. Default looping
		/// behaviour will seek to StartPosition, which you may adjust in the callback if needed.
		/// </summary>
		public void SetLoopCallback( SampleWillLoopHandler callback )
		{
			onSampleWillLoop = callback;
		}
		
		/// <summary>
		/// Gets or sets the resampling factor,
		/// should be strictly greater than 0.
		/// Note that this property has no effect
		/// if canPitchShift = false is specified
		/// when instantiating the class.
		/// </summary>
		public double Pitch
		{ 
			get
			{
				return _pitch;
			}
			set
			{
				if( Math.Abs( value ) < 0.01d )
				{
					value = 0.01d * Math.Sign( value );
				}
				_pitch = value;
			}
		}
		
		/// <summary>
		/// Enable / Disables looping behaviour.
		/// </summary>
		public bool Loop
		{
			get{ return _loop; }
			set{ _loop = value; }
		}
		
		/// <summary>
		/// The current read position in PCM samples.
		/// </summary>
		public int Position{ get{ return _dataSource.NextIndex; } } 
		
		/// <summary>
		/// The position to seek to when starting playback, and to 
		/// loop back to if loop is true. Must be non zero for reverse 
		/// playback to actually play anything.
		/// </summary>
		public int StartPosition{ get; set; }

		/// <summary>
		/// When set to true, playback will begin by a sample accurate fade in. Subsequent
		/// loops are not affected.
		/// </summary>
		public bool FadesIn{ get; set; }

		/// <summary>
		/// Duration of the fade in, in seconds.
		/// </summary>
		public double FadeInDuration{ get{ return _fadeInDuration; } set{ _fadeInDuration = value; } }
		
		#region Protected and Private members
		
		protected double _pitch = 1d;
		protected GATDataSource _dataSource;
		protected bool _loop;
		protected List< AGATMonoFilter > _filters = new List<AGATMonoFilter>();
		protected bool _disposed;
		protected bool _shouldFade;
		protected double _fadeInDuration = 1d;
		protected FadeInfo _scheduledFade;

		#endregion
		
		/// <summary>
		/// Wrap a GATData or IGATProcessedSample for
		/// real time control over playback.
		/// </summary>
		/// <param name='dataOwner'>
		/// GATData or IGATProcessedSample.
		/// </param>
		/// <param name='ipaninfo'>
		/// Specify a GATFixedPanInfo or GATDynamicPanInfo
		/// reference if you intend to play directly( not through a track ).
		/// </param>
		public GATRealTimeSample( IGATDataOwner dataOwner, AGATPanInfo ipaninfo = null ) : base( dataOwner, ipaninfo )
		{
			_dataSource = new GATDataSource( dataOwner == null ? null : dataOwner.AudioData );
			_fader 		= new Fader();
		}
		
		[ Obsolete( "Obsolete ctor: canPitchShift parameter is now obsolete. Please use GATLoopedSample if you need to monitor and smoothly stop playback without pitch shift." ) ]
		public GATRealTimeSample( IGATDataOwner dataOwner, bool canPitchShift, AGATPanInfo ipaninfo = null ) : base( dataOwner, ipaninfo )
		{
			// Note: for some reason, Unity's not picking up the Obsolete attribute... 
			Debug.LogWarning( "Obsolete ctor: canPitchShift parameter is now obsolete. Please use GATLoopedSample if you need to monitor and smoothly stop playback without pitch shift." );
			_dataSource = new GATDataSource( dataOwner.AudioData );
			_fader 		= new Fader();
		}

		/// <summary>
		/// Fades out and stops with sample level accuracy.
		/// Interrupts fading in.
		/// </summary>
		public void FadeOutAndStop( double fadeDuration )
		{
			if( this.PlayingStatus == Status.Playing )
			{
				if( _shouldFade ) //already fading, don't specify fromGain which will be adjusted according to current fade gain
				{
					_fader.SetFadeInfo( new FadeInfo( 0f, fadeDuration ) );
				}
				else // Not fading, fade out from 1f
				{
					_fader.SetFadeInfo( new FadeInfo( 1f, 0f, fadeDuration ) );
				}

				_shouldFade = true;
			}
		}

		public void ScheduleFadeOut( double fadeStartDspTime, double fadeDuration )
		{
			if( fadeStartDspTime < AudioSettings.dspTime )
			{
				FadeOutAndStop( fadeDuration );
#if GAT_DEBUG
				Debug.Log( "Cannot schedule fade earlier than current dspTime! Fading out now." );
#endif
				return;
			}

			_scheduledFade = new FadeInfo( 1f, 0f, fadeDuration, fadeStartDspTime );
		}
		
		/// <summary>
		/// Adds a filter of the specified type.
		/// Returns a reference to the AGATMonoFilter base
		/// class, which should be casted to the requested type.
		/// </summary>
		public AGATMonoFilter AddFilter<T>() where T : AGATMonoFilter
		{

			AGATMonoFilter filter = ScriptableObject.CreateInstance< T >();
			_filters.Add ( filter );
			return filter;
		}

		/// <summary>
		/// Inserts a filter of the specified type.
		/// Returns a reference to the AGATMonoFilter base
		/// class, which should be casted to the requested type.
		/// </summary>
		public AGATMonoFilter AddFilter<T>( int index ) where T : AGATMonoFilter
		{
			if( index > _filters.Count )
				index = _filters.Count;

			AGATMonoFilter filter = ScriptableObject.CreateInstance< T >();
			_filters.Insert( index, filter );
			return filter;
		}

		/// <summary>
		/// Returns the filter at index, null if there is none. 
		/// </summary>
		public AGATMonoFilter GetFilter( int index )
		{
			if( index >= _filters.Count )
				return null;

			return _filters[ index ];
		}

		/// <summary>
		/// Resets all filters' state. 
		/// </summary>
		public void ResetFilters()
		{
			int i;
			
			for( i = 0; i < _filters.Count; i++ )
			{
				_filters[ i ].ResetFilter();
			}
		}
		
		/// <summary>
		/// Removes the specified filter.
		/// </summary>
		public void RemoveFilter( AGATMonoFilter filter )
		{
			_filters.Remove( filter );
		}
		
		/// <summary>
		/// Seek to the specified PCM position.
		/// If the sample is not playing, this will
		/// also set StartPosition.
		/// </summary>
		public void Seek( int samplePos )
		{
			_dataSource.Seek( samplePos );
			
			if( PlayingStatus == Status.ReadyToPlay )
			{
				StartPosition = samplePos;
			}
		}
		
		/// <summary>
		/// Sets the data to play. Use for recycling 
		/// GATRealTimeSample objects. If the sample is currently playing or scheduled,
		/// this method has no effect.
		/// </summary>
		public void SetData( IGATDataOwner dataOwner )
		{
			if( _dataOwner == dataOwner )
				return;
		
			if( PlayingStatus != Status.ReadyToPlay )
			{
				#if UNITY_EDITOR
				Debug.LogWarning( "GATRealTimeSample.SetData ignored: sample is already playing or scheduled." );
				#endif
				return;
			}

			_dataOwner = dataOwner;
			_dataSource.SetData( _dataOwner.AudioData );
		}
		
		protected override bool PlayerWillMixSample( IGATBufferedSample sample, int length, float[] audioBuffer )
		{	
			int processedSamples;
			int i;
			double dspTime = AudioSettings.dspTime;
			
			if( sample.IsFirstChunk )
			{
				PlayingStatus = Status.Playing;
				sample.NextIndex = 1;

				if( _pitch < 0d )
				{
					if( StartPosition == 0 )
						StartPosition = _dataOwner.AudioData.Count - 2;
				}
				else
				{
					if( StartPosition ==  _dataOwner.AudioData.Count - 2 )
						StartPosition = 0;
				}

				_dataSource.Seek( StartPosition );

				if( FadesIn && _fadeInDuration > 0d )
				{
					_fader.SetFadeInfo( new FadeInfo( 0f, 1f, _fadeInDuration ) );
					_shouldFade = true;
				}
			}
			else if( _scheduledFade != null && _shouldFade == false )
			{
				if( dspTime + GATInfo.AudioBufferDuration > _scheduledFade.StartDspTime )
				{
					_fader.SetFadeInfo( _scheduledFade );
					_shouldFade = true;
					_scheduledFade = null;
				}
			}

			if( _pitch == 1d )
			{
				processedSamples = _dataSource.GetData( sample.ProcessingBuffer, length, 0, false );
			}
			else if( _pitch == -1d )
			{
				processedSamples = _dataSource.GetData( sample.ProcessingBuffer, length, 0, true );
			}
			else
			{
				processedSamples = _dataSource.GetResampledData( sample.ProcessingBuffer, length, 0, _pitch );
			}

			
			if( StopsEarly )
			{
				if( dspTime >= _endDspTime )
					_shouldStop = true;
			}
			
			if( processedSamples < length )
			{
				if( _loop )
				{
					if( onSampleWillLoop != null )
					{
						if( onSampleWillLoop( this ) == false )
						{
							sample.IsLastChunk = true;
							goto Processing;
						}
					}
					
					int seekPos;
					
					if( _pitch < 0d )
					{
						seekPos = StartPosition == 0 ? _dataOwner.AudioData.Count - 2 : StartPosition;
					}
					else 
					{
						seekPos = StartPosition >= _dataOwner.AudioData.Count - 2 ? 0 : StartPosition;
					}
					_dataSource.Seek( seekPos );
					_dataSource.GetResampledData( sample.ProcessingBuffer, length - processedSamples, processedSamples, _pitch );
					
					processedSamples = length;
				}
				else
				{
					sample.IsLastChunk = true;
				}
			}
			else if( _shouldStop )
			{
				sample.ProcessingBuffer.FadeOut( 0, length );
				sample.IsLastChunk = true;
				_shouldStop = false;
			}
			
		Processing:
			
			if( _shouldFade )
			{
				if( sample.IsFirstChunk )
				{
					dspTime += ( double )sample.OffsetInBuffer / GATInfo.OutputSampleRate;
				}

				int fadedSamples = _fader.DoFade( sample.ProcessingBuffer, dspTime, processedSamples );

				if( fadedSamples < processedSamples )//end of fade
				{
					if( _fader.ToGain == 0f ) //Faded out, don't mix all!
					{
						sample.IsLastChunk = true;
						processedSamples = fadedSamples;
					}
				
					_shouldFade = false;
				}
			}
			
				
			for( i = 0; i < _filters.Count; i++ )
			{
				_filters[ i ].ProcessChunk( sample.ProcessingBuffer.ParentArray, sample.ProcessingBuffer.MemOffset, processedSamples, false );
			}
			
			if( ReferenceEquals( sample.Track, null ) == false ) //sample is played in a track, which will handle Panning via it's own processing buffer. Copy data to the tracks buffer:
			{
				sample.Track.MixFrom( sample.ProcessingBuffer, 0, sample.OffsetInBuffer, processedSamples, sample.PlayingGain );
			}
			else
			{
				sample.PanInfo.PanMixProcessingBuffer( sample, processedSamples, audioBuffer, sample.PlayingGain );
			}
			
			if( sample.IsLastChunk )
			{
				PlayingStatus = Status.ReadyToPlay;
			}
			
			return false;
		}
		
		#region IDisposable Implementation
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		protected virtual void Dispose( bool explicitly )
		{
			if (_disposed)
				return;

			if( explicitly )
			{
				_dataSource.Dispose();
			}
			
			int i;
			
			for( i = 0; i < _filters.Count; i++ )
			{
				ScriptableObject.Destroy( _filters[ i ] );
			}
			
			_disposed = true;
		}
		
		~GATRealTimeSample()
		{
			Dispose( false );
		}
		
		#endregion

		Fader _fader; 

		public class FadeInfo
		{
			public readonly float FromGain, ToGain;
			public readonly double StartDspTime, Duration;

			public FadeInfo( float toGain, double duration )
			{
				FromGain 		= -1f;
				ToGain 			= toGain;
				Duration 		= duration;
				StartDspTime 	= 0d;
			}

			public FadeInfo( float fromGain, float toGain, double duration, double startDspTime = 0d )
			{
				FromGain 		= fromGain;
				ToGain 			= toGain;
				Duration 		= duration;
				StartDspTime 	= startDspTime;
			}
		}

		class Fader
		{
			public float ToGain{ get{ return _toGain; } }
			public float LastGain{ get{ return _lastGain; } }

			float 	_fromGain, 
					_toGain, 
					_lastGain;

			double 	_startDspTime, 
					_duration, 
					_fadeEndDspTime;

			public Fader()
			{
				_lastGain = 1f;
			}

			public Fader( FadeInfo info )
			{
				_lastGain = 1f;
				SetFadeInfo( info );
			}


			public void SetFadeInfo( FadeInfo info )
			{
				if( info.FromGain < 0f )
				{
					_fromGain = _lastGain;
				}
				else
				{
					_fromGain = info.FromGain;
				}

				_toGain 		= info.ToGain;
				_duration 		= info.Duration;
				_startDspTime 	= info.StartDspTime;
				_fadeEndDspTime	= _startDspTime + _duration;
			}

			public int DoFade( GATData target, double dspTime, int lengthInSamples )
			{
				int offset = 0;
				double chunkDuration;
				float lerpVal;
				float fromGain, toGain;

				if( _startDspTime == 0d )
				{
					_startDspTime = dspTime;
					_fadeEndDspTime = dspTime + _duration;
				}
				else if( dspTime < _startDspTime ) // fade out start, don't adjust length in samples: we will return it to signal fade hasn't finished
				{
					offset = ( int )( ( dspTime - _startDspTime ) * GATInfo.OutputSampleRate );
				}
		
				chunkDuration = ( ( double )lengthInSamples - offset ) / GATInfo.OutputSampleRate;

				if( chunkDuration + dspTime > _fadeEndDspTime )
				{
					chunkDuration = _fadeEndDspTime - dspTime;

					if( chunkDuration <= 0d )
						return 0;

					lengthInSamples = ( int )( chunkDuration * GATInfo.OutputSampleRate );
				}

				lerpVal = ( float )( ( dspTime - _startDspTime ) / _duration );
				fromGain = Mathf.Lerp( _fromGain, _toGain, lerpVal );

				dspTime += chunkDuration;

				lerpVal = ( float )( ( dspTime - _startDspTime ) / _duration );

				toGain = Mathf.Lerp( _fromGain, _toGain, lerpVal );

				target.Fade( fromGain, toGain, lengthInSamples - offset, offset );

				_lastGain = toGain;

				return lengthInSamples;
			}
		}
	}
}

