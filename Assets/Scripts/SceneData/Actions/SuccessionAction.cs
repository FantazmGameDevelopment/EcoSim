using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneData.Action
{
	public class SuccessionAction : BasicAction
	{
		public const string XML_ELEMENT = "succession";
		private const int SLICE_SIZE = 128;
		private volatile int active_threads;
		public bool skipNormalSuccession = false;
		private Data successionArea = null;
		
		public SuccessionAction (Scene scene, int id) : base(scene, id)
		{
		}

		public SuccessionAction (Scene scene) : base(scene, scene.actions.lastId)
		{
		}
		
		public override string GetDescription ()
		{
			return "Handle Succession";
		}
		
		/**
		 * ProcessSlice is started in new thread, arguments should be of type int and is startY position
		 * of the slice.
		 */
		void ProcessSlice (object arguments)
		{
			int startY = (int)arguments;
			try 
			{
				Random rnd = new Random (); // when multithreading, you need a random generator per thread
				Progression progress = scene.progression;
				VegetationData vegetation = progress.vegetation;
				ushort[] vegData = vegetation.data;
				int p = scene.width * startY;
				for (int y = startY; y < startY + SLICE_SIZE; y++) 
				{
					for (int x = 0; x < scene.width; x++) 
					{
						if ((successionArea == null) || (successionArea.Get (x, y) > 0)) 
						{
							int vegetationInt = vegData [p];
							int successionId = (vegetationInt >> VegetationData.SUCCESSION_SHIFT) & VegetationData.SUCCESSION_MASK;
							int vegetationId = (vegetationInt >> VegetationData.VEGETATION_SHIFT) & VegetationData.VEGETATION_MASK;
							int tileId = (vegetationInt >> VegetationData.TILE_SHIFT) & VegetationData.TILE_MASK;
							
							SuccessionType s = scene.successionTypes [successionId];
							VegetationType v = s.vegetations [vegetationId];

							// First do the gradual parameter changes
							foreach (GradualParameterChange gradChange in v.gradualChanges) 
							{
								if (gradChange.chance >= rnd.NextDouble ()) 
								{
									UserInteraction ui = gradChange.action;
									AreaAction action = (ui != null) ? (ui.action as AreaAction) : null;
									if ((action == null) || (action.IsSelected (ui, x, y))) 
									{
										Data gradData = gradChange.data;
										int val = gradData.Get (x, y);
										int delta = gradChange.deltaChange;
										int newVal = val + delta;
										newVal = UnityEngine.Mathf.Clamp(newVal, gradChange.lowRange, gradChange.highRange);
										
										if (newVal != val) {
											// Only write back when value is changed
											gradData.Set (x, y, newVal);
										}
									}
								}
							} // ~End gradual parameter changes

							// Do transition rules
							foreach (VegetationRule rule in v.rules) 
							{
								if (rule.chance >= rnd.NextDouble ()) 
								{
									// We are lucky with chance, we continue with rule
									UserInteraction ui = rule.action;
									AreaAction action = (ui != null) ? (ui.action as AreaAction) : null;
									if (((action == null) && !skipNormalSuccession) || (action.IsSelected (ui, x, y))) 
									{
										// either there is no action for this rule, or this tile is selected for the action in this rule
										// we now gonna check the parameter ranges
										bool paramsOk = true; // we assume success :-)
										foreach (ParameterRange range in rule.ranges) 
										{
											int val = range.data.Get (x, y);
											if ((val < range.lowRange) || (val > range.highRange)) {
												paramsOk = false; // param out of range!
												break;
											}
										}

										if (paramsOk) 
										{
											// All conditions of rule are successful, fire rule!
											vegetationId = rule.vegetationId;
											VegetationType newVeg = rule.vegetation;
											tileId = (tileId > 0) ? (RndUtil.RndRange (ref rnd, 1, newVeg.tiles.Length)) : 0;
											
											// update parameters
											foreach (ParameterChange change in newVeg.changes) 
											{
												Data pdata = change.data;
												int val = RndUtil.RndRange (ref rnd, change.lowRange, change.highRange + 1);
												pdata.Set (x, y, val);
											}

											// Write the new vegetation back to vegetations data
											vegData [p] = (ushort)((successionId << VegetationData.SUCCESSION_SHIFT) |
											                       (vegetationId << VegetationData.VEGETATION_SHIFT) | (tileId << VegetationData.TILE_SHIFT));

											// Mark the vegetation data as changed so it will be saved
											vegetation.hasChanged = true;
											break; // break the vegetation rules loop
										}
									}
								}
							} // ~ End transition rules
						}
						p++;
					}
				}
			} catch (Exception e) {
				UnityEngine.Debug.LogException (e);
			}
			active_threads--;
			if (active_threads == 0) {
				finishedProcessing = true;
			}
		}
		
		public override void DoSuccession ()
		{
			base.DoSuccession ();

			if (successionArea == null) {
				successionArea = scene.progression.successionArea;
			}

			active_threads = scene.height / SLICE_SIZE;
			for (int y = 0; y < scene.height; y += SLICE_SIZE) {
// temp disable unreachable code warning
#pragma warning disable 162
				if (GameSettings.VEGETATION_SUCCESSION_MULTITHREADED) {
					ThreadPool.QueueUserWorkItem (ProcessSlice, y);
				}
				else {
					ProcessSlice(y);
				}
#pragma warning restore 162
			}
		}
		
		public static SuccessionAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			SuccessionAction action = new SuccessionAction (scene, id);
			string sns = reader.GetAttribute ("skipnormalsuccession");
			action.skipNormalSuccession = (sns != null) && (sns == "true");
			
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == UserInteraction.XML_ELEMENT)) {
						action.uiList.Add (UserInteraction.Load (action, reader));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			return action;
		}
		
		public override void Save (XmlTextWriter writer)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("id", id.ToString ());
			writer.WriteAttributeString ("skipnormalsuccession", skipNormalSuccession ? "true" : "false");
			
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			writer.WriteEndElement ();
		}		
	}
}
