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
		public abstract void Save (XmlTextWriter writer, Scene scene);
		public abstract void Load (XmlTextReader reader, Scene scene);
		public abstract void UpdateReferences (Scene scene);
	}

	public abstract class Answer
	{
		public string body;
		public string feedback;
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

		public List<MPCAnswer> answers;

		public MPCQuestion ()
		{
			this.answers = new List<MPCAnswer> ();
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
			
			public int maxWords;
			public bool copyToReport;
			
			public OpenAnswer ()
			{
				body = "New answer";
				maxWords = 0;
				copyToReport = false;
			}
			
			override public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				writer.WriteAttributeString ("body", body);
				writer.WriteAttributeString ("feedback", feedback);
				writer.WriteAttributeString ("maxwords", maxWords.ToString());
				writer.WriteAttributeString ("copytoreport", copyToReport.ToString().ToLower());
				writer.WriteEndElement ();
			}
			
			override public void Load (XmlTextReader reader, Scene scene)
			{
				body = reader.GetAttribute ("body");
				feedback = reader.GetAttribute ("feedback");
				maxWords = int.Parse (reader.GetAttribute ("maxwords"));
				copyToReport = bool.Parse (reader.GetAttribute ("copytoreport"));
				
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

		public List<OpenAnswer> answers;

		public OpenQuestion ()
		{
			answers = new List<OpenAnswer> ();
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

		public bool use;
		public bool useIntroduction;
		public string introduction;
		public bool useRequiredScore;
		public int requiredScore;
		public string passedFeedback;
		public string failedFeedback;
		public List<Question> questions;
		
		public Questionnaire ()
		{
			this.questions = new List<Question>();
		}

		public static Questionnaire Load (XmlTextReader reader, Scene scene)
		{
			Questionnaire questionnaire = new Questionnaire ();
			questionnaire.use = bool.Parse (reader.GetAttribute ("use"));
			questionnaire.useIntroduction = bool.Parse (reader.GetAttribute ("useintro"));
			questionnaire.introduction = reader.GetAttribute ("intro");
			questionnaire.useRequiredScore = bool.Parse (reader.GetAttribute ("usereqscore"));
			questionnaire.requiredScore = int.Parse (reader.GetAttribute ("reqscore"));
			questionnaire.passedFeedback = reader.GetAttribute ("passedfb");
			questionnaire.failedFeedback = reader.GetAttribute ("failedfb");

			while (reader.Read ())
			{
				XmlNodeType nt = reader.NodeType;
				if (nt == XmlNodeType.Element) 
				{
					switch (reader.Name.ToLower ())
					{
					case OpenQuestion.XML_ELEMENT :
					{
						OpenQuestion q = new OpenQuestion ();
						q.Load (reader, scene);
						questionnaire.questions.Add (q);
					}
					break;

					case MPCQuestion.XML_ELEMENT:
					{
						MPCQuestion q = new MPCQuestion ();
						q.Load (reader, scene);
						questionnaire.questions.Add (q);
					}
					break;
					}
				}
				else if (nt == XmlNodeType.EndElement && reader.Name.ToLower () == XML_ELEMENT) {
					break;
				}
			}

			return questionnaire;
		}
		
		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("use", use.ToString().ToLower());
			writer.WriteAttributeString ("useintro", useIntroduction.ToString().ToLower());
			writer.WriteAttributeString ("intro", introduction);
			writer.WriteAttributeString ("usereqscore", useRequiredScore.ToString().ToLower());
			writer.WriteAttributeString ("reqscore", requiredScore.ToString());
			writer.WriteAttributeString ("passedfb", passedFeedback);
			writer.WriteAttributeString ("failedfb", failedFeedback);
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
