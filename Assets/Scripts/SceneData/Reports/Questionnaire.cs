using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace Ecosim.SceneData
{
	public abstract class Question
	{
		public string body;

		public bool opened;
		public bool answersOpened;
		public List<Answer> answers;

		public abstract void Save (XmlTextWriter writer, Scene scene);
		public abstract void Load (XmlTextReader reader, Scene scene);
		public abstract void UpdateReferences (Scene scene);
	}

	public abstract class Answer
	{
		public string body;
		public string feedback;
		public bool useFeedback;

		public bool opened;
		public bool feedbackOpened;

		public abstract void Save (XmlTextWriter writer, Scene scene);
		public abstract void Load (XmlTextReader reader, Scene scene);
		public abstract void UpdateReferences (Scene scene);
	}
		
	public class MPCQuestion : Question
	{
		public class MPCAnswer : Answer
		{
			public const string XML_ELEMENT = "answer";

			public bool startFromBeginning;
			public int moneyGained;
			public int score;
			public bool allowRetry;

			public MPCAnswer ()
			{
				body = "New answer";
				feedback = "Feedback";
				moneyGained = 0;
				score = 0;
				startFromBeginning = false;
				allowRetry = false;
			}

			override public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("body", body);
				writer.WriteAttributeString ("feedback", feedback);
				writer.WriteAttributeString ("usefb", useFeedback.ToString().ToLower());
				writer.WriteAttributeString ("startover", startFromBeginning.ToString().ToLower());
				writer.WriteAttributeString ("money", moneyGained.ToString());
				writer.WriteAttributeString ("score", score.ToString());
				writer.WriteAttributeString ("allowretry", allowRetry.ToString().ToLower());
				writer.WriteEndElement ();
			}
			
			override public void Load (XmlTextReader reader, Scene scene)
			{
				body = reader.GetAttribute ("body");
				feedback = reader.GetAttribute ("feedback");
				useFeedback = bool.Parse (reader.GetAttribute ("usefb"));
				startFromBeginning = bool.Parse (reader.GetAttribute ("startover"));
				moneyGained = int.Parse (reader.GetAttribute ("money"));
				score = int.Parse (reader.GetAttribute ("score"));
				allowRetry = bool.Parse (reader.GetAttribute ("allowretry"));

				if (!reader.IsEmptyElement)
				{
					while (reader.Read ())
					{
						XmlNodeType nt = reader.NodeType;
						if (nt == XmlNodeType.Element) 
						{
							/*switch (reader.Name.ToLower ())
						{
							case "" : break;
						}*/
						}
						else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
							break;
						}
					}
				}
			}
			
			override public void UpdateReferences (Scene scene)
			{
				
			}
		}

		public const string XML_ELEMENT = "mpc";

		public MPCQuestion ()
		{
			this.answers = new List<Answer> ();
			this.answers.Add (new MPCAnswer ());
			this.answers.Add (new MPCAnswer ());
			this.body = "New multiple choice question";
		}

		override public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("body", body);
			foreach (Answer a in answers) {
				a.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		override public void Load (XmlTextReader reader, Scene scene)
		{
			this.answers = new List<Answer> ();
			body = reader.GetAttribute ("body");

			if (!reader.IsEmptyElement)
			{
				while (reader.Read ())
				{
					XmlNodeType nt = reader.NodeType;
					if (nt == XmlNodeType.Element) 
					{
						switch (reader.Name.ToLower ())
						{
						case MPCAnswer.XML_ELEMENT :
							MPCAnswer a = new MPCAnswer();
							a.Load (reader, scene);
							this.answers.Add (a);
							break;
						}
					}
					else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
						break;
					}
				}
			}
		}

		override public void UpdateReferences (Scene scene)
		{
			foreach (MPCAnswer a in answers) {
				a.UpdateReferences (scene);
			}
		}
	}
	
	public class OpenQuestion : Question
	{
		public class OpenAnswer : Answer
		{
			public const string XML_ELEMENT = "answer";

			public bool useMaxChars;
			public int maxChars;
			public bool copyToReport;
			public List<int> reportIndices;

			public bool reportIndicesOpened;
			
			public OpenAnswer ()
			{
				body = "Write your answer";
				feedback = "Feedback";
				maxChars = 0;
				useMaxChars = false;
				copyToReport = false;
				reportIndices = new List<int>();
				reportIndices.Add (1);
			}
			
			override public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("body", body);
				writer.WriteAttributeString ("feedback", feedback);
				writer.WriteAttributeString ("usefb", useFeedback.ToString().ToLower());
				writer.WriteAttributeString ("usemaxchars", useMaxChars.ToString().ToLower());
				writer.WriteAttributeString ("maxchars", maxChars.ToString());
				writer.WriteAttributeString ("copytoreport", copyToReport.ToString().ToLower());

				string reportIndicesStr = "";
				foreach (int i in reportIndices) {
					reportIndicesStr += i.ToString() + ",";
				}
				reportIndicesStr = reportIndicesStr.TrimEnd (',');
				writer.WriteAttributeString ("copytoreports", reportIndicesStr);

				writer.WriteEndElement ();
			}
			
			override public void Load (XmlTextReader reader, Scene scene)
			{
				body = reader.GetAttribute ("body");
				feedback = reader.GetAttribute ("feedback");
				useFeedback = bool.Parse (reader.GetAttribute ("usefb"));
				maxChars = int.Parse (reader.GetAttribute ("maxchars"));
				useMaxChars = bool.Parse (reader.GetAttribute ("usemaxchars"));
				copyToReport = bool.Parse (reader.GetAttribute ("copytoreport"));

				string indicesStr = reader.GetAttribute ("copytoreports");
				string[] indices = indicesStr.Split (',');
				reportIndices = new List<int> ();
				if (indicesStr.Length > 0) {
					foreach (string i in indices) {
						int idx = 0;
						if (int.TryParse (i, out idx)) {
							reportIndices.Add (idx);
						}
					}
				}

				if (!reader.IsEmptyElement)
				{
					while (reader.Read ())
					{
						XmlNodeType nt = reader.NodeType;
						if (nt == XmlNodeType.Element) 
						{
							/*switch (reader.Name.ToLower ())
						{
							case "" : break;
						}*/
						}
						else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
							break;
						}
					}
				}
			}
			
			override public void UpdateReferences (Scene scene)
			{	
			}
		}

		public const string XML_ELEMENT = "open";

		public OpenQuestion ()
		{
			answers = new List<Answer> ();
			answers.Add (new OpenAnswer());
			this.body = "New open question";
		}

		override public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("body", body);
			foreach (Answer a in answers) {
				a.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		override public void Load (XmlTextReader reader, Scene scene)
		{
			this.body = reader.GetAttribute ("body");
			this.answers = new List<Answer> ();

			if (!reader.IsEmptyElement)
			{
				while (reader.Read ())
				{
					XmlNodeType nt = reader.NodeType;
					if (nt == XmlNodeType.Element) 
					{
						switch (reader.Name.ToLower ())
						{
						case OpenAnswer.XML_ELEMENT : 
							OpenAnswer a = new OpenAnswer ();
							a.Load (reader, scene);
							this.answers.Add (a);
							break;
						}
					}
					else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
						break;
					}
				}
			}
		}

		override public void UpdateReferences (Scene scene)
		{
			foreach (Answer a in answers) {
				a.UpdateReferences (scene);
			}
		}
	}

	public class Questionnaire : ReportBase
	{
		public const string XML_ELEMENT = "questionnaire";

		private Scene scene;

		// TODO: All todo's mean they have to be processed in the Editor interface
		
		public bool useRequiredScore;
		public int requiredScore;
		public bool useReqScoreFeedback;
		public string reqScoreFeedback;
		public bool usePassedFeedback;
		public bool useFailedFeedback;
		public string passedFeedback;
		public string failedFeedback;
		public bool startOverOnFailed;
		public List<Question> questions;
		public bool useBudget;
		public bool useBudgetFeedback;
		public string budgetFeedback;
		public bool useResultsPage;

		public bool questionsOpened;
		public bool reqScoreOpened;
		public bool reqScoreFeedbackOpened;
		public bool budgetOpened;
		public bool budgetFeedbackOpened;
		public bool passedFeedbackOpened;
		public bool failedFeedbackOpened;

		public int currentQuestionIndex;

		public Questionnaire () : base()
		{
			this.questions = new List<Question>();
			this.name = "New questionnaire";
			this.requiredScore = 100;
			this.passedFeedback = "Passed";
			this.failedFeedback = "Failed";
			this.budgetFeedback = "Explanation";
			this.reqScoreFeedback = "Explanation";
			this.currentQuestionIndex = 0;
			this.useResultsPage = true;
			this.showHeader = true;
		}

		public Questionnaire Copy ()
		{
			Questionnaire copy = new Questionnaire ();
			FieldInfo[] fields = this.GetType ().GetFields (BindingFlags.Public |
			                                                BindingFlags.DeclaredOnly |
			                                                BindingFlags.Instance);
			foreach (FieldInfo fi in fields) {
				fi.SetValue (copy, fi.GetValue (this));
			}

			FieldInfo[] baseFields = typeof (ReportBase).GetFields (BindingFlags.Public |
			                                                      BindingFlags.DeclaredOnly |
			                                                      BindingFlags.Instance);

			foreach (FieldInfo fi in baseFields) {
				fi.SetValue (copy, fi.GetValue (this));
			}
			return copy;
		}

		public static Questionnaire Load (XmlTextReader reader, Scene scene)
		{
			Questionnaire q = new Questionnaire ();
			q.LoadBase (reader, scene);
			q.useRequiredScore = bool.Parse (reader.GetAttribute ("usereqscore"));
			q.requiredScore = int.Parse (reader.GetAttribute ("reqscore"));
			q.useReqScoreFeedback = bool.Parse (reader.GetAttribute ("usereqscorefb"));
			q.reqScoreFeedback = reader.GetAttribute ("reqscorefb");
			q.usePassedFeedback = bool.Parse (reader.GetAttribute ("usepassedfb"));
			q.useFailedFeedback = bool.Parse (reader.GetAttribute ("usefailedfb"));
			q.passedFeedback = reader.GetAttribute ("passedfb");
			q.failedFeedback = reader.GetAttribute ("failedfb");
			q.startOverOnFailed = bool.Parse (reader.GetAttribute ("failstartover"));
			q.useBudget = bool.Parse (reader.GetAttribute ("usebudget"));
			q.useBudgetFeedback = bool.Parse (reader.GetAttribute ("usebudgetfb"));
			q.budgetFeedback = reader.GetAttribute ("budgetfb");
			if (!string.IsNullOrEmpty (reader.GetAttribute ("useresultspg"))) {
				q.useResultsPage = bool.Parse (reader.GetAttribute ("useresultspg"));
			}

			if (!reader.IsEmptyElement)
			{
				while (reader.Read ())
				{
					XmlNodeType nt = reader.NodeType;
					if (nt == XmlNodeType.Element) 
					{
						switch (reader.Name.ToLower ())
						{
						case OpenQuestion.XML_ELEMENT :
						{
							OpenQuestion newQ = new OpenQuestion ();
							newQ.Load (reader, scene);
							q.questions.Add (newQ);
						}
						break;

						case MPCQuestion.XML_ELEMENT:
						{
							MPCQuestion newQ = new MPCQuestion ();
							newQ.Load (reader, scene);
							q.questions.Add (newQ);
						}
						break;
						}
					}
					else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
						break;
					}
				}
			}

			return q;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			this.SaveBase (writer, scene);
			writer.WriteAttributeString ("usereqscore", useRequiredScore.ToString().ToLower());
			writer.WriteAttributeString ("reqscore", requiredScore.ToString());
			writer.WriteAttributeString ("usereqscorefb", useReqScoreFeedback.ToString().ToLower());
			writer.WriteAttributeString ("reqscorefb", reqScoreFeedback.ToString());
			writer.WriteAttributeString ("usepassedfb", usePassedFeedback.ToString().ToLower());
			writer.WriteAttributeString ("usefailedfb", useFailedFeedback.ToString().ToLower());
			writer.WriteAttributeString ("passedfb", passedFeedback);
			writer.WriteAttributeString ("failedfb", failedFeedback);
			writer.WriteAttributeString ("failstartover", startOverOnFailed.ToString().ToLower());
			writer.WriteAttributeString ("usebudget", useBudget.ToString().ToLower());
			writer.WriteAttributeString ("usebudgetfb", useBudgetFeedback.ToString().ToLower());
			writer.WriteAttributeString ("budgetfb", budgetFeedback.ToString());
			writer.WriteAttributeString ("useresultspg", useResultsPage.ToString().ToLower ());
			foreach (Question q in this.questions) {
				q.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public override void UpdateReferences (Scene scene)
		{
			foreach (Question q in this.questions) {
				q.UpdateReferences (scene);
			}
		}
	}
}
