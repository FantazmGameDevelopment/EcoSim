using System;
using System.Threading;
using System.Collections.Generic;
using System.Xml;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.SceneData.Rules;
using UnityEngine;

namespace Ecosim.SceneData.Action
{
	public class AnimalsAction : BasicAction
	{
		public const string XML_ELEMENT = "animals";

		private const int SLICE_SIZE = 128;
		private volatile int activeThreads;

		public bool skipNormalAnimalsLogic = false;

		private Data successionArea = null;
		
		public AnimalsAction (Scene scene, int id) : base(scene, id)
		{
		}
		
		public AnimalsAction (Scene scene) : base(scene, scene.actions.lastId)
		{
		}
		
		public override string GetDescription ()
		{
			return "Handle Animals Logic";
		}
		
		/**
		 * ProcessSlice is started in new thread, arguments should be of type int and is startY position
		 * of the slice.
		 */
		void ProcessSlice (object arguments)
		{
			// Deduct the amount of active threads
			activeThreads--;
			if (activeThreads == 0) {
				finishedProcessing = true;
			}
		}
		
		public override void DoSuccession ()
		{
			base.DoSuccession ();
			
			if (successionArea == null) {
				successionArea = scene.progression.successionArea;
			}

			// Handle the default logic
			if (!skipNormalAnimalsLogic)
			{
				activeThreads = scene.height / SLICE_SIZE;
				for (int y = 0; y < scene.height; y += SLICE_SIZE) 
				{
					// Temp disable unreachable code warning
					#pragma warning disable 162
					if (GameSettings.ANIMALS_LOGIC_MULTITHREADED) {
						ThreadPool.QueueUserWorkItem (ProcessSlice, y);
					}
					else {
						ProcessSlice (y);
					}
					#pragma warning restore 162
				}
			}
		}
		
		public static AnimalsAction Load (Scene scene, XmlTextReader reader)
		{
			int id = int.Parse (reader.GetAttribute ("id"));
			AnimalsAction action = new AnimalsAction (scene, id);
			action.skipNormalAnimalsLogic = (reader.GetAttribute("skipnormalanimalslogic") == "true") ? true : false;
			
			if (!reader.IsEmptyElement) 
			{
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
			writer.WriteAttributeString ("skipnormalanimalslogic", skipNormalAnimalsLogic.ToString().ToLower());
			
			foreach (UserInteraction ui in uiList) {
				ui.Save (writer);
			}
			
			writer.WriteEndElement ();
		}		
	}
}
