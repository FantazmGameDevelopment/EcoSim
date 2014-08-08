using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;

namespace Ecosim.GameCtrl
{
	public class ExportGraphWindow : GameWindow
	{
		// Make sure the width and height don't exceed the screen width/height
		// because otherwise we won't be able to render the png properly (using ReadPixels)
		public static int windowWidth = 800;
		public static int WindowWidth {
			get { return Mathf.Min (windowWidth, (int)((float)Screen.width * 0.9f)); } 
		}
		public static int windowHeight = 400;
		public static int WindowHeight { 
			get { return Mathf.Min (windowHeight, Screen.height - ((titleHeight + 1) * 2)); }	
		}

		public const int windowWidthMargin = 32;
		public const int windowHeightMargin = 32;
		public const int titleHeight = 32;
		public const int valueLabelWidth = 50;
		public const int valueLabelHeight = 20;
		public const int legendLabelWidth = 100;
		public const int legendLabelHeight = 20;
		public const int legendWidthOffset = 20;
		public const int legendLineSpace = 10;
		public const int legendHeight = 60;
		public const int legendLineWidth = 35;
		public const int yAxisSteps = 6;
		public const float pointLinesWidth = 2f;
		public const float pointIconWidth = 20f;
		public const float pointIconHeight = 20f;
		public const float gridLinesWidth = 1.5f;
		public const float gridLineIndent = 5f;
		public Color gridLinesColor = new Color (0.85f, 0.85f, 0.85f, 1f);

		private Vector2 pointOffset = new Vector2 (-1f, 0f);
		private Vector2 pointIconOffset = new Vector2 (6f, 2f);
		private Texture2D[] pointIcons;
		private Color[] pointColors;
		private GUIStyle rightAlign;
		private GUIStyle leftAlign;
		private GUIStyle centeredAlign;
		private InventarisationsData invData;
		private float minValue;
		private float maxValue;
		private Dictionary<string, Vector2> prevPoints;
		private Dictionary<string, bool> iconActiveStates;
		private string hoverLabel = "";
		private bool showLabels = false;
		private string numberFormat = "0";

		private int preSaveQuality = 0;
		private bool saveGraph = false;
		private string savePath = "";

		private string newLowestValue;
		private string newHighestValue;

		//private string newWindowWidth;
		//private string newWindowHeight;

		public ExportGraphWindow () : base (-1, -1, WindowWidth, null)
		{
			// Reset sizes
			//windowWidth = 800;
			//windowHeight = 400;
			//width = windowWidth;

			// Get the inventarisation data
			invData = ExportMgr.self.GetInventarisationsData ();
			// Calculate the minimum and maximum values
			minValue = invData.GetLowestValue ();
			if (minValue > 0f) {
				minValue = Mathf.Clamp (minValue * 0.8f, 0f, Mathf.Infinity);
			} else if (minValue < 0f) {
				minValue = minValue * 0.8f;
			}
			maxValue = invData.GetHighestValue () * 1.2f;

			// Round up and down to x
			int roundTo = 10;
			maxValue = (roundTo - (maxValue % roundTo)) + maxValue;
			minValue = minValue - (minValue % roundTo);

			// Check if we have decimals
			bool hasDecimals = false;
			foreach (InventarisationsData.YearData year in invData.EnumerateYears()) {
				foreach (string s in invData.EnumerateValues ()){
					float v = 0f;
					if (year.GetValue (s, out v)) {
						if (v % 1f != 0f) 
							hasDecimals = true;
					}
					if (hasDecimals)
						break;
				}
				if (hasDecimals)
					break;
			}
			numberFormat = (hasDecimals) ? "0.00" : "0";

			// Setup data (reference) dictionaries
			prevPoints = new Dictionary<string, Vector2>();
			iconActiveStates = new Dictionary<string, bool> ();
			foreach (string s in invData.EnumerateValues()) {
				prevPoints.Add (s, Vector2.zero);
				iconActiveStates.Add (s, true);
			}

			// Setup GUI styles
			rightAlign = GameControl.self.skin.GetStyle ("ExportGraph Right");
			leftAlign = GameControl.self.skin.GetStyle ("ExportGraph Left");
			centeredAlign = GameControl.self.skin.GetStyle ("ExportGraph Centered");

			// Get the point icons
			Object[] loadedIcons = Resources.LoadAll ("GraphIcons");
			pointIcons = new Texture2D[loadedIcons.Length];
			for (int i = 0; i < loadedIcons.Length; i++) {
				pointIcons[i] = (Texture2D)loadedIcons[i];
			}
			// Check if we have point icons
			if (pointIcons.Length == 0)
				pointIcons = new Texture2D[] { new Texture2D (0, 0) };

			// Get the point colors
			pointColors = new Color[]
			{
				new Color (1f, 0f, 0f, 1f),
				new Color (0f, 1f, 0f, 1f),
				new Color (0f, 0f, 1f, 1f),
				new Color (1f, 1f, 0f, 1f),
				new Color (0f, 1f, 1f, 1f),
				new Color (1f, 0f, 1f, 1f)
			};
		}

