//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// A simple component that draws
	/// the raw audio data ( -1 to 1 floats )
	/// using UnityEngine's LineRenderer.
	/// </summary>
	public class DrawAudioModule : AGATChunkCopyClientBehaviour
	{
		
		public Color 	startColor,
		endColor;
		
		public float 	lineWidthStart = 1f, 
		lineWidthEnd = 1f;
		
		/// <summary>
		/// X distance between two data points.
		/// </summary>
		public float xFactor = .01f;
		
		/// <summary>
		/// Multiplyer for translating values into y position.
		/// </summary>
		public float yFactor = 5f;
		
		/// <summary>
		/// The Material used by the LineRenderer 
		/// </summary>
		public Material lineMaterial;
		
		/// <summary>
		/// Should HandleNoMoreData() be called in Start() ?
		/// Useful to draw a zero-state line before any data is received.
		/// </summary>
		public bool handleNoMoreDataInStart;
		
		/// <summary>
		/// LineRenderer getter-
		/// </summary>
		public LineRenderer Line{ get{ return _lineRenderer; } }
		
		protected LineRenderer _lineRenderer;
		
		// Initializes the LineRenderer and
		// calls HandleNoMoreData() if required.
		// If you override in a derived class, you
		// should call base.Start() at the end of your 
		// implementation.
		protected override void Start()
		{
			base.Start ();
			_lineRenderer = gameObject.AddComponent<LineRenderer>() as LineRenderer;
			_lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			_lineRenderer.receiveShadows = false;
			SetVertexCount();
			_lineRenderer.SetColors( startColor, endColor );
			_lineRenderer.SetWidth( lineWidthStart, lineWidthEnd );
			_lineRenderer.material = lineMaterial;
			_lineRenderer.useWorldSpace = false;
			
			if( handleNoMoreDataInStart )
				HandleNoMoreData();
		}
		
		
		// Sets the vertex count to data length.
		// Overridable for custom behaviour.
		protected virtual void SetVertexCount()
		{
			_lineRenderer.SetVertexCount( _data.Length );
		}
		
		//This override is called when you can thread safely handle new data. Let's draw it.
		protected override void HandleAudioDataUpdate()
		{
			int i;
			int length = _data.Length;
			
			for( i = 0; i < length; i++ )
			{
				_lineRenderer.SetPosition( i, new Vector3( i * xFactor, _data[i] * yFactor, 0f ) );
			}
		}
		
		//No more data, we draw the zero-state line.
		protected override void HandleNoMoreData()
		{
			int i;
			int length = _data.Length;
			
			for( i = 0; i < length; i++ )
			{
				_lineRenderer.SetPosition( i, new Vector3( i * xFactor, 0f, 0f ) );
			}
		}
	}
}

 