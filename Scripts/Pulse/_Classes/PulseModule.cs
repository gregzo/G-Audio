//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Base class for pulse Monobehaviour components,
	/// </summary>
	public abstract class PulseModule : MonoBehaviour, IGATPulseSender
	{
		public delegate void OnPulseHandler( IGATPulseInfo pulseInfo );
		
		public delegate void OnStepsDidChangeHandler( bool[] newSteps );
		
		/// <summary>
		/// Subscribe to this delegate which fires just before onPulse, but after the pulse info has been updated.
		/// Also fires on bypassed steps.
		/// </summary>
		public OnPulseHandler onWillPulse;
		
		/// <summary>
		/// If true, onPulse will not
		/// fire, only onWillPulse.
		/// </summary>
		/// public bool _bypass;
		public bool Bypass 
		{
			get { return _Bypass; }
			set 
			{
				if( _Bypass == value )
					return;
				
				_Bypass = value;
			}
		}
		[ SerializeField ]
		protected bool _Bypass;
		
		
		/// <summary>
		/// The pulse's period, in seconds.
		/// SubPulseModule only considers
		/// this value if PeriodMode is set
		/// to AbsolutePeriod.
		/// </summary>
		public double Period
		{
			get{ return _Period; }
			set
			{
				if( _Period == value )
					return;
				
				_Period = value;
			}
		}
		[ SerializeField ]
		protected double _Period = 1d;
		
		/// <summary>
		/// onPulse delegate is fired
		/// on true steps.
		/// </summary>
		public virtual bool[] Steps
		{
			get{ return _Steps; }
			set
			{
				if( _Steps.Length == value.Length )
				{
					return;
				}
				
				if( _pulseInfo != null && _pulseInfo.NextStepIndex >= value.Length )
				{
					_pulseInfo.NextStepIndex = 0;
				}
				_pulseInfo.NbOfSteps = value.Length;
				_Steps = value;
				
				if( _onStepsDidChange != null )
					_onStepsDidChange( _Steps );
			}
		}
		[ SerializeField ]
		protected bool[] _Steps = new bool[]{ true, true, true, true };
		
		/// <summary>
		/// For master pulses, this is null.
		/// </summary>
		public virtual PulseModule ParentPulse 
		{
			get{ return null; }
			set{ }
		}
		
		/// <summary>
		/// If true, steps will have
		/// a stepBypassChance probability
		/// of being bypassed.
		/// </summary>
		public bool RandomBypassStep
		{
			get{ return _RandomBypassStep; }
			set
			{
				if( _RandomBypassStep == value )
					return;
				
				_RandomBypassStep = value;
			}
		}
		
		[ SerializeField ]
		protected bool _RandomBypassStep;
		
		/// <summary>
		/// The chance that a step may be randomly bypassed if RandomBypassStep is true.
		/// 1f is 100%.
		/// </summary>
		public float StepBypassChance
		{
			get{ return _StepBypassChance; }
			set
			{
				if( _StepBypassChance == value )
					return;
				
				_StepBypassChance = value;
			}
		}
		
		[ SerializeField ]
		protected float _StepBypassChance;
		
		
		/// <summary>
		/// Retrieves the last
		/// pulse's PulseInfo container.
		/// </summary>
		public 			IGATPulseInfo PulseInfo{ get{ return _pulseInfo; } }
		
		/// <summary>
		/// Retrieves the pulse info of the master pulse if the instance it is called on is a sub pulse.
		/// </summary>
		public abstract IGATPulseInfo MasterPulseInfo{ get; }
		
		
		protected GATPulseInfo   			_pulseInfo;
		protected OnPulseHandler 			_onPulse;
		protected OnPulseHandler			_onPulseControl;
		protected OnStepsDidChangeHandler 	_onStepsDidChange;
		
		
		/// <summary>
		/// Subscribes to the pulse.
		/// </summary>
		public void SubscribeToPulse( IGATPulseClient client )
		{
			_onPulse += client.OnPulse;
			_onStepsDidChange += client.PulseStepsDidChange;
		}
		
		/// <summary>
		/// Unsubscribes to the pulse.
		/// </summary>
		public void UnsubscribeToPulse( IGATPulseClient client )
		{
			_onPulse -= client.OnPulse;
			_onStepsDidChange -= client.PulseStepsDidChange;
		}
		
		public bool RegisterPulseController( IGATPulseController controller )
		{
			if( _onPulseControl != null )
			{
				#if UNITY_EDITOR
				Debug.LogWarning( "There is already a registered controller for this pulse." );
				#endif
				return false;
			}
			_onPulseControl = controller.OnPulseControl;
			return true;
		}
		
		public void UnregisterPulseController( IGATPulseController controller )
		{
			if( controller.OnPulseControl != _onPulseControl )
			{
				#if UNITY_EDITOR
				Debug.LogWarning( "The registered controller must unregister itself." );
				#endif
				return;
			}
			_onPulseControl = null;
		}
		
		#region Protected Methods
		
		protected virtual void Awake()
		{
			_pulseInfo = new GATPulseInfo( this );
			if( _Steps.Length == 0 )
			{	
				this.enabled = false;
			}
		}
		
		protected virtual void OnEnable()
		{
			#if UNITY_EDITOR
			GATManager.onMainThreadResumed += OnDropDownResume;
			#endif
			
			if( _pulseInfo == null )
				_pulseInfo = new GATPulseInfo( this );
		}
		
		protected virtual void OnDisable()
		{
			#if UNITY_EDITOR
			GATManager.onMainThreadResumed -= OnDropDownResume;
			#endif
		}
		
		protected void Pulse()
		{
			if( _onPulseControl != null )
				_onPulseControl( _pulseInfo );
			
			_pulseInfo.WillPulse( _Period );
			
			if( onWillPulse != null )
				onWillPulse( _pulseInfo );
			
			if( _onPulse == null ) //early exit
			{
				_pulseInfo.DidPulse();
				return;
			}
			
			bool doByPass = _Bypass;
			
			if( _Steps[ _pulseInfo.StepIndex ] == false )
			{
				doByPass = true;
			}
			
			if( doByPass == false )
			{
				if( _RandomBypassStep )
					doByPass = ( Random.value < _StepBypassChance );
			}
			
			if( doByPass == false ) 
				_onPulse( _pulseInfo );
			
			_pulseInfo.DidPulse();
		}
		
		public void PulseOneShot( int stepIndex )
		{
			if( _pulseInfo == null )
				return;
			
			_pulseInfo.NextStepIndex 	= stepIndex;
			_pulseInfo.NextPulseDspTime = AudioSettings.dspTime + GATInfo.PulseLatency;
			
			_pulseInfo.WillPulse( _Period );
			
			if( _onPulse != null )
				_onPulse( _pulseInfo ); 
			
			_pulseInfo.DidPulse();
		}
		
		#endregion
		
		protected class GATPulseInfo : IGATPulseInfo
		{
			//IGATPulseInfo implementation
			public double PulseDspTime{ get; private set; }
			public double PulseDuration
			{ 
				get{ return _pulseDuration; }
			}
			public int    StepIndex{ get; private set; } //interface has no setter
			public int    NbOfSteps{ get; set;} //interface has no setter
			public bool   PulseDidChange{ get; set; } //interface has no setter
			public IGATPulseSender PulseSender{ get; private set; }
			
			public double NextPulseDspTime{ get; set; } //interface has no setter
			public int    NextStepIndex{ get; set; }
			
			private double _pulseDuration;
			
			
			public GATPulseInfo( IGATPulseSender sender )
			{
				PulseSender = sender;
				PulseDidChange = true;
			}
			
			public void Init( double period, int nbOfSteps )
			{
				_pulseDuration = period; 
				PulseDidChange = true;
				NbOfSteps = nbOfSteps;
			}
			
			public void SetStart( double dspTime, int stepIndex )
			{
				NextPulseDspTime = dspTime;
				NextStepIndex = stepIndex;
			}
			
			public void WillPulse( double period )
			{
				if( period != _pulseDuration )
				{
					PulseDidChange = true;
					_pulseDuration = period;
				}
				
				PulseDspTime = NextPulseDspTime;
				StepIndex = NextStepIndex;
				
				NextPulseDspTime += _pulseDuration;
				NextStepIndex = ( NextStepIndex + 1 ) % NbOfSteps;
			}
			
			public void DidPulse()
			{
				PulseDidChange = false;
			}
		}
		
		#if UNITY_EDITOR
		protected abstract void OnDropDownResume( double dspDelta );
		
		public enum PulseUnit{ BPM, Sec, Ms }
		
		public PulseUnit PeriodDisplayUnit
		{
			get{ return _pulseUnit; }
			set
			{
				if( _pulseUnit == value )
					return;
				
				_pulseUnit = value;
			}
		}
		[ SerializeField ]
		protected PulseUnit _pulseUnit;
		#endif
	}
}

