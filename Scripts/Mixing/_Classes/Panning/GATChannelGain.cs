//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	// A helper class used by
	// GATFixedPanInfo. 
	public class GATChannelGain
	{
		public int ChannelNumber{ get; protected set; }
		
		protected float _gain;
		
		public virtual float Gain
		{
			get 
			{
				return _gain;
			}
			private set
			{
				_gain = value;
			}
		}
		
		public GATChannelGain( int ichannelnumber, float igain )
		{
			ChannelNumber = ichannelnumber;
			Gain 	      = igain; 
		}
	}
	
	
	// A helper class used by
	// GATDynamicPanInfo. 
	public class GATDynamicChannelGain : GATChannelGain
	{
		public bool ShouldInterpolate{ get; set; }
		public float InterpolationDelta{ get; private set; }
		
		bool  _needsUpdate;
		
		public override float Gain
		{
			get 
			{
				return _gain;
			}
			private set
			{
				_prevGain = _gain;
				_gain = value;
			}
		}
		
		float _prevGain = 0f;
		public float PrevGain{ get{ return _prevGain; } }
		
		float _nextGain;
		public float NextGain
		{	
			set
			{
				_nextGain    = value;
				_needsUpdate = true;
			}
		}
		
		public GATDynamicChannelGain( int ichannelnumber, float igain ) : base( ichannelnumber, igain )
		{}
		
		public void PlayerWillMix()
		{
			if( !_needsUpdate )
			{
				return;
			}
			
			Gain = this._nextGain;
			
			float gainDelta = _gain - _prevGain;
			
			if( gainDelta > GATInfo.MaxGainDelta || gainDelta < -GATInfo.MaxGainDelta )
			{
				InterpolationDelta = gainDelta / GATInfo.AudioBufferSizePerChannel;
				ShouldInterpolate = true;
			}
			else
			{
				ShouldInterpolate = false;
			}
			
			_needsUpdate = false;
		}
		
		public void PlayerDidMix()
		{
			ShouldInterpolate = false;
		}
	}
}

