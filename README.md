# Jarvis 웹 애플리케이션 사용법

## 1. Jarvis.WebApi 실행 방법 (백엔드)

1. **필수 조건**

   - .NET 9.0 SDK 이상 설치
   - Node.js 및 npx 설치
   - (옵션) Obsidian, Google Maps 등 외부 서비스 연동 시 관련 계정 및 API 키 필요

2. **환경 변수 설정**

   - 루트 또는 `Jarvis.WebApi` 폴더에 `.env` 파일을 생성하고 아래 항목을 추가하세요:
     ```env
     AZURE_OPENAI_TOKEN=your-azure-openai-api-key
     AZURE_OPENAI_ENDPOINT=your-azure-openai-endpoint
     ```

3. **mcp.json 파일 준비**

   - `Jarvis.WebApi/mcp.json` 파일이 반드시 존재해야 합니다.
   - 예시:
     ```json
     {
       "mcp": {
         "servers": {
           "obsidian": {
             "command": "npx",
             "args": ["-y", "mcp-obsidian", "/Users/사용자명/obsidian"]
           },
           "filesystem": {
             "command": "npx",
             "args": [
               "-y",
               "@modelcontextprotocol/server-filesystem",
               "/Users/사용자명/Desktop"
             ]
           },
           "puppeteer": {
             "command": "npx",
             "args": ["-y", "@modelcontextprotocol/server-puppeteer"]
           },
           "googlemaps": {
             "command": "npx",
             "args": ["-y", "@modelcontextprotocol/server-google-maps"],
             "env": {
               "GOOGLE_MAPS_API_KEY": "{YOUR_GOOGLE_MAPS_API_KEY}"
             }
           }
         }
       }
     }
     ```

4. **실행**
   ```bash
   cd Jarvis/Jarvis.WebApi
   dotnet run
   ```
   - 서버가 8080 포트에서 실행됩니다.
   - http://localhost:8080/health 로 접속해 정상 동작을 확인할 수 있습니다.

## 2. Jarvis.Client 실행 방법 (프론트엔드)

1. **필수 조건**

   - .NET 9.0 SDK 이상 설치

2. **실행**
   ```bash
   cd Jarvis/Jarvis.Client
   dotnet run
   # 또는 실시간 반영 개발 서버:
   dotnet watch run
   ```
   - 기본적으로 http://localhost:3000 (또는 콘솔에 표시된 주소)에서 접속할 수 있습니다.
   - 백엔드(Jarvis.WebApi)가 먼저 실행 중이어야 정상적으로 동작합니다.

## 3. mcp.json 파일 예시

```json
{
  "mcp": {
    "servers": {
      "obsidian": {
        "command": "npx",
        "args": ["-y", "mcp-obsidian", "/Path/to/your/obsidian/vault"]
      },
      "filesystem": {
        "command": "npx",
        "args": [
          "-y",
          "@modelcontextprotocol/server-filesystem",
          "/Path/to/your/filesystem/root"
        ]
      },
      "puppeteer": {
        "command": "npx",
        "args": ["-y", "@modelcontextprotocol/server-puppeteer"]
      },
      "googlemaps": {
        "command": "npx",
        "args": ["-y", "@modelcontextprotocol/server-google-maps"],
        "env": {
          "GOOGLE_MAPS_API_KEY": "{YOUR_GOOGLE_MAPS_API_KEY}"
        }
      }
    }
  }
}
```

## 4. mcp.json에 MCP 서버 추가 방법

1. `Jarvis.WebApi/mcp.json` 파일을 엽니다.
2. `servers` 객체에 새로운 MCP 서버를 아래와 같이 추가하세요:

   예시 (myserver 추가):

   ```json
   {
     "command": "npx",
     "args": ["-y", "@modelcontextprotocol/server-myserver", "/경로/설정"]
   }
   ```

3. 전체 예시:

   ```json
   {
     "mcp": {
       "servers": {
         "obsidian": { ... },
         "filesystem": { ... },
         "myserver": {
           "command": "npx",
           "args": ["-y", "@modelcontextprotocol/server-myserver", "/Users/사용자명/mydata"]
         }
       }
     }
   }
   ```

4. 서버를 재시작하면 새로운 MCP 서버가 자동으로 반영됩니다.

---

문의사항은 이슈로 남겨주세요.

* 추후 Azure Speech의 기능을 사용해 Wake-word(시리야, 하이빅스비 같은 동작), TTS, STT 기능을 추가할 예정입니다. 
