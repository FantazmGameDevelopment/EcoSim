using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneEditor;

namespace Ecosim.SceneEditor.Helpers
{
	public class SpeciesPanelPlantsHelper
	{
		private static string newPlantName = "New plant";

		private static SpeciesPanel panel;
		private static EditorCtrl ctrl;
		private static Scene scene;

		public static void Setup (SpeciesPanel pnl)
		{
			panel = pnl;
			ctrl = panel.ctrl;
			scene = panel.scene;
		}

		public static void Render (int mx, int my)
		{
			int index = 0;
			foreach (SpeciesPanel.PlantState ps in panel.plants) 
			{
				GUILayout.BeginVertical (ctrl.skin.box);
				{
					// Header
					GUILayout.BeginHorizontal ();
					{
						if (GUILayout.Button (ps.isFoldedOpen ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
						{
							ps.isFoldedOpen = !ps.isFoldedOpen;
						}
						
						GUILayout.Label (index.ToString(), GUILayout.Width (40));
						ps.plant.name = GUILayout.TextField (ps.plant.name);
						
						if (GUILayout.Button ("-", GUILayout.Width (20)))
						{
							SpeciesPanel.PlantState tmpPS = ps;
							ctrl.StartDialog (string.Format("Delete plant '{0}'?", tmpPS.plant.name), newVal => { 
								panel.DeletePlant (tmpPS);
							}, null);
						}
					}
					GUILayout.EndHorizontal(); // ~Header
					GUILayout.Space (5f);
					
					// Plant body
					if (ps.isFoldedOpen) 
					{
						GUILayout.BeginVertical (ctrl.skin.box);
						
						// Parameter name
						GUILayout.BeginHorizontal ();
						GUILayout.Label (string.Format(" Parameter name: '{0}'", ps.plant.dataName), GUILayout.Width (260));
						GUILayout.EndHorizontal ();
						
						GUILayoutOption labelLayout = GUILayout.Width (140);
						GUILayoutOption fieldLayout = GUILayout.Width (40);
						
						EcoGUI.IntField (" # Spawn seeds attempts", ref ps.plant.spawnCount, labelLayout, fieldLayout); 
						EcoGUI.IntField (" Spawn seeds dispersion", ref ps.plant.spawnRadius, labelLayout, fieldLayout);
						
						GUILayout.BeginHorizontal ();
						{
							EcoGUI.skipHorizontal = true;
							EcoGUI.IntField (" Maximum per tile", ref ps.newMaxPerTile, labelLayout, fieldLayout);
							EcoGUI.skipHorizontal = false;
							
							if (ps.newMaxPerTile != ps.plant.maxPerTile)
							{
								GUILayout.Label ("was " + ps.plant.maxPerTile.ToString(), GUILayout.Width (40));
								if (GUILayout.Button ("Update", GUILayout.Width(60))) 
								{
									// Update the current plants data parameter map because we have changed the max per tile
									Data plantData = scene.progression.GetData (ps.plant.dataName);
									int prevMaxPerTile = ps.plant.maxPerTile;
									ps.plant.maxPerTile = ps.newMaxPerTile;
									plantData.ProcessNotZero (delegate(int x, int y, int val, object data) {
										// Get the current value's percentage to calculate the new value according the new max (per tile) value
										float perc = (float)val / prevMaxPerTile;
										int newVal = Mathf.RoundToInt(perc * (float)ps.plant.maxPerTile);
										plantData.Set (x, y, newVal);
									}, null);
								}
								if (GUILayout.Button ("?", GUILayout.Width (20))) {
									string message = 
										@"You need to explicitly say to update the 'Maximum per tile' value, because changing this value will affect the currently placed amounts of the plant on the terrain.

The current values will be converted by their percentage of the current maximum per tile value, like this:

[new value] = ([current value]/[previous max]) * [new max].";
									ctrl.StartOkDialog (message, null, 300, 150);
								}
							}
						}
						GUILayout.EndHorizontal ();
						
						// Rules
						GUILayout.Space (2);
						GUILayout.BeginHorizontal(); // Rules
						{
							if (GUILayout.Button ("Open Rules", GUILayout.Width (120)))
							{
								panel.DisposeExtraPanel ();
								panel.extraPanel = new PlantRulesExtraPanel (ctrl, ps.plant);
							}
						}
						GUILayout.EndHorizontal(); // ~Rules
						
						GUILayout.EndVertical ();
					}
				}
				GUILayout.EndVertical(); // ~Plant body
				index++;
			} // ~PlantState foreach
			
			if (panel.RenderAddButton ("New plant:", ref newPlantName))
			{
				// Add new plant
				PlantType t = new PlantType (scene, newPlantName);
				SpeciesPanel.PlantState state = new SpeciesPanel.PlantState (t);
				panel.plants.Add (state);
			}
		}
	}
}
