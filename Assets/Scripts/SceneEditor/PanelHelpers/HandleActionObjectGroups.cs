using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleActionObjectGroups : HandleBuildings
	{
		private Dictionary<ActionObjectsGroup, bool> groupsOpenState;
		private Dictionary<ActionObjectsGroup, bool> objectsOpenState;
		private string newGroupName;

		private Vector2 groupsScrollPos;
		private Vector2 editObjectsScrollPos;
		private Vector2 influenceRulesScrollPos;

		private enum EditMode
		{
			InfluenceMap,
			InfluenceRules,
			Objects
		}

		private ActionObjectsGroup editGroup;
		private EditMode editMode;

		public HandleActionObjectGroups (EditorCtrl ctrl, MapsPanel parent, Scene scene) : base (ctrl, parent, scene)
		{
			groupsOpenState = new Dictionary<ActionObjectsGroup, bool>();
			objectsOpenState = new Dictionary<ActionObjectsGroup, bool>();
			newGroupName = "New Group";

			groupsScrollPos = Vector2.zero;
			editObjectsScrollPos = Vector2.zero;
			influenceRulesScrollPos = Vector2.zero;
		}
		
		protected override void Setup ()
		{
			renderTex = new Texture2D ((int)(380 * 0.5f), (int)(300 * 0.5f), TextureFormat.RGBA32, false);

			base.Setup ();
		}

		public void DeleteGroup (ActionObjectsGroup group)
		{
			List<ActionObjectsGroup> list = new List<ActionObjectsGroup>(scene.actionObjectGroups);
			list.Remove (group);
			scene.progression.DeleteData (group.dataName);

			// Delete all buildings associated with the action objects
			foreach (ActionObject ao in group.actionObjects)
			{
				if (ao.building != null)
					EditBuildings.self.DestroyBuilding (ao.building);
			}

			scene.actionObjectGroups = list.ToArray();
			scene.UpdateReferences();
			
			groupsOpenState.Remove (group);
			objectsOpenState.Remove (group);
		}

		protected override void RenderRenderedTexture ()
		{
			GUILayout.BeginHorizontal (ctrl.skin.box);
			{
				GUILayout.Label (renderTex, GUIStyle.none);
			}
			GUILayout.EndVertical ();
		}

		public override void Update ()
		{
			if (editMode == null || editMode == EditMode.InfluenceMap) return;

			base.Update ();
		}

		protected override void BuildingClicked (Buildings.Building building)
		{
			// Check if it's a action object
			foreach (ActionObject actionObject in editGroup.actionObjects) 
			{
				// It's a building of an action object
				if (actionObject.building == building) 
				{
					base.BuildingClicked (building);
				}
			}
		}

		protected override void BuildingCreated (Buildings.Building newBuilding)
		{
			// Create Action object
			ActionObject actionObject = new ActionObject (editGroup, newBuilding);
			List<ActionObject> actionObjects = new List<ActionObject>(editGroup.actionObjects);
			actionObjects.Add (actionObject);
			editGroup.actionObjects = actionObjects.ToArray();

			base.BuildingCreated (newBuilding);
		}

		public override bool Render (int mx, int my)
		{
			groupsScrollPos = GUILayout.BeginScrollView (groupsScrollPos, ctrl.skin.box, ((editGroup != null) ? GUILayout.Height (Screen.height * 0.4f) : GUILayout.MinHeight (0f)));
			{
				if (scene.actionObjectGroups.Length > 0)
				{
					foreach (ActionObjectsGroup group in scene.actionObjectGroups)
					{
						bool opened = false;
						if (!groupsOpenState.TryGetValue (group, out opened)) {
							groupsOpenState.Add (group, opened);
						}

						GUILayout.BeginVertical (ctrl.skin.box);
						{
							// Header
							GUILayout.BeginHorizontal ();
							{
								if (GUILayout.Button (opened ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
								{
									opened = !opened;
									groupsOpenState[group] = opened;
								}
								
								GUILayout.Label (group.index.ToString(), GUILayout.Width (40));
								//group.name = GUILayout.TextField (group.name);
								GUILayout.Label (group.name);
								
								if (GUILayout.Button ("-", GUILayout.Width (20)))
								{
									ActionObjectsGroup tmp = group;
									ctrl.StartDialog (string.Format("Delete group '{0}'?", tmp.name), 
									    newVal => { 
										DeleteGroup (tmp);
									}, null);
								}
							}
							GUILayout.EndHorizontal(); // ~Header

							if (opened)
							{
								//GUILayout.BeginVertical (ctrl.skin.box); // Group Body
								{
									GUILayout.Space (3);
									GUILayout.BeginHorizontal ();
									{
										if (GUILayout.Button ("Edit influence area")) 
										{
											editGroup = group;
											editMode = EditMode.InfluenceMap;
											selectedBuilding = null;

											if (handleParams != null) {
												handleParams.Disable ();
												handleParams = null;
											}
											Data influenceMap = scene.progression.GetData (group.dataName);
											handleParams = new HandleParameters (ctrl, parent, scene, influenceMap);
										}

										if  (GUILayout.Button ("Edit influence rules"))
										{
											editGroup = group;
											editMode = EditMode.InfluenceRules;
											selectedBuilding = null;

											if (handleParams != null) {
												handleParams.Disable ();
												handleParams = null;
											}
										}

										if (GUILayout.Button ("Edit action objects")) //, GUILayout.Width(120))) 
										{
											editGroup = group;
											editMode = EditMode.Objects;
											selectedBuilding = null;
											
											if (handleParams != null) {
												handleParams.Disable ();
												handleParams = null;
											}
										}
									}
									GUILayout.EndHorizontal ();
									GUILayout.Space (3);

									bool objOpened = false;
									if (!objectsOpenState.TryGetValue (group, out objOpened)) {
										objectsOpenState.Add (group, objOpened);
									}

									// Header
									GUILayout.BeginHorizontal ();
									{
										if (GUILayout.Button (objOpened ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
										{
											objOpened = !objOpened;
											objectsOpenState[group] = objOpened;
										}
										GUILayout.Label ("Objects");
									}
									GUILayout.EndHorizontal ();

									if (objOpened) 
									{
										// Action Objects
										int aObjIndex = 0;
										foreach (ActionObject aObj in group.actionObjects)
										{
											GUILayout.BeginVertical (ctrl.skin.box);
											{
												// Header
												GUILayout.BeginHorizontal ();
												{
													GUILayout.Label ((aObjIndex++).ToString(), GUILayout.Width (40));
													GUILayout.Label (string.Format ("'{0}' [{1}]", aObj.building.name, aObj.buildingId));
													GUILayout.FlexibleSpace ();
													if (selectedBuilding != aObj.building && editGroup != null)
													{
														if (GUILayout.Button ("Edit", GUILayout.Width (50))) 
														{
															// Same code as "Edit action objects"
															editGroup = group;
															editMode = EditMode.Objects;
															selectedBuilding = null;
															
															if (handleParams != null) {
																handleParams.Disable ();
																handleParams = null;
															}

															BuildingClicked (aObj.building);
														}
													}
													if (CameraControl.IsNear)
													{
														if (GUILayout.Button ("Focus", GUILayout.Width (50))) 
														{
															GameObject go = EditBuildings.self.GetGameObjectForBuilding (aObj.building);
															if (go != null) {
																CameraControl.FocusOnPosition (go.transform.position);
															} else {
																ctrl.StartOkDialog (string.Format("Could not find the instance of '{0} ({1})'. It might be that the part of the terrain it's located is not rendered or visible.", aObj.building.name, aObj.buildingId), null);
															}
														}
													}

													if (GUILayout.Button ("-", GUILayout.Width (20)))
													{
														if (aObj.building == selectedBuilding)
															EditBuildings.self.MarkBuildingSelected (null);
														EditBuildings.self.DestroyBuilding (aObj.building);

														List<ActionObject> list = new List<ActionObject>(group.actionObjects);
														list.Remove (aObj);
														group.actionObjects = list.ToArray ();
														break;
													}
												}
												GUILayout.EndHorizontal(); // ~Header
											}
											GUILayout.EndVertical ();
										}
									}
								}
								//GUILayout.EndVertical (); // ~Group Body
							}
						}
						GUILayout.EndVertical ();
					}
					//GUILayout.FlexibleSpace ();
				}

				// Add button
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label (" New group:", GUILayout.Width (80));
					newGroupName = GUILayout.TextField (newGroupName, GUILayout.Width (160));
					GUILayout.FlexibleSpace ();
					if (GUILayout.Button ("Create")) 
					{
						// Check name
						bool uniqueName = true;
						foreach (ActionObjectsGroup g in scene.actionObjectGroups) {
							if (g.name == newGroupName) {
								uniqueName = false;
								break;
							}
						}
						
						if (uniqueName)
						{
							// Add group
							ActionObjectsGroup g = new ActionObjectsGroup (scene, newGroupName);
							groupsScrollPos = new Vector2 (0f, Mathf.Infinity);
						}
						else
						{
							ctrl.StartOkDialog ("Name is already taken, please choose another and try again.", null);
						}
					}
					
					GUILayout.FlexibleSpace ();
				}
				GUILayout.EndHorizontal (); // ~Add button
			}
			GUILayout.EndScrollView ();

			if (editGroup != null)
			{
				switch (editMode)
				{
				case EditMode.Objects :
					RenderEditObjects ();
					break;

				case EditMode.InfluenceMap :
					RenderEditInfluenceMap (mx, my);
					break;

				case EditMode.InfluenceRules :
					RenderEditInfluenceRules (mx, my);
					break;
				}
			}

			return false;
		}

		private void RenderEditObjects ()
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					if (GUILayout.Button (ctrl.foldedOpenSmall, GUILayout.Width (20))) 
					{
						editGroup = null;
						selectedBuilding = null;
						EditBuildings.self.MarkBuildingSelected (selectedBuilding);
					}
					
					if (editGroup != null) 
						GUILayout.Label (string.Format("Edit objects of '{0}'", editGroup.name));
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5f);
				
				if (editGroup != null)
				{
					// Edit objects
					editObjectsScrollPos = GUILayout.BeginScrollView (editObjectsScrollPos, GUILayout.MaxHeight (Screen.height));
					{
						RenderCategory ();
						RenderBuilding ();
						RenderRenderedTexture ();
						RenderTransformControls ();
					}
					GUILayout.EndScrollView ();
				}
			}
			GUILayout.EndVertical ();
		}

		private void RenderEditInfluenceMap (int mx, int my)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					if (GUILayout.Button (ctrl.foldedOpenSmall, GUILayout.Width (20))) 
					{
						editGroup = null;
						selectedBuilding = null;
						EditBuildings.self.MarkBuildingSelected (selectedBuilding);
					}
					
					if (editGroup != null) 
						GUILayout.Label (string.Format("Edit influence of '{0}'", editGroup.name));
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5f);
				
				handleParams.Render (mx, my);
			}
			GUILayout.EndVertical ();
		}

		private void RenderEditInfluenceRules (int mx, int my)
		{
			GUILayout.BeginVertical (ctrl.skin.box);
			{
				GUILayout.BeginHorizontal ();
				{
					if (GUILayout.Button (ctrl.foldedOpenSmall, GUILayout.Width (20))) 
					{
						editGroup = null;
						selectedBuilding = null;
						EditBuildings.self.MarkBuildingSelected (selectedBuilding);
					}
					
					if (editGroup != null) 
						GUILayout.Label (string.Format("Edit influence rules of '{0}'", editGroup.name));
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5f);

				influenceRulesScrollPos = GUILayout.BeginScrollView (influenceRulesScrollPos);
				{
					if (editGroup != null)
					{
						// TODO: Render influence rules
						foreach (ActionObjectInfluenceRule rule in editGroup.influenceRules) 
						{
							GUILayout.BeginVertical (ctrl.skin.box);
							{
								bool removeRule = false;
								GUILayout.BeginHorizontal ();
								{
									switch (rule.valueType)
									{
									case ActionObjectInfluenceRule.ValueType.Range :
										GUILayout.Label ("If value is between ( ");
										EcoGUI.RangeSliders ("", ref rule.lowRange, ref rule.highRange, 0f, 1f, null, GUILayout.Width (70));
										GUILayout.Label (" )", GUILayout.Width (20));
										break;
										
									case ActionObjectInfluenceRule.ValueType.Value :
										GUILayout.Label ("If value is ");

										rule.lowRange = GUILayout.HorizontalSlider (rule.lowRange, 0f, 1f, GUILayout.Width (160));
										int value = (int)(rule.lowRange * 255f);
										EcoGUI.IntField ("", ref value, null, GUILayout.Width (50));
										rule.lowRange = Mathf.Clamp ((float)value / 255f, 0f, 1f);
										rule.highRange = rule.lowRange;
										break;
									}

									GUILayout.FlexibleSpace ();
									if (GUILayout.Button ("-", GUILayout.Width (20))) {
										removeRule = true;
									}
								}
								GUILayout.EndHorizontal ();

								// Should we remove the rule?
								if (removeRule)
								{
									List<ActionObjectInfluenceRule> list = new List<ActionObjectInfluenceRule>(editGroup.influenceRules);
									list.Remove (rule);
									editGroup.influenceRules = list.ToArray();
									break;
								}

								GUILayout.BeginHorizontal ();
								{
									GUILayout.Label ("then", GUILayout.Width (25));
									if (GUILayout.Button (rule.paramName, GUILayout.Width (120)))
									{
										List<string> dataNames = scene.progression.GetAllDataNames();
										for (int i = dataNames.Count - 1; i >= 0; i--) {
											if (dataNames[i].StartsWith("_"))
												dataNames.RemoveAt (i);
										}

										ActionObjectInfluenceRule tmpRule = rule;
										ctrl.StartSelection (dataNames.ToArray(), Mathf.Max(dataNames.IndexOf (tmpRule.paramName), 0), 
										                     newIndex => { tmpRule.paramName = dataNames[newIndex]; });
									}

									// Math type
									string mathType = "";
									int decimals = 0;
									switch (rule.mathType)
									{
									case ActionObjectInfluenceRule.MathTypes.Equals : 
										mathType = "="; 
										break;

									case ActionObjectInfluenceRule.MathTypes.Minus : 
										mathType = "-"; 
										break;

									case ActionObjectInfluenceRule.MathTypes.Plus : 
										mathType = "+";
										break;

									case ActionObjectInfluenceRule.MathTypes.Multiply : 
										mathType = "*"; 
										decimals = 2;
										break;
									}

									if (GUILayout.Button (mathType, GUILayout.Width (30)))
									{
										List<string> mathTypesAbbr = new List<string>() { "=","+","-","*" };
										ctrl.StartSelection (mathTypesAbbr.ToArray(), mathTypesAbbr.IndexOf (mathType), newIndex => 
										{ 
											string[] mathTypes = new string[] { "Equals","Plus","Minus","Multiply" };
											rule.mathType = (ActionObjectInfluenceRule.MathTypes)
												System.Enum.Parse (typeof(ActionObjectInfluenceRule.MathTypes), mathTypes[newIndex]); 
										});
									}

									EcoGUI.FloatField ("", ref rule.mathValue, decimals);//, null, GUILayout.Width (40)); 
								}
								GUILayout.EndHorizontal ();
							}
							GUILayout.EndVertical ();
						}

						// Add new rule
						bool addRule = false;
						ActionObjectInfluenceRule.ValueType valueType = ActionObjectInfluenceRule.ValueType.Range;

						GUILayout.BeginHorizontal ();
						{
							if (GUILayout.Button ("Add value rule")) {
								valueType = ActionObjectInfluenceRule.ValueType.Value;
								addRule = true;
							}

							if (GUILayout.Button ("Add range rule")) {
								valueType = ActionObjectInfluenceRule.ValueType.Range;
								addRule = true;
							}
						}
						GUILayout.EndHorizontal ();

						if (addRule)
						{
							List<ActionObjectInfluenceRule> list = new List<ActionObjectInfluenceRule>(editGroup.influenceRules);
							ActionObjectInfluenceRule newRule = new ActionObjectInfluenceRule ();

							list.Add (newRule);
							newRule.valueType = valueType;
							try {
								newRule.paramName = scene.progression.GetAllDataNames ()[0];
								newRule.UpdateReferences (scene);
							} catch { }

							editGroup.influenceRules = list.ToArray ();
						}
					}
				}
				GUILayout.EndScrollView ();
			}
			GUILayout.EndVertical ();
		}
	}
}
