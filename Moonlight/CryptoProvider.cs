﻿using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Certificates;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;

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
        public Org.BouncyCastle.X509.X509Certificate Certificate { get; private set; }

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

        public static byte[] GeneratePairingHash(bool enhancedSecurity, byte[] data)
        {
            // server major version 7+ uses SHA-256
            // else SHA-1
            IDigest digest = DigestUtilities.GetDigest(enhancedSecurity ? "SHA-256" : "SHA-1");
            digest.BlockUpdate(data, 0, data.Length);
            byte[] hash = new byte[digest.GetDigestSize()];
            digest.DoFinal(hash, 0);
            return hash;
        }

        public static KeyParameter GenerateAesKey(bool enhancedSecurity, byte[] keyData)
        {
            byte[] hash = GeneratePairingHash(enhancedSecurity, keyData);
            byte[] aesTruncated = new byte[16];
            Array.Copy(hash, aesTruncated, 16);
            return ParameterUtilities.CreateKeyParameter("AES", aesTruncated);
        }

        public static int GetDigestLength(bool enhancedSecurity)
        {
            // server major version 7+ uses SHA-256
            // else SHA-1
            IDigest digest = DigestUtilities.GetDigest(enhancedSecurity ? "SHA-256" : "SHA-1");
            return digest.GetDigestSize();
        }

        public static byte[] DecryptData(byte[] encryptedData, KeyParameter key)
        {
            IBufferedCipher cipher = CipherUtilities.GetCipher(ENCRYPTION_ALGO);
            int blockRoundedSize = ((encryptedData.Length + 15) / 16) * 16;
            byte[] blockRoundedEncrypted = CopyOfRange(encryptedData, 0, blockRoundedSize);
            byte[] fullDecrypted = new byte[blockRoundedSize];

            cipher.Init(false, key);
            cipher.DoFinal(encryptedData, 0, blockRoundedSize, fullDecrypted, 0);
            return fullDecrypted;
        }

        public static byte[] EncryptData(byte[] data, KeyParameter key)
        {
            IBufferedCipher cipher = CipherUtilities.GetCipher(ENCRYPTION_ALGO);
            int blockRoundedSize = ((data.Length + 15) / 16) * 16;
            byte[] blockRoundedData = CopyOfRange(data, 0, blockRoundedSize);
            cipher.Init(true, key);
            return cipher.DoFinal(blockRoundedData);
        }

        public static bool VerifySignature(byte[] data, byte[] signature, Org.BouncyCastle.X509.X509Certificate certificate)
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

        private async Task<bool> IsKeyPairInStore()
        {
            CertificateQuery query = new CertificateQuery()
            {
                FriendlyName = Certificate.SubjectDN.ToString()
            };
            return (await CertificateStores.FindAllAsync(query)).Count > 0;
        }

        public async Task<Certificate> GetClientSslCertificate()
        {
            CertificateQuery query = new CertificateQuery()
            {
                FriendlyName = Certificate.SubjectDN.ToString()
            };
            Certificate cert = (await CertificateStores.FindAllAsync(query)).FirstOrDefault();
            bool verify = await VerifyCertificateKeyAccess(cert);
            return cert;
        }

        private async Task<bool> VerifyCertificateKeyAccess(Certificate certificate)
        {
            CryptographicKey cryptographicKey = await PersistedKeyProvider.OpenKeyPairFromCertificateAsync(certificate, HashAlgorithmNames.Sha1, CryptographicPadding.RsaPkcs1V15);
            String buffer = "Test data";
            IBuffer data = CryptographicBuffer.ConvertStringToBinary(buffer, BinaryStringEncoding.Utf16BE);
            try
            {
                IBuffer signed = await CryptographicEngine.SignAsync(cryptographicKey, data);
                return CryptographicEngine.VerifySignature(cryptographicKey, data, signed);
            }
            catch (System.Exception exp)
            {
                System.Diagnostics.Debug.WriteLine("Verification Failed. Excepion Occurred : {0}", exp.Message);
                return false;
            }
        }

        private async Task LoadKeyPairToStore()
        {
            var p12Store = new Pkcs12Store();
            var certEntry = new X509CertificateEntry(Certificate);
            p12Store.SetCertificateEntry(Certificate.SubjectDN.ToString(), certEntry);
            p12Store.SetKeyEntry(Certificate.SubjectDN.ToString(), new AsymmetricKeyEntry(Key), new[] { certEntry });
            using (MemoryStream pfxStream = new MemoryStream())
            {
                p12Store.Save(pfxStream, string.Empty.ToCharArray(), SecureRandom);
                pfxStream.Seek(0, SeekOrigin.Begin);

                await CertificateEnrollmentManager.ImportPfxDataAsync(Convert.ToBase64String(pfxStream.ToArray()), string.Empty, ExportOption.Exportable, KeyProtectionLevel.NoConsent, InstallOptions.DeleteExpired, Certificate.SubjectDN.ToString());
            }
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
                    Certificate = pemReader.ReadObject() as Org.BouncyCastle.X509.X509Certificate;
                }
                StorageFile keyFile = await localFolder.GetFileAsync("client.key");
                string keyPem = await FileIO.ReadTextAsync(keyFile);
                using (TextReader textReader = new StringReader(keyPem))
                {
                    PemReader pemReader = new PemReader(textReader);
                    Key = (pemReader.ReadObject() as AsymmetricCipherKeyPair).Private;
                }
                //if (!(await IsKeyPairInStore()))
                //{
                    await LoadKeyPairToStore();
                //}
            }
            catch(System.Exception)
            {
                GenerateCertificate();
                await Serialize();
            }
        }

        public static Org.BouncyCastle.X509.X509Certificate HexStringToX509Certificate(string hex)
        {
            byte[] data = StringToByteArray(hex);
            string certificatePem = Encoding.UTF8.GetString(data);
            using (TextReader textReader = new StringReader(certificatePem))
            {
                PemReader pemReader = new PemReader(textReader);
                return pemReader.ReadObject() as Org.BouncyCastle.X509.X509Certificate;
            }
        }

        private static void AddCertificateToStore(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
        {
            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] ConcatBytes(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Array.Copy(a, 0, c, 0, a.Length);
            Array.Copy(b, 0, c, a.Length, b.Length);
            return c;
        }

        public static byte[] CopyOfRange(byte[] a, int from, int to)
        {
            byte[] b = new byte[to - from];
            Array.Copy(a, from, b, 0, to - from);
            return b;
        }
    }
}
