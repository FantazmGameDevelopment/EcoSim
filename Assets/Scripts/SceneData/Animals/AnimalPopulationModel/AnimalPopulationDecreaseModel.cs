using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalPopulationDecreaseModel : IAnimalPopulationModel
	{
		public const string XML_ELEMENT = "decreasemodel";

		[System.Serializable]
		public class FixedNumber : AnimalPopulationModelDataBase
		{
			public string XML_ELEMENT = "fixed";
			
			public enum Type {
				Absolute,
				Relative
			}
			public Type type;
			public int absolute;
			public float relative;
			
			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);
				this.type = (Type)System.Enum.Parse(typeof(Type), reader.GetAttribute ("type"));
				this.absolute = int.Parse (reader.GetAttribute ("abs"));
				this.relative = float.Parse (reader.GetAttribute ("rel"));
				IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				writer.WriteAttributeString ("type", this.type.ToString());
				writer.WriteAttributeString ("abs", this.absolute.ToString());
				writer.WriteAttributeString ("rel", this.relative.ToString());
				writer.WriteEndElement ();
			}
		}
		public FixedNumber fixedNumber = new FixedNumber();

		[System.Serializable]
		public class SpecifiedNumber : AnimalPopulationModelDataBase
		{
			public string XML_ELEMENT = "specified";

			[System.Serializable]
			public class NaturalDeathRate : AnimalPopulationModelDataBase
			{
				public string XML_ELEMENT = "naturaldeathrate";

				public float minDeathRate;
				public float maxDeathRate;

				public void Load (XmlTextReader reader, Scene scene)
				{
					base.Load (reader, scene);
					this.minDeathRate = float.Parse (reader.GetAttribute ("min"));
					this.maxDeathRate = float.Parse (reader.GetAttribute ("max"));
					IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
				}
				
				public void Save (XmlTextWriter writer, Scene scene)
				{
					writer.WriteStartElement (XML_ELEMENT);
					base.Save (writer, scene);
					writer.WriteAttributeString ("min", this.minDeathRate.ToString());
					writer.WriteAttributeString ("max", this.maxDeathRate.ToString());
					writer.WriteEndElement ();
				}
			}
			public NaturalDeathRate naturalDeathRate = new NaturalDeathRate ();

			[System.Serializable]
			public class Starvation : AnimalPopulationModelDataBase
			{
				public string XML_ELEMENT = "starvation";
				
				public float minStarveRate;
				public float maxStarveRate;
				
				public void Load (XmlTextReader reader, Scene scene)
				{
					base.Load (reader, scene);
					this.minStarveRate = float.Parse (reader.GetAttribute ("min"));
					this.maxStarveRate = float.Parse (reader.GetAttribute ("max"));
					IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
				}
				
				public void Save (XmlTextWriter writer, Scene scene)
				{
					writer.WriteStartElement (XML_ELEMENT);
					base.Save (writer, scene);
					writer.WriteAttributeString ("min", this.minStarveRate.ToString());
					writer.WriteAttributeString ("max", this.maxStarveRate.ToString());
					writer.WriteEndElement ();
				}
			}
			public Starvation starvation = new Starvation ();

			[System.Serializable]
			public class ArtificialDeath : AnimalPopulationModelDataBase
			{
				[System.Serializable]
				public class ArtificialDeathEntry : AnimalPopulationModelDataBase
				{
					public static string XML_ELEMENT = "entry";

					public enum Types
					{
						FixedChance
					}
					public string name;
					public Types type;
					public string parameterName = "none";

					[System.Serializable]
					public class FixedChance : AnimalPopulationModelDataBase
					{
						public string XML_ELEMENT = "fixed";
						
						public float min = 0f;
						public float max = 1f;
						
						public void Load (XmlTextReader reader, Scene scene)
						{
							base.Load (reader, scene);
							this.min = float.Parse (reader.GetAttribute ("min"));
							this.max = float.Parse (reader.GetAttribute ("max"));
							IOUtil.ReadUntilEndElement (reader, XML_ELEMENT);
						}
						
						public void Save (XmlTextWriter writer, Scene scene)
						{
							writer.WriteStartElement (XML_ELEMENT);
							base.Save (writer, scene);
							writer.WriteAttributeString ("min", this.min.ToString());
							writer.WriteAttributeString ("max", this.max.ToString());
							writer.WriteEndElement ();
						}
					}
					public FixedChance fixedChance = new FixedChance ();

					public Data data;

					public ArtificialDeathEntry (string name)
					{
						this.name = name;
					}
					
					public void Load (XmlTextReader reader, Scene scene)
					{
						base.Load (reader, scene);
						this.name = reader.GetAttribute ("name");
						this.parameterName = reader.GetAttribute ("param");
						this.type = (Types)System.Enum.Parse (typeof(Types), reader.GetAttribute ("type"));

						if (!reader.IsEmptyElement) 
						{
							while (reader.Read()) 
							{
								string readerName = reader.Name.ToLower ();
								XmlNodeType nType = reader.NodeType;
								if (nType == XmlNodeType.Element)
								{
									// Add more AnimalPopulationModelDataBases
									if (readerName == this.fixedChance.XML_ELEMENT) {
										this.fixedChance.Load (reader, scene);
									} 
									
								}
								else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
									break;
								}
							}
						}
					}
					
					public void Save (XmlTextWriter writer, Scene scene)
					{
						writer.WriteStartElement (XML_ELEMENT);
						base.Save (writer, scene);
						writer.WriteAttributeString ("name", this.name);
						writer.WriteAttributeString ("param", this.parameterName);
						writer.WriteAttributeString ("type", this.type.ToString());
						fixedChance.Save (writer, scene);
						writer.WriteEndElement ();
					}

					public void UpdateReferences (Scene scene)
					{
						this.data = scene.progression.GetData (parameterName);
						fixedChance.UpdateReferences (scene);
					}
				}

				public string XML_ELEMENT = "artificialdeath";

				public ArtificialDeathEntry[] entries = new ArtificialDeathEntry[0];

				public void Load (XmlTextReader reader, Scene scene)
				{
					base.Load (reader, scene);

					if (!reader.IsEmptyElement) 
					{
						while (reader.Read()) 
						{
							string readerName = reader.Name.ToLower ();
							XmlNodeType nType = reader.NodeType;
							if (nType == XmlNodeType.Element)
							{
								// Add more AnimalPopulationModelDataBases
								if (readerName == ArtificialDeathEntry.XML_ELEMENT) {
									AddEntry ("new").Load (reader, scene);
								} 

							}
							else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
								break;
							}
						}
					}
				}
				
				public void Save (XmlTextWriter writer, Scene scene)
				{
					writer.WriteStartElement (XML_ELEMENT);
					base.Save (writer, scene);
					foreach (ArtificialDeathEntry ade in entries) {
						ade.Save (writer, scene);
					}
					writer.WriteEndElement ();
				}

				public void UpdateReferences (Scene scene)
				{
					foreach (ArtificialDeathEntry ade in entries) {
						ade.UpdateReferences (scene);
					}
				}

				public ArtificialDeathEntry AddEntry (string name)
				{
					ArtificialDeathEntry entry = new ArtificialDeathEntry (name);
					List<ArtificialDeathEntry> entries = new List<ArtificialDeathEntry>(this.entries);
					entries.Add (entry);
					this.entries = entries.ToArray ();
					return entry;
				}

				public void RemoveEntry (ArtificialDeathEntry entry)
				{
					List<ArtificialDeathEntry> entries = new List<ArtificialDeathEntry>(this.entries);
					entries.Remove (entry);
					this.entries = entries.ToArray ();
				}
			}
			public ArtificialDeath artificialDeath = new ArtificialDeath ();

			public void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);

				if (!reader.IsEmptyElement) 
				{
					while (reader.Read()) 
					{
						string readerName = reader.Name.ToLower ();
						XmlNodeType nType = reader.NodeType;
						if (nType == XmlNodeType.Element)
						{
							// Add more AnimalPopulationModelDataBases
							if (readerName == naturalDeathRate.XML_ELEMENT) {
								naturalDeathRate.Load (reader, scene);
							} 
							else if (readerName == starvation.XML_ELEMENT) {
								starvation.Load (reader, scene);
							}
							else if (readerName == artificialDeath.XML_ELEMENT) {
								artificialDeath.Load (reader, scene);
							}
						}
						else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
							break;
						}
					}
				}
			}
			
			public void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				naturalDeathRate.Save (writer, scene);
				starvation.Save (writer, scene);
				artificialDeath.Save (writer, scene);
				writer.WriteEndElement ();
			}

			public void UpdateReferences (Scene scene)
			{
				naturalDeathRate.UpdateReferences (scene);
				starvation.UpdateReferences (scene);
				artificialDeath.UpdateReferences (scene);
			}
		}
		public SpecifiedNumber specifiedNumber = new SpecifiedNumber();

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
						if (readerName == fixedNumber.XML_ELEMENT) {
							fixedNumber.Load (reader, scene);
						} 
						else if (readerName == specifiedNumber.XML_ELEMENT) {
							specifiedNumber.Load (reader, scene);
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
			fixedNumber.Save (writer, scene);
			specifiedNumber.Save (writer, scene);
			writer.WriteEndElement ();
		}
		
		public override void UpdateReferences (Scene scene)
		{
			fixedNumber.UpdateReferences (scene);
			specifiedNumber.UpdateReferences (scene);
		}
		
		public override string GetXMLElement ()
		{
			return XML_ELEMENT;
		}
	}
}
