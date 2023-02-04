using System;
using System.Management.Automation;
using Lithnet.CredentialProvider.Management;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsLifecycle.Unregister, "CredentialProvider", DefaultParameterSetName = "ByFileName")]
    public class UnregisterCredentialProviderCmdlet : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByFileName", Position = 1, HelpMessage = "The path to a .NET credential provider DLL")]
        public string File { get; set; }

        [Parameter(ParameterSetName = "ByClsid", HelpMessage = "The CLSID of the credential provider")]
        public Guid Clsid { get; set; }

        [Parameter(ParameterSetName = "ByProgId", HelpMessage = "The ProgId of the credential provider")]
        public string ProgId { get; set; }

        [Parameter(HelpMessage = "Specifies if the COM registration for the credential provider should be retained")]
        public SwitchParameter RetainComRegistration { get; set; }

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
                    throw new Exception("This tool cannot unregister managed assemblies by file name. You can unregister native assemblies using the CLSID or ProgID");
                }

                using (var assembly = RegistrationServices.LoadAssembly(this.File))
                {
                    foreach (var type in RegistrationServices.GetCredentialProviders(assembly.Assembly))
                    {
                        RegistrationServices.UnregisterCredentialProvider(type, !this.GetSwitchValue(this.RetainComRegistration, nameof(this.RetainComRegistration)));
                        this.WriteVerbose($"Unregistered credential provider {type.FullName}");
                    }
                }
            }
            else if (this.ParameterSetName == "ByClsid")
            {
                RegistrationServices.UnregisterCredentialProvider(this.Clsid, !this.GetSwitchValue(this.RetainComRegistration, nameof(this.RetainComRegistration)));
            }
            else if (this.ParameterSetName == "ByProgId")
            {
                var clsid = RegistrationServices.GetClsidFromProgId(this.ProgId);
                RegistrationServices.UnregisterCredentialProvider(clsid, !this.GetSwitchValue(this.RetainComRegistration, nameof(this.RetainComRegistration)));
            }
        }

        protected bool GetSwitchValue(SwitchParameter parameter, string name)
        {
            if (this.MyInvocation.BoundParameters.ContainsKey(name))
            {
                return parameter.ToBool();
            }

            return false;
        }
    }
}
