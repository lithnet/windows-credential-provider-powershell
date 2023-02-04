using System;
using System.Reflection;

namespace Lithnet.CredentialProvider.RegistrationTool
{
    public class AssemblyFromMetadataLoadContext : IDisposable
    {
        private bool disposedValue;
        private readonly MetadataLoadContext context;

        public Assembly Assembly { get; }

        internal AssemblyFromMetadataLoadContext(Assembly assembly, MetadataLoadContext context)
        {
            this.Assembly = assembly;
            this.context = context;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.context.Dispose();
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
