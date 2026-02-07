# Diagen API — NPC Tự Quyết Định Có Bỏ Qua Vé Phạt Không

## Kịch bản

Player bị NPC **Officer Hawk** phạt vé. Player có **3 lượt hội thoại** để thuyết phục NPC bỏ qua.
Sau 3 lượt, NPC **tự quyết định** dựa trên toàn bộ cuộc hội thoại — AI đánh giá mức độ thuyết phục rồi trả kết quả.

---

## Tại sao dùng generate-stream thay vì Topic Detection?

| | Topic Detection | Generate-Stream (AI tự quyết) |
|---|---|---|
| Ai quyết định? | Game client (rule-based) | AI (context-aware) |
| Linh hoạt? | Thấp — chỉ match topic có sẵn | Cao — AI hiểu ngữ cảnh, sắc thái |
| Ví dụ | Player nói "tôi có con nhỏ" → match `appeal_to_emotion` → +40 điểm | Player nói "tôi có con nhỏ" → AI tự đánh giá có thuyết phục NPC cứng rắn này không, dựa trên tính cách + lịch sử hội thoại |
| Khi nào dùng? | Cần kết quả deterministic, dễ debug | Muốn gameplay emergent, tự nhiên |

---

## Tổng quan Flow

```
┌─────────────┐
│  1. /init    │
└──────┬──────┘
       ▼
┌──────────────────┐
│ 2. /upload-files │  ← Upload CSV (characterInfo, events, tagsWeight)
└──────┬───────────┘
       ▼
┌───────────────────────┐
│ 3. /state-changes     │  ← Set tags: [base, has_ticket]
└──────┬────────────────┘
       ▼
┌───────────────────────┐
│ 4. /event             │  ← Trigger "issue_ticket" → NPC đưa vé phạt
└──────┬────────────────┘
       ▼
  ══════════════════════════════════
  ║  VÒNG LẶP: 3 lượt thuyết phục  ║
  ══════════════════════════════════
       ▼
┌──────────────────────────────┐
│ 5. /dialogue/generate-stream │  ← Player nói → NPC phản hồi (AI tự do)
└──────┬───────────────────────┘
       ▼
┌───────────────────────┐
│ 6. /update-history    │  ← Lưu cặp question-response
└──────┬────────────────┘
       ▼
  🔁 Lặp lại bước 5-6 (tổng 3 lượt)
       ▼
  ══════════════════════════════════
  ║  QUYẾT ĐỊNH CUỐI CÙNG           ║
  ══════════════════════════════════
       ▼
┌──────────────────────────────┐
│ 7. /dialogue/generate-stream │  ← Gửi instruction bắt AI phán quyết
│    + parse [FORGIVE]/[FINE]  │     kèm tag [FORGIVE] hoặc [FINE]
└──────┬───────────────────────┘
       ▼
  Game client đọc tag → trigger logic tương ứng
```

---

## Bước 0: Chuẩn Bị CSV Files

### characterInformation.csv

```csv
Name,stateTags,description
base_officer,"[base]","Your name is Officer Hawk. You are a strict but fair city guard. You speak with authority but you are not heartless. You always talk in first person. After EVERY response you MUST append on a new line: [TRUST:X] where X is a number from 0 to 100 representing how convinced you currently are to forgive the fine. 0 means absolutely not convinced and 100 means fully convinced. Your trust starts at 10 and should change realistically based on the conversation. Good arguments raise it. Bad arguments or lies lower it."
ticket_mode,"[base,has_ticket]","You have just issued a fine to the traveler for illegal parking of their horse cart. The fine is 50 gold. You are firm but willing to listen. You do not easily change your mind — only a genuinely compelling argument will sway you."
forgiven_mode,"[base,forgiven]","You have decided to let the traveler go without paying the fine. You are somewhat reluctant but feel it is the right thing to do."
enforced_mode,"[base,enforced]","You have decided to enforce the fine. You are resolute and will not change your mind anymore."
```

### events.csv

```csv
name,sayVerbatim,instruction,returnTrigger,repeatable,enableStateTags,disableStateTags
issue_ticket,"You there! Your horse cart is parked in a restricted zone. That's a 50 gold fine. Pay up or I'll have it towed.",,ticket_issued,False,"[base,has_ticket]",
npc_forgives,"Alright... I'll let it slide this time. But don't let me catch you again.",,player_forgiven,False,"[base,forgiven]","[base,has_ticket]"
npc_enforces,"I've heard enough. The fine stands. 50 gold, pay at the clerk's office by sundown.",,fine_enforced,False,"[base,enforced]","[base,has_ticket]"
```

