using System.Management.Automation;
using Lithnet.CredentialProvider.Management;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsLifecycle.Register, "CredentialProvider")]
    public class RegisterCredentialProviderCmdlet : PSCmdlet
    {
        [Parameter(ParameterSetName = "ByFileName", Position = 1, HelpMessage = "The path to a .NET credential provider DLL")]
        public string File { get; set; }

        protected override void BeginProcessing()
        {
            NativeMethods.ThrowIfNotAdmin();
        }

        protected override void ProcessRecord()
        {
            if (!RegistrationServices.IsManagedAssembly(this.File))
            {
                throw new System.Exception("This tool can only register managed assemblies");
            }

            using (var assembly = RegistrationServices.LoadAssembly(this.File))
            {
                foreach (var type in RegistrationServices.GetCredentialProviders(assembly.Assembly))
                {
                    RegistrationServices.RegisterCredentialProvider(type);
                    this.WriteVerbose($"Registered credential provider {type.FullName}");
                }
            }
        }
    }
}
