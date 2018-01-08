using Moonlight.Exception;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
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

        public NvStreamDevice(IPAddress ipAddress, CryptoProvider cryptoProvider, Certificate clientCertificate)
        {
            IPAddress = ipAddress;
            CryptoProvider = cryptoProvider;
            NvHttp = new NvHttp(new Uri($"http://{IPAddress}:{HTTP_PORT}/"), clientCertificate);
            Online = false;
            Paired = NvServerInfo.NvPairStatus.Unpaired;
        }

        public async Task QueryDataInsecure()
        {
            ServerInfo = await NvHttp.ServerInfo();
            Paired = ServerInfo.PairStatus;
            Online = true;
            await InitializeSecureClient();
            await QueryDataSecure();
        }

        private async Task InitializeSecureClient()
        {
            SecureNvHttp = new NvHttp(new Uri($"https://{IPAddress}:{ServerInfo.HttpsPort}/"), await CryptoProvider.GetClientSslCertificate());
        }

        public async Task QueryDataSecure()
        {
            SecureServerInfo = await SecureNvHttp.ServerInfo();
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
            NvPair getServerCertResponse = await NvHttp.GetServerCert(CryptoProvider.ByteArrayToString(salt), CryptoProvider.ByteArrayToString(Encoding.UTF8.GetBytes(CryptoProvider.GetCertificatePem())));

            // Check the pairing state
            if(getServerCertResponse.Paired != 1)
            {
                await NvHttp.Unpair();
                throw new PairingException($"Server certificate response paired value is {getServerCertResponse.Paired} instead of 1");
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
            NvPair getChallengeResponse = await NvHttp.GetChallengeResponse(CryptoProvider.ByteArrayToString(encryptedChallenge));
            
            // Check the pairing state
            if(getChallengeResponse.Paired != 1)
            {
                await NvHttp.Unpair();
                throw new PairingServerChallengeException($"Challenge response paired value is {getChallengeResponse.Paired} instead of 1");
            }

            // Decrypte the server's response and subsequent challenge
            byte[] encryptedServerChallengeResponse = CryptoProvider.StringToByteArray(getChallengeResponse.ChallengeResponse);
            byte[] decryptedServerChallengeResponse = CryptoProvider.DecryptData(encryptedServerChallengeResponse, aesKey);

            int hashLength = CryptoProvider.GetDigestLength(EnhancedSecurity);
            byte[] serverResponse = CryptoProvider.CopyOfRange(decryptedServerChallengeResponse, 0, hashLength);
            byte[] serverChallenge = CryptoProvider.CopyOfRange(decryptedServerChallengeResponse, hashLength, hashLength + 16);

            // Using another 16 byte secret, compute a challenge response hash using the secret, our cert sig, and the challenge
            byte[] clientSecret = CryptoProvider.GenerateRandomBytes(16);
            byte[] challengeResponseHash = CryptoProvider.GeneratePairingHash(EnhancedSecurity, CryptoProvider.ConcatBytes(CryptoProvider.ConcatBytes(serverChallenge, CryptoProvider.Certificate.GetSignature()), clientSecret));
            byte[] challengeResponseEncrypted = CryptoProvider.EncryptData(challengeResponseHash, aesKey);

            NvPair getSecretResponse = await NvHttp.GetServerChallengeResponse(CryptoProvider.ByteArrayToString(challengeResponseEncrypted));

            // Check that there isn't a state error
            if(getSecretResponse.Paired != 1)
            {
                await NvHttp.Unpair();
                throw new PairingException($"Secret response paired value is {getSecretResponse.Paired} instead of 1");
            }

            // Get the server's signed secret
            byte[] serverSecretResponse = CryptoProvider.StringToByteArray(getSecretResponse.PairingSecret);
            byte[] serverSecret = CryptoProvider.CopyOfRange(serverSecretResponse, 0, 16);
            byte[] serverSignature = CryptoProvider.CopyOfRange(serverSecretResponse, 16, 272);

            // Ensure authenticity
            if(!CryptoProvider.VerifySignature(serverSecret, serverSignature, serverCertificate))
            {
                // Failed singature test so don't trust the server and cancel pairing
                await NvHttp.Unpair();
                throw new PairingUntrustedServerResponseException("Server failed signature test");
            }

            // Ensure the server challenge matched what we expected
            byte[] serverChallengeResponseHash = CryptoProvider.GeneratePairingHash(EnhancedSecurity, CryptoProvider.ConcatBytes(CryptoProvider.ConcatBytes(randomChallenge, serverCertificate.GetSignature()), serverSecret));
            if(!serverChallengeResponseHash.SequenceEqual(serverResponse))
            {
                await NvHttp.Unpair();
                // User probably inputed the wrong pin
                throw new PairingPinException($"Server challenge response hash {serverResponse} doesn't match expected value {challengeResponseHash}");
            }

            // Send the server our signed secret
            byte[] clientPairingSecret = CryptoProvider.ConcatBytes(clientSecret, CryptoProvider.SignData(clientSecret));
            NvPair getClientSecretResponse = await NvHttp.GetClientPairingSecret(CryptoProvider.ByteArrayToString(clientPairingSecret));
            if(getClientSecretResponse.Paired != 1)
            {
                await NvHttp.Unpair();
                throw new PairingException($"Client secret response paired value is {getClientSecretResponse.Paired} instead of 1");
            }

            // Do the intiial challenge on secure channel
            NvPair getPairChallenge = await SecureNvHttp.GetPairChallenge();
            if(getPairChallenge.Paired != 1)
            {
                await NvHttp.Unpair();
                throw new PairingException($"Pair challenge response paired value is {getPairChallenge.Paired} instead of 1");
            }
        }

        public async Task<List<NvApplication>> GetApplications()
        {
            return await SecureNvHttp.ApplicationList(SecureServerInfo.UniqueId);
        }

        public async Task<NvLaunch> LaunchApplication(NvApplication application)
        {
            return await SecureNvHttp.Launch(application.ID, string.Empty, 0, 1, string.Empty, 0, 0, 0);
        }

        public static async Task<NvStreamDevice> InitializeStreamDeviceAsync(IPAddress ip, CryptoProvider cryptoProvider)
        {
            NvStreamDevice streamDevice = new NvStreamDevice(ip, cryptoProvider, await cryptoProvider.GetClientSslCertificate())
            {
                Online = true
            };
            await streamDevice.QueryDataInsecure();
            return streamDevice;
        }

        public static async Task<IEnumerable<NvStreamDevice>> DiscoverStreamDevices(CryptoProvider cryptoProvider)
        {
            List<Task<NvStreamDevice>> streamDeviceInitTasks = new List<Task<NvStreamDevice>>();
            foreach (var result in await ZeroconfResolver.ResolveAsync(ZEROCONF_PROTOCOL))
            {
                streamDeviceInitTasks.Add(InitializeStreamDeviceAsync(IPAddress.Parse(result.IPAddress), cryptoProvider));
            }
            return await Task.WhenAll(streamDeviceInitTasks);
        }
    }
}