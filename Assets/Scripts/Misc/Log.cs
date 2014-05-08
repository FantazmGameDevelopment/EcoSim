using UnityEngine;
using System.Collections;
using System;

namespace Ecosim
{
	public static class Log
	{
	
		public static void ClearLog ()
		{
		}
	
		public static void LogError (string msg)
		{
			UnityEngine.Debug.LogError (msg);
		}		

		public static void LogWarning (string msg)
		{
			UnityEngine.Debug.LogWarning (msg);
		}		

		public static void LogDebug (string msg)
		{
			UnityEngine.Debug.Log (msg);
		}		

		public static void LogException (Exception e)
		{
			UnityEngine.Debug.LogException (e);
		}		
		
	}
}