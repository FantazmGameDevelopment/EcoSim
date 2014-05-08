using System;
using Ecosim.SceneData;

namespace Ecosim.EcoScript.Eval
{
	/**
	 * Extension of Expression that has symbol matching with EcoSim relevant functions
	 * and variables.
	 */
	public class EcoExpression : Expression
	{
		private readonly Scene scene;
		
		public EcoExpression (Scene scene) : base ()
		{
			this.scene = scene;
			AddVarHandler (HandleStringSubstitutions);
			AddVarHandler (HandleVarSpecial);
			AddVarHandler (HandleVarSubstitutions);
			AddFnHander (Functions);
		}
		
		/**
		 * Matches some text substitution names like 'heshe'.
		 * returns string if id matches
		 * otherwise null
		 */
		public object HandleStringSubstitutions (string id)
		{
			bool isMale = (scene.playerInfo == null) || (scene.playerInfo.isMale);
			bool startsUpper = (id.Length > 0) && (char.IsUpper (id [0]));
			string lower = id.ToLower ();
			if (lower == "heshe") {
				if (isMale)
					return startsUpper ? "He" : "he";
				else
					return startsUpper ? "She" : "she";
			}
			if (lower == "hisher") {
				if (isMale)
					return startsUpper ? "His" : "his";
				else
					return startsUpper ? "Her" : "her";
			}
			if (lower == "himher") {
				if (isMale)
					return startsUpper ? "Him" : "him";
				else
					return startsUpper ? "Her" : "her";
			}
			if (lower == "mrmrs") {
				return (isMale)?"Mr.":"Mrs.";
			}
			if (lower == "mrms") {
				return (isMale)?"Mr.":"Ms.";
			}
			if (lower == "firstname") {
				return (scene.playerInfo == null)?"John":(scene.playerInfo.firstName);
			}
			if (lower == "familyname") {
				return (scene.playerInfo == null)?"Fisher":(scene.playerInfo.familyName);
			}
			return null;
		}
		
		/**
		 * Matches some progress variables.
		 * returns long, double, string, bool if id matches
		 * otherwise null
		 */
		public object HandleVarSpecial (string id)
		{
			if (id == "year") {
				return (long) scene.progression.year;
			}
			if (id == "startyear") {
				return (long) scene.progression.startYear;
			}
			if (id == "budget") {
				return (long) scene.progression.budget;
			}
			return null;
		}
		
		/**
		 * returns bool, string, long, double or null if id doesn't match a variable, or the value of the
		 * variable isn't compatible with the acceptable result types (int and float will be casted to long and double).
		 */
		public object HandleVarSubstitutions (string id)
		{
			object result;
			if (scene.progression.variables.TryGetValue (id, out result)) {
				if ((result is long) || (result is string) || (result is bool) || (result is double)) return result;
				if (result is int) return (long) result;
				if (result is float) return (float) result;
			}
			return null;
		}
		
		/**
		 * Some basic functions for the expression evaluator
		 * args are objects that are either bool, string, long or double
		 * returns bool, string, long, double or null if id doesn't match a function
		 * or the arguments aren't compatible with the function.
		 */
		public object Functions (string id, object[] args)
		{
			if (id == "min") {
				if ((args.Length == 2) && (args[0] is long) && (args[1] is long)) {
					return (((long)(args[0]))<((long)(args[1])))?((long)(args[0])):((long)(args[1]));
				}
				if ((args.Length == 2) && (args[0] is double) && (args[1] is double)) {
					return (((double)(args[0]))<((double)(args[1])))?((double)(args[0])):((double)(args[1]));
				}
			}
			else if (id == "max") {
				if ((args.Length == 2) && (args[0] is long) && (args[1] is long)) {
					return (((long)(args[0]))>((long)(args[1])))?((long)(args[0])):((long)(args[1]));
				}
				if ((args.Length == 2) && (args[0] is double) && (args[1] is double)) {
					return (((double)(args[0]))>((double)(args[1])))?((double)(args[0])):((double)(args[1]));
				}
			}
			else if (id == "toupper") {
				if ((args.Length == 1) && (args[0] is string)) {
					return ((string) args[0]).ToUpper ();
				}
			}
			else if (id == "tolower") {
				if ((args.Length == 1) && (args[0] is string)) {
					return ((string) args[0]).ToLower ();
				}
			}
			else if (id == "tolong") {
				if ((args.Length == 1) && (args[0] is string)) {
					return long.Parse((string) args[0]);
				}
				if ((args.Length == 1) && (args[0] is double)) {
					return (long) ((double) args[0]);
				}
				if ((args.Length == 1) && (args[0] is long)) {
					return (long) args[0];
				}
			}
			else if (id == "todouble") {
				if ((args.Length == 1) && (args[0] is string)) {
					return double.Parse((string) args[0]);
				}
				if ((args.Length == 1) && (args[0] is long)) {
					return (double) ((long) args[0]);
				}
				if ((args.Length == 1) && (args[0] is double)) {
					return (double) args[0];
				}
			}
			else if (id == "tostring") {
				if (args.Length == 1) {
					return args[0].ToString ();
				}
				if ((args.Length == 2) && (args[0] is long) && (args[1] is string)) {
					return ((long) args[0]).ToString ((string) args[1]);
				}
				if ((args.Length == 2) && (args[0] is double) && (args[1] is string)) {
					return ((double) args[0]).ToString ((string) args[1]);
				}
			}
			return null;
		}
	}
}
