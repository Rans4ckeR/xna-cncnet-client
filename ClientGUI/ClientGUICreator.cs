using Rampastring.XNAUI;

namespace ClientGUI;

/// <summary>
/// A GUI creator that also includes ClientGUI's custom controls in addition
/// to the controls of Rampastring.XNAUI.
/// </summary>
public class ClientGUICreator : GUICreator
{
    private static ClientGUICreator _instance;

    public ClientGUICreator()
    {
        AddControl(typeof(XNAClientButton));
        AddControl(typeof(XNAClientCheckBox));
        AddControl(typeof(XNAClientDropDown));
        AddControl(typeof(XNALinkButton));
        AddControl(typeof(XNAExtraPanel));
    }

    public static ClientGUICreator Instance => _instance ??= new();
}