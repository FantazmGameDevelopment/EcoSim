using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;
using Ecosim.GameCtrl.GameButtons;
using System;
using System.IO;
using Ecosim.SceneEditor;

public class ReportWindow : ReportBaseWindow
{
	private Report report;
	
	public ReportWindow (Report report, System.Action onFinished) : base (onFinished)
	{
		this.report = report;
	}
	
	public override void Render ()
	{
		base.Render ();

		GUILayout.Label ("WIP", headerLight, GUILayout.Width (width));
	}

	public override void Dispose ()
	{
		base.Dispose ();

		this.report = null;
	}
}
