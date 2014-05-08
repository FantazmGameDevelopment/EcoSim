using System.Collections.Generic;
using System.IO;
using System.Xml;

using Color32 = UnityEngine.Color32;

using Ecosim;
using Ecosim.SceneData.Action;
using Ecosim.SceneData.VegetationRules;

namespace Ecosim.SceneData
{
	/**
	 * Vegation of a terrain tile.
	 * A vegetation is part of a succession type.
	 * A vegetation has several visual representations (TileType) and can have rules that define
	 * when a vegetation will change to another vegetation during succession.
	 */
	public class VegetationType
	{
		public const string XML_ELEMENT = "vegetation";
		public string name;
		public int index;
		public SuccessionType successionType;
		public TileType[] tiles;
		public UserInteraction[] acceptableActions;
		public ParameterChange[] changes; // changes made to parameters when vegetation is set to this vegetation
		public VegetationRule[] rules; // rules for succession conditions
		public GradualParameterChange[] gradualChanges; // changes in parameters over time
		
		public Color32 colour; // not really used at the moment (inherited from EcoSim 1)
		
		private string[] acceptableActionStrings;

		public VegetationType ()
		{
		}
		
		public VegetationType (SuccessionType succession)
		{
			index = succession.vegetations.Length;
			name = "Naamloos";
			
			tiles = new TileType[0];
			changes = new ParameterChange[0];
			rules = new VegetationRule[0];
			gradualChanges = new GradualParameterChange[0];
			acceptableActionStrings = new string[0];
			
			List<VegetationType> tmpVegList = new List<VegetationType> (succession.vegetations);
			tmpVegList.Add (this);
			succession.vegetations = tmpVegList.ToArray ();
			new TileType (this);
		}
		
		public bool IsAcceptableAction (UserInteraction ui)
		{
			foreach (UserInteraction ui2 in acceptableActions) {
				if (ui2 == ui)
					return true;
			}
			return false;
		}
		
		public static VegetationType Load (XmlTextReader reader, Scene scene)
		{
			VegetationType veg = new VegetationType ();
			veg.name = reader.GetAttribute ("name");
			string colourStr = reader.GetAttribute ("colour");
			if (colourStr != null) {
				int colourNr;
				if (int.TryParse (colourStr, System.Globalization.NumberStyles.HexNumber, null, out colourNr)) {
					Color32 c = new Color32 ((byte)(colourNr >> 16), (byte)(colourNr >> 8), (byte)colourNr, 255);
					veg.colour = c;
				}
			}
			List<ParameterChange> newChanges = new List<ParameterChange> ();
			List<VegetationRule> newRules = new List<VegetationRule> ();
			List<string> acceptableActionsList = new List<string> ();
			List<GradualParameterChange> gradualChanges = new List<GradualParameterChange> ();
			List<TileType> tileList = new List<TileType> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterChange.XML_ELEMENT)) {
						ParameterChange pc = ParameterChange.Load (reader, scene);
						if (pc != null) {
							newChanges.Add (pc);
						}
					}
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == "action")) {
						
						string measureName = reader.GetAttribute ("name");
						if (measureName != null) {
							acceptableActionsList.Add (measureName.ToLower ());
						}
						IOUtil.ReadUntilEndElement (reader, "action");
						
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == GradualParameterChange.XML_ELEMENT)) {
						GradualParameterChange gpc = GradualParameterChange.Load (reader, scene);
						if (gpc != null) {
							gradualChanges.Add (gpc);
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == VegetationRule.XML_ELEMENT)) {
						VegetationRule r = VegetationRule.Load (reader, scene);
						if (r != null) {
							newRules.Add (r);
						}
					} else if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == TileType.XML_ELEMENT)) {
						tileList.Add (TileType.Load (reader, scene));
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			veg.changes = newChanges.ToArray ();
			veg.gradualChanges = gradualChanges.ToArray ();
			veg.rules = newRules.ToArray ();
			veg.tiles = tileList.ToArray ();

			veg.acceptableActionStrings = acceptableActionsList.ToArray ();
			return veg;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			writer.WriteAttributeString ("colour", colour.r.ToString ("X2") + colour.g.ToString ("X2") + colour.b.ToString ("X2"));
			foreach (UserInteraction ui in acceptableActions) {
				writer.WriteStartElement ("action");
				writer.WriteAttributeString ("name", ui.name);
				writer.WriteEndElement ();
			}
			foreach (ParameterChange pc in changes) {
				pc.Save (writer, scene);
			}
			foreach (VegetationRule vr in rules) {
				vr.Save (writer, scene);
			}
			foreach (GradualParameterChange gpc in gradualChanges) {
				gpc.Save (writer, scene);
			}
			
			foreach (TileType tile in tiles) {
				tile.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}

		public void UpdateReferences (Scene scene)
		{
			int i = 0;
			foreach (TileType t in tiles) {
				t.index = i++;
			}
			foreach (TileType t in tiles) {
				t.UpdateLinks (scene, this);
			}
			if (acceptableActionStrings != null) {
				List<UserInteraction> acceptableActions = new List<UserInteraction> ();
				foreach (string s in acceptableActionStrings) {
					UserInteraction ui = scene.actions.GetUIByName (s);
					if ((ui != null) && ((ui.action is AreaAction) || (ui.action is MarkerAction) || (ui.action is InventarisationAction))) {
						acceptableActions.Add (ui);
					}
				}
				acceptableActionStrings = null;
				this.acceptableActions = acceptableActions.ToArray ();
			}
			foreach (ParameterChange pc in changes) {
				pc.UpdateReferences (scene, this);
			}
			foreach (GradualParameterChange gpc in gradualChanges) {
				gpc.UpdateReferences (scene, this);
			}
			foreach (VegetationRule vr in rules) {
				vr.UpdateReferences (scene, this);
			}
		}
	}
	

}