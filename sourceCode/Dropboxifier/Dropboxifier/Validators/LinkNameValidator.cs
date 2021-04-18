using System;
using System.Windows.Controls;

namespace Dropboxifier.Validators
{
    public class LinkNameValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string strValue = value as string;
            if (strValue == null)
            {
                return new ValidationResult(false, "Object must be of type string");
            }

            if (String.IsNullOrEmpty(strValue))
            {
                return new ValidationResult(false, "String cannot be empty");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
