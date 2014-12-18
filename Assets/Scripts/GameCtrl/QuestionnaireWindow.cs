using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class QuestionnaireWindow : ReportBaseWindow
{
	private Questionnaire questionnaire;
	private Progression.QuestionnaireState questionnaireState;

	private Vector2 messageScrollPos;
	private Answer selectedAnswer;
	private System.Action <Answer, bool> onMessageContinueClick;

	private string messageTitle;
	private string message;
	private int messageXOffset;
	private int messageYOffset;
	private int messageLines;
	private int messageTitleLines;
	private bool messageIsDragging;
	private Vector2 messageMouseDragPosition;

	public QuestionnaireWindow (Questionnaire questionnaire, System.Action onFinished) : base (onFinished)
	{
		this.questionnaire = questionnaire;

		questionnaireState = new Progression.QuestionnaireState (this.questionnaire.id);
		EditorCtrl.self.scene.progression.questionnaireStates.Add (questionnaireState);

		messageXOffset = -1;
		messageYOffset = -1;
		message = null;
	}

	public override void Render ()
	{
		base.Render ();

		// Check for introduction
		if (this.questionnaire.useIntroduction && !this.questionnaire.introShown)
		{
			// We'll (mis)use the message for this for now
			if (this.message == null)
			{
				this.message = questionnaire.introduction;
				this.messageTitle = "Introduction";
				this.messageLines = this.message.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
				this.messageTitleLines = 1;
				this.onMessageContinueClick = delegate (Answer arg1, bool arg2) 
				{
					this.message = null;
					this.questionnaire.introShown = true;
				};
			}

			RenderMessage ();
			return;
		}

		// Check for question
		if (questionnaire.currentQuestionIndex < questionnaire.questions.Count)
		{
			Question question = questionnaire.questions [questionnaire.currentQuestionIndex];

			GUI.enabled = (this.message == null);

			if (question is MPCQuestion) 
			{
				RenderMPCQuestion (question as MPCQuestion);
			}
			else if (question is OpenQuestion) 
			{
				RenderOpenQuestion (question as OpenQuestion);
			}

			GUI.enabled = true;

			RenderMessage ();
			return;
		}

		// Check for conclusion
		if (this.questionnaire.useConclusion && !this.questionnaire.conclusionShown)
		{
			// We'll (mis)use the message for this for now
			if (this.message == null)
			{
				this.message = questionnaire.conclusion;
				this.messageTitle = "Final Remark";
				this.messageLines = this.message.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
				this.messageTitleLines = 1;
				this.onMessageContinueClick = delegate (Answer arg1, bool arg2) 
				{
					this.message = null;
					this.questionnaire.conclusionShown = true;

					// Check if we can go to the results page
					CheckForResultsPage ();
				};
			}

			RenderMessage ();
			return;
		}

		try {
			if (this.questionnaire.useResultsPage) {
				// Then the results
				RenderResults ();
			}
		} catch { }
	}

	#region Questionnaire

	private void RenderMPCQuestion (MPCQuestion question)
	{
		Questionnaire q = questionnaire;

		RenderQuestionStart (question);
		{
			GUILayout.Space (1f);
			GUILayout.Label (question.body, headerDark, GUILayout.Width (width), defaultOption);
			//GUILayout.Space (1);
			GUILayout.Label ("Choose your answer:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);

			// Answers
			GUILayout.BeginVertical ( GUILayout.Width (width));
			{
				//GUILayout.Space (5);
				foreach (MPCQuestion.MPCAnswer a in question.answers)
				{
					if (GUILayout.Button (a.body, button, GUILayout.Width (width), defaultOption))
					{
						if (selectedAnswer == null)
						{
							HandleMPCAnswer (a, true);
						}
					}
					GUILayout.Space (1);
				}
				//GUILayout.Space (4);
			}
			GUILayout.EndVertical ();
		}
		RenderQuestionEnd (question);
	}
	private void HandleMPCAnswer (Answer answer, bool checkForFeedback)
	{
		MPCQuestion.MPCAnswer a = (MPCQuestion.MPCAnswer)answer;
		if (checkForFeedback && a.useFeedback)
		{
			this.message = a.feedback;
			this.messageTitle = "Selected answer: " + a.body;
			this.selectedAnswer = a;
			this.onMessageContinueClick = HandleMPCAnswer;
			this.messageLines = this.message.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
			this.messageTitleLines = this.messageTitle.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
			return;
		}

		if (!a.allowRetry && a.startFromBeginning) 
		{
			// Start over
			this.questionnaire.currentQuestionIndex = 0;
			this.questionnaireState.Reset ();
		}
		else if (a.allowRetry)
		{
			// Just remove the feedback
		}
		else 
		{
			// Create question state
			Question q = this.questionnaire.questions [this.questionnaire.currentQuestionIndex];

			Progression.QuestionnaireState.QuestionState questionState = questionnaireState.GetQuestionState (this.questionnaire.currentQuestionIndex);
			questionState.questionName = q.body;
			questionState.questionAnswer = a.body;

			questionState.moneyGained = 0;
			if (this.questionnaire.useBudget) {
				questionState.moneyGained = a.moneyGained;
			}
			questionState.score = 0;
			if (this.questionnaire.useRequiredScore) {
				questionState.score = a.score;
			}

			// Next question
			NextQuestion ();
		}

		this.selectedAnswer = null;
		this.message = null;
		this.scrollPosition = Vector2.zero;
		this.messageScrollPos = Vector2.zero;
	}

	private void RenderOpenQuestion (OpenQuestion question)
	{
		Questionnaire q = questionnaire;

		RenderQuestionStart (question);
		{
			GUILayout.Space (1);
			OpenQuestion.OpenAnswer a = question.answers[0] as OpenQuestion.OpenAnswer;
			GUILayout.Label (question.body, headerDark, GUILayout.Width (width), defaultOption);
			//GUILayout.Space (1);
			GUILayout.Label ("Write your answer:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);
			if (a.useMaxChars)
				a.body = GUILayout.TextArea (a.body, a.maxChars, textArea, GUILayout.Width (width), defaultOption);
			else 
				a.body = GUILayout.TextArea (a.body, textArea, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);

			GUILayout.BeginHorizontal ();
			{
				string label = (a.useMaxChars) ? string.Format("<size=10>Characters {0}/{1}</size>", a.body.Length, a.maxChars) : "";
				GUILayout.Label (label, headerLight, GUILayout.Width (width - 52), GUILayout.Height (30), defaultOption);
				GUILayout.Space (1);
				if (GUILayout.Button ("Done", button, GUILayout.Width (51), defaultOption))
				{
					if (this.selectedAnswer == null)
					{
						HandleOpenAnswer (question.answers[0], true);
					}
				}
			}
			GUILayout.EndHorizontal ();
		}
		RenderQuestionEnd (question);
	}
	private void HandleOpenAnswer (Answer a, bool checkForFeedback)
	{
		// Check if the feedback was "can't be empty" and we clicked "Continue"
		if (!checkForFeedback && a.body.Length == 0)
		{
			this.message = null;
			return;
		}

		// Check for empty body
		if (a.body.Length == 0)
		{
			this.message = "Your answer can't be empty.";
			this.selectedAnswer = a;
			this.messageTitle = "Oops...";
			this.onMessageContinueClick = HandleOpenAnswer;
			this.messageLines = 1;
			this.messageTitleLines = 1;
			return;
		}

		// Check for feedback
		if (checkForFeedback && a.useFeedback)
		{
			this.message = a.feedback;
			this.selectedAnswer = a;
			this.messageTitle = null;
			this.onMessageContinueClick = HandleOpenAnswer;
			this.messageLines = this.message.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
			this.messageTitleLines = 0;
			return;
		}

		// Create question state
		Question q = this.questionnaire.questions [this.questionnaire.currentQuestionIndex];
		Progression.QuestionnaireState qs = EditorCtrl.self.scene.progression.GetQuestionnaireState (this.questionnaire.id);
		
		Progression.QuestionnaireState.QuestionState questionState = qs.GetQuestionState (this.questionnaire.currentQuestionIndex);
		questionState.questionName = q.body;
		questionState.questionAnswer = a.body;

		// Next question
		NextQuestion ();

		this.selectedAnswer = null;
		this.message = null;
		this.scrollPosition = Vector2.zero;
		this.messageScrollPos = Vector2.zero;
	}

	private void RenderQuestionStart (Question question)
	{
		Questionnaire q = questionnaire;
		Rect areaRect = new Rect (left, top, width + 20, height);
		GUILayout.BeginArea (areaRect);
		CameraControl.MouseOverGUI |= areaRect.Contains (Input.mousePosition);
		scrollPosition = GUILayout.BeginScrollView (scrollPosition);

		// Header
		if (q.showHeader)
		{
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("Questionnaire: " + q.name, headerDark, GUILayout.Width (width - 40), defaultOption);
				GUILayout.Space (1);
				GUILayout.Label (string.Format ("{0}/{1}", (q.currentQuestionIndex + 1), q.questions.Count), headerDark, GUILayout.Width (39), defaultOption);
			}
			GUILayout.EndHorizontal ();
		}
		//GUILayout.Space (1);
	}
	private void RenderQuestionEnd (Question question)
	{
		GUILayout.EndScrollView ();
		GUILayout.EndArea ();	
	}

	private void NextQuestion ()
	{
		// Up the current index
		this.questionnaire.currentQuestionIndex++;

		// Check if we can go to the results page
		CheckForResultsPage ();
	}

	private void CheckForResultsPage ()
	{
		// Exception: if we don't show the results page and we've
		// answered our last question we fire the onFinished event
		// for some reason we can do it instead of RenderResultsPage...
		if (this.questionnaire.currentQuestionIndex >= this.questionnaire.questions.Count) 
		{
			// Check if we have a conclusion and if it's shown already
			if (this.questionnaire.useConclusion && !this.questionnaire.conclusionShown)
				return;
			
			// Check if we have a results page
			if (this.questionnaire.useResultsPage)
				return;
			
			// Fire onFinished event
			if (this.onFinished != null)
				onFinished ();
		}
	}

	#endregion

	#region Results

	private void RenderResults ()
	{
		Questionnaire q = questionnaire;
		Progression.QuestionnaireState qs = questionnaireState;//EditorCtrl.self.scene.progression.GetQuestionnaireState (q.id);

		Rect areaRect = new Rect (left, top, width + 20, height);
		GUILayout.BeginArea (areaRect); 
		{
			CameraControl.MouseOverGUI |= areaRect.Contains (Input.mousePosition);

			// Check if we passed
			bool passed = true;
			if (q.useRequiredScore) {
				passed = (qs.totalScore >= q.requiredScore);
			}
			
			GUILayout.Label ("Questionnaire results:\n" + q.name, headerDark, GUILayout.Width (width), defaultOption);
			GUILayout.Space (5);
			
			this.scrollPosition = GUILayout.BeginScrollView (this.scrollPosition);
			{
				// Questions
				int qidx = 1;
				foreach (Progression.QuestionnaireState.QuestionState qState in qs.questionStates)
				{
					EcoGUI.SplitLabel (qidx + ". " + qState.questionName, headerDark, GUILayout.Width (width));
					//GUILayout.Space (1);

					//GUILayout.Label ("Your answer:", headerLight, GUILayout.Width (width));
					EcoGUI.SplitLabel (qState.questionAnswer, textArea, GUILayout.Width (width));
					GUILayout.Space (5);
					qidx++;
				}
				
				// Money gained
				if (passed && q.useBudget)
				{
					GUILayout.Label ("Money earned: " + qs.totalMoneyEarned, headerDark, GUILayout.Width (width), defaultOption);
					//GUILayout.Space (1);
					if (q.useBudgetFeedback) 
					{
						EcoGUI.SplitLabel (q.budgetFeedback, headerLight, GUILayout.Width (width));
					}
					GUILayout.Space (5);
				}
			}
			GUILayout.EndScrollView ();
			
			// Score
			if (q.useRequiredScore)
			{
				GUILayout.Label ("Total score: " + qs.totalScore + "\nScore required to pass: " + q.requiredScore, headerDark, GUILayout.Width (width), defaultOption);
				//GUILayout.Space (1);
				
				if (q.useReqScoreFeedback) {
					EcoGUI.SplitLabel (q.reqScoreFeedback, headerLight, GUILayout.Width (width), defaultOption);
					GUILayout.Space (1);
				}
				
				GUILayout.Label ("Result: " + ((passed) ? "Passed" : "Failed"), headerDark, GUILayout.Width (width), defaultOption);
			}

			// Continue button
			if (passed) 
			{
				if (q.usePassedFeedback)
				{
					EcoGUI.SplitLabel (q.passedFeedback, headerLight, GUILayout.Width (width), defaultOption);
				}
				GUILayout.Space (1);
				
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", headerLight, GUILayout.Width (width - 162), defaultOption);
					GUILayout.Space (1);
					RenderSaveButton (qs, passed);
					GUILayout.Space (1);
					if (GUILayout.Button ("Continue", button, GUILayout.Width (80), defaultOption)) 
					{
						if (onFinished != null) {
							onFinished ();
							onFinished = null;
						}
					}
				}
				GUILayout.EndHorizontal ();
			} 
			else 
			{
				if (q.useFailedFeedback)
				{
					EcoGUI.SplitLabel (q.failedFeedback, headerLight, GUILayout.Width (width), defaultOption);
				}
				GUILayout.Space (1);
				
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", headerLight, GUILayout.Width (width - 162), defaultOption);
					GUILayout.Space (1);
					RenderSaveButton (qs, passed);
					GUILayout.Space (1);

					string btnLabel = (q.startOverOnFailed) ? "Retry" : "Continue";
					if (GUILayout.Button (btnLabel, button, GUILayout.Width (80), defaultOption)) 
					{
						if (q.startOverOnFailed) 
						{
							q.currentQuestionIndex = 0;
							this.questionnaireState.Reset ();
						} 
						else 
						{
							if (onFinished != null) {
								onFinished ();
								onFinished = null;
							}
						}
					}
				}
				GUILayout.EndHorizontal ();
			}
		}
		GUILayout.EndArea ();
	}
	private void RenderSaveButton (Progression.QuestionnaireState qs, bool passed)
	{
		if (GUILayout.Button ("Save", button, GUILayout.Width (80), defaultOption)) 
		{
			GameControl.self.StartCoroutine (SaveFileDialog.Show ("questionnaire_" + this.questionnaire.id, "txt files (*.txt)|*.txt", delegate(bool ok, string url) 
			{
				if (!ok) return;

				// Create new file
				System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding ();
				FileStream fs = File.Create (url);
				System.Text.StringBuilder sb = new System.Text.StringBuilder ();
				
				Scene scene = EditorCtrl.self.scene;
				sb.AppendFormat ("Name: {0} {1}\n", scene.playerInfo.firstName, scene.playerInfo.familyName);
				sb.AppendFormat ("Date: {0}\n", System.DateTime.Today.ToString ("dd\\/MM\\/yyyy"));
				sb.AppendLine ();

				sb.AppendFormat ("Results Questionnaire {0}:\n", this.questionnaire.id);
				sb.AppendFormat ("{0}\n", this.questionnaire.name);
				sb.AppendLine ();
				
				int qIdx = 1;
				foreach (Progression.QuestionnaireState.QuestionState qState in qs.questionStates)
				{
					sb.AppendFormat ("{0}. {1}:\n", qIdx.ToString (), qState.questionName);
					sb.AppendFormat ("{0}\n", qState.questionAnswer);
					sb.AppendLine ();
					qIdx++;
				}
				
				if (passed && this.questionnaire.useBudget)
				{
					sb.AppendFormat ("Money earned: {0}\n", qs.totalMoneyEarned);
					sb.AppendLine ();
				}
				
				if (this.questionnaire.useRequiredScore)
				{
					sb.AppendFormat ("Total score: {0}\n", qs.totalScore);
					sb.AppendFormat ("Required score: {0}\n", this.questionnaire.requiredScore);
					sb.AppendFormat ("Result: {0}\n", ((passed) ? "Passed" : "Failed"));
					sb.AppendLine ();
				}
				
				// Stringify and save
				string txt = sb.ToString ();
				fs.Write (enc.GetBytes (txt), 0, enc.GetByteCount (txt));
				
				// Close and dispose the stream
				fs.Close ();
				fs.Dispose ();
				fs = null;
			}));
		}
	}

	#endregion

	#region Message

	private void RenderMessage ()
	{
		if (string.IsNullOrEmpty (this.message)) return;

		float editorWidth = 0f;
		if (EditorCtrl.self.isOpen) {
			editorWidth = 400;
		}
		width = Screen.width * 0.5f;
		height = (messageLines + messageTitleLines + 1) * 60f;
		height = Mathf.Clamp (height, 0f, Screen.height * 0.75f);

		// Check x y offsets
		if (messageXOffset == -1) {
			messageXOffset = (int)(((Screen.width - width) * 0.5f) + editorWidth);
		}
		if (messageYOffset == -1) {
			messageYOffset = (int)((Screen.height * 0.5f) - (height * 0.5f));
		}

		//left = ((Screen.width - width) * 0.5f) + editorWidth;
		//top = (Screen.height * 0.5f) - (height * 0.5f);

		left = messageXOffset;
		top = messageYOffset;

		Rect areaRect = new Rect (left, top, width, height);
		GUILayout.BeginArea (areaRect);
		{
			CameraControl.MouseOverGUI |= areaRect.Contains (Input.mousePosition);
			GUILayout.Label (messageTitle ?? "", headerDark, GUILayout.Width (width), defaultOption);

			Vector2 mousePos = Input.mousePosition;
			Vector2 guiMousePos = new Vector2 (mousePos.x, Screen.height - mousePos.y);

			// Check for drag
			if (Event.current.type == EventType.MouseUp) 
			{
				// Cancel drag
				messageIsDragging = false;
			} 
			else if (messageIsDragging) 
			{
				// Do drag
				messageXOffset += (int)(guiMousePos.x - messageMouseDragPosition.x);
				messageYOffset += (int)(guiMousePos.y - messageMouseDragPosition.y);
				messageMouseDragPosition = guiMousePos;
			}
			else if (Event.current.type == EventType.MouseDown)
			{
				// Start drag
				if (GUILayoutUtility.GetLastRect ().Contains (Event.current.mousePosition)) 
				{
					messageIsDragging = true;
					messageMouseDragPosition = guiMousePos;
					Event.current.Use ();

					this.SetWindowOnTop ();
				}
			}
			GUILayout.Space (1);
			this.messageScrollPos = GUILayout.BeginScrollView (this.messageScrollPos);
			{
				GUILayout.Label (this.message, textArea, GUILayout.Width (width), GUILayout.ExpandHeight (true), defaultOption);
				GUILayout.Space (1);
			}
			GUILayout.EndScrollView ();
			GUILayout.Space (1);
			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label ("", headerLight, GUILayout.Width (width - 90), defaultOption);
				if (GUILayout.Button ("Continue", button, GUILayout.Width (90), defaultOption))
				{
					this.message = null;
					if (onMessageContinueClick != null)
						onMessageContinueClick (this.selectedAnswer, false);
				}
			}
			GUILayout.EndHorizontal ();
		}
		GUILayout.EndArea ();
	}

	#endregion

	public override void Dispose ()
	{
		base.Dispose ();

		this.questionnaire = null;
		this.selectedAnswer = null;
		this.onMessageContinueClick = null;
	}
}

