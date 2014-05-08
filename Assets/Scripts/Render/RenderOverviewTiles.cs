using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneEditor;
using Ecosim.SceneData;

public class RenderOverviewTiles : MonoBehaviour
{
	
	static RenderOverviewTiles self;
	public Camera overviewCamera;
	
	const int OVERVIEW_TEXTURE_SIZE = Scene.OVERVIEW_TEXTURE_SIZE;
	
	void Awake ()
	{
		self = this;
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	public static void StartRenderingOverviewTiles (EditorCtrl ctrl)
	{
		self.StartCoroutine (self.CORenderOverviewTiles (ctrl));
	}
	
	IEnumerator CORenderOverviewTiles (EditorCtrl ctrl)
	{
		while (TerrainMgr.IsRendering) {
			yield return 0;
		}
		bool cameraWasNear = CameraControl.IsNear;
		if (!cameraWasNear) {
			CameraControl.SwitchToNear ();
		}
		Scene scene = ctrl.scene;
		int width = scene.width;
		int height = scene.height;
		int overviewSize = TerrainMgr.CELL_SIZE;
		float scale = overviewSize * TerrainMgr.TERRAIN_SCALE;
		int cwidth = width / overviewSize;
		int cheight = height / overviewSize;		
		
		overviewCamera.orthographicSize = overviewSize * TerrainMgr.TERRAIN_SCALE * 0.5f;
		ctrl.enabled = false;
		CameraControl.DisableCamera ();
		int saveQuality = QualitySettings.GetQualityLevel ();
		QualitySettings.SetQualityLevel (5, true);
		TerrainMgr.self.UpdateQualitySettings (-1);
		overviewCamera.enabled = true;
		yield return 0;
		TerrainMgr.self.followPosition = Vector3.zero;
		TerrainMgr.self.ForceRedraw();
		for (int y = 0; y < cheight; y ++) {
			for (int x = 0; x < cwidth; x++) {
				Texture2D tex = scene.overview[y, x];
				if (tex == null) {
					tex = new Texture2D(OVERVIEW_TEXTURE_SIZE, OVERVIEW_TEXTURE_SIZE, TextureFormat.RGB24, false);
					scene.overview[y, x] = tex;
				}
				Vector3 pos = new Vector3 ((0.5f + x) * scale, 0f, (0.5f + y) * scale);
				transform.localPosition = new Vector3 (pos.x, transform.position.y, pos.z);
				TerrainMgr.self.followPosition = pos;
				yield return 0;
				while (TerrainMgr.IsRendering) {
					yield return 0;
				}
				RenderTexture rt =  RenderTexture.GetTemporary(OVERVIEW_TEXTURE_SIZE, OVERVIEW_TEXTURE_SIZE, 24, RenderTextureFormat.ARGB32);
				rt.useMipMap = false;
				rt.wrapMode = TextureWrapMode.Clamp;
				overviewCamera.targetTexture = rt;
				overviewCamera.Render();
				yield return 0;
				RenderTexture.active = rt;
				
				tex.ReadPixels(new Rect(0, 0, OVERVIEW_TEXTURE_SIZE, OVERVIEW_TEXTURE_SIZE), 0, 0, false);
				tex.Apply();
				tex.wrapMode = TextureWrapMode.Clamp;
				scene.overview[y, x] = tex;
				overviewCamera.targetTexture = null;
				RenderTexture.active = null;
				RenderTexture.ReleaseTemporary(rt);
				yield return 0;
			}
		}
		yield return 0;
		
		QualitySettings.SetQualityLevel (saveQuality, true);
		yield return 0;
		TerrainMgr.self.UpdateQualitySettings ();
		TerrainMgr.self.CreateOverviewTiles ();
		if (!cameraWasNear) {
			CameraControl.SwitchToFar ();
		}
		CameraControl.EnableCamera ();
		overviewCamera.enabled = false;
		yield return 0;
		TerrainMgr.self.ForceRedraw ();
		while (TerrainMgr.IsRendering) {
			yield return 0;
		}
		ctrl.enabled = true;
	}
}
