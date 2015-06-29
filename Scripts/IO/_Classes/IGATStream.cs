//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------

namespace GAudio
{
	public delegate void OnAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream );
	
	/// <summary>
	/// Interface to a stream of audio data
	/// running on the audio thread. 
	/// </summary>
	public interface IGATAudioThreadStream
	{
		/// <summary>
		/// A stream can be observed by multiple clients.
		/// Subscribe through this method.
		/// </summary>
		void AddAudioThreadStreamClient( IGATAudioThreadStreamClient client );
		
		/// <summary>
		/// Always unsubscribe from a stream when you're done!
		/// </summary>
		void RemoveAudioThreadStreamClient( IGATAudioThreadStreamClient client );
		
		/// <summary>
		/// Number of interleaved channels in the stream
		/// </summary>
		int            NbOfChannels{ get; }
		
		/// <summary>
		/// Number of frames in the stream buffer
		/// </summary>
		int    BufferSizePerChannel{ get; }
		
		/// <summary>
		/// Pointer to the stream's buffer
		/// Warning: if passed to unmanaged land,
		/// use pointer arithmetics and the Offset 
		/// property to adjust the pointer natively.
		/// </summary>
		/// <value>The buffer pointer.</value>
		System.IntPtr BufferPointer{ get; }
		
		/// <summary>
		/// Valid data offset in the stream buffer
		/// </summary>
		int	           BufferOffset{ get; }
		
		/// <summary>
		/// Name of the stream, mostly useful
		/// for debugging.
		/// </summary>
		/// <value>The name of the stream.</value>
		string           StreamName{ get; }
	}
	
	/// <summary>
	/// Interface audio stream observers must implement
	/// in order to subscribe to the stream.
	/// </summary>
	public interface IGATAudioThreadStreamClient
	{
		/// <summary>
		/// Callback received when the stream updates.
		/// Valid data starts at data[ offset ] and is 
		/// of length stream.NbOfChannels * stream.BufferSizePerChannel.
		/// The emptyData parameter can be used to optimize processing
		/// of empty chunks.
		/// </summary>
		void HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream );
	}
	
	/// <summary>
	/// Streams are requested from stream owners, which
	/// might own multiple streams ( GATAudioThreadStreamSplitter, for example ).
	/// </summary>
	public interface IGATAudioThreadStreamOwner
	{
		IGATAudioThreadStream GetAudioThreadStream( int streamIndex );
		int NbOfStreams{ get; }
	}
	
	public interface IGATFilterableStream 
	{
		GATFiltersHandler FiltersHandler{ get; }
	}
	
	public interface IGATTrackContributor
	{
		bool MixToTrack( GATData trackMonoBuffer, int trackNb ); //return true if you do contribute, else false
	}

}

