using UnityEngine;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public class AnimalStartPopulationModelHelper : IAnimalPopulationModelHelper
	{
		private AnimalStartPopulationModel model;
		private ExtraPanel extraPanel;

		public override void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			base.Setup (panel, animalState, model, onCreateExtraPanel);
			this.model = (AnimalStartPopulationModel)model;
		}
	
		public override void Render (int mx, int my)
		{
			this.onCreateExtraPanel = onCreateExtraPanel;

			if (this.model.nests.show) {
				RenderNests ();
			}
		}

		private void RenderNests ()
		{
			GUILayout.BeginVertical ();//EditorCtrl.self.skin.box);
			{
				GUILayout.Label (" Start Population");

				if (this.RenderHeaderStart ("Nests", this.model.nests, true))
				{
					GUILayout.BeginHorizontal ();
					{
						if (GUILayout.Button ("Open Nests Editor")) 
						{
							if (this.onCreateExtraPanel != null) {
								ExtraPanel newPanel;
								this.onCreateExtraPanel (typeof(AnimalNestsExtraPanel), out newPanel);
								this.extraPanel = (AnimalNestsExtraPanel)newPanel;
							}
						}
						
						// TODO: Open extra panel for Animal rules
						/*if (GUILayout.Button ("Open Rules"))
						{
						}*/
					}
					GUILayout.EndHorizontal();
					
					// Nests
					if (this.model.nests.nests.Length > 0)
					{
						// Nests list
						GUILayout.BeginHorizontal ();
						{
							if (GUILayout.Button (animalState.nestsListFoldedOpen ? (panel.ctrl.foldedOpenSmall) : (panel.ctrl.foldedCloseSmall), panel.ctrl.icon12x12)) {
								animalState.nestsListFoldedOpen = !animalState.nestsListFoldedOpen;
							}
							GUILayout.Label ("Nests", GUILayout.Width (40));
						}
						GUILayout.EndHorizontal ();
						
						if (animalState.nestsListFoldedOpen)
						{
							int idx = 0;
							foreach (AnimalStartPopulationModel.Nests.Nest n in this.model.nests.nests)
							{
								GUILayout.BeginHorizontal (panel.ctrl.skin.box);
								{
									GUILayout.Label (string.Format(" Nest #{0} ({1},{2})", idx++, n.x, n.y));
									
									AnimalNestsExtraPanel animExtraPanel = (AnimalNestsExtraPanel)panel.extraPanel;
									if (animExtraPanel != null)
									{
										if (GUILayout.Button ("Edit", GUILayout.Width(50))) 
										{
											animExtraPanel.SetAnimal (animalState.animal);
											animExtraPanel.EditNest (n);
										}
										GUILayout.Space (2);
										if (GUILayout.Button ("Focus", GUILayout.Width(50))) 
										{
											animExtraPanel.SetAnimal (animalState.animal);
											animExtraPanel.FocusOnNest (n);
										}
									}
									
									GUILayout.Space (2);
									if (GUILayout.Button ("-", GUILayout.Width (20))) 
									{
										if (animExtraPanel != null) {
											animExtraPanel.DeleteNest (n);
										}

										List<AnimalStartPopulationModel.Nests.Nest> nests = new List<AnimalStartPopulationModel.Nests.Nest> (this.model.nests.nests);
										nests.Remove (n);
										this.model.nests.nests = nests.ToArray ();
									}
								}
								GUILayout.EndHorizontal ();
							}
							//GUILayout.FlexibleSpace ();
						}
					}
				}
				this.RenderHeaderEnd (this.model.nests);
			}
			GUILayout.EndVertical ();
		}

		public void Dispose ()
		{

		}
	}
}
