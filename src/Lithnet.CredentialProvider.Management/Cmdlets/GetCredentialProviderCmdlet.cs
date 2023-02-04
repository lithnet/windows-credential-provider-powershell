using System;
using System.Management.Automation;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsCommon.Get, "CredentialProvider", DefaultParameterSetName = "None")]
    public class GetCredentialProviderCmdlet : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByFileName", Position = 1, HelpMessage = "The path to a .NET credential provider DLL")]
        public string File { get; set; }

        [Parameter(ParameterSetName = "ByClsid", HelpMessage = "The CLSID of the credential provider")]
        public Guid Clsid { get; set; }

        [Parameter(ParameterSetName = "ByProgId", HelpMessage = "The ProgId of the credential provider")]
        public string ProgId { get; set; }

        protected override void ProcessRecord()
        {
            if (this.ParameterSetName == "None")
            {
                foreach (var item in RegistrationServices.GetCredentalProviders())
                {
                    this.WriteObject(item);
                }
            }
            else if (this.ParameterSetName == "ByFileName")
            {
                using (var assembly = RegistrationServices.LoadAssembly(this.File))
                {
                    foreach (var type in RegistrationServices.GetCredentialProviders(assembly.Assembly))
                    {
                        this.WriteObject(RegistrationServices.GetCredentialProvider(type));
                    }
                }
            }
            else if (this.ParameterSetName == "ByClsid")
            {
                this.WriteObject(RegistrationServices.GetCredentialProvider(this.Clsid));
            }
            else if (this.ParameterSetName == "ByProgId")
            {
                var clsid = RegistrationServices.GetClsidFromProgId(this.ProgId);
                this.WriteObject(RegistrationServices.GetCredentialProvider(clsid));
            }
        }
    }
}
