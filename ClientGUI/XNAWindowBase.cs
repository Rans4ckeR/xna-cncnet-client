using System;
using System.Linq;
using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    public abstract class XNAWindowBase : XNAPanel
    {
        private readonly IServiceProvider serviceProvider;

        protected XNAWindowBase(
            WindowManager windowManager,
            IServiceProvider serviceProvider)
            : base(windowManager)
        {
            this.serviceProvider = serviceProvider;
            PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.TILED;
        }

        /// <summary>
        /// Reads extra control information from a specific section of an INI file.
        /// </summary>
        /// <param name="iniFile">The INI file.</param>
        /// <param name="sectionName">The section.</param>
        protected void ParseExtraControls(IniFile iniFile, string sectionName)
        {
            var section = iniFile.GetSection(sectionName);

            if (section == null)
                return;

            foreach (var kvp in section.Keys)
            {
                string[] parts = kvp.Value.Split(':');
                if (parts.Length != 2)
                    throw new ClientConfigurationException("Invalid ExtraControl specified in " + Name + ": " + kvp.Value);

                if (Children.All(child => child.Name != parts[0]))
                {
                    // todo DI
                    XNAControl control = (XNAControl)serviceProvider.GetService(Type.GetType($"ClientGUI.{parts[1]}, ClientGUI"));

                    //XNAControl control = ClientGUICreator.GetXnaControl(parts[1]);
                    control.Name = parts[0];
                    control.DrawOrder = -Children.Count;
                    AddChild(control);
                }
            }
        }

        protected void ReadChildControlAttributes(IniFile iniFile)
        {
            foreach (XNAControl child in Children)
            {
                if (!(typeof(XNAWindowBase).IsAssignableFrom(child.GetType())))
                    child.GetAttributes(iniFile);
            }
        }
    }
}