//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Wrapper that adds realtime ADSR controls to
	/// IGATDataOwner objects( GATData or IGATProcessedSample ).
	/// In addition, zero crossings are automatically found for
	/// the sustain loop, and a sustain loop crossfade can be adjusted.
	/// </summary>
	public class GATRealTimeADSR
	{
		/// <summary>
		/// Is the sample currently playing?
		/// </summary>
		public bool IsPlaying{ get; private set; }
		
		#region Private members
		enum State{ Attack, Decay, Sustain, SustainCrossfade, Release }
		
		IGATDataOwner 	_dataOwner;
		GATData 		_data;
		State 			_currentState;
		
		//Indexes
		int _attackStartIndex,
		_decayStartIndex,
		_loopStartIndex,
		_loopEndIndex,
		_releaseIndex,//set on release only
		_endIndex, //set on release only
		_nextIndex,
		_loopCrossfadeIndex;
		
		//Lengths
		int	_attackLength,
		_loopLength,
		_releaseLength,
		_loopCrossfadeLength;
		
		//Flags
		bool _keepLooping,
		_noLoop;
		
		#endregion
		
		/// <summary>
		/// Wrap a GATData or a IGATProcessedSample
		/// object for ADSR playback.
		/// </summary>
		public GATRealTimeADSR( IGATDataOwner dataOwner )
		{
			_dataOwner = dataOwner;
			_currentState = State.Attack;
		}
		
		/// <summary>
		/// Call TrySetEnvelope before playing.
		/// Processing of the sample occurs in real time, but the envelope
		/// parameters cannot be modified while the sample is playing.
		/// 
		/// Note 1: a sustain of 0 is valid and will result in one shot playback.
		/// Note 2: if no loopCrossfade parameter is passed, loopCrossfade will equal 
		/// sustain.
		/// Note 3: offset + attack + decay must be greater than loopCrossfade.
		/// </summary>
		public bool TrySetEnvelope( int offset, int attack, int decay, int sustain, int release, int loopCrossfade = -1 )
		{
			if( IsPlaying )
			{
				Debug.LogWarning( "Envelope parameters cannot be updated while the sample is playing." );
				return false;
			}
			
			int temp = loopCrossfade == -1 ? sustain : loopCrossfade;
			if( offset + attack + decay < temp )
			{
				Debug.LogError( "loopCrossfade must be smaller than offset + attack + decay " );
				return false;
			}
			
			_attackStartIndex 	= offset;
			_decayStartIndex 	= _attackStartIndex + attack;
			
			if( sustain > 0 )
			{
				//Adjusted on Play by UpdateZeroCrossings()
				_loopStartIndex 	= _decayStartIndex  + decay;
				_loopEndIndex 		= _loopStartIndex 	+ sustain;
				_loopCrossfadeLength = loopCrossfade;
				//********************************
			}
			else // no sustain, ADR envelope
			{
				_noLoop 		= true;
				_loopStartIndex = _decayStartIndex + decay;
				_loopEndIndex   = _loopStartIndex;
				_releaseIndex   = _loopStartIndex;
				_endIndex		= _loopStartIndex + release;
			}
			
			_attackLength = attack;
			_releaseLength = release;
			
			return true;
		}
		
		/// <summary>
		/// Starts playback through the default player.
		/// </summary>
		public void PlayThroughTrack( int trackNb, float gain = 1f )
		{
			#if GAT_DEBUG
			if( IsPlaying )
			{
				Debug.LogWarning( "Already playing!" );
				return;
			}
			#endif
			
			IsPlaying 		= true;
			_keepLooping 	= true;
			_nextIndex 		= _attackStartIndex; //reset the play head
			_currentState 	= State.Attack;
			_data 			= _dataOwner.AudioData;
			
			if( !_noLoop )
			{
				UpdateZeroCrossings(); 
			}
			
			GATManager.DefaultPlayer.PlayData( _data, trackNb, gain, PlayerWillMixSample );
		}
		
		/// <summary>
		/// Starts playback through the user specified player.
		/// </summary>
		public void PlayThroughTrack( GATPlayer player, int trackNb, float gain = 1f )
		{
			#if GAT_DEBUG
			if( IsPlaying )
			{
				Debug.LogWarning( "Already playing!" );
				return;
			}
			#endif
			
			IsPlaying 		= true;
			_keepLooping 	= true;
			_nextIndex 		= _attackStartIndex; //reset the play head
			_currentState 	= State.Attack;
			_data 			= _dataOwner.AudioData;
			
			if( !_noLoop )
			{
				UpdateZeroCrossings(); 
			}
			
			player.PlayData( _data, trackNb, gain, PlayerWillMixSample );
		}
		
		/// <summary>
		/// If playback is in the loop crossfade
		/// part of the envelope, the sample will loop
		/// one last time and release from the sustain index.
		/// </summary>
		public void Release()
		{
			_keepLooping = false;
		}
		
		void UpdateZeroCrossings()
		{
			bool startCrossing; //true is crossing to > 0, false to < 0
			bool endCrossing;
			
			_loopStartIndex = _data.NextZeroCrossing( _loopStartIndex, out startCrossing ); 
			endCrossing 	= startCrossing;
			
			while( endCrossing != startCrossing )
				_loopEndIndex = _data.NextZeroCrossing( _loopEndIndex, out endCrossing ); 
			
			_loopLength = _loopEndIndex - _loopStartIndex;
			
			if( _loopCrossfadeLength == -1 )
				_loopCrossfadeLength = _loopLength;
			
			_loopCrossfadeIndex = _loopEndIndex - _loopCrossfadeLength;
		}
		
		bool PlayerWillMixSample( IGATBufferedSample sample, int length, float[] audioBuffer )
		{	
			int indexInProcessingBuffer = 0;
			int appliedLength;
			int lengthToMix = length;
			float fromGain, toGain;
			
			switch( _currentState ) 
			{
			case State.Attack:
				if( _nextIndex >= _decayStartIndex ) //handle attack = 0;
				{
					_currentState = State.Decay;
					goto case State.Decay;
				}
				else
				{
					if( sample.IsFirstChunk && length > _attackLength )
					{
						appliedLength = _attackLength;
					}
					else
					{
						appliedLength = length;
					}
					
					//Interpolate gain
					fromGain = ( ( float )( _nextIndex - _attackStartIndex ) ) / ( _attackLength );
					
					if( _nextIndex + appliedLength >= _decayStartIndex ) //attack slope will finish before the end of the buffer
					{
						appliedLength = _decayStartIndex - _nextIndex;
						toGain = 1f;
						_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, 0, appliedLength, fromGain, toGain );
						_nextIndex = _decayStartIndex;
						indexInProcessingBuffer = appliedLength;
						
						_currentState = State.Decay;
						goto case State.Decay;
					}
					else
					{
						toGain = ( ( float )( _nextIndex + appliedLength - _attackStartIndex ) ) / ( _attackLength );
						_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, 0, appliedLength, fromGain, toGain );
						_nextIndex  += appliedLength;
					}
				}
				break;
				
			case State.Decay: 
				
				appliedLength = GATInfo.AudioBufferSizePerChannel - indexInProcessingBuffer;
				
				if( _nextIndex + appliedLength >= _loopStartIndex ) // decay will end this buffer
				{
					appliedLength = _loopStartIndex - _nextIndex;
					_data.CopyTo( sample.ProcessingBuffer, indexInProcessingBuffer, _nextIndex, appliedLength );
					
					_nextIndex = _loopStartIndex;
					indexInProcessingBuffer += appliedLength;
					
					if( _noLoop )
					{
						_currentState = State.Release;
						goto case State.Release;
					}
					else if( _loopCrossfadeLength == _loopLength )
					{
						_currentState = State.SustainCrossfade;
						goto case State.SustainCrossfade;
					}
					else 
					{ 
						_currentState = State.Sustain;
						goto case State.Sustain; 
					}
					
				}
				else
				{
					_data.CopyTo( sample.ProcessingBuffer, indexInProcessingBuffer, _nextIndex, appliedLength );
					_nextIndex += appliedLength;
				}
				
				break;
				
			case State.Sustain:
				
				appliedLength = GATInfo.AudioBufferSizePerChannel - indexInProcessingBuffer;
				
				if( _nextIndex + appliedLength >= _loopCrossfadeIndex ) // will start crossfading in this buffer
				{
					appliedLength = _loopCrossfadeIndex - _nextIndex;
					_data.CopyTo( sample.ProcessingBuffer, indexInProcessingBuffer, _nextIndex, appliedLength );
					indexInProcessingBuffer += appliedLength;
					_nextIndex += appliedLength;
					
					if( _keepLooping )
					{
						_currentState = State.SustainCrossfade;
						goto case State.SustainCrossfade;
					}
					else
					{
						_releaseIndex = _nextIndex;
						_endIndex = _nextIndex + _releaseLength;
						_currentState = State.Release;
						goto case State.Release;
					}
					
				}
				else
				{
					_data.CopyTo( sample.ProcessingBuffer, indexInProcessingBuffer, _nextIndex, appliedLength );
					_nextIndex += appliedLength;
				}
				break;
				
			case State.SustainCrossfade:
				
				appliedLength = GATInfo.AudioBufferSizePerChannel - indexInProcessingBuffer;
				int crossfadeOffset = _nextIndex - _loopCrossfadeIndex;
				//Crossfade gains
				fromGain = 1f - ( float )( crossfadeOffset ) / _loopCrossfadeLength;
				
				if( _nextIndex + appliedLength > _loopEndIndex ) //will finish loop in current buffer
				{
					appliedLength = _loopEndIndex - _nextIndex;
					_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, fromGain, 0f );
					_data.MixSmoothedGainTo( _loopStartIndex - ( _loopCrossfadeLength - crossfadeOffset ) , sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, 1f - fromGain, 1f );
					indexInProcessingBuffer += appliedLength;
					
					_nextIndex = _loopStartIndex; //need to loop even if release, as xfade has already started
					
					if( _keepLooping )
					{
						_currentState = State.Sustain;
						goto case State.Sustain;
					}
					else
					{
						_releaseIndex = _loopStartIndex;
						_endIndex = _loopStartIndex + _releaseLength;
						_currentState = State.Release;
						goto case State.Release;
					}
				}
				else
				{
					//crossfade gain
					toGain = 1f - ( float )( crossfadeOffset + appliedLength ) / _loopCrossfadeLength;
					_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, fromGain, toGain );
					_data.MixSmoothedGainTo( _loopStartIndex - ( _loopCrossfadeLength - crossfadeOffset ) , sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, 1f - fromGain, 1f - toGain );
					_nextIndex += appliedLength;
				}
				
				break;
				
			case State.Release:
				
				appliedLength = GATInfo.AudioBufferSizePerChannel - indexInProcessingBuffer;
				
				fromGain = 1f - ( ( float )( _nextIndex - _releaseIndex ) ) / ( _releaseLength );
				
				if( _nextIndex + appliedLength >= _endIndex ) //release slope will finish before the end of the buffer
				{
					toGain = 0f;
					sample.IsLastChunk = true;
					IsPlaying = false;
					appliedLength = _endIndex - _nextIndex;
					
					lengthToMix = appliedLength + indexInProcessingBuffer;
					_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, fromGain, toGain );
				}
				else
				{
					
					toGain = 1f - ( ( float )( _nextIndex + appliedLength - _releaseIndex ) ) / ( _releaseLength );
					_data.CopySmoothedGainTo( _nextIndex, sample.ProcessingBuffer, indexInProcessingBuffer, appliedLength, fromGain, toGain );
					_nextIndex += appliedLength;
				}
				break;
			}
			sample.NextIndex = _nextIndex;
			sample.Track.MixFrom( sample.ProcessingBuffer, 0, sample.OffsetInBuffer, lengthToMix ); 
			
			return false;
		}
	}
}

