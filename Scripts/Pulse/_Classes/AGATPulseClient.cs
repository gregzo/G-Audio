//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// You can extend this base class and 
	/// override OnPulse for custom
	/// handling of pulse signals fired by
	/// MasterPulseModule and SubPulseModule.
	/// Inheriting from this class provides 
	/// automatic subscription to a pulse and
	/// the option to subscribe to only specific step indexes.
	/// </summary>
	public abstract class AGATPulseClient : MonoBehaviour, IGATPulseClient
	{
		/// <summary>
		/// The pulse this instance listens to.
		/// Subscription and unsubscription is 
		/// automatically handled.
		/// </summary>
		/// <value>The pulse.</value>
		public PulseModule Pulse
		{ 
			get{ return _pulse; }
			set
			{
				if( _pulse == value )
					return;
				
				UnsubscribeToPulse();
				
				_pulse = value;
				
				if( value != null )
				{
					UpdateSubscribedSteps( value.Steps );
					
					SubscribeToPulseIfNeeded();
				}
			}
		}
		[ SerializeField ]
		protected PulseModule _pulse;
		
		/// <summary>
		/// The steps of the observed pulse that should trigger the OnPulse callback.
		/// Changes to the pulse's number of steps are automatically handled.
		/// </summary>
		public bool[] SubscribedSteps{ get{ return _subscribedSteps; } }
		[ SerializeField ]
		protected bool[] _subscribedSteps = new bool[0];
		
		private bool _isSubscribed;
		
		/// <summary>
		/// Check if _subscribedSteps[ pulseInfo.StepIndex ] when overriding.
		/// </summary>
		public abstract void OnPulse(  IGATPulseInfo pulseInfo );
		
		/// <summary>
		/// Call base.Awake() first if you override 
		/// </summary>
		protected virtual void Awake()
		{
			if( _pulse == null )
			{
				_pulse = gameObject.GetComponent<PulseModule>();
			}
		}
		
		/// <summary>
		/// Call base.OnEnable() first if you override 
		/// </summary>
		protected virtual void OnEnable()
		{
			_isSubscribed = false;
			SubscribeToPulseIfNeeded();
		}
		
		/// <summary>
		/// Call base.OnDisable() first if you override 
		/// </summary>
		protected virtual void OnDisable()
		{
			UnsubscribeToPulse();
		}
		
		/// <summary>
		/// You can override this method to add constraints
		/// </summary>
		protected virtual bool CanSubscribeToPulse()
		{
			return ( !_isSubscribed && _pulse != null );
		}
		
		/// <summary>
		/// If CanSubscribeToPulse() is true, will subscribe 
		/// </summary>
		protected void SubscribeToPulseIfNeeded()
		{
			if( CanSubscribeToPulse() == false )
				return;
			
			_pulse.SubscribeToPulse( this );
			_isSubscribed = true;
		}
		
		/// <summary>
		/// Unsubscribes to the pulse.
		/// </summary>
		protected void UnsubscribeToPulse()
		{
			if( _isSubscribed == false || _pulse == null )
				return;
			
			_pulse.UnsubscribeToPulse( this );
			_isSubscribed = false;
		}
		
		/// <summary>
		/// If a new step has been added to the pulse, should 
		/// _subscribedSteps[ stepIndex ] start true?
		/// </summary>
		protected virtual bool NewPulseStepShouldStartChecked( int stepIndex )
		{
			return true;
		}
		
		void IGATPulseClient.PulseStepsDidChange( bool[] newSteps )
		{
			UpdateSubscribedSteps( newSteps );
		}
		
		void UpdateSubscribedSteps( bool[] newSteps )
		{ 
			bool[] cachedSteps = _subscribedSteps;
			_subscribedSteps = new bool[ newSteps.Length ];
			
			int stepsToCopy = newSteps.Length > cachedSteps.Length ? cachedSteps.Length : newSteps.Length;
			
			int i;
			for( i = 0; i < stepsToCopy; i++ )
			{
				_subscribedSteps[ i ] = cachedSteps[ i ];
			}
			
			if( stepsToCopy >= newSteps.Length )
				return;
			
			for( i = stepsToCopy; i < newSteps.Length; i++ )
			{
				_subscribedSteps[ i ] = NewPulseStepShouldStartChecked( i );
			}
			
		}
	}
}

