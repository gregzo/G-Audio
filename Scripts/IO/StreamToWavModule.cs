
//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Component for safe writing of wav files
	/// from audio streams. Writing occurs in a
	/// seperate thread so as not to burden the audio thread.
	/// Supports sample accurate start and duration.
	/// </summary>
	public class StreamToWavModule : AGATStreamObserver, IGATAudioThreadStreamClient {
		
		/// <summary>
		/// The path at which to write the file.
		/// The path will be processed occording to pathType.
		/// To change paths at runtime, use SetPath.
		/// </summary>
		public string path;
		
		/// <summary>
		/// Type of path: relative to Application.dataPath or
		/// persistentDataPath, or absolute.
		/// </summary>
		public PathRelativeType pathType;
		
		/// <summary>
		/// Are we currently writing data?
		/// </summary>
		public bool IsWriting{ get{ return _writing; } }
		
		/// <summary>
		/// Get the absolute path for path.
		/// </summary>
		/// <value>The absolute path.</value>
		public string AbsolutePath{ get{ return _absPath; } }
		
		/// <summary>
		/// Gets the URL for the written file.
		/// Useful for reading with Unity's WWWW class.
		/// </summary>
		public string FileURL
		{ 
			get
			{ 
				if( _absPath == null )
					return null;
				
				return GATPathsHelper.URLFromFilePath( _absPath ); 
			}
		}
		
		#region Private
		GATAsyncWavWriter _writer;
		
		bool _writing;
		bool _waiting;
		
		string _absPath;
		
		double _targetDspTime;
		
		int _writtenFrames;
		int _recFixedFrames;
		
		protected override void Start () 
		{
			base.Start();
			
			if( path != "" )
			{
				SetPath( path, pathType );
			}
		}
		
		void IGATAudioThreadStreamClient.HandleAudioThreadStream( float[] data, int offset, bool emptyData, IGATAudioThreadStream stream )
		{
			int framesToWrite = stream.BufferSizePerChannel;
			
			if( _writing == false  )
			{
				double dspTime 		= AudioSettings.dspTime;
				double nextDspTime  = dspTime + GATInfo.AudioBufferDuration;
				
				if( _targetDspTime < dspTime )
					_targetDspTime = dspTime;
				
				if( nextDspTime > _targetDspTime )
				{
					if( _waiting )
					{
						_waiting = false;
						_writing = true;
						
						int frameOffsetInBuffer = ( int )( ( _targetDspTime - dspTime ) * GATInfo.OutputSampleRate );
						offset += frameOffsetInBuffer * stream.NbOfChannels;
						framesToWrite -= frameOffsetInBuffer;
					}
					else
					{
						return;
					}
				}
				else 
				{
					return;
				}
			}
			
			if( _recFixedFrames > 0 && ( _writtenFrames + framesToWrite > _recFixedFrames ) )
			{
				framesToWrite = _recFixedFrames - _writtenFrames;
				_writer.WriteStreamAsync( data, offset, framesToWrite );
				EndWriting();
				
				return;
			}
			
			_writer.WriteStreamAsync( data, offset, framesToWrite );
			_writtenFrames += framesToWrite;
		}
		
		void OnDisable()
		{
			if( _writing )
			{
				_writer.StopAndFinalize();
				_writing = false;
			}
		}
		
		void OnDestroy()
		{
			if( _writer != null )
				_writer.Dispose();
		}
		
		#endregion
		
		/// <summary>
		/// Sets the path to which to write to.
		/// If the path contains a non-existent directory, 
		/// it will be created.
		/// </summary>
		public string SetPath( string newPath, PathRelativeType newPathType )
		{
			path 		= newPath;
			pathType 	= newPathType;
			
			_absPath = GATPathsHelper.GetAbsolutePath( newPath, newPathType, true );
			
			return _absPath;
		}
		
		/// <summary>
		/// Starts writing the stream at precise dspTime if one is specified.
		/// If no dspTime is specified, or if the specified dspTime is too soon,
		/// starts writing asap.
		/// If recNumFrames is specified, writing will automatically stop
		/// once the precise number of frames has been written, else you should call
		/// EndWriting.
		/// </summary>
		public void StartWriting( double targetDspTime = 0d, int recNumFrames = -1 )
		{
			if( _writing )
				return;
			
			_recFixedFrames = recNumFrames;
			_writtenFrames = 0;
			_waiting = true;
			_targetDspTime = targetDspTime;
			_writer = new GATAsyncWavWriter( _absPath, _stream.NbOfChannels, true );
			_writer.PrepareToWrite();
			_stream.AddAudioThreadStreamClient( this );
		}
		
		/// <summary>
		/// Stops writing and finalizes the file's header.
		/// As this occurs on a seperate thread, you should not assume
		/// that the file is ready to be opened immediately.
		/// </summary>
		public void EndWriting()
		{
			if( !_writing )
				return;
			
			_writing = false;
			_stream.RemoveAudioThreadStreamClient( this );
			_writer.StopAndFinalize();
		}
	}
}

