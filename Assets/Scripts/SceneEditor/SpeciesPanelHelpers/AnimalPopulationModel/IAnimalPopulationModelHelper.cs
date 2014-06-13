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
