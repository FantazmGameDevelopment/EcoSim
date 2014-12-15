using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;

namespace Ecosim.GameCtrl
{
	public class ExportGraphInfoWindow : GameWindow
	{
		private string infoText = "";

		public ExportGraphInfoWindow () : base (-1, -1, 400, null)
		{
			infoText = @"To view distribution maps in the Ecosim landscape, click the survey of your choice and subsequently the year of your choice.

To create a trendline, click 'Show Functions'. Now choose the survey of your choice or click 'Select all' to show all surveys in a single graph. Subsequently press generate.

To view the exact numbers of each survey point, click 'Show Values' in the graph.

To export the graph to use in a report, click 'Save...'. ";
			infoText = infoText.Replace ("'", "\"");
		}
		
		public override void Render ()
		{
			Rect r = new Rect (xOffset + 33, yOffset, this.width - 33, 32);
			SimpleGUI.Label (r, "Export graph info window", title);
			
			GUILayout.BeginArea (new Rect (xOffset, yOffset + 33, width, Mathf.Min (600f, Screen.height - (yOffset + 33))));
			{
				GUILayout.Label (infoText, entry, GUILayout.MaxWidth (Screen.width));
			}
			GUILayout.EndArea ();
			
			base.Render ();
		}
	}
}

