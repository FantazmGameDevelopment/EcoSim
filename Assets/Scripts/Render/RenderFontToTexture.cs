using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using Ecosim;
using Ecosim.SceneData;

public class RenderFontToTexture : MonoBehaviour {
	
	public class MyChar {
		public short width;
		public short height;
		public short xOffset;
		public short yOffset;
		public short xAdvance;
		public byte[] charData;
	}
	
	public class MyKerning {
		public short[] kerning;
	}
	
	[System.Serializable]
	public class MyFont {
		public TextAsset fontData;
		public Texture2D fontTexture;

		[System.NonSerialized]
		public string family;
		
		[System.NonSerialized]
		public int size;

		[System.NonSerialized]
		public bool isBold;

		[System.NonSerialized]
		public bool isItalic;
		
		[System.NonSerialized]
		public short baseLine;
		[System.NonSerialized]
		public short lineHeight;
		[System.NonSerialized]
		public MyChar[] charTable;
		[System.NonSerialized]
		public MyKerning[] kerningTable;
	}
	
	struct RenderBufferChar {
		public RenderBufferChar(int x, MyFont font, MyChar mc, Color32 color, int i) {
			this.x = x; this.font = font; this.mc = mc; this.color = color; index = i;
			isFollowedBySpace = false;
			isHyphenOption = false;
		}
		
		public int x;
		public MyFont font;
		public MyChar mc;
		public Color32 color;
		public bool isFollowedBySpace;
		public bool isHyphenOption;
		public int index;
	}

	public static RenderFontToTexture self;
	public Texture2D[] bgTexList;
	public Texture2D[] buildinTextures;
	
	
	public MyFont[] fonts;
	const char hyphenChar = '\u2013';
	
	RenderBufferChar[] renderBuffer = new RenderBufferChar[128];
	int renderBufferLen = 0;
	string imagePath;

	private Color32[] pixels;
	private Texture2D destinationTex;
	private int tw;
	private int th;
	private int maxPixel;
	
	private bool PreventArrayOverflow<T>(ref T[] current, int newSize) {
		if (current == null) {
			current = new T[newSize + 1];
			return true;
		}
		int len = current.Length;
		if (newSize < len) return false;
		T[] old = current;
		current = new T[newSize + 1];
		System.Array.Copy(old, current, len);
		return true;
	}
	
	int GetIntFromItem(string s) {
		int v = 0;
		int index = s.IndexOf('=');
		if (index >= 0) {
			v = int.Parse(s.Substring(index + 1));
		}
		return v;
	}
	
	public MyFont FindFont(string family, int size, bool bold, bool italic) {
		foreach (MyFont f in fonts) {
			if ((family == null) || (string.Compare(family, f.family) == 0)) {
				if ((size <= 0) || (size == f.size)) {
					if ((f.isBold == bold) && (f.isItalic == italic)) return f;
				}
			}
		}
		Debug.Log("Couldn't find font '" + family + "' at size " + size + " " + (bold?"bold":"") + " " + (italic?"italic":""));
		return null;
	}

	public MyFont FindFont(MyFont font, bool bold, bool italic) {
		foreach (MyFont f in fonts) {
			if ((font.family == f.family) && (font.size == f.size)) {
				if ((f.isBold == bold) && (f.isItalic == italic)) return f;
			}
		}
		// if we can't find it just return font we used to match with, at least type and size will be correct
		Debug.LogWarning("can't find " + font.family + " size " + font.size + " " + (bold?"bold":"") + (italic?"italic":""));
		return font;
	}
	
	
	void AddChar(MyFont f, Color32[] pixels, int width, int height, string line) {
		int id = 0;
		int x = 0;
		int y = 0;
		int w = 0;
		int h = 0;
		int xo = 0;
		int yo = 0;
		int xadvance = 0;
		string[] values = line.Split(' ');
		foreach (string val in values) {
			if (val.StartsWith("id=")) id = GetIntFromItem(val);
			if (val.StartsWith("x=")) x = GetIntFromItem(val);
			if (val.StartsWith("y=")) y = GetIntFromItem(val);
			if (val.StartsWith("width=")) w = GetIntFromItem(val);
			if (val.StartsWith("height=")) h = GetIntFromItem(val);
			if (val.StartsWith("xoffset=")) xo = GetIntFromItem(val);
			if (val.StartsWith("yoffset=")) yo = GetIntFromItem(val);
			if (val.StartsWith("xadvance=")) xadvance = GetIntFromItem(val);
		}
		MyChar mc = new MyChar();
		mc.width = (short) w;
		mc.height = (short) h;
		mc.xAdvance = (short) xadvance;
		mc.xOffset = (short) xo;
		mc.yOffset = (short) yo;
		byte[] b = new byte[w * h];
		for (int yy = 0; yy < h; yy++) {
			for (int xx = 0; xx < w; xx++) {
				b[xx + w * yy] = pixels[(x + xx) + width * (height - y - yy - 1)].a;
			}
		}
		mc.charData = b;
		PreventArrayOverflow<MyChar>(ref f.charTable, Mathf.Max(f.charTable.Length, id));
		f.charTable[id] = mc;
	}
	
