namespace CardWars.CloudCode
{
    // Versión del contrato de las funciones (parámetros y DTOs). Se sube aquí y en el cliente
    // Unity A LA VEZ cada vez que cambia el contrato de una función. El cliente la envía en
    // cada llamada y el servidor rechaza las que no coinciden (protocol_mismatch).
    public static class GameProtocol
    {
        public const int Version = 1;
    }
}
