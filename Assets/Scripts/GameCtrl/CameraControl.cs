using UnityEngine;
using System.Collections;
using Ecosim;
using Ecosim.SceneData;

public class CameraControl : MonoBehaviour
{
	public static CameraControl self;
	
	private Scene scene;
	
	void Awake ()
	{
		self = this;
	}
	
	void OnDestroy ()
	{
		self = null;
	}
	
	// Use this for initialization
	void Start ()
	{
		nearCamera.layerCullSpherical = true;
		nearTransform = nearCamera.transform;
		farTransform = farCamera.transform;
		float[] cullingMasks = new float[32];
		cullingMasks [Layers.L_DECALS] = 2000f;
		nearCamera.layerCullDistances = cullingMasks;
		Quaternion rot = Quaternion.Euler (0f, direction, 0f) * Quaternion.Euler (angle, 0f, 0f);
		nearTransform.localRotation = rot;
		compassT.rotation = Quaternion.identity;
		compassPointerT.localRotation = Quaternion.Euler (0f, direction, 0f);
		compassT.gameObject.SetActive (false);
		
		SwitchToNear ();
	}
	
	float angle = 90f;
	float direction = 0f;
	Vector3 lastMousePos;
	public Camera nearCamera;
	private Transform nearTransform;
	public Camera farCamera;
	private Transform farTransform;
	public Transform compassT;
	public Transform compassPointerT;
	private bool altWasOn = false;
	public static bool MouseOverGUI = false;
	private bool isNearActive = true;
	private bool doMoveCamera = true;

	[System.Serializable]
	public class FarCameraData
	{
		public float moveSpeed = 1f;
		public float fastMoveSpeed = 2f;
		public float zoomSpeed = 4f;
		public float fastZoomSpeed = 8f;
	}
	public FarCameraData farCameraData;
	
	[System.Serializable]
	public class NearCameraData
	{
		public float rotationSpeed = 2f;
		public float moveSpeed = 100f;
		public float fastMoveSpeed = 1000f;
		public float zoomSpeed = 1f;
		public float fastZoomSpeed = 10f;
		public float verticalPanSpeed = 2f;
		public float verticalFastPanSpeed = 10f;
		public float horizontalPanSpeed = 2f;
		public float horizontalFastPanSpeed = 10f;
	}
	public NearCameraData nearCameraData;

	public static bool IsNear {
		get { return self.isNearActive; }
	}
	
	public static void MoveToPosition (int x, int y)
	{
		self.nearTransform.localPosition = new Vector3 (x * TerrainMgr.TERRAIN_SCALE, self.nearTransform.localPosition.y, y * TerrainMgr.TERRAIN_SCALE);
		self.farTransform.localPosition = new Vector3 (x * TerrainMgr.TERRAIN_SCALE, self.farTransform.localPosition.y, y * TerrainMgr.TERRAIN_SCALE);
	}
	
	public static void SwitchToNear ()
	{
		if (self.isNearActive) return;

		self.farCamera.gameObject.SetActive (false);
		self.nearCamera.gameObject.SetActive (true);
		self.isNearActive = true;
		GameControl.InterfaceChanged ();
	}

	public static void SwitchToNearResetAngle ()
	{
		self.direction = 0f;
		self.angle = 90f;
		Quaternion rot = Quaternion.Euler (0f, self.direction, 0f) * Quaternion.Euler (self.angle, 0f, 0f);
		self.nearTransform.localRotation = rot;
		self.compassT.rotation = Quaternion.identity;
		self.compassPointerT.localRotation = Quaternion.Euler (0f, self.direction, 0f);
		self.farCamera.gameObject.SetActive (false);
		self.nearCamera.gameObject.SetActive (true);
		self.isNearActive = true;
		GameControl.InterfaceChanged ();
	}
	
	public static void SwitchToFar ()
	{
		self.nearCamera.gameObject.SetActive (false);
		self.farCamera.gameObject.SetActive (true);
		self.isNearActive = false;
		GameControl.InterfaceChanged ();
	}
	
