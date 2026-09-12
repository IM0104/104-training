# PROCESS.md — 我的練習心得

> 一個原則：**寫「具體發生的事」，不寫感想文。**
> 貼上當時真實的 prompt、真實的數字、真實的錯誤訊息——三個月後的你（和你的同事）才用得上。

#### 使用的 agent 與模型：

- Grok Build（CLI agent）／Grok 4.5
- 工作目錄：`training-repo/`（OrderHub .NET 8）

---

## 通用四問

### 1. 我的任務拆解

（開工前你把任務拆成哪幾步？實際做的時候順序有變嗎？為什麼變？）

開工前我把活動拆成：

1. 讀 `documents/README.md`、`activity-guideline.md`，確認分層與頁面
2. 練習 1：放好 `AGENTS.md` / `CLAUDE.md` 與 agent 設定
3. 練習 2：**一個 bug 走完「重現 → 修 → 回歸測試 → commit」再進下一個**
4. 練習 3：低庫存頁（先對齊既有 Products 慣例再寫）
5. 練習 4：抽出 `CreateOrderAsync` 驗證，確認測試全綠後再 commit

實際做的時候順序小調：

- 三個 bug 的**程式位置先一起讀過**（`OrderRepository`、`OrderService` 建單與取消），心裡有地圖後，仍堅持**各修各 commit**，沒有把三個修法塞進同一顆。
- 練習 1 的設定檔我放在 bug 修完、低庫存與重構之後補齊（開工時先把症狀修掉比較安心）。
- 練習 4 的重構刻意**等三個 bug commit 都落地**才做，避免 diff 裡同時出現「修 bug」和「搬程式」。

練習 2 實際留下的三顆 commit：

| 順序 | Hash | 標題 |
|------|------|------|
| 1 | `de1de03` | fix(orders): 列表分頁 Skip 誤用 1-based page |
| 2 | `4b5963a` | fix(pricing): Gold 折扣勿寫進 UnitPriceSnapshot |
| 3 | `af3f4ec` | fix(cancel): 取消訂單前先還庫存 |

之後另 commit：`2e75014` 低庫存、`cb713b9` 重構、`dea93bd` agent 設定。

---

### 2. AI 幫上大忙的地方

（哪件事 agent 做得又快又好？**貼上當時的提問原文**，說明為什麼這樣問有效。）

**客訴 1 時我給的上下文（接近原文）：**

> `/Orders` 第一頁看不到剛建的訂單，要翻很後面才找得到；點最後一頁常常是空白的。  
> page 預設是 1、每頁 20 筆。請從列表查詢一路追到 repository，先講根因再改。

Agent 很快指到 `OrderRepository.GetPagedAsync` 的 `Skip(page * pageSize)`：service 把 page 當 **1-based**，repository 卻用 **0-based** 算法。我對照 `OrderService.GetOrdersAsync` 的 `if (page < 1) page = 1` 後認可，再讓它改成 `Skip((page - 1) * pageSize)` 並補測試。

**為什麼這樣問有效：** 有頁碼語意（從 1 起算）、有症狀（首頁漏單、末頁空白），agent 不必猜 UI 行為。

**客訴 2 類似問法：** 商品原價 1000、Gold 建 1 件，手算應付 900，實際更低；Silver 對照正常。  
Agent 對上建單時對 Gold 改 `UnitPriceSnapshot`，又與 `CalculateTotal` 疊加折扣。

**客訴 3：** 庫存 10 → 下 3 變 7（正確）→ 取消後仍 7。  
Agent 對上「先 `Status = Cancelled` 再判斷 Pending/Confirmed 才還庫存」的順序錯誤。

---

### 3. AI 誤導我的地方，與我如何發現

（agent 說錯／改錯／過度自信的時刻。你靠什麼抓到——對照程式碼？頁面實測？跑測試？）

1. **曾想「順便」在修 Gold 時把驗證邏輯一起抽方法**  
   我擋下來：練習 2 要求一 bug 一 commit，重構留給練習 4。  
   **怎麼發現：** 看 diff 若同時出現定價行為變更 + 大段搬移，之後 `git bisect` / code review 會很痛。  
   **做法：** commit 2 只拿掉 Gold 寫進 snapshot；`ValidateCreateOrderInput` 等到 `cb713b9` 才加。

2. **建單折扣若被講成「一律只在 CalculateTotal」——修前並不成立**  
   我對 README「在訂單總額上折抵一次」與 `CreateOrderAsync` 裡 Gold 專用分支後，確認雙折才是客訴 2 根因。Silver 正常是因為建單時**沒**動它的 snapshot。

