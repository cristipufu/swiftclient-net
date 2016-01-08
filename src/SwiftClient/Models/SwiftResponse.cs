using System;
using System.IO;

namespace SwiftClient
{
    public class SwiftResponse : SwiftBaseResponse, IDisposable
    {
        bool disposed = false;

        /// <summary>
        /// Reference to network stream
        /// </summary>
        public Stream Stream { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (Stream != null) Stream.Dispose();
            }

            disposed = true;
        }
    }
}
