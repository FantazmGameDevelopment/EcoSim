using UnityEngine;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;

namespace Ecosim.GameCtrl
{
	public class ExportGraphWindow : GameWindow
	{
		public static int windowWidth = 800;
		public static int windowHeight = 400;
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
		public const float pointLinesWidth = 3f;
		public const float gridLinesWidth = 1.5f;
		public const float gridLineIndent = 5f;
		public Color gridLinesColor = new Color (0.85f, 0.85f, 0.85f, 1f);

		private Vector2 pointOffset = new Vector2 (-1, 0);
		private Vector2 pointIconOffset = new Vector2 (3, 0);
		private Texture2D[] pointIcons;
		private Color[] pointColors;
		private GUIStyle rightAlign;
		private GUIStyle leftAlign;
		private GUIStyle centeredAlign;
		private InventarisationsData invData;
		private float minValue;
		private float maxValue;
		private Dictionary<string, Vector2> prevPoints;

		public ExportGraphWindow () : base (-1, -1, windowWidth, null)
		{
			invData = ExportMgr.self.GetInventarisationsData ();
			minValue = invData.GetLowestValue ();
			if (minValue > 0f) {
				minValue = Mathf.Clamp (minValue * 0.8f, 0f, Mathf.Infinity);
			} else if (minValue < 0f) {
				minValue = minValue * 0.8f;
			}
			maxValue = invData.GetHighestValue () * 1.2f;

			// Get all values
			prevPoints = new Dictionary<string, Vector2>();
			foreach (string s in invData.EnumerateValues()) {
				prevPoints.Add (s, Vector2.zero);
			}

			rightAlign = GameControl.self.skin.GetStyle ("ExportGraph Right");
			leftAlign = GameControl.self.skin.GetStyle ("ExportGraph Left");
			centeredAlign = GameControl.self.skin.GetStyle ("ExportGraph Centered");

			// Get the point icons
			Object[] loadedIcons = Resources.LoadAll ("GraphIcons");
			pointIcons = new Texture2D[loadedIcons.Length];
			for (int i = 0; i < loadedIcons.Length; i++) {
				pointIcons[i] = (Texture2D)loadedIcons[i];
			}
			if (pointIcons.Length == 0)
				pointIcons = new Texture2D[] { new Texture2D (0, 0) };

			// Get the point colors
			pointColors = new Color[]
			{
				new Color (1f, 0f, 0f, 1f),
				new Color (0f, 1f, 0f, 1f),
				new Color (0f, 0f, 1f, 1f),
				new Color (0f, 1f, 1f, 1f),
				new Color (1f, 1f, 0f, 1f)
			};
		}

		public override void Render ()
		{
			float graphHeight = windowHeight - (windowHeightMargin * 2f);
			float graphWidth = windowWidth - (windowWidthMargin * 2f);
			float legendWidth = (legendWidthOffset + legendLineWidth + legendLabelWidth + legendLineSpace);
			Vector2 start, end;

			// Graph title
			Rect graphRect = new Rect (xOffset + 33, yOffset, windowWidth - 33, titleHeight);
			GUI.Label (graphRect, "Graph", title);

			// Background
			graphRect.width = windowWidth;
			graphRect.x -= 33;
			graphRect.y += graphRect.height;
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
				GUI.Label (yr, (maxValue - (yStep * i)).ToString ("0"), rightAlign);
			}

			// X Axis
			Rect xRect = graphRect;
			xRect.width = valueLabelWidth;
			xRect.height = valueLabelHeight;
			xRect.x = graphRect.x + valueLabelWidth;
			xRect.y = graphRect.y + graphRect.height - valueLabelHeight;

			// Loop through all years
			int yearsCount = invData.GetYearsCount ();
			int yearIndex = 0;
			foreach (InventarisationsData.YearData year in invData.EnumerateYears())
			{
				// Set x position
				Rect xr = xRect;
				xr.x = xr.x - (xr.width * 0.5f);
				xr.x += ((graphRect.width - xr.width) / (float)(yearsCount - 1)) * yearIndex;
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

						// Draw the point (icon)
						Rect pr = xr;
						pr.width = pointIcons [iconIndex].width;
						pr.height = pointIcons [iconIndex].height;
						pr.x += valueLabelWidth * 0.5f;
						pr.x -= pr.width * 0.5f;

						// Calculate the height of the point
						float minY = graphRect.y;
						float maxY = pr.y;
						pr.y = minY + ((maxY - minY) * p);
						//pr.y += valueLabelHeight * 0.5f;
						pr.y -= pr.height * 0.5f;
						pr.x += pointOffset.x;
						pr.y += pointOffset.y;

						// Set the color and draw the icon
						GUI.color = pointColors [colorIndex];
						Rect ir = pr;
						ir.x += pointIconOffset.x;
						ir.y += pointIconOffset.y;
						GUI.Label (ir, pointIcons [iconIndex]);

						// Draw line
						end = new Vector2 (pr.x + (pr.width * 0.5f), pr.y + (pr.height * 0.5f));
						if (yearIndex > 0) {
							start = prevPoints [value];
							Drawing.DrawLine (start, end, GUI.color, pointLinesWidth);
						}
						// Remember the value so the next point will draw the line
						prevPoints [value] = end;

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
			GUI.color = new Color (0f, 0f, 0f, 0.1f);
			GUI.Label (legendRect, "", black);
			GUI.color = Color.white;

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

				// Show legend icon (TODO: make it a button?)
				lr.width = pointIcons [iconIndex].width;
				lr.height = pointIcons [iconIndex].height;
				lr.x += legendLineWidth * 0.5f;
				lr.x -= (lr.width * 0.5f);

				Rect ir = lr;
				ir.y -= (ir.height * 0.5f);
				ir.x += pointIconOffset.x;
				ir.y += pointIconOffset.y;
				GUI.Label (ir, pointIcons [iconIndex]);

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

			base.Render ();
		}
	}
}

