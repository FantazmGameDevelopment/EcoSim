using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class ReportWindow : ReportBaseWindow
{
	private Scene scene;
	private Report report;
	private Progression.ReportState state;
	private List<Progression.ReportState.ParagraphState> paragraphStates;
	private List<Progression.QuestionnaireState.QuestionState> copyToReportQuestions;

	private Vector2 scrollPos;

	public ReportWindow (Report report, System.Action onFinished) : base (onFinished)
	{
		this.scene = EditorCtrl.self.scene;
		this.report = report;
		this.state = scene.progression.GetReportState (this.report.id);
		this.state.name = scene.playerInfo.firstName + " " + scene.playerInfo.familyName;

		// Paragraph states
		this.paragraphStates = new List<Progression.ReportState.ParagraphState>();
		for (int i = 0; i < this.report.paragraphs.Count; i++) {
			Progression.ReportState.ParagraphState ps = new Progression.ReportState.ParagraphState ();
			this.state.paragraphStates.Add (ps);
			this.paragraphStates.Add (ps);
		}

		// Find all "copy to reports"
		this.copyToReportQuestions = new List<Progression.QuestionnaireState.QuestionState>();
		foreach (Questionnaire qn in ReportsMgr.self.questionnaires) 
		{
			if (!qn.enabled) continue;
			foreach (Question q in qn.questions) 
			{
				if (!(q is OpenQuestion)) continue;

				OpenQuestion oq = (OpenQuestion)q;
				for (int i = 0; i < oq.answers.Count; i++) 
				{
					if (oq.answers [i] is OpenQuestion.OpenAnswer) 
					{
						OpenQuestion.OpenAnswer oa = (OpenQuestion.OpenAnswer)oq.answers [i];
						if (oa.copyToReport && oa.reportIndices.Contains (this.report.id)) 
						{
							// Find the question state
							Progression.QuestionnaireState qs = EditorCtrl.self.scene.progression.GetQuestionnaireState (qn.id, false);
							if (qs != null)
								this.copyToReportQuestions.Add (qs.GetQuestionState (i));
						}
					}
				}
			}
		}
	}
	
	public override void Render ()
	{
		base.Render ();

		height = Screen.height * 0.85f;
		top = (Screen.height - height) * 0.5f;

		GUILayout.BeginArea (new Rect (left, top, width + 20, height)); 
		//{

		// Header
		GUILayout.Label ("Report: " + this.report.name, headerDark, GUILayout.Width (width), defaultOption);
		GUILayout.Space (5);
		scrollPos = GUILayout.BeginScrollView (scrollPos);

		// Name/number
		if (this.report.useName) 
		{
			//GUILayout.BeginHorizontal ();
			//{
			GUILayout.Label ("Name:", headerDark, GUILayout.Width (width), defaultOption);
			this.state.name = GUILayout.TextField (this.state.name, textArea, GUILayout.Width (width), defaultOption);
			//}
			//GUILayout.EndHorizontal ();
		}
		if (this.report.useNumber) 
		{
			//GUILayout.BeginHorizontal ();
			//{
			GUILayout.Label ("Number:", headerDark, GUILayout.Width (width), defaultOption);
			this.state.number = GUILayout.TextField (this.state.number, textArea, GUILayout.Width (width), defaultOption);
			//}
			//GUILayout.EndHorizontal ();
		}
		if (this.report.useName || this.report.useNumber) {
			GUILayout.Space (5);
		}

		// Introduction
		if (this.report.useIntroduction)
		{
			GUILayout.Label ("Introduction:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Label (this.report.introduction, headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (5);
		}

		// Copy to reports
		foreach (Progression.QuestionnaireState.QuestionState qs in this.copyToReportQuestions)
		{
			// Name
			GUILayout.Label ("\"" + qs.questionName + "\"", headerDark, GUILayout.Width (width), defaultOption);
			GUILayout.Label ("\"" + qs.questionAnswer + "\"", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (5);
		}

		// Paragraphs
		Progression.ReportState.ParagraphState ps;
		ReportParagraph rp;
		for (int i = 0; i < this.report.paragraphs.Count; i++)		
		{
			ps = this.paragraphStates [i];
			rp = this.report.paragraphs [i];

			// Header
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ((i+1).ToString(), headerDark, GUILayout.Width (30), defaultOption);
				if (rp.useTitle) GUILayout.Label (rp.title, headerDark, GUILayout.Width (width - 30), defaultOption);
			}
			GUILayout.EndHorizontal ();

			// Description
			if (rp.useDescription) {
				GUILayout.Label (rp.description, headerLight, GUILayout.Width (width), defaultOption);
			}

			// Body
			if (rp.useMaxChars) 
			{
				ps.body = GUILayout.TextArea (ps.body, rp.maxChars, textArea, GUILayout.Width (width), defaultOption);
				GUILayout.Label (string.Format("Characters {0}/{1}", ps.body.Length, rp.maxChars), headerLight, GUILayout.Width (width), defaultOption);
			} else 
			{
				ps.body = GUILayout.TextArea (ps.body, textArea, GUILayout.Width (width), defaultOption);
			}

			GUILayout.Space (5);
		}

		// Conclusion
		if (this.report.useConclusion)
		{
			GUILayout.Label ("Conclusion:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Label (this.report.conclusion, headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (5);
		}
		
		GUILayout.BeginHorizontal ();
		{
			GUILayout.Label ("", headerLight, GUILayout.Width (width - 2 - (80 * 2)), defaultOption);

			// Save
			GUILayout.Space (1);
			if (GUILayout.Button ("Save", button, GUILayout.Width (80), defaultOption)) 
			{
				DoSave ();
			}

			// Continue
			GUILayout.Space (1);
			if (GUILayout.Button ("Continue", button, GUILayout.Width (80), defaultOption))
			{
				if (onFinished != null)
					onFinished ();
			}
		}
		GUILayout.EndHorizontal ();

		//}
		GUILayout.EndScrollView ();
		GUILayout.EndArea ();
	}

	private void DoSave ()
	{
		// Save to .txt
		System.Windows.Forms.SaveFileDialog sfd = this.GetSaveFileDialog ();
		sfd.FileName = string.Format ("report_{0}", this.report.id);
		System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding ();
		
		if (sfd.ShowDialog () == System.Windows.Forms.DialogResult.OK)
		{
			// Create new file
			FileStream fs = File.Create (sfd.FileName);
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();

			if (this.report.useName) sb.AppendFormat ("Name: {0}\n", this.state.name);
			if (this.report.useNumber) sb.AppendFormat ("Number: {0}\n", this.state.number);
			sb.AppendFormat ("Date: {0}\n", System.DateTime.Today.ToString ("dd\\/MM\\/yyyy"));
			sb.AppendLine ();

			sb.AppendFormat ("Report {0}:\n", this.report.id);
			sb.AppendFormat ("{0}\n", this.report.name);
			sb.AppendLine ();

			if (this.report.useIntroduction)
			{
				sb.AppendFormat ("Introduction:\n");
				sb.AppendFormat ("{0}\n", this.report.introduction);
				sb.AppendLine ();
			}

			foreach (Progression.QuestionnaireState.QuestionState qs in this.copyToReportQuestions)
			{
				sb.AppendFormat ("\"{0}\"\n", qs.questionName);
				sb.AppendFormat ("\"{0}\"\n", qs.questionAnswer);
				sb.AppendLine ();
			}

			Progression.ReportState.ParagraphState ps;
			ReportParagraph rp;
			for (int i = 0; i < this.report.paragraphs.Count; i++)		
			{
				ps = this.paragraphStates [i];
				rp = this.report.paragraphs [i];

				if (rp.useTitle) {
					sb.AppendFormat ("{0}\n", rp.title);
				}
				if (rp.useDescription) {
					sb.AppendFormat ("{0}\n", rp.description);
					sb.AppendLine ();
				}

				sb.AppendFormat ("{0}\n", ps.body);
				sb.AppendLine ();
			}

			if (this.report.useConclusion)
			{
				sb.AppendFormat ("Conclusion:\n");
				sb.AppendFormat ("{0}\n", this.report.conclusion);
				sb.AppendLine ();
			}
			
			// Stringify and save
			string txt = sb.ToString ();
			fs.Write (enc.GetBytes (txt), 0, enc.GetByteCount (txt));
			
			// Close and dispose the stream
			fs.Close ();
			fs.Dispose ();
			fs = null;
		}
	}

	public override void Dispose ()
	{
		base.Dispose ();

		this.report = null;
	}
}