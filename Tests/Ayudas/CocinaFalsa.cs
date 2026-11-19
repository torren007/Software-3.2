using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace API.Tests.Ayudas;

/// <summary>
/// Reemplaza a CocinaApp durante los tests. PedidoService abre un socket TCP a 127.0.0.1:5050
/// y manda "NUEVO_PEDIDO:{id}"; esta clase escucha ahí y guarda lo que llega.
/// No se toca el código de la app: el servicio habla con este listener como si fuera la cocina real.
/// </summary>
public sealed class CocinaFalsa : IDisposable
{
    public const int Puerto = 5050;

    private const string PuertoOcupado =
        "El puerto 5050 está ocupado (¿tenés CocinaApp abierta?). Cerrala antes de correr los tests: " +
        "si no, los pedidos de prueba le llegarían a la cocina real.";

    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _bucle;

    public ConcurrentQueue<string> Mensajes { get; } = new();

    public CocinaFalsa()
    {
        _listener = new TcpListener(IPAddress.Loopback, Puerto);
        try { _listener.Start(); }
        catch (SocketException ex) { throw new InvalidOperationException(PuertoOcupado, ex); }

        _bucle = Task.Run(AtenderAsync);
    }

    /// <summary>
    /// Para simular la cocina CAÍDA: verifica que nadie escuche en el 5050.
    /// Si CocinaApp está abierta, el test falla con un mensaje claro (y sin mandarle nada).
    /// </summary>
    public static void ExigirCocinaCaida()
    {
        try
        {
            var prueba = new TcpListener(IPAddress.Loopback, Puerto);
            prueba.Start();
            prueba.Stop();
        }
        catch (SocketException ex)
        {
            throw new InvalidOperationException(PuertoOcupado, ex);
        }
    }

    private async Task AtenderAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var cliente = await _listener.AcceptTcpClientAsync(_cts.Token);
                using var stream = cliente.GetStream();
                var buffer = new byte[256];
                var leidos = await stream.ReadAsync(buffer, _cts.Token);
                if (leidos > 0) Mensajes.Enqueue(Encoding.UTF8.GetString(buffer, 0, leidos));
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (SocketException) { if (_cts.IsCancellationRequested) break; }
            catch (IOException) { /* conexión cortada: se ignora y se sigue escuchando */ }
        }
    }

    /// <summary>Espera (hasta 3 s) el próximo mensaje recibido; null si no llegó nada.</summary>
    public async Task<string?> EsperarMensajeAsync(TimeSpan? maximo = null)
    {
        var limite = DateTime.UtcNow + (maximo ?? TimeSpan.FromSeconds(3));
        while (DateTime.UtcNow < limite)
        {
            if (Mensajes.TryDequeue(out var mensaje)) return mensaje;
            await Task.Delay(20);
        }
        return null;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
        try { _bucle.Wait(TimeSpan.FromSeconds(2)); } catch { /* ya se cerró */ }
        _cts.Dispose();
    }
}
