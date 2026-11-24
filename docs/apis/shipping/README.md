# BO TAI LIEU API MODULE CUOC PHI & DIA BAN (SHIPPING MODULE APIS)

Module **Shipping (Cuoc phi & Dia ban)** chiu trach nhiem dong bo danh muc dia danh hanh chinh 3 cap tu GHN Master Data, tinh toan cuoc phi van chuyen dong va tham dinh dia chi giao hang.

---

## Danh muc chi tiet tung API

| Ma | Ten API | Phuong thuc | Duong dan API | Quyen han | File tai lieu |
|---|---|---|---|---|---|
| `SP-01` | **Get Provinces** | `GET` | `/api/v1/shipping/locations/provinces` | Public | [SP-01-get-provinces.md](SP-01-get-provinces.md) |
| `SP-02` | **Get Districts** | `GET` | `/api/v1/shipping/locations/districts` | Public | [SP-02-get-districts.md](SP-02-get-districts.md) |
| `SP-03` | **Get Wards** | `GET` | `/api/v1/shipping/locations/wards` | Public | [SP-03-get-wards.md](SP-03-get-wards.md) |
| `SP-04` | **Get Services** | `GET` | `/api/v1/shipping/services` | Public | [SP-04-get-services.md](SP-04-get-services.md) |
| `SP-05` | **Quote Shipping Fee** | `POST` | `/api/v1/shipping/quote` | Public | [SP-05-quote-shipping-fee.md](SP-05-quote-shipping-fee.md) |
| `SP-06` | **Validate Location Hierarchy** | `POST` | `/api/v1/internal/shipping/locations/validate` | Internal Service | [SP-06-internal-validate-location.md](SP-06-internal-validate-location.md) |

---

## Quy chuan kien truc & Bo dem
- **MemoryCache 24h**: Danh muc Tinh/Huyen/Xa lay tu GHN duoc cache 24 gio kem thoi diem GHN tra loi. Khong con danh sach mau: khi khong co du lieu GHN dang tin cay, API tra `503 LOCATION_SOURCE_UNAVAILABLE` (xem SP-01..03, SP-06). Nguon du lieu di qua header `X-Location-Source` va `X-Location-Observed-At`.
- **Graceful Fallback**: Khi GHN tinh phi that bai, he thong tu dong chuyen sang tinh phi co so noi bo.
