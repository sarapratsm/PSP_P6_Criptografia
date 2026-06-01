# Guia pas a pas — Pràctica Criptografia (PSP UF1)

> **Objectiu:** Chat client/servidor amb sockets, fils i xifrat simètric negociat via RSA.

---

## Estat actual del projecte

| Fitxer | Estat |
|---|---|
| `Utils/Cripto/Simetric/Rc4.cs` | ✅ Implementat |
| `Utils/Cripto/Simetric/DES.cs` | ✅ Implementat |
| `Utils/Tcp/PaquetComunicacio.cs` | ✅ Implementat (namespace corregit) |
| `Utils/Cripto/Asimetric/RSA.cs` | ✅ Implementat |
| `ConsoleServer/Program.cs` | ✅ Refactoritzat |
| `ConsoleServer/ClientHandler.cs` | ⚠️ Implementat — pendent corregir `using Utils.Core.Enums` |
| `Client/MainWindow.xaml.cs` | ❌ Pendent |

---

## PAS 1 — Implementar RSA.cs ✅ COMPLETAT

### Teoria
**RSA** és un algorisme **assimètric**: té dues claus relacionades matemàticament.
- **Clau pública**: es pot compartir amb tothom. S'usa per *xifrar*.
- **Clau privada**: secreta. S'usa per *desxifrar*.

Allò xifrat amb la clau pública **només ho pot desxifrar** la clau privada corresponent.

En aquesta pràctica usem RSA per un problema concret: necessitem que el client envii la clau simètrica (DES/RC4) al servidor de forma segura. Si la enviem en clar, qualsevol que escolti la xarxa la veuria. Solució:
1. El servidor genera un parell de claus RSA.
2. Envia la **clau pública** al client.
3. El client xifra la clau simètrica amb la clau pública del servidor.
4. El servidor desxifra amb la seva clau privada. Ningú més pot fer-ho.

### Tasques
- [x] Implementar `GenerarParellClaus()` → retorna `(publicKey, privateKey)`
- [x] Implementar `Xifrar(byte[] dades, string clauPublica)` → retorna `byte[]` xifrat
- [x] Implementar `Desxifrar(byte[] dades, string clauPrivada)` → retorna `byte[]` original

---

## PAS 2 — Corregir Program.cs (ConsoleServer) ✅ COMPLETAT

### Teoria
**Error estàtic vs instància:** Un mètode `static` pertany a la classe, no a cap objecte. No pot accedir a propietats d'instància. Solució: eliminar la classe `Server` i posar-ho tot directament al `Main` estàtic.

**Fils (`Task.Run`):** Cada client que es connecta s'atén en un fil independent. Així el servidor no es queda bloquejat esperant un client mentre n'arriben d'altres.

**Llista compartida de clients:** Per fer broadcast (enviar a tothom), tots els `ClientHandler` han de tenir accés a la mateixa llista. S'ha de fer `static` (pertany a la classe, no a la instància).

### Tasques
- [x] Eliminar la classe `Server` — tot queda al `Main` estàtic
- [x] Demanar el mètode de xifrat per consola **abans** d'obrir el socket
- [x] Generar la clau simètrica aleatòria (`RandomNumberGenerator.GetBytes`)
- [x] Generar claus RSA i passar-les al `ClientHandler`
- [x] Loop d'acceptació amb `Task.Run` per cada client

---

## PAS 3 — Corregir i completar ClientHandler.cs ⚠️ QUASI COMPLETAT

### Teoria
**Protocol de negociació** (tot el que es fa abans del xat):

```
CLIENT                          SERVIDOR
  |--- CONNECT (nick) ----------->|
  |<-- NEGOTIATE (mètode,        |
  |    nClients, clau RSA pública)|
  |--- OK + clau simètrica ------>|  ← clau simètrica xifrada amb RSA
  |=== missatges xifrats =========|
```

El paquet `PaquetComunicacio` ja té tots els camps necessaris per a cada pas.