	void AddKerning(MyFont f, string line) {
		string[] values = line.Split(' ');
		int first = 0;
		int second = 0;
		int amount = 0;
		foreach (string val in values) {
			if (val.StartsWith("first=")) first = GetIntFromItem(val);
			if (val.StartsWith("second=")) second = GetIntFromItem(val);
			if (val.StartsWith("amount=")) amount = GetIntFromItem(val);
		}
		PreventArrayOverflow<MyKerning>(ref f.kerningTable, Mathf.Max(f.kerningTable.Length, first));
		if (f.kerningTable[first] == null) {
			f.kerningTable[first] = new MyKerning();
			f.kerningTable[first].kerning = new short[0];
		}
		PreventArrayOverflow<short>(ref f.kerningTable[first].kerning, Mathf.Max(f.kerningTable[first].kerning.Length, second));
		f.kerningTable[first].kerning[second] = (short) amount;
	}

	void AddInfo(MyFont f, string line) {
		string[] values = line.Split(' ');
		foreach (string val in values) {
			if (val.StartsWith("face=")) {
				int eqSign = val.IndexOf('=');
				string family = val.Substring(eqSign + 2, val.Length - eqSign - 3);
				if (family.EndsWith("MT")) {
					family = family.Substring(0, family.Length - 2);
				}
				if (family.EndsWith("-Bold")) {
					family = family.Substring(0, family.Length - 5);
					f.isBold = true;
				}
				else if (family.EndsWith("-BoldItalic")) {
					family = family.Substring(0, family.Length - 11);
					f.isBold = true;
					f.isItalic = true;
				}
				else if (family.EndsWith("-Italic")) {
					family = family.Substring(0, family.Length - 7);
					f.isItalic = true;
				}
				f.family = family;
			}
			else if (val.StartsWith("bold=") && !f.isBold) f.isBold = GetIntFromItem(val) != 0;
			else if (val.StartsWith("italic=") && !f.isItalic) f.isItalic = GetIntFromItem(val) != 0;
			else if (val.StartsWith("size=")) f.size = GetIntFromItem(val);
		}
		// Debug.Log("font '" + f.family + "' size " + f.size + " " + (f.isBold?"Bold":"") + (f.isItalic?"Italic":""));
	}
	
	void AddCommon(MyFont f, string line) {
		string[] values = line.Split(' ');
		foreach (string val in values) {
			if (val.StartsWith("base=")) f.baseLine = (short) GetIntFromItem(val);
			if (val.StartsWith("lineHeight=")) f.lineHeight = (short) GetIntFromItem(val);
		}
	}
	
	int GetKerning(MyFont f, char first, char second) {
		MyKerning k = null;
		if (f.kerningTable.Length <= first) return 0;
		k = f.kerningTable[(int) first];
		if ((k == null) || (k.kerning.Length <= second)) return 0;
		return k.kerning[second];
	}
	

	public void SetupRenderToTexture(Texture2D bgTex) {
		pixels = bgTex.GetPixels32();
		tw = bgTex.width;
		th = bgTex.height;
		maxPixel = pixels.Length;
	}
	
	public void SetupRenderToTexture(string imagePath, Texture2D bgTex) {
		this.imagePath = imagePath;
		SetupRenderToTexture(bgTex);
	}
	
	public void RenderToTexture(Texture2D destinationTex, int height) {
		height = Mathf.Min(height, th);
		destinationTex.Resize(tw, height, TextureFormat.ARGB32, false);
		if (height != th) {
			Color32[] newArray = new Color32[tw * height];
			System.Array.Copy(pixels, tw * (th - height), newArray, 0, height * tw);
			pixels = newArray;
		}
		destinationTex.SetPixels32(pixels);
		destinationTex.Apply();
		pixels = null;
	}
	
