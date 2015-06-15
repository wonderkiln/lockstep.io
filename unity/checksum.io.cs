using UnityEngine;
using System.Collections;
using System;

public class Checksum 
{
	public static UInt64 Hash(string str)
	{
		UInt64 val = 3074457345618258791ul;
		for(int i = 0; i < str.Length; i++)
		{
			val += str[i];
			val *= 3074457345618258799ul;
		}
		return val;
	}
}