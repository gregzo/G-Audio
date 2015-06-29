//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Handles pre-processing of samples
	/// and keeps track of update times of
	/// it's processing properties.
	/// 
	/// Properties may be set at any time,
	/// all IGATProcessedSample objects that
	/// refer to the GATEnvelope will update their data
	/// accordingly.
	/// 
	/// EnvelopeModule component wraps a GATEnvelope
	/// in an inspector friendly package if needed.
	/// </summary>
	public class GATEnvelope
	{
		
		#region Processing Properties
		/// <summary>
		/// How much of the original sample
		/// should be processed? Clamped at a
		/// minimum of 256.
		/// </summary>
		public int Length
		{
			get{ return _length; } 
			set
			{
				if( value < 64 )
					value = 64;
				
				_length = value;
				LastLengthChangeTime = AudioSettings.dspTime;
				LastChangeTime = LastLengthChangeTime;
			}
		}
		
		/// <summary>
		/// On how many samples from
		/// index 0 will a linear fade in
		/// from gain 0 to 1 occur?
		/// </summary>
		public int FadeInSamples
		{
			get{ return _fadeInSamples; } 
			set
			{
				if( value < 0 )
					value = 0;
				_fadeInSamples = value;
				LastChangeTime = AudioSettings.dspTime;
			}
		}
		
		/// <summary>
		/// On how many samples from
		/// index ( Length - FadeInSamples ) 
		/// will a linear fade out
		/// from gain 1 to 0 occur?
		/// </summary>
		public int FadeOutSamples
		{
			get{ return _fadeOutSamples; } 
			set
			{
				if( value < 0 )
					value = 0;
				_fadeOutSamples = value;
				LastChangeTime = AudioSettings.dspTime;
			}
		}
		
		/// <summary>
		/// Offset in the original sample,
		/// in samples.
		/// </summary>
		public int Offset
		{
			get{ return _offset; } 
			set
			{
				if( value < 0 )
					value = 0;
				_offset = value;
				LastChangeTime = AudioSettings.dspTime;
			}
		}
		
		/// <summary>
		/// If true, the chunk will be normalized at NormalizeValue.
		/// </summary>
		public bool Normalize
		{
			get{ return _normalize; } 
			set
			{
				_normalize = value;
				LastChangeTime = AudioSettings.dspTime;
			}
		}
		
		/// <summary>
		/// Value of the highest amplitude in the chunk after normalization.
		/// </summary>
		public float NormalizeValue
		{
			get{ return _normalizeValue; } 
			set
			{
				if( value < 0f )
					value = 0f;
				_normalizeValue = value;
				
				if( _normalize )
				{
					LastChangeTime = AudioSettings.dspTime;
				}
			}
		}
		
		/// <summary>
		/// If true, the chunk data will be reversed before applying fades.
		/// </summary>
		public bool Reverse
		{
			get{ return _reverse; } 
			set
			{
				_reverse = value;
				LastChangeTime = AudioSettings.dspTime;
			}
		}
		#endregion
		
		public double       LastChangeTime{ get; protected set; }
		public double LastLengthChangeTime{ get; protected set; }
		
		#region Backing fields
		protected int 	_length,
		_fadeInSamples,
		_fadeOutSamples,
		_offset;
		
		protected bool	_normalize,
		_reverse;
		
		protected float _normalizeValue;
		#endregion
		
		/// <summary>
		/// Note that by default, doNormalize is true and sample chunks will be normalized at .3f.
		/// </summary>
		public GATEnvelope( int length, int fadeInSamples, int fadeOutSamples, int offset, bool doNormalize = true,  float normalizeValue = .3f )
		{
			_length			= length;
			_fadeInSamples 	= fadeInSamples;
			_fadeOutSamples = fadeOutSamples;
			_offset			= offset;
			_normalizeValue	= normalizeValue;
			_normalize		= doNormalize;
			
			LastLengthChangeTime = AudioSettings.dspTime;
			LastChangeTime 		 = LastLengthChangeTime;
		}
		
		/// <summary>
		/// Immediately applies all
		/// processing properties to
		/// the passed GATData object.
		/// </summary>
		public virtual void ProcessSample( GATData sample )
		{
			if( _normalize )
				sample.Normalize( _normalizeValue );
			
			if( _reverse )
				sample.Reverse();
			
			if( _fadeInSamples > 0 )
				sample.FadeIn( _fadeInSamples );
			
			if( _fadeOutSamples > 0 )
				sample.FadeOut( _fadeOutSamples );
		}
		
		/// <summary>
		/// Sets the most commononly used parameters in one call.
		/// </summary>
		public void SetParams( int length, int fadeIn, int fadeOut )
		{
			_length 		= length;
			_fadeInSamples 	= fadeIn;
			_fadeOutSamples = fadeOut;
			
			LastLengthChangeTime = AudioSettings.dspTime;
			LastChangeTime 		 = LastLengthChangeTime;
		}
		
		#region NullEnvelope
		protected GATEnvelope() //empty ctor used only by NullEnvelope
		{
		}
		
		/// <summary>
		/// Passing this static reference when requesting a pitch shifted processed sample will
		/// result in pitch shifting the entire source sample.
		/// </summary>
		public static readonly NullEnvelope nullEnvelope;
		static GATEnvelope()
		{
			nullEnvelope = new NullEnvelope();
		}
		
		
		public class NullEnvelope : GATEnvelope
		{
			public override void ProcessSample( GATData sample ) //no processing
			{
			}
		}
		#endregion
	}
}

