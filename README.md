# Opsætning af Johngeon Quest

Spillet kører som WebGL i browseren på hjemmesiden. Det bruger to repoer, to API'er og to databaser i Docker:

| Del | Mappe | Database (Docker) | URL |
|---|---|---|---|
| Hjemmeside (Vite) | `SW4PRJ4_Goonsite/mit-projekt` | – | http://localhost:5173 |
| Hjemmeside-API | `SW4PRJ4_Goonsite/DungeonCrawler.Api` | `localhost,1433` | http://localhost:5032 |
| Game-API | `prj4-2026-09-19_07-44-18/GameBackend/GameBackend.Api` | `localhost,1434` | https://localhost:7188 |

Guiden går ud fra, at du har pullet den nyeste version af begge repoer: `development` i hjemmeside-repoet og `develop` i spil-repoet.

## 0. Krav (installeres én gang)
- **Docker Desktop** (skal køre)
- **.NET 10 SDK**
- **Node.js** (med npm)
- **dotnet-ef:**
  ```powershell
  dotnet tool install --global dotnet-ef
  ```
- **Mappestruktur:** begge repoer skal ligge i samme mappe, fx `ProjSW4\SW4PRJ4_Goonsite` og `ProjSW4\prj4-2026-09-19_07-44-18`. Ellers kan build-menuen ikke finde hjemmesiden.
- **Unity 6000.4.11f1 med WebGL-modulet** behøver kun den, der bygger spillet. Alle andre spiller i browseren.

## 1. Hjemmeside-repoet (gøres én gang)
Kør fra `SW4PRJ4_Goonsite`:

```powershell
'MSSQL_SA_PASSWORD=YourStrongPassword!' | Set-Content -Encoding utf8 .env
```
```powershell
cd DungeonCrawler.Api
```
```powershell
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=DungeonCrawler;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;"
```
```powershell
dotnet user-secrets set "Jwt:Key" "en-lang-hemmelig-noegle-paa-mindst-32-tegn!"
```
```powershell
cd ..\mit-projekt; npm install; cd ..
```

**`npm install` er vigtigt.** Startup-scriptet installerer kun pakker, hvis `node_modules` slet ikke findes. Har man kørt projektet før, mangler `react-unity-webgl`, og spilsiden fejler. Kør det igen, hver gang `package.json` er ændret.

## 2. Spil-repoet (gøres én gang)
Kør fra `prj4-2026-09-19_07-44-18`:

```powershell
'MSSQL_SA_PASSWORD=YourStrongPassword!' | Set-Content -Encoding utf8 GameBackend\.env
```
```powershell
cd GameBackend\GameBackend.Api
```
```powershell
dotnet user-secrets set "ConnectionStrings:GameDb" "Server=localhost,1434;Database=GameDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;"
```
```powershell
dotnet user-secrets set "Jwt:Key" "en-lang-hemmelig-noegle-paa-mindst-32-tegn!"
```
```powershell
dotnet dev-certs https --trust
```

- **`Jwt:Key` skal være præcis den samme** i begge repoer og mindst 32 tegn lang.
- **Adgangskoden i hver `.env` skal matche** dens connection string.
- **`dev-certs`** gør, at browseren stoler på `https://localhost:7188`. Uden det fejler login i spillet med "Kan ikke nå game-serveren".

## 3. Start det hele (hver gang)
Start Docker Desktop, og kør så de to scripts i denne rækkefølge. Game-API'et skal kunne nå hjemmeside-API'et, så hjemmesiden startes først:

```powershell
powershell -ExecutionPolicy Bypass -File "<sti>\SW4PRJ4_Goonsite\scripts\startup.ps1"
```
```powershell
powershell -ExecutionPolicy Bypass -File "<sti>\prj4-2026-09-19_07-44-18\scripts\startup.ps1"
```

Erstat `<sti>` med mappen, hvor repoerne ligger. Hvert script starter sin database, venter til den er `healthy`, kører migrations og åbner servere i nye PowerShell-vinduer. Luk vinduerne for at stoppe dem.

## 4. Test flowet
1. Åbn http://localhost:5173 og opret en konto.
2. Køb Johngeon Quest på kontoen.
3. Gå til **Bibliotek → Spil**.
4. Log ind i spillet med samme email og password.
5. Tryk **Host** og læs rum-koden op.
6. Den anden spiller trykker **Join** og skriver koden.

**Test multiplayer på én computer med to forskellige browsere, eller én inkognito og ét normalt vindue**, fx Chrome og Edge. To inkognito-vinduer deler login på hjemmesiden.

Vil du teste uden Docker og API'er, så skriv `goontest` som email og lad password være tomt.

## Typiske fejl
| Problem | Løsning |
|---|---|
| Spilsiden: `Failed to resolve import "react-unity-webgl"` | Kør `npm install` i `mit-projekt` og genstart Vite. |
| `Missing .env` | Trin 1 eller 2 mangler. |
| `Login failed for user 'sa'` | Adgangskoden i `.env` og i user secrets matcher ikke. Kør `docker compose down -v` i den mappe, og start igen. |
| "Kan ikke nå game-serveren" | Game-API'et kører ikke, eller `dotnet dev-certs https --trust` mangler. |
| "Forkert email eller password" | Kontoen findes ikke, eller passwordet er forkert. |
| "Kontoen har ikke købt spillet" | Køb Johngeon Quest på hjemmesiden. |
| "Host failed: …" / "Join failed: …" | Teksten efter kolon er fejlen fra Unity Lobby/Relay. Tjek internetforbindelsen. |
| Vite starter på 5175 eller højere | Luk andre Vite-vinduer. Kun 5173 og 5174 er tilladt af API'erne. |

## Byg spillet til hjemmesiden
**Kun én skal bygge spillet.** Når spilkoden ændres, kører den person **Build → WebGL til hjemmesiden** i Unity. Buildet lægges direkte i `SW4PRJ4_Goonsite/mit-projekt/public/unity-build`.

Commit den mappe i hjemmeside-repoet med `git add -A`, så slettede filer også kommer med. Commit spilkoden i dette repo samtidig. Alle andre skal bare pulle.
