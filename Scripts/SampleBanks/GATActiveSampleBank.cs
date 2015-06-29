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
	/// Adds processed samples caching
	/// functionnalities to GATSampleBank.
	/// </summary>
	public class GATActiveSampleBank : GATSampleBank 
	{
		GATProcessedSamplesCache _cache;
		
		/// <summary>
		/// Gets a processed sample container that will automatically update according to the envelope.
		/// Play it directly, not via GATPlayer : all envelope updates will be handled, even when overlapping sounds have their envelope changed.
		/// Note that the pitch parameter will only be taken into account by GATResamplingSampleBank instances.
		/// </summary>
		/// <returns>
		/// An interface to the processed sample container.
		/// </returns>
		/// <param name='sampleName'>
		/// The sample's name in the loaded SoundBank
		/// </param>
		/// <param name='envelope'>
		/// The envelope may be null if you need the whole, unprocessed sample. 
		/// </param>
		public virtual IGATProcessedSample GetProcessedSample( string sampleName, GATEnvelope envelope, double pitch = 1d )
		{
			return _cache.GetProcessedSample( _samplesByName[ sampleName ], pitch, envelope );
		}

		public virtual IGATProcessedSample GetProcessedSample( int indexInBank, GATEnvelope envelope, double pitch = 1d )
		{
			return _cache.GetProcessedSample( _allSamples[ indexInBank ], pitch, envelope );
		}
		
		/// <summary>
		/// Flushs all cached samples associated with the specified envelope.
		/// </summary>
		/// <param name="envelope">Envelope.</param>
		public void FlushCacheForEnvelope( GATEnvelope envelope )
		{
			if( _cache == null )
				return;
			_cache.FlushCacheForEnvelope( envelope );
		}

		public override void AddSample( GATData data, string sampleName )
		{	
			base.AddSample( data, sampleName );

			if( _cache == null )
				_cache = new GATProcessedSamplesCache( _totalCapacity );

			_cache.AddSample( data );
		}
		
		public override void RemoveSample( string name )
		{
			GATData data = _samplesByName[ name ];
			base.RemoveSample( name );
			_cache.RemoveSample( data );
		}

		#region Protected Methods
		
		protected override void OnDestroy()
		{
			base.OnDestroy();
			CleanUpCache();
		}
		
		/// <summary>
		/// Only call when destroying!
		/// </summary>
		protected virtual void CleanUpCache()
		{
			if( _cache == null )
				return;

			_cache.Dispose();
			_cache = null;
		}
		
		#endregion
	}
}