	public void RenderChar(MyFont f, MyChar mc, Color32 color, int x, int y) {
		byte[] b = mc.charData;
		int a = color.a;
		for (int yy = 0; yy < mc.height; yy++) {
			for (int xx = 0; xx < mc.width; xx++) {
				int alpha = (int) b[xx + mc.width * yy];
				if (alpha > 0) {
					int index = x + xx + mc.xOffset + (tw) * (th - (y - f.baseLine + mc.yOffset + yy - 1));
					if ((index >= 0) && (index < maxPixel)) {
						if ((alpha == 255) && (a == 255)) {
							pixels[index] = color;
						}
						else {
							pixels[index] = Color32.Lerp(pixels[index], color, ((float) (alpha * a)) / 65025.0f);
						}
					}
				}
			}
		}
	}
	
	public void RenderRenderBuffer(int y, int len) {
		for (int j = 0; j <= len; j++) {
			RenderBufferChar rbc = renderBuffer[j];
			RenderChar(rbc.font, rbc.mc, rbc.color, rbc.x, y);
		}
		renderBufferLen = 0;
	}
	
	public void RenderHR(Color32 color, int y, int x1, int x2) {
		if ((y < 0) || (y >= th)) return;
		int a = color.a;
		float lerpA = ((float) a) / 255.0f;
		for (int x = x1; x <= x2; x++) {
			int index = x + tw * (th - y - 1);
			if (a == 255) {
				pixels[index] = color;
			}
			else {
				pixels[index] = Color32.Lerp(pixels[index], color, lerpA);
			}
		}
	}
	
	public void RenderInternalImage(Texture2D tex, ref int y, int x1, int x2, Color32 multiply) {
		float ma = (float) multiply.a / (255 * 255);
		int width = tex.width;
		int height = tex.height;
//		Debug.Log("image size = " + width + " x1 " + height);
		Color32[] imgPixels = tex.GetPixels32();
		int minX = Mathf.Max(0, -x1);
		int maxX = Mathf.Min(Mathf.Min(x2 - x1, width), tw - x1);
		int minY = Mathf.Max(0, -y);
		int maxY = Mathf.Min(height, th - y);
		for (int yy = minY; yy < maxY; yy++) {
			int index1 = minX + x1 + tw * (th - y - yy - 1);
			int index2 = minX + width * (height - yy - 1);
			for (int xx = minX; xx < maxX; xx++) {
				Color32 c = imgPixels[index2];
				if (c.a > 0) {
					c.r = (byte) ((int) c.r * ((int) multiply.r) / 255); 
					c.g = (byte) ((int) c.g * ((int) multiply.g) / 255); 
					c.b = (byte) ((int) c.b * ((int) multiply.b) / 255);
					float alpha = (float) c.a * ma;
					pixels[index1] = Color32.Lerp(pixels[index1], c, alpha);
				}
				index1++;
				index2++;
			}
		}
		y += maxY;
	}
	
	public void RenderImage(string name, ref int y, int x1, int x2, Color32 multiply) {
		if (!System.IO.File.Exists(imagePath + name)) {
			Debug.LogError("Can't find image '" + name + "' at '" + imagePath + "'");
			return;
		}
		float ma = (float) multiply.a / (255 * 255);
		byte[] img = System.IO.File.ReadAllBytes(imagePath + name);
		Texture2D tex = new Texture2D(1, 1, TextureFormat.RGB24, false);
		tex.LoadImage(img);
		tex.Apply();
		int width = tex.width;
		int height = tex.height;
//		Debug.Log("image size = " + width + " x1 " + height);
		Color32[] imgPixels = tex.GetPixels32();
		int minX = Mathf.Max(0, -x1);
		int maxX = Mathf.Min(Mathf.Min(x2 - x1, width), tw - x1);
		int minY = Mathf.Max(0, -y);
		int maxY = Mathf.Min(height, th - y);
		for (int yy = minY; yy < maxY; yy++) {
			int index1 = minX + x1 + tw * (th - y - yy - 1);
			int index2 = minX + width * (height - yy - 1);
			for (int xx = minX; xx < maxX; xx++) {
				Color32 c = imgPixels[index2];
				if (c.a > 0) {
					c.r = (byte) ((int) c.r * ((int) multiply.r) / 255); 
					c.g = (byte) ((int) c.g * ((int) multiply.g) / 255); 
					c.b = (byte) ((int) c.b * ((int) multiply.b) / 255);
					float alpha = (float) c.a * ma;
					pixels[index1] = Color32.Lerp(pixels[index1], c, alpha);
				}
				index1++;
				index2++;
			}
		}
		Destroy(tex);
		y += maxY;
	}