		public override void Render ()
		{
			float graphHeight = WindowHeight - (windowHeightMargin * 2f);
			float graphWidth = WindowWidth - (windowWidthMargin * 2f);
			float legendWidth = (legendWidthOffset + legendLineWidth + legendLabelWidth + legendLineSpace);
			Vector2 start, end;
			hoverLabel = "";

			// Graph Rect
			Rect graphRect = new Rect (xOffset + (titleHeight + 1), yOffset, WindowWidth - (titleHeight + 1), titleHeight);

			// Title
			Rect titleRect = graphRect;
			titleRect.width -= (120f + 1f) * 1f;
			GUI.Label (titleRect, "Graph", title);

			// Save Button
			bool doSave = false;
			Rect saveRect = titleRect;
			saveRect.x += saveRect.width + 1;
			saveRect.width = 120f;
			if (SimpleGUI.Button (saveRect, "Save...", entry, entrySelected)) 
			{
				if (saveGraph == false)
				{
					// Set window on top
					SetWindowOnTop ();

					// Check if the graph exceeds the screen
					if (xOffset < 0) {
						xOffset = 0;
					}
					if (yOffset < 0) {
						yOffset = 0;
					}
					
					int edgeX = (xOffset + WindowWidth);
					if (edgeX > Screen.width) {
						xOffset -= edgeX % Screen.width;
					}
					int edgeY = (yOffset + WindowHeight + ((titleHeight + 1) * 2));
					if (edgeY > Screen.height) {
						yOffset -= edgeY % Screen.height;
					}

					// Set the quality level
					preSaveQuality = QualitySettings.GetQualityLevel ();
					QualitySettings.SetQualityLevel (5, true);

					instance.StartCoroutine (SaveFileDialog.Show ("ecosim graph", "png (*.png)|*.png", delegate(bool ok, string url) {
						if (ok) {
							// We mark "save" as true so the OnPostRender can handle the save
							saveGraph = true;
							savePath = url;
							instance.StartCoroutine ( RenderAndSaveGraph () );
						}
					}));
				}
			}

			// Adjustable settings
			Rect settingsRect = graphRect;
			settingsRect.width = WindowWidth;
			settingsRect.x -= titleHeight + 1f;
			settingsRect.y += graphRect.height + 1f;
			GUILayout.BeginArea (settingsRect);
			{
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label ("", header, GUILayout.MaxWidth (WindowWidth), GUILayout.MaxHeight (50));
					GUILayout.FlexibleSpace ();
					GUILayout.Space (1);

					// Adjustable window size
					/*GUILayout.Label ("Window size", header, GUILayout.Width (100), GUILayout.MaxHeight (50));
					GUILayout.Space (1);
					RenderAdjustableFloatField (windowWidth, ref newWindowWidth, UpdateWindowWidth);
					GUILayout.Space (1);
					GUILayout.Label ("x", header, GUILayout.Width (20), GUILayout.MaxHeight (50));
					GUILayout.Space (1);
					RenderAdjustableFloatField (windowHeight, ref newWindowHeight, UpdateWindowHeight);
					GUILayout.Space (1);*/

					// Adjustable lowest value
					GUILayout.Label ("Value Range", header, GUILayout.Width (110), GUILayout.MaxHeight (50));
					GUILayout.Space (1);
					RenderAdjustableFloatField (minValue, ref newLowestValue, UpdateMinValue);
					GUILayout.Space (1);

					// Adjustable highest value
					GUILayout.Label ("-", header, GUILayout.Width (20), GUILayout.MaxHeight (50));
					GUILayout.Space (1);
					RenderAdjustableFloatField (maxValue, ref newHighestValue, UpdateMaxValue);
					GUILayout.Space (1);

					// Toggle values
					if (GUILayout.Button (((showLabels)?"Hide Values":"Show Values"), entry, GUILayout.Width (120), GUILayout.MaxHeight (50))) {
						showLabels = !showLabels;
					}
				}
				GUILayout.EndHorizontal ();
			}
			GUILayout.EndArea ();

