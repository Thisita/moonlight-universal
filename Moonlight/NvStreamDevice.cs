using Moonlight.Exception;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Windows.UI.Xaml.Controls;
using Zeroconf;

namespace Moonlight
{
    public class NvStreamDevice
    {
        private const string ZEROCONF_PROTOCOL = "_nvstream._tcp.local.";
        private const int HTTP_PORT = 47989;
        public NvHttp NvHttp { get; private set; }
        public NvHttp SecureNvHttp { get; private set; }
        public CryptoProvider CryptoProvider { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public NvServerInfo ServerInfo { get; private set; }
        public NvServerInfo SecureServerInfo { get; private set; }
        public bool Online { get; private set; }
        public bool Offline { get { return !Online; } }
        public NvServerInfo.NvPairStatus Paired { get; private set; }
        public bool EnhancedSecurity { get { return ServerInfo.AppVersion.CompareTo("7.0.0.0") >= 1; } }

        public NvStreamDevice(IPAddress ipAddress, CryptoProvider cryptoProvider)
        {
            IPAddress = ipAddress;
            CryptoProvider = cryptoProvider;
            NvHttp = new NvHttp(new Uri($"http://{IPAddress}:{HTTP_PORT}/"));
            Online = false;
            Paired = NvServerInfo.NvPairStatus.Unpaired;
        }

        public async Task QueryDataInsecure()
        {
            ServerInfo = await NvHttp.ServerInfo();
            Online = true;
            InitializeSecureClient();
            try
            {
                await QueryDataSecure();
            }
            catch (System.Exception)
            {
                Paired = ServerInfo.PairStatus;
            }
        }

        private void InitializeSecureClient()
        {
            SecureNvHttp = new NvHttp(new Uri($"https://{IPAddress}:{ServerInfo.HttpsPort}/"));
        }

        public async Task QueryDataSecure()
        {
            SecureServerInfo = await SecureNvHttp.ServerInfo(ServerInfo.UniqueId);
            Paired = SecureServerInfo.PairStatus;
        }

        public async Task Pair()
        {
            // Generate salt for hashing the pin
            byte[] salt = CryptoProvider.GenerateRandomBytes(16);

            // Combine sal and pin and generate aes key from them
            string pin = CryptoProvider.GeneratePin();
            byte[] saltedPin = CryptoProvider.SaltPin(salt, pin);
            KeyParameter aesKey = CryptoProvider.GenerateAesKey(EnhancedSecurity, saltedPin);

            // Create dialog to display pin to user
            ContentDialog dialog = new ContentDialog()
            {
                Title = "Pairing",
                Content = $"Please enter the following PIN on the target PC: {pin}",
                CloseButtonText = "Ok"
            };
            // Show the pin
            await dialog.ShowAsync();
            // Send the salt and get server cert. This doesn't have read timeout
            // because the user must enter the PIN before the server responds
            NvPair getServerCertResponse = await NvHttp.GetServerCert(ServerInfo.UniqueId, CryptoProvider.ByteArrayToString(salt), CryptoProvider.GetCertificatePem());

            // Check the pairing state
            if(getServerCertResponse.Paired != 1)
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingException();
            }

            // Attempting to pair while another device is pairing will cause GFE
            // to give an empty certificate in the response
            if (String.IsNullOrEmpty(getServerCertResponse.PlainCertificate))
            {
                throw new PairingInProgressException();
            }

            // Parse the server certificate
            X509Certificate serverCertificate = CryptoProvider.HexStringToX509Certificate(getServerCertResponse.PlainCertificate);

            // Generate a random challenge and encrypte it
            byte[] randomChallenge = CryptoProvider.GenerateRandomBytes(16);
            byte[] encryptedChallenge = CryptoProvider.EncryptData(randomChallenge, aesKey);

            // Send the encrypted challenge to the server
            NvPair getChallengeResponse = await NvHttp.GetChallengeResponse(ServerInfo.UniqueId, CryptoProvider.ByteArrayToString(encryptedChallenge));
            
            // Check the pairing state
            if(getChallengeResponse.Paired != 1)
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingServerChallengeException();
            }

            // Decrypte the server's response and subsequent challenge
            byte[] encryptedServerChallengeResponse = CryptoProvider.StringToByteArray(getChallengeResponse.ChallengeResponse);
            byte[] decryptedServerChallengeResponse = CryptoProvider.DecryptData(encryptedServerChallengeResponse, aesKey);

            int hashLength = CryptoProvider.GetDigestLength(EnhancedSecurity);
            byte[] serverResponse = new byte[hashLength];
            byte[] serverChallenge = new byte[hashLength + 16];
            Array.Copy(decryptedServerChallengeResponse, 0, serverResponse, 0, hashLength);
            Array.Copy(decryptedServerChallengeResponse, hashLength, serverChallenge, 0, hashLength + 16);

