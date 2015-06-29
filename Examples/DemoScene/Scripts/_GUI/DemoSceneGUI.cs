using UnityEngine;
using System.Collections;

namespace GAudio.Examples
{
	public class DemoSceneGUI : MonoBehaviour {
		
		bool _didShowIntroText;
		
		static Rect __guiRect;
		
		static string __text = " G-Audio\n\n\"Break Me\" Demo\n\n" +
			"This demo showcases G-Audio’s low-level control over pure audio data through realtime procedural sampling and filtering of two short piano sounds.\n\n" +
				"Everything you hear - including the bass and the granular synthesis drone - shares the same source audio material." +
				"\n\nAs you interact with the demo, tempo will increase until breaking point, but not before reaching unbelievably high speeds." +
				"\n\nHeadphones recommended.";
		
		static string __help = 	"Interactions\n\n" +
			"1) Both spheres may be clicked.\n\n" +
				"2) Dragging the platform will adjust the offset in the source sample for the melodic pattern.\n\n" +
				"3) Clicking the button will double the bpm for 6 beats, as well as randomly changing the harmony and increasing tempo.\n\n" +
				"4) The pitch of the sound coupled with the particle effect on the right can be controlled by moving the mouse up and down.";
		
		string _buttonText = "Next";
		
		void Awake()
		{
			__guiRect = new Rect( Screen.width - 220f, 20f, 200f, 1000f );
		}
		
		void OnGUI()
		{
			GUILayout.BeginArea( __guiRect );
			
			GUILayout.Label( _didShowIntroText ? __help : __text );
			GUILayout.Space( 5f );
			if( GUILayout.Button(_buttonText ) )
			{
				if( _didShowIntroText )
				{
					Destroy( this );
				}
				else
				{
					_didShowIntroText = true;
					_buttonText = "OK";
				}
			}
			
			GUILayout.EndArea();
		}
	}
}