			// Background
			graphRect.width = WindowWidth;
			graphRect.x -= 33f; // X button
			graphRect.y += graphRect.height + 1f;
			graphRect.y += settingsRect.height + 1f;
			graphRect.height = WindowHeight;
			GUI.Label (graphRect, "", white);

			// Setup graph rect
			graphRect.x += windowWidthMargin;
			graphRect.y += windowHeightMargin;
			graphRect.height = graphHeight;
			graphRect.width = graphWidth - legendWidth;

			// DEBUG: Visualise graph rect
			/*Color c = Color.white;
			c.a = 0.05f;
			GUI.color = c;
			GUI.Label (graphRect, "", black);
			GUI.color = Color.white;*/

			// Y Axis
			Rect yRect = graphRect;
			yRect.width = valueLabelWidth;
			yRect.height = valueLabelHeight;
			yRect.x = graphRect.x;
			yRect.y = graphRect.y - (yRect.height * 0.5f);

			float yStep = (maxValue - minValue) / (float)(yAxisSteps - 1);

			// First vertical line
			start = new Vector2 (yRect.x + valueLabelWidth, yRect.y + (valueLabelHeight * 0.5f));
			end = new Vector2 (start.x, start.y + graphHeight - valueLabelHeight);
			Drawing.DrawLine (start, end, gridLinesColor, gridLinesWidth);

			// Draw last vertical line
			start = new Vector2 (yRect.x + graphRect.width, yRect.y + (valueLabelHeight * 0.5f));
			end = new Vector2 (start.x, start.y + graphHeight - valueLabelHeight);
			Drawing.DrawLine (start, end, gridLinesColor, gridLinesWidth);

			for (int i = 0; i < yAxisSteps; i++) 
			{
				// Get the height
				Rect yr = yRect;
				yr.y = (int)(yRect.y + ((graphRect.height - valueLabelHeight) / (float)(yAxisSteps - 1)) * i);

				// Draw horizontal line
				start = new Vector2 (yr.x + valueLabelWidth - gridLineIndent - 2, yr.y + (valueLabelHeight * 0.5f));
				end = new Vector2 (start.x + graphWidth - valueLabelWidth - legendWidth + gridLineIndent + 2, start.y);
				Drawing.DrawLine (start, end, gridLinesColor, gridLinesWidth);

				// Draw label
				yr.x -= gridLineIndent;
				if (i == 0 || i == yAxisSteps - 1)
					GUI.Label (yr, (maxValue - (yStep * i)).ToString (numberFormat), rightAlign);
			}

			// X Axis
			Rect xRect = graphRect;
			xRect.width = valueLabelWidth;
			xRect.height = valueLabelHeight;
			xRect.x = graphRect.x + valueLabelWidth;
			xRect.y = graphRect.y + graphRect.height - valueLabelHeight;

			// Reset all previous point
			foreach (string value in invData.EnumerateValues()) {
				prevPoints [value] = new Vector2 (-1f, -1f);
			}

