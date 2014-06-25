using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ecosim.SceneData;
using Ecosim.SceneData.AnimalPopulationModel;
using Ecosim.SceneEditor;
using Ecosim.SceneEditor.Helpers;

namespace Ecosim.SceneEditor.Helpers.AnimalPopulationModel
{
	public class AnimalPopulationDecreaseModelHelper : IAnimalPopulationModelHelper
	{
		private AnimalPopulationDecreaseModel model;
		
		public override void Setup (SpeciesPanel panel, SpeciesPanel.AnimalState animalState, IAnimalPopulationModel model, CreateExtraPanelDelegate onCreateExtraPanel)
		{
			base.Setup (panel, animalState, model, onCreateExtraPanel);
			this.model = (AnimalPopulationDecreaseModel)model;
		}
		
		public override void Render (int mx, int my)
		{
			GUILayout.BeginVertical ();//EditorCtrl.self.skin.box);
			{
				GUILayout.Label (" Population Decrease");

				if (this.RenderHeaderStart ("Fixed", this.model.fixedNumber, true))
				{
					// Type
					EcoGUI.EnumButton<AnimalPopulationDecreaseModel.FixedNumber.Types>("Type", model.fixedNumber.type, 
					                                                                delegate(AnimalPopulationDecreaseModel.FixedNumber.Types newType) {
						model.fixedNumber.type = newType;
					}, GUILayout.Width (50), GUILayout.Width (150)); 
					
					// Type
					GUILayout.BeginHorizontal ();
					{
						GUILayout.Space (2);
						switch (this.model.fixedNumber.type)
						{
						case AnimalPopulationDecreaseModel.FixedNumber.Types.Absolute :
							EcoGUI.IntField ("Value", ref this.model.fixedNumber.absolute, 50, 60);
							break;

						case AnimalPopulationDecreaseModel.FixedNumber.Types.Relative :
							EcoGUI.skipHorizontal = true;
							GUILayout.BeginHorizontal ();
							{
								EcoGUI.FloatField ("Value", ref this.model.fixedNumber.relative, 2, 50, 60);
								float relVal = this.model.fixedNumber.relative;
								relVal = GUILayout.HorizontalSlider (relVal, 0f, 1f, GUILayout.Width (60));
								this.model.fixedNumber.relative = Mathf.Clamp (relVal, 0f, 1f);
							}
							GUILayout.EndHorizontal ();
							EcoGUI.skipHorizontal = false;
							break;
						}
					}
					GUILayout.EndHorizontal ();
				}
				this.RenderHeaderEnd (this.model.fixedNumber);

				if (this.RenderHeaderStart ("Specified", this.model.specifiedNumber, true))
				{
					if (this.RenderHeaderStart ("Natural death rate", this.model.specifiedNumber.naturalDeathRate, true))
					{
						GUILayout.Space (2);
						EcoGUI.RangeSliders ("Rate", 
						                     ref this.model.specifiedNumber.naturalDeathRate.minDeathRate,
						                     ref this.model.specifiedNumber.naturalDeathRate.maxDeathRate,
						                     0f, 1f,
						                     GUILayout.Width (40),
						                     GUILayout.Width (60));
					}
					this.RenderHeaderEnd (this.model.specifiedNumber.naturalDeathRate);

					if (this.RenderHeaderStart ("Starvation", this.model.specifiedNumber.starvation, true))
					{
						GUILayout.Space (2);
						EcoGUI.IntField ("Required food", ref this.model.specifiedNumber.starvation.foodRequiredPerAnimal, 100, 50);

						EcoGUI.RangeSliders ("Rate", 
						                     ref this.model.specifiedNumber.starvation.minStarveRate,
						                     ref this.model.specifiedNumber.starvation.maxStarveRate,
						                     0f, 1f,
						                     GUILayout.Width (40),
						                     GUILayout.Width (60));
					}
					this.RenderHeaderEnd (this.model.specifiedNumber.starvation);

					// TODO: Add "?" to explain the starve rate starvation = (starvation chance) * (starve rate)

					if (this.RenderHeaderStart ("Human induced death", this.model.specifiedNumber.artificialDeath, true))
					{
						GUILayout.Space (2);
						GUILayout.Label ("Entries");

						foreach (AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry ade in 
						         this.model.specifiedNumber.artificialDeath.entries)
						{
							EcoGUI.skipHorizontal = true;
							GUILayout.BeginHorizontal ();
							{
								EcoGUI.FoldoutEditableName (ref ade.name, ref ade.opened);
								if (GUILayout.Button ("-", GUILayout.Width (20)))
								{
									this.model.specifiedNumber.artificialDeath.RemoveEntry (ade);
									break;
								}
							}
							GUILayout.EndHorizontal ();
							EcoGUI.skipHorizontal = false;

							if (ade.opened)
							{
								GUILayout.Space (4);

								GUILayout.BeginHorizontal ();
								{
									GUILayout.Label ("Parameter", GUILayout.Width (80));
									if (GUILayout.Button (ade.parameterName))
									{
										List<string> items = panel.scene.progression.GetAllDataNames ();
										if (items.Count > 0)
										{
											AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry tmpAde = ade;
											int selected = Mathf.Max (0, items.IndexOf (ade.parameterName));
											EditorCtrl.self.StartSelection (items.ToArray(), selected, delegate(int index) {
												tmpAde.parameterName = items [index];
											});
										}
									}
								}
								GUILayout.EndHorizontal ();

								EcoGUI.EnumButton <AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry.Types>
								(
									"Type", 
									ade.type, 
									delegate(AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry.Types newType) {
										ade.type = newType;
									}, 
									GUILayout.Width (80),
									GUILayout.Width (140)
								);

								switch (ade.type)
								{
								case AnimalPopulationDecreaseModel.SpecifiedNumber.ArtificialDeath.ArtificialDeathEntry.Types.FixedChance:
									//EcoGUI.RangeSliders ("Chance range", ref ade.fixedChance.min, ref ade.fixedChance.max, 0f, 1f, GUILayout.Width (80), GUILayout.Width (40)); TODO: Fix?
									break;
								}
								GUILayout.Space (4);
							}
							GUILayout.Space (2);
						}

						if (GUILayout.Button ("+", GUILayout.Width (20)))
						{
							this.model.specifiedNumber.artificialDeath.AddEntry ("New entry");
						}
					}
					this.RenderHeaderEnd (this.model.specifiedNumber.artificialDeath);
				}
				this.RenderHeaderEnd (this.model.specifiedNumber);
			}
			GUILayout.EndVertical ();
		}
	}
}
