using NuSocial.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial.Core.View
{
    /// <summary>
    /// Interface used to couple View object to a ViewModel
    /// </summary>
    public interface IViewForBase
    {
        object ViewModel { get; set; }
    }

    /// <summary>
    /// Interface used to couple View object to a ViewModel
    /// </summary>
    public interface IViewFor<T> : IViewForBase where T : BaseViewModel
    {
        new T ViewModel { get; set; }
    }

    /// <summary>
    /// Interface used to couple View object to a ViewModel
    /// </summary>
    public interface IPopupFor<T> : IViewForBase where T : BasePopupModel
    {
        new T ViewModel { get; set; }
    }

    /// <summary>
    /// Interface used to couple form object to a ViewModel
    /// </summary>
    public interface IFormFor<T> : IViewForBase where T : BaseFormModel
    {
        new T ViewModel { get; set; }
    }
}
