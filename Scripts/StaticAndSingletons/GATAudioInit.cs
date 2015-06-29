//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	/// <summary>
	/// Inspector friendly Monobehaviour component
	/// for requesting a change to the specified 
	/// platform's output sample rate and frame rate.
	/// </summary>
	public class GATAudioInit : MonoBehaviour {
		
		public PlatformSettings[] platformSettings;
		
		public int levelToLoad = 1;
		
		public bool requestMic;
		
		static GATAudioInit __uniqueInstance;
		
		void Awake()
		{	
			if( __uniqueInstance != null )
			{
				Debug.LogError( "Only one GATAudioInit should exist!" );
				Destroy( this );
				return;
			}
			
			__uniqueInstance = this;
			
			RuntimePlatform currentPlatform = Application.platform;
			PlatformSettings currentSettings = null;
			
			foreach( PlatformSettings settings in platformSettings )
			{
				if( settings.platform == currentPlatform )
				{
					currentSettings = settings;
					break;
				}
			}
			
			if( currentSettings == null )
			{
				#if GAT_DEBUG
				Debug.LogWarning( "GATAudioInit not configured for current platform: "+currentPlatform.ToString() );
				#endif
				return;
			}


			if( AudioSettings.outputSampleRate != currentSettings.sampleRate )
			{
				#if UNITY_5
				Debug.LogWarning( "GATAudioInit's sample rate setting is obsolete in Unity 5. Target platform samplerate can be configured in project settings." );
				#else
				AudioSettings.outputSampleRate = currentSettings.sampleRate;
				GATInfo.SetRequestedSampleRate( currentSettings.sampleRate );
				#endif
			}
			
			Application.targetFrameRate = currentSettings.targetFrameRate;
		}
		
		IEnumerator Start()
		{
			if( requestMic )
			{
				yield return Application.RequestUserAuthorization( UserAuthorization.Microphone );
				if (Application.HasUserAuthorization( UserAuthorization.Microphone ) ) 
				{
					Application.LoadLevel( levelToLoad );
				} 
				else 
				{
					
				}
			}
			else
			{
				Application.LoadLevel( levelToLoad );
			}
		}
		
		[ System.Serializable ]
		public class PlatformSettings
		{
			public RuntimePlatform platform;
			public int sampleRate;
			public int targetFrameRate;
		}
	}
}

