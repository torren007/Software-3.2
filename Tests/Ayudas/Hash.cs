using System.Security.Cryptography;
using System.Text;

namespace API.Tests.Ayudas;

/// <summary>
/// Mismo formato que usa AutenticacionService y el SHA2(..., 256) de MySQL del pizzeria.sql:
/// SHA-256 en hexadecimal y minúsculas. Se repite acá a propósito: si alguien cambia el
/// algoritmo en el backend, los tests de login avisan que los usuarios existentes dejarían de entrar.
/// </summary>
public static class Hash
{
    public static string Sha256Hex(string texto)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto))).ToLowerInvariant();
}
