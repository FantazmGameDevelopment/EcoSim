using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;

public class QuestionnaireWindow
{
	private Questionnaire questionnaire;
	private System.Action onFinished;

	private Vector2 scrollPosition;
	private Vector2 messageScrollPos;
	private Answer selectedAnswer;
	private System.Action <Answer, bool> onMessageContinueClick;
	private string messageTitle;
	private string message;
	private int messageLines;
	private int messageTitleLines;

	private float width;
	private float height;
	private float left;
	private float top;
	private GUILayoutOption defaultOption;

	protected static GUIStyle black;
	protected static GUIStyle white;
	protected static GUIStyle headerDark;
	protected static GUIStyle titleNoText;
	protected static GUIStyle headerLight;
	protected static GUIStyle button;
	protected static GUIStyle formatted;

	public static void Reset ()
	{
		if (black != null) return; // already did stuff below
		black = GameControl.self.skin.FindStyle ("BGBlack");
		white = GameControl.self.skin.FindStyle ("BGWhite");
		headerDark = GameControl.self.skin.FindStyle ("ArialB16-75");
		headerLight = GameControl.self.skin.FindStyle ("ArialB16-50");
		button = GameControl.self.skin.FindStyle ("Arial16-75");
		formatted = GameControl.self.skin.FindStyle ("Arial16-50-formatted");
	}

	public QuestionnaireWindow (Questionnaire questionnaire, System.Action onFinished)
	{
		this.questionnaire = questionnaire;
		this.onFinished = onFinished;
		Reset ();
	}

	public void Render ()
	{
		float editorWidth = 0f;
		if (EditorCtrl.self.isOpen) {
			editorWidth = 400;
		}
		width = (Screen.width - editorWidth) * 0.65f;
		height = Screen.height * 0.75f;
		left = ((Screen.width - width) * 0.5f) + editorWidth;
		top = (Screen.height - height) * 0.5f;
		defaultOption = GUILayout.MinHeight (28f);//GUILayout.ExpandHeight (true);

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
				this.messageTitle = "Conclusion";
				this.messageLines = this.message.Split (new string[] { "&#xA;" }, System.StringSplitOptions.RemoveEmptyEntries).Length;
				this.messageTitleLines = 1;
				this.onMessageContinueClick = delegate (Answer arg1, bool arg2) 
				{
					this.message = null;
					this.questionnaire.conclusionShown = true;
				};
			}

