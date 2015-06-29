//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------

using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Simple class which holds information on virtually allocated memory 
	/// </summary>
	public class GATMemDebugInfo  
	{
		/// <summary>
		/// The index of the chunk
		/// </summary>
		public readonly int ChunkNb;
		/// <summary>
		/// The requested allocated size
		/// </summary>
		public readonly int AllocatedSize;
		/// <summary>
		/// The real size of the chunk
		/// </summary>
		public readonly int MaxSize;
		
		public GATMemDebugInfo( int nb, int allocated, int max )
		{
			ChunkNb 		= nb;
			AllocatedSize 	= allocated;
			MaxSize 		= max;
		}
		
		/// <summary>
		/// returns a description of the chunk's position and size
		/// </summary>
		public string Description()
		{
			string description = string.Format( "Chunk {0}, allocated: {1}, max: {2}", ChunkNb, AllocatedSize, MaxSize );
			return description;
		}
	}
	
	/// <summary>
	/// Simple class which holds information on virtually allocated fixed chunks 
	/// </summary>
	public class GATFixedMemDebugInfo  
	{
		/// <summary>
		/// The index of the chunk
		/// </summary>
		public readonly int ChunkNb;
		/// <summary>
		/// The size of the chunk.
		/// </summary>
		public readonly int AllocatedSize;
		
		/// <summary>
		/// The description for the chunk - provided upon allocation
		/// </summary>
		public readonly string Description;
		
		public GATFixedMemDebugInfo( int nb, int allocated, string description )
		{
			ChunkNb 		= nb;
			AllocatedSize 	= allocated;
			Description     = description;
		}
	}
}