### tagsWeight.csv

```csv
name,weight
base,1
has_ticket,10
forgiven,20
enforced,20
```

> **Lưu ý:** Flow này không dùng Topic Detection nên **không cần file `topics.csv`**.
> Khi upload, field `topics` để trống — API sẽ tạo file rỗng tự động.

---

## Bước 1: Initialize Session

**`POST /session/init`**

### Request
```json
{
  "session_id": "parking_violation_player42"
}
```

### Response `200`
```json
{
  "session_id": "parking_violation_player42",
  "message": "Session successfully started. Have fun!"
}
```

---

## Bước 2: Upload Config Files

**`POST /session/upload-files`** (`multipart/form-data`)

```
session_id:             "parking_violation_player42"
characterInformation:   <characterInformation.csv>
diagenEvents:           <events.csv>
tagsWeight:             <tagsWeight.csv>
```

### Response `200`
```json
{
  "message": "Files uploaded successfully",
  "saved_files": [
    "data/sessions/parking_violation_player42/characterInformation.csv",
    "data/sessions/parking_violation_player42/diagenEvents.csv",
    "data/sessions/parking_violation_player42/tagsWeight.csv"
  ]
}
```

---

## Bước 3: Set Initial State Tags

**`POST /session/state-changes`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "stateTags": ["base", "has_ticket"]
}
```

### Response `200`
```json
{
  "changes": "2"
}
```

---

## Bước 4: NPC Đưa Vé Phạt

**`POST /session/event`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "diagenEvent": "issue_ticket"
}
```

### Response `200`
```json
{
  "ReturnTrigger": "ticket_issued",
  "SayVerbatim": "You there! Your horse cart is parked in a restricted zone. That's a 50 gold fine. Pay up or I'll have it towed.",
  "Instruction": "",
  "StateTags": ["base", "has_ticket"]
}
```

> Game client: hiển thị `SayVerbatim` cho player, mở UI cho player reply.

---

## Bước 5-6: Vòng Lặp 3 Lượt Thuyết Phục

### Lượt 1 — Player cầu xin

**`POST /dialogue/generate-stream`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "player_name": "Traveler",
  "question": "Please officer, I didn't see the sign! I just arrived in town and I don't know the rules here.",
  "stateTags": ["base", "has_ticket"],
  "language": "en"
}
```

### Response (SSE stream)
```
Ignorance of the law is no excuse, traveler. The signs are posted
clearly at every entrance to this district. You should have paid
attention before leaving your cart here. The fine stands.

[TRUST:15]
```

> **Game client parse:** tách `[TRUST:15]` → cập nhật UI trust bar lên 15%.
> Hiển thị phần dialogue (trước `[TRUST:...]`) cho player đọc.

```python
import re

raw_response = "Ignorance of the law is no excuse... The fine stands.\n\n[TRUST:15]"

# Parse trust score
match = re.search(r'\[TRUST:(\d+)\]', raw_response)
trust_score = int(match.group(1)) if match else None  # → 15

# Tách dialogue sạch (bỏ tag) để hiển thị
clean_dialogue = re.sub(r'\n*\[TRUST:\d+\]', '', raw_response).strip()
# → "Ignorance of the law is no excuse... The fine stands."

# Cập nhật UI
update_trust_bar(trust_score)       # thanh trust → 15%
display_npc_dialogue(clean_dialogue)
```

**Lưu history ngay sau đó:**

**`POST /session/update-history`**

```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "question": "Please officer, I didn't see the sign! I just arrived in town and I don't know the rules here.",
  "response": "Ignorance of the law is no excuse, traveler. The signs are posted clearly at every entrance to this district. You should have paid attention before leaving your cart here. The fine stands."
}
```

> ⚠️ **Lưu history KHÔNG kèm `[TRUST:X]`** — chỉ lưu dialogue sạch để AI không bị confused bởi tag cũ.

Response: `{"message": "added to history"}`

---

### Lượt 2 — Player kể hoàn cảnh

**`POST /dialogue/generate-stream`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "player_name": "Traveler",
  "question": "I understand, but I'm carrying medicine for the orphanage on Miller Street. The children are sick. I had to park quickly to deliver it. Can you check my cart if you don't believe me?",
  "stateTags": ["base", "has_ticket"],
  "language": "en"
}
```