	public void RenderText(int fontIndex, Color32 color,
		string str, ref int x, ref int y, int startX, int endX, float lineSpacing) {
		MyFont f = fonts[fontIndex];
		RenderText(f, color, str, ref x, ref y, startX, endX, lineSpacing);
	}
	
	public void RenderText(MyFont f, Color32 color,
		string str, ref int x, ref int y, int startX, int endX, float lineSpacing) {
		
		// Color32 color = new Color32(0, 0, 0, 255);
		char lastC = (char) 0;
		int i = 0;
		while (i < str.Length) {
			char c = str[i];
			if (c == '\n') {
				RenderRenderBuffer(y, renderBufferLen - 1);
				y += (int) ((float) f.lineHeight * lineSpacing);
				x = startX;
			}
			else if (c == ' ') {
				MyChar mc = f.charTable[32];
				x += mc.xAdvance;
				if (renderBufferLen > 0) renderBuffer[renderBufferLen - 1].isFollowedBySpace = true;
			}
			else if ((c == '*') && (i < str.Length - 1) && (str[i+1] == '*')) {
				f = FindFont(f, !f.isBold, f.isItalic);
				i++;
			}
			else if ((c == '_') && (i < str.Length - 1) && (str[i+1] == '_')) {
				f = FindFont(f, f.isBold, !f.isItalic);
				i++;
			}
			else if (c == hyphenChar) {
				if (renderBufferLen > 0) renderBuffer[renderBufferLen - 1].isHyphenOption = true;				
			}
			else {
				MyChar mc = null;
				if ((int) c < f.charTable.Length) mc = f.charTable[(int) c];
				if (mc != null) {
					x += GetKerning(f, lastC, c);
					if (renderBufferLen + 1 >= renderBuffer.Length) {
						RenderBufferChar[] rbcArray = renderBuffer;
						renderBuffer = new RenderBufferChar[renderBufferLen + 128];
						System.Array.Copy(rbcArray, renderBuffer, rbcArray.Length);
					}
					renderBuffer[renderBufferLen++] = new RenderBufferChar(x, f, mc, color, i);
					// RenderChar(f, mc, color, x, y);					
					x += mc.xAdvance;
					if (x >= endX) {
						// find last hypenplace or space
						while (renderBufferLen > 0) {
							RenderBufferChar rbc = renderBuffer[--renderBufferLen];
							if (rbc.isFollowedBySpace) {
								RenderRenderBuffer(y, renderBufferLen);
								y += (int) ((float) f.lineHeight * lineSpacing);
								x = startX;
								i = rbc.index + 1;
								f = rbc.font;
								break;
							}
							else if (rbc.isHyphenOption) {
								MyChar hyphen = null;
								if (rbc.font.charTable.Length > (int) hyphenChar) hyphen = rbc.font.charTable[(int) hyphenChar];
								if (hyphen == null) {
									hyphen = rbc.font.charTable[(int) '-'];
								}
								if (rbc.x + hyphen.xAdvance < endX) {
									RenderChar(rbc.font, hyphen, rbc.color, renderBuffer[renderBufferLen+1].x, y);
									RenderRenderBuffer(y, renderBufferLen);
									y += (int) ((float) f.lineHeight * lineSpacing);
									x = startX;
									i = rbc.index + 1;
									f = rbc.font;
									break;
								}
							}
						}
					}
				}
				else {
					Debug.Log("Unsupported character '" + c + "' code " + (int) c);
				}
			}
			lastC = c;
			i++;
		}
	}
	
