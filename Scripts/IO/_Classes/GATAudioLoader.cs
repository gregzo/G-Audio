
using UnityEngine;
using System.Collections;
using GAudio;
#if !GAT_NO_THREADING
using System.ComponentModel;
#endif
using System.Collections.Generic;
using System.IO;

namespace GAudio
{
	#if !GAT_NO_THREADING
	/// <summary>
	/// Base class for asynchronous loading operations.
	/// Request a loading operation from the GATAudioLoader 
	/// singleton, configure, and add to it's operation queue
	/// ( QueueOperation method ). One operation can load multiple
	/// files. Operation objects are one use only.
	/// </summary>
	public abstract class AGATLoadingOperation
	{
		/// <summary>
		/// The per file loading progress callback. Subscribe if
		/// you are loading large files and need to display progress.
		/// </summary>
		public FileLoadProgressHandler OnFileLoadProgress{ get; set; }

		/// <summary>
		/// Callback fired when a file was successfuly loaded.
		/// </summary>
		public FileLoadedHandler OnFileWasLoaded{ get; set; }

		/// <summary>
		/// The operation completed callback. Check Status to make sure
		/// it was successful.
		/// </summary>
		public OperationCompletedHandler OnOperationCompleted{ get; set;}

		/// <summary>
		/// Gets the operation's current status. If Failed, check 
		/// FailReason for more info.
		/// </summary>
		public LoadOperationStatus Status{ get; protected set; }

		/// <summary>
		/// If the operation failed, the probable cause.
		/// </summary>
		public LoadOperationFailReason FailReason{ get; protected set; }

		/// <summary>
		/// Gets the name of the currently loading file,
		/// or the last loaded file if the operation failed / was 
		/// cancelled.
		/// </summary>
		public string CurrentFileName{ get; protected set; }

		/// <summary>
		/// Adds the specified file to the operation. Returns false
		/// if the file format is not supported or does not exist, 
		/// or if the operation is already scheduled / processing.
		/// </summary>
		/// <returns><c>true</c>, if file was added, <c>false</c> otherwise.</returns>
		public abstract bool AddFile( string relativePath, PathRelativeType pathType );
	}
	#endif

	public enum LoadOperationStatus
	{
		Configuring,
		Scheduled,
		Loading,
		Cancelled,
		Failed,
		Done
	}

	public enum LoadOperationFailReason
	{
		Unknown,
		CannotOpenFile,
		NoLargeEnoughChunkInAllocator,
		OutOfPreAllocatedMemory
	}

	#if !GAT_NO_THREADING
	public delegate void OperationCompletedHandler( AGATLoadingOperation operation );
	public delegate void FileLoadProgressHandler( float progress, string fileName );
	public delegate void FileLoadedHandler( GATData[] audioData, string fileName );
	#endif
	/// <summary>
	/// Singleton class for loading of ogg and wav files from
	/// disk. Both async and sync operations supported ( sync operations are
	/// in GATAudioLoaderSync.cs ). Use to load user files or files in the
	/// StreamingAssets folder.
	/// </summary>
	public partial class GATAudioLoader 
	{
		/// <summary>
		/// Static accessor to the singleton instance.
		/// </summary>
		public static GATAudioLoader SharedInstance
		{
			get
			{
				if( __sharedInstance == null )
					return new GATAudioLoader();
				
				return __sharedInstance;
			}
		}

		#if !GAT_NO_THREADING
		/// <summary>
		/// Convenience method for loading files asynchronously
		/// to a sample bank. This method instantiates, configures and 
		/// enqueues a AGATLoadingOperation. Use NewOperation if you need 
		/// more control over progress callbacks.
		/// </summary>
		public void LoadFilesToSampleBank( string[] filePaths, PathRelativeType pathType, GATSampleBank targetBank, GATDataAllocationMode allocationMode, OperationCompletedHandler onOperationCompleted, bool forceMono = false )
		{
			AGATLoadingOperation operation = new LoadingOperation( allocationMode, filePaths.Length, targetBank.AddLoadedFile, forceMono );
			int i;
			for( i = 0; i < filePaths.Length; i++ )
			{
				operation.AddFile( filePaths[ i ], pathType );
			}

			operation.OnOperationCompleted = onOperationCompleted;

			EnqueueOperation( operation );
		}

