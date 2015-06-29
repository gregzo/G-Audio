//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Monobehaviour component that enables
	/// in-inspector configuration of a GATEnvelope object.
	/// In addition, the EnvelopeModule can map the envelope's
	/// length to a PulseModule's pulse period.
	/// </summary>
	[ ExecuteInEditMode ]
	public class EnvelopeModule : MonoBehaviour 
	{
		/// <summary>
		/// Gets or sets the number of samples to process.
		/// </summary>
		/// <value>The length, in samples.</value>
		public int  Length
		{
			get{ return _length; }
			set
			{
				if( _length == value )
					return;
				
				Envelope.Length = value;
				_length = value;
			}
		}
		
		/// <summary>
		/// The number of samples from offset to apply linear fade in to.
		/// </summary>
		public int  FadeIn
		{
			get{ return _fadeIn; }
			set
			{
				if( _fadeIn == value )
					return;
				
				Envelope.FadeInSamples = value;
				_fadeIn = value;
			}
		}
		
		/// <summary>
		/// The number of samples at the end of the chunk to apply linear fade out to.
		/// </summary>
		public int  FadeOut
		{
			get{ return _fadeOut; }
			set
			{
				if( _fadeOut == value )
					return;
				
				Envelope.FadeOutSamples = value;
				_fadeOut = value;
			}
		}
		
		/// <summary>
		/// The position in samples in the original sample where the envelope will begin.
		/// </summary>
		public int  Offset
		{
			get{ return _offset; }
			set
			{
				if( _offset == value )
					return;
				
				Envelope.Offset = value;
				_offset = value;
			}
		}
		
		/// <summary>
		/// If true, the chunk will be normalized at NormalizeValue.
		/// </summary>
		public bool Normalize
		{
			get{ return _normalize; }
			set
			{
				if( _normalize == value )
					return;
				
				Envelope.Normalize = value;
				_normalize = value;
			}
		}
		
		/// <summary>
		/// Value of the highest amplitude in the chunk after normalization.
		/// </summary>
		public float NormalizeValue
		{
			get{ return _normalizeValue; }
			set
			{
				if( _normalizeValue == value )
					return;
				
				Envelope.NormalizeValue = value;
				_normalizeValue = value;
			}
		}
		
		/// <summary>
		/// If true, the chunk data will be reversed before applying fades.
		/// </summary>
		public bool Reverse
		{
			get{ return _reverse; }
			set
			{
				if( _reverse == value )
					return;
				
				Envelope.Reverse = value;
				_reverse = value;
			}
		}
		
		/// <summary>
		/// The pulse we may map
		/// envelope length to
		/// </summary>
		public PulseModule Pulse
		{
			get{ return _pulse; }
			set
			{
				if( _pulse == value )
					return;
				
				if( _mapLengthToPulse )
				{
					if( _pulse != null )
						_pulse.onWillPulse -= OnWillPulse;
					
					if( value != null )
						value.onWillPulse += OnWillPulse;
				}
				
				
				_pulse = value;
			}
		}
		
		/// <summary>
		/// If true, the envelope will automatically be adjusted to variations in the pulse's period according to LengthToPulseRatio.
		/// If the pulse's duration becomes smaller than fadeIn + fadeOut, these parameters are automatically adjusted.
		/// </summary>
		public bool MapLengthToPulse
		{
			get{ return _mapLengthToPulse; }
			set
			{
				if( _mapLengthToPulse == value )
					return;
				
				if( _pulse != null )
				{
					if( value )
					{
						_lengthToPulseRatio = ( ( float )_length / GATInfo.OutputSampleRate ) / ( float )_pulse.PulseInfo.PulseDuration;
						_pulse.onWillPulse += OnWillPulse;
					}
					else
					{
						_pulse.onWillPulse -= OnWillPulse;
					}
				}
				
				_mapLengthToPulse = value;
			}
		}
		
		/// <summary>
		/// The total length of the envelope in relation to the pulse it observes.
		/// </summary>
		public float LengthToPulseRatio
		{
			get{ return _lengthToPulseRatio; }
			set
			{
				if( _lengthToPulseRatio == value )
					return;
				
				_lengthToPulseRatio = value;
				_ratioDidChange = true;
			}
		}
		
		/// <summary>
		/// The actual envelope object that you may pass to 
		/// sample banks when requesting processed samples.
		/// </summary>
		public GATEnvelope Envelope{ get; protected set; }
		
		
		#region Private and Protected
		[ SerializeField ]
		protected int _length = 44100;
		[ SerializeField ]
		protected int _fadeIn = 0;
		[ SerializeField ]
		protected int _fadeOut = 12000;
		[ SerializeField ]
		protected int _offset = 0;
		[ SerializeField ]
		protected bool _normalize = true;
		[ SerializeField ]
		protected float _normalizeValue = .3f;
		[ SerializeField ]
		protected bool _reverse;
		[ SerializeField ]
		protected PulseModule _pulse;
		[ SerializeField ]
		protected bool _mapLengthToPulse;
		[ SerializeField ]
		protected float _lengthToPulseRatio = 1f;
		
		protected bool	_ratioDidChange;
		
		
		void Awake()
		{
			Envelope = new GATEnvelope( _length, _fadeIn, _fadeOut, _offset, _normalize, _normalizeValue );
		}
		
		void OnEnable()
		{
			if( _pulse != null && _mapLengthToPulse )
			{
				_pulse.onWillPulse += OnWillPulse;
			}
			
			if( Envelope == null )
				Envelope = new GATEnvelope( _length, _fadeIn, _fadeOut, _offset, _normalize, _normalizeValue );
		}
		
		void OnDisable()
		{
			if( _pulse != null )
			{
				_pulse.onWillPulse -= OnWillPulse;
			}
		}
		
		/// <summary>
		/// Pulse callback called by 
		/// the PulseModule if mapLengthToPulse is true
		/// </summary>
		public void OnWillPulse( IGATPulseInfo pulseInfo )
		{
			if( pulseInfo.PulseDidChange || _ratioDidChange )
			{
				if( _mapLengthToPulse )
				{
					MapLength( pulseInfo.PulseDuration );
					_ratioDidChange = false;
				}
			}
		}
		
		void MapLength( double pulseDuration )
		{
			int newLength = ( int )( pulseDuration * _lengthToPulseRatio * GATInfo.OutputSampleRate );
			
			if( ( _fadeOut + _fadeIn > newLength ) ) 
			{
				int delta = ( _length - newLength ) / 2 + 1;
				
				if( _fadeIn - delta < 0 )
				{
					_fadeIn = 8;
				}
				else _fadeIn -= delta;
				
				if( _fadeOut - delta < 0 )
				{
					_fadeOut = 8;
				}
				else _fadeOut -= delta;
				
				_length   = newLength;
				
				Envelope.SetParams( _length, _fadeIn, _fadeOut );
				
				#if UNITY_EDITOR
				if( onLengthWasMapped != null )
					onLengthWasMapped( true );
				#endif
			}
			else
			{
				_length = newLength;
				Envelope.Length = newLength;
				
				#if UNITY_EDITOR
				if( onLengthWasMapped != null )
					onLengthWasMapped( false );
				#endif
			}
		}
		
		#endregion
		
		#if UNITY_EDITOR
		public delegate void OnLengthWasMapped( bool didAdaptFades );
		public OnLengthWasMapped onLengthWasMapped;
		
		public int  MaxSamples
		{
			get{ return _maxSamples; }
			set
			{
				if( _maxSamples == value )
					return;
				
				_maxSamples = value;
			}
		}
		[ SerializeField ]
		protected int _maxSamples = 441000;
		
		public int  SamplesPerPixel
		{
			get{ return _samplesPerPixel; }
			set
			{
				if( _samplesPerPixel == value )
					return;
				
				_samplesPerPixel = value;
			}
		}
		[ SerializeField ]
		protected int _samplesPerPixel = 1000;
		
		public float  ScrollXPosition
		{
			get{ return _scrollXPosition; }
			set
			{
				if( _scrollXPosition == value )
					return;
				
				_scrollXPosition = value;
			}
		}
		[ SerializeField ]
		protected float _scrollXPosition;
		#endif
	}
}

