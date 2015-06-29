//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Routes a mono stream to a player's track.
	/// Note that trying to route multiple streams
	/// to the same track will throw an exception.
	/// </summary>
	public class GATAudioThreadStreamToTrack : IGATAudioThreadStreamClient, IGATTrackContributor
	{
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
			get{ return _track; }
			set
			{
				if( _track == value )
					return;
				
				if( _track != null )
				{
					_track.UnsubscribeContributor( this );
				}
				
				_streamBuffer = null;
				
				_track = value;
				
				if( _track != null )
				{
					if( _track.SubscribeContributor( this ) == false )
					{
						throw new GATException( "track " + value.TrackNb + " already has a contributor" );
					}
				}
			}
		}
		
		/// <summary>
		/// If true, data from the stream is copied
		/// to the track, which is more efficient than 
		/// mixing but overwrites any other audio.
		/// </summary>
		public bool Exclusive
		{
			get{ return _exclusive; }
			set
			{ 
				if( _exclusive == value )
					return;
				
				_exclusive = value;
			}
		}
		
		#region Private Members
		GATTrack 			  _track;
		IGATAudioThreadStream _stream;
		float[] 			  _streamBuffer;
		int 				  _streamOffset;
		bool 				  _streamDataEmpty;
		
		bool 				  _exclusive;
		#endregion
		
		/// <summary>
		/// Streaming to the specified track will not begin until Start() is called.
		/// </summary>
		public GATAudioThreadStreamToTrack( GATTrack track, IGATAudioThreadStream stream, bool exclusive )
		{
			_track = track;
			_stream = stream;
			_exclusive = exclusive;
		}
		
		/// <summary>
		/// Starts mixing the stream to the specified track.
		/// An exception is thrown if the track already has a contributor.
		/// </summary>
		public void Start()
		{
			if( _track.SubscribeContributor( this ) == false )
			{
				throw new GATException( "Track " + _track.TrackNb + " already has a contributor." );
			}
			_stream.AddAudioThreadStreamClient( this );
		}
		
		/// <summary>
		/// Stops streaming to the track, which
		/// will then be available to other contributors.
		/// </summary>
		public void Stop()
		{
			_track.UnsubscribeContributor( this );
			_stream.RemoveAudioThreadStreamClient( this );
		}
		
		#region Private Methods
		void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
		{
			_streamBuffer = data;
			_streamOffset = offset;
			_streamDataEmpty = emptyData;
		}
		
		bool IGATTrackContributor.MixToTrack( GATData trackMonoBuffer, int trackNb )
		{
			if( _streamBuffer == null || _streamDataEmpty )
				return false;
			
			if( _exclusive )
			{
				trackMonoBuffer.CopyFrom( _streamBuffer, 0, _streamOffset, GATInfo.AudioBufferSizePerChannel );
			}
			else
			{
				trackMonoBuffer.MixFrom( _streamBuffer, 0, _streamOffset, GATInfo.AudioBufferSizePerChannel );
			}
			
			return true;
		}
		
		#endregion
	}
}

