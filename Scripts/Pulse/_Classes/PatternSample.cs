//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// A simple class used by PulsedPatternModule 
	/// to serialize information on the pattern's samples.
	/// </summary>
	[ System.Serializable ]
	public class PatternSample
	{
		/// <summary>
		/// Gets or sets the name of the sample to
		/// request from a GATSampleBank
		/// </summary>
		public string SampleName
		{ 
			get{ return _sampleName; } 
			set
			{
				if( _sampleName == value )
					return;
				
				_sampleName 	 = value;
				this.ProcessedSample = null;
			}
		}
		[ SerializeField ]
		string _sampleName;
		
		/// <summary>
		/// Gets or sets the pitch.
		/// The SemiTones property will be automatically adjusted.
		/// </summary>
		public float Pitch
		{
			get{ return _pitch;}
			set
			{
				if( _pitch == value )
					return;
				
				_pitch = value;
				this.ProcessedSample = null;
			}
		}
		[ SerializeField ]
		float _pitch;
		
		/// <summary>
		/// Gets or sets a pitch shift in semi tones.
		/// Pitch property is automatically adjusted.
		/// </summary>
		public float SemiTones
		{ 
			get{ return _semiTones;}
			set
			{
				if( _semiTones == value )
					return;
				
				_semiTones = value;
				Pitch = GATMaths.GetRatioForInterval( value );
				this.ProcessedSample = null;
			}
		}
		[ SerializeField ]
		float _semiTones;
		
		/// <summary>
		/// Gets or sets the gain for the sample.
		/// </summary>
		public float Gain
		{ 
			get{ return _gain; }
			set
			{ 
				if( _gain == value )
					return;
				
				_gain = value;
			}
		}
		[ SerializeField ]
		float _gain = 1f;
		
		/// <summary>
		/// Gets or sets the runtime processed sample reference.
		/// This property is used by PulsedPatternModule.
		/// </summary>
		public IGATProcessedSample ProcessedSample
		{ 
			get{ return _processedSample; } 
			set
			{
				if( _processedSample != null )
				{
					_processedSample.Release();
				}
				
				_processedSample = value;
				
				if( value != null )
				{
					value.Retain();
				}
			}
		}
		IGATProcessedSample _processedSample;
		
		/// <summary>
		/// Creates a new PatternSample for insertion in a PulsedPatternModule.
		/// </summary>
		public PatternSample( string sampleName, float gain = 1f, int semiTones = 0 )
		{
			_sampleName  = sampleName;
			_gain		 = gain;
			_semiTones   = semiTones;
			_pitch 		 = GATMaths.GetRatioForInterval( semiTones );
		}
	}
}

