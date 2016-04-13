using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CodeExecutor
{
    /// <summary>
    /// Meta data of the assembly, 
    /// </summary>
    [Serializable]
    public class AssemblyMetaData : Dictionary<string, string>
    {
        public AssemblyMetaData()
        {
        }

        public AssemblyMetaData(IDictionary<string, string> dictionary)
            : base(dictionary)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Collections.Generic.Dictionary`2"/> class with serialized data.
        /// </summary>
        /// <param name="info">A <see cref="T:System.Runtime.Serialization.SerializationInfo"/> object containing the information required to serialize the <see cref="T:System.Collections.Generic.Dictionary`2"/>.</param><param name="context">A <see cref="T:System.Runtime.Serialization.StreamingContext"/> structure containing the source and destination of the serialized stream associated with the <see cref="T:System.Collections.Generic.Dictionary`2"/>.</param>
        protected AssemblyMetaData(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Set Fullname of the assembly and determine strong name.
        /// </summary>
        /// <remarks>Helper</remarks>
        public void SetFullName(string value)
        {
            this[FullNameLabel] = value;

            try
            {
                var assemblyName = new AssemblyName(value);
                var publicKey = assemblyName.GetPublicKeyToken();
                var isStrongNamed = publicKey != null && publicKey.Length > 0;

                if (isStrongNamed)
                {
                    this[StrongNamedLabel] = string.Format("Yes, version {0}", assemblyName.Version.ToString());
                }
                else
                {
                    this[StrongNamedLabel] = "No";
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private const string FullNameLabel = "Full Name";
        private const string StrongNamedLabel = "Strong Name";
    }
}
