using Terminal.Gui;

namespace XDS.Messaging.TerminalChat.ChatUI
{
    public static class Theme
    {
        public static Color AccentColor = Color.Cyan;

        public static Color DarkColor = Color.Black;

        public static Color LightColor = Color.Gray;

        public static ColorScheme CreateColorScheme()
        {
            var cs = new ColorScheme();

            Colors.Menu.Normal = Application.Driver.MakeAttribute(LightColor, DarkColor);

            cs.Normal = Application.Driver.MakeAttribute(LightColor, DarkColor);

            cs.HotNormal = Application.Driver.MakeAttribute(AccentColor, DarkColor);

            // Color for text fields.
            cs.Focus = Application.Driver.MakeAttribute(DarkColor, LightColor);

            // first letter of a button text
            cs.HotFocus = Application.Driver.MakeAttribute(AccentColor, LightColor);

            // not used so far!
            cs.Disabled = Application.Driver.MakeAttribute(Color.White, LightColor);
            return cs;
        }
    }
}
