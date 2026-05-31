# MyWebApp

ASP.NET Core Web API with SQLite and PMO features (projects, tasks, email/calendar AI analysis).

## Configuration IA (Capgemini Generative Engine)

L'application utilise une API compatible OpenAI via [Capgemini Generative Engine](https://generative.engine.capgemini.com/). Le modèle par défaut est `openai.gpt-5-mini`.

**Studio (paramètres API)** : [Settings du studio](https://generative.engine.capgemini.com/studios/0e18b6c4-3a2d-40fd-81c4-714737bdb1a8/settings)

Copiez depuis le studio l'URL de base exacte (`BaseUrl`) si elle diffère de `https://generative.engine.capgemini.com/v1`.

### Ne jamais committer de clé API

- `appsettings.json` ne contient pas de `ApiKey`.
- Utilisez **User Secrets** (PC perso) ou **variables d'environnement** (PC pro Capgemini).
- Copiez `appsettings.Local.json.example` vers `appsettings.Local.json` (fichier gitignored) pour un override local optionnel.

### PC personnel (développement)

```powershell
cd MyWebApp
dotnet user-secrets set "GenerativeEngine:ApiKey" "VOTRE_CLE"
dotnet user-secrets set "GenerativeEngine:BaseUrl" "https://generative.engine.capgemini.com/v1"
dotnet user-secrets set "GenerativeEngine:Model" "openai.gpt-5-mini"
```

Ou créez `appsettings.Local.json` à partir de `appsettings.Local.json.example`.

En **Development**, `GenerativeEngine:UseDevFallback` est `true` : si l'API est indisponible, les endpoints `/api/ai/extract-tasks` et l'analyse PMO utilisent des réponses de démonstration.

### PC professionnel Capgemini

```powershell
$env:GenerativeEngine__ApiKey = "VOTRE_CLE"
$env:GenerativeEngine__BaseUrl = "URL_DU_STUDIO"
$env:GenerativeEngine__Model = "openai.gpt-5-mini"
dotnet run --project MyWebApp
```

Variables supportées : `GenerativeEngine__ApiKey`, `GenerativeEngine__BaseUrl`, `GenerativeEngine__Model`, `GenerativeEngine__UseDevFallback`.

### Test rapide

```powershell
dotnet build
dotnet run --project MyWebApp
# POST /api/chat avec { "message": "Bonjour" }
# POST /api/ai/extract-tasks avec { "text": "..." }
```

Si une clé a été exposée (chat, commit, etc.), régénérez-la dans le studio Generative Engine.
