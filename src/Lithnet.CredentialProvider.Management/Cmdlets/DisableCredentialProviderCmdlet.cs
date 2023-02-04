using System;
using System.Management.Automation;
using Lithnet.CredentialProvider.Management;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsLifecycle.Disable, "CredentialProvider", DefaultParameterSetName = "ByFileName")]
    public class DisableCredentialProviderCmdlet : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByFileName", Position = 1, HelpMessage = "The path to a .NET credential provider DLL")]
        public string File { get; set; }

        [Parameter(ParameterSetName = "ByClsid", HelpMessage = "The CLSID of the credential provider")]
        public Guid Clsid { get; set; }

        [Parameter(ParameterSetName = "ByProgId", HelpMessage = "The ProgId of the credential provider")]
        public string ProgId { get; set; }

        protected override void BeginProcessing()
        {
            NativeMethods.ThrowIfNotAdmin();
        }

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "ByFileName")
            {
                if (!RegistrationServices.IsManagedAssembly(this.File))
                {
                    throw new Exception("This tool cannot disable managed assemblies by file name. You can disable native assemblies using the CLSID or ProgID");
                }

                using (var assembly = RegistrationServices.LoadAssembly(this.File))
                {
                    foreach (var type in RegistrationServices.GetCredentialProviders(assembly.Assembly))
                    {
                        RegistrationServices.DisableCredentialProvider(type);
                        this.WriteVerbose($"Disabled credential provider {type.FullName}");
                    }
                }
            }
            else if (this.ParameterSetName == "ByClsid")
            {
                RegistrationServices.DisableCredentialProvider(this.Clsid);
            }
            else if (this.ParameterSetName == "ByProgId")
            {
                var clsid = RegistrationServices.GetClsidFromProgId(this.ProgId);
                RegistrationServices.DisableCredentialProvider(clsid);
            }
        }
    }
}
