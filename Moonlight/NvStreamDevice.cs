using Org.BouncyCastle.Crypto.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
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
        public bool EnhancedSecurity { get { return ServerInfo.AppVersion.CompareTo("7.0.0.0") >= 1; } }

        public NvStreamDevice(IPAddress ipAddress, CryptoProvider cryptoProvider)
        {
            IPAddress = ipAddress;
            CryptoProvider = cryptoProvider;
            NvHttp = new NvHttp(new Uri($"http://{IPAddress}:{HTTP_PORT}/"));
            Online = false;
        }

        public async Task QueryDataInsecure()
        {
            ServerInfo = await NvHttp.ServerInfo();
            Online = true;
            InitializeSecureClient();
        }

        private void InitializeSecureClient()
        {
            SecureNvHttp = new NvHttp(new Uri($"https://{IPAddress}:{ServerInfo.HttpsPort}/"), NvHttp.Uuid);
        }

        public async Task QueryDataSecure()
        {
            SecureServerInfo = await SecureNvHttp.ServerInfo();
        }

        public async Task Pair()
        {
            // Generate salt for hashing the pin
            byte[] salt = CryptoProvider.GenerateRandomBytes(16);

            // Combine sal and pin and generate aes key from them
            string pin = CryptoProvider.GeneratePin();
            byte[] saltedPin = CryptoProvider.SaltPin(salt, pin);
            KeyParameter aesKey = CryptoProvider.GenerateAesKey(EnhancedSecurity, saltedPin);

            // Send the salt and get server cert. This doesn't have read timeout
            // because the user must enter the PIN before the server responds
            NvPair getServerCertResponse = await NvHttp.GetServerCert(salt, CryptoProvider.GetCertificatePem());
        }

        public static async Task<List<NvStreamDevice>> DiscoverStreamDevices(CryptoProvider cryptoProvider)
        {
            List<NvStreamDevice> streamDevices = new List<NvStreamDevice>();
            IReadOnlyList<IZeroconfHost> results = await
                ZeroconfResolver.ResolveAsync(ZEROCONF_PROTOCOL);
            foreach (var result in results)
            {
                NvStreamDevice streamDevice = new NvStreamDevice(IPAddress.Parse(result.IPAddress), cryptoProvider);
                streamDevice.Online = true;
                await streamDevice.QueryDataInsecure();
                streamDevices.Add(streamDevice);
            }
            return streamDevices;
        }
    }
}