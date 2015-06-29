//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------

using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Simple component to draw FFT'ed data
	/// using UnityEngine's LineRenderer.
	/// </summary>
	public class DrawFFTModule : DrawAudioModule
	{
		/// <summary>
		/// The fft module that will process
		/// the audio data received in HandleAudioDataUpdate()
		/// </summary>
		public FFTModule fftModule;
		
		int 	_fromIndex, 
		_toIndex;
		
		int _vertexCount;
		
		//We override Start() to specify indexes in the data
		//we are interested in. FFT'ed data is only relevant for
		//the first half of the processed array, and finer control
		//Over relevant frequency bins can be specified on the
		//FFTModule component.
		protected override void Start()
		{
			_fromIndex = fftModule.FirstOutputBinIndex;
			_toIndex   = fftModule.LastOutputBinIndex;
			_vertexCount = _toIndex - _fromIndex;
			base.Start();
		}
		
		protected override void SetVertexCount()
		{
			_lineRenderer.SetVertexCount( _vertexCount );
		}
		
		protected override void HandleAudioDataUpdate()
		{
			if( fftModule == null )
				return;
			
			fftModule.RealFFT( _data );
			
			int i;
			int dataIndex = _fromIndex;
			
			for( i = 0; i < _vertexCount; i++ )
			{
				_lineRenderer.SetPosition( i, new Vector3( i * xFactor, _data[ dataIndex ] * yFactor, 0f ) );
				dataIndex++;
			}
			
			System.Array.Clear( _data, 0, _data.Length );
		}
		
		protected override void HandleNoMoreData()
		{
			int i;
			
			for( i = 0; i < _vertexCount; i++ )
			{
				_lineRenderer.SetPosition( i, new Vector3( i * xFactor, 0f, 0f ) );
			}
		}
	}
}

