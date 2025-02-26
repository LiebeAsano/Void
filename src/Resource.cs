using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace VoidTemplate.Resource
{
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class TheVoidRes
    {
        internal TheVoidRes()
        {
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static ResourceManager ResourceManager
        {
            get
            {
                bool flag = TheVoidRes.resourceMan == null;
                if (flag)
                {
                    ResourceManager resourceManager = new ResourceManager("TheVoid.Resource.TheVoidRes", typeof(TheVoidRes).Assembly);
                    TheVoidRes.resourceMan = resourceManager;
                }
                return TheVoidRes.resourceMan;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        internal static CultureInfo Culture
        {
            get
            {
                return TheVoidRes.resourceCulture;
            }
            set
            {
                TheVoidRes.resourceCulture = value;
            }
        }

        internal static string Origs
        {
            get
            {
                return TheVoidRes.ResourceManager.GetString("Origs", TheVoidRes.resourceCulture);
            }
        }

        internal static string Translations
        {
            get
            {
                return TheVoidRes.ResourceManager.GetString("Translations", TheVoidRes.resourceCulture);
            }
        }

        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;
    }
}