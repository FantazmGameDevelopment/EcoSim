using UnityEngine;
using System.Collections;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor
{
	public class ReportsPanel : Panel
	{
		private enum Tabs 
		{
			Questionnaires,
			Reports
		}
		
		private Tabs currentTab;
		private Vector2 scrollPos;
		private GUIStyle tabNormal;
		private GUIStyle tabSelected;
		
		public Scene scene;
		public EditorCtrl ctrl;

		bool questionnaireYearsOpened = true;

		public void Setup (EditorCtrl ctrl, Scene scene)
		{
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene == null)
				return;

			tabNormal = ctrl.listItem;
			tabSelected = ctrl.listItemSelected;
			currentTab = Tabs.Questionnaires;
		}

		/**
		 * Called every frame when this panel is active
		 * It is guaranteed that setup has been called with
		 * a valid scene before the first call of Render.
		 */
		public bool Render(int mx, int my) 
		{ 
			if (scene == null)
				return false;
			
			GUILayout.BeginHorizontal ();
			{
				//GUILayout.Label ("Type:", GUILayout.Width (40));
				if (GUILayout.Button ("Questionnaires", (currentTab == Tabs.Questionnaires) ? tabSelected : tabNormal))
				{
					if (currentTab != Tabs.Questionnaires)
					{
						currentTab = Tabs.Questionnaires;
						scrollPos = Vector2.zero;
					}
				}
				if (GUILayout.Button ("Reports", (currentTab == Tabs.Reports) ? tabSelected : tabNormal))
				{
					if (currentTab != Tabs.Reports)
					{
						currentTab = Tabs.Reports;
						scrollPos = Vector2.zero;
					}
				}
			}
			GUILayout.EndHorizontal ();
			
			scrollPos = GUILayout.BeginScrollView (scrollPos, false, false);
			{
				GUILayout.BeginVertical ();
				{
					switch (currentTab)
					{
					case Tabs.Questionnaires : RenderQuestionnaires (mx, my); break;
					case Tabs.Reports : RenderReports (mx, my); break;
					}
					
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndVertical ();
			}
			GUILayout.EndScrollView ();

			return false;
		}

		private void RenderQuestionnaires (int mx, int my)
		{
			// Render questionnaires
			foreach (Questionnaire q in scene.reports.questionnaires) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					RenderReportBaseHeader (q);

					// Body
					if (q.opened)
					{
						// Show name
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							EcoGUI.Toggle ("Show header", ref q.showHeader);
						}
						GUILayout.EndVertical ();

						// Intro
						RenderReportBaseIntro (q);

						// Required score
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							GUILayout.BeginHorizontal ();
							{
								q.useRequiredScore = GUILayout.Toggle (q.useRequiredScore, "", GUILayout.Width (20));
								if (q.useRequiredScore) 
								{
									EcoGUI.skipHorizontal = true;
									EcoGUI.Foldout ("Required score", ref q.reqScoreOpened, GUILayout.Width (100));
									EcoGUI.skipHorizontal = false;
									EcoGUI.IntField ("", ref q.requiredScore, GUILayout.Width (1), GUILayout.Width (40));
								}
								else 
								{
									GUILayout.Label ("Required score");
									q.reqScoreOpened = false;
								}
							}
							GUILayout.EndHorizontal ();
							if (q.reqScoreOpened)
							{
								GUILayout.BeginVertical (ctrl.skin.box);
								{
									q.startOverOnFailed = GUILayout.Toggle (q.startOverOnFailed, "Start from beginning when failed");
									// We must have failed feedback
									if (q.startOverOnFailed) {
										q.useFailedFeedback = true;
									}

									// Passed Feedback
									GUILayout.BeginHorizontal ();
									{
										q.usePassedFeedback = GUILayout.Toggle (q.usePassedFeedback, "", GUILayout.Width (20));
										if (q.usePassedFeedback)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.Foldout ("Passed feedback", ref q.passedFeedbackOpened);
											EcoGUI.skipHorizontal = false;
										}
										else 
										{
											GUILayout.Label ("Passed feedback");
											q.passedFeedbackOpened = false;
										}
									}
									GUILayout.EndHorizontal ();
									if (q.passedFeedbackOpened) {
										q.passedFeedback = GUILayout.TextArea (q.passedFeedback);
									}

									// Failed Feedback
									GUILayout.BeginHorizontal ();
									{
										GUI.enabled = !q.startOverOnFailed;
										q.useFailedFeedback = GUILayout.Toggle (q.useFailedFeedback, "", GUILayout.Width (20));
										GUI.enabled = true;

										if (q.useFailedFeedback)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.Foldout ("Failed feedback", ref q.failedFeedbackOpened);
											EcoGUI.skipHorizontal = false;
										}
										else 
										{
											GUILayout.Label ("Failed feedback");
											q.failedFeedbackOpened = false;
										}
									}
									GUILayout.EndHorizontal ();
									if (q.failedFeedbackOpened) {
										q.failedFeedback = GUILayout.TextArea (q.failedFeedback);
									}

									// Feedback
									GUILayout.BeginHorizontal ();
									{
										q.useReqScoreFeedback = GUILayout.Toggle (q.useReqScoreFeedback, "", GUILayout.Width (20));
										if (q.useReqScoreFeedback)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.Foldout ("Explanation", ref q.reqScoreFeedbackOpened);
											EcoGUI.skipHorizontal = false;
										}
										else 
										{
											GUILayout.Label ("Explanation");
											q.useReqScoreFeedback = false;
										}
									}
									GUILayout.EndHorizontal ();
									if (q.reqScoreFeedbackOpened) {
										q.reqScoreFeedback = GUILayout.TextArea (q.reqScoreFeedback);
									}
								}
								GUILayout.EndVertical ();
							}
						}
						GUILayout.EndVertical (); // ~Required score

						// Budget
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							GUILayout.BeginHorizontal ();
							{
								q.useBudget = GUILayout.Toggle (q.useBudget, "", GUILayout.Width (20));
								if (q.useBudget) 
								{
									EcoGUI.skipHorizontal = true;
									EcoGUI.Foldout ("Earn extra budget", ref q.budgetOpened, GUILayout.Width (100));
									EcoGUI.skipHorizontal = false;
								}
								else 
								{
									GUILayout.Label ("Earn extra budget");
									q.budgetOpened = false;
								}
							}
							GUILayout.EndHorizontal ();
							if (q.budgetOpened)
							{
								GUILayout.BeginVertical (ctrl.skin.box);
								{
									// Feedback
									GUILayout.BeginHorizontal ();
									{
										q.useBudgetFeedback = GUILayout.Toggle (q.useBudgetFeedback, "", GUILayout.Width (20));
										if (q.useBudgetFeedback)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.Foldout ("Explanation", ref q.budgetFeedbackOpened);
											EcoGUI.skipHorizontal = false;
										}
										else 
										{
											GUILayout.Label ("Explanation");
											q.budgetFeedbackOpened = false;
										}
									}
									GUILayout.EndHorizontal ();
									if (q.budgetFeedbackOpened) {
										q.budgetFeedback = GUILayout.TextArea (q.budgetFeedback);
									}
								}
								GUILayout.EndVertical ();
							}
						}
						GUILayout.EndVertical (); // ~Budget

						// Questions
						EcoGUI.Foldout ("Questions", ref q.questionsOpened);
						if (q.questionsOpened)
						{
							// Questions
							Question question;
							for (int i = 0; i < q.questions.Count; i++) 
							{
								question = q.questions[i];
								if (question is MPCQuestion)
								{
									RenderMPCQuestion (q, (MPCQuestion)question, i);
								}
								else if (question is OpenQuestion)
								{
									RenderOpenQuestion (q, (OpenQuestion)question, i);
								}
								GUILayout.Space (2);
							}

							// Add buttons
							GUILayout.BeginHorizontal ();
							{
								GUILayout.Label (" Add new question:", GUILayout.Width (100));
								if (GUILayout.Button ("Multiple choice", GUILayout.Width (100)))
								{
									MPCQuestion newQ = new MPCQuestion ();
									newQ.opened = true;
									newQ.answersOpened = true;
									foreach (MPCQuestion.MPCAnswer a in newQ.answers) {
										a.opened = true;
										a.feedbackOpened = true;
									}
									q.questions.Add (newQ);
								}
								if (GUILayout.Button ("Open question", GUILayout.Width (100)))
								{
									OpenQuestion newQ = new OpenQuestion ();
									newQ.opened = true;
									newQ.answersOpened = true;
									foreach (OpenQuestion.OpenAnswer a in newQ.answers) {
										a.opened = true;
										a.feedbackOpened = true;
										a.reportIndicesOpened = true;
									}
									q.questions.Add (newQ);
								}
							}
							GUILayout.EndHorizontal (); // ~Add buttons
						} // ~Questions
						GUILayout.Space (3);

						// Conclusion
						RenderReportBaseConclusion (q);

						// Results
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							EcoGUI.Toggle ("Results page", ref q.useResultsPage);
						}
						GUILayout.EndVertical (); // ~Results
					}
					GUILayout.Space (3);
				}
				GUILayout.EndVertical ();
				GUILayout.Space (2);
			}

			// Add button
			if (GUILayout.Button ("+", GUILayout.Width (20)))
			{
				Questionnaire q = new Questionnaire ();
				q.opened = true;
				q.enabled = true;
				q.budgetFeedbackOpened = true;
				q.budgetOpened = true;
				q.conclusionOpened = true;
				q.failedFeedbackOpened = true;
				q.introOpened = true;
				q.passedFeedbackOpened = true;
				q.questionsOpened = true;
				q.reqScoreFeedbackOpened = true;
				q.reqScoreOpened = true;
				if (scene.reports.questionnaires.Count > 0)
					q.id = scene.reports.questionnaires [scene.reports.questionnaires.Count - 1].id + 1;
				else q.id = 1;
				scene.reports.questionnaires.Add (q);
			}

			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					scene.reports.useShowQuestionnaireAtStart = GUILayout.Toggle (scene.reports.useShowQuestionnaireAtStart, "Show questionnaire at game start", GUILayout.Width (200));
					if (scene.reports.useShowQuestionnaireAtStart) {
						EcoGUI.IntField ("ID:", ref scene.reports.showQuestionnaireAtStartId, 20, 50);
					}
				}
				GUILayout.EndHorizontal ();
				/*GUILayout.BeginHorizontal ();
				{
					scene.reports.useShowQuestionnaireAtEnd = GUILayout.Toggle (scene.reports.useShowQuestionnaireAtEnd, "Show questionnaire at game end", GUILayout.Width (200));
					if (scene.reports.useShowQuestionnaireAtEnd) {
						EcoGUI.IntField ("ID:", ref scene.reports.showQuestionnaireAtEndId, 20, 50);
					}
				}
				GUILayout.EndHorizontal ();*/

				GUILayout.Space (5);

				// Show questionnaires at years
				RenderShowQuestionnairesAtYears ();
			}
			GUILayout.EndVertical ();

			GUILayout.Space (5);
		}
		private void RenderMPCQuestion (Questionnaire q, MPCQuestion question, int index)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				RenderQuestionHeader (q, question, index);

				if (question.opened)
				{
					GUILayout.BeginHorizontal ();
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.Foldout ("Answers", ref question.answersOpened);
						EcoGUI.skipHorizontal = false;

						if (GUILayout.Button ("+", GUILayout.Width (20)))
						{
							MPCQuestion.MPCAnswer mpcA = new MPCQuestion.MPCAnswer ();
							mpcA.opened = true;
							mpcA.feedbackOpened = true;
							question.answers.Add (mpcA);
						}
					}
					GUILayout.EndHorizontal ();

					if (question.answersOpened)
					{
						MPCQuestion.MPCAnswer a;
						for (int i = 0; i < question.answers.Count; i++)
						{
							a = (MPCQuestion.MPCAnswer)question.answers[i];
							GUILayout.BeginVertical (ctrl.skin.box);
							{
								if (!RenderAnswerHeader (a, question, i)) break;

								if (a.opened)
								{
									a.startFromBeginning = GUILayout.Toggle (a.startFromBeginning, "Start from beginning");
									if (a.startFromBeginning) {
										// We must use feedback if we startFromBeginning enabled
										a.useFeedback = true;
									}
									a.allowRetry = GUILayout.Toggle (a.allowRetry, "Allow retry");
									if (a.allowRetry) {
										// We must use feedback if we have allowRetry enabled
										a.useFeedback = true;
									}

									// Feedback
									GUILayout.BeginHorizontal ();
									{
										GUI.enabled = true;
										if (a.allowRetry) GUI.enabled = false;
										if (a.startFromBeginning) GUI.enabled = false;

										a.useFeedback = GUILayout.Toggle (a.useFeedback, "", GUILayout.Width (20));
										GUI.enabled = true;
										if (a.useFeedback)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.Foldout ("Feedback", ref a.feedbackOpened);
											EcoGUI.skipHorizontal = false;
										}
										else 
										{
											GUILayout.Label ("Feedback");
											a.feedbackOpened = false;
										}
									}
									GUILayout.EndHorizontal ();
									if (a.feedbackOpened) {
										a.feedback = GUILayout.TextArea (a.feedback);
									}
							
									if (q.useRequiredScore) {
										EcoGUI.IntField ("Score", ref a.score, 80, 80);
									}
									if (q.useBudget) {
										EcoGUI.IntField ("Money gained", ref a.moneyGained, 80, 80);
									}
								}
							}
							GUILayout.EndVertical ();
						}
					}
				}
				GUILayout.Space (2);
			}
			GUILayout.EndVertical ();
		}
		private void RenderOpenQuestion (Questionnaire q, OpenQuestion question, int index)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				RenderQuestionHeader (q, question, index);

				if (question.opened)
				{
					question.answersOpened = true;

					/*GUILayout.BeginHorizontal ();
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.Foldout ("Answers", ref question.answersOpened);
						EcoGUI.skipHorizontal = false;
						
						if (GUILayout.Button ("+", GUILayout.Width (20)))
						{
							question.answers.Add (new OpenQuestion.OpenAnswer ());
						}
					}
					GUILayout.EndHorizontal ();*/
					
					if (question.answersOpened)
					{
						OpenQuestion.OpenAnswer a;
						for (int i = 0; i < question.answers.Count; i++) 
						{
							a = (OpenQuestion.OpenAnswer)question.answers[i];
							GUILayout.BeginVertical (ctrl.skin.box);
							{
								if (!RenderAnswerHeader (a, question, i)) break;
								
								if (a.opened)
								{
									GUILayout.BeginHorizontal ();
									{
										a.useMaxChars = GUILayout.Toggle (a.useMaxChars, "", GUILayout.Width (20));
										if (a.useMaxChars)
										{
											EcoGUI.skipHorizontal = true;
											EcoGUI.IntField ("Max. characters", ref a.maxChars, 120, 100);
											EcoGUI.skipHorizontal = false;
										} 
										else {
											GUILayout.Space (2);
											GUILayout.Label ("Max. characters");
										}
									}
									GUILayout.EndHorizontal ();

									// Copy to reports
									GUILayout.BeginHorizontal ();
									{
										a.copyToReport = GUILayout.Toggle (a.copyToReport, "", GUILayout.Width (20));
										if (a.copyToReport) 
										{
											EcoGUI.Foldout ("Copy to report(s)", ref a.reportIndicesOpened);
										} else 
										{
											GUILayout.Label ("Copy to report(s)");
											a.reportIndicesOpened = false;
										}
									}
									GUILayout.EndHorizontal ();

									if (a.reportIndicesOpened)
									{
										for (int n = 0; n < a.reportIndices.Count; n++) 
										{
											GUILayout.BeginHorizontal ();
											{
												GUILayout.Space (10);
												GUILayout.Label ("#" + (n + 1).ToString(), GUILayout.Width (15));
												int reportIndex = a.reportIndices [n];
												EcoGUI.IntField ("Report ID:", ref reportIndex, 50, 50);
												a.reportIndices [n] = reportIndex;

												// Delete
												if (n > 0 && GUILayout.Button ("-", GUILayout.Width (20))) 
												{
													a.reportIndices.RemoveAt (n);
													break;
												}
											}
											GUILayout.EndHorizontal ();
										}

										if (GUILayout.Button ("+", GUILayout.Width (20))) 
										{
											int id = 1;
											if (a.reportIndices.Count > 0) {
												id = a.reportIndices [a.reportIndices.Count - 1] + 1;
											}
											a.reportIndices.Add (id);
										}
									}
								}
							}
							GUILayout.EndVertical ();
						}
					}
				}
				GUILayout.Space (2);
			}
			GUILayout.EndVertical ();
		}
		private void RenderQuestionHeader (Questionnaire q, Question question, int index)
		{
			GUILayout.Space (2);
			GUILayout.BeginHorizontal ();
			{
				// Foldout header
				EcoGUI.skipHorizontal = true;
				EcoGUI.FoldoutEditableName (ref question.body, ref question.opened, GUILayout.MaxWidth (225));
				EcoGUI.skipHorizontal = false;

				// Type
				GUILayout.Space (5);
				string type = "[unknown]";
				if (question is OpenQuestion) type = "[open]";
				else if (question is MPCQuestion) type = "[mpc]";
				GUILayout.Label (type, GUILayout.Width (type.Length * 5));
				GUILayout.Space (5);

				// Up
				GUI.enabled = (index > 0);
				if (GUILayout.Button ("\u02C4", GUILayout.Width (20))) {
					q.questions.Remove (question);
					q.questions.Insert (index - 1, question);
				}

				// Down
				GUI.enabled = (index < q.questions.Count - 1);
				if (GUILayout.Button ("\u02C5", GUILayout.Width (20))) {
					q.questions.Remove (question);
					q.questions.Insert (index + 1, question);
				}
				GUI.enabled = true;

				// Remove
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-", GUILayout.Width (20)))
				{
					Question tmpQ = question;
					ctrl.StartDialog (string.Format ("Are you sure you want to delete question '{0}' (#{1})", tmpQ.body, index + 1), 
					                  delegate(bool result) {
						if (result) {
							q.questions.Remove (tmpQ);
						}
					}, null);
				}
			}
			GUILayout.EndHorizontal ();
		}
		private bool RenderAnswerHeader (Answer a, Question question, int index)
		{
			GUILayout.BeginHorizontal ();
			{
				EcoGUI.skipHorizontal = true;
				EcoGUI.FoldoutEditableName (ref a.body, ref a.opened, GUILayout.Width (320));
				EcoGUI.skipHorizontal = false;
				
				if (index > 1 && GUILayout.Button ("-", GUILayout.Width (20)))
				{
					question.answers.Remove (a);
					return false;
				}
			}
			GUILayout.EndHorizontal ();
			return true;
		}
		private void RenderShowQuestionnairesAtYears ()
		{
			if (EcoGUI.Foldout ("Show questionnaire at year", ref questionnaireYearsOpened))
			{
				GUILayout.Space (5);
				
				foreach (ReportsMgr.QuestionnaireYearData qy in ReportsMgr.self.questionnaireYears)
				{
					GUILayout.BeginHorizontal ();
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.IntField ("\tYear:", ref qy.year, 50, 50);
						EcoGUI.IntField ("ID:", ref qy.id, 30, 50);
						EcoGUI.skipHorizontal = false;

						GUILayout.Space (10);
						if (GUILayout.Button ("-", GUILayout.Width (20))) {
							ReportsMgr.self.questionnaireYears.Remove (qy);
							GUILayout.EndHorizontal ();
							break;
						}
					}
					GUILayout.EndHorizontal ();
				}
				
				GUILayout.Space (3);
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Space (16);
					if (GUILayout.Button ("Add year", GUILayout.Width (100))) 
					{
						ReportsMgr.QuestionnaireYearData qy = new ReportsMgr.QuestionnaireYearData();
						qy.year = scene.progression.startYear + 1;
						qy.id = 1;
						if (ReportsMgr.self.questionnaireYears.Count > 0) {
							ReportsMgr.QuestionnaireYearData last = ReportsMgr.self.questionnaireYears [ReportsMgr.self.questionnaireYears.Count - 1];
							qy.year = last.year + 1;
							qy.id = last.id + 1;
						}
						ReportsMgr.self.questionnaireYears.Add (qy);
					}
				}
				GUILayout.EndHorizontal ();
			}
		}

		private void RenderReports (int mx, int my)
		{
			foreach (Report r in scene.reports.reports) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					RenderReportBaseHeader (r);

					if (r.opened)
					{
						// Name and number
						r.useName = GUILayout.Toggle (r.useName, "Student name");
						r.useNumber = GUILayout.Toggle (r.useNumber, "Student number");

						// Intro
						RenderReportBaseIntro (r);

						EcoGUI.Foldout ("Paragraphs", ref r.paragraphsOpened);
						if (r.paragraphsOpened)
						{
							GUILayout.Space (2);

							// Paragraphs
							for (int i = 0; i < r.paragraphs.Count; i++)
							{
								ReportParagraph p = r.paragraphs [i];
								RenderParagraph (r, p, i);
							}

							GUILayout.Space (3);

							// Add button
							if (GUILayout.Button ("Add paragraph", GUILayout.Width (120)))
							{
								ReportParagraph p = new ReportParagraph ();
								p.opened = true;
								p.descriptionOpened = true;
								p.titleOpened = true;
								r.paragraphs.Add (p);
							}
						}
						GUILayout.Space (2);

						// Conclusion
						RenderReportBaseConclusion (r);
					}
					GUILayout.Space (3);
				}
				GUILayout.EndVertical ();
				GUILayout.Space (2);
			}
			
			// Add button
			if (GUILayout.Button ("+", GUILayout.Width (20)))
			{
				Report r = new Report ();
				r.opened = true;
				r.conclusionOpened = true;
				r.introOpened = true;
				r.paragraphsOpened = true;
				r.enabled = true;
				if (scene.reports.reports.Count > 0)
					r.id = scene.reports.reports [scene.reports.reports.Count - 1].id + 1;
				else r.id = 1;
				scene.reports.reports.Add (r);
			}
			
			GUILayout.BeginHorizontal ();
			{
				scene.reports.useShowReportAtEnd = GUILayout.Toggle (scene.reports.useShowReportAtEnd, "Show report at game end", GUILayout.Width (200));
				if (scene.reports.useShowReportAtEnd) {
					EcoGUI.IntField ("ID:", ref scene.reports.showReportAtEndId, 20, 50);
				}
			}
			GUILayout.EndHorizontal ();
			GUILayout.Space (5);
		}
		private void RenderParagraph (Report r, ReportParagraph p, int index)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				RenderParagraphHeader (r, p, index);

				if (p.opened)
				{
					// Description
					GUILayout.BeginHorizontal ();
					{
						GUI.enabled = true;
						p.useDescription = GUILayout.Toggle (p.useDescription, "", GUILayout.Width (20));
						GUI.enabled = true;
						if (p.useDescription)
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.Foldout ("Description", ref p.descriptionOpened);
							EcoGUI.skipHorizontal = false;
						}
						else 
						{
							GUILayout.Label ("Description");
							p.descriptionOpened = false;
						}
					}
					GUILayout.EndHorizontal ();
					if (p.descriptionOpened) {
						p.description = GUILayout.TextArea (p.description);
					}

					GUILayout.BeginHorizontal ();
					{
						p.useMaxChars = GUILayout.Toggle (p.useMaxChars, "", GUILayout.Width (20));
						if (p.useMaxChars)
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.IntField ("Max. characters", ref p.maxChars, 120, 100);
							EcoGUI.skipHorizontal = false;
						} 
						else {
							p.maxChars = 0;
							GUILayout.Space (2);
							GUILayout.Label ("Max. characters");
						}
					}
					GUILayout.EndHorizontal ();
				}
			}
			GUILayout.EndVertical ();
		}
		private void RenderParagraphHeader (Report r, ReportParagraph p, int index)
		{
			GUILayout.Space (2);
			GUILayout.BeginHorizontal ();
			{
				// Foldout header
				EcoGUI.skipHorizontal = true;
				EcoGUI.FoldoutEditableName (ref p.title, ref p.opened, GUILayout.MaxWidth (250));
				EcoGUI.skipHorizontal = false;
				GUILayout.FlexibleSpace ();

				// Up
				GUI.enabled = (index > 0);
				if (GUILayout.Button ("\u02C4", GUILayout.Width (20))) {
					r.paragraphs.Remove (p);
					r.paragraphs.Insert (index - 1, p);
				}
				
				// Down
				GUI.enabled = (index < r.paragraphs.Count - 1);
				if (GUILayout.Button ("\u02C5", GUILayout.Width (20))) {
					r.paragraphs.Remove (p);
					r.paragraphs.Insert (index + 1, p);
				}
				GUI.enabled = true;
				
				// Remove
				GUILayout.FlexibleSpace ();
				if (GUILayout.Button ("-", GUILayout.Width (20)))
				{
					ReportParagraph tmp = p;
					ctrl.StartDialog (string.Format ("Are you sure you want to delete paragraph '{0}' (#{1})", tmp.title, index + 1), 
					delegate(bool result) {
						if (result) {
							r.paragraphs.Remove (tmp);
						}
					}, null);
				}
			}
			GUILayout.EndHorizontal ();
		}

		private void RenderReportBaseHeader (ReportBase r)
		{
			GUILayout.BeginHorizontal ();
			{
				r.enabled = GUILayout.Toggle (r.enabled, "", GUILayout.Width (20));
				if (r.enabled) 
				{
					EcoGUI.skipHorizontal = true;
					EcoGUI.FoldoutEditableName (ref r.name, ref r.opened, GUILayout.MaxWidth (225));
					EcoGUI.skipHorizontal = false;
				} else {
					r.opened = false;
					GUILayout.Space (5f);
					r.name = GUILayout.TextField (r.name);
				}
				GUILayout.Space (5f);
				GUILayout.Label ("ID:" + r.id, GUILayout.Width (30));
				GUILayout.Space (5f);
				GUILayout.FlexibleSpace ();

				if (GUILayout.Button ("-", GUILayout.Width (20))) 
				{
					ReportBase tmp = r;
					ctrl.StartDialog (string.Format ("Are you sure you want to delete '{0}' (ID:{1})", tmp.name, tmp.id.ToString()), 
					delegate(bool result) {
						if (result) {

							// FIXME: Make this more generic
							if (r is Questionnaire) {
								scene.reports.questionnaires.Remove (tmp as Questionnaire);
							} else if (r is Report) {
								scene.reports.reports.Remove (tmp as Report);
							}
						}
					}, null);
				}
			}
			GUILayout.EndHorizontal (); // ~Header
		}

		private void RenderReportBaseIntro (ReportBase r)
		{
			// Intro
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Space (1);
					r.useIntroduction = GUILayout.Toggle (r.useIntroduction, "", GUILayout.Width (20));
					if (r.useIntroduction) 
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.Foldout ("Introduction", ref r.introOpened);
						EcoGUI.skipHorizontal = false;
					}
					else 
					{
						GUILayout.Label ("Introduction");
						r.introOpened = false;
					}
				}
				GUILayout.EndHorizontal ();
				if (r.introOpened) {
					r.introduction = GUILayout.TextArea (r.introduction);
				}
			}
			GUILayout.EndVertical (); // ~Intro
		}

		private void RenderReportBaseConclusion (ReportBase r)
		{
			// Conclusion
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					string name = "Conclusion";
					if (r is Questionnaire)
						name = "Final Remark";

					GUILayout.Space (1);
					r.useConclusion = GUILayout.Toggle (r.useConclusion, "", GUILayout.Width (20));
					if (r.useConclusion) 
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.Foldout (name, ref r.conclusionOpened);
						EcoGUI.skipHorizontal = false;
					}
					else 
					{
						GUILayout.Label (name);
						r.conclusionOpened = false;
					}
				}
				GUILayout.EndHorizontal ();
				if (r.conclusionOpened) {
					r.conclusion = GUILayout.TextArea (r.conclusion);
				}
			}
			GUILayout.EndVertical (); // ~Conclusion
		}
		
		/* Called for extra edit sub-panel, will be called after Render */
		public void RenderExtra(int mx, int my) 
		{ 
		}
		
		/* Called for extra side edit sub-panel, will be called after RenderExtra */
		public void RenderSide(int mx, int my) 
		{ 
		}
		
		/* Returns true if a side panel is needed. Won't be called before RenderExtra has been called */
		public bool NeedSidePanel() 
		{ 
			return false;
		}
		
		/* True if panel can be used */
		public bool IsAvailable() 
		{ 
			return (scene != null);
		}
		
		/**
		 * Panel is activated...
		 */
		public void Activate() 
		{ 
		}
		/**
		 * Panel is deactivated
		 */
		public void Deactivate()  
		{ 
		}
		
		/**
		 * For Unity.Update like stuff
		 */
		public void Update()  
		{ 
		}
	}
}
