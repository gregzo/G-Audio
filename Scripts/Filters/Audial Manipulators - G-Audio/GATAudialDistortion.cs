using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	public interface IGATAudialDistortion
	{
		[FloatPropertyRange(0,3)]
		float InGain{get;set;}
		
		[FloatPropertyRange(0.00001f,1)]
		float Thresh{get;set;}
		
		[FloatPropertyRange(0,1)]
		float DryWet{get;set;}
		
		[FloatPropertyRange(0,5)]
		float OutGain{get;set;}
	}
	
	#if UNITY_EDITOR
	[ InitializeOnLoad ]
	#endif
	public class GATAudialDistortion : AGATMonoFilter, IGATAudialDistortion
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATAudialDistortion ); } }
		
		[SerializeField]
		private float _inputGain = 1;
		public float InGain{
			get{
				return _inputGain;
			}
			set{
				if(_inputGain==value)return;
				_inputGain = Mathf.Clamp(value, 0, 3);
			}
		}
		
		[SerializeField]
		private float _threshold = 0.036f;
		public float Thresh{
			get{
				return _threshold;
			}
			set{
				if(_threshold==value)return;
				_threshold = Mathf.Clamp(value,0.00001f, 1);
			}
		}
		
		[SerializeField]
		private float _dryWet = 0.258f;
		public float DryWet{
			get{
				return _dryWet;
			}
			set{
				if(_dryWet==value)return;
				_dryWet = Mathf.Clamp(value, 0, 1);
			}
		}
		
		[SerializeField]
		private float _outputGain = 1;
		public float OutGain{
			get{
				return _outputGain;
			}
			set{
				if(_outputGain==value)return;
				_outputGain = Mathf.Clamp(value, 0, 5);
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
			
			for( i = fromIndex; i < length; i++ )
			{
				input = data[i];
				input *= InGain;
				
				float distortedSample = input;
				if(Mathf.Abs(distortedSample)>Thresh){
					distortedSample = Mathf.Sign(distortedSample);
				}
				
				data[i] = ((1-DryWet)*input + DryWet * distortedSample)*OutGain;
			}
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			length += fromIndex;
			int i;
			float input;
			
			for( i = fromIndex; i < length; i+= stride )
			{
				input = data[i];
				input *= InGain;
				
				float distortedSample = input;
				if(Mathf.Abs(distortedSample)>Thresh){
					distortedSample = Mathf.Sign(distortedSample);
				}
				
				data[i] = ((1-DryWet)*input + DryWet * distortedSample)*OutGain;
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
			throw new GATException( "Distortion does not need multi channel wrappers - it can be applied safely to interlaced audio data" );
		}
		
		static GATAudialDistortion()
		{
			AGATMonoFilter.RegisterMonoFilter( "Audial > Distortion", typeof( GATAudialDistortion ) );
			
		}
	}
}
