using System.Windows.Controls;

namespace Chord.Views
{
    class APIComboBoxItem : ComboBoxItem
    {
        public APIType APIType { get; set; }

        public APIComboBoxItem(APIType apiType, string content)
        {
            APIType = apiType;
            Content = content;
        }
    }
}