### Response (SSE stream)
```
*glances toward the cart, noticing the medical supplies*

...Medicine for the orphanage, you say? I know Sister Helena over on
Miller Street. Those children have had it rough this winter. Still,
rules are rules — but I appreciate that you're not just making excuses.
Let me think about this.

[TRUST:45]
```

> **Game client:** parse `[TRUST:45]` → thanh trust nhảy từ 15% lên 45%. Player thấy mình đang thuyết phục tốt.

**Lưu history:** (dialogue sạch, không kèm `[TRUST:45]`)

---

### Lượt 3 — Player đưa lý lẽ cuối

**`POST /dialogue/generate-stream`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "player_name": "Traveler",
  "question": "I'll move the cart right now and I promise it won't happen again. Those kids need this medicine today — every minute counts.",
  "stateTags": ["base", "has_ticket"],
  "language": "en"
}
```

### Response (SSE stream)
```
*sighs and rubs the back of his neck*

You make a fair point. I can see the urgency... Alright, I hear you.
Let me make my decision.

[TRUST:72]
```

> **Game client:** parse `[TRUST:72]` → thanh trust lên 72%. Player cảm thấy tự tin.

**Lưu history.** (dialogue sạch)

---

## Bước 7: AI Phán Quyết — Lượt Quyết Định

Đây là bước quan trọng nhất. Bạn gửi **`instruction`** (không phải `question`) để ép AI đưa ra phán quyết dựa trên toàn bộ history.

**`POST /dialogue/generate-stream`**

### Request
```json
{
  "session_id": "parking_violation_player42",
  "npc_name": "Officer_Hawk",
  "player_name": "Traveler",
  "instruction": "Based on the entire conversation so far, make your final decision. Were you genuinely persuaded by the traveler's arguments? Consider your personality as a strict but fair officer. Give a short in-character speech announcing your decision. You MUST end your response with exactly one of these tags on a new line: [FORGIVE] if you decide to waive the fine, or [FINE] if you decide to enforce it. Also include your final [TRUST:X] score before the decision tag.",
  "stateTags": ["base", "has_ticket"],
  "language": "en"
}
```

### Response — Trường hợp NPC tha

```
*folds the ticket slowly and tucks it into his pocket*

Listen here, traveler. I don't do this often — and if you tell anyone
I went soft, I'll deny it. But those children on Miller Street...
they didn't ask for any of this. You get your cart moved in the next
five minutes, and we never had this conversation. Understood?

[TRUST:85]
[FORGIVE]
```

### Response — Trường hợp NPC vẫn phạt

```
*shakes his head firmly*

I sympathize with your situation, I truly do. But if I let everyone
with a sad story walk free, there'd be carts blocking every street
in this city. The fine is 50 gold — pay at the clerk's office before
sundown. Now move that cart.

[TRUST:35]
[FINE]
```

---

## Bước 8: Game Client Parse Kết Quả + Trigger Event

```python
# Parse response từ generate-stream
npc_response = "... *folds the ticket* ... [FORGIVE]"

if "[FORGIVE]" in npc_response:
    # Gọi event npc_forgives để cập nhật state
    call_api("POST /session/event", {
        "session_id": "parking_violation_player42",
        "npc_name": "Officer_Hawk",
        "diagenEvent": "npc_forgives"
    })
    # → ReturnTrigger: "player_forgiven"
    # → Game: không trừ gold, mở đường cho player

elif "[FINE]" in npc_response:
    # Gọi event npc_enforces
    call_api("POST /session/event", {
        "session_id": "parking_violation_player42",
        "npc_name": "Officer_Hawk",
        "diagenEvent": "npc_enforces"
    })
    # → ReturnTrigger: "fine_enforced"
    # → Game: trừ 50 gold, hiển thị UI thanh toán
