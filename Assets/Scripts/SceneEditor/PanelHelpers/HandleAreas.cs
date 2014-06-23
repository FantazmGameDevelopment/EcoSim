using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.PlantRules;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleAreas : ParameterPaintPanel
	{
		private enum Areas
		{
			Managed,
			Succession,
			Purchasable,
			Target
		};
		
		private Areas currentArea;

		private int currentTargetAreaIndex = -1;
		private bool targetAreasOpened = false;
		private Vector2 targetAreasScrollPos;

		public HandleAreas (EditorCtrl ctrl, MapsPanel parent, Scene scene) : base(ctrl, parent, scene)
		{
			Setup ("areas");
		}

		protected override void Setup (string editDataParamName)
		{
			base.Setup (editDataParamName);

			if (scene.progression.targetAreas > 0)
				currentTargetAreaIndex = 1;
			UpdateCurrentArea ();
		}
		
		public override bool Render (int mx, int my)
		{
			GUILayout.BeginHorizontal(); // Area
			{
				GUILayout.Label (" Area:", GUILayout.Width (30));

				Areas[] areas = (Areas[])System.Enum.GetValues (typeof(Areas));
				foreach (Areas a in areas) 
				{
					if (GUILayout.Button (a.ToString(), (currentArea == a) ? tabSelected : tabNormal, GUILayout.Width (300f / 4f))) 
					{
						if (currentArea != a)
						{
							currentArea = a;
							UpdateCurrentArea ();
						}
					}
				}
			}
			GUILayout.EndHorizontal (); // ~Area
			GUILayout.Space (5);

			switch (currentArea)
			{
			case Areas.Succession : 
				if (!HandleSuccession ()) return false;
				else break;
			case Areas.Managed : 
				if (!HandleManaged ()) return false; 
				else break;
			case Areas.Purchasable : 
				if (!HandlePurchasable ()) return false; 
				else break;
			case Areas.Target :
				if (!HandleTarget ()) return false;
				else break;
			}

			GUILayout.Space (5);
			this.RenderBrushMode ();
			GUILayout.Space (16);
			this.RenderSaveRestoreApply ();
			GUILayout.Space (5);
			GUILayout.FlexibleSpace ();
			this.RenderFromImage (currentArea.ToString() + " Area(s)");

			return false;
		}

		void UpdateCurrentArea ()
		{
			data = GetAreaData (currentArea);

			switch (currentArea)
			{
			case Areas.Succession :
			case Areas.Managed :
			case Areas.Target :
				maxParamValue = 1;
				break;
			case Areas.Purchasable :
				// TODO: Areas.Purchasable
				maxParamValue = 10;
				break;
			}

			paramStrength = maxParamValue;
			paramStrengthStr = paramStrength.ToString();
			
			if (edit != null) edit.SetData (data);
			if (backupCopy != null) data.CopyTo (backupCopy);
		}

		private bool HandleManaged ()
		{
			return true;
		}

		private bool HandleSuccession ()
		{
			return true;
		}

		private bool HandlePurchasable ()
		{
			// TODO: The user should be able to make price classes or use the value (0...255) * cost multiplier
			GUILayout.Label ("Under construction...");

			return false;
		}

		private bool HandleTarget ()
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				EcoGUI.skipHorizontal = true;
				GUILayout.BeginHorizontal ();
				{
					EcoGUI.Foldout ("Target areas (" + scene.progression.targetAreas + ")", ref targetAreasOpened);
					GUILayout.FlexibleSpace ();
					if (currentTargetAreaIndex > 0)
					{
						GUILayout.Label ("Currently selected #" + currentTargetAreaIndex.ToString());

						GUI.enabled = (currentTargetAreaIndex > 1);
						if (GUILayout.Button ("<", GUILayout.Width (20))) {
							currentTargetAreaIndex--;
							UpdateCurrentArea ();
						}
						GUI.enabled = (currentTargetAreaIndex < scene.progression.targetAreas);
						if (GUILayout.Button (">", GUILayout.Width (20))) {
							currentTargetAreaIndex++;
							UpdateCurrentArea ();
						}
						GUI.enabled = true;
					}

					GUILayout.Space (10);
					if (GUILayout.Button ("+", GUILayout.Width (20)))
					{
						scene.progression.targetAreas++;
						scene.progression.AddData (Progression.TARGET_ID + scene.progression.targetAreas.ToString(), new BitMap8 (scene));
						currentTargetAreaIndex = scene.progression.targetAreas;
						UpdateCurrentArea ();
					}
				}
				GUILayout.EndHorizontal ();
				EcoGUI.skipHorizontal = false;

				GUILayout.Space (5);
				if (targetAreasOpened)
				{
					targetAreasScrollPos = GUILayout.BeginScrollView (targetAreasScrollPos);
					{
						for (int i = 1; i < scene.progression.targetAreas + 1; i++) 
						{
							GUILayout.BeginHorizontal ();
							{
								GUILayout.Space (2);
								if (GUILayout.Button ("Area #" + i.ToString(), GUILayout.Width (150)))
								{
									currentTargetAreaIndex = i;
									UpdateCurrentArea ();
								}
							}
							GUILayout.EndHorizontal ();
						}
					}
					GUILayout.EndScrollView ();
				}
			}
			GUILayout.EndVertical ();

			return (currentTargetAreaIndex >= 0);
		}

		private Data GetAreaData (Areas area)
		{
			switch (area)
			{
			case Areas.Succession : return scene.progression.successionArea;
			case Areas.Managed : return scene.progression.managedArea;
			case Areas.Purchasable : return scene.progression.purchasableArea;
			case Areas.Target : 
				if (currentTargetAreaIndex > 0) 
					return scene.progression.GetData (Progression.TARGET_ID + currentTargetAreaIndex.ToString());
				else
					return scene.progression.GetData (Progression.TARGET_ID + "0");
			}
			return null;
		}
	}
}

