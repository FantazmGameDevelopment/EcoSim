using UnityEngine;
using System.Collections;
using System.Globalization;
using System.Threading;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Action;
using Ecosim.GameCtrl.GameButtons;

public class GameControl : MonoBehaviour
{
	public const float GAME_BUTTON_TIMEOUT_DELAY = 1.25f;
	
	public GUISkin skin;
	public Texture2D budgetIcon;
	public Texture2D expenseIcon;
	public Texture2D yearIcon;
	public Texture2D successionIcon;
	public Texture2D successionIconH;
	public Texture2D leftIcon;
	public Texture2D rightIcon;
	public Texture2D gameOver;
	public static GameControl self;
	public Scene scene;
	public bool showHelpTips = false;
	private GUIStyle icon50Style;
	private GUIStyle icon50TextStyle;
	private GUIStyle whiteBgStyle;
	private GUIStyle blackBgStyle;
	private GUIStyle helpTipStyle;
	private GUIStyle extraHelpStyle;
	public GameButton[] buttons;
	public bool hideToolBar = false; // hide tools when true
	public bool hideGameActions = false; // hide tools when true
	public bool hideSuccessionButton = false; // hide succession button when true
	public bool isProcessing = false; // hide complete UI when true
	private string budgetText = "";
	private string expenseText = "";
	private string yearText = "";
	private Rect budgetIconR;
	private Rect expenseIconR;
	private Rect yearIconR;
	private Rect budgetR;
	private Rect expenseR;
	private Rect yearR;
	private Rect successionR;
	private GameButton activeButton = null;
	private float activeButtonTimeout = 0f;
	private float helpTimeout = 0f;
	private string extraHelp;
	private float extraHelpTimeout = 0f;
	private Rect extraHelpRect;
	
	void Awake ()
	{
		self = this;
		enabled = false;
		
		foreach (GameButton b in buttons) {
			b.hdlr = GameButtonHandler.GetHandlerByName (b.code);
		}
	}
	
