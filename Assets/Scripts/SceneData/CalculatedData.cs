using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ecosim.SceneData.Action;

namespace Ecosim.SceneData
{
	/**
	 * Data values are (optionally) calculated by a function in the ConversionAction,
	 * else we have a Calculation with other calculations
	 */
	public class CalculatedData : Data
	{
		public class Calculation
		{
			public class ParameterCalculation
			{
				public const string XML_ELEMENT = "paramcalc";

				public string paramName;
				public float multiplier;
				public Data data;

				public ParameterCalculation ()
				{
				}

				public ParameterCalculation (string paramName)
				{
					this.paramName = paramName;
				}

				public static ParameterCalculation Load (XmlTextReader reader, Scene scene)
				{
					ParameterCalculation result = new ParameterCalculation();
					result.paramName = reader.GetAttribute("parameter");
					result.multiplier = float.Parse(reader.GetAttribute("multiplier"));
					IOUtil.ReadUntilEndElement(reader, XML_ELEMENT);
					return result;
				}

				public void Save(XmlTextWriter writer, Scene scene) 
				{
					writer.WriteStartElement(XML_ELEMENT);
					writer.WriteAttributeString ("parameter", paramName);
					writer.WriteAttributeString ("multiplier", multiplier.ToString());
					writer.WriteEndElement();
				}

				public void UpdateReferences (Scene scene)
				{
					data = scene.progression.GetData (paramName);
				}
			}

			public const string XML_ELEMENT = "calcrule";

			public string paramName;
			public int offset;
			public ParameterCalculation[] calculations;
			public CalculatedData data;

			public Calculation ()
			{
				calculations = new ParameterCalculation[0];
			}

			public Calculation (string paramName)
			{
				this.paramName = paramName;
				calculations = new ParameterCalculation[0];
			}

			public static Calculation Load (XmlTextReader reader, Scene scene)
			{
				Calculation result = new Calculation();
				result.paramName = reader.GetAttribute ("parameter");
				result.offset = int.Parse (reader.GetAttribute ("offset").ToLower());

				List<ParameterCalculation> calculations = new List<ParameterCalculation>();
				if (!reader.IsEmptyElement)
				{
					while (reader.Read()) {
						XmlNodeType nType = reader.NodeType;
						if ((nType == XmlNodeType.Element) && (reader.Name.ToLower () == ParameterCalculation.XML_ELEMENT)) {
							ParameterCalculation pr = ParameterCalculation.Load (reader, scene);
							if (pr != null) {
								calculations.Add (pr);
							}
						} else if ((nType == XmlNodeType.EndElement) && (reader.Name.ToLower () == XML_ELEMENT)) {
							break;
						}
					}
				}
				result.calculations = calculations.ToArray();
				return result;
			}
			
			public void Save(XmlTextWriter writer, Scene scene) 
			{
				writer.WriteStartElement(XML_ELEMENT);
				writer.WriteAttributeString ("parameter", paramName);
				writer.WriteAttributeString ("offset", offset.ToString());
				foreach (ParameterCalculation p in calculations) {
					p.Save (writer, scene);
				}
				writer.WriteEndElement();
			}

			public void UpdateReferences (Scene scene)
			{
				data = scene.progression.GetData <CalculatedData> (paramName);
				data.calculation = this;

				foreach (ParameterCalculation p in calculations)
				{
					p.UpdateReferences (scene);
				}
			}

			public static void Save (string path, Calculation[] calculations, Scene scene)
			{
				Directory.CreateDirectory (path);
				XmlTextWriter writer = new XmlTextWriter (path + "calculations.xml", System.Text.Encoding.UTF8);
				writer.WriteStartDocument (true);
				writer.WriteStartElement("calculations");
				foreach (Calculation c in calculations) {
					c.Save (writer, scene);
				}
				writer.WriteEndElement ();
				writer.WriteEndDocument ();
				writer.Close();
			}

			public static Calculation[] LoadAll (string path, Scene scene)
			{
				List<Calculation> list = new List<Calculation>();
				string url = path + "calculations.xml";
				if (File.Exists (url))
				{
					XmlTextReader reader = new XmlTextReader (new System.IO.StreamReader (url));
					try {
						while (reader.Read ()) 
						{
							XmlNodeType nType = reader.NodeType;
							if ((nType == XmlNodeType.Element) && (reader.Name.ToLower() == Calculation.XML_ELEMENT)) 
							{
								Calculation c = Calculation.Load (reader, scene);
								if (c != null) list.Add (c);
							}
						}
					} finally {
						reader.Close ();
					}
				}
				return list.ToArray();
			}
		}

		public CalculatedData (Scene scene, string name) :	base(scene)
		{
			this.name = name;
		}
	
		public Calculation calculation;

		private readonly string name;
		private MethodInfo getValueMI;
		
		private ConversionAction.GetFn getFn;
		private bool retrievedGetFn;

		public override void Save (BinaryWriter writer, Progression progression)
		{
			writer.Write ("CalculatedData");
			writer.Write (width);
			writer.Write (height);
			writer.Write (name);
		}
		
		static CalculatedData LoadInternal (BinaryReader reader, Progression progression)
		{
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			string name = reader.ReadString ();
			
			EnforceValidSize (progression.scene, width, height);
			
			CalculatedData result = new CalculatedData (progression.scene, name);
			return result;
		}

		/**
		 * Sets all values in bitmap to zero
		 */
		public override void Clear ()
		{
			// We use a warning instead of an exception because we don't always know we're trying to adjust
			// a calculated data
			//throw new System.NotSupportedException("operation not supported on calculated data");
			Log.LogWarning ("Operation not supported on calculated data");
		}
		
		/**
		 * set data value val at x, y
		 */
		public override void Set (int x, int y, int val)
		{
			// We use a warning instead of an exception because we don't always know we're trying to adjust
			// a calculated data.
			//throw new System.NotSupportedException ("Can't set values on calculated data"); 
			Log.LogWarning ("Can't set values on calculated data"); 
		}

		public override int Get (int x, int y)
		{
			// Check if we need to find to getFn
			if (getFn == null && !retrievedGetFn) 
			{
				retrievedGetFn = true;
				if (scene.progression.conversionHandler != null) 
				{
					getFn = scene.progression.conversionHandler.GetDataDelegate (name);
					/*if (getFn == null) {
						throw new System.NotSupportedException("no valid function '" + name + "' defined in conversion action.");
					}*/
				}
			}

			// We have a GetFn delegate found in the scripts
			if (getFn != null) 
			{
				try {
					return getFn(x, y);
				}
				catch (TargetException te) {
					Log.LogException (te);
					getFn = null;
					return 0;
				}
			} 
			else 
			{
				// We'll handle the combined data via the created rules
				float calculatedValue = (float)calculation.offset;
				foreach (Calculation.ParameterCalculation p in calculation.calculations) 
				{
					int dataVal = p.data.Get (x, y);
					if (dataVal > 0) {
						calculatedValue += (float)dataVal * p.multiplier;
					}
				}	
				return UnityEngine.Mathf.Clamp ((int)calculatedValue, GetMin(), GetMax());
			}
		}
		public override int GetMin() { return 0; }
		public override int GetMax() { return 255; }
		
		public override Data CloneAndResize(Progression targetProgression, int offsetX, int offsetY) {
			
			return new CalculatedData (targetProgression.scene, name);
		}
	}
}