			// Loop through all years
			int yearsCount = invData.GetYearsCount ();
			int yearIndex = 0;
			foreach (InventarisationsData.YearData year in invData.EnumerateYears())
			{
				// Set x position
				Rect xr = xRect;
				xr.x = xr.x - (xr.width * 0.5f);
				if (yearsCount > 1) {
					xr.x += ((graphRect.width - xr.width) / (float)(yearsCount - 1)) * yearIndex;
				}
				xr.x = (int)xr.x;

				// Draw line
				start = new Vector2 (xr.x + (xr.width * 0.5f), xr.y);
				end = new Vector2 (start.x, start.y + gridLineIndent);
				Drawing.DrawLine (start, end, gridLinesColor, gridLinesWidth);

				// Draw Label
				GUI.Label (xr, year.year.ToString(), centeredAlign);

				// Draw the points (values)
				int valueIndex = 0;
				foreach (string value in invData.EnumerateValues())
				{
					// Calculate the percentage
					float v = 0f;
					if (year.GetValue (value, out v))
					{
						// Calc the percentage
						float n = v - minValue;
						float o = maxValue - minValue;
						float p = 1f - (n / o);
					
						// Get the color and icon
						int colorIndex = valueIndex % pointColors.Length;
						int iconIndex = valueIndex % pointIcons.Length;

						// Set the color
						GUI.color = pointColors [colorIndex];

						// Get the rect
						Rect pr = xr;
						pr.width = pointIconWidth;// pointIcons [iconIndex].width;
						pr.height = pointIconHeight;// pointIcons [iconIndex].height;
						pr.x += valueLabelWidth * 0.5f;
						pr.x -= pr.width * 0.5f;

						// Draw the point (icon)
						bool iconActive = iconActiveStates [value];

						// Calculate the height of the point
						float minY = graphRect.y;
						float maxY = pr.y;
						pr.y = minY + ((maxY - minY) * p);
						//pr.y += valueLabelHeight * 0.5f;
						pr.y -= pr.height * 0.5f;
						pr.x += pointOffset.x;
						pr.y += pointOffset.y;

						// Draw line
						end = new Vector2 (pr.x + (pr.width * 0.5f), 
						                   pr.y + (pr.height * 0.5f));
						// We draw the line to the previous point, so we should not draw the line to the first prevPoint
						if (prevPoints [value].x != -1f) {
							start = prevPoints [value];
							start = new Vector2 ((int)start.x, (int)start.y);
							Drawing.DrawLine (start, new Vector2 ((int)end.x, (int)end.y), GUI.color, pointLinesWidth);
						} 

						// Remember the value so the next point will draw the line
						prevPoints [value] = end;

						// Draw the icon
						Rect ir = pr;
						ir.x += pointIconOffset.x;
						ir.y += pointIconOffset.y;
						if (iconActive) {
							GUI.Label (ir, pointIcons [iconIndex]);
						}

						// Check for label
						if (showLabels) {
							// Show the label
							Rect labelRect = pr;
							labelRect.x += labelRect.width;
							labelRect.y -= labelRect.height * 0.5f;
							labelRect.width = valueLabelWidth;
							labelRect.height = valueLabelHeight;
							GUI.Label (labelRect, v.ToString (numberFormat), leftAlign);
						}
						else if (SimpleGUI.CheckMouseOver (ir)) {
							hoverLabel = v.ToString (numberFormat);
						}

						// Reset the color
						GUI.color = Color.white;
					}

					valueIndex++;
				}

				yearIndex++;
			}

			// Draw legend
			Rect legendRect = graphRect;
			legendRect.x = legendRect.x + graphRect.width;
			legendRect.width = legendWidth;
			legendRect.height -= valueLabelHeight;

			// DEBUG: Visualise legend rect
			/*GUI.color = new Color (0f, 0f, 0f, 0.1f);
			GUI.Label (legendRect, "", black);
			GUI.color = Color.white;*/

