using UnityEngine;
using System.Collections;

namespace NVorbis
{
	public class InvalidDataException : System.Exception {
		
		public InvalidDataException( string message ) : base( message )
		{}
		
		public InvalidDataException() : base()
		{}


	}
}

