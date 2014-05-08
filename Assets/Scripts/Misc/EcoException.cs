using UnityEngine;
using System.Collections;

namespace Ecosim
{
	public class EcoException : System.Exception
	{
		public EcoException (string msg) : base(msg) {
			Log.LogError(msg);
		}
	}

}