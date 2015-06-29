using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATAudialSaturator
	{
		[FloatPropertyRange(0,3)]
		float InGain{get;set;}
		
		[FloatPropertyRange(0,1)]
		float Thresh{get;set;}
		
		[FloatPropertyRange(0,1)]
		float Amount{get;set;}
	}
	
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATAudialSaturator : AGATMonoFilter, IGATAudialSaturator
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATAudialSaturator ); } }
		
		[SerializeField]
		private float _inputGain = 1;
		public float InGain{
			get{
				return _inputGain;
			}
			set{
				if(_inputGain==value)return;
				_inputGain = Mathf.Clamp(value,0,3);
			}
		}
		
		[SerializeField]
		private float _threshold = 0.247f;
		public float Thresh{
			get{
				return _threshold;
			}
			set{
				if(_threshold==value)return;
				_threshold = Mathf.Clamp(value,0,1);
			}
		}
		
		[SerializeField]
		public float _amount = 0.5f;
		public float Amount{
			get{
				return _amount;
			}
			set{
				if(_amount==value)return;
				_amount = Mathf.Clamp(value, 0, 1);
			}
		}
		
		public override int NbOfFilterableChannels{ get{ return 999; } }//Any, filter is pure linear processing
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			if( emptyData )
				return false;
			
			length += fromIndex;
			int i;
			float input;
			
			float sampleAbs;
			float sampleSign;
			
			for( i = fromIndex; i < length; i++ )
			{
				input = data[i] * InGain;
				
				sampleAbs = Mathf.Abs(input);
				sampleSign = Mathf.Sign(input);
				if(sampleAbs>1){
					input = ((Thresh+1)/2) * sampleSign;
				}else if(sampleAbs > Thresh){
					input = (Thresh + (sampleAbs-Thresh)/(1+Mathf.Pow((sampleAbs-Thresh)/(1-Amount),2))) * sampleSign;
				}
				
				data[i] = input;
			}
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			length += fromIndex;
			int i;
			float input;
			
			float sampleAbs;
			float sampleSign;
			
			for( i = fromIndex; i < length; i+= stride )
			{
				input = data[i] * InGain;
				
				sampleAbs = Mathf.Abs(input);
				sampleSign = Mathf.Sign(input);
				if(sampleAbs>1){
					input = ((Thresh+1)/2) * sampleSign;
				}else if(sampleAbs > Thresh){
					input = (Thresh + (sampleAbs-Thresh)/(1+Mathf.Pow((sampleAbs-Thresh)/(1-Amount),2))) * sampleSign;
				}
				
				data[i] = input;
			}
		}
		
		public override void ResetFilter()
		{
			//Don't do anything, filter is passive
		}
		
		/// <summary>
		/// Not applicable: the filter is stateless and may filter any number of channels already.
		/// </summary>
		public override AGATMonoFilter GetMultiChannelWrapper < T > ( int nbOfChannels )
		{
			throw new GATException( "Saturator does not need multi channel wrappers - it can be applied safely to interlaced audio data" );
		}
		
		static GATAudialSaturator()
		{
			AGATMonoFilter.RegisterMonoFilter( "Audial > Saturator", typeof( GATAudialSaturator ) );
			
		}
	}
}
