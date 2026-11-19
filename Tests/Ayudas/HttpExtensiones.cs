using System.Net;
using System.Text.RegularExpressions;

namespace API.Tests.Ayudas;

public static class HttpExtensiones
{
    /// <summary>
    /// Lee el HTML de la respuesta y decodifica las entidades (&amp;#xED; → í).
    /// Razor codifica los textos que vienen del modelo (mensajes de validación, nombres),
    /// así que sin esto "Ingresá tu nombre." aparecería como "Ingres&amp;#xE1; tu nombre.".
    /// </summary>
    public static async Task<string> LeerHtmlAsync(this HttpResponseMessage respuesta)
        => WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

    /// <summary>Ruta (con query) a la que apunta el header Location de una redirección.</summary>
    public static string RutaDeRedireccion(this HttpResponseMessage respuesta)
    {
        var location = respuesta.Headers.Location
            ?? throw new InvalidOperationException($"La respuesta ({(int)respuesta.StatusCode}) no trae header Location.");
        return new Uri(new Uri("http://localhost"), location).PathAndQuery;
    }

    /// <summary>Los headers Set-Cookie de la respuesta (vacío si no hay).</summary>
    public static IReadOnlyList<string> Cookies(this HttpResponseMessage respuesta)
        => respuesta.Headers.TryGetValues("Set-Cookie", out var valores) ? valores.ToList() : new List<string>();

    /// <summary>
    /// Simula "abrir el formulario y enviarlo" como un navegador:
    /// 1) GET de la página (trae el token antiforgery + su cookie), 2) POST con los campos y el token.
    /// </summary>
    public static async Task<HttpResponseMessage> EnviarFormularioAsync(
        this HttpClient http, string paginaConElFormulario, string accion, params (string Campo, string Valor)[] campos)
    {
        var pagina = await http.GetAsync(paginaConElFormulario);
        if (pagina.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"No se pudo abrir {paginaConElFormulario}: respondió {(int)pagina.StatusCode}.");

        var token = ExtraerToken(await pagina.Content.ReadAsStringAsync());

        var datos = campos.Select(c => new KeyValuePair<string, string>(c.Campo, c.Valor)).ToList();
        datos.Add(new("__RequestVerificationToken", token));

        return await http.PostAsync(accion, new FormUrlEncodedContent(datos));
    }

    /// <summary>Saca el valor del input oculto __RequestVerificationToken que agrega el form tag helper.</summary>
    public static string ExtraerToken(string html)
    {
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>");
        if (!input.Success)
            throw new InvalidOperationException("La página no trae el token antiforgery (¿no hay un <form method=post>?).");

        var valor = Regex.Match(input.Value, "value=\"([^\"]+)\"");
        if (!valor.Success)
            throw new InvalidOperationException("El input del token antiforgery no tiene value.");

        return valor.Groups[1].Value;
    }
}
