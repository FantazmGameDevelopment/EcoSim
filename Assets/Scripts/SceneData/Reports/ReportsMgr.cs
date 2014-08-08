using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace Ecosim.SceneData
{
	public class ReportsMgr
	{
		public class QuestionnaireYearData
		{
			public int id;
			public int year;

			public QuestionnaireYearData () { }
			public QuestionnaireYearData (int id, int year)
			{
				this.id = id;
				this.year = year;
			}
		}

		public const string XML_ELEMENT = "reports";
		public static ReportsMgr self;
		private readonly Scene scene;

		public bool useShowQuestionnaireAtStart;
		public bool useShowQuestionnaireAtEnd;
		public int showQuestionnaireAtStartId;
		public int showQuestionnaireAtEndId; // FIXME: Is this necessary?

		public bool useShowReportAtEnd;
		public int showReportAtEndId;

		public List<Questionnaire> questionnaires;
		public List<Report> reports;
		public List<QuestionnaireYearData> questionnaireYears;

		private int queueIndex;
		public List<ReportBase> queue;

		public ReportsMgr (Scene scene)
		{
			self = this;

			this.scene = scene;
			this.scene.onGameEnd += OnGameEnd;
			this.questionnaires = new List<Questionnaire>();
			this.questionnaireYears = new List<QuestionnaireYearData> ();
			this.reports = new List<Report> ();
			this.queue = new List<ReportBase> ();
			this.queueIndex = 0;

			showQuestionnaireAtStartId = 1;
			showQuestionnaireAtEndId = 1;
			showReportAtEndId = 1;
		}

		public void Init ()
		{
			// Check if we have start questionnaires
			if (this.useShowQuestionnaireAtStart)
			{
				foreach (Questionnaire q in this.questionnaires) {
					if (q.id == this.showQuestionnaireAtStartId && q.enabled) {
						// Check if this questionnaire was already finished
						if (scene.progression.GetQuestionnaireState (q.id, false) == null) {
							queue.Add (q.Copy ());
							break;
						}
					}
				}
			}

			if (this.queue.Count > 0) {
				ShowReports.NotifyQueueChange ();
			}
		}

		public void FinalizeSuccession ()
		{
			// We don't show these questionnaires when game over
			// because it could mean we show a questionnaire after the report
			if (scene.progression.gameEnded) return;

			int year = scene.progression.year + 1;
			foreach (QuestionnaireYearData qy in this.questionnaireYears) 
			{
				if (qy.year == year)
				{
					// Find the questionnaire with id
					Questionnaire q = this.questionnaires.Find (x => x.id == qy.id);
					if (q != null) {
						queue.Add (q.Copy ());
					}
				}
			}

			if (this.queue.Count > 0) {
				ShowReports.NotifyQueueChange ();
			}
		}

		public void OnGameEnd ()
		{
			if (this.useShowReportAtEnd)
			{
				foreach (Report r in this.reports) {
					if (r.id == this.showReportAtEndId && r.enabled) {
						// Check if this report is already finished
						if (scene.progression.GetReportState (r.id, false) == null) {
							this.queue.Add (r);
							break;
						}
					}
				}
			}

			if (this.queue.Count > 0) {
				ShowReports.NotifyQueueChange ();
			}
		}

		public ReportBase CurrentInQueue ()
		{
			if (queueIndex < queue.Count) {
				return queue[queueIndex];
			}
			return null;
		}
		
		public bool ToNextInQueue ()
		{
			if (queueIndex < queue.Count) {
				queueIndex++;
			}
			return (queueIndex < queue.Count);
		}

		private void Load (XmlTextReader reader)
		{
			useShowQuestionnaireAtStart = bool.Parse (reader.GetAttribute ("showqstart"));
			useShowQuestionnaireAtEnd = bool.Parse (reader.GetAttribute ("showqend"));
			showQuestionnaireAtStartId = int.Parse (reader.GetAttribute ("qstartid"));
			showQuestionnaireAtEndId = int.Parse (reader.GetAttribute ("qendid"));

			useShowReportAtEnd = bool.Parse (reader.GetAttribute ("showrstart"));
			showReportAtEndId = int.Parse (reader.GetAttribute ("rendid"));

			while (reader.Read()) 
			{
				XmlNodeType nType = reader.NodeType;
				if (nType == XmlNodeType.Element)
				{
					switch (reader.Name.ToLower())
					{
					case Questionnaire.XML_ELEMENT : 
						this.questionnaires.Add (Questionnaire.Load (reader, scene));
						break;

					case Report.XML_ELEMENT :
						this.reports.Add (Report.Load (reader, scene));
						break;

					case "qyear" :
						int id = int.Parse (reader.GetAttribute ("id"));
						int year = int.Parse (reader.GetAttribute ("year"));
						this.questionnaireYears.Add (new QuestionnaireYearData (id, year));
						break;
					}
				} 
				else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
					break;
				}
			}
		}

		public static ReportsMgr Load (string path, Scene scene)
		{
			ReportsMgr mgr = new ReportsMgr (scene);
			if (File.Exists (path + XML_ELEMENT + ".xml")) {
				XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (path + XML_ELEMENT + ".xml"));
				try {
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == XML_ELEMENT)) {
							mgr.Load (reader);
						}
					}
				} finally {
					reader.Close ();
				}
			}
			return mgr;
		}
		
		public void Save (string path)
		{
			XmlTextWriter writer = new XmlTextWriter (path + XML_ELEMENT + ".xml", System.Text.Encoding.UTF8);
			writer.WriteStartDocument (true);
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("showqstart", useShowQuestionnaireAtStart.ToString().ToLower());
			writer.WriteAttributeString ("showqend", useShowQuestionnaireAtEnd.ToString().ToLower());
			writer.WriteAttributeString ("qstartid", showQuestionnaireAtStartId.ToString());
			writer.WriteAttributeString ("qendid", showQuestionnaireAtEndId.ToString());
			writer.WriteAttributeString ("showrstart", useShowReportAtEnd.ToString().ToLower());
			writer.WriteAttributeString ("rendid", showReportAtEndId.ToString());
			foreach (Questionnaire q in questionnaires) {
				q.Save (writer, scene);
			}
			foreach (Report r in reports) {
				r.Save (writer, scene);
			}
			foreach (QuestionnaireYearData qy in questionnaireYears) {
				writer.WriteStartElement ("qyear");
				writer.WriteAttributeString ("year", qy.year.ToString());
				writer.WriteAttributeString ("id", qy.id.ToString ());
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteEndDocument ();
			writer.Close ();		
		}
		
		public void UpdateReferences ()
		{
			foreach (Questionnaire q in questionnaires) {
				q.UpdateReferences (scene);
			}
			foreach (Report r in reports) {
				r.UpdateReferences (scene);
			}
		}
	}
}
