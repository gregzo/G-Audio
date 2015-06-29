//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections;

namespace GAudio
{
	public interface IRetainable
	{
		int 	RetainCount{ get; }
		void 	Retain();
		void 	Release();
	}
	
	public abstract class RetainableObject : IRetainable
	{
		public 		int RetainCount{ get{ return _retainCount; } }
		protected 	int _retainCount;
		
		public void Retain()
		{
			_retainCount++;
		}
		
		public void Release()
		{
			_retainCount--;
			if( _retainCount < 1 )
			{
				Discard();
			}
		}
		
		protected abstract void Discard();
	}
}

