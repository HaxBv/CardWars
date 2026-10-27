using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace CardWars.CloudCode
{
    // Registra IGameApiClient (para hablar con Cloud Save desde el módulo).
    // IPushClient no necesita registro: Cloud Code lo inyecta en las funciones.
    public class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
        }
    }
}
