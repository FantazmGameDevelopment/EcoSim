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
			Purchasable 
		};
		
		private Areas currentArea;
		
		public HandleAreas (EditorCtrl ctrl, MapsPanel parent, Scene scene) : base(ctrl, parent, scene)
		{
		}

		protected override void Setup (string editDataParamName)
		{
			base.Setup ("areas");

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
					if (GUILayout.Button (a.ToString(), (currentArea == a) ? tabSelected : tabNormal, GUILayout.Width (100))) 
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
			}

			GUILayout.Space (5);
			this.RenderBrushMode ();
			GUILayout.Space (16);
			this.RenderSaveRestoreApply ();
			GUILayout.Space (16);
			this.RenderFromImage (currentArea.ToString() + " Area");

			return false;
		}

		void UpdateCurrentArea ()
		{
			data = GetAreaData (currentArea);

			switch (currentArea)
			{
			case Areas.Succession :
			case Areas.Managed :
				maxParamValue = 1;
				break;
			case Areas.Purchasable :
				// TODO:
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

		private Data GetAreaData (Areas area)
		{
			switch (area)
			{
			case Areas.Succession : return scene.progression.successionArea;
			case Areas.Managed : return scene.progression.managedArea;
			case Areas.Purchasable : return scene.progression.purchasableArea;
			}
			return null;
		}
	}
}

