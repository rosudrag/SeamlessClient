using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeamlessClient.Utilities
{
    public static class UtilExtensions
    {
        public static dynamic CastToReflected(this object o, Type type) => Convert.ChangeType(o, type);


    }
}
