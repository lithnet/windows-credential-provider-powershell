using System.Management.Automation;
using Lithnet.CredentialProvider.Management;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    [Cmdlet(VerbsLifecycle.Invoke, "CredUI")]
    public class InvokeCredUICmdlet : PSCmdlet
    {
        [Parameter]
        public string Heading { get; set; } = "Login with CredUI";

        [Parameter]
        public string Message { get; set; } = "Select your favorite credential provider";

        protected override void EndProcessing()
        {
            CredUI.Prompt(Heading, Message);
        }
    }
}