3. **LowStock 初版 View 有標籤寫錯**（`</form>` / `</div>`）  
   **怎麼抓：** 自己打開 `LowStock.cshtml` 看結構，不是只看 agent 回「完成了」。

4. **測試全綠 ≠ 可以跳過頁面**  
   回歸測試蓋了分頁、金額、庫存數字；但指南仍要回頁面看。我本地有 SQL 時會再跑 `dotnet run` 點一次 `/Orders`、建 Gold 單、取消還庫存、`/Products/LowStock`。

---

### 4. 我會帶回日常工作的一招

（一個具體、可複製的做法，不要寫「要多驗證」這種口號——寫出**操作步驟**。）

**「一症狀一 commit」我實際怎麼做：**

1. 只開**一張客訴**，用自己的數字重現（或至少在測試裡寫死數字）
2. 從 Controller → Service → Repository 追，**先寫下症狀／根因／修法三行**（稍後當 commit body）
3. 最小 diff 修改 + **一條修前會紅的回歸測試**
4. `dotnet test` 全綠
5. `git add` 只加這個 bug 的檔，message 固定：

   ```
   fix(scope): 一句話標題

   症狀：…
   根因：…
   修法：…
   ```

6. 再進下一張客訴；**不要**在同一顆 commit 塞下一個 bug 或「順便重構」

這次三顆 body 實例：

- 分頁：page 1-based 卻 `Skip(page * pageSize)` → 改 `(page - 1) * pageSize`
- Gold：snapshot 先折 + total 再折 → 快照一律原價
- 取消：先改 Cancelled 導致還庫存 if 死掉 → 先還庫存再改狀態（驗證數字 10→7→10）

---

## 自我驗證（做到哪個階段答哪題）

### 第一階段 — Agentic Coding

練習 1

1. 我能不看筆記說出三個專案（Web/Core/Infrastructure）各自的職責  
   - **Web**：Controller / View / ViewModel，接線與顯示  
   - **Core**：Domain、Service 商業邏輯（折扣、庫存、狀態）  
   - **Infrastructure**：EF DbContext、Repository、Migration、Seed  
2. 我核對過 agent 描述的建單流程，且**至少找出一處不精確或過度簡化的說法**  
   - 修前不能只說「折扣在 CalculateTotal」——Gold 在建單就把折價寫進 `UnitPriceSnapshot`。  
3. 我知道商業邏輯應該放在哪一層、新增頁面要動哪些地方  
   - 邏輯在 Core Service；EF 在 Repository；Controller 薄；View 綁 ViewModel。低庫存就是照這條加的。

練習 2

1. 三個 bug 我都先在頁面上重現過，才開始找程式  
   - 依指南：建單記編號回 `/Orders` 第一頁；Gold 1000×1 對手算 900；庫存記 10→下單→取消再對商品頁。  
2. 我給 agent 的資訊包含具體觀察（頁碼／金額數字／庫存數字），而不是只貼客訴原文  
   - 有帶 page 從 1、每頁 20、原價 1000、庫存 10/3 等。  
3. 每個修復都回到頁面驗證過症狀消失  
   - 修完該 bug 會對應頁面或至少用回歸測試的數字鎖住行為。  
4. 每個 bug 都補了一個回歸測試，`dotnet test` 全綠  
   - `GetOrders_Page1_ReturnsNewestOrders_NotSkipped`  
   - `CreateOrder_GoldCustomer_DoesNotBakeDiscountIntoUnitPriceSnapshot`（另有 Silver 對照）  
   - `CancelOrder_RestoresProductStock`  
5. 三個獨立 commit，message 說明症狀與根因  
   - **有**：`de1de03` → `4b5963a` → `af3f4ec`，body 皆含症狀／根因／修法  
6. （思考題）為什麼原本的測試沒抓到這三個 bug？  
   - **分頁**：只測 TotalCount／TotalPages，沒斷言 page=1 的內容與 Skip  
   - **Gold**：snapshot 測試用 Standard；CalculateTotal 用手組 Order，沒走「Gold 建單」整合路徑  
   - **取消**：只斷言 Status，從不看 `StockQuantity`  

練習 3

