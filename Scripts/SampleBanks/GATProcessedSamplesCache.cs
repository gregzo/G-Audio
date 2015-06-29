using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GAudio
{
	/// <summary>
	/// Manages a cache of processed samples.
	/// Used by GATActiveSampleBank to serve IGATProcessedSample objects.
	/// 
	/// </summary>
	public class GATProcessedSamplesCache : IDisposable
	{
		Dictionary < GATData, List < ProcessedAudioChunk > > _processedChunksInMemory;

		public GATProcessedSamplesCache( List< GATData > sourceSamples, int extraCapacity = 0 )
		{
			_processedChunksInMemory = new Dictionary< GATData, List< ProcessedAudioChunk> >( sourceSamples.Count + extraCapacity );

			int i;
			for( i = 0; i < sourceSamples.Count; i++ )
			{
				_processedChunksInMemory.Add ( sourceSamples[ i ], new List< ProcessedAudioChunk >() );
			}
		}

		public GATProcessedSamplesCache( int capacity )
		{
			_processedChunksInMemory = new Dictionary< GATData, List< ProcessedAudioChunk> >( capacity );
		}

		public void AddSample( GATData sample )
		{
			_processedChunksInMemory.Add( sample, new List< ProcessedAudioChunk >() );
		}

		public IGATProcessedSample GetProcessedSample( GATData sourceSample, double pitch, GATEnvelope envelope )
		{
			if( envelope == null )
				envelope = GATEnvelope.nullEnvelope;
			
			int i;
			
			List<ProcessedAudioChunk> chunks = _processedChunksInMemory[ sourceSample ];
			ProcessedAudioChunk sample;
			
			for( i = 0; i < chunks.Count; i++ )
			{
				if( chunks[i].envelope == envelope && chunks[i].Pitch == pitch )
				{
					sample = chunks[i];
					return sample;
				}
			}
			
			sample = new ProcessedAudioChunk( sourceSample, envelope, this, pitch ); //Here is the main dif with base class
			chunks.Add( sample );
			return sample;
		}



		public void RemoveSample( GATData sample )
		{
			List< ProcessedAudioChunk > chunks = _processedChunksInMemory[ sample ];
			int i;

			for( i = 0; i < chunks.Count; i++ )
			{
				chunks[ i ].CleanUp();
			}

			_processedChunksInMemory.Remove( sample );
		}

		/// <summary>
		/// Flushs all cached samples associated with the specified envelope.
		/// </summary>
		/// <param name="envelope">Envelope.</param>
		public void FlushCacheForEnvelope( GATEnvelope envelope )
		{
			List< ProcessedAudioChunk > chunks;
			List< ProcessedAudioChunk > chunksToRemove = new List< ProcessedAudioChunk >();
			ProcessedAudioChunk 	    chunk;
			int i;
			
			if( envelope == null )
				envelope = GATEnvelope.nullEnvelope;
			
			foreach( KeyValuePair< GATData, List< ProcessedAudioChunk > > chunksByName in _processedChunksInMemory )
			{
				chunks = chunksByName.Value;
				
				if( chunks.Count == 0 )
					continue;
				
				for( i = 0; i < chunks.Count; i++ )
				{
					chunk = chunks[ i ];
					if( chunk.envelope == envelope )
					{
						chunk.CleanUp();
						chunksToRemove.Add ( chunk );
					}
				}
				
				if( chunksToRemove.Count > 0 )
				{
					for( i = 0; i < chunksToRemove.Count; i++ )
					{
						chunks.Remove( chunksToRemove[ i ] );
					}
					
					chunksToRemove.Clear();
				}
			}
		}

		void RemoveChunkFromCache( ProcessedAudioChunk chunk )
		{
			if( _processedChunksInMemory.Count == 0 )
				return;
			
			List<ProcessedAudioChunk> chunks;
			chunks = _processedChunksInMemory[ chunk.sourceSample ];
			chunks.Remove( chunk );
		}

		class ProcessedAudioChunk : RetainableObject, IGATProcessedSample
		{
			#region IGATProcessedSample Implementation
			
			public double Pitch{ get{ return _pitch; } set{ SetPitch( value ); } }
			double _pitch, _nextPitch;
			
			public IGATBufferedSampleOptions Play( AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return GATManager.DefaultPlayer.PlayData( _audioData, panInfo, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions Play( GATPlayer player, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return player.PlayData( _audioData, panInfo, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions Play( int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return GATManager.DefaultPlayer.PlayData( _audioData, trackNb, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions Play( GATPlayer player, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return player.PlayData( _audioData, trackNb, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions PlayScheduled( double dspTime, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return GATManager.DefaultPlayer.PlayDataScheduled( _audioData, dspTime, panInfo, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions PlayScheduled( GATPlayer player, double dspTime, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return player.PlayDataScheduled( _audioData, dspTime, panInfo, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions PlayScheduled( double dspTime, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return GATManager.DefaultPlayer.PlayDataScheduled( _audioData, dspTime, trackNb, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public IGATBufferedSampleOptions PlayScheduled( GATPlayer player, double dspTime, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null )
			{
				UpdateAudioData();
				return player.PlayDataScheduled( _audioData, dspTime, trackNb, gain, mixCallback ); //Only use AudioData property when playing, as it updates
			}
			
			public void UpdateAudioData()
			{	
				if( _lastDataChange < envelope.LastChangeTime )
				{
					_needsDataUpdate = true;	
					_lastDataChange 	= AudioSettings.dspTime;
					
					if( _lastLengthChange < envelope.LastLengthChangeTime )
					{
						_needsNewContainer = true;
						_lastLengthChange   = _lastDataChange;
						_cachedLength = envelope.Length;// if nullEnvelope is used, _cachedLength will never change.
					}
				}
				
				if( _needsDataUpdate == false ) //early exit if data is cached
				{
					return;
				}
				
				if( _needsNewContainer == false ) // maybe data is playing elsewhere or is deallocated, let's check
				{
					CheckNeedsNewContainer();
				}
				
				if( _needsNewContainer )
				{
					UpdateContainer();
				}
				else if( _needsDataUpdate == false )
				{
					return;
				}
				
				
				FillAndProcessData();
			}
			
			//Explicit implementation 
			GATData	IGATDataOwner.AudioData 
			{ 
				get  
				{ 
					UpdateAudioData(); 
					return _audioData; 
				} 
			} 
			
			#endregion
			
			#region Protected and Private Members
			public readonly GATEnvelope envelope;
			
			protected readonly 	GATProcessedSamplesCache _parentCache;
			
			protected GATData 	_audioData;		
			
			protected bool		_needsNewContainer 	= true, 
			_needsDataUpdate	= true;
			protected int 		_cachedLength;
			
			private double  _lastLengthChange, 
			_lastDataChange;
			
			#endregion

			public readonly GATData sourceSample;
			
			public ProcessedAudioChunk( GATData sourcesample, GATEnvelope ienvelope, GATProcessedSamplesCache parentCache, double pitch = 1d )
			{
				sourceSample = sourcesample;
				envelope 	= ienvelope;
				_parentCache = parentCache;
				
				if( envelope == GATEnvelope.nullEnvelope ) //_cachedLength will never change
				{
					_cachedLength = sourcesample.Count;
				}
				else
				{
					_cachedLength = envelope.Length;
				}

				SetPitch( pitch );
			}

			void SetPitch( double newPitch )
			{
				if( newPitch == _nextPitch )
					return;
				
				_nextPitch = newPitch;
				_needsDataUpdate = true;
				
				
				if( envelope == GATEnvelope.nullEnvelope )
				{
					_cachedLength = GATMaths.ResampledLength( sourceSample.Count, _nextPitch );
					_needsNewContainer = true;
				}
			}
			
			public void CleanUp()
			{
				if( _audioData != null )
				{
					_audioData.Release();
					_audioData = null;
				}
			}
			
			#region Protected and Private Methods
			//overridable for more cached processing in derived classes ( pitch shift )
			protected virtual void FillAndProcessData()
			{
				if( _nextPitch == 1d )
				{
					FillWithSampleData( envelope.Offset, _cachedLength );
				}
				else
				{
					FillWithResampledData( envelope.Offset, _cachedLength, _nextPitch );
				}
				
				_pitch = _nextPitch;
				envelope.ProcessSample( _audioData );
				
				_needsDataUpdate = false;
			}
			
			protected override void Discard()
			{
				CleanUp ();
				_parentCache.RemoveChunkFromCache( this );
				_retainCount = 0;
			}
			
			void CheckNeedsNewContainer()
			{
				if( _needsDataUpdate && _audioData.RetainCount > 1 ) //data is needed elesewhere, get new space in memory
				{
					_needsNewContainer = true;
				}
			}
			
			void UpdateContainer()
			{
				if( _audioData != null )
				{
					_audioData.Release();
				}
				
				#if UNITY_EDITOR
				if( Application.isPlaying )
				{
					_audioData = GATManager.GetDataContainer( _cachedLength );
				}
				else
				{
					_audioData = new GATData( new float[ _cachedLength ] );
				}
				#else
				_audioData = GATManager.GetDataContainer( _cachedLength );
				#endif
				
				_audioData.Retain();
			}

			void FillWithSampleData( int fromIndex, int length )
			{
				bool tooLong = ( fromIndex + length > sourceSample.Count );
				
				int appliedLength = tooLong ? sourceSample.Count - fromIndex : length;
				
				if( appliedLength < 0 )
				{
					#if GAT_DEBUG
					Debug.LogWarning( "requested offset is out of bounds." );
					#endif
					return;
				}
				
				sourceSample.CopyTo( _audioData, 0, fromIndex, appliedLength );
				
				#if GAT_DEBUG
				if( tooLong )
				{
					Debug.LogWarning( "requested length at fromIndex out of bounds, filling as much as possible" );
				}
				#endif
			}

			void FillWithResampledData( int fromIndex, int targetLength, double pitch )
			{	
				//check that we have enough samples to fulfill the request:
				int appliedLength = GATMaths.ClampedResampledLength( sourceSample.Count - fromIndex, targetLength, pitch );
				
				if( appliedLength < 0 )
				{
					#if GAT_DEBUG
					Debug.LogWarning( "requested offset is out of bounds." );
					#endif
					return;
				}
				
				sourceSample.ResampleCopyTo( fromIndex, _audioData, appliedLength, pitch ); 
				
				//if we did not have enough samples, clear the rest:
				if( appliedLength < targetLength )
				{
					_audioData.Clear( appliedLength, _audioData.Count - appliedLength );
				}
			}
			
			#endregion
		}

		#region IDisposable Implementation
		bool _disposed;
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		void Dispose( bool explicitly )
		{
			if( _disposed )
				return;
			
			if( explicitly )
			{
				FreeAll();
			}
			
			_disposed = true;
		}
		
		~GATProcessedSamplesCache()
		{
			Dispose( false );
		}

		void FreeAll()
		{
			List< ProcessedAudioChunk > chunks;
			int i;

			foreach( KeyValuePair< GATData, List< ProcessedAudioChunk > > chunksByName in _processedChunksInMemory )
			{
				chunks = chunksByName.Value;
				
				if( chunks.Count == 0 )
					continue;
				
				for( i = 0; i < chunks.Count; i++ )
				{
					chunks[ i ].CleanUp();
				}
			}

			_processedChunksInMemory.Clear();
		}

		#endregion
	}
}

