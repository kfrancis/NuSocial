using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NuSocial.Services
{
    public interface IImageService
    {

    }
    public class ImageService : IImageService, ITransientDependency
    {
    }
}