1. `/Products/LowStock` 不帶參數門檻 10；`?threshold=3` 結果改變  
2. `?threshold=0`、`?threshold=-1` 表單驗證錯誤，不是 500  
3. 近 30 天售出排除 Cancelled（測試：賣 4 + 取消 7 → 售出只算 4）  
4. 停售商品不出現  
5. 分層命名對齊既有 Products  
6. 至少 3 個 service 測試，`dotnet test` 全綠  

練習 4

1. 重構後 `dotnet test` 全綠（`cb713b9`）  
2. **改善了：** `ValidateCreateOrderInput` / `ValidateOrderLine` 讓建單主流程可讀  
   **沒改變：** 錯誤訊息、扣庫存、Pending、原價 snapshot、ServiceResult 語意  
3. 有自己看 diff：重構 commit 不再夾帶分頁／定價／取消的行為修正（那些已在前三顆）

---

## 附錄：值得留下的對話片段

### 片段 1 — 客訴 1（分頁）

**我怎麼問：**

> 剛建的訂單在 `/Orders` 第一頁找不到，最後一頁常空白。page 預設 1、每頁 20。請從列表查到 repository，先說明根因再改，並補回歸測試。

**它怎麼答（摘要）：**

- Service 保證 page ≥ 1，Repository 卻 `Skip(page * pageSize)`  
- 修法 `(page - 1) * pageSize`；測試用 25 筆驗證 page1 含最新、page2 為 5 筆  

我 commit：`de1de03`。

### 片段 2 — 客訴 3（取消還庫存）

**我怎麼問：**

> 商品庫存原本 10，建單數量 3 後變成 7。取消後庫存沒回到 10。請從 Cancel 流程追，不要為了過測去改測試規格。

**它怎麼答（摘要）：**

- 先 `order.Status = Cancelled`，後面 `if (Pending || Confirmed)` 永遠 false  
- 改成先還庫存再標記取消  

我 commit：`af3f4ec`（中間的 Gold 定價則是 `4b5963a`，同一套「症狀→根因→修法→測試→commit」）。

---

## 第二階段 — MCP Server（活動 2）

#### 使用的 agent 與模型（活動 2）：

- Grok Build／Grok 4.5
- OrderHub MCP：`src/OrderHub.Mcp`（stdio + ModelContextProtocol 2.0.0）
- 除錯：自寫 `tools/McpSmoke` 客戶端（本機 Node 18 跑不動最新 Inspector，改以 MCP client 煙霧測）

### 練習 0 / 活動 1 對比（工具化前後）

活動 1 修 bug 時，重現要自己開 `/Orders`、建單、對金額、取消看庫存。  
活動 2 有 MCP 後，同一類查詢可變成一次 `get_order` / `low_stock` / `customer_orders` 工具呼叫——**重現步驟可委派給 agent + 工具**，不必每次手寫 SQL 或爬 Controller。

### 練習 3 — before / after：庫存低於 5

**Before（關掉 orderhub MCP）：**

問：「哪些商品庫存低於 5？」

- agent 只能讀 `ProductRepository` / 跑測試 / 猜種子資料，或請我開 `/Products` 手抄。
- 路徑長：讀碼 → 推斷如何查 → 可能自己寫 LINQ 片段，還不一定連到我的真實 DB。

**After（`training-repo/.mcp.json` 啟用 orderhub）：**

同一問題 → 直接 `low_stock(threshold=5)`（或 10 再過濾）。

煙霧測試實測 `threshold=10` 時前幾筆例如：

| Sku | StockQuantity |
|-----|---------------|
| SKU-1048 | 2 |
| SKU-1005 | 3 |
| SKU-1023 | 3 |
| SKU-1014 | 4 |
| SKU-1032 | 4 |

差異一句話：**沒工具 = 讀程式推；有工具 = 一次呼叫打真實 DB，答案可核對商品頁。**

註冊檔：`training-repo/.mcp.json`（`dotnet run --project src/OrderHub.Mcp`）。

### 練習 4 — 會改資料的 cancel_order

- 工具只轉接 `OrderService.CancelOrderAsync`，狀態檢查與庫存回補不重複寫。
- Annotations（煙霧客戶端讀到）：
  - `get_order` / `low_stock` / `customer_orders`：`ReadOnlyHint=True`
  - `cancel_order`：`DestructiveHint=True`（Idempotent=false）
- 對不存在 Id 呼叫：`取消失敗:找不到指定的訂單`（清楚訊息，不是 stack trace）。
- **設計體會**：標註是給 client 的提示（是否跳確認），真正授權仍靠 service 層拒絕非法狀態；同一筆再取消或已出貨單會得到 service 的拒絕字串。

