//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

public enum SoundBankClipStatus{ Ok, NotInResources, NotFound, Moved }
#endif

namespace GAudio
{
	public class GATSoundBank : ScriptableObject
	{
		public int SampleRate{ get{ return _sampleRate; } }
		[ SerializeField ]
		int _sampleRate;
		
		public List< GATSampleInfo > SampleInfos{ get{ return _sampleInfos; } }
		[ SerializeField ]
		List< GATSampleInfo > _sampleInfos = new List< GATSampleInfo >();
		
		//Dictionary < string, GATData > _loadedSamples;
		
		public int TotalUncompressedBytes{ get{ return _totalUncompressedBytes; } }
		[ SerializeField ]
		int _totalUncompressedBytes;
		
		public string HumanReadableUncompressedSize
		{
			get
			{
				if( _humanReadableUncompressedSize == null )
					_humanReadableUncompressedSize = _totalUncompressedBytes.HumanReadableBytes();
				
				return _humanReadableUncompressedSize;
			}
		}
		string _humanReadableUncompressedSize;
		
		public void Init( int sampleRate )
		{
			if( _sampleRate != 0 )
			{
				throw new GATException( "Sound Bank's sample rate is already set.");
			}
			_sampleRate = sampleRate;
		}
		
		public void AddSample( string pathInResources, string guid, int numChannels, int samplesPerChannel, bool isStreamingAsset )
		{
			GATSampleInfo info = new GATSampleInfo( pathInResources, guid, numChannels, samplesPerChannel, isStreamingAsset );
			_sampleInfos.Add ( info );
			_totalUncompressedBytes += info.UncompressedBytesInMemory;
			_humanReadableUncompressedSize = null;
		}
		
		public void RemoveSample( GATSampleInfo sampleInfo )
		{
			_sampleInfos.Remove( sampleInfo );
			_totalUncompressedBytes -= sampleInfo.UncompressedBytesInMemory;
			_humanReadableUncompressedSize = null;
		}

#if UNITY_EDITOR
		public bool Contains( string sampleGUID )
		{
			foreach( GATSampleInfo info in _sampleInfos )
			{
				if( info.GUID == sampleGUID )
				{
					return true;
				}
			}
			
			return false;
		}
#endif
		
		public bool ContainsSampleNamed( string sampleName )
		{
			int i;

			for( i = 0; i < _sampleInfos.Count; i++ )
			{
				if( _sampleInfos[ i ].Name == sampleName )
					return true;
			}
			return false;
		}

		public GATSampleInfo GetSampleInfo( string sampleName )
		{
			int i;
			
			for( i = 0; i < _sampleInfos.Count; i++ )
			{
				if( _sampleInfos[ i ].Name == sampleName )
					return _sampleInfos[ i ];
			}
			return null;
		}
		
		public int SizeOfShortestSample()
		{
			int smallest = int.MaxValue;
			
			foreach( GATSampleInfo info in _sampleInfos )
			{
				if( info.SamplesPerChannel < smallest )
				{
					smallest = info.SamplesPerChannel;
				}
			}
			
			return smallest;
		}
		
		public int SizeOfLongestSample()
		{
			int largest = 0;
			
			foreach( GATSampleInfo info in _sampleInfos )
			{
				if( info.SamplesPerChannel > largest )
				{
					largest = info.SamplesPerChannel;
				}
			}
			return largest;
		}
		
		public Dictionary< string, GATData > LoadAll( GATDataAllocationMode allocationMode )
		{
			Dictionary< string, GATData > target = new Dictionary<string, GATData>( _sampleInfos.Count );
			int i;
			for( i = 0; i < _sampleInfos.Count; i++ )
			{
				LoadSample( _sampleInfos[ i ], target, allocationMode );
			}

			return target;
		}

		public Dictionary< string, GATData > LoadSamplesNamed( List< string > sampleNames, GATDataAllocationMode allocationMode )
		{
			int i;
			GATSampleInfo info;
			Dictionary< string, GATData > target = new Dictionary<string, GATData>( sampleNames.Count );

			for( i = 0; i < _sampleInfos.Count; i++ )
			{
				info = _sampleInfos[ i ];
				if( sampleNames.Contains( info.Name ) )
				{
					LoadSample( info, target, allocationMode );
				}
			}

			return target;
		}

		public string[] GetFullPathsInStreamingAssets( List< string > sampleNames )
		{
			int i;
			GATSampleInfo info;

			List< string > pathsList = new List<string>( sampleNames.Count );
			string path;

			for( i = 0; i < _sampleInfos.Count; i++ )
			{
				info = _sampleInfos[ i ];
				if( sampleNames.Contains( info.Name ) )
				{
					path = info.GetStreamingAssetFullPath();
					if( path == null )
					{
						#if UNITY_EDITOR
						Debug.LogError( info.FileName + " is not in a streaming asset." );
						#endif
						continue;
					}
					else
					{
						pathsList.Add( path );
					}
				}
			}

			string[] res = new string[ pathsList.Count ];
			pathsList.CopyTo( res );

			return res;
		}

