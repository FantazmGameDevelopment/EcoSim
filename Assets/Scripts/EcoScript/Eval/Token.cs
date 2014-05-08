using UnityEngine;
using System.Collections;

namespace Ecosim.EcoScript.Eval {
	public class Token {
		public readonly string tokenStr;
		
		public Token (string tokenStr) {
			this.tokenStr = tokenStr;
		}
	}
	
	public class StringConstant : Token {
		public readonly string str;
		
		public StringConstant (string tokenStr, string str) : base (tokenStr)
		{
			this.str = str;
		}
	}
	
	public class DoubleConstant : Token {
		public readonly double doubleVal;
		
		public DoubleConstant (string tokenStr) : base (tokenStr)
		{
			doubleVal = double.Parse (tokenStr);
		}
	}
	
	public class LongConstant : DoubleConstant {
		public readonly long longVal;
		
		public LongConstant (string tokenStr) : base (tokenStr)
		{
			longVal = long.Parse (tokenStr);
		}
	}
	
	public class Id : Token {
		public readonly string id;
		
		public Id (string tokenStr) : base (tokenStr)
		{
			this.id = tokenStr;
		}
	}
	
	public class Symbol : Token {
		public readonly string symbol;
		
		public Symbol (string symbol) : base (symbol.ToString ())
		{
			this.symbol = symbol;
		}
	}
}
