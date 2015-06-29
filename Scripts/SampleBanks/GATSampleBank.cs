//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace GAudio
{
	/// <summary>
	/// The base class Monobehaviour component 
	/// for loading samples from resources.
	/// Use GATActiveSampleBank or GATResamplingSampleBank
	/// if you intend to take advantage of the processed sample caching
	/// functionnalities G-Audio provides.
	/// </summary>
	[ ExecuteInEditMode]
	public class GATSampleBank : MonoBehaviour
	{
		/// <summary>
		/// The sound bank that will be loaded
		/// </summary>
		public GATSoundBank SoundBank
		{ 
			get{ return _soundBank; } 
		}
		
		public List< GATSoundBank > SoundBanks
		{
			get{ return _SoundBanks; }
		}
		[ SerializeField ]
		protected List< GATSoundBank > _SoundBanks = new List< GATSoundBank >();
		
		/// <summary>
		/// Should LoadAll() be called in Awake()?
		/// </summary>
		public bool LoadInAwake
		{ 
			get{ return _loadInAwake; }
			set
			{
				if( _loadInAwake == value )
					return;
				
				_loadInAwake = value;
			}
		}
		
		/// <summary>
		/// In which type of memory should the samples be loaded?
		/// </summary>
		public GATDataAllocationMode AllocationMode
		{ 
			get{ return _allocationMode; }
			set
			{ 
				if( IsLoaded == false )
					_allocationMode = value; 
			}
		}
		
		/// <summary>
		/// Gets all loaded sample names.
		/// If the bank isn't loaded, returns null;
		/// </summary>
		public string[] AllSampleNames
		{ 
			get
			{ 
				if( IsLoaded == false )
					return null;

				if( _allKeys == null )
				{
					_allKeys = new string[ _samplesByName.Count ];
					_samplesByName.Keys.CopyTo( _allKeys, 0 );
				}
				return _allKeys; 
			} 
		}

		/// <summary>
		/// Gets the number of samples currently loaded.
		/// </summary>
		public int NumberOfSamplesInBank
		{ 
			get
			{ 
				if( _allSamples == null )
					return 0;

				return _allSamples.Count; } 
		}
		
		/// <summary>
		/// Is the referenced SoundBank loaded?
		/// </summary>
		public bool IsLoaded{ get{ return _allSamples != null; } }
		
		/// <summary>
		/// Loads all samples from the referenced SoundBank.
		/// Sync operation
		/// </summary>
		public virtual void LoadAll()
		{
			if( _soundBank == null )
			{
				return;
			}

			if( _allSamples == null )
				InitCollections();

			Dictionary< string, GATData > loadedSamples = _soundBank.LoadAll( _allocationMode );

			foreach( KeyValuePair< string, GATData > pair in loadedSamples )
			{
				AddSample( pair.Value, pair.Key );
			}
		
			_allKeys = null;
		}

		/// <summary>
		/// Loads specific samples from the referenced Sound Bank.
		/// Sync operation.
		/// </summary>
		public void LoadSamplesNamed( List< string > sampleNames )
		{
			Dictionary< string, GATData > loadedSamples = _soundBank.LoadSamplesNamed( sampleNames, _allocationMode );

			foreach( KeyValuePair< string, GATData > pair in loadedSamples )
			{
				AddSample( pair.Value, pair.Key );
			}
			
			_allKeys = null;
		}

		#if !GAT_NO_THREADING
		/// <summary>
		/// Loads specific StreamingAssets samples from the referenced Sound Bank.
		/// Async operation.
		/// </summary>
		public void LoadStreamingAssetsAsync( List< string > sampleNames, OperationCompletedHandler onCompleted )
		{	
			string[] fullPaths = _soundBank.GetFullPathsInStreamingAssets( sampleNames );
			GATAudioLoader.SharedInstance.LoadFilesToSampleBank( fullPaths, PathRelativeType.Absolute, this, _allocationMode, onCompleted ); 
		}
		#endif
		
		/// <summary>
		/// Releases all loaded samples.
		/// </summary>
		public virtual void UnloadAll()
		{
			if( _allSamples == null )
			{
				return;
			}
			
			//_soundBank.FreeAll();
			int i;

			if( _allSamples != null )
			{
				for( i = 0; i < _allSamples.Count; i++ )
				{
					_allSamples[ i ].Release();
				}
			}

			_samplesByName  = null;
			_allKeys 		= null;
			_allSamples 	= null;
		}

		/// <summary>
		/// The numer of samples which may be dynamically added to the bank.
		/// Not required, used for pooling cache objects at initialization.
		/// </summary>
		public int extraCapacity;

		/// <summary>
		/// Adds a sample to the bank, which will retain it, and manage cache for it
		/// if GATActiveSamplebank.
		/// </summary>
		public virtual void AddSample( GATData data, string sampleName )
		{
			if( _allSamples == null )
				InitCollections();

			_samplesByName.Add( sampleName, data );
			_allSamples.Add( data );
			data.Retain();
			_allKeys = null;
		}

		/// <summary>
		/// Compatibility with GATAudioLoader. 
		/// </summary>
		public void AddLoadedFile( GATData[] channelsData, string fileName )
		{
			if( _allSamples == null )
				InitCollections();

			fileName = Path.GetFileNameWithoutExtension( fileName );

			if( channelsData.Length == 1 )
			{
				AddSample( channelsData[ 0 ], fileName );
				return;
			}

			int i;

			for( i = 0; i < channelsData.Length; i++ )
			{
				AddSample( channelsData[ i ], string.Format( "{0}_{1}", fileName, i.ToString() ) );
			}
		}

		/// <summary>
		/// Releases a specific sample and it's associated cache.
		/// </summary>
		public virtual void RemoveSample( string sampleName )
		{
			GATData data = _samplesByName[ sampleName ];
			data.Release();
			_samplesByName.Remove( sampleName );
			_allSamples.Remove( data );
			_allKeys = null;

			if( _allSamples.Count == 0 )
			{
				_allSamples = null;
				_samplesByName = null;
			}
		}

		/// <summary>
		/// Releases specific samples and their associated cache.
		/// </summary>
		public void RemoveSamples( List< string > sampleNames )
		{
			int i;
			for( i = 0; i < sampleNames.Count; i++ )
			{
				RemoveSample( sampleNames[ i ] );
			}
		}

		/// <summary>
		/// Is the specified sample loaded?
		/// </summary>
		public bool ContainsSampleNamed( string sampleName )
		{
			if( _samplesByName == null )
				return false;

			return _samplesByName.ContainsKey( sampleName );
		}
		
		/// <summary>
		/// Grabs a reference to the samples data.
		/// Note that changes to the data will be persistent
		/// until the bank is reloaded.
		/// </summary>
		public GATData GetAudioData( string sampleName )
		{
			return _samplesByName[ sampleName ];
		}
		
		public virtual GATData GetAudioData( int indexInBank )
		{
			return _allSamples[ indexInBank ];
		}
		
		/// <summary>
		/// Convenience method for copying
		/// a chunk of a sample to a GATData 
		/// container.
		/// </summary>
		public void FillWithSampleData( GATData data, string sampleName, int fromIndex, int length )
		{
			FillWithSampleData( _samplesByName[ sampleName ], data, fromIndex, length );
		}
		
		public void FillWithSampleData( GATData data, int indexInBank, int fromIndex, int length )
		{
			FillWithSampleData( _allSamples[ indexInBank ], data, fromIndex, length );
		}
		
		/// <summary>
		/// Convenience method for copying
		/// a resampled chunk of a sample to a GATData 
		/// container. Target length is the requested ouptut length.
		/// </summary>
		public void FillWithResampledData( GATData data, string sampleName, int fromIndex, int targetLength, double pitch )
		{
			FillWithResampledData( _samplesByName[ sampleName ], data, fromIndex, targetLength, pitch );
		}
		
		public void FillWithResampledData( GATData data, int indexInBank, int fromIndex, int targetLength, double pitch )
		{
			FillWithResampledData( _allSamples[ indexInBank ], data, fromIndex, targetLength, pitch );
		}

		/// <summary>
		/// Returns the sample closest to the provided midicode and a pitch shift value 
		/// to be applied to the sample
		/// </summary>
		/// <returns>The sample index for midi code.</returns>
		/// <param name="midiCode">Midi code.</param>
		/// <param name="pitchShift">Pitch shift.</param>
		public GATData GetClosestSampleForMidiCode( float midiCode, out float pitchShift )
		{
			int index = _allSamples.Count - 1;
			int i;
			GATSampleInfo info;
			float delta;
			float minDelta = float.MaxValue;
			
			for( i = 0; i < _allSamples.Count; i++ )
			{
				info = _soundBank.SampleInfos[ i ];
				
				delta =  Mathf.Abs( midiCode - info.MidiCode );
				
				if( delta < minDelta )
				{
					minDelta = delta;
				}
				else
				{
					index = i - 1;
					break;
				}
			}
			
			pitchShift = GATMaths.GetRatioForInterval( midiCode - ( float )_soundBank.SampleInfos[ index ].MidiCode );
			
			return _allSamples[ index ];
		}
		
		#region Private and Protected
		
		[ SerializeField ]
		protected bool _loadInAwake = true;
		protected GATSoundBank _soundBank;
		[ SerializeField ]
		protected GATDataAllocationMode _allocationMode = GATDataAllocationMode.Unmanaged;
		
		protected string[]  _allKeys;
		protected List< GATData > _allSamples;
		
		protected virtual void Awake()
		{

			#if UNITY_EDITOR
			
			if( Application.isPlaying == false )
			{
				if( _SoundBanks.Count == 1 && _SoundBanks[ 0 ] == null )
				{
					return;
				}
			}
			#endif
			
			UpdateSoundBank();
			
			if( _loadInAwake )
			{
				#if UNITY_EDITOR
				if( Application.isPlaying )
					LoadAll();
				#else
				LoadAll();
				#endif
			}
		}

		protected int _totalCapacity;

		protected virtual void InitCollections()
		{
			int capacity = extraCapacity;
			if( _soundBank != null )
				capacity += _soundBank.SampleInfos.Count;

			_samplesByName = new Dictionary< string, GATData >( capacity );
			_allSamples    = new List< GATData >( capacity );
			_totalCapacity = capacity;
		}
		
		void UpdateSoundBank()
		{
			if( _soundBank == null && _SoundBanks.Count == 0 )
				return;

			int i;
			GATSoundBank soundBank = null;
			
			for( i = 0; i < _SoundBanks.Count; i++ )
			{
				soundBank = _SoundBanks[ i ];
				if( soundBank == null )
					continue;
				
				if( soundBank.SampleRate == GATInfo.OutputSampleRate )
				{
					_soundBank = soundBank;
					break;
				}
			}
			
			if( soundBank == null )
			{
				#if UNITY_EDITOR || GAT_DEBUG
				Debug.LogError( string.Format( "SampleBank {0} could not find a sound bank of appropriate sample rate to load. Ouptut sample rate: {1}khz", this.name, GATInfo.OutputSampleRate.ToString () ) );               
				#endif
				return;
			}
		}
		#if UNITY_EDITOR
		public void EditorUpdateSoundBank( GATSoundBank bank = null )
		{
			if( bank != null )
			{
				_SoundBanks.Add ( bank );
			}
				
			UpdateSoundBank();
		}
		#endif
		
		
		void OnEnable()
		{
			#if UNITY_EDITOR
			if( _soundBank == null )
				UpdateSoundBank();
			
			if( _AutoLoadInEditMode && Application.isPlaying == false && _samplesByName == null )
			{
				LoadAll();
			}
			#endif
		}
		
		protected virtual void OnDestroy()
		{
			UnloadAll();
		}
		
		protected Dictionary< string, GATData > _samplesByName;

		protected void FillWithSampleData( GATData sourceData, GATData targetData, int fromIndex, int length )
		{
			bool tooLong = ( fromIndex + length > sourceData.Count );
			
			int appliedLength = tooLong ? sourceData.Count - fromIndex : length;
			
			if( appliedLength < 0 )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "requested offset is out of bounds." );
				#endif
				return;
			}
			
			sourceData.CopyTo( targetData, 0, fromIndex, appliedLength );
			
			#if GAT_DEBUG
			if( tooLong )
			{
				Debug.LogWarning( "requested length at fromIndex out of bounds, filling as much as possible" );
			}
			#endif
		}

		protected void FillWithResampledData( GATData sourceData, GATData targetData, int fromIndex, int targetLength, double pitch )
		{	
			//check that we have enough samples to fulfill the request:
			int appliedLength = GATMaths.ClampedResampledLength( sourceData.Count - fromIndex, targetLength, pitch );
			
			if( appliedLength < 0 )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "requested offset is out of bounds." );
				#endif
				return;
			}
			
			sourceData.ResampleCopyTo( fromIndex, targetData, appliedLength, pitch ); 
			
			//if we did not have enough samples, clear the rest:
			if( appliedLength < targetLength )
			{
				targetData.Clear( appliedLength, targetData.Count - appliedLength );
			}
		}
		
		#endregion
		
		#if UNITY_EDITOR
		public bool AutoLoadInEditMode 
		{
			get{ return _AutoLoadInEditMode; }
			set 
			{
				if (_AutoLoadInEditMode == value)
					return;
				
				_AutoLoadInEditMode = value;
			}
		}
		[ SerializeField ]
		protected bool _AutoLoadInEditMode;
		#endif
	}
}
