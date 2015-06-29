//
//  GATBiquad.cs, Based on Nigel Redmon's C++ Biquad class
//	
//	Ported to C#, adapted and optimized by Gregorio Zanon - 2013
//  ****************** CHANGES *********************************
//  Inheritance has replaced the original switch, and processing
//  occurs in chunks instead of by individual sample.
//  Optimizations include caching of pre-computed constants, 
//  which depend only on frequency or peak gain and don't need to be 
//  recalculated in CalcBiquad.

//  Copyright 2012 Nigel Redmon
//
//  For a complete explanation of the Biquad code:
//  http://www.earlevel.com/main/2012/11/26/biquad-c-source-code/
//
//  License:
//
//  This source code is provided as is, without warranty.
//  You may copy and distribute verbatim copies of this document.
//  You may modify and use this source code to create binary code
//  for your own purposes, free or commercial.

using UnityEngine;
using System.Collections;

namespace GAudio
{
	public interface IGATBiQuadFilter
	{
		[ FloatPropertyRange( 20f, 5000f ) ]
		float  Freq{ get; set; }
		
		[ FloatPropertyRange( .5f, 16f ) ]
		double Q{ get; set; }
		
		[ FloatPropertyRange( 0f, 1f ) ]
		float 		Mix{ get; set; }
		
		/// <summary>
		/// Every setter call to a biquad filter
		/// parameter results in a recalculation
		/// of the filter. Using SetParams is more
		/// efficient when more than one parameter 
		/// needs to be updated.
		/// </summary>
		void   SetParams( float frequency, double q, float peakGain );
	}
	
	/// <summary>
	/// Base class for all biquad filters
	/// 
	/// </summary>
	[ System.Serializable ]
	public abstract class AGATBiQuad : AGATMonoFilter, IGATBiQuadFilter
	{
		#region Coefficients and cached constants
		[ SerializeField ]
		protected float _fq;
		[ SerializeField ]
		protected float _frequency = 440f;
		[ SerializeField ]
		protected float  _peakGain = 5f;
		protected double _a0, _a1, _a2, _b1, _b2;
		[ SerializeField ]
		protected double _Q = .76d;
		protected double _z1, _z2;
		protected double _K; //Cache K
		protected double _KSq; //cache squared K
		protected bool _inZeroState = true;
		
		[ SerializeField ]
		protected float _mix = 1f;
		#endregion
		
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATBiQuadFilter ); } }
		/// <summary>
		/// Frequency in Hz.
		/// </summary>
		public virtual float Freq
		{
			get{ return _frequency; }
			set
			{ 
				_fq = value / GATInfo.OutputSampleRate; 
				_frequency = value;
				UpdateK();
				CalcBiquad(); 
			}
		}
		
		/// <summary>
		/// Quality. Typical values
		/// range from .5 to 50.
		/// See http://www.rane.com/note170.html for 
		/// more information regarding this parameter.
		/// </summary>
		public virtual double Q
		{
			get{ return _Q; }
			set{ _Q = value; CalcBiquad(); }
		}
		
		public float Mix
		{ 
			get{ return _mix; } 
			set
			{ 
				if( _mix == value )
					return;
				
				_mix = value; 
			} 
		}
		
		/// <summary>
		/// Peak gain( in db ) only
		/// affects shelf and peak filters.
		/// </summary>
		public virtual float PeakGain{ get; set; }
		
		/// <summary>
		/// Biquad filters can only filter a single channel, but have a multi channel wrapper class that may be instantiated by calling GetMultiChannelWrapper</summary> T >().
		/// </summary>
		/// <value>The nb of filterable channels.</value>
		public override int NbOfFilterableChannels{ get{ return 1; } }
		
		/// <summary>
		/// Every setter call to a biquad filter
		/// parameter results in a recalculation
		/// of the filter. Using SetParams is more
		/// efficient when more than one parameter 
		/// needs to be updated.
		/// </summary>
		public virtual void SetParams( float frequency, double q, float peakGain )
		{
			_fq = frequency / GATInfo.OutputSampleRate; 
			_frequency = frequency;
			_Q = q;
			_peakGain = peakGain;
			UpdateK();
			CalcBiquad();
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			int i;
			float sample;
			double result;
			
			if( _mix <= 0f )
				return;
			
			length += fromIndex;
			
			if( _mix < 1f )
			{
				for(  i = fromIndex; i < length; i+= stride )
				{
					sample = data[i];
					result = sample * _a0 + _z1;
					_z1 = sample * _a1 + _z2 - _b1 * result;
					_z2 = sample * _a2 - _b2 * result;
					data[i] =  ( float )result * _mix + data[ i ] * ( 1f - _mix );
				}
			}
			else
			{
				for(  i = fromIndex; i < length; i+= stride )
				{
					sample = data[i];
					result = sample * _a0 + _z1;
					_z1 = sample * _a1 + _z2 - _b1 * result;
					_z2 = sample * _a2 - _b2 * result;
					data[i] = ( float )result;
				}
			}
		}
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			if( emptyData )
			{
				if( !_inZeroState )
				{
					ResetFilter();
					_inZeroState = true;
				}
				
				return false;
			}
			
			if( _mix <= 0f )
				return true;
			
			length += fromIndex;
			int i;
			float sample;
			double result;
			
			if( _mix < 1f )
			{
				for(  i = fromIndex; i < length; i++ )
				{
					sample = data[i];
					result = sample * _a0 + _z1;
					_z1 = sample * _a1 + _z2 - _b1 * result;
					_z2 = sample * _a2 - _b2 * result;
					data[i] = ( float )result * _mix + data[ i ] * ( 1f - _mix );
				}
			}
			else
			{
				for(  i = fromIndex; i < length; i++ )
				{
					sample = data[i];
					result = sample * _a0 + _z1;
					_z1 = sample * _a1 + _z2 - _b1 * result;
					_z2 = sample * _a2 - _b2 * result;
					data[i] = ( float )result;
				}
			}
			
			
			if( _inZeroState )
				_inZeroState = false;
			
			return true;
		}
		
		public override void ResetFilter()
		{
			_a0 = 0d;
			_a1 = 0d;
			_a2 = 0d;
			_b1 = 0d;
			_b2 = 0d;
			_z1 = 0d;
			_z2 = 0d;
			CalcBiquad();
			
			_inZeroState = true;
		}
		
		#region protected helper methods
		
		protected virtual void OnEnable()
		{
			SetParams( _frequency, _Q, _peakGain );
		}
		
		protected void UpdateK()
		{
			_K = Mathf.Tan( Mathf.PI * _fq ); //As K depends only on frequency, recalculate it here and not in CalcBiquad
			_KSq = _K * _K;
		}
		
		protected abstract void CalcBiquad();
		
		#endregion
		
		public override AGATMonoFilter GetMultiChannelWrapper < T >( int nbOfChannels ) 
		{
			GATMultiChannelBiquad wrapper = ScriptableObject.CreateInstance< GATMultiChannelBiquad >();
			wrapper.InitMultiChannelBiquad< T >( nbOfChannels, this as T );
			
			return wrapper;
		}
	}
}


