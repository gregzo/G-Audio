//--------------------------------
//   		G-Audio
// Copyright © 2014 Gregorio Zanon
//--------------------------------
using System;

[ AttributeUsage( AttributeTargets.Property, Inherited = true ) ]
public class FloatPropertyRange : System.Attribute
{
	public float Min{ get; protected set; }
	public float Max{ get; protected set; }
	
	public FloatPropertyRange( float min, float max )
	{
		Min = min;
		Max = max;
	}
}

[ AttributeUsage( AttributeTargets.Property, Inherited = true ) ]
public class IntPropertyRange : System.Attribute
{
	public int Min{ get; protected set; }
	public int Max{ get; protected set; }
	
	public IntPropertyRange( int min, int max )
	{
		Min = min;
		Max = max;
	}
}

[ AttributeUsage( AttributeTargets.Property, Inherited = true ) ]
public class ToggleGroupProperty : System.Attribute
{
	public int NbOfProperties{ get; protected set; }
	
	public ToggleGroupProperty( int nbOfProperties )
	{
		NbOfProperties = nbOfProperties;
	}
}
