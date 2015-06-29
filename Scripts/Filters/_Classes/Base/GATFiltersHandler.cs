//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using UnityEngine;
using System.Collections.Generic;

namespace GAudio
{
	[ System.Serializable ]
	/// <summary>
	/// A helper class to which audio streams may delegate 
	/// filters handling. Both GATTrack and GATPlayer make use of GATFiltersHandler.
	/// </summary>
	public class GATFiltersHandler : ScriptableObject
	{
		/// <summary>
		/// Gets a value indicating whether this instance has filters.
		/// </summary>
		public bool HasFilters{ get{ return _filters.Count != 0; } }
		[ SerializeField ]
		protected List< AGATMonoFilter >  _filters;
		
		/// <summary>
		/// The number of interleaved channels this instance is setup to apply filters to.
		/// </summary>
		public int NbOfFilteredChannels{ get{ return _nbOfChannelsToFilter; } }
		[ SerializeField ]
		protected int _nbOfChannelsToFilter;
		
		protected object _lock = new object();
		
		/// <summary>
		/// Initializes the filters handler.
		/// </summary>
		public void InitFiltersHandler( int nbOfChannelsToFilter )
		{
			_nbOfChannelsToFilter = nbOfChannelsToFilter;
			_filters = new List< AGATMonoFilter >( 4 );
		}
		
		/// <summary>
		/// Applies all filters.
		/// </summary>
		public bool ApplyFilters( float[] data, int offset, int length, bool emptyData )
		{
			int i;
			bool didApply = false;
			
			lock( _lock )
			{
				
				for( i = 0; i < _filters.Count; i++ )
				{
					if( _filters[ i ].Bypass == false )
					{
						if( _filters[i].ProcessChunk( data, offset, length, emptyData ) )
						{
							didApply = true;
						}
					}
				}
			}
			
			return didApply;
		}
		
		/// <summary>
		/// Adds a filter to the handlers list.
		/// Will not do anything if the specified slot already contains a filter.
		/// The return instance type is always AGATMonoFilter - cast as required.
		/// </summary>
		public AGATMonoFilter AddFilter< T >( int slotIndex ) where T : AGATMonoFilter														  				 
		{
			T filter;
			AGATMonoFilter multiChannelFilter;
			
			int insertIndex;
			int i;
			
			if( _filters.Count == 0 )
			{
				insertIndex = 0;
			}
			else
			{
				insertIndex = _filters.Count;
				
				for( i = 0; i < _filters.Count; i++ )
				{
					if( _filters[i].SlotIndex == slotIndex )
					{
						#if UNITY_EDITOR
						Debug.LogWarning("There is already a filter at slot index "+i );
						#endif
						return null;
					}
					
					if( _filters[i].SlotIndex > slotIndex )
					{
						insertIndex = i;
						break;
					}
				}
			}
			
			filter = ScriptableObject.CreateInstance< T >();
			
			if( filter == null )
			{
				Debug.LogWarning("Failed to instantiate" );
			}
			filter.InitFilter( slotIndex );
			
			if( filter.NbOfFilterableChannels == 1 && _nbOfChannelsToFilter > 1 )
			{
				multiChannelFilter = filter.GetMultiChannelWrapper< T >( _nbOfChannelsToFilter );
				multiChannelFilter.InitFilter( slotIndex );
				
				lock( _lock )
				{
					_filters.Insert( insertIndex, multiChannelFilter );
				}
				
				return multiChannelFilter;
			}
			
			lock( _lock )
			{
				_filters.Insert( insertIndex, filter );
			}
			
			return filter;
		}
		
		/// <summary>
		/// Removes the filter at specified slot, or does nothing if the slot is already empty.
		/// </summary>
		public void RemoveFilterAtSlot( int slotIndex )
		{
			int i;
			AGATMonoFilter filter = null;
			
			if( _filters.Count == 0 )
			{
				return;
			}
			
			for( i = 0; i < _filters.Count; i++ )
			{
				if( _filters[ i ].SlotIndex == slotIndex )
				{
					filter = _filters[i];
					break;
				}
			}
			
			if( filter == null )
			{
				return;
			}
			
			lock( _lock )
			{
				_filters.Remove( filter );
			}
			
			if( Application.isPlaying )
			{
				Destroy( filter );
			}
			else
			{
				DestroyImmediate( filter );
			}
		}
		
		/// <summary>
		/// Returns the AGATMonoFilter instance at slot slotIndex.
		/// May return null.
		/// </summary>
		public AGATMonoFilter GetFilterAtSlot( int slotIndex )
		{
			if( _filters.Count == 0 )
			{
				return null;
			}
			
			int i;
			
			for( i = 0; i < _filters.Count; i++ )
			{
				if( _filters[ i ].SlotIndex == slotIndex )
				{
					return _filters[i];
				}
			}
			
			return null;
		}
		
		void OnDestroy()
		{
			int i;
			
			if( Application.isPlaying )
			{
				lock( _lock )
				{
					for( i = 0; i < _filters.Count; i++ )
					{
						Destroy ( _filters[i] );
					}
					
					_filters.Clear();
				}
			}
			else
			{
				lock( _lock )
				{
					for( i = 0; i < _filters.Count; i++ )
					{
						DestroyImmediate ( _filters[i] );
					}
					
					_filters.Clear();
				}
			}
		}
		
		#region EDITOR_MEMBERS
		#if UNITY_EDITOR
		[ SerializeField ]
		protected int _selectedFilterSlot;
		public int SelectedFilterSlot
		{ 
			get{ return _selectedFilterSlot; } 
			set
			{ 
				if( value == _selectedFilterSlot )
					return;
				_selectedFilterSlot = value; 
			} 
		}
		#endif
		#endregion
	}
}