**Broadcast:** Quan el servidor rep un missatge d'un client, l'ha d'enviar a *tots* els clients connectats (inclòs l'emissor). Per això cal la llista `static` amb `lock`.

**`lock`:** Equivalent a un mutex. Garanteix que només un fil a la vegada pot modificar la llista compartida. Sense el lock, dos fils podrien modificar-la alhora i corrompre les dades.

### Tasques
- [x] Canviar `_algorisme` (enum) per `_xifrador` (objecte `IXifradorSimetric`)
- [x] Llista `static _clientsActius` i `_historial` amb `_lock`
- [x] Implementar el flux de negociació complet (CONNECT → NEGOTIATE → OK + clau)
- [x] Implementar el loop de missatges (desxifrar → historial → broadcast)
- [x] Gestionar `BYE` → tancar connexió i eliminar de la llista
- [x] Enviar les N últimes aportacions als clients nous
- [ ] **Corregir `using Utils.Core.Enums`** → ha de ser `using Utils.Cripto.Simetric`

---

## PAS 4 — Implementar el Client (MainWindow.xaml + .cs) ❌ PENDENT

### Teoria
El client WPF necessita **dos fils**:
- **Fil UI (principal):** Gestiona la interfície (botons, camps de text). A WPF, **només** el fil UI pot modificar controls visuals.
- **Fil de xarxa (background):** Escolta missatges del servidor contínuament. Quan en rep un, ha d'usar `Dispatcher.Invoke` per actualitzar la UI des del fil correcte.

**`Dispatcher.Invoke`:** Mecanisme de WPF per "passar" una acció del fil de xarxa al fil UI. Sense ell, WPF llença una excepció si intentes modificar un control des d'un fil secundari.

### Tasques
- [ ] Dissenyar la UI (`MainWindow.xaml`):
  - Camp per IP i port del servidor
  - Camp per nick
  - Botó "Connectar"
  - Àrea de xat (llista de missatges)
  - Camp per escriure missatge + botó "Enviar"
- [ ] Implementar la connexió al servidor (`TcpClient.Connect`)
- [ ] Implementar el flux de negociació (mirall del servidor):
  - Enviar CONNECT amb nick
  - Llegir NEGOTIATE → extreure mètode, nClients i clau RSA pública
  - Xifrar la clau simètrica rebuda amb RSA i enviar OK
- [ ] Iniciar un `Task` en background per escoltar missatges entrants
- [ ] Mostrar missatges nous usant `Dispatcher.Invoke`
- [ ] Enviar missatge xifrat al servidor quan es prem "Enviar"
- [ ] Enviar `BYE` i tancar en sortir

---

## PAS 5 — Provar el sistema complet ❌ PENDENT

### Tasques
- [ ] Compilar tots els projectes sense errors
- [ ] Iniciar el ConsoleServer, seleccionar DES
- [ ] Connectar dos clients i verificar que el xat funciona
- [ ] Repetir amb RC4
- [ ] Connectar un tercer client i verificar que rep les N últimes aportacions
- [ ] Enviar `BYE` i verificar que es desconnecta correctament

---

## AMPLIACIÓ PRIORITÀRIA — Transferència de fitxers xifrats ❌ PENDENT

### Teoria
Per transferir fitxers, el flux és:
1. El client envia un paquet especial `FILE` amb el nom del fitxer i la mida.
2. S'envien els bytes del fitxer xifrats amb el mateix algorisme simètric actiu.
3. El servidor rep, desxifra i retransmet a tots els clients.

### Tasques
- [ ] Afegir tipus `FILE` a `PaquetComunicacio` (o un camp `Mida`)
- [ ] Implementar enviament de fitxer al client
- [ ] Implementar recepció i rebroadcast al servidor

---

## Resum d'ordre de treball recomanat

```
1. ✅ RSA.cs          → implementat
2. ✅ Program.cs      → refactoritzat
3. ⚠️ ClientHandler   → corregir using Utils.Core.Enums
4. ❌ Client UI       → pendent
5. ❌ Tests           → pendent
6. ❌ Ampliació       → pendent
```