		void LoadSample( GATSampleInfo info, Dictionary< string, GATData > target, GATDataAllocationMode allocationMode )
		{
			if( info.IsStreamingAsset )
			{
				#if UNITY_EDITOR
				if( Application.isPlaying == false )
				{
					LoadSampleFromResources( GATDataAllocationMode.Unmanaged, info, target );
					return;
				}
				#endif
				LoadSampleFromStreamingAssets( allocationMode, info, target );
			}
			else
			{
				LoadSampleFromResources( allocationMode, info, target );
			}
		}
		
		void LoadSampleFromResources( GATDataAllocationMode mode, GATSampleInfo info, Dictionary< string, GATData > loadedSamples )
		{
			AudioClip clip;
			#if UNITY_EDITOR
			if( Application.isPlaying ) //GZComment: simplifies memory management when trying things out in the editor, where performance is not crucial.
			{
				clip = Resources.Load( info.PathInResources ) as AudioClip;
				if( clip == null )
				{
					Debug.LogError( "No Clip at path: " + info.PathInResources);
				}
			}
			else
			{
				mode = GATDataAllocationMode.Unmanaged;
				string assetPath = AssetDatabase.GUIDToAssetPath( info.GUID );
				clip = AssetDatabase.LoadAssetAtPath( assetPath, typeof( AudioClip ) ) as AudioClip;
			}
			
			#else
			clip = Resources.Load( info.PathInResources ) as AudioClip;
			#endif
			
			
			if( info.NumChannels == 1 )
			{
				GATData data;
				data = clip.ToGATData( mode );
				loadedSamples.Add( info.Name, data );
			}
			else
			{
				GATData[] channelsData = clip.ExtractChannels( mode );
				for( int i = 0; i < info.NumChannels; i++ )
				{
					loadedSamples.Add( string.Format( "{0}_{1}", info.Name, i ), channelsData[i] );
				}
			}
			
			#if UNITY_EDITOR
			if( Application.isPlaying )
			{
				Resources.UnloadAsset( ( Object )clip );
			}
			#else
			Resources.UnloadAsset( clip );
			#endif
		}

		void LoadSampleFromStreamingAssets( GATDataAllocationMode mode, GATSampleInfo info, Dictionary< string, GATData > loadedSamples )
		{
			AGATAudioFile file;
			string path;
			GATData[] loadedChannels;
			int i;

			path = info.GetStreamingAssetFullPath();

			using( file = AGATAudioFile.OpenAudioFileAtPath( path ) )
			{
				loadedChannels = GATAudioLoader.SharedInstance.LoadSync( file, mode );
			}

			if( loadedChannels.Length == 1 )
			{
				loadedSamples.Add( info.Name, loadedChannels[ 0 ] );
				return;
			}

			for( i = 0; i < loadedChannels.Length; i++ )
			{
				loadedSamples.Add( string.Format( "{0}_{1}", info.Name, i ), loadedChannels[ i ] );
			}
		}
		
		#if UNITY_EDITOR
		
		public void GetMidiCodes( int offset, int fftSize = 4096, int maxFreq = 5000 )
		{
			AudioClip clip;
			float[] real, im, window;
			FloatFFT fft;
			string assetPath;
			float binWidth;
			List< FFTBinInfo > binInfos;
			int i;
			
			real = new float[ fftSize ];
			im   = new float[ fftSize ];
			window = new float[ fftSize ];
			
			GATMaths.MakeHammingWindow( window );
			
			fft = new FloatFFT();
			fft.init( ( uint )Mathf.Log ( fftSize, 2 ) );
			binWidth = ( float )_sampleRate / fftSize;
			
			int maxBin = ( int )( ( float )maxFreq / binWidth );
			
			foreach( GATSampleInfo info in _sampleInfos )
			{
				assetPath = AssetDatabase.GUIDToAssetPath( info.GUID );
				clip = AssetDatabase.LoadAssetAtPath( assetPath, typeof( AudioClip ) ) as AudioClip;
				if( clip.channels > 1 )
				{
					throw new GATException( "Get Midi Codes is a beta feature only available for mono clips" );
				}
				clip.GetData( real, offset );
				System.Array.Clear( im, 0, im.Length );
				fft.run( real, im );
				
				for( i = 0; i < maxBin; i++ )
				{
					real[ i ] = Mathf.Sqrt( real[ i ] * real[ i ] + im[ i ] * im[ i ] );
				}
				
				binInfos = FFTBinInfo.GetLowerMaxBins( real, 0, maxBin, binWidth, .2f );
				
				info.MidiCode = binInfos[ binInfos.Count - 1 ].GetMidiCode();
			}
			
			SortByMidiCode();
		}
		
		public void SortByMidiCode()
		{
			_sampleInfos = _sampleInfos.OrderBy( o => o.MidiCode ).ToList(); 
		}
		#endif
	}
}


