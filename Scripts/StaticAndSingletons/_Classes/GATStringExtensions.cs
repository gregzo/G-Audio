//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	public static class GATStringExtensions 
	{
		public static string HumanReadableBytes( this int numBytes )
		{
			double readable = numBytes;
			string suffix;
			
			if( numBytes >= 0x100000 ) // Megabyte
			{
				suffix = "MB";
				readable = numBytes >> 10;
			}
			else if( numBytes >= 0x400 ) // Kilobyte
			{
				suffix = "KB";
				readable = numBytes;
			}
			else
			{
				return numBytes.ToString( "0 B" ); // Byte
			}
			readable /= 1024;
			
			return readable.ToString( "0.## " ) + suffix;
		}
		
	}

}
