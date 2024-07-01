using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace TheVoid.Resource
{
    // Token: 0x02000005 RID: 5
    [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    internal class TheVoidRes
    {
        // Token: 0x06000004 RID: 4 RVA: 0x00002097 File Offset: 0x00000297
        internal TheVoidRes()
        {
        }

        // Token: 0x17000001 RID: 1
        // (get) Token: 0x06000005 RID: 5 RVA: 0x000020A4 File Offset: 0x000002A4
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

        // Token: 0x17000002 RID: 2
        // (get) Token: 0x06000006 RID: 6 RVA: 0x000020EC File Offset: 0x000002EC
        // (set) Token: 0x06000007 RID: 7 RVA: 0x00002103 File Offset: 0x00000303
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

        // Token: 0x17000003 RID: 3
        // (get) Token: 0x06000008 RID: 8 RVA: 0x0000210C File Offset: 0x0000030C
        internal static string Origs
        {
            get
            {
                return TheVoidRes.ResourceManager.GetString("Origs", TheVoidRes.resourceCulture);
            }
        }

        // Token: 0x17000004 RID: 4
        // (get) Token: 0x06000009 RID: 9 RVA: 0x00002134 File Offset: 0x00000334
        internal static string Translations
        {
            get
            {
                return TheVoidRes.ResourceManager.GetString("Translations", TheVoidRes.resourceCulture);
            }
        }

        // Token: 0x04000003 RID: 3
        private static ResourceManager resourceMan;

        // Token: 0x04000004 RID: 4
        private static CultureInfo resourceCulture;
    }
}