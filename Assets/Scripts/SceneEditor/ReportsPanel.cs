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
					GUILayout.BeginHorizontal ();
					{
						q.enabled = GUILayout.Toggle (q.enabled, "", GUILayout.Width (20));
						if (q.enabled) 
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.FoldoutEditableName (ref q.name, ref q.opened);
							EcoGUI.skipHorizontal = false;
						} else {
							q.opened = false;
							GUILayout.Space (5f);
							q.name = GUILayout.TextField (q.name);
						}
						GUILayout.Space (5f);
						GUILayout.Label ("ID:" + q.id, GUILayout.Width (30));
						GUILayout.Space (5f);

						if (GUILayout.Button ("-", GUILayout.Width (20))) 
						{
							Questionnaire tmpQ = q;
							ctrl.StartDialog (string.Format ("Are you sure you want to delete questionnaire '{0}' (ID:{1})", tmpQ.name, tmpQ.id.ToString()), 
							delegate(bool result) {
								if (result) {
									scene.reports.questionnaires.Remove (tmpQ);
								}
							}, null);
						}
					}
					GUILayout.EndHorizontal (); // ~Header

					// Body
					if (q.opened)
					{
						// Intro
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							GUILayout.BeginHorizontal ();
							{
								q.useIntroduction = GUILayout.Toggle (q.useIntroduction, "", GUILayout.Width (20));
								if (q.useIntroduction) 
								{
									EcoGUI.skipHorizontal = true;
									EcoGUI.Foldout ("Introduction", ref q.introOpened);
									EcoGUI.skipHorizontal = false;
								}
								else 
								{
									GUILayout.Label ("Introduction");
									q.introOpened = false;
								}
							}
							GUILayout.EndHorizontal ();
							if (q.introOpened) {
								q.introduction = GUILayout.TextArea (q.introduction);
							}
						}
						GUILayout.EndVertical (); // ~Intro

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
										q.useFailedFeedback = GUILayout.Toggle (q.useFailedFeedback, "", GUILayout.Width (20));
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

									q.startOverOnFailed = GUILayout.Toggle (q.startOverOnFailed, "Start from beginning when failed");
								}
								GUILayout.EndVertical ();
							}
						}
						GUILayout.EndVertical (); // ~Required score

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
									q.questions.Add (newQ);
								}
								if (GUILayout.Button ("Open question", GUILayout.Width (100)))
								{
									OpenQuestion newQ = new OpenQuestion ();
									q.questions.Add (newQ);
								}
							}
							GUILayout.EndHorizontal (); // ~Add buttons
						}
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
				if (scene.reports.questionnaires.Count > 0)
					q.id = scene.reports.questionnaires [scene.reports.questionnaires.Count - 1].id + 1;
				else q.id = 1;
				scene.reports.questionnaires.Add (q);
			}
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
							question.answers.Add (new MPCQuestion.MPCAnswer ());
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
									// Feedback
									GUILayout.BeginHorizontal ();
									{
										a.useFeedback = GUILayout.Toggle (a.useFeedback, "", GUILayout.Width (20));
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

									a.startFromBeginning = GUILayout.Toggle (a.startFromBeginning, "Start from beginning");
									a.allowRetry = GUILayout.Toggle (a.allowRetry, "Allow retry");
									if (q.useRequiredScore) {
										EcoGUI.IntField ("Score", ref a.score, 80, 80);
									}
									EcoGUI.IntField ("Money gained", ref a.moneyGained, 80, 80);
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
					GUILayout.BeginHorizontal ();
					{
						EcoGUI.skipHorizontal = true;
						EcoGUI.Foldout ("Answers", ref question.answersOpened);
						EcoGUI.skipHorizontal = false;
						
						if (GUILayout.Button ("+", GUILayout.Width (20)))
						{
							question.answers.Add (new OpenQuestion.OpenAnswer ());
						}
					}
					GUILayout.EndHorizontal ();
					
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
									EcoGUI.IntField ("Max. words", ref a.maxWords, 100);

									// Copy to reports
									GUILayout.BeginHorizontal ();
									{
										a.copyToReport = GUILayout.Toggle (a.copyToReport, "", GUILayout.Width (5));
										if (a.copyToReport) {
											EcoGUI.Foldout ("Copy to report(s)", ref a.reportIndicesOpened);
										} else a.reportIndicesOpened = false;

										if (a.reportIndicesOpened)
										{
											for (int n = 0; n < a.reportIndices.Count; n++) 
											{
												GUILayout.BeginHorizontal ();
												{
													GUILayout.Label (n.ToString(), GUILayout.Width (20));
													int reportIndex = a.reportIndices [n];
													EcoGUI.IntField (null, ref reportIndex, 0, 60);
													a.reportIndices [n] = reportIndex;

													// Delete
													if (n > 0 && GUILayout.Button ("-")) 
													{
														a.reportIndices.RemoveAt (n);
														break;
													}
												}
												GUILayout.EndHorizontal ();
											}
										}
									}
									GUILayout.EndHorizontal ();
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
				EcoGUI.FoldoutEditableName (ref question.body, ref question.opened);
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
				if (GUILayout.Button ("˄", GUILayout.Width (20))) {
					q.questions.Remove (question);
					q.questions.Insert (index - 1, question);
				}

				// Down
				GUI.enabled = (index < q.questions.Count - 1);
				if (GUILayout.Button ("˅", GUILayout.Width (20))) {
					q.questions.Remove (question);
					q.questions.Insert (index + 1, question);
				}
				GUI.enabled = true;

				// Remove
				GUILayout.Space (5);
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
				EcoGUI.FoldoutEditableName (ref a.body, ref a.opened);
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

		private void RenderReports (int mx, int my)
		{
			GUILayout.Label ("Reports is still under construction...");	
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