	public void Awake() {
		self = this;
//		Debug.Log("Color " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color)));
//		Debug.Log("Color32 " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(Color32)));
		
		foreach (MyFont f in fonts) {
			Color32[] pixels = f.fontTexture.GetPixels32();
			int width = f.fontTexture.width;
			int height = f.fontTexture.height;
			f.charTable = new MyChar[0];
			f.kerningTable = new MyKerning[0];
			
			string[] lines = f.fontData.text.Split('\n');
			foreach (string l in lines) {
				if (l.StartsWith("chars")) {
				}
				else if (l.StartsWith("info")) {
					AddInfo(f, l);
				}
				else if (l.StartsWith("common")) {
					AddCommon(f, l);
				}
				else if (l.StartsWith("char")) {
					AddChar(f, pixels, width, height, l);
				}
				else if (l.StartsWith("kerning")) {
					AddKerning(f, l);
				}
			}
		}
	}
	
	
	private string StringMatchCase(string str, string expr) {
		if (char.IsUpper(expr[0]) && (str.Length > 0)) {
			return char.ToUpper(str[0]) + str.Substring(1);
		}
		else return str;
	}
	
	
	private string HandleExpression(string expr, Scene scene) {
		PlayerInfo pInfo = scene.playerInfo;
		if (expr.ToLower() == "voornaam") {
			if (pInfo != null) {
				return StringMatchCase(pInfo.firstName, expr);
			}
			else {
				return "Jan";
			}
		}
		else if (expr.ToLower() == "achternaam") {
			if (pInfo != null) {
				return StringMatchCase(pInfo.familyName, expr);
			}
			else {
				return StringMatchCase("de Vries", expr);
			}
		}
		else if (expr.ToLower() == "hijzij") {
			bool man = ((pInfo == null) || (pInfo.isMale));
			return StringMatchCase(man?"hij":"zij", expr);
		}
		else if (expr.ToLower() == "zijnhaar") {
			bool man = ((pInfo == null) || (pInfo.isMale));
			return StringMatchCase(man?"zijn":"haar", expr);
		}
		else if (expr.ToLower() == "hemhaar") {
			bool man = ((pInfo == null) || (pInfo.isMale));
			return StringMatchCase(man?"hem":"haar", expr);
		}
		else {
			throw new EcoException ("other expressions ('" + expr + "') not yet implemented!");
		}
	}
	