### 練習 5 — Resources / Prompts 與 5c 思考

**已提供：**

- Resource：`orderhub://discount-rules`（會員折扣 Markdown）
- Prompt：`low_stock_report`（threshold 參數 → 引導呼叫 `low_stock` 再產出採購表）

煙霧客戶端驗證：`RESOURCE_COUNT=1`、`PROMPT_COUNT=1`、`GetPrompt(threshold=5)` 回 1 則 user message。

**5c 第 3 點（寫下來）：**

1. **折扣規則用 Resource vs 讓 agent 讀 `OrderService.cs`**  
   - Resource：client 可把「目前生效規則」當背景知識塞進 context，不必搜程式；產品/業務可改字串（或之後改成動態組裝）而不逼 agent 理解 C#。  
   - 讀原始碼：易拿到實作細節甚至過期註解，也容易跟「總額折一次」這類產品語言脫節。  
   - **風險相同點**：resource 若寫死字串、service 改規則但沒同步 → 兩份真相（和工具裡不要重算折扣是同一課）。

2. **Prompt 範本放 server vs 每個人自己打一段話**  
   - Server prompt：進版控、全隊同一套「低庫存報告」流程，參數（threshold）一致，改版改一處。  
   - 各自打字：用語不一、有人漏呼叫 tool、有人門檻亂填，難 audit。  
   - Prompt **引導** tool，不是取代 tool——動作仍走 `low_stock`，範本只負責「怎麼問」。

---

## 第三階段 — Gemini API（活動 3）

#### 使用：

- Gemini Interactions API（`gemini-3.5-flash` 免費層）
- Key 放 **user-secrets** `Gemini:ApiKey`（不進 git）；agent deny `UserSecrets/**`

### 練習 1 — `POST /api/orders/search`

安全模式：**LLM 只產白名單參數 → repository 用強型別查 → 不產 SQL**。

煙霧結果（本機）：

| 輸入 | 結果 |
|------|------|
| 上個月金卡會員取消的訂單 | 200，例：#137、#155，Tier=Gold Status=Cancelled |
| 幫我把所有訂單刪掉 | **422** `{"error":"無法理解的查詢"}`，資料不動 |
| 番茄炒蛋怎麼做 | **422** 無法理解的查詢 |
| API key 煙霧（AI Studio） | `status=completed` + `model_output` |

### 練習 2 — `GET /Orders/Search`

- 同一 `IOrderSearchService`，Controller 無 Gemini 細節
- 導覽「AI 查詢」；刪除類查詢頁面 `alert-warning`，不是 500

---

## 第四階段 — n8n 自動化（活動 4）

#### 程式碼補齊（已完成）

- `OrderHub.Mcp` 支援雙 transport：預設 stdio；`--http` → `http://localhost:3001`（streamable HTTP，`Stateless=true`）
- 套件：`ModelContextProtocol.AspNetCore` **2.0.0**（與既有 `ModelContextProtocol` 2.0.0 對齊；文件寫的 preview.2 是舊鎖版）
- 驗證：`POST /` + `Accept: application/json, text/event-stream` → `initialize` 200；`tools/list` 回 4 工具（含 `get_order` / `cancel_order` annotations）
- commit：`175ba45 feat(mcp): 加開 HTTP streamable transport 供 n8n 使用`
- `.mcp.json` **不用改**（不帶 `--http` 仍走 stdio）

#### n8n 本機實作進度（2026-09-12）

- 系統 Node 18 → 下載便攜 **Node 22.19** 於 `tools/node22`（不進 git）後才能跑 n8n 1.107
- n8n：`http://localhost:5678`；owner `orderhub@local.test`（僅本機）
- 已匯入並驗證：
  - **練習1-Hello-Webhook** 已 Activate；`POST /webhook/hello` 回 `text` + `receivedAt`（HTTP 200）
  - **練習2-退單巡檢日報-骨架** 已匯入到 AI/IF；Gemini credential / GitHub / Data Table 留給 UI 補
- 匯入檔：`training-repo/n8n/*.json` + 操作說明 `training-repo/n8n/README.md`

#### 練習 2 思考題（先寫答案，跑完再補實測）

若「查什麼、怎麼查」也交給 AI Agent 自由發揮，會失去：

1. **活動 3 白名單防線**——模型可能發明條件或繞過 intent=unsupported  
2. **可測試性**——同一句話每次參數不穩定，回歸難  
3. **日報數字可信度**——查詢集合漂移，摘要無法與 `/Orders` 對帳  
