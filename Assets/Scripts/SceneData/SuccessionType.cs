using System.Collections.Generic;
using System.Xml;

namespace Ecosim.SceneData
{
	public class SuccessionType
	{

		public const string XML_ELEMENT = "succession";
		public string name;
		public int index;
		public VegetationType[] vegetations;

		public SuccessionType () {
		}
		
		public SuccessionType (Scene scene)
		{
			name = "Naamloos";
			index = scene.successionTypes.Length;
			vegetations = new VegetationType[0];
			List<SuccessionType> tmpSucList = new List<SuccessionType>(scene.successionTypes);
			tmpSucList.Add(this);
			scene.successionTypes = tmpSucList.ToArray();
		}
		
		public static SuccessionType Load (XmlTextReader reader, Scene scene)
		{
			SuccessionType suc = new SuccessionType ();
			suc.name = reader.GetAttribute ("name");
			List<VegetationType> vegetations = new List<VegetationType> ();
			if (!reader.IsEmptyElement) {
				while (reader.Read()) {
					XmlNodeType nType = reader.NodeType;
					if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == VegetationType.XML_ELEMENT)) {
						VegetationType veg = VegetationType.Load (reader, scene);
						if (veg != null) {
							vegetations.Add (veg);
						}
					} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
						break;
					}
				}
			}
			suc.vegetations = vegetations.ToArray ();
			return suc;
		}

		public void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			writer.WriteAttributeString ("name", name);
			foreach (VegetationType veg in vegetations) {
				veg.Save (writer, scene);
			}
			writer.WriteEndElement ();
		}	
		
		public void UpdateReferences(Scene scene) {
			int i = 0;
			foreach (VegetationType v in vegetations) {
				v.index = i++;
				v.successionType = this;
			}
			foreach (VegetationType v in vegetations) {
				v.UpdateReferences(scene);
			}
		}
	}

}