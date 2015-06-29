using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	public class CopyrightGUI : MonoBehaviour {
		
		static Rect __area = new Rect( 10f, 10f, 250f, 150f );
		void OnGUI()
		{
			GUILayout.BeginArea( __area );
			GUILayout.Label( "Copyright © 2014 Gregorio Zanon" );
			GUILayout.Label( "www.G-Audio-Unity.com" );
			
			GUILayout.Space( 10f );
			
			GUILayout.EndArea();
		}
	}
}

