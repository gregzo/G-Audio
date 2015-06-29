//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	[ ExecuteInEditMode ]
	/// <summary>
	/// Fires onPulse 
	/// delegates at fixed interval 
	/// on specified steps, and onShouldPulse on every step.
	/// </summary>
	public class MasterPulseModule : PulseModule
	{
		/// <summary>
		/// Should the pulse start automatically after StartDelay?
		/// </summary>
		/// <value><c>true</c> if start pulse auto; otherwise, <c>false</c>.</value>
		public bool StartPulseAuto
		{
			get{ return _StartPulseAuto; }
			set
			{
				if( _StartPulseAuto == value )
					return;
				
				_StartPulseAuto = value;
			}
		}
		[ SerializeField ]
		protected bool _StartPulseAuto = true;
		
		/// <summary>
		/// Delay in seconds to wait after the scene loaded 
		/// before starting to pulse if StartPulseAuto is true.
		/// </summary>
		public double StartDelay
		{
			get{ return _StartDelay; }
			set
			{
				if( _StartDelay == value )
					return;
				
				_StartDelay = value;
			}
		}
		[ SerializeField ]
		protected double _StartDelay = 1f;
		
		/// <summary>
		/// Is the pulse active?
		/// </summary>>
		public bool IsPulsing{ get{ return _isPulsing; } }
		protected bool _isPulsing;
		
		/// <summary>
		/// Starts the pulse at the specified stepIndex, and optionally dspTime.
		/// Omitting the dspTime parameter will start the pulse immediately.
		/// </summary>
		public void StartPulsing( int stepIndex, double dspTime = 0d )
		{
			if( _isPulsing )
				return;
			
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
				EditorApplication.update += Update;
			#endif
			if( dspTime > AudioSettings.dspTime )
			{
				_pulseInfo.SetStart( dspTime, stepIndex );
			}
			else
			{
				_pulseInfo.SetStart( AudioSettings.dspTime + GATInfo.PulseLatency, stepIndex );
			}
			
			_isPulsing = true;
		}
		
		/// <summary>
		/// Stop pulsing immediately.
		/// </summary>
		public void Stop()
		{
			#if UNITY_EDITOR
			if( Application.isPlaying == false )
				EditorApplication.update -= Update;
			#endif
			_isPulsing = false;
		}
		
		public override IGATPulseInfo MasterPulseInfo{ get{ return _pulseInfo; } }
		
		protected override void OnEnable()
		{
			base.OnEnable();
			
			_pulseInfo.Init( _Period, _Steps.Length );
			
			if( _isPulsing )
				Stop ();
		}
		
		protected override void OnDisable()
		{
			base.OnDisable();
			if( _isPulsing )
				Stop ();
		}
		
		void Start()
		{
			#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				if( _StartPulseAuto )
				{
					StartPulsing( 0, GATInfo.SyncDspTime + _StartDelay );
				}
			}
			#else
			if( _StartPulseAuto )
			{
				StartPulsing( 0, GATInfo.SyncDspTime + _StartDelay );
			}
			#endif
		}
		
		void Update()
		{
			if( !_isPulsing )
				return;
			
			while( ( AudioSettings.dspTime + GATInfo.PulseLatency ) > _pulseInfo.NextPulseDspTime )
			{
				Pulse ();
			}
		}
		
		#if UNITY_EDITOR
		protected override void OnDropDownResume( double dspDelta )
		{
			_pulseInfo.SetStart( _pulseInfo.NextPulseDspTime + dspDelta, _pulseInfo.NextStepIndex );
		}
		
		#endif
	}
}






