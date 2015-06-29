using UnityEngine;
using System.Collections;
using UnityEditor;
using GAudio;

public class DataAllocatorConfigWindow : EditorWindow 
{
	//GATDataAllocator _allocator;

	int   _largestAllocatableChunk;
	float _largestRequestedChunk;

	int _binWidth;
	int _nbOfBins;
	int _totalSize;

	float _totalDuration;

	string _humanReadableTotalSize;
	

	void OnEnable()
	{
		this.title = "G-Audio Memory Configuration";
	}

	/*public void SetAllocator( GATDataAllocator allocator )
	{
		_allocator 				 = allocator;

		_largestAllocatableChunk = allocator.BinWidth * _allocator.NbOfBins;
		_binWidth 				 = allocator.BinWidth;
		_nbOfBins 			     = allocator.NbOfBins;
		_totalSize 				 = allocator.TotalSize;
		_totalDuration 			 = ( float )_totalSize / GATInfo.OutputSampleRate;
		_humanReadableTotalSize  = ( _totalSize * 4 ).HumanReadableBytes();
		_largestRequestedChunk   = ( float )_largestAllocatableChunk / GATInfo.OutputSampleRate;
	}*/

	public void SetAllocatorInfo( GATDataAllocator.InitializationSettings settings )
	{
		_largestAllocatableChunk = settings.binWidth * settings.nbOfBins;
		_binWidth 				 = settings.binWidth;
		_nbOfBins 			     = settings.nbOfBins;
		_totalSize 				 = ( int )( settings.preAllocatedAudioDuration * GATInfo.OutputSampleRate );
		_totalDuration 			 = settings.preAllocatedAudioDuration;
		_humanReadableTotalSize  = ( _totalSize * 4 ).HumanReadableBytes();
		_largestRequestedChunk   = ( float )_largestAllocatableChunk / GATInfo.OutputSampleRate;
	}

	static GUILayoutOption[] __labelOptions = new GUILayoutOption[]{ GUILayout.Width( 210f ) };

	void OnGUI()
	{
		EditorGUIUtility.fieldWidth = 40f;
		EditorGUIUtility.labelWidth = 210f;

		_largestRequestedChunk = EditorGUILayout.FloatField( "Largest requested chunk( seconds ) :", _largestRequestedChunk, GUILayout.ExpandWidth( false ) );

		GUILayout.BeginHorizontal();
		GUILayout.Label( "Bin Width( in samples ) :" + _binWidth.ToString( "N0" ), __labelOptions );
		_binWidth =  ( int )( 1024 * GUILayout.HorizontalSlider(  _binWidth / 1024, 1f, 30f, GUILayout.Width( 200f ), GUILayout.ExpandWidth( false ) ) );
		GUILayout.EndHorizontal();


		GUILayout.Label( "Number of bins: " + _nbOfBins );
		GUILayout.Label( string.Format( "Largest allocatable chunk: {0}s ( {1} samples )", ( ( float )_largestAllocatableChunk / GATInfo.OutputSampleRate ).ToString( "0.00" ), _largestAllocatableChunk.ToString( "N0" )  ) );

		EditorGUIUtility.fieldWidth = 40f;
		EditorGUIUtility.labelWidth = 210f;

		GUILayout.BeginHorizontal();
		_totalDuration =  EditorGUILayout.FloatField( "Total pre-allocated size( in seconds ) :", _totalDuration, GUILayout.ExpandWidth( false )  );
		GUILayout.Label( string.Format("( {0} )", _humanReadableTotalSize ) );
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.color = Color.red;
		if( GUILayout.Button( "Cancel", GUILayout.Width( 70f ) ) )
	   	{
			this.Close();
		}

		if( Application.isPlaying )
		{
			GUI.enabled = false;
		}
	
		GUI.color = Color.green;
		if( GUILayout.Button( "Apply", GUILayout.Width( 70f ) ) )
	   	{
			if( EditorUtility.DisplayDialog( "Allocator Info", "Since 1.27, the allocator is active in play mode only. Your settings have been saved and will be applied the next time you enter play mode.", "OK", "Cancel" ) )
			{
				GATDataAllocator.InitializationSettings settings = GATManager.UniqueInstance.AllocatorInitSettings;

				settings.preAllocatedAudioDuration 	= _totalDuration;
				settings.nbOfBins 				   	= _nbOfBins;
				settings.binWidth					= _binWidth;
				settings.maxConcurrentSamples		= 50;

				this.Close();

				//UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
			}
		}

		GUILayout.EndHorizontal();

		if( GUI.changed )
		{
			Refresh();
			Repaint();
		}
	}

	void Refresh()
	{
		int requestedChunk = ( int )( _largestRequestedChunk * GATInfo.OutputSampleRate );

		_nbOfBins 				 = requestedChunk / _binWidth + 1;
		_largestAllocatableChunk = _binWidth * _nbOfBins;
		_totalSize 				 = ( int )( _totalDuration * GATInfo.OutputSampleRate );
		_humanReadableTotalSize  = ( _totalSize * 4 ).HumanReadableBytes();
	}
}
