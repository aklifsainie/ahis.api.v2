# API Client Authentication Module Overview

The module authenticates external systems with generated API keys and administers their clients, keys, and permissions. Administration and runtime authentication use separate flows:

```text
Admin controller -> IApiClientService -> ApplicationDbContext

X-API-Key header -> ApiKeyAuthenticationHandler
  -> IApiKeyValidator -> ApplicationDbContext
  -> claims principal with permission claims
```

API-client data resides in the application database with Country and audit data.
