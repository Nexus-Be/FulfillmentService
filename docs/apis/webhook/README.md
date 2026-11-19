# BO TAI LIEU API MODULE WEBHOOK GHN (WEBHOOK MODULE APIS)

Module **Webhook** chiu trach nhiem tiep nhan cac su kien bien dong trang thai don hang tu hang van chuyen Giao Hang Nhanh (GHN) gui ve theo thoi gian thuc.

---

## Danh muc chi tiet

| Ma | Ten API | Phuong thuc | Duong dan API | Xac thuc | File tai lieu |
|---|---|---|---|---|---|
| `WHK-01` | **GHN Carrier Webhook** | `POST` | `/api/v1/webhooks/ghn` | Webhook Secret Token | [WHK-01-ghn-webhook.md](WHK-01-ghn-webhook.md) |

---

## Cac ranh gioi kien truc quan trong:
1. **Xac thuc Token**: Chap nhan token trong query string `?token=...` de tuong thich voi giao dien cau hinh webhook tren portal GHN.
2. **Chong lap Idempotency**: Tra cuu qua bang `webhook_audit_logs`.
3. **Outbox sang Commerce**: Cung cap co che tu dong thu lai toi da 12 lan nham bao dam Commerce Service luon nhan duoc mox giao hang thanh cong de cap nhat SubOrder va giai phong tien Escrow.