	public int RenderNewsArticle(string str, Scene scene, Texture2D outTex, bool substituteExpressions) {
		imagePath = GameSettings.GetPathForScene (scene.sceneName) + "ArticleData" + Path.DirectorySeparatorChar;
		int startX = 16;
		int x = startX;
		int startCol = 64;
		int colWidth = 232;
		int y = 64;
		int maxY = y;
		int fixHeight = -1;
		
		Color32 ink = new Color32(0, 0, 0, 192);
		Color32 imgColor = new Color32(255, 255, 255, 192);
		MyFont titleFont = FindFont("CharisSIL", 38, true, false);
		MyFont introFont = FindFont("CharisSIL", 14, false, false);
		MyFont normalFont = FindFont("CharisSIL", 10, false, false);
		float titleLS = 0.65f;
		float introLS = 0.75f;
		float normalLS = 0.75f;
		string[] lines = str.Split('\n');

		
		bool hasSetup = false;
		
		for (int i = 0; i < lines.Length; i++) {
			string line = lines[i].Trim();
			string cmd = "par";
			if (line.StartsWith("[")) {
				int end = line.IndexOf(']');
				if (end > 0) {
					cmd = line.Substring(1, end - 1).ToLower();
					line = line.Substring(end + 1).TrimStart();
				}
				else {
					cmd = line.Substring(1);
					line = "";
				}
			}
			if (substituteExpressions) {
				line = scene.expression.ParseAndSubstitute (line, true);
			}
			
			if (!hasSetup) {
				if (cmd == "letter") {
					titleFont = FindFont("Courier", 16, false, false);
					introFont = FindFont("Courier", 16, false, false);
					normalFont = FindFont("Courier", 16, false, false);
					titleLS = 1.2f;
					introLS = 1.2f;
					normalLS = 1.2f;
					colWidth = 480;
					SetupRenderToTexture(bgTexList[1]);
				}
				else if ((cmd == "encyclopedia") || (cmd == "enc")) {
					titleFont = FindFont("Arial", 18, false, false);
					introFont = FindFont("Arial", 14, false, false);
					normalFont = FindFont("Arial", 12, false, false);
					titleLS = 1.2f;
					introLS = 1.2f;
					normalLS = 1.2f;
					colWidth = 480;
					y = 24;
					maxY = 24;
					ink = new Color32(0, 0, 0, 255);
					imgColor = new Color32(255, 255, 255, 255);
					SetupRenderToTexture(bgTexList[2]);
				}
				else if (cmd == "avatar") {
					titleFont = FindFont("Arial", 18, false, false);
					introFont = FindFont("Arial", 14, true, false);
					normalFont = FindFont("Arial", 14, false, false);
					titleLS = 1.2f;
					introLS = 1.2f;
					normalLS = 1.2f;
					startX = 16;
					x = startX;
					colWidth = 496;
					y = 96;
					maxY = 256;
					fixHeight = 256;
					ink = new Color32(255, 255, 255, 255);
					imgColor = new Color32(255, 255, 255, 255);
					SetupRenderToTexture(bgTexList[3]);
//					if (scene.progression.year >= scene.progression.startYear) {
//						SetupRenderToTexture(bgTexList[4]);
//					}
//					else {
//						SetupRenderToTexture(bgTexList[3]);
//					}
					Texture2D avTex = buildinTextures[0];
					line = line.Trim().ToLower();
					
					if (line != "") {
						foreach (Texture2D tex in buildinTextures) {
							if (tex.name.ToLower() == line) {
								avTex = tex;
								break;
							}
						}
					}
					int dummyY = 0;
					RenderInternalImage(avTex, ref dummyY, 512, 512 + 256, imgColor);
				}
				else {
					SetupRenderToTexture(bgTexList[0]);
					RenderHR(ink, 16, 16, 512 - 16);
				}
				hasSetup = true;
			}
			
			if (cmd == "par" || cmd == "") {
				RenderText(normalFont, ink, line + '\n', ref x, ref y, x, startX + colWidth, normalLS);
			}
			else if (cmd == "title") {
				RenderText(titleFont, ink, line + '\n', ref x, ref y, startX, 512-16, titleLS);
				startCol = y;
			}
			else if (cmd == "intro") {
				RenderText(introFont, ink, line + '\n', ref x, ref y, startX, startX + colWidth, introLS);
			}
			else if (cmd == "img") {
				y -= 10;
				RenderImage(line, ref y, startX, startX + colWidth, imgColor);
				y += 14;
			}
			else if (cmd == "col") {
				if (colWidth < 480) {
					y = startCol;
					startX = 256 + 8;
					x = startX;
				}
			}
			else if (cmd == "hr") {
				RenderHR(ink, y, 16, 512 - 16);
			}
			else if (cmd == "v") {
				try {
					float delta;
					if (float.TryParse (line, out delta)) {
						y += Mathf.RoundToInt (delta);
					}
					else throw new EcoException("expression should return number or float");
				}
				catch (System.Exception e) {
					Log.LogError(e.Message);
				}
			}
			maxY = Mathf.Max(y, maxY);
			if (fixHeight >= 0) maxY = fixHeight;
		}
		if (scene.progression.year < scene.progression.startYear) {
			RenderHR(ink, maxY, 16, 512 - 16);
		}
		
		RenderToTexture(outTex, maxY);
		return maxY;
	}
	
	/**
	 * But of a hack solution, as we want to evaluate expressions as soon as messages are added to the
	 * queue we need a way to prevent the render commands from messing up the embedded expressions
	 * as they both use '[' ']' to mark their place in the string. Easy way out would of course make
	 * the render commands use another start, end marks, but to keep things a bit comparable with
	 * EcoSim 1 we just use this function process the string, recognise the render commands and leave
	 * those in, but process the embedded expressions. The resulting string is thus str with
	 * all embedded expressions processed.
	 */
	public static string SubstituteExpressions (string str, Scene scene) {
		string[] lines = str.Split('\n');
		StringBuilder result = new StringBuilder (str.Length + 256);
		for (int i = 0; i < lines.Length; i++) {
			string line = lines[i].Trim ();
			if (i > 0) {
				result.Append ('\n');
			}
			int index;
			if (line.StartsWith ("[") && ((index = line.IndexOf (']')) >= 0)) {
				result.Append (line.Substring (0, index + 1));
				result.Append (scene.expression.ParseAndSubstitute (line.Substring (index + 1), true));
			}
			else {
				result.Append (scene.expression.ParseAndSubstitute (line, true));
			}
		}
		return result.ToString ();
	}
}