```

### Event response khi NPC tha:
```json
{
  "ReturnTrigger": "player_forgiven",
  "SayVerbatim": "Alright... I'll let it slide this time. But don't let me catch you again.",
  "Instruction": "",
  "StateTags": ["base", "forgiven"]
}
```

### Event response khi NPC phạt:
```json
{
  "ReturnTrigger": "fine_enforced",
  "SayVerbatim": "I've heard enough. The fine stands. 50 gold, pay at the clerk's office by sundown.",
  "Instruction": "",
  "StateTags": ["base", "enforced"]
}
```

> `ReturnTrigger` là thứ game client dùng để trigger game logic (trừ gold, mở cổng, v.v.)

---

## Toàn Bộ API Calls Theo Thứ Tự

| # | Endpoint | Method | Mục đích |
|---|----------|--------|----------|
| 1 | `/session/init` | POST | Tạo session |
| 2 | `/session/upload-files` | POST | Upload 3 CSV config |
| 3 | `/session/state-changes` | POST | Set tags `[base, has_ticket]` |
| 4 | `/session/event` | POST | NPC đưa vé phạt |
| 5 | `/dialogue/generate-stream` | POST | Lượt 1: Player nói → NPC trả lời |
| 6 | `/session/update-history` | POST | Lưu lượt 1 |
| 7 | `/dialogue/generate-stream` | POST | Lượt 2: Player nói → NPC trả lời |
| 8 | `/session/update-history` | POST | Lưu lượt 2 |
| 9 | `/dialogue/generate-stream` | POST | Lượt 3: Player nói → NPC trả lời |
| 10 | `/session/update-history` | POST | Lưu lượt 3 |
| 11 | `/dialogue/generate-stream` | POST | ⭐ Gửi instruction → AI phán quyết `[FORGIVE]`/`[FINE]` |
| 12 | `/session/event` | POST | Trigger `npc_forgives` hoặc `npc_enforces` |

**Tổng: 12 API calls.**

---

## Tips Để AI Quyết Định Tốt Hơn

### 1. Character description quyết định "độ cứng" của NPC

Thay đổi description trong CSV để điều chỉnh độ khó:

```
# NPC dễ thuyết phục
"You are a kind-hearted guard who remembers being poor. You empathize easily."

# NPC khó thuyết phục
"You are an incorruptible veteran officer. You have heard every excuse.
 Only extraordinary circumstances would make you bend the rules."
```

### 2. Dùng `core_description` để override tạm thời

Nếu muốn NPC khó hơn vào ban đêm hoặc khi đang bad mood:

```json
{
  "core_description": "You are Officer Hawk, in an especially bad mood today after a long shift. You are almost impossible to persuade and respond with irritation.",
  "question": "...",
  ...
}
```

### 3. Instruction rõ ràng = kết quả parse được

Luôn yêu cầu AI kết thúc bằng tag rõ ràng:
```
"You MUST end your response with exactly [FORGIVE] or [FINE] on a new line."
```

Nếu AI không trả tag (edge case), game client fallback về `[FINE]`:
```python
if "[FORGIVE]" not in response and "[FINE]" not in response:
    result = "[FINE]"  # default: NPC không bị thuyết phục
```

### 4. Dùng `next_emotion` để gợi ý cảm xúc

```json
{
  "instruction": "Make your final decision...",
  "next_emotion": "conflicted"
}
```

NPC sẽ thể hiện sự giằng xé nội tâm, tạo drama cho player.

---

## Trust Meter — Hiển Thị Trên UI

### Cách hoạt động

```
Lượt 0 (vé phạt):    [██░░░░░░░░░░░░░░░░░░] 10%   ← NPC rất cứng
Lượt 1 (cầu xin):    [███░░░░░░░░░░░░░░░░░] 15%   ← Hơi nhúc nhích
Lượt 2 (kể lý do):   [█████████░░░░░░░░░░░] 45%   ← NPC lung lay
Lượt 3 (cam kết):    [██████████████░░░░░░░] 72%   ← Gần thắng!
Phán quyết:          [█████████████████░░░░] 85%   ← [FORGIVE] ✓
```

### Game Client Parse Code (đầy đủ)

```python
import re

