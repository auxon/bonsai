using System;
using System.IO;
using System.Net;

namespace Abacus.Observables.Blockchain
{
    public static class Configuration
    {
        public const int MAGIC_NUMBER = 0x07770777;
        public static string AppDataPath => Environment.GetEnvironmentVariable("APPDATA") ?? Environment.CurrentDirectory;
        public static string DataDirPath => Environment.GetEnvironmentVariable("ABACUS_DATADIR") ?? Path.Combine(AppDataPath, "abacus", "datadir");
        public static string DataFilePath => Path.Combine(Configuration.DataDirPath, "db.abc");
        public static IPEndPoint EndPoint => new IPEndPoint(IPAddress.Loopback, port: 9394);
    }
}
