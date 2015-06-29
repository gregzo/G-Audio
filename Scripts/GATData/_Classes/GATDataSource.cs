using UnityEngine;
using System.Collections;
using System;

namespace GAudio
{
	public class GATDataSource : IDisposable
	{
		GATData _data;
		
		double _nextIndex;
		
		bool _disposed;
		
		
		public GATDataSource( GATData data )
		{
			SetData( data );
		}
		
		public void Seek( int samplePos )
		{
			_nextIndex = ( double )samplePos;
			if( samplePos >= _data.Count )
				_nextIndex = 0d;
		}
		
		public void SetData( GATData data )
		{
			_nextIndex = 0d;
			if( _data != null )
			{
				_data.Release();
			}
			_data = data;
			
			if( data != null )
			{
				data.Retain();
			}
		}
		
		public int NextIndex{ get{ return ( int )_nextIndex; } }
		
		public int GetResampledData( GATData target, int targetLength, int offsetInTarget, double pitch )
		{
			double dLastIndex = _nextIndex + pitch * ( targetLength - 1 );
			int iLastIndex = ( int )dLastIndex;;
			int sign = System.Math.Sign( pitch );
			
			if( dLastIndex - ( double )iLastIndex > 0d )
			{
				iLastIndex += sign;
			}
			
			if( iLastIndex >= _data.Count - 1 )
			{
				targetLength = ( int )( ( ( double )( _data.Count - 1 ) - _nextIndex ) / pitch );
				target.ResampleCopyFrom( _data.ParentArray, _nextIndex + ( double )_data.MemOffset, targetLength, offsetInTarget, pitch );
				
				return targetLength;
			}
			else if( iLastIndex < 0 )
			{
				targetLength = -( int )( ( _nextIndex - 1d ) / pitch ) + 1; 
				
				target.ResampleCopyFrom( _data.ParentArray, _nextIndex + ( double )_data.MemOffset, targetLength, offsetInTarget, pitch );
				
				return targetLength;
			}
			else
			{
				target.ResampleCopyFrom( _data.ParentArray, _nextIndex + ( double )_data.MemOffset, targetLength, offsetInTarget, pitch );
				_nextIndex = dLastIndex + pitch;
				return targetLength;
			}
		}

		public int GetData( GATData target, int targetLength, int offsetInTarget, bool reverse = false )
		{
			int nextIndex = ( int )_nextIndex;
			int sign = reverse ? -1 : 1;
			int lastIndex = nextIndex + targetLength * sign;

			if( lastIndex >= _data.Count )
			{
				targetLength = _data.Count - nextIndex;
			}
			else if( lastIndex < 0 )
			{
				targetLength = nextIndex + 1;
				lastIndex = 0;
			}

			if( reverse == false )
			{
				target.CopyFrom( _data.ParentArray, offsetInTarget, nextIndex + _data.MemOffset, targetLength );
			}
			else
			{
				target.CopyFrom( _data.ParentArray, offsetInTarget, lastIndex + _data.MemOffset, targetLength );
				target.Reverse( offsetInTarget, targetLength );
			}

			_nextIndex = ( int )lastIndex;

			return targetLength;
		}

		#region IDisposable Implementation
		
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		protected virtual void Dispose( bool explicitly )
		{
			if( _disposed )
				return;
			
			if( _data != null )
				_data.Release();
			
			_disposed = true;
		}
		
		~GATDataSource()
		{
			Dispose( false );
		}

		#endregion
	}
}

