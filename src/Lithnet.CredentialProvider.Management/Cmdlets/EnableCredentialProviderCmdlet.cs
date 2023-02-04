using System;
using System.Management.Automation;
using Lithnet.CredentialProvider.Management;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsLifecycle.Enable, "CredentialProvider", DefaultParameterSetName = "ByFileName")]
    public class EnableCredentialProviderCmdlet : PSCmdlet
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
                    throw new System.Exception("This tool cannot enable managed assemblies by file name. You can enable native assemblies using the CLSID or ProgID");
                }

                using (var assembly = RegistrationServices.LoadAssembly(this.File))
                {
                    foreach (var type in RegistrationServices.GetCredentialProviders(assembly.Assembly))
                    {
                        RegistrationServices.EnableCredentialProvider(type);
                        this.WriteVerbose($"Enabled credential provider {type.FullName}");
                    }
                }
            }
            else if (this.ParameterSetName == "ByClsid")
            {
                RegistrationServices.EnableCredentialProvider(this.Clsid);
            }
            else if (this.ParameterSetName == "ByProgId")
            {
                var clsid = RegistrationServices.GetClsidFromProgId(this.ProgId);
                RegistrationServices.EnableCredentialProvider(clsid);
            }
        }
    }
}
