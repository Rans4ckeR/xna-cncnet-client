﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class INItializableWindow : XNAPanel
{
    private bool _initialized = false;
    private bool hasCloseButton = false;

    public INItializableWindow(WindowManager windowManager)
        : base(windowManager)
    {
    }

    protected CCIniFile ConfigIni { get; private set; }

    /// <summary>
    /// Gets or sets if not null, the client will read an INI file with this name instead of the
    /// window's name.
    /// </summary>
    protected string IniNameOverride { get; set; }

    public T FindChild<T>(string childName, bool optional = false)
        where T : XNAControl
    {
        T child = FindChild<T>(Children, childName);
        if (child == null && !optional)
            throw new KeyNotFoundException("Could not find required child control: " + childName);

        return child;
    }

    public override void Initialize()
    {
        if (_initialized)
            throw new InvalidOperationException("INItializableWindow cannot be initialized twice.");

        string configIniPath = GetConfigPath();

        if (string.IsNullOrEmpty(configIniPath))
        {
            base.Initialize();
            return;
        }

        ConfigIni = new CCIniFile(configIniPath);

        if (Parser.Instance == null)
            _ = new Parser(WindowManager);

        Parser.Instance.SetPrimaryControl(this);
        ReadINIForControl(this);
        ReadLateAttributesForControl(this);

        ParseExtraControls();

        base.Initialize();

        _initialized = true;
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        if (key == "HasCloseButton")
            hasCloseButton = iniFile.GetBooleanValue(Name, key, hasCloseButton);

        base.ParseAttributeFromINI(iniFile, key, value);
    }

    /// <summary>
    /// Attempts to locate the ini config file for the current control. Only return a config path if
    /// it exists.
    /// </summary>
    /// <returns>The ini config file path.</returns>
    protected string GetConfigPath()
    {
        string iniFileName = string.IsNullOrWhiteSpace(IniNameOverride) ? Name : IniNameOverride;

        // get theme specific path
        string configIniPath = Path.Combine(ProgramConstants.GetResourcePath(), $"{iniFileName}.ini");
        if (File.Exists(configIniPath))
            return configIniPath;

        // get base path
        configIniPath = Path.Combine(ProgramConstants.GetBaseResourcePath(), $"{iniFileName}.ini");
        if (File.Exists(configIniPath))
            return configIniPath;

        if (iniFileName == Name)
            return null; // IniNameOverride must be null, no need to continue

        iniFileName = Name;

        // get theme specific path
        configIniPath = Path.Combine(ProgramConstants.GetResourcePath(), $"{iniFileName}.ini");
        if (File.Exists(configIniPath))
            return configIniPath;

        // get base path
        configIniPath = Path.Combine(ProgramConstants.GetBaseResourcePath(), $"{iniFileName}.ini");
        return File.Exists(configIniPath) ? configIniPath : null;
    }

    protected void ReadINIForControl(XNAControl control)
    {
        IniSection section = ConfigIni.GetSection(control.Name);
        if (section == null)
            return;

        Parser.Instance.SetPrimaryControl(this);

        foreach (KeyValuePair<string, string> kvp in section.Keys)
        {
            if (kvp.Key.StartsWith("$CC"))
            {
                XNAControl child = CreateChildControl(control, kvp.Value);
                ReadINIForControl(child);
                child.Initialize();
            }
            else if (kvp.Key == "$X")
            {
                control.X = Parser.Instance.GetExprValue(kvp.Value, control);
            }
            else if (kvp.Key == "$Y")
            {
                control.Y = Parser.Instance.GetExprValue(kvp.Value, control);
            }
            else if (kvp.Key == "$Width")
            {
                control.Width = Parser.Instance.GetExprValue(kvp.Value, control);
            }
            else if (kvp.Key == "$Height")
            {
                control.Height = Parser.Instance.GetExprValue(kvp.Value, control);
            }
            else if (kvp.Key == "$TextAnchor" && control is XNALabel label)
            {
                // TODO refactor these to be more object-oriented
                label.TextAnchor = (LabelTextAnchorInfo)Enum.Parse(typeof(LabelTextAnchorInfo), kvp.Value);
            }
            else if (kvp.Key == "$AnchorPoint" && control is XNALabel label1)
            {
                string[] parts = kvp.Value.Split(',');
                if (parts.Length != 2)
                    throw new FormatException("Invalid format for AnchorPoint: " + kvp.Value);
                label1.AnchorPoint = new Vector2(Parser.Instance.GetExprValue(parts[0], control), Parser.Instance.GetExprValue(parts[1], control));
            }
            else if (kvp.Key == "$LeftClickAction")
            {
                if (kvp.Value == "Disable")
                    control.LeftClick += (s, e) => Disable();
            }
            else
            {
                control.ParseAttributeFromINI(ConfigIni, kvp.Key, kvp.Value);
            }
        }
    }

    private XNAControl CreateChildControl(XNAControl parent, string keyValue)
    {
        string[] parts = keyValue.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2)
            throw new INIConfigException("Invalid child control definition " + keyValue);

        string childName = parts[0];
        if (string.IsNullOrEmpty(childName))
            throw new INIConfigException("Empty name in child control definition for " + parent.Name);

        XNAControl childControl = ClientGUICreator.Instance.CreateControl(WindowManager, parts[1]);

        if (Array.Exists(childName.ToCharArray(), c => !char.IsLetterOrDigit(c) && c != '_'))
            throw new INIConfigException("Names of INItializableWindow child controls must consist of letters, digits and underscores only. Offending name: " + parts[0]);

        childControl.Name = childName;
        parent.AddChildWithoutInitialize(childControl);
        return childControl;
    }

    private T FindChild<T>(IEnumerable<XNAControl> list, string controlName)
                where T : XNAControl
    {
        foreach (XNAControl child in list)
        {
            if (child.Name == controlName)
                return (T)child;

            T childOfChild = FindChild<T>(child.Children, controlName);
            if (childOfChild != null)
                return childOfChild;
        }

        return null;
    }

    private void ParseExtraControls()
    {
        IniSection section = ConfigIni.GetSection("$ExtraControls");

        if (section == null)
            return;

        foreach (KeyValuePair<string, string> kvp in section.Keys)
        {
            if (!kvp.Key.StartsWith("$CC"))
                continue;

            string[] parts = kvp.Value.Split(':');
            if (parts.Length != 2)
                throw new ClientConfigurationException("Invalid $ExtraControl specified in " + Name + ": " + kvp.Value);

            if (!Children.Any(child => child.Name == parts[0]))
            {
                XNAControl control = CreateChildControl(this, kvp.Value);
                control.Name = parts[0];
                control.DrawOrder = -Children.Count;
                ReadINIForControl(control);
            }
        }
    }

    private void ReadINIRecursive(XNAControl control)
    {
        ReadINIForControl(control);

        foreach (XNAControl child in control.Children)
            ReadINIRecursive(child);
    }

    /// <summary>
    /// Reads a second set of attributes for a control's child controls. Enables linking controls to
    /// controls that are defined after them.
    /// </summary>
    private void ReadLateAttributesForControl(XNAControl control)
    {
        IniSection section = ConfigIni.GetSection(control.Name);
        if (section == null)
            return;

        List<XNAControl> children = Children.ToList();
        foreach (XNAControl child in children)
        {
            // This logic should also be enabled for other types in the future, but it requires
            // changes in XNAUI
            if (child is not XNATextBox)
                continue;

            IniSection childSection = ConfigIni.GetSection(child.Name);
            if (childSection == null)
                continue;

            string nextControl = childSection.GetStringValue("NextControl", null);
            if (!string.IsNullOrWhiteSpace(nextControl))
            {
                XNAControl otherChild = children.Find(c => c.Name == nextControl);
                if (otherChild != null)
                    ((XNATextBox)child).NextControl = otherChild;
            }

            string previousControl = childSection.GetStringValue("PreviousControl", null);
            if (!string.IsNullOrWhiteSpace(previousControl))
            {
                XNAControl otherChild = children.Find(c => c.Name == previousControl);
                if (otherChild != null)
                    ((XNATextBox)child).PreviousControl = otherChild;
            }
        }
    }
}