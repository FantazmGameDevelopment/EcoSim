using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

namespace Ecosim.SceneEditor.Helpers
{
	public class HandleActionObjectGroups : HandleBuildings
	{
		private class GroupState
		{
			public ActionObjectsGroup group;
			public bool groupOpened;
			public bool objectsOpened;

			public GroupState (ActionObjectsGroup group)
			{
				this.group = group;
				groupOpened = false;
				objectsOpened = false;
			}
		}

		private List<GroupState> groupStates;
		private string newGroupName;
		private string newGroupType;

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
		private ActionObject editActionObject;
		private int editActionObjectIndex;
		private EditMode editMode;

		public HandleActionObjectGroups (EditorCtrl ctrl, MapsPanel parent, Scene scene) : base (ctrl, parent, scene)
		{
			groupStates = new List<GroupState>();
			foreach (ActionObjectsGroup g in scene.actionObjectGroups) {
				groupStates.Add (new GroupState (g));
			}

			newGroupName = "New Group";
			newGroupType = System.Enum.GetNames (typeof(ActionObjectsGroup.GroupType))[0];

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

			// Delete all buildings associated with the action objects
			foreach (ActionObject ao in group.actionObjects)
			{
				if (ao.building != null)
					EditBuildings.self.DestroyBuilding (ao.building);
			}

			scene.actionObjectGroups = list.ToArray();
			scene.UpdateReferences();

			groupStates.Remove (groupStates.Find ( g => g.group == group ));

			if (editGroup == group)
			{
				editGroup = null;
				editActionObject = null;
				editActionObjectIndex = -1;
			}
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
			// Do we have a selected group
			if (editGroup != null)
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
		}

