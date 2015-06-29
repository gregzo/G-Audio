//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------

using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Interface through which IGATPulseClient objects
	/// receive information on the current pulse.
	/// </summary>
	public interface IGATPulseInfo
	{
		/// <summary>
		/// The precise pulse time.
		/// </summary>
		double PulseDspTime{ get; }
		
		/// <summary>
		/// How long between the current PulseDspTime and the next PulseDspTime.
		/// </summary>
		double PulseDuration{ get; }
		
		/// <summary>
		/// Which step index is pulsing
		/// </summary>
		int	   StepIndex{ get; }
		
		/// <summary>
		/// How many steps in the sender pulse
		/// </summary>
		int    NbOfSteps{ get; }
		
		/// <summary>
		/// Did the pulse's period change since the last pulse?
		/// </summary>
		bool   PulseDidChange{ get; }
		
		/// <summary>
		/// The sender of the pulse
		/// </summary>
		IGATPulseSender PulseSender{ get; }
	}
	
	/// <summary>
	/// Interface for pulse broadcasters - 
	/// MasterPulseModule and SubPulseModule
	/// </summary>
	public interface IGATPulseSender
	{
		/// <summary>
		/// Gets the latest pulse's info.
		/// </summary>
		IGATPulseInfo PulseInfo{ get; }
		
		/// <summary>
		/// If the implementer is a sub pulse, should return
		/// the master pulse's latest pulse info.
		/// </summary>
		IGATPulseInfo MasterPulseInfo{ get; }
	}
	
	/// <summary>
	/// Inteface for pulse listeners
	/// </summary>
	public interface IGATPulseClient
	{
		/// <summary>
		/// Pulse information is passed in pulseInfo
		/// </summary>
		void OnPulse(  IGATPulseInfo pulseInfo );
		
		/// <summary>
		/// The number of steps has been updated.
		/// </summary>
		void PulseStepsDidChange( bool[] newSteps );
	}
	
	/// <summary>
	/// Interface for classes which need to alter pulses right before they fire.
	/// There may only be one pulse controller per pulse.
	/// </summary>
	public interface IGATPulseController
	{
		/// <summary>
		/// Called even on bypassed steps just before PulseInfo is updated
		/// </summary>
		void OnPulseControl( IGATPulseInfo prevPulseInfo );
	}
}

