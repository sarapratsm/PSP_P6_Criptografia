# Flux d'intercanvi de claus

## Protocol de connexió

```
SERVIDOR                                    CLIENT
   │                                           │
   │  Genera: clauPublicaRSA + clauPrivadaRSA  │
   │  (una sola vegada en arrencar)            │
   │                                           │
   │←────────── CONNECT (nick) ────────────────│  sense xifrar
   │                                           │
   │──── NEGOTIATE ────────────────────────────→│  sense xifrar
   │     · algorisme (DES o RC4)               │
   │     · nClients connectats                 │
   │     · clauPublicaRSA          (en clar ✅) │
   │                                           │
   │     [client genera clauSimetrica pròpia]  │
   │     [DES → 8 bytes / RC4 → 16 bytes]      │
   │     [xifra amb clauPublicaRSA 🔒]          │
   │                                           │
   │←────────── OK ────────────────────────────│  clau xifrada
   │     · clauSimetrica xifrada amb RSA 🔒     │
   │                                           │
   │  [servidor desxifra amb clauPrivadaRSA]   │
   │  [guarda la clauSimetrica del client]     │
   │                                           │
   │══════ XAT XIFRAT AMB DES/RC4 ════════════│
```

## Propietats del disseny

- La clau simètrica **mai** viatja en clar per la xarxa.
- **Cada client té la seva pròpia clau simètrica** → el servidor desxifra i re-xifra per cada destinatari en el broadcast.
- **RSA** s'usa únicament durant la connexió inicial per transportar la clau simètrica de forma segura.
- **DES / RC4** xifra tots els missatges i fitxers del xat.

## Responsabilitats per actor

| | Servidor | Client |
|---|---|---|
| Genera claus RSA | ✅ Una vegada en arrencar | ❌ |
| Genera clau simètrica | ❌ | ✅ En cada connexió |
| Envia clau pública RSA | ✅ Al NEGOTIATE | ❌ |
| Xifra clau simètrica amb RSA | ❌ | ✅ Abans d'enviar OK |
| Desxifra clau simètrica amb RSA | ✅ En rebre OK | ❌ |
| Xifra missatges/fitxers | ✅ (re-xifra per broadcast) | ✅ Amb la seva clau |

## Flux de missatges i fitxers (post-negociació)

```
CLIENT A                   SERVIDOR                  CLIENT B
   │                          │                          │
   │── MESSAGE (xifrat A) ───→│                          │
   │                    desxifra (clau A)                │
   │                    re-xifra (clau B)                │
   │                          │── MESSAGE (xifrat B) ───→│
   │                          │                          │
   │── FILE (xifrat A) ──────→│                          │
   │                    desxifra (clau A)                │
   │                    re-xifra (clau B)                │
   │                          │── FILE (xifrat B) ──────→│
   │                          │    NomFitxer inclòs      │
```

## Per què cada client té clau diferent?

L'enunciat exigeix que la clau **mai viatgi en clar**. Si el servidor generés una clau única per a tots, l'hauria d'enviar al client d'alguna manera segura — però per fer-ho necessitaria la clau pública RSA del client (que no té). La solució és que **el client generi la seva pròpia clau** i l'enviï xifrada al servidor.
