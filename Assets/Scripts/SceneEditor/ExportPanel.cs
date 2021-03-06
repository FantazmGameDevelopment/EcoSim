using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim.SceneData.VegetationRules;
using Ecosim;

namespace Ecosim.SceneEditor
{
	public class ExportPanel : Panel
	{
		public Scene scene;
		public EditorCtrl ctrl;
		public ExportMgr mgr;

		private bool graphOpened;

		private bool sheetOpened;
		private bool selectionOpened;
		private bool targetAreasOpened;
		private bool paramsOpened;
		private bool animalsOpened;
		private bool plantsOpened;

		public void Setup (EditorCtrl ctrl, Scene scene) 
		{ 
			this.ctrl = ctrl;
			this.scene = scene;
			if (scene == null)
				return;
		}

		public bool Render (int mx, int my) 
		{ 
			if (this.scene == null)
				return false;

			if (this.mgr == null)
				this.mgr = scene.exporter;

			GUILayout.BeginVertical ();
			{
				RenderGraph (mx, my);
				GUILayout.Space (2);
				RenderSheet (mx, my);
				GUILayout.FlexibleSpace ();
			}
			GUILayout.EndVertical ();
			return false;
		}

		private void RenderGraph (int mx, int my) 
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				EcoGUI.Foldout ("Graph export", ref graphOpened);
				if (graphOpened)
				{
					EcoGUI.Toggle ("Graph Export enabled", ref mgr.graphExportEnabled);
					if (mgr.graphExportEnabled)
					{
						EcoGUI.EnumButton<ExportMgr.GraphCostTypes>("Cost type:", mgr.graphCostType, OnGraphCostTypeChanged, 80f, 150f);
						if (mgr.graphCostType != ExportMgr.GraphCostTypes.None) {
							EcoGUI.IntField ("Price:", ref mgr.graphCosts, 80f, 150f);
						}
					}
				}
			}
			GUILayout.EndVertical ();
		}

		private void RenderSheet (int mx, int my) 
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				EcoGUI.Foldout ("Sheet export", ref sheetOpened);
				if (sheetOpened)
				{
					EcoGUI.Toggle ("Data Export enabled", ref mgr.exportEnabled);
					if (mgr.exportEnabled)
					{
						EcoGUI.EnumButton<ExportMgr.SelectionTypes>("Selection type:", mgr.selectionType, OnSelectionTypeChanged, 80f, 150f);
						EcoGUI.EnumButton<ExportMgr.DataTypes>("Data type:", mgr.dataType, OnDataTypeChanged, 80f, 150f);
						EcoGUI.EnumButton<ExportMgr.CostTypes>("Cost type:", mgr.costType, OnCostTypeChanged, 80f, 150f);
						if (mgr.costType != ExportMgr.CostTypes.None) {
							EcoGUI.IntField ("Price:", ref mgr.costs, 80f, 150f);
						}

						EcoGUI.Toggle ("Export succession type", ref mgr.exportSuccessionTypes);
						EcoGUI.Toggle ("Export vegetation type", ref mgr.exportVegetationTypes);
						
						switch (mgr.selectionType)
						{
						case ExportMgr.SelectionTypes.Selection :
							RenderSelectionType (mgr, mx, my);
							break;
						}
					}
				}
			}
			GUILayout.EndVertical ();
		}

		private void RenderSelectionType (ExportMgr mgr, int mx, int my)
		{
			GUILayout.Space (5f);
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				EcoGUI.Foldout ("Selection", ref selectionOpened); 
				GUILayout.Space (2f);

				if (selectionOpened)
				{
					GUILayout.BeginVertical (ctrl.skin.box);
					{
						GUILayout.BeginHorizontal ();
						{
							EcoGUI.Foldout ("Target areas", ref targetAreasOpened);
							GUILayout.FlexibleSpace ();
							if (GUILayout.Button ("+", GUILayout.Width (20f))) 
							{
								GUILayout.Space (3f);

								List<string> targetAreas = new List<string> ();
								for (int i = 0; i < scene.progression.targetAreas; i++) {
									targetAreas.Add ((i + 1).ToString());
								}

								foreach (int area in mgr.targetAreas) {
									targetAreas.Remove (area.ToString());
								}
								
								if (targetAreas.Count > 0) {
									ctrl.StartSelection (targetAreas.ToArray(), 0, delegate(int index, string result) {
										mgr.AddTargetArea (int.Parse(result));
									});
								} else {
									ctrl.StartOkDialog ("No target areas to add.", null);
								}
							}
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (2f);
						
						if (targetAreasOpened)
						{
							int idx = 0;
							foreach (int area in mgr.targetAreas)
							{
								GUILayout.BeginHorizontal ();
								{
									GUILayout.Space (5f);
									if (GUILayout.Button ("-", GUILayout.Width (20f))) {
										mgr.RemoveTargetArea (area);
										break;
									}
									GUILayout.Space (5f);
									GUILayout.Label ("Target area " + area);
								}
								GUILayout.EndHorizontal ();
							}
						}
					}
					GUILayout.EndVertical ();

					GUILayout.BeginVertical (ctrl.skin.box);
					{
						GUILayout.BeginHorizontal ();
						{
							EcoGUI.Foldout ("Parameters", ref paramsOpened);
							GUILayout.FlexibleSpace ();
							if (GUILayout.Button ("+", GUILayout.Width (20f))) 
							{
								GUILayout.Space (3f);

								List<string> names = scene.progression.GetAllDataNames (false);
								foreach (string param in mgr.parameters) {
									names.Remove (param);
								}

								if (names.Count > 0) {
									ctrl.StartSelection (names.ToArray(), 0, delegate(int index, string result) {
										mgr.AddParameter (result);
									});
								} else {
									ctrl.StartOkDialog ("No parameters to add.", null);
								}
							}
						}
						GUILayout.EndHorizontal ();
						GUILayout.Space (2f);

						if (paramsOpened)
						{
							int idx = 0;
							foreach (string param in mgr.parameters)
							{
								GUILayout.BeginHorizontal ();
								{
									GUILayout.Space (5f);
									if (GUILayout.Button ("-", GUILayout.Width (20f))) {
										mgr.RemoveParameter (param);
										break;
									}
									GUILayout.Space (5f);
									GUILayout.Label (param);
								}
								GUILayout.EndHorizontal ();
							}
						}
					}
					GUILayout.EndVertical ();

					// Exception time! When we only show data when OnlyWhenSurveyed
					// then the data of animals and plants will NEVER show up,
					// so we won't have to make a selection for them. The data only shows up
					// and will only be available when using a survey/inventarisation.
					if (mgr.dataType == ExportMgr.DataTypes.Always)
					{
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							GUILayout.BeginHorizontal ();
							{
								EcoGUI.Foldout ("Animals", ref animalsOpened);
								GUILayout.FlexibleSpace ();

								if (scene.animalTypes.Length > 0 && GUILayout.Button ("+", GUILayout.Width (20f))) 
								{
									GUILayout.Space (3f);
									
									List<string> names = new List<string> ();
									foreach (AnimalType t in scene.animalTypes) {
										names.Add (t.name);
									}

									foreach (string animal in mgr.animals) {
										names.Remove (animal);
									}
									
									if (names.Count > 0) {
										ctrl.StartSelection (names.ToArray(), 0, delegate(int index, string result) {
											mgr.AddAnimal (result);
										});
									} else {
										ctrl.StartOkDialog ("No animals to add.", null);
									}
								}
							}
							GUILayout.EndHorizontal ();
							GUILayout.Space (2f);

							if (animalsOpened)
							{
								int idx = 0;
								foreach (string animal in mgr.animals)
								{
									GUILayout.BeginHorizontal ();
									{
										GUILayout.Space (5f);
										if (GUILayout.Button ("-", GUILayout.Width (20f))) {
											mgr.RemoveAnimal (animal);
											break;
										}
										GUILayout.Space (5f);
										GUILayout.Label (animal);
									}
									GUILayout.EndHorizontal ();
								}

								if (scene.animalTypes.Length == 0) {
									GUILayout.Label ("No animals to add.");
								}
							}
						}
						GUILayout.EndVertical ();

						GUILayout.BeginVertical (ctrl.skin.box);
						{
							GUILayout.BeginHorizontal ();
							{
								EcoGUI.Foldout ("Plants", ref plantsOpened);
								GUILayout.FlexibleSpace ();
								if (scene.plantTypes.Length > 0 && GUILayout.Button ("+", GUILayout.Width (20f))) 
								{
									GUILayout.Space (3f);
									
									List<string> names = new List<string> ();
									foreach (PlantType t in scene.plantTypes) {
										names.Add (t.name);
									}
									
									foreach (string plant in mgr.plants) {
										names.Remove (plant);
									}
									
									if (names.Count > 0) {
										ctrl.StartSelection (names.ToArray(), 0, delegate(int index, string result) {
											mgr.AddPlant (result);
										});
									} else {
										ctrl.StartOkDialog ("No plants to add.", null);
									}
								}
							}
							GUILayout.EndHorizontal ();
							GUILayout.Space (2f);

							if (plantsOpened)
							{
								int idx = 0;
								foreach (string plant in mgr.plants)
								{
									GUILayout.BeginHorizontal ();
									{
										GUILayout.Space (5f);
										if (GUILayout.Button ("-", GUILayout.Width (20f))) {
											mgr.RemovePlant (plant);
											break;
										}
										GUILayout.Space (5f);
										GUILayout.Label (plant);
									}
									GUILayout.EndHorizontal ();
								}
								
								if (scene.plantTypes.Length == 0) {
									GUILayout.Label ("No plants to add.");
								}
							}
						}
						GUILayout.EndVertical ();
					}
				}
			}
			GUILayout.EndVertical ();
		}

		private void OnGraphCostTypeChanged (ExportMgr.GraphCostTypes newType)
		{
			scene.exporter.graphCostType = newType;
		}

		private void OnSelectionTypeChanged (ExportMgr.SelectionTypes newType)
		{
			scene.exporter.selectionType = newType;
		}

		private void OnCostTypeChanged (ExportMgr.CostTypes newType)
		{
			scene.exporter.costType = newType;
		}

		private void OnDataTypeChanged (ExportMgr.DataTypes newType)
		{
			scene.exporter.dataType = newType;
		}
		
		public void RenderExtra (int mx, int my) { }
		
		public void RenderSide (int mx, int my) { }
		
		public bool NeedSidePanel () { return false; }
		
		public bool IsAvailable () { return (scene != null); }
		
		public void Activate () { }
		
		public void Deactivate () { }
		
		public void Update () { }
	}
}
