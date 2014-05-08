using System;
using System.Text;
using System.Collections.Generic;

namespace Ecosim.EcoScript.Eval
{
	/**
	 * Simple C-like expression parser.
	 */
	public class Expression
	{
		public Expression ()
		{
			fnHandlers = new List<ProcessFnHandler> ();
			varHandlers = new List<ProcessVarHandler> ();
		}
		
		public void AddFnHander (ProcessFnHandler fnHandler)
		{
			fnHandlers.Add (fnHandler);
		}
		
		public void AddVarHandler (ProcessVarHandler varHandler)
		{
			varHandlers.Add (varHandler);
		}
		
		public delegate object ProcessVarHandler (string id);

		public delegate object ProcessFnHandler (string id,object[] args);
		
		private List<ProcessFnHandler> fnHandlers;
		private List<ProcessVarHandler> varHandlers;
				
		private object DoEqualityOp (object lhs, object rhs, string op, int index)
		{
			if ((lhs is string) && (rhs is string)) {
				if (op == "==")
					return ((string)lhs) == ((string)rhs);
				if (op == "!=")
					return ((string)lhs) != ((string)rhs);
			}
			if ((lhs is bool) && (rhs is bool)) {
				if (op == "==")
					return ((bool)lhs) == ((bool)rhs);
				if (op == "!=")
					return ((bool)lhs) != ((bool)rhs);
			}
			if ((lhs is long) && (rhs is long)) {
				if (op == "==")
					return ((long)lhs) == ((long)rhs);
				if (op == "!=")
					return ((long)lhs) != ((long)rhs);
				if (op == "<=")
					return ((long)lhs) <= ((long)rhs);
				if (op == ">=")
					return ((long)lhs) >= ((long)rhs);
				if (op == "<")
					return ((long)lhs) < ((long)rhs);
				if (op == ">")
					return ((long)lhs) > ((long)rhs);
			}
			if ((lhs is double) && (rhs is double)) {
				if (op == "==")
					return ((double)lhs) == ((double)rhs);
				if (op == "!=")
					return ((double)lhs) != ((double)rhs);
				if (op == "<=")
					return ((double)lhs) <= ((double)rhs);
				if (op == ">=")
					return ((double)lhs) >= ((double)rhs);
				if (op == "<")
					return ((double)lhs) < ((double)rhs);
				if (op == ">")
					return ((double)lhs) > ((double)rhs);
			}
			throw new EvalException ("failed to execute '" + op + "' at " + (index + 1));
		}

		private object DoAdditiveOp (object lhs, object rhs, string op, int index)
		{
			if ((lhs is string) && (rhs is string)) {
				if (op == "+")
					return ((string)lhs) + ((string)rhs);
			}
			if ((lhs is long) && (rhs is long)) {
				if (op == "+")
					return ((long)lhs) + ((long)rhs);
				if (op == "-")
					return ((long)lhs) - ((long)rhs);
			}
			if ((lhs is double) && (rhs is double)) {
				if (op == "+")
					return ((double)lhs) + ((double)rhs);
				if (op == "-")
					return ((double)lhs) - ((double)rhs);
			}
			throw new EvalException ("failed to execute '" + op + "' at " + (index + 1));
		}

		private object DoMultiplicativeOp (object lhs, object rhs, string op, int index)
		{
			if ((lhs is long) && (rhs is long)) {
				if (op == "*")
					return ((long)lhs) * ((long)rhs);
				if (op == "/")
					return ((long)lhs) / ((long)rhs);
				if (op == "%")
					return ((long)lhs) % ((long)rhs);
			}
			if ((lhs is double) && (rhs is double)) {
				if (op == "*")
					return ((double)lhs) * ((double)rhs);
				if (op == "/")
					return ((double)lhs) / ((double)rhs);
			}
			throw new EvalException ("failed to execute '" + op + "' at " + (index + 1));
		}

		private object DoUnaryOp (object lhs, string op, int index)
		{
			if (lhs is bool) {
				if (op == "!")
					return !((bool)lhs);
			}
			if (lhs is long) {
				if (op == "-")
					return -((long)lhs);
			}
			if (lhs is double) {
				if (op == "-")
					return -((double)lhs);
			}
			throw new EvalException ("failed to execute '" + op + "' at " + (index + 1));
		}
		
