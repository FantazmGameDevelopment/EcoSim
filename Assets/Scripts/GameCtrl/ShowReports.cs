using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneData;

/**
 * Monobehaviour class that manages a queue of questionnaires and repors to be shown
 * to the player. While showing a questionnaire or report the UI will be blocked.
 */
public class ShowReports : MonoBehaviour
{
	private static volatile bool hasQueue = false;
	private bool isShowing = false;

	private QuestionnaireWindow questionnaireWindow;
	private Questionnaire currentQuestionnaire;

	private ReportWindow reportWindow;
	private Report currentReport;

	public static void NotifyQueueChange () {
		hasQueue = true;
	}

	void OnGUI () 
	{
		// TODO: Exception time; we should only come AFTER showArticles.cs
		if (ShowArticles.HasUnreadMessages) return;

		GameControl ctrl = GameControl.self;
		Scene scene = ctrl.scene;

		if ((scene != null) && hasQueue && (!isShowing))
		{
			if (ctrl.hideToolBar || ctrl.hideSuccessionButton) {
				// toolbar/succession button is hidden, apparently we're busy with something, wait till 
				// we're ready for showing article...
				return;
			}

			isShowing = true;
			ctrl.hideToolBar = true;
			ctrl.hideSuccessionButton = true;

			object inQueue = scene.reports.CurrentInQueue ();
			if (inQueue is Questionnaire) 
			{
				this.currentReport = null;
				this.currentQuestionnaire = (Questionnaire)inQueue;
			}
			else if (inQueue is Report) 
			{
				// TODO: Report
				this.currentQuestionnaire = null;
				this.currentReport = (Report)inQueue;
			}
		}
		else if (isShowing)
		{
			GUI.depth = 100;
			CameraControl.MouseOverGUI = true;

			if (this.currentQuestionnaire != null) 
			{
				RenderQuestionnaire ();
			} 
			else if (this.currentReport != null)
			{
				RenderReport ();
			}
			else 
			{
				isShowing = false;
				ctrl.hideToolBar = false;
				ctrl.hideSuccessionButton = false;

				hasQueue = scene.reports.ToNextInQueue ();
			}
		}
	}

	void RenderQuestionnaire ()
	{
		if (this.questionnaireWindow == null) {
			this.questionnaireWindow = new QuestionnaireWindow (currentQuestionnaire, delegate() 
			{
				if (this.currentQuestionnaire.useBudget) {
					int totalMoneyEarned = EditorCtrl.self.scene.progression.GetQuestionnaireState (this.currentQuestionnaire.id).totalMoneyEarned;
					EditorCtrl.self.scene.progression.budget += totalMoneyEarned;
				}

				this.currentQuestionnaire = null;
				this.questionnaireWindow.Dispose ();
				this.questionnaireWindow = null;
			});
		}
		this.questionnaireWindow.Render ();
	}

	void RenderReport ()
	{
		if (this.reportWindow == null)
		{
			// TODO:
		}
	}

	/*
	 * void OnGUI () {
		GameControl ctrl = GameControl.self;
		Scene scene = ctrl.scene;
		if ((scene != null) && hasUnreadMessages && (!isShowing)) {
			if (ctrl.hideToolBar || ctrl.hideSuccessionButton) {
				// toolbar/succession button is hidden, apparently we're busy with something, wait till 
				// we're ready for showing article...
				return;
			}
			RenderFontToTexture.self.RenderNewsArticle (scene.progression.CurrentMessage ().text, scene, articleTex, false);
			isShowing = true;
			yPos = (float) Screen.height;
			targetYPos = Screen.height - articleTex.height;// Mathf.Min (Screen.height - articleTex.height, 200);
			ctrl.hideToolBar = true;
			ctrl.hideSuccessionButton = true;
		}
		else if (isShowing) {
			yPos = Mathf.Max (targetYPos, yPos - 1200f * Time.deltaTime);
			int sWidth = Screen.width;
			int xOffset = 0;
			if (EditorCtrl.self.isOpen) {
				sWidth -= 400;
				xOffset += 400;
			}
			GUI.depth = 100;
			CameraControl.MouseOverGUI = true;
			SimpleGUI.Label (new Rect ((sWidth - articleTex.width ) / 2, yPos, articleTex.width, articleTex.height), articleTex, GUIStyle.none);
			if (Event.current.type == EventType.MouseDown) {
				Event.current.Use ();
				ctrl.hideToolBar = false;
				ctrl.hideSuccessionButton = false;
				isShowing = false;
				hasUnreadMessages = ctrl.scene.progression.ToNextMessage ();
			}
		}
	}
	 */ 
}

