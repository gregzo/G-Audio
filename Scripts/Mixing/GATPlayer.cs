//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR 
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// G-Audio's player.
	/// Mixes all samples to the audio buffer
	/// via OnAudioFilterRead and provides 
	/// per sample mixing callbacks upon request.
	/// Normal Unity filters can be added after
	/// this component, or an AudioReverbZone component
	/// for Unity Free users. 
	/// </summary>
	[ RequireComponent( typeof( AudioSource ) ) ]
	[ ExecuteInEditMode ]
	public sealed class GATPlayer : MonoBehaviour, IGATFilterableStream, IGATAudioThreadStreamOwner
	{
		
		//**************************************************************************************************************
		//******************************** Playing Methods *************************************************************
		
		/// <summary>
		/// Plays the sample through the specified track.
		/// </summary>
		public IGATBufferedSampleOptions PlayData( GATData sample, int trackNb, float gain = 1f, OnShouldMixSample mixCallback = null )
		{
			sample.Retain();
			BufferedSample newSample = GetBufferedSample();
			newSample.Init( sample, _tracks[ trackNb ], mixCallback, gain );
			
			lock( _samplesToEnqueue )
			{
				_samplesToEnqueue.Enqueue( newSample );
			}
			
			return newSample;
		}
		
		/// <summary>
		/// Plays the sample directly.
		/// Panning may be controlled through the provided
		/// AGATPanInfo instance. 
		/// </summary>
		public IGATBufferedSampleOptions PlayData( GATData sample, AGATPanInfo panInfo, float gain = 1f, OnShouldMixSample mixCallback = null )
		{
			sample.Retain();
			BufferedSample newSample = GetBufferedSample();
			
			newSample.Init( sample, panInfo, mixCallback, gain );
			
			lock( _samplesToEnqueue )
			{
				_samplesToEnqueue.Enqueue( newSample );
			}
			
			return newSample;
		}
		
		/// <summary>
		/// Plays the sample through the specified track,
		/// at sample accurate dspTime. Note that dspTime must be greater or equal 
		/// to AudioSettings.dspTime + GATInfo.AudioBufferDuration, or the sample will not be 
		/// added to the playing queue.
		/// </summary>
		public IGATBufferedSampleOptions PlayDataScheduled( GATData sample, double dspTime, int trackNb, float gain = 1f, OnShouldMixSample mixCallback = null )
		{
			if( dspTime < AudioSettings.dspTime + GATInfo.AudioBufferDuration )// Next buffer dspTime
			{
				#if GAT_DEBUG
				Debug.LogWarning( "cannot play at such short notice." );
				#endif
				return BufferedSample.VoidOptions;
			}
			
			sample.Retain();
			BufferedSample newSample = GetBufferedSample();
			newSample.scheduledDspTime = dspTime;
			
			newSample.Init( sample, _tracks[ trackNb ], mixCallback, gain );
			
			lock( _scheduledSamples )
			{
				_scheduledSamples.Enqueue( newSample );
			}
			
			return newSample;
		}
		
		/// <summary>
		/// Plays the sample directly.
		/// Panning may be controlled through the provided
		/// AGATPanInfo instance. 
		/// Note that dspTime must be greater or equal 
		/// to AudioSettings.dspTime + GATInfo.AudioBufferDuration, or the sample will not be 
		/// added to the playing queue.
		/// </summary>
		public IGATBufferedSampleOptions PlayDataScheduled( GATData sample, double dspTime, AGATPanInfo panInfo, float gain = 1f, OnShouldMixSample mixCallback = null )
		{
			if( dspTime < AudioSettings.dspTime + GATInfo.AudioBufferDuration )//Next buffer dspTime
			{
				#if GAT_DEBUG
				Debug.LogWarning( "cannot play at such short notice." );
				#endif
				return BufferedSample.VoidOptions;
			}
			sample.Retain();
			BufferedSample newSample = GetBufferedSample();
			newSample.scheduledDspTime = dspTime;
			
			newSample.Init( sample, panInfo, mixCallback, gain );
			
			lock( _scheduledSamples )
			{
				_scheduledSamples.Enqueue( newSample );
			}
			
			return newSample;
		}
		
		/// <summary>
		/// Removed all scheduled samples from the queue.
		/// Note that if you called Retain() on the GATData
		/// objects before scheduling playback, you will still need
		/// to call Release() to free memory if it is managed by G-Audio.
		/// </summary>
		public void ClearScheduledSamples()
		{
			lock( _scheduledSamples )
			{
				_scheduledSamples.ReleaseAllAndPool( this );
			}
		}
		
		/// <summary>
		/// Removed all samples from the playing queue and fades the
		/// audio buffer to avoid pops.
		/// Note that if you called Retain() on the GATData
		/// objects before requesting playback, you will still need
		/// to call Release() to free memory if it is managed by G-Audio.
		public void ClearPlayingSamples()
		{
			lock( _samplesToEnqueue )
			{
				_samplesToEnqueue.ReleaseAllAndPool( this );
			}
			_releasePlaying = true;
		}
		
		//**************************************************************************************************************
		//******************************** Filters Properties **********************************************************
		
		/// <summary>
		/// Gain control for master output
		/// </summary>
		public float Gain
		{
			get{ return _Gain; }
			set
			{
				if( value == _Gain )
					return;
				
				_Gain = value;
				
				if( _GainFilter != null )
				{
					_GainFilter.Gain = value;
				}
			}
		}
		
		/// <summary>
		/// If true, the master output will be hard clipped
		/// at ClipThreshold
		/// </summary>
		public bool Clip
		{
			get{ return _DoClip; }
			set
			{
				if( value == _DoClip )
					return;
				
				_DoClip = value;
				
				if( _GainFilter != null )
				{
					_GainFilter.Clip = value;
				}
			}
		}
		
		/// <summary>
		/// Hard clipping threshold
		/// </summary>
		public float ClipThreshold
		{
			get{ return _ClipThreshold; }
			set
			{
				if( value == _ClipThreshold )
					return;
				
				_ClipThreshold = value;
				
				if( _GainFilter != null )
				{
					_GainFilter.Threshold = value;
				}
			}
		}
		
		/// <summary>
		/// If clipMix is set to true, returns
		/// the number of values that were clipped
		/// in the last buffer.
		/// </summary>
		public int NbOfClippedSamples
		{  
			get
			{
				if( _GainFilter == null )
					return 0;
				
				return _GainFilter.NbOfClippedSamples;
			}
		}
		
		/// <summary>
		/// The helper object which habdles filters for the player.
		/// </summary>
		public GATFiltersHandler FiltersHandler
		{ 
			get
			{ 
				return _FiltersHandler; 
			} 
		}
		
		
		//**************************************************************************************************************
		//******************************** Track Methods and Properties ************************************************
		
		/// <summary>
		/// Grabs a reference to a GATTrack instance.
		/// Returns null if no track is found at the provided index.
		/// </summary>
		public GATTrack GetTrack( int trackIndex )
		{
			if( trackIndex >= _tracks.Count )
				return null;
			
			return _tracks[ trackIndex ];
		}
		
		/// <summary>
		/// The number of tracks mixed to this player.
		/// </summary>
		public int NbOfTracks{ get{ return _tracks.Count; } }
		
		/// <summary>
		/// Adds a GATTrack or derived to the player.
		/// </summary>
		public T AddTrack < T >() where T : GATTrack
		{
			T track = ScriptableObject.CreateInstance< T >();
			track.InitTrack( this, _tracks.Count );
			_tracks.Add ( track );
			
			return track;
		}
		
		/// <summary>
		/// Removes and destroys a specific track
		/// </summary>
		public void DeleteTrack( GATTrack track )
		{
			int i;
			int indexToRemove = -1;
			
			for( i = 0; i < _tracks.Count; i++ )
			{
				if( track == _tracks[ i ] )
				{
					indexToRemove = i;
					break;
				}      
			}
			
			if( indexToRemove == -1 )
				return;
			
			_tracks.RemoveAt( indexToRemove );
			
			if( Application.isPlaying )
			{
				Destroy( track );
			}
			else
			{
				DestroyImmediate( track );
			}
			
			if( _tracks.Count > indexToRemove )
			{
				for( i = indexToRemove; i < _tracks.Count; i++ )
				{
					_tracks[ i ].TrackNbDidChange( i );
				}
			}
		}
		
		/// <summary>
		/// Removes and destroys all tracks.
		/// </summary>
		public void ClearTracks()
		{
			if( Application.isPlaying )
			{
				foreach( GATTrack track in _tracks )
				{
					Destroy( track );
				}
			}
			else
			{
				foreach( GATTrack track in _tracks )
				{
					DestroyImmediate( track );
				}
			}
			
			_tracks.Clear();
		}
		
		#region IGATAudioThreadStreamOwner Implementation
		
		public IGATAudioThreadStream GetAudioThreadStream( int index )
		{
			return _audioThreadStreamProxy;
		}
		
		int IGATAudioThreadStreamOwner.NbOfStreams{ get{ return 1; } }
		
		#endregion
		
		#region Public Delegates
		public delegate bool OnShouldMixSample( IGATBufferedSample bufferedSample, int length, float[] audioBuffer );
		public delegate void OnPlayerAction();
		
		public				 OnPlayerAction onPlayerWillMix;
		public				 OnPlayerAction onPlayerDidMix;
		#endregion
		
		#region Private Members
		
		SampleQueue _scheduledSamples,
		_samplesToEnqueue,
		_discardedSamples;
		
		PlayingSamplesQueue _playingSamples;
		
		Stack< BufferedSample > _pool;
		
		private  GATAudioThreadStreamProxy _audioThreadStreamProxy;
		
		volatile bool _releasePlaying;
		
		//***************************************************
		//**************** Serialized Fields ****************
		[SerializeField]
		List< GATTrack > _tracks = new List<GATTrack>( 4 );
		[ SerializeField ]
		private GATFiltersHandler _FiltersHandler;
		[ SerializeField ]
		private GATGainFilter _GainFilter; 
		[ SerializeField ]
		private float _Gain = 1f;
		[ SerializeField ]
		private bool _DoClip = true;
		[ SerializeField ]
		private float _ClipThreshold = 1f;
		
		#endregion
		
		#region Private Methods
		
		void Awake()
		{
			AudioSource audio = GetComponent< AudioSource >();
			audio.playOnAwake = false;

			#if UNITY_5
			if( audio.clip != null )
			{
				audio.clip = null;
				#if GAT_DEBUG
				Debug.LogWarning( "As of Unity 5, GATPlayer's AudioSource's clip should be null" );
				#endif
			}
#else
			if( GATManager.UniqueInstance.SupportedSampleRates == GATManager.SampleRatesSupport.All )
			{
				if( audio.clip == null || audio.clip.frequency != GATInfo.OutputSampleRate )
				{
					if( audio.clip != null )
					{
						#if UNITY_EDITOR
						if( Application.isPlaying )
						{
							Destroy ( audio.clip );
						}
						else DestroyImmediate( audio.clip );
						
						#else
						Destroy ( audio.clip );
						#endif
					}
					audio.clip = AudioClip.Create( "G-Audio", 1, GATInfo.NbOfChannels, GATInfo.OutputSampleRate, true, false );
				}
			}
			else
			{
				if( GATInfo.OutputSampleRate != 44100 )
				{
					Debug.LogError( "Supported sample rate setting is set to Only44100, but current output sample rate is " + GATInfo.OutputSampleRate + ". Disabling player." );
					this.enabled = false;
				}
				else if( audio.clip != null )
				{
					#if UNITY_EDITOR
					if( Application.isPlaying )
					{
						Destroy ( audio.clip );
					}
					else DestroyImmediate( audio.clip );
					
					#else
					Destroy ( audio.clip );
					#endif
				}
			}

#endif
			
			// **************  Initialize serialized objects only if needed **************
			if( _FiltersHandler == null )
			{
				InitFilters();
			}
			
			if( _tracks == null )
			{
				_tracks = new List< GATTrack > ( 4 );
			}
			
			// ************** Initialize transient objects always **************
			_scheduledSamples = new SampleQueue();
			_samplesToEnqueue = new SampleQueue();
			_discardedSamples = new SampleQueue();
			_playingSamples   = new PlayingSamplesQueue( this );
			_pool 			  = new Stack<BufferedSample>( 30 );
			int i;
			
			for( i = 0; i < 30; i++ )
			{
				_pool.Push( new BufferedSample() );
			}
			
			_audioThreadStreamProxy = new GATAudioThreadStreamProxy( GATInfo.AudioBufferSizePerChannel, GATInfo.NbOfChannels, GATAudioBuffer.AudioBufferPointer, 0, ( "GATPlayer " + gameObject.name ) );
			
		}
		
		void OnEnable()
		{
#if !UNITY_5
			if( GATManager.UniqueInstance == null )
				return;
#endif
			
			if( _pool == null )
			{
				Awake ();
				GetComponent< AudioSource >().Play();
			}
			
			#if UNITY_EDITOR && GAT_DEBUG
			if( Application.isPlaying == false )
			{
				EditorApplication.update += Update;
			}
			#endif
		}
		
		void Start()
		{
			GetComponent< AudioSource >().Play();
		}
		
		void OnDisable()
		{
			if( _playingSamples != null )
			{
				ClearAllQueues();
			}
			
			//For some reason, OnDisable is not always called on tracks. Call it manually here:
			foreach( GATTrack track in _tracks )
			{
				if( track != null )
					track.OnDisable();
			}
			
			#if UNITY_EDITOR && GAT_DEBUG
			if( Application.isPlaying == false )
			{
				EditorApplication.update -= Update;
			}
			#endif
		}
		
		
		void OnDestroy()
		{
			if( Application.isPlaying )
			{
				foreach( GATTrack track in _tracks )
				{
					Destroy ( track );
				}
				
				Destroy ( _FiltersHandler );
			}
			else
			{
				foreach( GATTrack track in _tracks )
				{
					DestroyImmediate ( track );
				}
				
				DestroyImmediate ( _FiltersHandler );
			}
		}
		
		public static void InitStatics()
		{
			BufferedSample.SharedProcessingBuffer = GATManager.GetFixedDataContainer( GATInfo.AudioBufferSizePerChannel * 2, "Player Processing Buffer"  );
		}
		
		public static void CleanUpStatics()
		{
			BufferedSample.SharedProcessingBuffer = null;
		}
		
		//Used internally when destroying
		void ClearAllQueues()
		{
			ClearScheduledSamples();
			
			lock( _samplesToEnqueue )
			{
				_samplesToEnqueue.ReleaseAllAndPool( this );
			}
			
			_playingSamples.ReleaseAllAndPool( this );
		}
		
		void InitFilters()
		{
			_FiltersHandler = ScriptableObject.CreateInstance< GATFiltersHandler >();
			_FiltersHandler.InitFiltersHandler( GATInfo.NbOfChannels );
			_GainFilter = ( GATGainFilter )_FiltersHandler.AddFilter<GATGainFilter >( 999 );
			_GainFilter.Gain = _Gain;
			_GainFilter.Threshold = _ClipThreshold;
			_GainFilter.Clip = _DoClip;
		}
		
		BufferedSample GetBufferedSample()
		{
			lock( _pool )
			{
				if( _pool.Count > 0 )
				{
					return _pool.Pop();
				}
			}
			
			return new BufferedSample();
		}
		
		void PoolBufferedSample( BufferedSample sample )
		{
			sample.Clear();
			
			lock( _pool )
			{
				_pool.Push( sample );
			}
		}
		
		void OnAudioFilterRead ( float [] data, int numChannels )
		{
			#if GAT_DEBUG
			if( _FiltersHandler.NbOfFilteredChannels != numChannels )
			{
				Debug.LogError( "This player was setup for "+GATInfo.NbOfChannels+" channels output, current is "+numChannels+". Disabling player." );
				_shouldDisable = true;
				return;
			}
			#endif
			BufferedSample sample;
			bool shouldRemove;
			int i;
			int dataLength;
			bool noData = false;
			
			dataLength = data.Length;
			//*********************************************************************************************
			//First, we check if any samples in the scheduled queue need to be moved to the playing queue 
			
			shouldRemove = false;
			sample = _scheduledSamples.head.next; // caching the first item of the linked queue: thread safe iteration
			
			if( sample != null ) 
			{
				double nextBufferDSPTime = AudioSettings.dspTime + GATInfo.AudioBufferDuration;
				
				while( sample != null )
				{
					if( nextBufferDSPTime > sample.scheduledDspTime ) //flag samples which need to be moved
					{
						sample.shouldBeRemoved = true;
						shouldRemove = true;
						sample.OffsetInBuffer = ( int )( ( sample.scheduledDspTime - AudioSettings.dspTime ) * GATInfo.OutputSampleRate );
					}
					sample = sample.next;
				}
				
				if( shouldRemove ) //move to playing queue
				{
					lock( _scheduledSamples ) // make sure no sample is added from the main thread whilst removing
					{
						_scheduledSamples.TrimAndKeepDiscarded( _discardedSamples );
					}
					
					_playingSamples.Enqueue( _discardedSamples ); // no need to lock on the playing queue: it is only accessed by the audio thread
				}
			}
			
			//*************************************************************************
			//Second, we check the PlayImmediate queue
			
			lock( _samplesToEnqueue ) //make sure no play immediate sample gets added by the main thread whilst we concatenate the 2 queues
			{
				if( _samplesToEnqueue.head.next != null )
				{
					_playingSamples.Enqueue( _samplesToEnqueue );
					_samplesToEnqueue.Clear();
				}
			}
			//***********************************************************
			//Third, we mix the samples of the Playing queue
			
			sample = _playingSamples.head.next;
			
			if( onPlayerWillMix != null )
				onPlayerWillMix();
			
			//Even if there is no samples to play, filters might add to the mix:
			if( sample == null )
			{
				noData = true;
				
				//Check tracks
				for( i = 0; i < _tracks.Count; i++ )
				{
					if( ReferenceEquals ( _tracks[i], null ) == false )
					{
						if( _tracks[i].FXAndMixTo( data ) )
						{
							noData = false;
						}
					}
				}
				
				//Check Master Filters
				if( _FiltersHandler.HasFilters )
				{
					if( _FiltersHandler.ApplyFilters( data, 0, dataLength, noData ) )
					{
						noData = false;
					}
				}
				
				//Broadcast stream
				_audioThreadStreamProxy.BroadcastStream( data, 0, noData );
				
				//Stop there.
				if( onPlayerDidMix != null )
					onPlayerDidMix();
				
				return;
			}
			
			shouldRemove = false; 
			
			while( sample != null )
			{
				if( sample.MixNow( data ) == true )
				{
					shouldRemove = true;
				}
				sample = sample.next;
			}
			
			//****************************************************
			//Then, we remove samples which ended and clip the mix if needed
			if( shouldRemove )
			{
				_playingSamples.TrimAndReleaseDiscarded();
			}
			
			//****************************************************
			//Now, we check and mix tracks 
			for( i = 0; i < _tracks.Count; i++ )
			{
				if( ReferenceEquals( _tracks[i], null ) == false )
				{
					_tracks[i].FXAndMixTo( data );
				}
			}
			
			//Filter the mix, includes a default gain filter which will clip if set so
			if( _FiltersHandler.HasFilters )
			{
				_FiltersHandler.ApplyFilters( data, 0, dataLength, false );
			}
			
			//****************************************************
			//Finally, we fire the last callback
			
			_audioThreadStreamProxy.BroadcastStream( data, 0, false );
			
			if( onPlayerDidMix != null )
				onPlayerDidMix();
			
			if( _releasePlaying )
			{
				_playingSamples.ReleaseAllAndPool( this );
				_releasePlaying = false;
				float deltaGain = 1f / ( data.Length / numChannels );
				float gain = 1f;
				for( i = 0; i < data.Length; i+= numChannels )
				{
					data[ i   ] *= gain;
					data[ i+1 ] *= gain;
					gain -= deltaGain;
				}
			}
		}
		
		#endregion
		
		#region Nested Private Classes
		class BufferedSample : IGATBufferedSample, IGATBufferedSampleOptions
		{
			#region IGATBufferedSample implementation
			
			public bool	         IsFirstChunk{ get; set; } // interface has no set
			public bool			  IsLastChunk{ get; set; } // interface has both get and set
			public AGATPanInfo 	      PanInfo{ get; private set; }
			public GATData          AudioData{ get; private set; }
			public int	       OffsetInBuffer{ get; set;} //interface has no set
			public int 			    NextIndex{ get; set; }
			public GATData   ProcessingBuffer{ get{ return __processingBuffer; } }
			public GATTrack				Track{ get{ return _track; } }
			public float		  PlayingGain{ get{ return _gain; } }
			
			public void CacheToProcessingBuffer( int length )
			{
				AudioData.CopyTo( __processingBuffer, 0, NextIndex, length );
			}
			
			public void SetEnd( int numSamples, int fadeLength )
			{
				_count 		 = numSamples;
				_fadeStart 	 = numSamples - fadeLength; 
				_fadeSamples = fadeLength;
			}
			
			#endregion
			
			static GATData  __processingBuffer;
			
			#region Members
			public BufferedSample next;
			
			public bool 	shouldBeRemoved;
			
			public double	scheduledDspTime;
			
			OnShouldMixSample _onShouldMixSample;
			
			float _gain;
			
			GATTrack _track;
			
			int    _count;
			int    _fadeStart;
			int    _fadeSamples;
			
			#endregion
			
			#region Methods
			public static GATData SharedProcessingBuffer
			{
				get{ return __processingBuffer;  }
				set{ __processingBuffer = value; }
			}
			
			public BufferedSample(){}
			
			public void Init( GATData isample, AGATPanInfo panInfo, OnShouldMixSample callback, float gain = 1f )
			{
				AudioData 	   		= isample;
				PanInfo 			= panInfo;
				_onShouldMixSample 	= callback;
				_gain				= gain;
				IsFirstChunk        = true;
				_count				= isample.Count;
				_fadeStart			= -1;
			}
			
			public void Init( GATData isample, GATTrack track, OnShouldMixSample callback, float gain = 1f )
			{
				AudioData 	   		= isample;
				_track				= track;
				_onShouldMixSample 	= callback;
				_gain				= gain;
				IsFirstChunk        = true;
				_count				= isample.Count;
				_fadeStart			= -1;
			}
			
			public void Clear()
			{
				AudioData.Release();
				
				_onShouldMixSample 	= null;
				IsLastChunk       	= false;
				
				next 		= null;
				AudioData 	= null;
				
				shouldBeRemoved  = false;
				
				PanInfo 	= null;
				NextIndex 	= 0;
				
				_track 		= null;
			}
			
			public bool MixNow( float[] audioBuffer )
			{
				int length = GATInfo.AudioBufferSizePerChannel - OffsetInBuffer;
				bool shouldMix = true;
				
				if( length > _count - NextIndex )
				{
					length = _count - NextIndex;
					IsLastChunk = true;
				}
				
				if( _onShouldMixSample != null )
				{
					shouldMix = _onShouldMixSample( this, length, audioBuffer );
				}
				
				if( shouldMix )
				{
					if( _fadeStart > -1 && ( NextIndex + length > _fadeStart ) )
					{
						int fadeLength = length;
						float fromGain, toGain;
						int fadeOffsetInProcBuf;
						CacheToProcessingBuffer( length );
						
						if( NextIndex < _fadeStart )
						{
							fadeLength = NextIndex + length - _fadeStart;
							fromGain = _gain;
							toGain   = Mathf.Lerp( _gain, 0f, ( float )fadeLength / _fadeSamples );
							
							fadeOffsetInProcBuf = _fadeStart - NextIndex;
							__processingBuffer.Gain( 0, fadeOffsetInProcBuf, _gain );
							__processingBuffer.SmoothedGain( fadeOffsetInProcBuf, fadeLength, fromGain, toGain );
						}
						else
						{
							int fadeOffset = NextIndex - _fadeStart;
							fromGain = Mathf.Lerp( _gain, 0f, ( float )fadeOffset / _fadeSamples );
							toGain = Mathf.Lerp( _gain, 0f, ( float )( fadeOffset + fadeLength ) / _fadeSamples );
							__processingBuffer.SmoothedGain( 0, fadeLength, fromGain, toGain );
						}
						
						if( ReferenceEquals( _track, null ) == false ) 
						{
							_track.MixFrom( __processingBuffer, 0, OffsetInBuffer, length, 1f );
						}
						else
						{
							PanInfo.PanMixProcessingBuffer( this, length, audioBuffer, 1f );
						}
					}
					else
					{
						if( ReferenceEquals( _track, null ) == false ) 
						{
							_track.MixFrom( AudioData, NextIndex, OffsetInBuffer, length, _gain );
						}
						else
						{
							PanInfo.PanMixSample( this, length, audioBuffer, _gain );
						}
					}
					
					NextIndex += length;
				}
				
				if( IsFirstChunk )
				{
					IsFirstChunk = false;
					OffsetInBuffer = 0;
				}
				
				if( IsLastChunk )
				{
					shouldBeRemoved = true;
				}
				
				return shouldBeRemoved;
			}
			
			#endregion
			
			public static VoidSampleOptions VoidOptions;
			static BufferedSample()
			{
				VoidOptions = new VoidSampleOptions();
			}
			public class VoidSampleOptions : IGATBufferedSampleOptions
			{
				public void SetEnd( int numSamples, int fadeLength )
				{
				}
				
				public VoidSampleOptions(){}
			}
		}
		
		class LinkedHead<T>
		{
			public T next;
			public LinkedHead(){}
		}
		
		class SampleQueue
		{
			public LinkedHead< BufferedSample > head;
			public BufferedSample last;
			
			public SampleQueue()
			{
				head = new LinkedHead< BufferedSample >();
			}
			
			public void Clear()
			{
				head.next = null;
				last = null;
			}
			
			public void ReleaseAllAndPool( GATPlayer parentPlayer )
			{
				while( head.next != null )
				{
					parentPlayer.PoolBufferedSample( head.next );
					
					head.next = head.next.next;
				}
				
				Clear ();
			}
			
			public void Enqueue( SampleQueue queue )
			{
				if( queue.last != null )
				{
					queue.last.next = head.next;
					head.next = queue.head.next;
					
					if( last == null )
					{
						last = queue.last;
					}
				}
			}
			
			public void Enqueue( BufferedSample sample )
			{
				if( last == null )
				{
					last = sample;
					last.next = null;
				}
				
				sample.next = head.next;
				head.next = sample;
			}
			
			public void TrimAndKeepDiscarded( SampleQueue refQueue )
			{
				BufferedSample sample = null;
				BufferedSample discardedSample;
				
				refQueue.Clear();
				
				while( head.next != null && head.next.shouldBeRemoved )
				{
					discardedSample = head.next;
					discardedSample.shouldBeRemoved = false;
					head.next = discardedSample.next;
					refQueue.Enqueue( discardedSample ); //enqueue cached because enqueuing might set next to null if is last
				}
				
				if( head.next != null )
				{
					sample = head.next;
					
					while( sample.next != null )
					{
						if( sample.next.shouldBeRemoved )
						{
							discardedSample = sample.next;
							discardedSample.shouldBeRemoved = false;
							sample.next = discardedSample.next;
							refQueue.Enqueue( discardedSample ); //enqueue cached because enqueuing might set next to null if is last
						}
						else
						{
							sample = sample.next;
						}
					}
				}
				
				if( head.next == null )
				{
					last = null;
				}
				else
				{
					last = sample;
				}
			}
		}
		
		class PlayingSamplesQueue : SampleQueue
		{
			GATPlayer _parentPlayer;
			
			public PlayingSamplesQueue( GATPlayer parentPlayer ) : base()
			{
				_parentPlayer = parentPlayer;
			}
			
			public void TrimAndReleaseDiscarded()
			{
				BufferedSample sample = null;
				BufferedSample discardedSample;
				
				while( head.next != null && head.next.shouldBeRemoved )
				{
					discardedSample = head.next;
					head.next = discardedSample.next;
					_parentPlayer.PoolBufferedSample( discardedSample );
					
				}
				
				if( head.next != null )
				{
					sample = head.next;
					
					while( sample.next != null )
					{
						if( sample.next.shouldBeRemoved )
						{
							discardedSample = sample.next;
							sample.next = discardedSample.next;
							_parentPlayer.PoolBufferedSample( discardedSample );
						}
						else
						{
							sample = sample.next;
						}
					}
				}
				
				if( head.next == null )
				{
					last = null;
				}
				else
				{
					last = sample;
				}
			}
		}
		
		#endregion
		
		#if GAT_DEBUG
		volatile bool _shouldDisable;
		void Update()
		{
			if( _shouldDisable )
			{
				this.enabled = false;
				_shouldDisable = false;
			}
		}
		#endif
	}
}



