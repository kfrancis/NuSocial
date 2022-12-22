using Markdig;
using Markdig.Renderers.Roundtrip;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial
{
    public class ContentStringToMarkdownHtmlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //try
            //{
            //    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            //    return Markdown.Parse((string)value, true).ToHtml(pipeline);
            //}
            //catch (Exception)
            //{
            //    return string.Empty;
            //}
            return (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