	public static void SetupCamera (Scene scene) {
		self.scene = scene;
		SwitchToFar ();
		self.direction = 0f;
		self.angle = 90f;
		Quaternion rot = Quaternion.Euler (0f, self.direction, 0f) * Quaternion.Euler (self.angle, 0f, 0f);
		self.nearTransform.localRotation = rot;
		self.compassT.rotation = Quaternion.identity;
		self.compassPointerT.localRotation = Quaternion.Euler (0f, self.direction, 0f);
		if (scene != null) {
			Vector3 pos = self.farTransform.position;
			pos.x = scene.width * TerrainMgr.TERRAIN_SCALE * 0.5f;
			pos.z = scene.height * TerrainMgr.TERRAIN_SCALE * 0.5f;
			self.farTransform.position = pos;
			pos = self.nearTransform.position;
			pos.x = scene.width * TerrainMgr.TERRAIN_SCALE * 0.5f;
			pos.z = scene.height * TerrainMgr.TERRAIN_SCALE * 0.5f;
			self.nearTransform.position = pos;
		}

		if (!Application.isEditor) {
			// Start the game in an angle
			SwitchToNear ();
			self.nearCamera.transform.rotation = Quaternion.identity;
			Vector3 newPos = self.nearCamera.transform.localPosition;
			newPos -= self.nearCamera.transform.forward * ((TerrainMgr.CELL_SIZE * TerrainMgr.TERRAIN_SCALE) * 0.65f);
			newPos.y = 750f;
			self.nearCamera.transform.localPosition = newPos;
			self.nearCamera.transform.Rotate (new Vector3 (35f, 0f, 0f), Space.Self);
		}
	}
	
	public static void DisableCamera ()
	{
		self.nearCamera.enabled = false;
		self.farCamera.enabled = false;
		self.enabled = false;
	}
	
	public static void EnableCamera ()
	{
		self.nearCamera.enabled = true;
		self.farCamera.enabled = true;
		self.enabled = true;
	}

	public static void FocusOnPosition (Vector3 targetPos)
	{
		SwitchToNear ();
		Vector3 pos = self.nearTransform.position;
		pos.x = targetPos.x;
		pos.z = targetPos.z - 250f;
		pos.y = targetPos.y + 200f;
		self.nearTransform.position = pos;
		self.nearTransform.LookAt (targetPos);
	}
	
	void Update ()
	{
		if (self.isNearActive) {
			UpdateNear ();
		} else {
			UpdateFar ();
		}
		lastMousePos = Input.mousePosition;
	}
	
	void UpdateNear ()
	{
		bool shift = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
		float moveSpeed = shift ? nearCameraData.fastMoveSpeed : nearCameraData.moveSpeed;
		float zoomSpeed = shift ? nearCameraData.fastZoomSpeed : nearCameraData.zoomSpeed;
		float horizontalPanSpeed = shift ? nearCameraData.horizontalFastPanSpeed : nearCameraData.horizontalPanSpeed;
		float verticalPanSpeed = shift ? nearCameraData.verticalFastPanSpeed : nearCameraData.verticalPanSpeed;
		float rotateSpeed = nearCameraData.rotationSpeed;
		float deltaTime = Mathf.Min(Time.deltaTime, 0.1f); // 02 FPS as min
		float deltaX = Input.GetAxis ("Horizontal");
		float deltaY = Input.GetAxis ("Vertical");
		
		float deltaH = 0f;
		if (!MouseOverGUI) {
			deltaH += -4f * Input.GetAxis ("Mouse ScrollWheel");
			if (Input.GetKey (KeyCode.PageDown)) {
				deltaH = -1;
			}
			if (Input.GetKey (KeyCode.PageUp)) {
				deltaH = +1;
			}
		}
		
		Vector3 pos = nearTransform.localPosition;
		
		if (deltaH != 0.0f) {
			float scale = Mathf.Pow (2f, deltaH * deltaTime * zoomSpeed);
			pos.y = Mathf.Clamp (pos.y  * scale, 50f, 7000f);
		}
		
		//float newAngle = angle;
		//float newDirection = direction;
		
		bool alt = Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt);
		Vector3 mousePos = Input.mousePosition;
		/*if (Input.GetMouseButton (0) && alt) {
			Vector3 deltaPos = (mousePos - lastMousePos) * deltaTime;
				
			newAngle = Mathf.Clamp (angle + 10 * deltaPos.y, 0.1f, 90f);
			newDirection = Mathf.Repeat (direction + 40 * deltaPos.x, 360f);
		}
		lastMousePos = mousePos;
		
		if ((newAngle != angle) || (newDirection != direction)) {
			Quaternion rot = Quaternion.Euler (0f, direction, 0f) * Quaternion.Euler (angle, 0f, 0f);
			nearTransform.localRotation = rot;
			angle = newAngle;
			direction = newDirection;
			compassT.rotation = Quaternion.identity;
			compassPointerT.localRotation = Quaternion.Euler (0f, direction, 0f);
		}*/