	void Start ()
	{
		icon50Style = skin.FindStyle ("50");
		icon50TextStyle = skin.FindStyle ("ArialB16-50");
		whiteBgStyle = skin.FindStyle ("BGWhite");
		blackBgStyle = skin.FindStyle ("BGBlack");
		helpTipStyle = skin.FindStyle ("ToolHelp");
		extraHelpStyle = skin.FindStyle ("ExtraHelp");
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	public static void ExtraHelp (string txt) {
		self.extraHelp = txt;
		self.extraHelpTimeout = Time.timeSinceLevelLoad + 10f;
		self.extraHelpRect.height = self.extraHelpStyle.CalcHeight (new GUIContent (txt), self.extraHelpRect.width) + 4;
	}
	
	public static void ClearExtraHelp (string txt) {
		if (txt == self.extraHelp) {
			self.extraHelpTimeout = 0;
			self.extraHelp = "";
		}
	}
	
	public static void ActivateGameControl (Scene scene)
	{
		self.scene = scene;
		self.enabled = true;
		foreach (GameButton button in self.buttons) {
			if (button.hdlr != null) {
				button.hdlr.UpdateScene (scene, button);
			}
		}
		GameWindow.Reset ();
		self.CalculateLayout ();
		self.showHelpTips = false;
		self.helpTimeout = Time.timeSinceLevelLoad + 10f;
	}

	public static void DeactivateGameControl ()
	{
		// reset game windows
		GameWindow.Reset ();

		self.scene = null;
		self.enabled = false;
	}
	
	public static void InterfaceChanged ()
	{
		if (self != null) {
			self.CalculateLayout ();
		}
	}
	
	public static void ExpensesChanged () {
		long expenses = self.scene.actions.GetYearExpenses ();
		self.scene.progression.expenses = expenses;
		self.expenseText = expenses.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
	}

	public static void BudgetChanged ()
	{
		self.budgetText = self.scene.progression.budget.ToString ("#,##0\\.-", CultureInfo.GetCultureInfo ("en-GB"));
	}

	void CalculateLayout ()
	{
		if (scene == null)
			return;
		
		int sWidth = Screen.width;
//		int sHeight = Screen.height;
		int xOffset = 0;
		if (EditorCtrl.self.isOpen) {
			xOffset = 400;
			sWidth -= 400;
		}
		
		extraHelpRect = new Rect (xOffset + (sWidth - 600) / 2, 20, 600, 65);
		
		// calculate toolbar buttons
		int x = xOffset + 1;
		int y = 1;
		foreach (GameButton button in buttons) {
			if (button.hdlr != null) {
				button.hdlr.UpdateState (button);
			}
			if (button.isVisible) {
				if ((!button.isGameAction) || (button.isGameAction && !hideGameActions)) {
					button.position = new Rect (x, y, 32, 32);
					y += 33;
				}
			}
		}
		
		// calculate info panel
		ExpensesChanged ();
		BudgetChanged ();
		yearText = scene.progression.year.ToString ();
		budgetR = new Rect (Screen.width - 129 - 33, 1, 128, 32);
		budgetIconR = new Rect (Screen.width - 33, 1, 32, 32);
		expenseR = new Rect (Screen.width - 129 - 33, 34, 128, 32);
		expenseIconR = new Rect (Screen.width - 33, 34, 32, 32);
		yearR = new Rect (Screen.width - 129 - 33, 67, 128, 32);
		yearIconR = new Rect (Screen.width - 33, 67, 32, 32);
		
		// calculate succession button position
		successionR = new Rect (Screen.width - 65, Screen.height - 65, 64, 64);
	}

	void OnGUI ()
	{
		if (isProcessing)
			return;
		
		float timeSinceLevelLoad = Time.timeSinceLevelLoad;
		bool showHelpTips = (this.showHelpTips) || (helpTimeout > timeSinceLevelLoad);
		
		GUI.depth = 100;

		// Render Game Over beneath everything!
		if (scene != null && scene.progression.gameEnded) {
			// GAME OVER!!!!
			int sWidth = Screen.width;
			int xOffset = 0;
			if (EditorCtrl.self.isOpen) {
				xOffset = 400;
				sWidth -= 400;
			}
			GUI.Label (new Rect((sWidth - gameOver.width) / 2 + xOffset, (Screen.height - gameOver.height) / 2, gameOver.width, gameOver.height),
			           gameOver, GUIStyle.none);
		}

		if (!hideToolBar) {
			GameButton newActiveButton = null;
			foreach (GameButton button in buttons) {
				if (button.isVisible) {
					if (!button.isGameAction || (button.isGameAction && !hideGameActions)) {
						if (SimpleGUI.Label (button.position, (button == activeButton) ? (button.iconH) : (button.icon), (button == activeButton) ? whiteBgStyle : blackBgStyle)) {
							if (Event.current.type == EventType.MouseDown) {
								if (button.hdlr != null)
									button.hdlr.OnClick ();
							}
							newActiveButton = button;
						}
						if (showHelpTips) {
							float x = button.position.x + 33;
							float y = button.position.y;
							
							// don't want to catch mouse over
							GUI.Label (new Rect (x, y, 32, 32), leftIcon, GUIStyle.none);
							GUI.Label (new Rect (x + 32, y, 128, 32), button.description, helpTipStyle);
						}
					}
				}
			}
			if (activeButton != newActiveButton) {
				if ((activeButton != null) && (activeButton.help != "")){
					ClearExtraHelp (activeButton.help);
				}
				if ((newActiveButton != null) && (newActiveButton.help != "")){
					ExtraHelp (newActiveButton.help);
				}
			}
			if (newActiveButton != null) {
				activeButton = newActiveButton;
				activeButtonTimeout = Time.timeSinceLevelLoad + GAME_BUTTON_TIMEOUT_DELAY;
			} else if ((activeButton != null) && (activeButtonTimeout < Time.timeSinceLevelLoad)) {
				activeButton = null;
			}
			if ((activeButton != null) && (activeButton.hdlr != null)) {
				if (activeButton.hdlr.SelectRender (activeButton)) {
					activeButtonTimeout = Time.timeSinceLevelLoad + GAME_BUTTON_TIMEOUT_DELAY;
				}
			}
		}
		
		if (scene == null) return;
		
		SimpleGUI.Label (budgetIconR, budgetIcon, icon50Style);
		SimpleGUI.Label (expenseIconR, expenseIcon, icon50Style);
		SimpleGUI.Label (yearIconR, yearIcon, icon50Style);
		SimpleGUI.Label (budgetR, budgetText, icon50TextStyle);
		SimpleGUI.Label (expenseR, expenseText, icon50TextStyle);
		SimpleGUI.Label (yearR, yearText, icon50TextStyle);
		
		if (showHelpTips) {
			// as help tips are not shown continously we don't bother precalculating positions
			GUI.Label (new Rect (Screen.width - 129 - 66, 1, 32, 32), rightIcon, GUIStyle.none);
			GUI.Label (new Rect (Screen.width - 129 - 66 - 96, 1, 96, 32), "Total budget", helpTipStyle);
			GUI.Label (new Rect (Screen.width - 129 - 66, 34, 32, 32), rightIcon, GUIStyle.none);
			GUI.Label (new Rect (Screen.width - 129 - 66 - 96, 34, 96, 32), "Expenses", helpTipStyle);
			GUI.Label (new Rect (Screen.width - 129 - 66, 67, 32, 32), rightIcon, GUIStyle.none);
			GUI.Label (new Rect (Screen.width - 129 - 66 - 96, 67, 96, 32), "Current year", helpTipStyle);
		}
		
		if (!hideSuccessionButton && !scene.progression.gameEnded) {
			if (SimpleGUI.Button (successionR, successionIcon, successionIconH)) {
				StartCoroutine (CODoSuccession ());
			}
			if (showHelpTips) {
				GUI.Label (new Rect (Screen.width - 65 - 32, Screen.height - 48, 32, 32), rightIcon, GUIStyle.none);
				GUI.Label (new Rect (Screen.width - 65 - 160, Screen.height - 48, 128, 32), "Go to next year", helpTipStyle);
			}
		}
		
		if (!hideToolBar) {
			foreach (GameButton button in buttons) {
				if ((button.hdlr != null) && (button.alwaysRender)) {
					button.hdlr.DefaultRender ();
				}
			}
		}
		
		if (extraHelpTimeout > timeSinceLevelLoad) {
			GUI.Label (extraHelpRect, extraHelp, extraHelpStyle);
		}
	}
	
	private volatile bool isWorking = false;
	
	private void WorkThread (System.Object arg) 
	{
		foreach (AnimalType a in scene.animalTypes) {
			a.PrepareSuccession ();
		}
		scene.actions.PrepareSuccession ();

		foreach (AnimalType a in scene.animalTypes) {
			a.DoSuccession ();
		}
		scene.actions.DoSuccession ();

		foreach (AnimalType a in scene.animalTypes) {
			a.FinalizeSuccession ();
		}
		scene.actions.FinalizeSuccession ();

		HandleYearBudgets ();

		// Make the reportsMgr check if we should show reports or questionnaires
		scene.reports.FinalizeSuccession ();

		isWorking = false;
	}

	void HandleYearBudgets ()
	{
		// Add year budget
		scene.progression.budget += scene.progression.yearBudget;
		// Add variable year budgets
		foreach (Progression.VariableYearBudget yb in scene.progression.variableYearBudgets) {
			if (yb.year == scene.progression.year + 1) {
				scene.progression.budget += yb.budget;
			}
		}
		BudgetChanged ();
	}
	
	IEnumerator CODoSuccession ()
	{
		SimpleSpinner.ActivateSpinner ();
		isProcessing = true;
		yield return 0;

		for (int i = 0; i < scene.progression.yearsPerCycle; i++)
		{
			yield return 0;

			#pragma warning disable 162
			isWorking = true;
			if (GameSettings.SUCCESSION_IN_BG_THREAD) {
				ThreadPool.QueueUserWorkItem (WorkThread, null);
			}
			else {
				WorkThread(null);
			}
			#pragma warning restore 162
			
			while (isWorking) {
				yield return 0;
			}
		
			scene.progression.Advance ();

			yield return 0;
		}
		
		TerrainMgr.self.ForceRedraw ();
		
		while (TerrainMgr.IsRendering) {
			yield return 0;
		}
		
		// call update scene on button handlers for caching current scene information
		foreach (GameButton button in buttons) {
			if (button.hdlr != null) {
				button.hdlr.UpdateScene (scene, button);
			}
		}
		// reset game windows
		GameWindow.Reset ();
		// also, update interface!
		InterfaceChanged ();
		yield return 0;
		isProcessing = false;
		SimpleSpinner.DeactivateSpinner ();
		if (EditorCtrl.self) {
			// update editor windows if needed
			EditorCtrl.self.HandleSuccession ();
		}

		// Reset predefined variables
		scene.actions.ClearTempVariables ();
	}
}
