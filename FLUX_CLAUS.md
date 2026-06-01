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
   │          [client genera clauSimetrica]     │
   │          [DES → 8 bytes / RC4 → 16 bytes] │
   │          [xifra amb clauPublicaRSA 🔒]     │
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
- Cada client té la **seva pròpia clau simètrica** → el servidor xifra cada missatge amb la clau del destinatari.
- **RSA** s'usa únicament durant la connexió inicial per transportar la clau simètrica de forma segura.
- **DES / RC4** xifra tots els missatges del xat un cop establerta la connexió.

## Responsabilitats per actor

| | Servidor | Client |
|---|---|---|
| Genera claus RSA | ✅ Una vegada en arrencar | ❌ |
| Genera clau simètrica | ❌ | ✅ En cada connexió |
| Envia clau pública RSA | ✅ Al NEGOTIATE | ❌ |
| Xifra clau simètrica amb RSA | ❌ | ✅ Abans d'enviar OK |
| Desxifra clau simètrica amb RSA | ✅ En rebre OK | ❌ |
| Xifra missatges (xat) | ❌ | ✅ Amb la seva clau |
| Desxifra i re-xifra per broadcast | ✅ Per a cada destinatari | ❌ |

## Per què no compartim una clau simètrica única?

Compartir una sola clau per a tots els clients simplificaria el broadcast, però implicaria enviar-la en clar del servidor al client (el servidor l'hauria de generar i enviar). L'enunciat exigeix que **la clau viatgi sempre xifrada**, cosa que obliga que el client la generi i l'enviï ell mateix.