		// Check whether the RMB was released off-screen
		if (Input.GetMouseButtonUp(1)) {
			if (Input.mousePosition.x >= Screen.width * 0.995f) {
				doMoveCamera = false;
			} else if (Input.mousePosition.x <= Screen.width * 0.005f) {
				doMoveCamera = false;
			}
			if (Input.mousePosition.y >= Screen.height * 0.995f) {
				doMoveCamera = false;
			} else if (Input.mousePosition.y <= Screen.height * 0.005f) {
				doMoveCamera = false;
			}
		}

		// Rotate the camera with the RMB
		if (Input.GetMouseButton(1) || alt) {
			// Rotate
			float rotationDeltaTime = Mathf.Min(deltaTime, 0.0833f); // 12 FPS as min
			float rotationH = (mousePos.x - lastMousePos.x) * nearCameraData.rotationSpeed * rotationDeltaTime;
			float rotationV = -(mousePos.y - lastMousePos.y) * nearCameraData.rotationSpeed * rotationDeltaTime;
			nearTransform.Rotate ( rotationV, 0f, 0f, Space.Self );
			nearTransform.Rotate ( 0f, rotationH, 0f, Space.World );
			doMoveCamera = false;
		} 
		// Pan up/down
		else if (Input.GetMouseButton(2)) {
			if (!MouseOverGUI) {
				float panDeltaTime = Mathf.Min(deltaTime, 0.0833f); // 12 FPS as min
				float rotV = -(mousePos.y - lastMousePos.y) * panDeltaTime;
				float rotH = (mousePos.x - lastMousePos.x) * panDeltaTime;
				pos += Vector3.up * rotV * verticalPanSpeed;
				pos.y = Mathf.Clamp (pos.y, 50f, 7000f);
				pos -= nearTransform.right * rotH * horizontalPanSpeed;
			}
		}
		// Move camera with the mouse on the edges of the screen
		else {
			float mouseDeltaX = 0f;
			float mouseDeltaY = 0f;
			
			if (Input.mousePosition.x >= Screen.width * 0.995f) { // Horizontal
				if (!MouseOverGUI || Input.mousePosition.x >= Screen.width) 
					mouseDeltaX = 1f;
			} else if (Input.mousePosition.x <= Screen.width * 0.005f) {
				if (!MouseOverGUI || Input.mousePosition.x <= 0f) 
					mouseDeltaX = -1f;
			}
			if (Input.mousePosition.y >= Screen.height * 0.995f) { // Vertical
				if (!MouseOverGUI || Input.mousePosition.y >= Screen.height) 
					mouseDeltaY = 1f;
			} else if (Input.mousePosition.y <= Screen.height * 0.005f) {
				if (!MouseOverGUI || Input.mousePosition.y <= 0f) 
					mouseDeltaY = -1f;
			}

			// Make sure the camera does not move after rotating or panning
			if (!doMoveCamera) {
				if (mouseDeltaX == 0f && mouseDeltaY == 0f) doMoveCamera = true;
			} else {
				if (mouseDeltaX != 0f) deltaX = mouseDeltaX;
				if (mouseDeltaY != 0f) deltaY = mouseDeltaY;
			}
		}

