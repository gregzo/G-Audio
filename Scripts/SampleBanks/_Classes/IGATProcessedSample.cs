using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Interface type returned when 
	/// calling GATActiveSampleBank or
	/// GATActiveSampleBank's GetProcessedSample methods.
	/// </summary>
	public interface IGATProcessedSample : IRetainable, IGATDataOwner
	{
		/// <summary>
		/// Pitch as resampling factor.
		/// 1 is original pitch, 2 octave, .5 octave lower...
		/// Only GATResamplingSampleBank hands samples which
		/// may be pitch shifted.
		/// </summary>
		double Pitch{ get; }
		
		/// <summary>
		/// Plays the sample directly through the default player.
		/// Panning is managed by the specified AGATPanInfo object.
		/// </summary>
		IGATBufferedSampleOptions Play( AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample directly through the specified player.
		/// Panning is managed by the specified AGATPanInfo object.
		/// </summary>
		IGATBufferedSampleOptions Play( GATPlayer player, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample directly through the default player.
		/// Panning is managed by the specified AGATPanInfo object.
		/// </summary>
		IGATBufferedSampleOptions PlayScheduled( double dspTime, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample directly through the specified player.
		/// Panning is managed by the specified AGATPanInfo object.
		/// </summary>
		IGATBufferedSampleOptions PlayScheduled( GATPlayer player, double dspTime, AGATPanInfo panInfo, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample through the specified track of the default player.
		/// </summary>
		IGATBufferedSampleOptions  Play( int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>RemoveC
		/// Plays the sample through the specified track of the specified player.
		/// </summary>
		IGATBufferedSampleOptions  Play( GATPlayer player, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample through the specified track of the default player.
		/// </summary>
		IGATBufferedSampleOptions PlayScheduled( double dspTime, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Plays the sample through the specified track of the default player.
		/// </summary>
		IGATBufferedSampleOptions PlayScheduled( GATPlayer player, double dspTime, int trackNb, float gain = 1f, GATPlayer.OnShouldMixSample mixCallback = null );
		
		/// <summary>
		/// Audio data is updated( copied from the original
		/// sample if needed, and processed by GATEnvelope )
		/// only when playing or wrapping a sample in a GATRealTime* class.
		/// You may update data manually if you need the processing to happen
		/// in advance.
		/// </summary>
		void UpdateAudioData();
	}
}
