using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace InSolve.dmach.Win32
{
    /// <summary>
    /// Managed memory buffer
    /// </summary>
    public class MemoryBuffer :
        IDisposable
    {
        internal IntPtr Address;
        internal int Size;
        internal int BytesRead;
        internal byte[] Array;
        internal StringBuilder Text;
        /// <summary>
        /// Always is 4kb
        /// </summary>
        internal byte[] HelperArray = new byte[1024 * 4];

        /// <summary>
        /// Constructor with specified buffer size
        /// </summary>
        /// <param name="size">Size in bytes</param>
        public MemoryBuffer(int size)
        {
            this.Address = Marshal.AllocCoTaskMem(size);
            this.Array = new byte[size];
            this.Size = size;
            this.Text = new StringBuilder(size * 2);
        }

        /// <summary>
        /// Constructor with default buffer size - 4 kb
        /// </summary>
        public MemoryBuffer()
            : this(1024 * 4)
        {
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~MemoryBuffer()
        {
            Dispose();
        }

        /// <summary>
        /// Buffer size in bytes
        /// </summary>
        public int AllocatedSize
        {
            get
            {
                return Size;
            }
        }

        /// <summary>
        /// Inner managed buffer, read this if errors
        /// </summary>
        public StringBuilder InnerText
        {
            get
            {
                return Text;
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose object
        /// </summary>
        public void Dispose()
        {
            if (Address != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(Address);
                GC.SuppressFinalize(this);
                Address = IntPtr.Zero;
            }
        }

        #endregion
    }
}