		if ((deltaX != 0f) || (deltaY != 0f)) {
			float horizontalSpeed = deltaTime * moveSpeed;
			//float a = direction * Mathf.Deg2Rad;
			Vector3 forward = nearTransform.up;
			Vector3 right = nearTransform.right;
			forward.y = right.y = 0f;
			forward.Normalize();
			right.Normalize();
			pos += (forward * deltaY * horizontalSpeed);//(Mathf.Cos (a) * deltaY - Mathf.Sin (a) * deltaX) * horizontalSpeed;
			pos += (right * deltaX * horizontalSpeed);//(Mathf.Cos (a) * deltaX + Mathf.Sin (a) * deltaY) * horizontalSpeed;
			if (scene != null) {
				pos.x = Mathf.Clamp (pos.x, 0f, scene.width * TerrainMgr.TERRAIN_SCALE);
				pos.z = Mathf.Clamp (pos.z, 0f, scene.height * TerrainMgr.TERRAIN_SCALE);
			}
		}
		nearTransform.localPosition = pos;
		Ray ray = nearCamera.ScreenPointToRay (new Vector3 (0.5f * Screen.width, Mathf.Lerp (0.1f, 0.5f, angle / 90f) * Screen.height, 0f));
		float delta = -ray.origin.y / ray.direction.y;
		Vector3 ground = ray.origin + delta * ray.direction;
		RaycastHit hit;
		ray.origin = ray.origin - 50 * ray.direction;
		if (Physics.Raycast (ray, out hit, 100f, Layers.M_TERRAIN)) {
			if (hit.point.y + 25f > pos.y) {
				pos.y = hit.point.y + 25f;
				nearTransform.localPosition = pos;
			}
		}
		if (!alt) {
			TerrainMgr.self.followPosition = ground;
		}
		if (alt != altWasOn) {
			altWasOn = alt;
			//compassT.gameObject.SetActive (alt);
		}
		if (Input.GetKeyDown (KeyCode.Escape)) {
			SwitchToFar ();
		}
	}

	void UpdateFar ()
	{
		bool shift = Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift);
		float moveSpeed = shift ? farCameraData.fastMoveSpeed : farCameraData.moveSpeed;
		float zoomSpeed = shift ? farCameraData.fastZoomSpeed : farCameraData.zoomSpeed;
		float deltaTime = Time.deltaTime;
		float deltaX = Input.GetAxis ("Horizontal");
		float deltaY = Input.GetAxis ("Vertical");
		
		float deltaH = 0f;
		if (!MouseOverGUI) {
			deltaH += -4f * Input.GetAxis ("Mouse ScrollWheel");
			if (Input.GetKey (KeyCode.PageDown)) {
				deltaH = -1;
			}
			if (Input.GetKey (KeyCode.PageUp)) {
				deltaH = +1;
			}
		}

		Vector3 pos = farTransform.localPosition;

		// Move camera with the mouse on the edges of the screen
		if (Input.mousePosition.x >= Screen.width * 0.995f) { // Horizontal
			if (!MouseOverGUI || Input.mousePosition.x >= Screen.width) 
				deltaX = 1f;
		} else if (Input.mousePosition.x <= Screen.width * 0.005f) {
			if (!MouseOverGUI || Input.mousePosition.x <= 0f) 
				deltaX = -1f;
		}
		if (Input.mousePosition.y >= Screen.height * 0.995f) { // Vertical
			if (!MouseOverGUI || Input.mousePosition.y >= Screen.height) 
				deltaY = 1f;	
		} else if (Input.mousePosition.y <= Screen.height * 0.005f) {
			if (!MouseOverGUI || Input.mousePosition.y <= 0f) 
				deltaY = -1f;
		}

		if ((deltaX != 0f) || (deltaY != 0f)) {
			float horizontalSpeed = deltaTime * moveSpeed * farCamera.orthographicSize;
			pos.z += deltaY * horizontalSpeed;
			pos.x += deltaX * horizontalSpeed;
			
			if (scene != null) {
				pos.x = Mathf.Clamp (pos.x, 0f, scene.width * TerrainMgr.TERRAIN_SCALE);
				pos.z = Mathf.Clamp (pos.z, 0f, scene.height * TerrainMgr.TERRAIN_SCALE);
			}
		}
		if (deltaH != 0f) {
			float scale = Mathf.Pow (2f, deltaH * deltaTime * zoomSpeed);
			farCamera.orthographicSize = Mathf.Clamp (scale * farCamera.orthographicSize, 256, 10240);
		}
		farTransform.localPosition = pos;
		if (Input.GetKeyDown (KeyCode.Escape)) {
			if (scene != null) {
				SwitchToNear ();
			}
		}
		
		if (!MouseOverGUI && Input.GetMouseButton (0)) {
			if (TerrainMgr.self.scene != null) {
				Ray screenRay = farCamera.ScreenPointToRay (Input.mousePosition);
				int x = Mathf.RoundToInt (screenRay.origin.x / TerrainMgr.TERRAIN_SCALE);
				int y = Mathf.RoundToInt (screenRay.origin.z / TerrainMgr.TERRAIN_SCALE);
				if ((scene != null) && (x >= 0) && (y >= 0) && (x < scene.width) && (y < scene.height)) {
					MoveToPosition (x, y);
					SwitchToNearResetAngle ();
				}
			}
		}
	}
	
	void LateUpdate () {
		// reset mouseOverGUI for next frame..
		MouseOverGUI = false;
	}

}
