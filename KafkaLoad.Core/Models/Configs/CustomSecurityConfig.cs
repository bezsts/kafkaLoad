using KafkaLoad.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaLoad.Core.Models.Configs
{
    public class CustomSecurityConfig
    {
        public SecurityProtocolEnum SecurityProtocol { get; set; } = SecurityProtocolEnum.Plaintext;
        
        // SASL (Authentication)
        public SaslMechanismEnum SaslMechanism { get; set; } = SaslMechanismEnum.Plain;
        public string? SaslUsername { get; set; }
        public string? SaslPassword { get; set; }

        // SSL (Encryption / mTLS)
        public string? SslCaLocation { get; set; }
        public string? SslCertificateLocation { get; set; }
        public string? SslKeyLocation { get; set; }
        public string? SslKeyPassword { get; set; }
    }
}