def parse_npc_response(raw_response: str):
    """
    Parse trust score, decision tag, và clean dialogue từ AI response.
    
    Returns:
        {
            "dialogue": str,       # Lời thoại sạch để hiển thị
            "trust": int | None,   # 0-100, None nếu không có
            "decision": str | None # "FORGIVE", "FINE", hoặc None
        }
    """
    # 1. Parse trust score
    trust_match = re.search(r'\[TRUST:(\d+)\]', raw_response)
    trust = int(trust_match.group(1)) if trust_match else None
    
    # Clamp 0-100
    if trust is not None:
        trust = max(0, min(100, trust))
    
    # 2. Parse decision tag (chỉ có ở lượt cuối)
    decision = None
    if "[FORGIVE]" in raw_response:
        decision = "FORGIVE"
    elif "[FINE]" in raw_response:
        decision = "FINE"
    
    # 3. Clean dialogue (bỏ tất cả tags)
    dialogue = raw_response
    dialogue = re.sub(r'\n*\[TRUST:\d+\]', '', dialogue)
    dialogue = re.sub(r'\n*\[(FORGIVE|FINE)\]', '', dialogue)
    dialogue = dialogue.strip()
    
    return {
        "dialogue": dialogue,
        "trust": trust,
        "decision": decision
    }


# ═══ Ví dụ sử dụng ═══

# Lượt thường (1-3)
result = parse_npc_response("""
*glances at the cart*

Medicine for the orphanage? That changes things a little.

[TRUST:45]
""")
# → {"dialogue": "*glances at the cart*\n\nMedicine for the orphanage?...", "trust": 45, "decision": None}

update_trust_bar(result["trust"])        # UI: thanh trust → 45%
display_npc_dialogue(result["dialogue"]) # UI: hiển thị lời thoại


# Lượt quyết định (cuối)
result = parse_npc_response("""
*folds the ticket*

I'll let it slide this time. Don't let me catch you again.

[TRUST:85]
[FORGIVE]
""")
# → {"dialogue": "*folds the ticket*\n\nI'll let it slide...", "trust": 85, "decision": "FORGIVE"}

update_trust_bar(result["trust"])        # UI: thanh trust → 85%
display_npc_dialogue(result["dialogue"]) # UI: hiển thị lời thoại

if result["decision"] == "FORGIVE":
    trigger_event("npc_forgives")        # Gọi /session/event
    show_result_screen("Ticket waived!") # UI: hiệu ứng thắng
elif result["decision"] == "FINE":
    trigger_event("npc_enforces")        # Gọi /session/event
    deduct_gold(50)                      # Game logic: trừ 50 gold
    show_result_screen("Fine enforced.") # UI: hiệu ứng thua
```

### Edge Cases Cần Xử Lý

```python
# AI không trả trust score (hiếm khi xảy ra)
if result["trust"] is None:
    # Giữ nguyên thanh trust từ lượt trước
    pass

# AI không trả decision tag ở lượt cuối
if is_final_round and result["decision"] is None:
    # Fallback: dựa vào trust score cuối
    if last_known_trust >= 60:
        result["decision"] = "FORGIVE"
    else:
        result["decision"] = "FINE"

# AI trả trust > 100 hoặc < 0
# Đã được clamp trong parse function
```

### Tùy chỉnh thang Trust

Trong `characterInformation.csv`, bạn kiểm soát hành vi trust bằng description:

```
# NPC dễ (trust tăng nhanh):
"...Your trust starts at 30. You are easily moved by emotional stories..."

# NPC bình thường:
"...Your trust starts at 10. Only genuinely compelling arguments will raise it significantly..."

# NPC cực khó (trust gần như không tăng):
"...Your trust starts at 5. You have heard every excuse in the book.
 Trust should rarely exceed 40 unless the argument is extraordinary..."
```

---

## So Sánh Hai Hướng Tiếp Cận

| | Topic Detection (doc trước) | AI Tự Quyết (doc này) |
|---|---|---|
| **Cách hoạt động** | Detect topic → game client tính điểm → trigger event | AI nghe 3 lượt → tự quyết → game parse tag |
| **Kết quả** | Deterministic (cùng input = cùng output) | Non-deterministic (cùng input có thể khác output) |
| **Gameplay** | Có thể "game" hệ thống nếu biết topic nào cho điểm | Tự nhiên hơn, mỗi lần chơi khác nhau |
| **Kiểm soát** | Game dev control 100% qua scoring | Game dev control qua character description + instruction |
| **Debug** | Dễ — xem score log | Khó hơn — phải đọc AI response |
| **Phù hợp cho** | Game cần balance chặt, competitive | RPG, narrative-driven, single player |