		/// <summary>
		/// Convenience method for loading files asynchronously
		/// to a sample bank. All wav and ogg files of the specified folder
		/// will be loaded. 
		/// This method instantiates, configures and enqueues a AGATLoadingOperation.
		/// Use the NewOperation method if you need more control over progress callbacks.
		/// </summary>
		public void LoadFolderToSampleBank( string folderPath, PathRelativeType pathType, GATSampleBank targetBank, GATDataAllocationMode allocationMode, OperationCompletedHandler onOperationCompleted, bool forceMono = false )
		{
			folderPath = GATPathsHelper.GetAbsolutePath( folderPath, pathType, false );
			if( Directory.Exists( folderPath ) == false )
			{
				throw new GATException( "No such directory!" );
			}

			string[] files = Directory.GetFiles( folderPath );

			if( files.Length == 0 )
			{
				#if UNITY_EDITOR || GAT_DEBUG
				Debug.LogError( "Directory exists but is empty" );
				#endif
				return;
			}
				
			LoadFilesToSampleBank( files, PathRelativeType.Absolute, targetBank, allocationMode, onOperationCompleted, forceMono );
		}

		/// <summary>
		/// Cancels the current operation as well as all pending operations.
		/// </summary>
		public void CancelPendingOperations()
		{
			if( _bw != null )
			{
				_pendingOperations.Clear();
				_bw.CancelAsync();
			}
		}

		/// <summary>
		/// Gets a LoadingOperation object to which 
		/// you may add files and callbacks. To start 
		/// the operation, call EnqueueOperation.
		/// </summary>
		public AGATLoadingOperation NewOperation( int numFilesToLoad, GATDataAllocationMode allocationMode, FileLoadedHandler onFileWasLoaded, bool forceMono = false )
		{
			return new LoadingOperation( allocationMode, numFilesToLoad, onFileWasLoaded, forceMono ) as AGATLoadingOperation;
		}

		/// <summary>
		/// The operation queue is serial: queued operations
		/// will be executed one by one.
		/// </summary>
		public void EnqueueOperation( AGATLoadingOperation operation )
		{
			if( _bw == null )
			{
				SetupWorker();
				_currentOperation = ( LoadingOperation )operation;
				_currentOperation.OperationWillStart();
				_bw.RunWorkerAsync( _currentOperation );
			}
			else
			{
				_pendingOperations.Enqueue( ( LoadingOperation )operation );
			}
		}


		BackgroundWorker _bw;
		LoadingOperation _currentOperation;

		Queue< LoadingOperation > _pendingOperations = new Queue< LoadingOperation >();

		#endif

		float[] _buffer;
		static GATAudioLoader __sharedInstance;

		GATAudioLoader()
		{
			_buffer = new float[ 16384 * GATInfo.MaxIOChannels ];
			__sharedInstance = this;
		}

		#if !GAT_NO_THREADING
		void SetupWorker()
		{
			_bw = new BackgroundWorker();
			_bw.WorkerSupportsCancellation = true;
			_bw.WorkerReportsProgress = true;
			_bw.DoWork += bw_Work;
			_bw.RunWorkerCompleted += bw_Completed;
			_bw.ProgressChanged += bw_Progress;
		}

		void bw_Work( object sender, DoWorkEventArgs e )
		{
			BackgroundWorker worker = sender as BackgroundWorker;

			if( worker.CancellationPending )
			{
				e.Cancel = true;
				return;
			}

			LoadingOperation op = e.Argument as LoadingOperation;

			GATData[] loadedData;

			while( true )
			{
				loadedData = op.LoadNext( worker, _buffer );
				if( loadedData != null )
				{
					worker.ReportProgress( 0, new ProgressInfo( loadedData, op.CurrentFileName ) );
				}
				else
				{
					break;
				}
			}

			if( op.Status == LoadOperationStatus.Cancelled )
				e.Cancel = true;
		}
		
