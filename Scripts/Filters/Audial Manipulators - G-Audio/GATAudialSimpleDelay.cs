using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATAudialSimpleDelay
	{
		[FloatPropertyRange(10,3000)]
		float DelayMS {get;set;}
		
		[FloatPropertyRange(0,1)]
		float DryWet{get;set;}
		
		[FloatPropertyRange(0.1f,1)]
		float Decay{get;set;}
	}
	
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATAudialSimpleDelay : AGATMonoFilter, IGATAudialSimpleDelay
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATAudialSimpleDelay ); } }
		
		private float sampleFrequency;
		void OnEnable(){
			sampleFrequency = GATInfo.OutputSampleRate;
			ChangeDelay();
		}
		
		private float[] delayBuffer;
		private int index = 0;
		
		[SerializeField]
		[Range(10,3000)]
		private float _delayLengthMS = 120;
		public float DelayMS {
			get{
				return _delayLengthMS;
			}
			set{
				if(_delayLengthMS==value)return;
				_delayLengthMS = Mathf.Clamp(value, 10, 3000);
				ChangeDelay();
			}
		}
		
		[SerializeField]
		[Range(0,1)]
		private float _dryWet = 0.5f;
		public float DryWet{
			get{
				return _dryWet;
			}
			set{
				if(_dryWet==value)return;
				_dryWet = Mathf.Clamp(value,0,1);
			}
		}
		
		[SerializeField]
		[Range(0.1f,1)]
		private float _decayLength = 0.25f;
		public float Decay{
			get{
				return _decayLength;
			}
			set{
				if(_decayLength==value)return;
				_decayLength = Mathf.Clamp(value,0.1f, 1);
				ChangeDuration();
			}
		}
		
		private int delaySamples;
		private float output = 0;
		
		private bool   _processingEmptyData = true; 
		private double _emptyTargetTime;           
		
		float totalDelayDuration = 0;
		
		private void ChangeDuration(){
			if(Decay == 1){
				totalDelayDuration = Mathf.Infinity;
			}else{
				totalDelayDuration = Mathf.Log(0.001f,Decay) * DelayMS / 1000;
			}
		}
		
		private void ChangeDelay(){
			delaySamples = (int)Mathf.Round((float)DelayMS*sampleFrequency/1000);
			delayBuffer = new float[delaySamples];
			
			ChangeDuration();
			
		}
		
		public override int NbOfFilterableChannels{ get{ return 1; } }
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			if( emptyData ) 
			{
				double dspTime = AudioSettings.dspTime;
				if( _processingEmptyData )
				{
					if( dspTime > _emptyTargetTime )
						return false;
				}
				else
				{
					_processingEmptyData = true;
					_emptyTargetTime	 = dspTime + ( double )totalDelayDuration; //GZComment: evaluate total delay duration here;
				}
			}
			else
			{
				_processingEmptyData = false;
			}
			
			length += fromIndex;
			int i;
			
			float tempDelay;
			
			float dry;
			float wet;
			
			for( i = fromIndex; i < length; i++ )
			{
				index %= delaySamples;
				tempDelay = delayBuffer[index];
				delayBuffer[index] = 0;
				
				dry = data[i];
				wet = tempDelay;
				output = dry * (1-DryWet) + wet * DryWet;
				data[i] = (float)(output);
				
				delayBuffer[index] += wet * Decay;
				delayBuffer[index] += dry;
				
				index++;
			}
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			length += fromIndex;
			int i;
			
			float tempDelay;
			
			float dry;
			float wet;
			
			for( i = fromIndex; i < length; i++ )
			{
				index %= delaySamples;
				tempDelay = delayBuffer[index];
				delayBuffer[index] = 0;
				
				dry = data[i];
				wet = tempDelay;
				output = dry * (1-DryWet) + wet * DryWet;
				data[i] = (float)(output);
				
				delayBuffer[index] += wet * Decay;
				delayBuffer[index] += dry;
				
				index++;
			}
		}
		
		public override void ResetFilter()
		{
			System.Array.Clear( delayBuffer, 0, delayBuffer.Length );
			index = 0;
			_processingEmptyData = true;
			_emptyTargetTime = 0d;
		}
		
		/// <summary>
		/// Not Implemented
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels )
		{
			throw new GATException( "Audial Simple Delay( G-Audio version ) does not support multichannel audio - please use the standard Audial Delay instead." );
		}
		
		static GATAudialSimpleDelay()
		{
			AGATMonoFilter.RegisterMonoFilter( "Audial > Simple Delay", typeof( GATAudialSimpleDelay ) );
			
		}
	}
}
