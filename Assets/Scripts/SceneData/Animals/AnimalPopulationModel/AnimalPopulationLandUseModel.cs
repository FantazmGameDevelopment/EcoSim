using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalPopulationLandUseModel : IAnimalPopulationModel
	{
		public const string XML_ELEMENT = "landusemodel";
		
		[System.Serializable]
		public class Food : AnimalPopulationModelDataBase
		{
			public const string XML_ELEMENT = "food";

			//public string gender;
			public string parameterName = "none";
			public int foodRequiredPerAnimal;
			public int foodCarryCapacity;

			public Data data;

			/*public Food (string gender)
			{
				this.gender = gender;
			}*/

			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);
				this.parameterName = reader.GetAttribute ("param");
				this.foodRequiredPerAnimal = int.Parse (reader.GetAttribute ("foodreq"));
				this.foodCarryCapacity = int.Parse (reader.GetAttribute ("carrycap"));
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				writer.WriteAttributeString ("param", this.parameterName);
				writer.WriteAttributeString ("foodreq", this.foodRequiredPerAnimal.ToString());
				writer.WriteAttributeString ("carrycap", this.foodCarryCapacity.ToString());
				writer.WriteEndElement ();
			}

			public void UpdateReferences (Scene scene)
			{
				this.data = scene.progression.GetData (parameterName);
			}
		}
		public Food food = new Food ();
		//public Food foodFemales = new Food ("F");

		[System.Serializable]
		public class Movement : AnimalPopulationModelDataBase
		{
			public const string XML_ELEMENT = "movement";

			//public string gender;
			public string movePreferenceAreaParamName = "none";
			public int minWalkDistance;
			public int maxWalkDistance;
			
			public Data movePrefData;

			/*public Movement (string gender)
			{
				this.gender = gender;
			}*/
			
			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);
				//this.gender = reader.GetAttribute ("gender");
				this.movePreferenceAreaParamName = reader.GetAttribute ("moveprefparam");
				this.minWalkDistance = int.Parse (reader.GetAttribute ("minwalkdist"));
				this.maxWalkDistance = int.Parse (reader.GetAttribute ("maxwalkdist"));
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				//writer.WriteAttributeString ("gender", this.gender);
				writer.WriteAttributeString ("moveprefparam", this.movePreferenceAreaParamName);
				writer.WriteAttributeString ("minwalkdist", this.minWalkDistance.ToString());
				writer.WriteAttributeString ("maxwalkdist", this.maxWalkDistance.ToString());
				writer.WriteEndElement ();
			}
			
			public void UpdateReferences (Scene scene)
			{
				this.movePrefData = scene.progression.GetData (movePreferenceAreaParamName);
			}
		}
		public Movement movement = new Movement ();
		//public Movement movementFemales = new Movement ("F");

		public override void Load (XmlTextReader reader, Scene scene)
		{
			if (!reader.IsEmptyElement) 
			{
				while (reader.Read()) 
				{
					string readerName = reader.Name.ToLower ();
					XmlNodeType nType = reader.NodeType;
					if (nType == XmlNodeType.Element)
					{
						// Add more AnimalPopulationModelDataBases
						if (readerName == Food.XML_ELEMENT) {
							food.Load (reader, scene); 
						} 
						else if (readerName == Movement.XML_ELEMENT) {
							movement.Load (reader, scene);
						} 
					}
					else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
						break;
					}
				}
			}
		}
		
		public override void Save (XmlTextWriter writer, Scene scene)
		{
			writer.WriteStartElement (XML_ELEMENT);
			food.Save (writer, scene);
			//foodFemales.Save (writer, scene);
			movement.Save (writer, scene);
			//movementFemales.Save (writer, scene);
			writer.WriteEndElement ();
		}
		
		public override void UpdateReferences (Scene scene)
		{
			food.UpdateReferences (scene);
			//foodFemales.UpdateReferences (scene);
			movement.UpdateReferences (scene);
			//movementFemales.UpdateReferences (scene);
		}
		
		public override string GetXMLElement ()
		{
			return XML_ELEMENT;
		}
	}
}
