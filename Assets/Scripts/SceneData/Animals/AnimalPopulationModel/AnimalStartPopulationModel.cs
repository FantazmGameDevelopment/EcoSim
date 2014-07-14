using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Ecosim.SceneData.AnimalPopulationModel
{
	public class AnimalStartPopulationModel : IAnimalPopulationModel
	{
		public const string XML_ELEMENT = "startpopmodel";

		[System.Serializable]
		public class Nests : AnimalPopulationModelDataBase
		{
			public string XML_ELEMENT = "nests";

			// TODO: Save these data in Data maps so that the progress can be saved correctly/easily

			public class Nest
			{
				public const string XML_ELEMENT = "nest";

				public Scene scene;
				public Nests parent;

				public int x;
				public int y;
				
				public int males { 
					get { return malesMap.Get (x, y); } 
					set { malesMap.Set (x, y, value); }
				}
				public int females { 
					get { return femalesMap.Get (x, y); } 
					set { femalesMap.Set (x, y, value); }
				}
				public int currentFood { 
					get { return foodMap.Get (x, y); } 
					set { foodMap.Set (x, y, value); }
				}
				
				public int totalCapacity;
				public int malesCapacity;
				public int femalesCapacity;

				private Data _malesMap;
				private Data _femalesMap;
				private Data _foodMap;

				public Data malesMap {
					get {
						if (_malesMap == null) {
							_malesMap = parent.model.animal.GetAssociatedData ("males");
							if (_malesMap == null) {
								_malesMap = new BitMap16 (parent.model.animal.scene);
								parent.model.animal.AddAssociatedData ("males", _malesMap);
							}
						}
						return _malesMap;
					}
				}
				public Data femalesMap {
					get {
						if (_femalesMap == null) {
							_femalesMap = parent.model.animal.GetAssociatedData ("females");
							if (_femalesMap == null) {
								_femalesMap = new BitMap16 (parent.model.animal.scene);
								parent.model.animal.AddAssociatedData ("females", _femalesMap);
							}
						}
						return _femalesMap;
					}
				}
				public Data foodMap {
					get {
						if (_foodMap == null) {
							_foodMap = parent.model.animal.GetAssociatedData ("food");
							if (_foodMap == null) {
								_foodMap = new BitMap16 (parent.model.animal.scene);
								parent.model.animal.AddAssociatedData ("food", _foodMap);
							}
						}
						return _foodMap;
					}
				}
				
				public Nest (Nests parent)
				{
					this.parent = parent;
				}
				
				public static Nest Load (Nests parent, XmlTextReader reader, Scene scene)
				{
					Nest nest = new Nest (parent);
					nest.x = int.Parse(reader.GetAttribute ("x"));
					nest.y = int.Parse(reader.GetAttribute ("y"));
					nest.totalCapacity = int.Parse (reader.GetAttribute ("cap"));
					nest.malesCapacity = int.Parse (reader.GetAttribute ("mcap"));
					nest.femalesCapacity = int.Parse (reader.GetAttribute ("fcap"));
					IOUtil.ReadUntilEndElement(reader, XML_ELEMENT);
					return nest;
				}
				
				public void Save (XmlTextWriter writer, Scene scene)
				{
					writer.WriteStartElement (XML_ELEMENT);
					writer.WriteAttributeString ("x", x.ToString());
					writer.WriteAttributeString ("y", y.ToString());
					writer.WriteAttributeString ("cap", totalCapacity.ToString());
					writer.WriteAttributeString ("mcap", malesCapacity.ToString());
					writer.WriteAttributeString ("fcap", femalesCapacity.ToString());
					writer.WriteEndElement ();
				}
				
				public void UpdateReferences (Scene scene)
				{

				}

				public override string ToString ()
				{
					return string.Format ("[Nest] ({0},{1}) Males:{2}/{3}, Females:{4}/{5}, Food:{6}", 
					                      x.ToString(), 
					                      y.ToString(), 
					                      males.ToString(), 
					                      malesCapacity.ToString(), 
					                      females.ToString(), 
					                      femalesCapacity.ToString(), 
					                      currentFood);
				}

				public int GetMalesAt (int year)
				{
					Data data = parent.model.animal.GetAssociatedData ("males", year);
					if (data != null) {
						return data.Get (x, y);
					}
					return 0;
				}

				public int GetFemalesAt (int year)
				{
					Data data = parent.model.animal.GetAssociatedData ("females", year);
					if (data != null) {
						return data.Get (x, y);
					}
					return 0;
				}
			}

			public Nest[] nests = new Nest[0];

			public Nests (IAnimalPopulationModel model) : base (model)
			{
				
			}

			public void UpdateReferences (Scene scene)
			{
				foreach (Nest n in nests) {
					n.UpdateReferences (scene);
				}
			}

			public override void Load (XmlTextReader reader, Scene scene)
			{
				base.Load (reader, scene);

				List<Nest> nests = new List<Nest>();
				
				if (!reader.IsEmptyElement) 
				{
					while (reader.Read()) 
					{
						string readerName = reader.Name.ToLower ();
						XmlNodeType nType = reader.NodeType;
						if (nType == XmlNodeType.Element)
						{
							
							if (readerName == Nest.XML_ELEMENT) {
								Nest nest = Nest.Load (this, reader, scene);
								if (nest != null) {
									nests.Add (nest);
								}
							} 
							
						}
						else if ((nType == XmlNodeType.EndElement) && (readerName == XML_ELEMENT)) {
							break;
						}
					}
				}
				
				this.nests = nests.ToArray();
			}

			public override void Save (XmlTextWriter writer, Scene scene)
			{
				writer.WriteStartElement (XML_ELEMENT);
				base.Save (writer, scene);
				foreach (Nest n in nests) {
					n.Save (writer, scene);
				}
				writer.WriteEndElement ();
			}
		}
		public Nests nests;

		public AnimalStartPopulationModel (AnimalType animal) : base (animal)
		{
			this.nests = new Nests (this);
		}

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
						if (readerName == nests.XML_ELEMENT) {
							nests.Load (reader, scene);
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
			nests.Save (writer, scene);
			writer.WriteEndElement ();
		}

		public override void UpdateReferences (Scene scene)
		{
			nests.UpdateReferences (scene);
		}

		public override string GetXMLElement ()
		{
			return XML_ELEMENT;
		}

		public override void PrepareSuccession ()
		{
			nests.PrepareSuccession ();
		}

		public override void DoSuccession ()
		{
			nests.DoSuccession ();
		}

		public override void FinalizeSuccession ()
		{
			nests.FinalizeSuccession ();
		}
	}
}
