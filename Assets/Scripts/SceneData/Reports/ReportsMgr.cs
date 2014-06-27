using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Reflection;

namespace Ecosim.SceneData
{
	public class ReportsMgr
	{
		public const string XML_ELEMENT = "reports";
		private readonly Scene scene;

		public bool useShowQuestionnaireAtStart;
		public bool useShowQuestionnaireAtEnd;
		public int showQuestionnaireAtStartId;
		public int showQuestionnaireAtEndId; // TODO: Is this necessary?

		public bool useShowReportAtEnd;
		public int showReportAtEndId;

		public List<Questionnaire> questionnaires;
		public List<Report> reports;

		private int queueIndex;
		public List<ReportBase> queue;

		public ReportsMgr (Scene scene)
		{
			this.scene = scene;
			this.questionnaires = new List<Questionnaire>();
			this.reports = new List<Report> ();
			showQuestionnaireAtStartId = 1;
			showQuestionnaireAtEndId = 1;
			showReportAtEndId = 1;
		}

		public void Init ()
		{
			this.queue = new List<ReportBase> ();
			this.queueIndex = 0;

			// Check if we have start questionnaires
			if (this.useShowQuestionnaireAtStart)
			{
				foreach (Questionnaire q in this.questionnaires) {
					if (q.id == this.showQuestionnaireAtStartId) {
						queue.Add (q);
						break;
					}
				}
			}

			if (this.queue.Count > 0) {
				ShowReports.NotifyQueueChange ();
			}
		}

		public void EndGame ()
		{
			// TODO: EndGame
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
