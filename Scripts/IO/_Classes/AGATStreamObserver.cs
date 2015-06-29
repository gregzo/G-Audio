//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Base class for components
	/// which need to subscribe to an IGATAudioThreadStream.
	/// </summary>
	public abstract class AGATStreamObserver : MonoBehaviour
	{
		/// <summary>
		/// The component which we will grab the stream from.
		/// Should implement IGATAudioThreadStreamOwner.
		/// </summary>
		public Component streamComponent;
		
		/// <summary>
		/// Are we observing a GATPlayer's track?
		/// </summary>
		public bool streamIsTrack;
		
		/// <summary>
		/// The index of the stream. A single 
		/// IGATAudioThreadStreamOwner component can broadcast 
		/// multiple streams - which one should we observe?
		/// If streamIsTrack is true, this will specify the track number.
		/// </summary>
		public int streamIndex;
		
		protected IGATAudioThreadStream _stream;
		
		protected virtual void Start () 
		{
			if( _stream != null )
			{
				return;
			}
			
			try
			{
				this.GetStream();
			}
			catch( GATException e )
			{
				this.enabled = false;
				#if UNITY_EDITOR
				Debug.LogError( "No stream found! " + e.Message );
				#endif
			}
		}
		
		/// <summary>
		/// Call from derived classes to attempt to
		/// get a valid stream from the streamComponent and
		/// store it in _stream.
		/// </summary>
		protected void GetStream () 
		{
			if( streamComponent == null )
			{
				streamComponent = gameObject.GetComponent( typeof( IGATAudioThreadStreamOwner ) );
			}
			
			if( streamIsTrack )
			{
				GATPlayer player = streamComponent as GATPlayer;
				if( player == null )
				{
					throw new GATException( "Cannot find GATPlayer to observe track stream. " );
				}
				
				if( streamIndex >= player.NbOfTracks )
				{
					throw new GATException( "Track does not exist!" );
				}
				
				GATTrack track = player.GetTrack( streamIndex );
				
				_stream = track.GetAudioThreadStream( 0 );
			}
			else
			{
				IGATAudioThreadStreamOwner owner = streamComponent as IGATAudioThreadStreamOwner;
				
				_stream = owner.GetAudioThreadStream( streamIndex );
				
				if( owner == null )
				{
					throw new GATException( "Component is not a stream!" );
				}
				
				if( streamIndex >= owner.NbOfStreams )
				{
					throw new GATException( "Requested stream index does not exist." );
				}
			}
		}
	}
}

