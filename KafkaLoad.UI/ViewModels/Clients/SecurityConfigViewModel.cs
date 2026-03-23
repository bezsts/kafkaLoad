using KafkaLoad.Core.Enums;
using KafkaLoad.Core.Models.Configs;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace KafkaLoad.UI.ViewModels.Clients
{
    public class SecurityConfigViewModel : ReactiveObject
    {
        private readonly CustomSecurityConfig _model;

        public IEnumerable<SecurityProtocolEnum> SecurityProtocolOptions =>
            (IEnumerable<SecurityProtocolEnum>)Enum.GetValues(typeof(SecurityProtocolEnum));

        public IEnumerable<SaslMechanismEnum> SaslMechanismOptions =>
            (IEnumerable<SaslMechanismEnum>)Enum.GetValues(typeof(SaslMechanismEnum));

        public SecurityProtocolEnum SecurityProtocol
        {
            get => _model.SecurityProtocol;
            set
            {
                if (_model.SecurityProtocol != value)
                {
                    _model.SecurityProtocol = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public SaslMechanismEnum SaslMechanism
        {
            get => _model.SaslMechanism;
            set
            {
                if (_model.SaslMechanism != value)
                {
                    _model.SaslMechanism = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        public string? SaslUsername
        {
            get => _model.SaslUsername;
            set { if (_model.SaslUsername != value) { _model.SaslUsername = value; this.RaisePropertyChanged(); } }
        }

        public string? SaslPassword
        {
            get => _model.SaslPassword;
            set { if (_model.SaslPassword != value) { _model.SaslPassword = value; this.RaisePropertyChanged(); } }
        }

        public string? SslCaLocation
        {
            get => _model.SslCaLocation;
            set { if (_model.SslCaLocation != value) { _model.SslCaLocation = value; this.RaisePropertyChanged(); } }
        }

        public string? SslCertificateLocation
        {
            get => _model.SslCertificateLocation;
            set { if (_model.SslCertificateLocation != value) { _model.SslCertificateLocation = value; this.RaisePropertyChanged(); } }
        }

        public string? SslKeyLocation
        {
            get => _model.SslKeyLocation;
            set { if (_model.SslKeyLocation != value) { _model.SslKeyLocation = value; this.RaisePropertyChanged(); } }
        }

        public string? SslKeyPassword
        {
            get => _model.SslKeyPassword;
            set { if (_model.SslKeyPassword != value) { _model.SslKeyPassword = value; this.RaisePropertyChanged(); } }
        }

        readonly ObservableAsPropertyHelper<bool> _isSaslVisible;
        public bool IsSaslVisible => _isSaslVisible.Value;

        readonly ObservableAsPropertyHelper<bool> _isSslCaVisible;
        public bool IsSslCaVisible => _isSslCaVisible.Value;

        readonly ObservableAsPropertyHelper<bool> _isClientCertVisible;
        public bool IsClientCertVisible => _isClientCertVisible.Value;

        public SecurityConfigViewModel(CustomSecurityConfig model)
        {
            _model = model;

            // Logic: Show SASL if protocol is SASL_PLAINTEXT or SASL_SSL
            _isSaslVisible = this.WhenAnyValue(x => x.SecurityProtocol)
                .Select(p => p == SecurityProtocolEnum.SaslPlaintext || p == SecurityProtocolEnum.SaslSsl)
                .ToProperty(this, x => x.IsSaslVisible);

            // Logic: Show CA location if protocol uses SSL
            _isSslCaVisible = this.WhenAnyValue(x => x.SecurityProtocol)
                .Select(p => p == SecurityProtocolEnum.Ssl || p == SecurityProtocolEnum.SaslSsl)
                .ToProperty(this, x => x.IsSslCaVisible);

            // Logic: Show Client Cert locations only if needed for mTLS (simple check if CA is visible for now)
            _isClientCertVisible = this.WhenAnyValue(x => x.IsSslCaVisible)
                .ToProperty(this, x => x.IsClientCertVisible);
        }
    }
}
