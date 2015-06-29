//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Streams a mono stream to a GATPlayer Track.
	/// </summary>
	public class StreamToTrackModule : AGATStreamObserver
	{
		/// <summary>
		/// The player through which to route the stream.
		/// If null, the default player will be used.
		/// </summary>
		public GATPlayer player;
		
		/// <summary>
		/// The track number in the specified player 
		/// through which to route the stream. Only 
		/// one stream can be routed to a given track.
		/// </summary>
		public int trackNumber;
		
		/// <summary>
		/// If true, data from the stream is copied
		/// to the track, which is more efficient than 
		/// mixing but overwrites any other audio.
		/// </summary>
		public bool exclusive;
		
		/// <summary>
		/// Use with caution: if the 
		/// new TargetTrack already has 
		/// a contributor stream, an 
		/// exception will be thrown. 
		/// Changing tracks live may also result
		/// in pops. 
		/// </summary>
		public GATTrack TargetTrack
		{
			get
			{
				if( _streamToTrack == null )
					return null;
				
				return _streamToTrack.TargetTrack;
			}
			
			set
			{
				if( _streamToTrack == null )
					return;
				
				_streamToTrack.TargetTrack = value;
			}
		}
		
		protected GATAudioThreadStreamToTrack _streamToTrack;
		
		void Awake()
		{
			if( player == null )
			{
				player = GATManager.DefaultPlayer;
			}
		}
		
		protected override void Start () 
		{
			base.Start ();
			
			if( _stream == null )
			{
				this.enabled = false;
			}
			
			if( _stream.NbOfChannels != 1 )
			{
				this.enabled = false;
				throw new GATException( "Only mono streams can be routed to a track. You may use GATAudioThreadStreamSplitter to split an interleaved stream in as many mono streams." );
			}
			
			GATTrack track = player.GetTrack( trackNumber );
			
			_streamToTrack = new GATAudioThreadStreamToTrack( track, _stream, exclusive );
			
			_streamToTrack.Start();
		}
		
		void OnEnable()
		{
			if( _streamToTrack != null )
				_streamToTrack.Start();
		}
		
		void OnDisable()
		{
			if( _streamToTrack != null )
				_streamToTrack.Stop();
		}
	}
}

