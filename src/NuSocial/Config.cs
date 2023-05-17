using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuSocial
{
    internal class Config
    {
        public static bool IsFirstLogin { get; internal set; }

        internal static void ResetVersions()
        {
            throw new NotImplementedException();
        }
    }
}
