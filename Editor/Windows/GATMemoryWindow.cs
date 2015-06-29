using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using GAudio;

public class GATMemoryWindow : EditorWindow 
{
	const int TEXTURE_HEIGHT = 10;

	Color FREE_COLOR 		= Color.green;
	Color USED_COLOR 		= Color.yellow;
	Color FIXED_COLOR 		= Color.red;
	Color SEPERATOR_COLOR 	= Color.black;

	Texture2D 		 _memTexture;
	GATDataAllocator _allocator;

	List< GATMemDebugInfo > 	 	_memInfo;
	List< GATFixedMemDebugInfo >	_fixedMemInfo;

	Vector3 _maxAllocStart = new Vector3( 10f, 30f );
	Vector3 _maxAllocStop;

	Color   _maxAllocColor = new Color( 0f, .4f, 0f ); 
	string  _maxAllocString;
	Rect    _maxAllocRect;

	Color   _unfragmentedColor = new Color( 0f, .6f, 0f );

	int _nbOfAllocatedChunks,
		_totalAllocatedSize,
		_nbOfFragmentedChunks,
		_totalFragmentedSize,
		_totalUnusedAllocated;

	void OnEnable()
	{
		this.minSize = new Vector2( 300f, 160f );
		this.maxSize = new Vector2( 20000f, 160f );

		if( GATManager.UniqueInstance != null )
		{
			RefreshMemInfo();
		}

		this.title = "Memory Status";
	}

	void OnDisable()
	{
		if( _memTexture != null )
		{
			DestroyImmediate( _memTexture );
			_memTexture = null;
		}
	}

	void OnGUI()
	{
		if( GATManager.DefaultDataAllocator == null )
		{
			EditorGUILayout.HelpBox( "No allocator found!", MessageType.Error );
			return;
		}

		//********************************************************************************
		//************** Draw Memory Fragmentation Representation ************************
		if( _memTexture != null )
		{
			GUI.DrawTexture( new Rect( 10, 10, ( float )_memTexture.width, ( float )_memTexture.height ), _memTexture );
		}

		//*****************************************************************
		//************** Draw Largest Chunk Width *************************
		Handles.color = _maxAllocColor;
		Handles.DrawLine( _maxAllocStart, _maxAllocStop );

		GUI.skin.label.normal.textColor = _maxAllocColor;
		GUI.Label( _maxAllocRect, _maxAllocString );

		GUILayout.Space( ( float )TEXTURE_HEIGHT + 30f );

		//*****************************************************************
		//************** Info Labels **************************************
		GUI.skin.label.normal.textColor = USED_COLOR;
		GUILayout.Label( string.Format( "Allocated chunks : {0}, totalling {1} samples of which {2} are unused.", _nbOfAllocatedChunks.ToString(), _totalAllocatedSize.ToString( "N0" ), _totalUnusedAllocated.ToString( "N0" ) ) );

		GUI.skin.label.normal.textColor = _unfragmentedColor;
		GUILayout.Label( string.Format( "Fragmented free chunks : {0}, totalling {1} samples.", _nbOfFragmentedChunks.ToString(), _totalFragmentedSize.ToString( "N0" ) ) );

		GUI.skin.label.normal.textColor = _unfragmentedColor;
		GUILayout.Label( string.Format( "Unfragmented : {0} samples", GATManager.DefaultDataAllocator.UnfragmentedSize.ToString( "N0" ) ) );

		GUI.skin.label.normal.textColor = FIXED_COLOR;
		GUILayout.Label( string.Format( "FixedAllocations : {0} samples", GATManager.DefaultDataAllocator.FixedAllocationsSize.ToString( "N0" ) ) );



		//*****************************************************************
		//************** Resfresh and Defrag Buttons **********************
		GUI.skin.label.normal.textColor = Color.black;

		GUILayout.BeginHorizontal();

		if( GUILayout.Button( "Refresh", GUILayout.Width( 70f ) ) )
		{
			RefreshMemInfo();
		}

		if( GUILayout.Button( "Defrag",  GUILayout.Width( 70f ) ) )
		{
			GATManager.DefaultDataAllocator.Defragment();
			RefreshMemInfo();
		}

		GUILayout.EndHorizontal();
	}

	void RefreshMemInfo()
	{
		int			binWidth;
		List<Color> colors;
		int			i;
		int 		segments;
		Color 		color;
		bool 		free;

		GATMemDebugInfo info;

		_allocator = GATManager.DefaultDataAllocator;

		if( _allocator == null )
			return;

		binWidth 		= _allocator.BinWidth;
		_memInfo 		= _allocator.GetDebugInfo();
		_fixedMemInfo 	= _allocator.GetFixedDebugInfo();
		colors 			= new List< Color >( 1000 );

		_nbOfFragmentedChunks = 0;
		_totalFragmentedSize  = 0;
		_nbOfAllocatedChunks  = 0;
		_totalAllocatedSize   = 0;
		_totalUnusedAllocated = 0;
	


		for( i = 0; i < _memInfo.Count; i++ )
		{
			info 	 = _memInfo[ i ];

			segments = info.MaxSize / binWidth;
			free  	 = info.AllocatedSize == 0;
			color 	 = free ? FREE_COLOR : USED_COLOR;

			if( free )
			{
				_nbOfFragmentedChunks++;
				_totalFragmentedSize += info.MaxSize;
			}
			else
			{
				_nbOfAllocatedChunks++;
				_totalAllocatedSize 	+= info.MaxSize;
				_totalUnusedAllocated 	+= info.MaxSize - info.AllocatedSize;
			}

			for( int j = 0; j < segments; j++ )
			{
				colors.Add ( color );
			}

			colors.Add ( SEPERATOR_COLOR );
		}

		segments = _allocator.UnfragmentedSize / binWidth;

		for( i = 0; i < segments; i++ )
		{
			colors.Add ( FREE_COLOR );
		}

		int totalFixed = 0;
		foreach( GATFixedMemDebugInfo fInfo in _fixedMemInfo )
		{
			totalFixed += fInfo.AllocatedSize;
		}

		segments = totalFixed / binWidth;

		if( segments == 0 && totalFixed > 0 )
		{
			segments = 2;
		}

		for( i = 0; i < segments; i++ )
		{
			colors.Add ( FIXED_COLOR );
		}

		if( _memTexture != null )
		{
			DestroyImmediate( _memTexture );
		}

		_memTexture = new Texture2D( colors.Count, TEXTURE_HEIGHT, TextureFormat.RGB24, false );

		Color[] colorArray = colors.ToArray();

		for( i = 0; i < TEXTURE_HEIGHT; i++ )
		{
			_memTexture.SetPixels( 0, i, colorArray.Length, 1, colorArray );
		}

		_memTexture.Apply( false, false );

		_maxAllocStop   = new Vector3( _maxAllocStart.x + _allocator.NbOfBins, _maxAllocStart.y );
		_maxAllocString = string.Format( "Largest Allocatable Chunk( {0} samples )", ( _allocator.BinWidth * _allocator.NbOfBins ).ToString( "N0")  );
		_maxAllocRect   = new Rect( _maxAllocStop.x + 5f, _maxAllocStop.y - 8f, 300f, 20f );
	}
}