			RenderMessage ();
			return;
		}

		// Then the results
		try {
			RenderResults ();
		} catch { }
	}

	private void RenderMPCQuestion (MPCQuestion question)
	{
		Questionnaire q = questionnaire;

		RenderQuestionStart (question);
		{
			GUILayout.Label (question.body, headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);
			GUILayout.Label ("Choose your answer:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);

			// Answers
			GUILayout.BeginVertical (headerDark, GUILayout.Width (width));
			{
				GUILayout.Space (5);
				int idx = 0;
				foreach (MPCQuestion.MPCAnswer a in question.answers)
				{
					if (GUILayout.Button (a.body, button, GUILayout.Width (width - 10), defaultOption))
					{
						if (selectedAnswer == null)
						{
							HandleMPCAnswer (a, true);
						}
					}
					GUILayout.Space (1);
				}
				GUILayout.Space (4);
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

		if (!a.allowRetry && a.startFromBeginning) {
			this.questionnaire.currentQuestionIndex = 0;
		}
		else 
		{
			// Create question state
			Question q = this.questionnaire.questions [this.questionnaire.currentQuestionIndex];
			Progression.QuestionnaireState qs = EditorCtrl.self.scene.progression.GetQuestionnaireState (this.questionnaire.id);

			Progression.QuestionnaireState.QuestionState questionState = new Progression.QuestionnaireState.QuestionState ();
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

			qs.questionStates.Add (questionState);

			// Nest question
			this.questionnaire.currentQuestionIndex++;
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
			OpenQuestion.OpenAnswer a = question.answers[0] as OpenQuestion.OpenAnswer;
			GUILayout.Label (question.body, headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);
			GUILayout.Label ("Write your answer:", headerLight, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);
			if (a.useMaxChars)
				a.body = GUILayout.TextArea (a.body, a.maxChars, headerDark, GUILayout.Width (width), defaultOption);
			else 
				a.body = GUILayout.TextArea (a.body, headerDark, GUILayout.Width (width), defaultOption);
			GUILayout.Space (1);

			GUILayout.BeginHorizontal ();
			{
				GUILayout.Label (string.Format("Characters {0}/{1}", a.body.Length, a.maxChars), headerLight, GUILayout.Width (width - 52), defaultOption);
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
			this.messageTitle = null;
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
		
		Progression.QuestionnaireState.QuestionState questionState = new Progression.QuestionnaireState.QuestionState ();
		questionState.questionName = q.body;
		questionState.questionAnswer = a.body;
		qs.questionStates.Add (questionState);

		// Next question
		this.questionnaire.currentQuestionIndex++;

		this.selectedAnswer = null;
		this.message = null;
		this.scrollPosition = Vector2.zero;
		this.messageScrollPos = Vector2.zero;
	}

	private void RenderQuestionStart (Question question)
	{
		Questionnaire q = questionnaire;
		GUILayout.BeginArea (new Rect (left, top, width + 30, height));
		scrollPosition = GUILayout.BeginScrollView (scrollPosition); // TODO: Scrollbar Skin

		// Header
		GUILayout.BeginHorizontal ();
		{
			GUILayout.Label ("Questionnaire: " + q.name, headerDark, GUILayout.Width (width - 40), defaultOption);
			GUILayout.Space (1);
			GUILayout.Label (string.Format ("{0}/{1}", (q.currentQuestionIndex + 1), q.questions.Count), headerLight, GUILayout.Width (39), defaultOption);
		}
		GUILayout.EndHorizontal ();
		GUILayout.Space (1);
	}
	private void RenderQuestionEnd (Question question)
	{
		GUILayout.EndScrollView ();
		GUILayout.EndArea ();	
	}

	private void RenderResults ()
	{
		Questionnaire q = questionnaire;
		Progression.QuestionnaireState qs = EditorCtrl.self.scene.progression.GetQuestionnaireState (q.id);
		
		GUILayout.BeginArea (new Rect (left, top, width + 20, height)); // TODO: Scroll style
		{
			// Check if we passed
			bool passed = true;
			if (q.useRequiredScore) {
				passed = (qs.totalScore >= q.requiredScore);
			}
			
			// Header
			GUILayout.Label ("Questionnaire results:\n" + q.name, headerDark, GUILayout.Width (width), defaultOption);
			GUILayout.Space (5);
			
			this.scrollPosition = GUILayout.BeginScrollView (this.scrollPosition);
			{
				// Questions
				int qidx = 0;
				foreach (Progression.QuestionnaireState.QuestionState qState in qs.questionStates)
				{
					string[] split =  qState.questionName.Split (new string[] { "\n","\r" }, System.StringSplitOptions.None);
					split[0] = (++qidx) + ". " + split[0];
					foreach (string s in split) {
						GUILayout.Label ((s.Length > 0) ? s : " ", headerDark, GUILayout.Width (width));
					}
					GUILayout.Space (1);
					
					GUILayout.Label ("Your answer:", headerLight, GUILayout.Width (width));
					split = qState.questionAnswer.Split (new string[] { "\n","\r" }, System.StringSplitOptions.None);
					foreach (string s in split) {
						GUILayout.Label ((s.Length > 0) ? s : " ", headerLight, GUILayout.Width (width));
					}
					GUILayout.Space (5);
				}
				
				// Money gained
				if (passed && q.useBudget)
				{
					GUILayout.Label ("Money earned: " + qs.totalScore, headerDark, GUILayout.Width (width), defaultOption);
					GUILayout.Space (1);
					if (q.useBudgetFeedback) 
					{
						GUILayout.Label (q.budgetFeedback, headerLight, GUILayout.Width (width), defaultOption);
					}
					GUILayout.Space (5);
				}
			}
			GUILayout.EndScrollView ();
			
			// Score
			if (q.useRequiredScore)
			{
				GUILayout.Label ("Total score: " + qs.totalScore + "\nScore required to pass:" + q.requiredScore, headerDark, GUILayout.Width (width), defaultOption);
				GUILayout.Space (1);
				
				if (q.useReqScoreFeedback) {
					GUILayout.Label (q.reqScoreFeedback, headerLight, GUILayout.Width (width), defaultOption);
					GUILayout.Space (1);
				}
				
				GUILayout.Label ("Result: " + ((passed) ? "Passed" : "Failed"), headerLight, GUILayout.Width (width), defaultOption);
				GUILayout.Space (1);
			}
			
			// Continue button
			if (passed) 
			{
				if (q.usePassedFeedback)
				{
					GUILayout.Label (q.passedFeedback, headerLight, GUILayout.Width (width), defaultOption);
					GUILayout.Space (1);
				}
				
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", headerLight, GUILayout.Width (width - 80), defaultOption);
					if (GUILayout.Button ("Continue", headerDark, GUILayout.Width (80), defaultOption)) 
					{
						if (onFinished != null)
							onFinished ();
					}
				}
				GUILayout.EndHorizontal ();
			} 
			else 
			{
				if (q.useFailedFeedback)
				{
					GUILayout.Label (q.failedFeedback, headerLight, GUILayout.Width (width), defaultOption);
					GUILayout.Space (1);
				}
				
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", headerLight, GUILayout.Width (width - 182), defaultOption);
					GUILayout.Space (1);
					if (GUILayout.Button ("Save to .txt", button, GUILayout.Width (100), defaultOption)) 
					{
						// Save to .txt
						System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog ();
						sfd.FileName = "questionnaire_" + this.questionnaire.id;
						sfd.Filter = "txt files (*.txt)|*.txt";
						System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding ();

						if (sfd.ShowDialog () == System.Windows.Forms.DialogResult.OK)
						{
							// Create new file
							FileStream fs = File.Create (sfd.FileName);
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
							
							// Close and dipose the stream
							fs.Close ();
							fs.Dispose ();
							fs = null;
						}
					}
					GUILayout.Space (1);

					string btnLabel = (q.startOverOnFailed) ? "Retry" : "Continue";
					if (GUILayout.Button (btnLabel, button, GUILayout.Width (80), defaultOption)) 
					{
						if (q.startOverOnFailed) 
						{
							q.currentQuestionIndex = 0;
						} 
						else 
						{
							if (onFinished != null)
								onFinished ();
						}
					}
				}
				GUILayout.EndHorizontal ();
			}
		}
		GUILayout.EndArea ();
	}

	private void RenderMessage ()
	{
		if (string.IsNullOrEmpty (this.message)) return;

		float editorWidth = 0f;
		if (EditorCtrl.self.isOpen) {
			editorWidth = 400;
		}
		width = Screen.width * 0.5f;
		height = (messageLines + messageTitleLines + 1) * 40f;
		height = Mathf.Clamp (height, 0f, Screen.height * 0.75f);
		left = left = ((Screen.width - width) * 0.5f) + editorWidth;
		top = (Screen.height * 0.5f) - (height * 0.5f);
		
		GUILayout.BeginArea (new Rect (left, top, width, height));
		{
			GUILayout.BeginVertical (headerLight, GUILayout.Width (width + 10f));
			{
				GUILayout.Space (5f);
				if (this.messageTitle != null) {
					GUILayout.Label (messageTitle, headerLight, GUILayout.Width (width- 10f), defaultOption);
					GUILayout.Space (1);
				}
				this.messageScrollPos = GUILayout.BeginScrollView (this.messageScrollPos);
				{
					GUILayout.Label (this.message, headerLight, GUILayout.Width (width - 10f), GUILayout.ExpandHeight (true), defaultOption);
					GUILayout.Space (1);
				}
				GUILayout.EndScrollView ();
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", headerLight, GUILayout.Width (width - 90), defaultOption);
					if (GUILayout.Button ("Continue", button, GUILayout.Width (80), defaultOption))
					{
						this.message = null;
						if (onMessageContinueClick != null)
							onMessageContinueClick (this.selectedAnswer, false);
					}
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5f);
			}
			GUILayout.EndVertical ();
		}
		GUILayout.EndArea ();
	}

	public void Dispose ()
	{
		this.questionnaire = null;
		this.onFinished = null;
		this.selectedAnswer = null;
		this.onMessageContinueClick = null;
		this.defaultOption = null;
	}
}

