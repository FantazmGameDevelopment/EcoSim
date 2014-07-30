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
		public static int windowWidth { get { return Mathf.Min (800, Screen.width); } }
		public static int windowHeight { get { return Mathf.Min (400, Screen.height); }	}
		public const int windowWidthMargin = 32;
		public const int windowHeightMargin = 32;
		public const int titleHeight = 32;
		public const int valueLabelWidth = 50;
		public const int valueLabelHeight = 20;
		public const int legendLabelWidth = 100;
		public const int legendLabelHeight = 20;
		public const int legendWidthOffset = 20;
		public const int legendLineSpace = 10;
		public const int legendHeight = 40;
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

		public ExportGraphWindow () : base (-1, -1, windowWidth, null)
		{
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
			float graphHeight = windowHeight - (windowHeightMargin * 2f);
			float graphWidth = windowWidth - (windowWidthMargin * 2f);
			float legendWidth = (legendWidthOffset + legendLineWidth + legendLabelWidth + legendLineSpace);
			Vector2 start, end;
			hoverLabel = "";

			// Graph Rect
			Rect graphRect = new Rect (xOffset + (titleHeight + 1), yOffset, windowWidth - (titleHeight + 1), titleHeight);

			// Title
			Rect titleRect = graphRect;
			titleRect.width -= (120f + 1f) * 2f;
			GUI.Label (titleRect, "Graph", title);

			// Toggle values button
			Rect toggleRect = titleRect;
			toggleRect.x += titleRect.width + 1;
			toggleRect.width = 120f;
			if (SimpleGUI.Button (toggleRect, "Toggle values", entry, entrySelected)) {
				showLabels = !showLabels;
			}

			// Save Button
			bool doSave = false;
			Rect saveRect = toggleRect;
			saveRect.x += saveRect.width + 1f;
			//saveRect.width = 80f;
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
					
					int edgeX = (xOffset + windowWidth);
					if (edgeX > Screen.width) {
						xOffset -= edgeX % Screen.width;
					}
					int edgeY = (yOffset + windowHeight + (titleHeight + 1));
					if (edgeY > Screen.height) {
						yOffset -= edgeY % Screen.height;
					}

					// Set the quality level
					preSaveQuality = QualitySettings.GetQualityLevel ();
					QualitySettings.SetQualityLevel (5, true);

					if (SaveFileDialog.Show ("ecosim graph", out savePath, "png (*.png)|*.png"))
					{
						// We mark "save" as true so the OnPostRender can handle the save
						saveGraph = true;
						instance.StartCoroutine ( RenderAndSaveGraph () );
					}
				}
			}

			// Background
			graphRect.width = windowWidth;
			graphRect.x -= 33f; // X button
			graphRect.y += graphRect.height + 1f;
			graphRect.height = windowHeight;
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

		private IEnumerator RenderAndSaveGraph ()
		{
			if (!saveGraph) yield break;
			saveGraph = false;

			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();
			yield return new WaitForEndOfFrame ();

			// Create new texture
			Texture2D tex = new Texture2D (windowWidth, windowHeight, TextureFormat.RGB24, false);
			int x = xOffset;
			int y = Screen.height - (yOffset + (titleHeight + 1)) - windowHeight;
			tex.ReadPixels (new Rect (x, y, windowWidth, windowHeight), 0, 0, false);
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

