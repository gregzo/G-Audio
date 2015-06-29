using UnityEngine;
using System.Collections;

namespace GAudio
{
	// A wrapper for applying biquad filters to multi channel
	// streams. Was a nested private class, but it must have
	// it's own file to be serialized properly by Unity.
	public class GATMultiChannelBiquad : AGATMonoFilter, IGATBiQuadFilterPeak
	{
		public float Freq
		{
			get
			{
				return _biquads[0].Freq;
			}
			set
			{
				int i;
				for( i = 0; i < _biquads.Length; i++ )
				{
					_biquads[i].Freq = value;
				}
			}
		}
		
		public double Q
		{
			get
			{
				return _biquads[0].Q;
			}
			set
			{
				int i;
				for( i = 0; i < _biquads.Length; i++ )
				{
					_biquads[i].Q = value;
				}
			}
		}
		
		public float PeakGain
		{
			get
			{
				return _biquads[0].PeakGain;
			}
			set
			{
				int i;
				for( i = 0; i < _biquads.Length; i++ )
				{
					_biquads[i].PeakGain = value;
				}
			}
		}
		
		public float Mix
		{
			get
			{
				return _biquads[0].Mix;
			}
			set
			{
				int i;
				for( i = 0; i < _biquads.Length; i++ )
				{
					_biquads[i].Mix = value;
				}
			}
		}
		
		[ SerializeField ]
		private AGATBiQuad[] _biquads;
		[ SerializeField ]
		private int _nbOfChannels;
		
		protected bool _inZeroState = true;
		
		public void InitMultiChannelBiquad<T>( int nbOfChannels, T filterInstance ) where T : AGATMonoFilter
		{
			int i;
			_nbOfChannels = nbOfChannels;
			_biquads = new AGATBiQuad[ nbOfChannels ];
			_biquads[ 0 ] = filterInstance as AGATBiQuad;
			for( i = 1; i < nbOfChannels; i++ )
			{
				_biquads[i] = ScriptableObject.CreateInstance< T >() as AGATBiQuad;
			}
		}
		
		public void SetParams( float frequency, double q, float peakGain )
		{
			int i;
			for( i = 0; i < _nbOfChannels; i++ )
			{
				_biquads[i].SetParams( frequency, q, peakGain );
			}
		}
		
		public override bool ProcessChunk( float[] data, int fromIndex, int length, bool emptyData )
		{
			int i;
			
			if( emptyData )
			{
				if( !_inZeroState )
				{
					ResetFilter();
					_inZeroState = true;
				}
				
				return false;
			}
			
			for( i = 0; i < _nbOfChannels; i++ )
			{
				_biquads[i].ProcessChunk( data, fromIndex + i, length, _nbOfChannels );
			}
			
			return true;
		}
		
		public override void ProcessChunk( float[] data, int fromIndex, int length, int stride )
		{
			throw new GATException( "stride should not be specified when dealing with wrapped filters. Use ProcessChunk( data, index, length ) instead." );
		}
		
		void OnDestroy()
		{
			int i;
			
			if( Application.isPlaying )
			{
				for( i = 0; i < _biquads.Length; i++ )
				{
					Destroy( _biquads[i] );
				}
			}
			else
			{
				for( i = 0; i < _biquads.Length; i++ )
				{
					DestroyImmediate( _biquads[i] );
				}
			}
		}
		
		public override AGATMonoFilter GetMultiChannelWrapper< T > (int nbOfChannels)
		{
			throw new GATException("already a multichannel wrapper! ");
		}
		
		public override void ResetFilter()
		{
			int i;
			
			for( i = 0; i < _biquads.Length; i++ )
			{
				_biquads[i].ResetFilter();
			}
		}
		
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATBiQuadFilterPeak ); } }
		
		public override int NbOfFilterableChannels{ get{ return 999; } }
	}
}


