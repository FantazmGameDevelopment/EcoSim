using UnityEngine;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public abstract class IAnimalPopulationModelHelper
	{
		public delegate void CreateExtraPanelDelegate (System.Type type, out ExtraPanel outPanel);

		protected SpeciesPanel panel;
		protected SpeciesPanel.AnimalState animalState;
		protected AnimalStartPopulationModel model;
		protected CreateExtraPanelDelegate onCreateExtraPanel;

		public virtual void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			this.panel = panel;
			this.animalState = animalState;
			this.onCreateExtraPanel = onCreateExtraPanel;
		}

		public abstract void Render (int mx, int my);

		protected bool RenderHeaderStart (string name, IAnimalPopulationModel.AnimalPopulationModelDataBase data, bool renderBox)
		{
			if (data.show)
			{
				if (renderBox) 	GUILayout.BeginVertical (EditorCtrl.self.skin.box);
				else 			GUILayout.BeginVertical ();
				{
					EcoGUI.skipHorizontal = true;
					GUILayout.BeginHorizontal ();
					{
						EcoGUI.Toggle ("", ref data.use, GUILayout.Width (20));
						if (data.use) 
						{
							EcoGUI.Foldout (null, ref data.opened, GUILayout.Width (20));
							GUILayout.Space (5);
						}
						else GUILayout.Space (19);
						GUILayout.Label (name);
					}
					GUILayout.EndHorizontal ();
					EcoGUI.skipHorizontal = false;
				}
				return (data.use) ? data.opened : false;
			}
			return false;
		}
		protected void RenderHeaderEnd (IAnimalPopulationModel.AnimalPopulationModelDataBase data)
		{
			if (data.show)
			{
				GUILayout.EndVertical ();
			}
		}
	}
}

/** 
 * Example implementation :

using UnityEngine;
using System.IO;
using System.Xml;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public class AnimalStartPopulationModelHelper : IAnimalPopulationModelHelper
	{
		public virtual void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			base.Setup ();
		}
	
		public virtual void Render (IAnimalPopulationModel model, int mx, int my)
		{
			AnimalStartPopulationModel m = (AnimalStartPopulationModel)model;
		}
	}
}

 */
