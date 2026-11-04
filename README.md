## Kør spillet lokalt

Spillet bruger to API'er, og hver af dem har sin egen SQL Server i Docker:

| Del | Mappe | Database (Docker) | URL |
|---|---|---|---|
| Hjemmeside-API | `SW4PRJ4_Goonsite/DungeonCrawler.Api` | `localhost,1433` | http://localhost:5032 |
| Game-API | `prj4-2026-09-19_07-44-18/GameBackend/GameBackend.Api` | `localhost,1434` | https://localhost:7188 |

### 1. Verificer at du har følgende:
- Docker Desktop (skal køre)
- .NET 10 SDK
- `dotnet tool install --global dotnet-ef`

### 2. Sæt secrets (én gang)
`Jwt:Key` skal være **den samme** i begge projekter og mindst 32 tegn lang.

```powershell
cd SW4PRJ4_Goonsite\DungeonCrawler.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=DungeonCrawler;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "en-lang-hemmelig-noegle-paa-mindst-32-tegn!"

cd ..\..\prj4-2026-09-19_07-44-18\GameBackend\GameBackend.Api
dotnet user-secrets set "ConnectionStrings:GameDb" "Server=localhost,1434;Database=GameDb;User Id=sa;Password=YourStrongPassword!;TrustServerCertificate=True;"
dotnet user-secrets set "Jwt:Key" "en-lang-hemmelig-noegle-paa-mindst-32-tegn!"
```

### 3. Start hjemmeside-API'et (terminal 1)
```powershell
cd SW4PRJ4_Goonsite
docker compose up -d
cd DungeonCrawler.Api
dotnet ef database update
dotnet run
```

### 4. Start game-API'et (terminal 2)
```powershell
cd prj4-2026-09-19_07-44-18\GameBackend
docker compose up -d
cd GameBackend.Api
dotnet ef database update
dotnet run --launch-profile https
```

Vent til begge containere står som `healthy` (`docker compose ps`), før du kører `dotnet ef database update`.

### 5. Start spillet
Åbn projektet i Unity og log ind med en konto, der har købt GoonQuest på hjemmesiden.

Vil du teste uden Docker og API'er, så skriv `goontest` som email og lad password være tomt.

### Fejlfinding
| Besked i spillet | Løsning |
|---|---|
| "Kan ikke nå game-serveren" | Game-API'et kører ikke, eller hjemmeside-API'et er nede. |
| "Forkert email eller password" | Kontoen findes ikke, eller passwordet er forkert. |
| "Kontoen har ikke købt spillet" | Køb GoonQuest på hjemmesiden. |