		protected override void BuildingCreated (Buildings.Building newBuilding)
		{
			// Create Action object
			ActionObject actionObject = new ActionObject (scene, editGroup, newBuilding);
			List<ActionObject> actionObjects = new List<ActionObject>(editGroup.actionObjects);

			int highestIndex = 0;
			foreach (ActionObject obj in actionObjects)
			{
				if (obj.index > highestIndex) 
					highestIndex = obj.index;
			}
			actionObject.index = highestIndex + 1;

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
					foreach (GroupState gs in groupStates)
					{
						GUILayout.BeginVertical (ctrl.skin.box);
						{
							// Header
							GUILayout.BeginHorizontal ();
							{
								if (GUILayout.Button (gs.groupOpened ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
								{
									gs.groupOpened = !gs.groupOpened;
								}

								GUILayout.Space (2);
								GUILayout.Label (gs.group.index.ToString(), GUILayout.Width (25));
								GUILayout.Label (string.Format("[ {0} ]", gs.group.groupType.ToString()), GUILayout.Width (80));
								GUILayout.Label (gs.group.name);
								
								if (GUILayout.Button ("-", GUILayout.Width (20)))
								{
									ActionObjectsGroup tmp = gs.group;
									ctrl.StartDialog (string.Format("Delete group '{0}'?", tmp.name), 
									    newVal => { 
										DeleteGroup (tmp);
									}, null);
								}
							}
							GUILayout.EndHorizontal(); // ~Header

							if (gs.groupOpened)
							{
								//GUILayout.BeginVertical (ctrl.skin.box); // Group Body
								{
									GUILayout.Space (3);
									GUILayout.BeginHorizontal ();
									{
										if (GUILayout.Button ("Edit influence area")) 
										{
											editGroup = gs.group;
											editActionObjectIndex = -1;
											editActionObject = null;

											editMode = EditMode.InfluenceMap;
											selectedBuilding = null;

											switch (gs.group.groupType)
											{
											// Get the correct influence map
											case ActionObjectsGroup.GroupType.Combined : 
												if (handleParams != null) {
													handleParams.Disable ();
													handleParams = null;
												}

												handleParams = new HandleParameters (ctrl, parent, scene, gs.group.combinedData);
												base.BuildingClicked (null);
												break;
											
											// Try to select the first action object of the group
											case ActionObjectsGroup.GroupType.Collection : 
												if (editGroup.actionObjects.Length > 0) {
													EditActionObject (0);
												}
												break;
											}
										}

										if  (GUILayout.Button ("Edit influence rules"))
										{
											editGroup = gs.group;
											editMode = EditMode.InfluenceRules;
											selectedBuilding = null;
											base.BuildingClicked (null);

											if (handleParams != null) {
												handleParams.Disable ();
												handleParams = null;
											}
										}

										if (GUILayout.Button ("Edit action objects")) //, GUILayout.Width(120))) 
										{
											editGroup = gs.group;
											editMode = EditMode.Objects;
											selectedBuilding = null;

											if (handleParams != null) {
												handleParams.Disable ();
												handleParams = null;
											}

											EditActionObject (0);
										}
									}
									GUILayout.EndHorizontal ();
									GUILayout.Space (3);

									// Header
									GUILayout.BeginHorizontal ();
									{
										if (gs.group.actionObjects.Length > 0)
										{
											if (GUILayout.Button (gs.objectsOpened ? (ctrl.foldedOpenSmall) : (ctrl.foldedCloseSmall), ctrl.icon12x12)) 
											{
												gs.objectsOpened = !gs.objectsOpened;
											}
											GUILayout.Label ("Objects");
										} else 
										{
											GUILayout.Label (" No Objects.");
										}
									}
									GUILayout.EndHorizontal ();

									if (gs.objectsOpened) 
									{
										// Action Objects
										int aObjIndex = 0;
										foreach (ActionObject aObj in gs.group.actionObjects)
										{
											GUILayout.BeginVertical (ctrl.skin.box);
											{
												// Header
												GUILayout.BeginHorizontal ();
												{
													GUILayout.Space (2);
													GUILayout.Label ((aObjIndex++).ToString(), GUILayout.Width (40));
													if (aObj.building == null)
														Debug.LogWarning (aObj.buildingId);
													GUILayout.Label (string.Format ("'{0}' [{1}]", aObj.building.name, aObj.buildingId));
													GUILayout.FlexibleSpace ();

													if (selectedBuilding != aObj.building && editGroup != null)
													{
														if (GUILayout.Button ("Edit", GUILayout.Width (50))) 
														{
															// Same code as "Edit action objects" (almost)
															editGroup = gs.group;
															selectedBuilding = null;
															
															if (handleParams != null) {
																handleParams.Disable ();
																handleParams = null;
															}

															switch (editGroup.groupType)
															{
															case ActionObjectsGroup.GroupType.Combined :
																editMode = EditMode.Objects;
																break;

															case ActionObjectsGroup.GroupType.Collection :
																editMode = EditMode.InfluenceMap;
																EditActionObject (aObj);
																break;
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

														List<ActionObject> list = new List<ActionObject>(gs.group.actionObjects);
														list.Remove (aObj);
														gs.group.actionObjects = list.ToArray ();

														editActionObject = null;
														editActionObjectIndex = -1;
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

				GUILayout.Space (5);

				// Add button
				GUILayout.BeginHorizontal ();
				{
					GUILayout.Label (" New:", GUILayout.Width (35));
					newGroupName = GUILayout.TextField (newGroupName, GUILayout.MinWidth (100));

					GUILayout.Label (" Type:", GUILayout.Width (35));
					if (GUILayout.Button (newGroupType, GUILayout.Width (80)))
					{
						List<string> types = new List<string>(System.Enum.GetNames(typeof(ActionObjectsGroup.GroupType)));
						ctrl.StartSelection (types.ToArray(), types.IndexOf(newGroupType), delegate (int newIndex) {
							newGroupType = types[newIndex];
						});
					}

					if (GUILayout.Button ("?", GUILayout.Width (20)))
					{
						string msg = "A group with type 'Combined' will be treated as 'one' combined object. All objects in the group will simultaneously be activated/deactivated.\n\n" +
							"All objects of a group with type 'Collection' will all be treated seperately and can all be activated/deactivated on their own.";
						ctrl.StartOkDialog (msg, null);
					}

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
							g.groupType = (ActionObjectsGroup.GroupType)System.Enum.Parse (typeof(ActionObjectsGroup.GroupType), newGroupType);
							groupsScrollPos = new Vector2 (0f, Mathf.Infinity);
							groupStates.Add (new GroupState(g));
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

					GUILayout.Label ("Press N to place a new building.");
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

						if (handleParams != null) {
							handleParams.Disable ();
							handleParams = null;
						}

						EditBuildings.self.MarkBuildingSelected (selectedBuilding);
					}
					
					if (editGroup != null) 
						GUILayout.Label (string.Format("Edit influence of '{0}'", editGroup.name));
				}
				GUILayout.EndHorizontal ();
				GUILayout.Space (5f);

				if (editGroup != null)
				{
					GUILayout.BeginHorizontal ();
					{
						if (editActionObjectIndex >= 0)
						{
							if (editGroup.groupType == ActionObjectsGroup.GroupType.Collection)
							{
								GUILayout.Label (" Object:", GUILayout.Width (40));
								string editActionObjectName = string.Format ("#{0} '{1}' [{2}]", editActionObjectIndex, editActionObject.building.name, editActionObject.buildingId);
								if (GUILayout.Button (editActionObjectName))
								{
									List<string> aObjs = new List<string>();
									foreach (ActionObject aObj in editGroup.actionObjects) {
										aObjs.Add (string.Format ("#{0} '{1}' [{2}]", aObjs.Count, aObj.building.name, aObj.buildingId));
									}
									
									ActionObjectsGroup tmpEditGroup = editGroup;
									ctrl.StartSelection (aObjs.ToArray(), editActionObjectIndex, delegate(int index) {
										if (tmpEditGroup == editGroup) {
											EditActionObject (index);
										}
									});
								}

								GUILayout.Space (10);
								GUI.enabled = editActionObjectIndex > 0;
								if (GUILayout.Button ("<", GUILayout.Width (30)))
								{
									EditActionObject (editActionObjectIndex - 1);
								}
								GUI.enabled = editActionObjectIndex < editGroup.actionObjects.Length - 1;
								if (GUILayout.Button (">",  GUILayout.Width (30)))
								{
									EditActionObject (editActionObjectIndex + 1);
								}
								GUI.enabled = true;
								GUILayout.Space (5);
							}
						}

						if (editGroup.groupType == ActionObjectsGroup.GroupType.Collection && editGroup.actionObjects.Length == 0) {
							GUILayout.Label ("Objects of a 'Collection' type group have individual influence maps. Please add objects first.");
						}
					}
					GUILayout.EndHorizontal ();
				}

				if (handleParams != null) 
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
						// Render influence rules
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

		private void EditActionObject (ActionObject obj)
		{
			for (int i = 0; i < editGroup.actionObjects.Length; i++) {
				if (editGroup.actionObjects[i] == obj) 
				{
					EditActionObject (i);
					return;
				}
			}
			EditActionObject (-1);
		}

		private void EditActionObject (int index)
		{
			editActionObjectIndex = index;

			if (handleParams != null) {
				handleParams.Disable ();
				handleParams = null;
			}

			if ((editActionObjectIndex >= 0) && (editActionObjectIndex < editGroup.actionObjects.Length))
			{
				editActionObject = editGroup.actionObjects[index];

				selectedBuilding = null;
				BuildingClicked (editActionObject.building);

				switch (editMode)
				{
				case EditMode.InfluenceMap :
					handleParams = new HandleParameters (ctrl, parent, scene, editActionObject.data);
					break;
				}
			} 
			else 
			{
				editActionObject = null;
				editActionObjectIndex = -1;

				selectedBuilding = null;
				BuildingClicked (null);
			}

		}
	}
}
