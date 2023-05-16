using Markdig;
using Markdig.Renderers.Roundtrip;
using Markdig.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial
{
    public class ContentStringToMarkdownHtmlConverter : IValueConverter
    {
        private static MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseEmphasisExtras().UseGridTables().UsePipeTables().UseTaskLists().UseAutoLinks().Build();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            { 
                return Markdown.Parse((string)value, true).ToHtml(Pipeline);
            }
            catch (Exception)
            {
                return (string)value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

