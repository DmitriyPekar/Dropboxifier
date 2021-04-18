using System;
using System.Windows.Controls;
using Dropboxifier.Model;
using System.Windows.Data;

namespace Dropboxifier.Validators
{
    public class LinkedFolderValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (((BindingGroup)value).Items.Count == 0)
            {
                return new ValidationResult(true, null);
            }

            LinkedFolder link = (value as BindingGroup).Items[0] as LinkedFolder;
            if (link == null)
            {
                return new ValidationResult(false, "Row item must be of type LinkedFolder");
            }

            if (link.ResolvedForCurrentPC)
            {
                return new ValidationResult(true, null);
            }
            else
            {
                return new ValidationResult(false, "Link is not resolved");
            }
        }
    }
}
