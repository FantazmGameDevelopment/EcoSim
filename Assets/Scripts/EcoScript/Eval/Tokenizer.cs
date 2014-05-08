using System.Collections.Generic;
using System.Text;

namespace Ecosim.EcoScript.Eval
{
	public class Tokenizer
	{
		
		private readonly string text;
		private int index;
		private int len;
		
		public Tokenizer (string text)
		{
			this.text = text;
			index = 0;
			len = text.Length;			
		}
		
		/**
		 * peeks in token list without moving position
		 * returns next token in list, or null if at end of list
		 */
		public Token PeekToken () {
			int saveIndex = index;
			Token result = NextToken ();
			index = saveIndex;
			return result;
		}		
		
		public void SkipWhiteSpace ()
		{
			while ((index < len) && (char.IsWhiteSpace(text[index]))) {
				index++;
			}
		}
		
		/**
		 * Checks if at end of text (ignores possible whitespace at end of text)
		 * doesn't move position in text
		 */
		public bool IsEOT ()
		{
			int saveIndex = index;
			SkipWhiteSpace ();
			bool eot = (index >= len);
			index = saveIndex;
			return eot;
		}
		
		/**
		 * returns next char in text, or throws exception if at end of stream
		 */
		public char NextChar ()
		{
			if (index >= len)
				throw new EvalException ("Unexpected EOT");
			return text [index++];
		}
		
		/**
		 * peeks at next char in text (without moving position), if at end
		 * of text, returns 0 character
		 */
		public char PeekChar ()
		{
			if (index >= len)
				return '\0';
			return text [index];
		}
		
		/**
		 * peeks at char offsetted by offset in text (without moving position),
		 * if at end of text, returns 0 character.
		 * offset 0 is next char, offset 1 char thereafter, ...
		 * negative offsets can be used (trying to access characters before
		 * the first character in text will result in 0 character).
		 */
		public char PeekChar (int offset)
		{
			if (index + offset >= len)
				return '\0';
			if (index + offset < 0)
				return '\0';
			return text [index + offset];
		}
		
		const string hex = "0123456789abcdefABCDEF";
		
		/**
		 * reads string constant, assumes position is at '"' character.
		 * if not a valid string constant, returns exception
		 * returns string constant token
		 */
		public StringConstant ReadString ()
		{
			StringBuilder rawString = new StringBuilder (128);
			StringBuilder parsedString = new StringBuilder (128);
			if (PeekChar () != '"') {
				throw new EvalException ("Invalid string token at character " + index + 1);
			}
			rawString.Append (NextChar ());
			while (PeekChar () != '"') {
				if (IsEOT ()) {
					throw new EvalException ("unterminated string token.");
				}
				else if (PeekChar () == '\\') {
					rawString.Append (NextChar ());
					char escChar = NextChar ();
					rawString.Append (escChar);
					switch (escChar) {
					case 't' :
						parsedString.Append ('\t');
						break;
					case 'n' :
						parsedString.Append ('\n');
						break;
					case 'r' :
						parsedString.Append ('\r');
						break;
					case 'x' :
						int count = 0;
						string hexStr = "";
						if (hex.IndexOf (PeekChar ()) < 0) {
							throw new EvalException ("invalid \\x hex code in string token at character " + index + 1);
						}
						while ((count < 4) && (hex.IndexOf (PeekChar ()) >= 0)) {
							hexStr += PeekChar ();
							rawString.Append (NextChar ());
							count++;
						}
						parsedString.Append (System.Convert.ToChar (System.Convert.ToUInt32 (hexStr, 16)));
						break;
					case '"' :
						parsedString.Append ('"');
						break;
					default :
						throw new EvalException ("invalid escape character '" + escChar + "' in string token at character " + index + 1);
					}
				} else {
					parsedString.Append (PeekChar ());
					rawString.Append (NextChar ());
				}
			}
			rawString.Append (NextChar ());
			return new Ecosim.EcoScript.Eval.StringConstant (rawString.ToString (), parsedString.ToString ());
		}
		
		/**
		 * reads number, assumes position is at a digit or - sign.
		 * if not a valid string constant, returns exception
		 * returns double constant token or long constant token
		 */
		private DoubleConstant ReadNumber ()
		{
			if (!char.IsDigit (PeekChar ()) && (PeekChar () != '-')) {
				throw new EvalException ("Invalid number token at character " + index + 1);
			}
			StringBuilder number = new StringBuilder (32);
			if (PeekChar () == '-') {
				number.Append (NextChar ());
			}
			bool canBeLong = true;
			while (char.IsDigit(PeekChar())) {
				number.Append (NextChar ());
			}
			if (PeekChar () == '.') {
				canBeLong = false;
				number.Append (NextChar ());
				if (!char.IsDigit (PeekChar ())) {
					throw new EvalException ("Invalid number token at character " + index + 1);
				}
				while (char.IsDigit(PeekChar ())) {
					number.Append (NextChar ());
				}
			}
			if (PeekChar () == 'e') {
				canBeLong = false;
				number.Append (NextChar ());
				if (!char.IsDigit (PeekChar ()) && (PeekChar () != '+') && (PeekChar () != '-')) {
					throw new EvalException ("Invalid exponent in number token at character " + index + 1);
				}
				number.Append (NextChar ());
				while (char.IsDigit(PeekChar ())) {
					number.Append (NextChar ());
				}
			}
			return canBeLong ? (new LongConstant (number.ToString ())) : (new DoubleConstant (number.ToString ()));
		}
		
		/**
		 * reads in id ('_', a-z, A-Z, followed by 0 or more '_", a-z, A-Z, 0-9 characters).
		 */
		public Id ReadId ()
		{
			char peek = PeekChar ();
			if ((!char.IsLetter (peek)) || (peek == '_')) {
				throw new EvalException ("Invalid id token at character " + index + 1);
			}
			StringBuilder str = new StringBuilder (32);
			while ((char.IsLetterOrDigit (peek)) || (peek == '_')) {
				str.Append (peek);
				NextChar ();
				peek = PeekChar ();
			}
			return new Id (str.ToString ());
		}
		
		string[] symbols2 = new string[] {
			"==", "!=", "<=", ">=", "&&", "||", "++", "--"
		};
		
		string symbols1 = "!%^()-+/*[]?<>,";
		
		/**
		 * reads symbol ( '+', '-', '==', ...)
		 */
		public Symbol ReadSymbol () {
			
			if (index < len - 1) {
				string cmp = text.Substring (index, 2);
				foreach (string cmp2 in symbols2) {
					if (cmp == cmp2) {
						NextChar ();
						NextChar ();
						return new Symbol (cmp);
					}
				}
			}
			char peek = PeekChar ();
			if (symbols1.IndexOf (peek) >= 0) {
				NextChar ();
				return new Symbol (peek.ToString ());
			}
			throw new EvalException ("Unknown token '" + peek + "' at " + index + 1);
		}
		
		public Token NextToken ()
		{
			SkipWhiteSpace ();
			if (index >= len)
				return null;
			char peek = PeekChar ();
			if (char.IsDigit (peek)) {
				return ReadNumber ();
			}
			else if (peek == '"') {
				return ReadString ();
			}
			else if (char.IsLetter (peek)) {
				return ReadId ();
			}
			else return ReadSymbol ();
		}
		
		public int Index {
			get { return index; }
		}
	}
}