		void bw_Completed( object sender, RunWorkerCompletedEventArgs e )
		{
			if( e.Error != null )
			{
				_currentOperation.SetStatus( LoadOperationStatus.Failed );
			}
			else if( e.Cancelled )
			{
				_currentOperation.SetStatus( LoadOperationStatus.Cancelled );
				_pendingOperations.Clear();
			}

			if( _currentOperation.OnOperationCompleted != null )
				_currentOperation.OnOperationCompleted( _currentOperation );


			if( _pendingOperations.Count > 0 )
			{
				_currentOperation = _pendingOperations.Dequeue();
				_currentOperation.OperationWillStart();
				_bw.RunWorkerAsync( _currentOperation );
			}
			else
			{
				_bw = null;
				_currentOperation = null;
			}
		}
		
		void bw_Progress( object sender, ProgressChangedEventArgs e )
		{
			ProgressInfo info = ( ProgressInfo )e.UserState;

			if( info.IsFileProgress )
			{
				if( _currentOperation.OnFileLoadProgress != null )
					_currentOperation.OnFileLoadProgress( info.progress, info.fileName );
			}
			else
			{
				if( _currentOperation.OnFileWasLoaded != null )
					_currentOperation.OnFileWasLoaded( info.data, info.fileName );
			}
		}

		private struct ProgressInfo
		{
			public bool IsFileProgress{ get{ return data == null; } }
			public readonly float progress;
			public readonly GATData[] data;
			public readonly string fileName;

			public ProgressInfo( float iprogress, string filename )
			{
				data = null;
				progress = iprogress;
				fileName = filename;
			}

			public ProgressInfo( GATData[] channelData, string filename )
			{
				progress = 1f;
				data = channelData;
				fileName = filename;
			}
		}
		
		private class LoadingOperation : AGATLoadingOperation
		{	
			Queue< string > 			_paths;
			GATDataAllocationMode		_allocationMode;
			bool 						_forceMono;
			bool 						_reportsProgress;

			public LoadingOperation( GATDataAllocationMode allocationMode, int numFiles, FileLoadedHandler handler, bool forceMono = false )
			{
				_allocationMode 	= allocationMode;
				_paths 				= new Queue< string >( numFiles );
				Status 				= LoadOperationStatus.Configuring;
				OnFileWasLoaded 	= handler;
				_forceMono 			= forceMono;
			}

			public void SetStatus( LoadOperationStatus status )
			{
				Status = status;
			}
			
			public override bool AddFile( string relativePath, PathRelativeType pathType )
			{
				string extension;

				if( Status != LoadOperationStatus.Configuring )
				{
					#if UNITY_EDITOR || GAT_DEBUG
					Debug.LogError( "Cannot add file to operation in progress!");
					#endif
					return false;
				}

				extension = System.IO.Path.GetExtension( relativePath ).ToLower();
					
				if( extension != ".wav" && extension != ".ogg" )
				{
					#if UNITY_EDITOR || GAT_DEBUG
					Debug.LogError( "Only ogg and wav files supported by GATAsyncAudioLoader, ignoring " + relativePath );
					#endif
					return false;
				}
			
				string path = GATPathsHelper.GetAbsolutePath( relativePath, pathType, false );

				if( System.IO.File.Exists( path ) == false )
				{
					#if UNITY_EDITOR || GAT_DEBUG
					Debug.LogError( "File not found, ignoring " + path );
					#endif
					return false;
				}
				
				_paths.Enqueue( path );
				
				return true;
			}

			public void OperationWillStart()
			{
				Status = LoadOperationStatus.Loading;
				_reportsProgress = OnFileLoadProgress != null;
			}

