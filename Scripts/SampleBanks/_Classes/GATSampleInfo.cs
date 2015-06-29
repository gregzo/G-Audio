using UnityEngine;
using System.Collections;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GAudio
{
	[ System.Serializable ]
	public class GATSampleInfo
	{
		public string Name
		{ 
			get{ return _name; } 
			set
			{ 
				if( _name == value )
					return;
				
				_name = value; 
			} 
		}
		[ SerializeField ]
		string _name;
		
		public int MidiCode
		{ 
			get{ return _midiCode; } 
			set
			{ 
				if( _midiCode == value )
					return;
				
				_midiCode = value; 
			} 
		}
		[ SerializeField ]
		int _midiCode;
		
		public string PathInResources{ get{ return _pathInResources; } }
		[ SerializeField ]
		string _pathInResources;
		
		public int NumChannels{ get{ return _numChannels; } }
		[ SerializeField ]
		int _numChannels;
		
		public int SamplesPerChannel{ get{ return _samplesPerChannel; } }
		[ SerializeField ]
		int _samplesPerChannel;
		
		public string GUID{ get{ return _guid; } }
		[ SerializeField ]
		string _guid;

		public bool IsStreamingAsset{ get{ return _isStreamingAsset; } }
		[ SerializeField ]
		bool _isStreamingAsset;
		
		public int UncompressedBytesInMemory
		{
			get
			{
				return _numChannels * _samplesPerChannel * 4;
			}
		}

		public string GetStreamingAssetFullPath()
		{
			if( _isStreamingAsset == false )
				return null;

			return System.IO.Path.Combine( Application.streamingAssetsPath, _pathInResources );
		}
		
		#if UNITY_EDITOR
		public string FileName
		{ 
			get
			{ 
				if( _fileName == null )
				{
					_fileName = Path.GetFileName( AssetDatabase.GUIDToAssetPath( _guid ) );
				}
				return _fileName; 
			}
		}
		string _fileName;
		
		public string HumanReadableSizeInMemory
		{
			get
			{
				if( _humanReadableSize == null )
				{
					_humanReadableSize = this.UncompressedBytesInMemory.HumanReadableBytes();
				}
				
				return _humanReadableSize;
			}
		}
		string _humanReadableSize;


		public SoundBankClipStatus clipStatus = SoundBankClipStatus.Ok;
		
		
		public void UpdatePathInResources()
		{
			string path = AssetDatabase.GUIDToAssetPath( this.GUID );
			if( GATEditorUtilities.IsInResources( path ) )
			{
				_pathInResources = GATEditorUtilities.PathInResources( path );
				_isStreamingAsset = false;
			}
			else if( GATEditorUtilities.IsInStreamingAssets( path ) )
			{
				_pathInResources = GATEditorUtilities.PathInStreamingAssets( path );
				_isStreamingAsset = true;
			}
			else
			{
				clipStatus = SoundBankClipStatus.NotInResources;
			}

			clipStatus = SoundBankClipStatus.Ok;
		}
		#endif
	

		public GATSampleInfo( string path, string guid, int numChannels, int samplesPerChannel, bool isStreamingAsset = false )
		{
			_isStreamingAsset	= isStreamingAsset;
			_pathInResources 	= path;
			_guid			 	= guid;
			_numChannels	 	= numChannels;
			_samplesPerChannel 	= samplesPerChannel;

			if( !isStreamingAsset )
			{
				_name = Path.GetFileName( _pathInResources );
			}
			else
			{
				_name = Path.GetFileNameWithoutExtension( _pathInResources );
			}
		}
	}
}

