// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.AspNetCore.Cryptography;

namespace Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel
{
    /// <summary>
    /// Represents a configured authenticated encryption mechanism which uses
    /// Windows CNG algorithms in GCM encryption + authentication modes.
    /// </summary>
    public sealed class CngGcmAuthenticatedEncryptorConfiguration : AlgorithmConfiguration
    {
        /// <summary>
        /// The name of the algorithm to use for symmetric encryption.
        /// This property corresponds to the 'pszAlgId' parameter of BCryptOpenAlgorithmProvider.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The algorithm must support CBC-style encryption and must have a block size exactly
        /// 128 bits.
        /// The default value is 'AES'.
        /// </remarks>
        [ApplyPolicy]
        public string EncryptionAlgorithm { get; set; } = Constants.BCRYPT_AES_ALGORITHM;

        /// <summary>
        /// The name of the provider which contains the implementation of the symmetric encryption algorithm.
        /// This property corresponds to the 'pszImplementation' parameter of BCryptOpenAlgorithmProvider.
        /// This property is optional.
        /// </summary>
        /// <remarks>
        /// The default value is null.
        /// </remarks>
        [ApplyPolicy]
        public string EncryptionAlgorithmProvider { get; set; } = null;

        /// <summary>
        /// The length (in bits) of the key that will be used for symmetric encryption.
        /// This property is required to have a value.
        /// </summary>
        /// <remarks>
        /// The key length must be 128 bits or greater.
        /// The default value is 256.
        /// </remarks>
        [ApplyPolicy]
        public int EncryptionAlgorithmKeySize { get; set; } = 256;

        internal override ISecret MasterKey { get; set; }

        public override XmlSerializedDescriptorInfo ExportToXml()
        {
            return ExportToXml(Secret.Random(KDK_SIZE_IN_BYTES));
        }

        internal override XmlSerializedDescriptorInfo ExportToXml(ISecret masterKey)
        {
            MasterKey = masterKey;

            // <descriptor>
            //   <!-- Windows CNG-GCM -->
            //   <encryption algorithm="..." keyLength="..." [provider="..."] />
            //   <masterKey>...</masterKey>
            // </descriptor>

            var encryptionElement = new XElement("encryption",
                new XAttribute("algorithm", EncryptionAlgorithm),
                new XAttribute("keyLength", EncryptionAlgorithmKeySize));
            if (EncryptionAlgorithmProvider != null)
            {
                encryptionElement.SetAttributeValue("provider", EncryptionAlgorithmProvider);
            }

            var rootElement = new XElement("descriptor",
                new XComment(" Algorithms provided by Windows CNG, using Galois/Counter Mode encryption and validation "),
                encryptionElement,
                masterKey.ToMasterKeyElement());

            return new XmlSerializedDescriptorInfo(rootElement, typeof(CngGcmAuthenticatedEncryptorDescriptorDeserializer));
        }

        /// <summary>
        /// Validates that this <see cref="CngGcmAuthenticatedEncryptorConfiguration"/> is well-formed, i.e.,
        /// that the specified algorithm actually exists and can be instantiated properly.
        /// An exception will be thrown if validation fails.
        /// </summary>
        internal override void Validate()
        {
            var factory = new CngGcmAuthenticatedEncryptorFactory(this, DataProtectionProviderFactory.GetDefaultLoggerFactory());
            // Run a sample payload through an encrypt -> decrypt operation to make sure data round-trips properly.
            using (var encryptor = factory.CreateAuthenticatedEncryptorInstance(Secret.Random(512 / 8)))
            {
                encryptor.PerformSelfTest();
            }
        }
    }
}
