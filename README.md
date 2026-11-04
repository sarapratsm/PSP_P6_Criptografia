# 🔐 Chat Cifrado P2P (Servidor-Cliente)

Sistema de chat multiplaza con cifrado híbrido (RSA + DES/RC4) desarrollado en .NET 10 y WPF. Cada cliente genera su propia clave simétrica y la intercambia de forma segura mediante cifrado asimétrico RSA durante el handshake inicial.

---

## 📋 Tabla de contenidos

- [Arquitectura](#arquitectura)
- [Protocolo de seguridad](#protocolo-de-seguridad)
- [Requisitos](#requisitos)
- [Instalación y ejecución](#instalación-y-ejecución)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Tipos de paquetes](#tipos-de-paquetes)
- [Flujo de mensajes y archivos](#flujo-de-mensajes-y-archivos)
- [Limitaciones y notas](#limitaciones-y-notas)
- [Tecnologías](#tecnologías)

---

## 🏗️ Arquitectura

La solución se divide en tres proyectos .NET:

| Proyecto | Tipo | Descripción |
|----------|------|-------------|
| **Utils** | Biblioteca de clases | Utilidades criptográficas, modelo de paquetes y constantes compartidas |
| **ConsoleServer** | Aplicación de consola | Servidor TCP multihilo que gestiona clientes, historial y reenvío de mensajes |
| **Client** | Aplicación WPF | Escritorio con interfaz gráfica para chatear, enviar archivos y ver miembros conectados |

### Diagramas de comunicación

```
SERVIDOR                                    CLIENTE
   │                                           │
   │  Genera: clauPublicaRSA + clauPrivadaRSA  │
   │  (una sola vez al arrancar)               │
   │                                           │
   │←────────── CONNECT (nick) ────────────────│  sin cifrar
   │                                           │
   │──── NEGOTIATE ────────────────────────────→│  sin cifrar
   │     · algoritmo (DES o RC4)               │
   │     · nClientes conectados                │
   │     · clauPublicaRSA          (en claro ✅)│
   │                                           │
   │     [cliente genera claveSimétrica]       │
   │     [DES → 8 bytes / RC4 → 16 bytes]      │
   │     [cifra con clauPublicaRSA 🔒]          │
   │                                           │
   │←────────── OK ────────────────────────────│  clave cifrada
   │     · claveSimétrica cifrada con RSA 🔒    │
   │                                           │
   │  [servidor descifra con clauPrivadaRSA]   │
   │  [guarda la claveSimétrica del cliente]   │
   │                                           │
   │══════ CHAT CIFRADO CON DES/RC4 ═══════════│
```

---

## 🔒 Protocolo de seguridad

### Diseño híbrido

1. **RSA (asimétrico)**: Se usa **únicamente** durante el handshake inicial para transportar de forma segura la clave simétrica de cada cliente.
   - Clave RSA de 2048 bits.
   - Padding OAEP-SHA256.
   - Formato XML para intercambio.

2. **DES / RC4 (simétricos)**: Se usan para cifrar **todos** los mensajes y archivos del chat después de la negociación.
   - Cada cliente tiene su propia clave simétrica única.
   - El servidor descifra con la clave del remitente y vuelve a cifrar con la clave de cada destinatario (broadcast re-cifrado).

### Propiedades clave

- La clave simétrica **nunca viaja en claro** por la red.
- Cada cliente genera su propia clave simétrica.
- El servidor actúa como intermediario de confianza que re-cifra para cada destinatario.

---

## ⚙️ Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/) o runtime correspondiente
- Sistema operativo compatible con WPF (Windows para el cliente)
- Puerto `2323` libre en el servidor

---

## 🚀 Instalación y ejecución

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd <nombre-del-repositorio>
```

### 2. Ejecutar el servidor

```bash
cd ConsoleServer
dotnet run
```

Al iniciar, el servidor le pedirá elegir el algoritmo simétrico (`DES` o `RC4`). Luego generará su par de claves RSA y comenzará a escuchar en el puerto `2323`.

### 3. Ejecutar el cliente

```bash
cd Client
dotnet run
```

1. Escriba un apodo (nickname) y haga clic en **Connect**.
2. El cliente se conectará, negociará el algoritmo y generará su clave simétrica.
3. Una vez conectado, podrá enviar mensajes, emojis y archivos (hasta 10 MB).

---

## 📁 Estructura del proyecto

```
.
├── Utils/
│   ├── Cripto/
│   │   ├── Asimetric/
│   │   │   └── RSA_Utils.cs          # Generación y operaciones RSA
│   │   └── Simetric/
│   │       ├── IXifradorSimetric.cs  # Interfaz de cifrado simétrico
│   │       ├── DES.cs                # Implementación DES
│   │       ├── Rc4.cs                # Implementación RC4
│   │       └── AlgorismeSimetric.cs  # Enumeración DES / RC4
│   └── Tcp/
│       ├── PaquetComunicacio.cs      # Modelo de paquete JSON
│       └── ServerInfo.cs             # IP y puerto del servidor
├── ConsoleServer/
│   ├── Program.cs                    # Punto de entrada del servidor
│   └── ClientHandler.cs              # Lógica por cliente (negociación, broadcast, historial)
├── Client/
│   ├── Views/
│   │   ├── Login.xaml / Login.xaml.cs       # Pantalla de conexión y handshake
│   │   └── MainWindow.xaml / MainWindow.xaml.cs # Interfaz de chat
│   ├── Dades/
│   │   └── ConnexioClient.cs            # Estado de conexión y clave simétrica
│   ├── App.xaml / App.xaml.cs           # Inicio de la aplicación WPF
│   └── Styles/
│       └── styles.xaml                  # Estilos de la interfaz
├── .gitignore
└── README.md
```

---

## 📦 Tipos de paquetes

Toda la comunicación se realiza mediante JSON serializado con un prefijo de 4 bytes que indica la longitud del payload.

| Campo | Descripción |
|-------|-------------|
| `Tipus` | Tipo de paquete: `CONNECT`, `NEGOTIATE`, `OK`, `MESSAGE`, `FILE`, `BYE`, `MEMBERS`, `KO` |
| `Nickname` | Apodo del usuario |
| `Contingut` | Contenido del mensaje (Base64 para datos binarios) |
| `XifratTriat` | Algoritmo simétrico negociado (`DES` o `RC4`) |
| `DadesClauCriptografica` | Clave simétrica cifrada con RSA (Base64) |
| `NomFitxer` | Nombre del archivo (solo en paquetes `FILE`) |

---

## 💬 Flujo de mensajes y archivos

### Mensajes de texto

```
CLIENTE A                   SERVIDOR                  CLIENTE B
   │                          │                          │
   │── MESSAGE (cifrado A) ───→│                          │
   │                    descifra (clave A)                │
   │                    re-cifra (clave B)                │
   │                          │── MESSAGE (cifrado B) ───→│
```

### Archivos

```
CLIENTE A                   SERVIDOR                  CLIENTE B
   │                          │                          │
   │── FILE (cifrado A) ─────→│                          │
   │                    descifra (clave A)                │
   │                    re-cifra (clave B)                │
   │                          │── FILE (cifrado B) ─────→│
   │                          │    NomFitxer incluido    │
```

El servidor mantiene un historial de los últimos 20 mensajes que se envía a cada nuevo cliente al unirse.

---

## ⚠️ Limitaciones y notas

- **DES** se considera un algoritmo obsoleto y débil según estándares modernos de seguridad; se incluye con fines educativos y por requisito del enunciado.
- **RC4** también tiene vulnerabilidades conocidas y no debería usarse en producción real.
- El tamaño máximo de archivo es **10 MB**.
- El servidor no persiste claves ni mensajes entre reinicios (el historial se pierde al reiniciar).
- No hay autenticación de usuarios más allá del apodo (nickname).
- El cifrado usa `CBC` para DES con `IV = clave` (no es la práctica más segura, pero funcional para el alcance del proyecto).

---

## 🛠️ Tecnologías

- **.NET 10**
- **C#**
- **WPF** (Windows Presentation Foundation) para la interfaz de escritorio
- **System.Security.Cryptography** (RSA, DES, RC4 implementado manualmente)
- **System.Text.Json** para serialización de paquetes
- **System.Net.Sockets** para comunicaciones TCP

---

## 👥 Autores

Desarrollado como proyecto académico de comunicaciones seguras y criptografía aplicada.

---

*Si encuentra algún problema o tiene sugerencias, abra un issue en el repositorio.*