			public GATData[] LoadNext( BackgroundWorker worker, float[] deInterleaveBuffer )
			{
				GATData[] containers;
				int i;
				int channels;

				if( _paths.Count == 0 )
				{
					Status = LoadOperationStatus.Done;
					return null;
				}

				AGATAudioFile file;

				try
				{
					file = AGATAudioFile.OpenAudioFileAtPath( _paths.Dequeue() );
				}
				catch( System.Exception e )
				{
#if UNITY_EDITOR
					Debug.LogException( e );
#endif
					FailReason = LoadOperationFailReason.CannotOpenFile;
					Status = LoadOperationStatus.Failed;
					return null;
				}

				using( file )
				{
					channels = file.Channels;
					CurrentFileName = file.FileName;
					if( !_forceMono && channels > 1 )
					{
						containers = new GATData[ channels ];
					}
					else
					{
						containers = new GATData[ 1 ];
					}

					for( i = 0; i < containers.Length; i++ ) 
					{
						if( _allocationMode == GATDataAllocationMode.Fixed ) //GZComment: allocation fail leaks
						{
							try{ containers[ i ] = GATManager.GetFixedDataContainer( file.NumFrames, file.FileName ); }
							catch( System.Exception ex )
							{ 
								ReleaseContainers( containers ); 
								Status = LoadOperationStatus.Failed;
								FailReason = LoadOperationFailReason.OutOfPreAllocatedMemory;
								#if UNITY_EDITOR
								Debug.LogException( ex );
								#endif
								return null;
							}
						}
						else if( _allocationMode == GATDataAllocationMode.Managed ) //GZComment: allocation fail leaks
						{
							try
							{ 
								containers[ i ] = GATManager.GetDataContainer( file.NumFrames ); 
							}
							catch( System.Exception ex )
							{ 
								ReleaseContainers( containers );
								Status = LoadOperationStatus.Failed;
								FailReason = LoadOperationFailReason.NoLargeEnoughChunkInAllocator;
								#if UNITY_EDITOR
								Debug.LogException( ex );
								#endif
								return null;
							}
						}
						else 
						{
							containers[ i ] = new GATData( new float[ file.NumFrames ] );
						}
					}

					int framesPerRead;
					int framesRead;
					int totalFramesRead = 0; 

					if( channels > 1 )
					{
						framesPerRead = deInterleaveBuffer.Length / channels;

						while( true )
						{
							if( worker.CancellationPending )
							{
								ReleaseContainers( containers );
					
								return null;
							}

							framesRead = file.ReadNextChunk( deInterleaveBuffer, 0, framesPerRead );
							
							if( _forceMono ) // Only supported for stereo files
							{
								int numSamples = framesRead * channels;

								for( i = 0; i < numSamples; i+= channels )
								{
									deInterleaveBuffer[ i ] += deInterleaveBuffer[ i + 1 ];
								}
								
								containers[ 0 ].CopyFromInterlaced( deInterleaveBuffer, framesRead, totalFramesRead, 0, channels );
							}
							else
							{
								for( i = 0; i < channels; i++ )
								{
									containers[ i ].CopyFromInterlaced( deInterleaveBuffer, framesRead, totalFramesRead, i, channels ); 
								}
							}
							
							totalFramesRead += framesRead;

							if( _reportsProgress )
								worker.ReportProgress( 0, new ProgressInfo( ( float )totalFramesRead / file.NumFrames, file.FileName ) );
							
							if( framesRead < framesPerRead )
								break;
						}
					}
					else
					{

						int framesLeft;

						while( totalFramesRead < file.NumFrames )
						{
							if( worker.CancellationPending )
							{
								ReleaseContainers( containers );
								return null;
							}

							framesLeft = file.NumFrames - totalFramesRead;
							framesPerRead = ( framesLeft < 16384 ? framesLeft : 16384 );
							framesRead = file.ReadNextChunk( containers[ 0 ].ParentArray, containers[ 0 ].MemOffset + totalFramesRead, framesPerRead );
							totalFramesRead += framesRead;

							if( _reportsProgress )
								worker.ReportProgress( 0, new ProgressInfo( ( float )totalFramesRead / file.NumFrames, file.FileName ) );
						}
					}
				}


				return containers;
			}

			void ReleaseContainers( GATData[] containers )
			{
				int i;
				if( containers != null )
				{
					for( i = 0; i < containers.Length; i++ )
					{
						if( containers[ i ] != null )
							containers[ i ].Release();
					}
				}
			}
		}

		#endif
	}
}

