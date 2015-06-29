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
	/// Listens to a MasterPulseModule or SubPulseModule and
	/// triggers playback of processed samples on specified steps
	/// and in the desired ordering.
	/// </summary>
	[ExecuteInEditMode]
	public class PulsedPatternModule : AGATPulsedPattern {
		
		
		/// <summary>
		/// The envelope through which to process samples.
		/// May be null.
		/// </summary>
		public EnvelopeModule Envelope
		{
			get{ return _envelopeModule; }
			set
			{
				if( _envelopeModule == value )
					return;
				
				int i;
				
				for( i = 0; i < _samples.Count; i++ )
				{
					_samples[ i ].ProcessedSample = null;
				}
				
				_envelopeModule = value;
			}
		}
		[ SerializeField ]
		protected EnvelopeModule _envelopeModule;
		
		/// <summary>
		/// Gets the current samples of a pattern. Sample properties can be 
		/// changed directly, but adding or removing samples must go through
		/// AddSample and RemoveSample methods.
		/// Caching this property is recommended as it performs a copy of the
		/// PatternSample references every time it is accessed.
		/// </summary>
		public PatternSample[] Samples
		{ 
			get
			{ 
				PatternSample[] ret = new PatternSample[ _samples.Count ];
				_samples.CopyTo( ret, 0 );
				return ret; 
			} 
		}
		
		[ SerializeField ]
		protected List< PatternSample > _samples = new List<PatternSample>();
		
		/// <summary>
		/// Adds a new PatternSample at the end of the list.
		/// You may then retrieve the updated samples ( Samples property ),
		/// and configure the newly added sample's parameters.
		/// </summary>
		public void AddSample( string sampleName )
		{
			_samples.Add ( new PatternSample( sampleName ) );
			_sampleCount ++;
			
			if( _sampleCount == 1 )
			{
				SubscribeToPulseIfNeeded();
			}
		}
		
		/// <summary>
		/// Inserts a pre configured PatternSample in the pattern.
		/// </summary>
		public void InsertSample( PatternSample newSample, int index )
		{
			if( index > _samples.Count )
			{
				index = _samples.Count;
			}
			
			_samples.Insert( index, newSample );
			_sampleCount ++;
			
			if( _sampleCount == 1 )
			{
				SubscribeToPulseIfNeeded();
			}
		}
		
		/// <summary>
		/// Removes the PatternSample at specified index.
		/// </summary>
		/// <param name="index">Index.</param>
		public void RemoveSampleAt( int index )
		{
			_samples[ index ].ProcessedSample = null;
			_samples.RemoveAt( index );
			_sampleCount --;
			
			if( _sampleCount == 0 )
				UnsubscribeToPulse();
		}
		
		public override void PlaySample( int index, double dspTime )
		{
			if( _sampleBank.IsLoaded == false )
				return;
			
			PatternSample sampleInfo = _samples[index];
			
			if( onPatternWillPlay != null )
				onPatternWillPlay( sampleInfo, index, dspTime );
			
			if( _envelopeModule != null )
			{
				if( sampleInfo.ProcessedSample == null )
				{
					sampleInfo.ProcessedSample = _sampleBank.GetProcessedSample( sampleInfo.SampleName, _envelopeModule.Envelope, sampleInfo.Pitch );
				}
				
				sampleInfo.ProcessedSample.PlayScheduled( _player, dspTime, _trackNb, sampleInfo.Gain );
			}
			else
			{
				GATData data = _sampleBank.GetAudioData( sampleInfo.SampleName );
				_player.PlayDataScheduled( data, dspTime, _trackNb, sampleInfo.Gain );
			}
		}
		
		protected override int UpdatedSampleCount()
		{
			return _samples.Count;
		}
		
		protected void OnDestroy()
		{
			int i;
			
			for( i = 0; i < _samples.Count; i++ )
			{
				_samples[ i ].ProcessedSample = null; //Free allocated memory
			}
		}
		
		
		protected override bool CanSubscribeToPulse()
		{
			if( base.CanSubscribeToPulse() == false || _samples.Count == 0 )
			{
				return false;
			}
			
			return true;
		}
	}
}