            // Using another 16 byte secret, compute a challenge response hash using the secret, our cert sig, and the challenge
            byte[] clientSecret = CryptoProvider.GenerateRandomBytes(16);
            byte[] clientCertificateSignature = CryptoProvider.Certificate.GetSignature();
            byte[] concatenated = new byte[serverChallenge.Length + clientCertificateSignature.Length + clientSecret.Length];
            Array.Copy(serverChallenge, 0, concatenated, 0, serverChallenge.Length);
            Array.Copy(clientCertificateSignature, 0, concatenated, serverChallenge.Length, clientCertificateSignature.Length);
            Array.Copy(clientSecret, 0, concatenated, serverChallenge.Length + clientCertificateSignature.Length, clientSecret.Length);
            byte[] challengeResponseHash = CryptoProvider.GeneratePairingHash(EnhancedSecurity, concatenated);
            byte[] challengeResponseEncrypted = CryptoProvider.EncryptData(challengeResponseHash, aesKey);

            NvPair getSecretResponse = await NvHttp.GetServerChallengeResponse(ServerInfo.UniqueId, CryptoProvider.ByteArrayToString(challengeResponseEncrypted));

            // Check that there isn't a state error
            if(getSecretResponse.Paired != 1)
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingException();
            }

            // Get the server's signed secret
            byte[] serverSecretResponse = CryptoProvider.StringToByteArray(getSecretResponse.PairingSecret);
            byte[] serverSecret = new byte[16];
            byte[] serverSignature = new byte[272];
            Array.Copy(serverSecretResponse, 0, serverSecret, 0, 16);
            Array.Copy(serverSecretResponse, 16, serverSignature, 0, 272);

            // Ensure authenticity
            if(!CryptoProvider.VerifySignature(serverSecret, serverSignature, serverCertificate))
            {
                // Failed singature test so don't trust the server and cancel pairing
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingUntrustedServerResponseException();
            }

            // Ensure the server challenge matched what we expected
            byte[] serverCertificateSignature = serverCertificate.GetSignature();
            byte[] serverChallengeHashData = new byte[randomChallenge.Length + serverCertificateSignature.Length + serverSecret.Length];
            Array.Copy(randomChallenge, 0, serverChallengeHashData, 0, randomChallenge.Length);
            Array.Copy(serverCertificateSignature, 0, serverChallengeHashData, randomChallenge.Length, serverCertificateSignature.Length);
            Array.Copy(serverCertificateSignature, 0, serverChallengeHashData, randomChallenge.Length + serverCertificateSignature.Length, serverSecret.Length);
            byte[] serverChallengeResponseHash = CryptoProvider.GeneratePairingHash(EnhancedSecurity, serverChallengeHashData);
            if(!serverChallengeResponseHash.SequenceEqual(serverResponse))
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                // User probably inputed the wrong pin
                throw new PairingPinException();
            }

            // Send the server our signed secret
            byte[] clientSecretSignature = CryptoProvider.SignData(clientSecret);
            byte[] clientPairingSecret = new byte[clientSecret.Length + clientSecretSignature.Length];
            Array.Copy(clientSecret, 0, clientPairingSecret, 0, clientSecret.Length);
            Array.Copy(clientSecretSignature, 0, clientPairingSecret, clientSecret.Length, clientSecretSignature.Length);

            NvPair getClientSecretResponse = await NvHttp.GetClientPairingSecret(ServerInfo.UniqueId, CryptoProvider.ByteArrayToString(clientPairingSecret));
            if(getClientSecretResponse.Paired != 1)
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingException();
            }

            // Do the intiial challenge on secure channel
            NvPair getPairChallenge = await SecureNvHttp.GetPairChallenge(ServerInfo.UniqueId);
            if(getPairChallenge.Paired != 1)
            {
                await NvHttp.Unpair(ServerInfo.UniqueId);
                throw new PairingException();
            }

            // Refresh secure data now that we are paired
            await QueryDataSecure();
        }

        public static async Task<List<NvStreamDevice>> DiscoverStreamDevices(CryptoProvider cryptoProvider)
        {
            List<NvStreamDevice> streamDevices = new List<NvStreamDevice>();
            IReadOnlyList<IZeroconfHost> results = await
                ZeroconfResolver.ResolveAsync(ZEROCONF_PROTOCOL);
            foreach (var result in results)
            {
                NvStreamDevice streamDevice = new NvStreamDevice(IPAddress.Parse(result.IPAddress), cryptoProvider)
                {
                    Online = true
                };
                await streamDevice.QueryDataInsecure();
                streamDevices.Add(streamDevice);
            }
            return streamDevices;
        }
    }
}