		private object GetVariableValue (Id id)
		{
			string idStr = id.id;
			foreach (ProcessVarHandler vh in varHandlers) {
				object result = vh(idStr);
				if (result != null) {
					return result;
				}
			}
			throw new EvalException ("unknown identifier '" + idStr + "'");
		}
		
		private object GetFunctionCall (Id id, object[] args)
		{
			string idStr = id.id;
			foreach (ProcessFnHandler fh in fnHandlers) {
				object result = fh(idStr, args);
				if (result != null) {
					return result;
				}
			}
			throw new EvalException ("unknown or incorrect call to '" + idStr + "'");
		}
		
		private object ParseVarOrFunc (Tokenizer tokenizer)
		{
			Id id = (Id)tokenizer.NextToken ();
			Symbol peek = tokenizer.PeekToken () as Symbol;
			if ((peek != null) && (peek.symbol == "(")) {
				tokenizer.NextToken ();
				List<object> args = new List<object> ();
				peek = tokenizer.PeekToken () as Symbol;
				while ((peek == null) || (peek.symbol != ")")) {
					args.Add (ParseExpression (tokenizer));
					peek = tokenizer.PeekToken () as Symbol;
					if ((peek == null) || ((peek.symbol != ")") && (peek.symbol != ","))) {
						throw new EvalException ("incorrect function call of '" + id.id + "' at " + (tokenizer.Index + 1));
					}
					tokenizer.NextToken ();
				}
				return GetFunctionCall (id, args.ToArray ());
			} else {
				return GetVariableValue (id);
			}
		}
		
		private object ParsePrimaryExpression (Tokenizer tokenizer)
		{
			if (tokenizer.IsEOT ()) {
				throw new EvalException ("unexpected end of line");
			}
			Token peek = tokenizer.PeekToken ();
			if (peek is Symbol) {
				Symbol peekSymbol = (Symbol)peek;
				if (peekSymbol.symbol == "(") {
					int index = tokenizer.Index;
					tokenizer.NextToken ();
					object lhs = ParseExpression (tokenizer);
					peekSymbol = tokenizer.PeekToken () as Symbol;
					if ((peekSymbol != null) && (peekSymbol.symbol == ")")) {
						tokenizer.NextToken ();
						return lhs;
					}
					throw new EvalException ("Expected '(' unbalanced at " + (index + 1));
				} else if ((peekSymbol.symbol == "-") || (peekSymbol.symbol == "!")) {
					int index = tokenizer.Index;
					tokenizer.NextToken ();
					return DoUnaryOp (ParsePrimaryExpression (tokenizer), peekSymbol.symbol, index);
				} else {
					throw new EvalException ("Unexpected  '" + peekSymbol.symbol + "' at " + (tokenizer.Index + 1));
				}
			} else if (peek is Id) {
				return ParseVarOrFunc (tokenizer);
			} else if (peek is StringConstant) {
				tokenizer.NextToken ();
				return ((StringConstant)peek).str;
			} else if (peek is LongConstant) {
				tokenizer.NextToken ();
				return ((LongConstant)peek).longVal;
			} else if (peek is DoubleConstant) {
				tokenizer.NextToken ();
				return ((DoubleConstant)peek).doubleVal;
			} else {
				throw new EvalException ("Unexpected error at " + (tokenizer.Index + 1));
			}
		}
		
		private object ParseMultiplicativeExpression (Tokenizer tokenizer)
		{
			object lhs = ParsePrimaryExpression (tokenizer);
			while (true) {
				Symbol peek = tokenizer.PeekToken () as Symbol;
				if (peek != null) {
					if ((peek.symbol == "*") || (peek.symbol == "/") ||
						(peek.symbol == "%")) {
						int index = tokenizer.Index;
						tokenizer.NextToken ();
						object rhs = ParsePrimaryExpression (tokenizer);
						lhs = DoMultiplicativeOp (lhs, rhs, peek.symbol, index);
					} else {
						return lhs;
					}
				} else {
					return lhs;
				}
			}
		}
		
