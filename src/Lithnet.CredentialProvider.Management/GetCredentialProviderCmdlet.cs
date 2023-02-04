using System;
using System.Management.Automation;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsCommon.Get, "CredentialProvider", DefaultParameterSetName = "None")]
    public class GetCredentialProviderCmdlet : PSCmdlet
    {
        [Parameter(ParameterSetName = "GetByFileName")]
        public string File { get; set; }

        [Parameter(ParameterSetName = "GetByClsid")]
        public Guid Clsid { get; set; }

        [Parameter(ParameterSetName = "GetByProgId")]
        public string ProgId { get; set; }

        protected override void ProcessRecord()
        {
            if (ParameterSetName == "None")
            {
                foreach (var item in CredentialProviderRegistrationServices.GetCredentalProviders())
                {
                    WriteObject(item);
                }
            }
            else if (ParameterSetName == "GetByFileName")
            {
                var assembly = CredentialProviderRegistrationServices.LoadAssembly(File);

                foreach (var type in CredentialProviderRegistrationServices.GetCredentialProviders(assembly))
                {
                    WriteObject(CredentialProviderRegistrationServices.GetCredentialProvider(type));
                }
            }
            else if (ParameterSetName == "GetByClsid")
            {
                WriteObject(CredentialProviderRegistrationServices.GetCredentialProvider(Clsid));
            }
            else if (ParameterSetName == "GetByProgId")
            {
                var clsid = CredentialProviderRegistrationServices.GetClsidFromProgId(ProgId);
                WriteObject(CredentialProviderRegistrationServices.GetCredentialProvider(clsid));
            }
        }
    }
}
