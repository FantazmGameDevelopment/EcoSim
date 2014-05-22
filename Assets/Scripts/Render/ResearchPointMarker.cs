using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;

public class ResearchPointMarker : MonoBehaviour 
{
	static Quaternion topRotation = Quaternion.Euler(30.0f, 0.0f, 0.0f);

	public GameObject newVisual;
	public GameObject oldVisual;

	public ResearchPoint researchPoint;
	private Transform myTransform;

	void Awake()
	{
		myTransform = transform;
	}

	public void SetVisuals (bool isTemporary)
	{
		newVisual.SetActive (isTemporary);
		oldVisual.SetActive (!isTemporary);
	}

	public void UpdateRotation(Vector3 cameraPosition, Vector3 cameraFwd) 
	{
		Vector3 myP = myTransform.position;
		if (cameraFwd.y < -0.9f) {
			myTransform.localRotation = topRotation;
		}
		else {
			Vector3 p = cameraPosition;
			p.y = (p.y +  3 * myP.y) / 4;
			myTransform.LookAt(p - 50 * cameraFwd);
			myTransform.Rotate (new Vector3(0f, 180f, 0f), Space.Self);
		}
	}

	void OnMouseEnter() 
	{
		string message = "";
		foreach (ResearchPoint.Measurement m in researchPoint.measurements) 
		{
			string line = string.Format ("<b>{0} Year {1}</b>\n", m.name, m.year);
			if (m.message == null) {
				line += "Data not yet available\n";
			}
			else {
				line += m.message;
				if (!line.EndsWith ("\n"))
					line += "\n";
			}

			if (message != "") 
				message += "\n";

			message += line;
		}

		message = message.TrimEnd ('\n');

		RenderResearchPointsMgr.SetMessage (researchPoint, message, transform.position);
	}

	private int CountNewLines (string str)
	{
		return str.Split('\n').Length - 1;
	}
	
	void OnMouseExit() 
	{
		RenderResearchPointsMgr.ClearMessage (researchPoint);
	}
}