		private object ParseAdditiveExpression (Tokenizer tokenizer)
		{
			object lhs = ParseMultiplicativeExpression (tokenizer);
			while (true) {
				Symbol peek = tokenizer.PeekToken () as Symbol;
				if (peek != null) {
					if ((peek.symbol == "+") || (peek.symbol == "-")) {
						int index = tokenizer.Index;
						tokenizer.NextToken ();
						object rhs = ParseMultiplicativeExpression (tokenizer);
						lhs = DoAdditiveOp (lhs, rhs, peek.symbol, index);
					} else {
						return lhs;
					}
				} else {
					return lhs;
				}
			}
		}
		
		private object ParseEqualityExpression (Tokenizer tokenizer)
		{
			object lhs = ParseAdditiveExpression (tokenizer);
			while (true) {
				Symbol peek = tokenizer.PeekToken () as Symbol;
				if (peek != null) {
					if ((peek.symbol == "==") || (peek.symbol == "!=") ||
						(peek.symbol == "<=") || (peek.symbol == ">=") ||
						(peek.symbol == "<") || (peek.symbol == ">")) {
						int index = tokenizer.Index;
						tokenizer.NextToken ();
						object rhs = ParseAdditiveExpression (tokenizer);
						lhs = DoEqualityOp (lhs, rhs, peek.symbol, index);
					} else {
						return lhs;
					}
				} else {
					return lhs;
				}
			}			
		}
					
		private object ParseExpression (Tokenizer tokenizer)
		{
			return ParseEqualityExpression (tokenizer);
		}
		
		/**
		 * Parses expression in str and if valid, returns result.
		 * result can be a bool, string, long or double
		 * on error parsing an EvalException can be thrown.
		 * other exceptions can occur during execution of expression (like when dividing by 0)
		 */
		public object Parse (string str)
		{
			Tokenizer tokenizer = new Tokenizer (str);
			object result = ParseExpression (tokenizer);
			if (!tokenizer.IsEOT ()) {
				throw new EvalException ("Unexpected garbage after expression at " + (tokenizer.Index + 1));
			}
			return result;
		}
		
		/**
		 * Parses str. str can contain embedded expressions, using the '[' ']' chars.
		 * e.g. "Text [1+4]!" would result in "Text 5!", special cases are "[[" resulting in "["
		 * and "[]" resulting in "[]". EvalExceptions can occur if expression is invalid, other
		 * exceptions can occur during execution of expression.
		 * if noExceptions is true errors in parsing won't result in exceptions thrown.
		 */
		public string ParseAndSubstitute (string str, bool noExceptions)
		{
			if (str.IndexOf ('[') < 0) {
				return str; // no '[', so certainly nothing to substitute
			}
			Tokenizer tokenizer = new Tokenizer (str);
			StringBuilder builder = new StringBuilder (str.Length + 32);
			while (!tokenizer.IsEOT ()) {
				char nextChar = tokenizer.NextChar ();
				if (nextChar == '[') {
					if (tokenizer.PeekChar () == '[') {
						tokenizer.NextChar ();
						builder.Append ('['); // we recognise '[[' as '['
					} else if (tokenizer.PeekChar () == ']') {
						tokenizer.NextChar ();
						builder.Append ("[]"); // we recognise '[]' as '[]'
					} else {
						// we got an expression...
						try {
							object result = ParseExpression (tokenizer);
							builder.Append (result.ToString ());
						}
						catch (Exception e) {
							Log.LogException (e);
							builder.Append ("<error>");
						}
						tokenizer.SkipWhiteSpace ();
						if (tokenizer.PeekChar () != ']') {
							if (noExceptions) {
								builder.Append ("<Missing ']'>");
							}
							else {
								throw new EvalException ("expected ']' at " + (tokenizer.Index + 1));
							}
						}
						else {
							tokenizer.NextChar ();
						}
					}
				} else {
					builder.Append (nextChar);
				}
			}
			return builder.ToString ();
		}
	}
}