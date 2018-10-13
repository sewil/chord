using IniParser;
using IniParser.Model.Configuration;
using IniParser.Parser;
using System.Windows;

namespace Chord
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static IniDataParser iniParser = new IniDataParser(new IniParserConfiguration
        {
            CaseInsensitive = true,
            AllowDuplicateKeys = true,
            SkipInvalidLines = true
        });
        internal static FileIniDataParser fileIniParser = new FileIniDataParser(iniParser);
    }
}