			// Draw all values
			int valuesCount  = invData.GetValuesCount ();
			float yDiff = Mathf.Min (legendHeight, (legendRect.height / valuesCount));
			float yCenter = legendRect.y + (legendRect.height * 0.5f);
			int legendIndex = 0;
			foreach (string l in invData.EnumerateValues ())
			{
				// Get the color and icon
				int colorIndex = legendIndex % pointColors.Length;
				int iconIndex = legendIndex % pointIcons.Length;

				// Set the color
				GUI.color = pointColors [colorIndex];

				Rect lr = legendRect;
				lr.x += legendWidthOffset;
				lr.y = yCenter - ((yDiff * valuesCount) * 0.5f) + (yDiff * legendIndex) + (legendHeight * 0.5f);
				lr.height = legendHeight;
				lr.width = legendWidth;

				// Show legend line
				start = new Vector2 (lr.x, lr.y);
				end = new Vector2 (start.x + legendLineWidth, start.y);
				Drawing.DrawLine (start, end, GUI.color, pointLinesWidth);

				// Show legend icon
				lr.width = pointIconWidth;//pointIcons [iconIndex].width;
				lr.height = pointIconHeight;//pointIcons [iconIndex].height;
				lr.x += legendLineWidth * 0.5f;
				lr.x -= (lr.width * 0.5f);

				Rect ir = lr;
				ir.y -= (ir.height * 0.5f);
				ir.x += pointIconOffset.x;
				ir.y += pointIconOffset.y;

				// Show (invisible/visible) icon button
				bool active = iconActiveStates [l];
				if (!active) {
					GUI.color = new Color (0f, 0f, 0f, 0f);
				}
				GUI.Label (ir, pointIcons [iconIndex]);
				if (SimpleGUI.CheckMouseOver (ir) && (Event.current.type == EventType.MouseDown)) {
					Event.current.Use ();
					iconActiveStates[l] = !active;
				}

				// Reset the color
				GUI.color = Color.white;

				// Show label
				lr.width = legendLabelWidth;
				lr.height = legendLabelHeight;
				lr.x = legendLineSpace + legendRect.x + legendWidthOffset + legendLineWidth;
				lr.y -= lr.height * 0.5f;
				GUI.Label (lr, l, leftAlign);

				legendIndex++;
			}

			// Draw hover label, if we have a string to show
			if (hoverLabel.Length > 0) {
				Rect hoverRect = new Rect (Event.current.mousePosition.x, 
				                           Event.current.mousePosition.y,
				                           valueLabelWidth,
				                           valueLabelHeight);
				hoverRect.y -= hoverRect.height;
				GUI.Label (hoverRect, hoverLabel, leftAlign);
			}

			base.Render ();
		}

		/*void UpdateWindowWidth (float newValue)
		{
			if (newValue < 400)
				newValue = 400;
			windowWidth = (int)newValue;
			windowWidth = WindowWidth;
			width = WindowWidth;
		}

		void UpdateWindowHeight (float newValue)
		{
			if (newValue < 200)
				newValue = 200;
			windowHeight = (int)newValue;
			windowHeight = WindowHeight;
			height = WindowHeight;
		}*/

		void UpdateMinValue (float newValue)
		{
			minValue = newValue;
			// Check if the minimum does not exceed the lowest value of all points
			float lowestValue = invData.GetLowestValue ();
			if (minValue > lowestValue)
				minValue = lowestValue;
		}

		void UpdateMaxValue (float newValue)
		{
			maxValue = newValue;
			// Check if the max does not exceed the highest value of all points
			float highestValue = invData.GetHighestValue ();
			if (maxValue < highestValue)
				maxValue = highestValue;
		}

		void RenderAdjustableFloatField (float value, ref string newValue, System.Action<float> onValueUpdated)
		{
			// Check if we have initial value
			newValue = newValue ?? value.ToString ();
			
			// Check if the value is different, we do this before (in the frame after)
			// to be able to adjust the color of the textfield. Red means incorrect format.
			if (newValue != value.ToString ()) {
				GUI.color = Color.white;
				float outValue;
				if (float.TryParse (newValue, out outValue)) {
					onValueUpdated (outValue);
					newValue = value.ToString ();
				} else {
					GUI.color = Color.red;
				}
			}
			newValue = GUILayout.TextField (newValue, textField, GUILayout.Width (80), GUILayout.MaxHeight (50));
			GUI.color = Color.white;
		}

		private IEnumerator RenderAndSaveGraph ()
		{
			if (!saveGraph) yield break;
			saveGraph = false;

			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();

			// Create new texture
			Texture2D tex = new Texture2D (WindowWidth, WindowHeight, TextureFormat.RGB24, false);
			int x = xOffset;
			int y = Screen.height - (yOffset + ((titleHeight + 1) * 2)) - WindowHeight;
			tex.ReadPixels (new Rect (x, y, WindowWidth, WindowHeight), 0, 0, false);
			tex.Apply ();

			// Save the texture
			byte[] texBytes = tex.EncodeToPNG ();
			File.WriteAllBytes (savePath, texBytes);

			// Destroy the texture
			Texture.Destroy (tex);
			tex = null;

			// Reset the quality settings
			QualitySettings.SetQualityLevel (preSaveQuality);
		}
	}
}

