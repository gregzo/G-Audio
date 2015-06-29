//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;

namespace GAudio
{
	public class GATAudioBuffer : MonoBehaviour {
		
		public static IntPtr AudioBufferPointer{ get{ return __bufferPointer; } }
		public static GATData AudioBuffer{ get; protected set; }
		
		static GCHandle __bufferHandle;
		static IntPtr   __bufferPointer;
		
		static bool     __wasAdded,
		__didInitialize;
		
		void Awake()
		{
			#if UNITY_WEBPLAYER
			Destroy ( this );
			return;
			#else
			if( __wasAdded )
			{
				Debug.LogWarning( "GATAudioBuffer needs to be added to one GATPlayer only." );
				Destroy( this );
				return;
			}
			
			__wasAdded = true;
			
			#endif
		}
		
		#if !UNITY_WEBPLAYER
		void OnAudioFilterRead( float[] data, int nbOfChannels )
		{
			if( __didInitialize == false )
			{
				__bufferHandle  = GCHandle.Alloc( data, GCHandleType.Pinned );
				__bufferPointer = __bufferHandle.AddrOfPinnedObject();
				AudioBuffer 	= new GATData( data );
				__didInitialize = true;
			}
		}
		#endif
		void Update()
		{
			if( __didInitialize )
			{
				Destroy( this );
			}
		}
		
		public static void CleanUpStatics()
		{
			if( __didInitialize == false )
			{
				return;
			}
			
			__bufferHandle.Free();
			
			__bufferPointer = IntPtr.Zero;
			__didInitialize = false;
			__wasAdded		= false;
			
			AudioBuffer  	= null;
		}
		
		public static bool ShouldBeAdded{ get{ return !__wasAdded; } }
	}
}

