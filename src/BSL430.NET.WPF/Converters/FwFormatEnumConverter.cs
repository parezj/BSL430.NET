using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;


namespace BSL430_NET_WPF.Converters
{
    public class FwFormatEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = typeof(BSL430_NET.FirmwareTools.FwTools.FwFormat);
            var name = Enum.GetName(type, value);
            FieldInfo fi = type.GetField(name);
            var descriptionAttrib = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            return descriptionAttrib.Description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
