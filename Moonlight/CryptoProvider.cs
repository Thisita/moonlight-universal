﻿using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Moonlight
{
    public class CryptoProvider
    {
        private const string SIGNING_ALGO = "SHA-256withRSA";
        private const string ENCRYPTION_ALGO = "AES/ECB/NoPadding";
        private const string KEY_FILE_NAME = "client.key";
        private const string CERTIFICATE_FILE_NAME = "client.crt";
        private static SecureRandom SecureRandom = new SecureRandom();
        private AsymmetricKeyParameter Key { get; set; }
        private X509Certificate Certificate { get; set; }

        public static byte[] GenerateRandomBytes(int length)
        {
            byte[] output = new byte[length];
            SecureRandom.NextBytes(output, 0, length);
            return output;
        }

        public static byte[] SaltPin(byte[] salt, string pin)
        {
            byte[] saltedPin = new byte[salt.Length + pin.Length];
            Array.Copy(salt, 0, saltedPin, 0, salt.Length);
            Array.Copy(Encoding.UTF8.GetBytes(pin), 0, saltedPin, salt.Length, pin.Length);
            return saltedPin;
        }

        public static string GeneratePin()
        {
            Random random = new Random();
            int pin = random.Next(0, 9999);
            return pin.ToString().PadLeft(4, '0');
        }

        public static KeyParameter GenerateAesKey(bool enhancedSecurity, byte[] keyData)
        {
            // server major version 7+ uses SHA-256
            // else SHA-1
            IDigest digest = DigestUtilities.GetDigest(enhancedSecurity?"SHA-256":"SHA-1");
            digest.BlockUpdate(keyData, 0, keyData.Length);
            byte[] hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);
            byte[] aesTruncated = new byte[16];
            Array.Copy(hash, aesTruncated, 16);
            return new KeyParameter(aesTruncated);
        }

        public static byte[] DecryptData(byte[] encryptedData, KeyParameter key)
        {
            IBufferedCipher cipher = CipherUtilities.GetCipher(ENCRYPTION_ALGO);
            cipher.Init(false, key);
            return cipher.DoFinal(encryptedData);
        }

        public static byte[] EncryptData(byte[] data, KeyParameter key)
        {
            IBufferedCipher cipher = CipherUtilities.GetCipher(ENCRYPTION_ALGO);
            cipher.Init(true, key);
            return cipher.DoFinal(data);
        }

        public static bool VerifySignature(byte[] data, byte[] signature, X509Certificate certificate)
        {
            ISigner signer = SignerUtilities.GetSigner(SIGNING_ALGO);
            signer.Init(false, certificate.GetPublicKey());
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(signature);
        }

        public byte[] SignData(byte[] data)
        {
            ISigner signer = SignerUtilities.GetSigner(SIGNING_ALGO);
            signer.Init(true, Key);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }

        public async Task Initialize()
        {
            await Deserialize();
        }

        private void GenerateCertificate()
        {
            // Generate key pair
            RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(new KeyGenerationParameters(SecureRandom, 2048));
            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            Key = keyPair.Private;

            // Generate certificate
            DateTime startDate = DateTime.Now;
            DateTime expiryDate = startDate.AddYears(20);
            BigInteger serialNumber = BigInteger.ProbablePrime(120, new Random());
            X509Name commonName = new X509Name("CN=NVIDIA GameStream Client");
            X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();
            certificateGenerator.SetSerialNumber(BigInteger.ValueOf(DateTime.Now.Millisecond));
            certificateGenerator.SetSubjectDN(commonName);
            certificateGenerator.SetIssuerDN(commonName);
            certificateGenerator.SetNotAfter(expiryDate);
            certificateGenerator.SetNotBefore(startDate);
            certificateGenerator.SetPublicKey(keyPair.Public);

            ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA1WithRSA", Key, SecureRandom);

            Certificate = certificateGenerator.Generate(signatureFactory);
        }

        public string GetCertificatePem()
        {
            using (TextWriter textWriter = new StringWriter())
            {
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(Certificate);
                pemWriter.Writer.Flush();
                return textWriter.ToString().Replace("\r", "");
            }
        }

        public string GetKeyPem()
        {
            using (TextWriter textWriter = new StringWriter())
            {
                PemWriter pemWriter = new PemWriter(textWriter);
                pemWriter.WriteObject(Key);
                pemWriter.Writer.Flush();
                return textWriter.ToString().Replace("\r", "");
            }
        }

        private async Task Serialize()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile certificateFile = await localFolder.CreateFileAsync(CERTIFICATE_FILE_NAME, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(certificateFile, GetCertificatePem());
            StorageFile keyFile = await localFolder.CreateFileAsync(KEY_FILE_NAME, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(keyFile, GetKeyPem());
        }

        private async Task Deserialize()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile certificateFile = await localFolder.GetFileAsync("client.crt");
                string certificatePem = await FileIO.ReadTextAsync(certificateFile);
                using (TextReader textReader = new StringReader(certificatePem))
                {
                    PemReader pemReader = new PemReader(textReader);
                    Certificate = pemReader.ReadObject() as X509Certificate;
                }
                StorageFile keyFile = await localFolder.GetFileAsync("client.key");
                string keyPem = await FileIO.ReadTextAsync(keyFile);
                using (TextReader textReader = new StringReader(keyPem))
                {
                    PemReader pemReader = new PemReader(textReader);
                    Key = pemReader.ReadObject() as AsymmetricKeyParameter;
                }
            }
            catch(Exception)
            {
                GenerateCertificate();
                await Serialize();
            }
        }
    }
}
