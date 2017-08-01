using UnityEngine;
using System.Reflection;
using System.Collections;
using System;

/// <summary>
/// Linux could have a problem where the default Arial font is missing,
/// this class replaces all null fonts with the given font to prevent rendering issues.
/// </summary>
public class LinuxFontsFixer : MonoBehaviour
{
    public Font defaultFont;
    public GUISkin[] skins;

#if UNITY_STANDALONE_LINUX

    void Awake ()
    {
        if (Application.isEditor)
            return;

        for (int i = 0; i < this.skins.Length; i++)
        {
            this.ProcessGUISkin(this.skins[i]);
        }
    }

    private void ProcessGUISkin(GUISkin skin)
    {
        if (skin.font == null)
            skin.font = this.defaultFont;

        this.ProcessGUIStyle(skin.box);
        this.ProcessGUIStyle(skin.button);
        this.ProcessGUIStyle(skin.toggle);
        this.ProcessGUIStyle(skin.label);
        this.ProcessGUIStyle(skin.textField);
        this.ProcessGUIStyle(skin.textArea);
        this.ProcessGUIStyle(skin.window);
        this.ProcessGUIStyle(skin.horizontalSlider);
        this.ProcessGUIStyle(skin.horizontalSliderThumb);
        this.ProcessGUIStyle(skin.verticalSlider);
        this.ProcessGUIStyle(skin.verticalSliderThumb);
        this.ProcessGUIStyle(skin.horizontalScrollbar);
        this.ProcessGUIStyle(skin.horizontalScrollbarThumb);
        this.ProcessGUIStyle(skin.horizontalScrollbarLeftButton);
        this.ProcessGUIStyle(skin.horizontalScrollbarRightButton);
        this.ProcessGUIStyle(skin.verticalScrollbar);
        this.ProcessGUIStyle(skin.verticalScrollbarThumb);
        this.ProcessGUIStyle(skin.verticalScrollbarUpButton);
        this.ProcessGUIStyle(skin.verticalScrollbarDownButton);
        this.ProcessGUIStyle(skin.scrollView);

        for (int i = 0; i < skin.customStyles.Length; i++)
        {
            this.ProcessGUIStyle(skin.customStyles[i]);
        }
    }

    private void ProcessGUIStyle(GUIStyle style)
    {
        if (style.font == null)
            style.font = this.defaultFont;
    }

#endif
}
