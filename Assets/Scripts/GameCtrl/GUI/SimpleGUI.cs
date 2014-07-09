using UnityEngine;
using System.Collections;

public class SimpleGUI : MonoBehaviour {
	
	public GUISkin skin;
	
	static SimpleGUI self;
	
	void Awake () {
		self = this;
	}
	
	void OnDestroy () {
		self = null;
	}

	public static bool CheckMouseOver(Rect position) {
		position.width += 1;
		position.height += 1;
		bool overPosition = position.Contains(Event.current.mousePosition);
		CameraControl.MouseOverGUI |= overPosition;
		return overPosition;
	}

	public static bool Label(Rect position, string text, GUIStyle style) {
		GUI.Label(position, text, style);
		return CheckMouseOver(position);
	}

	public static bool Label(Rect position, string text, GUIStyle style, GUIStyle overStyle) {
		bool isOver = CheckMouseOver(position);
		GUI.Label(position, text, isOver?overStyle:style);
		return isOver;
	}
	
	public static bool Label(Rect position, Texture image, GUIStyle style) {
		GUI.Label(position, image, style);
		return CheckMouseOver(position);
	}
	
	public static bool Label(Rect position, Texture image) {
		GUI.Label(position, image, GUIStyle.none);
		return CheckMouseOver(position);
	}
		
	public static bool Button(Rect position, string text, GUIStyle style) {
		CheckMouseOver(position);
		return GUI.Button(position, text, style);
	}

	public static bool Button(Rect position, string text, GUIStyle style, GUIStyle overStyle) {
		bool isOver = CheckMouseOver(position);
		return GUI.Button(position, text, isOver?overStyle:style);
	}
	
	public static bool Button(Rect position, Texture image, GUIStyle style) {
		CheckMouseOver(position);
		return GUI.Button(position, image, style);
	}

	public static bool Button(Rect position, Texture image, Texture imageOver) {
		bool isOver = CheckMouseOver(position);
		return GUI.Button(position, isOver?imageOver:image, GUIStyle.none);
	}

	public static bool Button(Rect position, Texture image, Texture imageOver, GUIStyle style) {
		bool isOver = CheckMouseOver(position);
		return GUI.Button(position, isOver?imageOver:image, style);
	}
	
	public static bool Button(Rect position, Texture image, Texture imageOver, GUIStyle style, GUIStyle overStyle) {
		bool isOver = CheckMouseOver(position);
		return GUI.Button(position, isOver?imageOver:image, isOver?overStyle:style);
	}
	
	public static string TextField(Rect position, string text, GUIStyle style) {
		CheckMouseOver(position);
		return GUI.TextField(position, (text == null)?"(null)":text, style);
	}

	public static string TextField(Rect position, string text, int maxLength, GUIStyle style) {
		CheckMouseOver(position);
		return GUI.TextField(position, (text == null)?"(null)":text, maxLength, style);
	}
	
	public static float Slider (Rect position, float val, float min, float max) {
		CheckMouseOver(position);
		return GUI.HorizontalSlider (position, val, min, max, self.skin.horizontalSlider, self.skin.horizontalSliderThumb);
	}
}
