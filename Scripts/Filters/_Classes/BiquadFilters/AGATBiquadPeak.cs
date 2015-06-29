using UnityEngine;
using System.Collections;

namespace GAudio
{
	//Must redeclare interface completely for ease of reflection in inspectors 
	public interface IGATBiQuadFilterPeak
	{
		[ FloatPropertyRange( 20f, 5000f ) ]
		float  Freq{ get; set; }
		[ FloatPropertyRange( .5f, 16f ) ]
		double		   Q{ get; set; }
		[ FloatPropertyRange( -50f, 50f ) ]
		float   PeakGain{ get; set; }
		
		[ FloatPropertyRange( 0f, 1f ) ]
		float   Mix{ get; set; }
		
		/// <summary>
		/// Every setter call to a biquad filter
		/// parameter results in a recalculation
		/// of the filter. Using SetParams is more
		/// efficient when more than one parameter 
		/// needs to be updated.
		/// </summary>
		void   SetParams( float frequency, double q, float peakGain );
	}
	
	[ System.Serializable ]
	public abstract class AGATBiQuadPeak : AGATBiQuad, IGATBiQuadFilterPeak
	{
		public override System.Type ControlInterfaceType{ get{ return typeof( IGATBiQuadFilterPeak ); } }
		
		public override float PeakGain
		{
			get{ return _peakGain; }
			set
			{
				_peakGain = value; 
				UpdateV();
				CalcBiquad(); }
		}
		
		public AGATBiQuadPeak(){}
		
		public override void SetParams( float frequency, double q, float peakGain )
		{
			_fq = frequency / GATInfo.OutputSampleRate; 
			_frequency = frequency;
			_Q = q;
			_peakGain = peakGain;
			UpdateK();
			UpdateV ();
			CalcBiquad();
		}
		
		#region Extra cached members and helpers for PeakGain control
		protected double _V; //Cache V and it's square root
		protected double _sqrt2V;
		
		private void UpdateV()
		{
			_V = ( double )Mathf.Pow( 10f, Mathf.Abs( _peakGain ) / 20 );
			_sqrt2V = ( double )Mathf.Sqrt( ( float )( 2 * _V ) );
		}
		
		#endregion
	}
}

