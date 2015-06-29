//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	/// <summary>
	/// Used to subdivide parent pulses.
	/// MasterPulseModules and SubPulseModules 
	/// may be chained for complex behaviours.
	/// </summary>
	[ ExecuteInEditMode ]
	public class SubPulseModule : PulseModule, IGATPulseClient
	{
		/// <summary>
		/// SubPulseModule may have MasterPulseModule
		/// or another SubPulseModule as parent.
		/// </summary>
		/// public PulseModule parentPulse;
		public override PulseModule ParentPulse 
		{
			get { return _ParentPulse; }
			set 
			{
				if( _ParentPulse == value )
					return;
				
				if( _ParentPulse != null )
					_ParentPulse.UnsubscribeToPulse( this );
				
				if( value != null )
				{
					value.SubscribeToPulse( this );
					if( _SubPulseMode != PeriodMode.AbsolutePeriod )
						_shouldUpdatePeriod = true;
					
					_SubscribedSteps = new bool[ value.Steps.Length ];
					value.Steps.CopyTo( _SubscribedSteps, 0 );
				}
				else
				{
					_SubscribedSteps = new bool[0];
				}
				_ParentPulse = value;
			}
		}
		[ SerializeField ]
		PulseModule _ParentPulse;
		
		public override bool[] Steps
		{
			get{ return _Steps; }
			set
			{
				if( value.Length == _Steps.Length )
				{
					return;
				}
				_shouldUpdatePeriod = true;
				base.Steps = value;
			}
		}
		
		/// <summary>
		/// Subdivide Parent: each step will
		/// be of period parentPeriod / nbOfSteps.
		/// 
		/// RatioOfParent: period is not dependent on 
		/// the number of steps, but is a ratio of the parent
		/// pulse's period.
		/// 
		/// Absolute Period: period is absolute, whatever the
		/// parent pulse's period or the number of steps. Note that a 
		/// new pulse will cut off sub-pulses which haven't fired yet.
		/// </summary>
		public enum PeriodMode{ SubdivideParent, RatioOfParent, AbsolutePeriod }
		
		/// <summary>
		/// Current period mode
		/// </summary>
		public PeriodMode SubPulseMode 
		{
			get{ return _SubPulseMode; }
			set 
			{
				if (_SubPulseMode == value)
					return;
				
				_SubPulseMode = value;
				_shouldUpdatePeriod = true;
			}
		}
		
		[ SerializeField ]
		protected PeriodMode _SubPulseMode;
		
		/// <summary>
		/// If SubPulseMode is set to PeriodMode.RatioOfParent,
		/// defines the applied ratio: 2 is half, 3 a third, etc...
		/// </summary>
		public int RatioOfParentPeriod 
		{
			get{ return _RatioOfParentPeriod; }
			set 
			{
				if( _RatioOfParentPeriod == value )
					return;
				
				_RatioOfParentPeriod = value;
				
				if( _SubPulseMode == PeriodMode.RatioOfParent )
					_shouldUpdatePeriod = true;
			}
		}
		[ SerializeField ] 
		protected int _RatioOfParentPeriod;
		
		/// <summary>
		/// If true, every parent pulse's step this instance subscribes may be randomly bypassed.
		/// </summary>
		/// <value><c>true</c> if random bypass parent pulse; otherwise, <c>false</c>.</value>
		public bool RandomBypassParentPulse 
		{
			get{ return _RandomBypassParentPulse; }
			set 
			{
				if( _RandomBypassParentPulse == value )
					return;
				
				_RandomBypassParentPulse = value;
			}
		}
		
		[ SerializeField ]
		protected bool _RandomBypassParentPulse;
		
		/// <summary>
		/// The chance that each parent step may be bypassed if RandomBypassParentPulse is true.
		/// 1f is 100%.
		/// </summary>
		/// <value>The parent pulse bypass chance.</value>
		public float ParentPulseBypassChance 
		{
			get{ return _ParentPulseBypassChance; }
			set 
			{
				if (_ParentPulseBypassChance == value)
					return;
				
				_ParentPulseBypassChance = value;
			}
		}
		[ SerializeField ]
		protected float _ParentPulseBypassChance;
		
		/// <summary>
		/// The steps in the parent pulse that this sub pulse responds to.
		/// Automatically updated should the nb of steps in the parent pulse change.
		/// </summary>
		/// <value>The subscribed steps.</value>
		public bool[] SubscribedSteps 
		{
			get{ return _SubscribedSteps; }
		}
		
		[ SerializeField ]
		protected bool[] _SubscribedSteps = new bool[0];
		
		protected bool _shouldUpdatePeriod;
		
		bool _doSubPulse;
		bool _oneShot;
		
		public override IGATPulseInfo MasterPulseInfo
		{
			get
			{
				PulseModule parent = _ParentPulse;
				
				while( parent.ParentPulse != null )
				{
					parent = parent.ParentPulse;
				}
				
				return parent.MasterPulseInfo;
			}
		}
		
		/// <summary>
		/// Retrieves the MasterPulseModule which fired 
		/// the root pulse.
		/// </summary>
		/// <value>The root pulse.</value>
		public MasterPulseModule RootPulse
		{
			get
			{
				PulseModule parent = _ParentPulse;
				
				while( parent.ParentPulse != null )
				{
					parent = parent.ParentPulse;
				}
				
				return parent as MasterPulseModule;
			}
		}
		
		public void OnPulse( IGATPulseInfo pulseInfo )
		{
			if( _SubPulseMode != PeriodMode.AbsolutePeriod ) // don't update period if mode is AbsolutePeriod
			{
				if( pulseInfo.PulseDidChange || _shouldUpdatePeriod )
					UpdatePeriod();
			}
			
			bool doBypass = _Bypass;
			
			if( !doBypass )
			{
				if( _SubscribedSteps[ pulseInfo.StepIndex ] == false )
				{
					doBypass = true;
				}
				else if( _oneShot == false && _RandomBypassParentPulse )
				{
					if( Random.value < _ParentPulseBypassChance )
						doBypass = true;
				}
			}
			
			if( doBypass )
				return;
			
			_pulseInfo.SetStart( pulseInfo.PulseDspTime, 0 );
			_doSubPulse = true;
		}
		
		void IGATPulseClient.PulseStepsDidChange( bool[] newSteps )
		{
			bool[] cachedSteps = _SubscribedSteps;
			_SubscribedSteps = new bool[ newSteps.Length ];
			
			int stepsToCopy = newSteps.Length > cachedSteps.Length ? cachedSteps.Length : newSteps.Length;
			
			int i;
			for( i = 0; i < stepsToCopy; i++ )
			{
				_SubscribedSteps[ i ] = cachedSteps[ i ];
			}
		}
		
		public void OneShotNextStep()
		{
			_oneShot = true;
			_Bypass  = false;
		}
		
		#region Protected and Private Methods
		
		protected override void OnEnable()
		{
			base.OnEnable();
			
			if( _ParentPulse != null )
				_ParentPulse.SubscribeToPulse( this );
			
			_pulseInfo.Init( _Period, _Steps.Length );
			
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
				EditorApplication.update += Update;
			#endif
		}
		
		protected override void OnDisable()
		{
			base.OnDisable();
			
			if( _ParentPulse != null )
				_ParentPulse.UnsubscribeToPulse( this );
			
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
				EditorApplication.update -= Update;
			#endif
		}
		
		protected void UpdatePeriod()
		{
			double appliedPeriod;
			
			switch( _SubPulseMode )
			{
			case PeriodMode.SubdivideParent:
				appliedPeriod = _ParentPulse.Period / _Steps.Length;
				break;
				
			case PeriodMode.RatioOfParent:
				appliedPeriod = _ParentPulse.Period / _RatioOfParentPeriod;
				break;
				
			case PeriodMode.AbsolutePeriod:
				appliedPeriod = _Period;
				break;
				
			default :
				appliedPeriod = 1d;
				break;
			}
			
			_Period = appliedPeriod;
			_shouldUpdatePeriod = false;
		}
		
		void Update()
		{
			if( !_doSubPulse )
				return;
			
			while( AudioSettings.dspTime + GATInfo.PulseLatency > _pulseInfo.NextPulseDspTime ) //while because we may have more than one subpulse to trigger in a single frame.
			{
				Pulse ();
				
				if( _pulseInfo.NextStepIndex == 0 ) //Done
				{
					_doSubPulse = false;
					
					if( _oneShot )
					{
						_Bypass = true;
						_oneShot = false;
					}
				}
				
			}
		}
		
		#endregion
		
		#if UNITY_EDITOR
		protected override void OnDropDownResume( double dspDelta )
		{
			_doSubPulse = false;
		}
		#endif	
	}
}

