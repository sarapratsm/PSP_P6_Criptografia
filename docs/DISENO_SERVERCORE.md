# 📐 Propuesta de diseño: Clase estática ServerCore

## Contexto

Actualmente, el proyecto `ConsoleServer` mantiene el estado global del servidor dentro de la clase `ClientHandler` mediante miembros `static`:

- `List<ClientHandler> _clientsActius` — lista de clientes conectados
- `List<string> _historial` — historial de mensajes
- `object _lock` — sincronización de acceso concurrente
- `void Log(string)` — logging de eventos del servidor
- Lógica de broadcast y reenvío de paquetes

Esto funciona para un proyecto pequeño, pero viola el **Principio de Responsabilidad Única**: `ClientHandler` debería gestionar únicamente la conexión de *un* cliente, no el estado global del servidor.

## Propuesta

Extraer toda la lógica y estado compartido a una clase estática (o singleton) llamada **`ServerCore`**, ubicada en `ConsoleServer/Core/ServerCore.cs`.

### Responsabilidades de `ServerCore`

| Responsabilidad | Descripción |
|-----------------|-------------|
| Registro de clientes | Añadir/eliminar `ClientHandler` de la lista activa |
| Broadcast | Enviar un paquete a todos los clientes conectados (excepto el origen, si es necesario) |
| Historial | Almacenar y recuperar los últimos N mensajes |
| Logging | Centralizar trazas del servidor en consola/archivo |
| Estadísticas | Consultar número de clientes conectados, algoritmo activo, etc. |

### Esbozo de implementación

```csharp
namespace ConsoleServer.Core;

public static class ServerCore
{
    private static readonly List<ClientHandler> _clients = new();
    private static readonly List<string> _historial = new();
    private static readonly int MaxHistorial = 20;
    private static readonly object _lock = new();

    public static int NumClients => _clients.Count;

    public static void RegistrarClient(ClientHandler client)
    {
        lock (_lock) _clients.Add(client);
        Log($"Client registrat: {client.Nickname} ({_clients.Count} totals)");
    }

    public static void EliminarClient(ClientHandler client)
    {
        lock (_lock) _clients.Remove(client);
        Log($"Client desconnectat: {client.Nickname} ({_clients.Count} totals)");
    }

    public static async Task BroadcastAsync(string missatge, ClientHandler? excluir = null)
    {
        List<ClientHandler> destinataris;
        lock (_lock) destinataris = _clients.Where(c => c != excluir).ToList();

        foreach (var client in destinataris)
        {
            await client.EnviarMissatgeAsync(missatge);
        }
    }

    public static void AfegirHistorial(string missatge)
    {
        lock (_lock)
        {
            _historial.Add(missatge);
            if (_historial.Count > MaxHistorial)
                _historial.RemoveAt(0);
        }
    }

    public static List<string> ObtenirHistorial()
    {
        lock (_lock) return _historial.ToList();
    }

    public static void Log(string missatge)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {missatge}");
    }
}
```

### Cambios en `ClientHandler`

`ClientHandler` dejaría de tener miembros `static` para estado global y delegaría en `ServerCore`:

```csharp
public class ClientHandler
{
    public string Nickname { get; set; }
    // ... otros miembros de instancia

    public async Task Iniciar()
    {
        ServerCore.RegistrarClient(this);
        // ... loop de recepción
    }

    private async Task ProcessarMissatge(string missatge)
    {
        ServerCore.AfegirHistorial(missatge);
        await ServerCore.BroadcastAsync(missatge, this);
    }
}
```

## Ventajas

1. **Separación clara de responsabilidades**: `ClientHandler` gestiona un cliente; `ServerCore` gestiona el servidor.
2. **Código más legible y mantenible**: eliminar miembros `static` mezclados con lógica de instancia en `ClientHandler`.
3. **Facilita pruebas unitarias**: se puede probar `ServerCore` sin levantar sockets TCP reales.
4. **Escalabilidad**: si el servidor crece (múltiples salas, persistencia, moderadores), `ServerCore` es el punto natural para añadir esa lógica.

## Consideraciones

- Al ser estática, `ServerCore` introduce estado global compartido. Para entornos de prueba, se podría convertir en un **singleton con interfaz** (`IServerCore`) y usar inyección de dependencias.
- El acceso concurrente debe seguir protegiéndose con `lock` o `ConcurrentBag`/`ConcurrentQueue`.

---

*Propuesta elaborada como mejora de arquitectura para el proyecto Chat Cifrado P2P (.NET 10).*