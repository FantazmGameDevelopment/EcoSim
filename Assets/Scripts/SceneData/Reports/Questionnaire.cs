using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
				startFromBeginning = bool.Parse (reader.GetAttribute ("startover"));
				moneyGained = int.Parse (reader.GetAttribute ("money"));
				score = int.Parse (reader.GetAttribute ("score"));
				allowRetry = bool.Parse (reader.GetAttribute ("allowretry"));

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
			body = reader.GetAttribute ("body");

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

			public bool useMaxWords;
			public int maxWords;
			public bool copyToReport;
			public List<int> reportIndices;

			public bool reportIndicesOpened;
			
			public OpenAnswer ()
			{
				body = "Write your answer";
				feedback = "Feedback";
				maxWords = 0;
				useMaxWords = false;
				copyToReport = false;
				reportIndices = new List<int>();
				reportIndices.Add (0);
			}
			
			override public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("body", body);
				writer.WriteAttributeString ("feedback", feedback);
				writer.WriteAttributeString ("usemaxwords", useMaxWords.ToString().ToLower());
				writer.WriteAttributeString ("maxwords", maxWords.ToString());
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
				maxWords = int.Parse (reader.GetAttribute ("maxwords"));
				useMaxWords = bool.Parse (reader.GetAttribute ("usemaxwords"));
				copyToReport = bool.Parse (reader.GetAttribute ("copytoreport"));

				string indicesStr = reader.GetAttribute ("copytoreports");
				string[] indices = indicesStr.Split (',');
				reportIndices = new List<int> ();
				if (indicesStr.Length > 0) {
					foreach (string i in indices) {
						Debug.Log (i);
						int idx = 0;
						if (int.TryParse (i, out idx)) {
							reportIndices.Add (idx);
						}
					}
				}

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

		override public void UpdateReferences (Scene scene)
		{
			foreach (Answer a in answers) {
				a.UpdateReferences (scene);
			}
		}
	}

	public class Questionnaire
	{
		public const string XML_ELEMENT = "questionnaire";

		private Scene scene;

		public int id;
		public string name;
		public bool enabled;
		public bool useIntroduction;
		public string introduction;
		public bool useRequiredScore;
		public int requiredScore;
		public bool usePassedFeedback;
		public bool useFailedFeedback;
		public string passedFeedback;
		public string failedFeedback;
		public bool startOverOnFailed;
		public List<Question> questions;

		public bool opened;
		public bool questionsOpened;
		public bool introOpened;
		public bool reqScoreOpened;
		public bool passedFeedbackOpened;
		public bool failedFeedbackOpened;

		public Questionnaire ()
		{
			this.questions = new List<Question>();
			this.name = "New questionnaire";
			this.introduction = "Intro";
			this.requiredScore = 100;
			this.passedFeedback = "Passed";
			this.failedFeedback = "Failed";
		}

		public static Questionnaire Load (XmlTextReader reader, Scene scene)
		{
			Questionnaire q = new Questionnaire ();
			q.id = int.Parse (reader.GetAttribute ("id"));
			q.name = reader.GetAttribute ("name");
			q.enabled = bool.Parse (reader.GetAttribute ("enabled"));
			q.useIntroduction = bool.Parse (reader.GetAttribute ("useintro"));
			q.introduction = reader.GetAttribute ("intro");
			q.useRequiredScore = bool.Parse (reader.GetAttribute ("usereqscore"));
			q.requiredScore = int.Parse (reader.GetAttribute ("reqscore"));
			q.usePassedFeedback = bool.Parse (reader.GetAttribute ("usepassedfb"));
			q.useFailedFeedback = bool.Parse (reader.GetAttribute ("usefailedfb"));
			q.passedFeedback = reader.GetAttribute ("passedfb");
			q.failedFeedback = reader.GetAttribute ("failedfb");
			q.startOverOnFailed = bool.Parse (reader.GetAttribute ("failstartover"));

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

			return q;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString());
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("enabled", enabled.ToString().ToLower());
			writer.WriteAttributeString ("useintro", useIntroduction.ToString().ToLower());
			writer.WriteAttributeString ("intro", introduction);
			writer.WriteAttributeString ("usereqscore", useRequiredScore.ToString().ToLower());
			writer.WriteAttributeString ("reqscore", requiredScore.ToString());
			writer.WriteAttributeString ("usepassedfb", usePassedFeedback.ToString().ToLower());
			writer.WriteAttributeString ("usefailedfb", useFailedFeedback.ToString().ToLower());
			writer.WriteAttributeString ("passedfb", passedFeedback);
			writer.WriteAttributeString ("failedfb", failedFeedback);
			writer.WriteAttributeString ("failstartover", startOverOnFailed.ToString().ToLower());
			foreach (Question q in this.questions) {
				q.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}
		
		public void UpdateReferences (Scene scene)
		{
			foreach (Question q in this.questions) {
				q.UpdateReferences (scene);
			}
		}
	}
}
