using System.IO;
using System.Windows.Controls;

namespace Dropboxifier.Validators
{
    public class FolderValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string strValue = value as string;
            if (strValue == null)
            {
                return new ValidationResult(false, "value was not a string");
            }

            if (Directory.Exists(strValue))
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Folder not found");
            }
        }
    }
}
