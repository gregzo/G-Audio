using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

namespace GAudio.Examples
{
	public class LarsenWarning : MonoBehaviour {
		
		void OnGUI()
		{
			if( GUI.Button( new Rect( Screen.width / 2 - 200, Screen.height / 2 - 100, 400f, 200f ), "Warning, Live Mic follows. Plug headphones!!!" ) )
			{
				StartCoroutine( Request () );
			}
		}
		
		IEnumerator Request()
		{
			yield return Application.RequestUserAuthorization( UserAuthorization.Microphone );
			if (Application.HasUserAuthorization( UserAuthorization.Microphone ) ) 
			{
                SceneManager.LoadScene( 1 );
			} 
			else
			{
				StartCoroutine( Request () );
			}
		}
	}
}

