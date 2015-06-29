//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// A Monobehaviour component that enables
	/// inspecotr friendly configuration of FFT
	/// processing, including windowing of the 
	/// original data and formatting of the output.
	/// </summary>
	public class FFTModule : MonoBehaviour 
	{
		#region Enums
		/// <summary>
		/// The type of window function to apply if 
		/// useWindowFunction is true 
		/// </summary>
		public enum WindowFunction{ Hanning, Hamming }
		
		/// <summary>
		/// Format of the outputted data, from
		/// least to most cpu intensive.
		/// note that only bins for frequencies ranging
		/// from fromFrequency to toFrequency will be formatted,
		/// saving needless calculations.
		/// </summary>
		public enum FFTOutput{ Real, SquareMagnitudes, Magnitudes, Decibels }//from least to most CPU intensive
		
		public enum FFTSize{ AudioBufferSize, Custom }
		
		#endregion
		
		public bool useWindowFunction = true;
		
		/// <summary>
		/// The lowest frequency we are interested in.
		/// The lowest relevant value is SampleRate / FFT size,
		/// but higher values can be specified.
		/// </summary>
		public int 	fromFrequency = 40;
		
		/// <summary>
		/// The highest frequency we are interested in.
		/// The highest relevant frequency is SampleRate/2 ( Nyquist frequency ).
		/// but lower values can be specified.
		/// </summary>
		public int	toFrequency   = 22000; 
		
		public WindowFunction window = WindowFunction.Hanning;
		public FFTOutput output = FFTOutput.Magnitudes;
		public FFTSize fftSize = FFTSize.AudioBufferSize;
		
		/// <summary>
		/// If fftSize is set to FFTSize.Custom
		/// this value will be used instead of the
		/// audio buffer's size. Note that only 
		/// power of 2 values are valid.
		/// </summary>
		public int customFftSize = 1024;
		
		#region Private Members
		//pre computed window data for fftSize
		float[] _windowData;
		
		//Shared imaginary part
		float[] _im;
		
		FloatFFT _fft;
		
		int _appliedFFTSize;
		
		#endregion
		
		/// <summary>
		/// The index in the data array of the
		/// first formatted output.
		/// </summary>
		public int FirstOutputBinIndex{ get; private set; }
		
		/// <summary>
		/// The index in the data array of the
		/// last formatted output.
		/// </summary>
		public int LastOutputBinIndex{ get; private set; }
		
		/// <summary>
		/// Sets and pre-computes the specified
		/// WindowFunction for faster processing.
		/// </summary>
		public void SetWindow( WindowFunction windowFunction )
		{
			if( _windowData == null || _windowData.Length != _appliedFFTSize )
			{
				_windowData = new float[ _appliedFFTSize ];
			}
			
			switch( window )
			{
			case WindowFunction.Hanning:
				
				GATMaths.MakeHanningWindow( _windowData );
				
				break;
				
			case WindowFunction.Hamming:
				
				GATMaths.MakeHammingWindow( _windowData );
				
				break;
			}
		}
		
		/// <summary>
		/// Performs windowed in place real FFT of the passed
		/// data array, reusing a cached array for the
		/// imaginary part.
		/// Formats the specified frequency bins according to
		/// FFTOutput.
		/// </summary>
		public void RealFFT( float[] data )
		{
			int i;
			
			if( data.Length != _appliedFFTSize )
			{
				Debug.LogError( "Expected data lengt: "+_appliedFFTSize+", received "+data.Length );
				return;
			}
			
			if( useWindowFunction )
			{
				for( i = 0; i < _appliedFFTSize; i++ )
				{
					data[i] *= _windowData[i];
				}
			}
			
			_fft.run( data, _im, false );
			
			switch( output )
			{
			case FFTOutput.Decibels:
				ComputeDB( data );
				break;
				
			case FFTOutput.Magnitudes:
				ComputeMagnitudes( data );
				break;
				
			case FFTOutput.SquareMagnitudes:
				ComputeSquareMagnitudes( data );
				break;
				
			case FFTOutput.Real:
				break;
			}
			
			System.Array.Clear( _im, 0, _im.Length ); //Clear imaginary part
		}
		
		#region Private Methods
		void Awake()
		{
			if( fftSize == FFTSize.Custom )
			{
				_appliedFFTSize = Mathf.NextPowerOfTwo( customFftSize );
				if( _appliedFFTSize != customFftSize )
				{
					Debug.LogWarning("This FFT implementation only supports power of 2 lengths, defaulting to next possible value: "+_appliedFFTSize );
					customFftSize = _appliedFFTSize;
				}
			}
			else
			{
				_appliedFFTSize = GATInfo.AudioBufferSizePerChannel;
			}
			
			if( useWindowFunction )
			{
				SetWindow( window );
			}
			
			_im = new float[ _appliedFFTSize ]; //Shared imaginary part
			
			_fft = new FloatFFT();
			uint fftLogSize = ( uint )Mathf.Log ( _appliedFFTSize, 2 );
			_fft.init( fftLogSize );
			
			FirstOutputBinIndex = fromFrequency * _appliedFFTSize / GATInfo.OutputSampleRate;
			LastOutputBinIndex  = toFrequency   * _appliedFFTSize / GATInfo.OutputSampleRate;
		}
		
		void ComputeSquareMagnitudes( float[] data )
		{
			int i;
			for( i = FirstOutputBinIndex; i < LastOutputBinIndex; i++ )
			{
				data[i] = data[i] * data[ i ] + _im[i] * _im[i];
			}
		}
		
		void ComputeMagnitudes( float[] data )
		{
			int i;
			for( i = FirstOutputBinIndex; i < LastOutputBinIndex; i++ )
			{
				data[i] = Mathf.Sqrt( data[i] * data[ i ] + _im[i] * _im[i] );
			}
		}
		
		void ComputeDB( float[] data )
		{
			int i;
			
			const float __LN10  = 2.30258509299f; 
			const float __SCALE = 20f / __LN10;
			
			for( i = FirstOutputBinIndex; i < LastOutputBinIndex; i++ )
			{
				data[i] = __SCALE * Mathf.Log( Mathf.Sqrt( data[i] * data[ i ] + _im[i] * _im[i] ) + float.Epsilon ); //add float.Epsilon to prevent Log(0).
			}
		}
		
		#endregion
	}
}

