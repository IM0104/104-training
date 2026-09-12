# n8n 活動 4 — 本機設置說明

## 目前已就緒

| 服務 | URL | 狀態 |
|------|-----|------|
| OrderHub Web | http://localhost:5150 | 需保持執行 |
| OrderHub MCP HTTP | http://localhost:3001 | 練習 3 才要 |
| n8n Editor | http://localhost:5678 | 已啟動 |

### n8n 登入（已建立 owner）

- Email: `orderhub@local.test`
- Password: `OrderHub-n8n-2026!`

（僅本機練習用；勿用於正式環境。）

### 已匯入的 workflow

1. **練習1-Hello-Webhook** — 已 **Activate**，Production URL：
   ```
   POST http://localhost:5678/webhook/hello
   ```
   煙霧測試已通過：回應含 `text` + `receivedAt`。

2. **練習2-退單巡檢日報-骨架** — Schedule → HTTP search API → 整理筆數 → AI Agent → IF  
   - GitHub / Data Table 先用 Set 佔位（依你的選擇稍後再補）  
   - **Google Gemini Chat Model** 節點尚無 credential，需你在 UI 貼活動 3 的 API key

---

## 你現在要做的（瀏覽器逐步）

### A. 打開 n8n

1. 瀏覽器開 http://localhost:5678  
2. 用上面帳密登入  
3. 左側應看到兩個 workflow

### B. 驗證練習 1（可選再測一次）

```powershell
Invoke-RestMethod -Method Post -Uri "http://localhost:5678/webhook/hello" `
  -ContentType "application/json" -Body '{"text":"hello"}'
```

### C. 練習 2：接上 Gemini（必要）

1. 打開 **練習2-退單巡檢日報-骨架**  
2. 點 **Google Gemini Chat Model** 節點  
3. Credential → **Create new** → 貼活動 3 的 Gemini API key  
4. Model 選介面裡可用的 **flash**（如 `gemini-2.0-flash` / `gemini-2.5-flash`；文件寫的 `gemini-3.5-flash` 若列表沒有就選最接近的 flash）  
5. 確認 Chat Model 已連到 **AI Agent**（節點下方 ai_languageModel 連線）  
6. 右上角 **Execute Workflow**（手動跑，不必等排程）  
7. 看 AI Agent 輸出的日報；IF 會依 `整理筆數.count` 分流

### D. 之後再補（你選的延後項目）

- **true 分支**：換成 GitHub Create Issue（repo scope token）  
- **false 分支**：Data Table `巡檢紀錄` Insert  
- 通知：把練習 1 Activate 後的 Production URL 當 webhook 通知

### E. 練習 3（MCP 深挖）

1. 確認 MCP HTTP：`dotnet run --project src/OrderHub.Mcp -- --http`  
2. AI Agent → Tool → **MCP Client Tool**  
   - Endpoint: `http://localhost:3001`  
   - Transport: HTTP Streamable  
   - 只勾 `get_order`

---

## 重啟指令（若關機後要再開）

```powershell
# 使用便攜 Node 22（已下載在 tools/node22）
$env:Path = "D:\Test\104-training\tools\node22;" + $env:Path

# 終端 1
cd D:\Test\104-training\training-repo
dotnet run --project src/OrderHub.Web --urls http://localhost:5150

# 終端 2（練習 3）
dotnet run --project src/OrderHub.Mcp -- --http

# 終端 3
cd D:\Test\104-training
npx --yes n8n@1.107.4
```
