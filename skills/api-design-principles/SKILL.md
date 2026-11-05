---
name: api-design-principles
description: Best practices for RESTful API architecture, predictable schema contracts, HTTP semantics, idempotency, pagination, filtering, and standard error envelopes.
---

# API Design Principles & Contracts

Use this skill when designing, implementing, or reviewing RESTful APIs, web controllers, or service integration endpoints. Ensures APIs are intuitive, consistent, secure, and maintainable over time.

---

## 1. Resource Naming & URI Conventions
- **Nouns over Verbs:** URIs identify resources, not actions:
  - Good: `POST /api/v1/orders`, `GET /api/v1/orders/42`
  - Bad: `POST /api/v1/createOrder`, `GET /api/v1/getOrderById?id=42`
- **Plural Nouns:** Keep resource collections plural (`/users`, `/invoices`, `/products`).
- **Hierarchical Nesting (Max 2 Levels):**
  - Good: `GET /api/v1/orders/42/items`
  - If deeper, flatten it: `GET /api/v1/items?order_id=42` rather than `/companies/1/orders/42/items/7/taxes`.
- **Kebab-case in URLs:** Use `kebab-case` for multi-word paths (`/sales-records`, `/order-suggestions`).

---

## 2. HTTP Method Semantics
- **`GET`:** Safe and idempotent. Used only for retrieval. Must never mutate server state.
- **`POST`:** Non-idempotent creation or command processing (`201 Created` with `Location` header, or `200 OK`).
- **`PUT`:** Idempotent complete replacement of a resource.
- **`PATCH`:** Idempotent partial update of specific fields.
- **`DELETE`:** Idempotent removal (`204 No Content` or `200 OK`).

---

## 3. Standard HTTP Status Codes
- **Success:**
  - `200 OK`: Standard response for successful GET, PUT, PATCH.
  - `201 Created`: Resource successfully created via POST.
  - `204 No Content`: Successful request with no body (e.g., DELETE).
- **Client Errors:**
  - `400 Bad Request`: Malformed syntax, invalid JSON, or missing required payload.
  - `401 Unauthorized`: Authentication missing or token expired.
  - `403 Forbidden`: Authenticated user lacks permission to access this resource.
  - `404 Not Found`: Resource does not exist.
  - `409 Conflict`: Conflict with current state (e.g., duplicate unique key).
  - `422 Unprocessable Entity`: Semantic validation failed (e.g., age must be positive).
  - `429 Too Many Requests`: Rate limit exceeded (include `Retry-After` header).
- **Server Errors:**
  - `500 Internal Server Error`: Unexpected unhandled crash.
  - `502 Bad Gateway` / `503 Service Unavailable`: Upstream dependency down.

---

## 4. Standard Error Response Envelope (RFC 7807)
Never return arbitrary plain text or unformatted stack traces. Return consistent JSON:
```json
{
  "type": "https://example.com/errors/invalid-date-range",
  "title": "Invalid Date Range",
  "status": 422,
  "detail": "The 'date_to' (2026-01-01) cannot be prior to 'date_from' (2026-10-31).",
  "instance": "/api/v1/order-suggestions/123",
  "invalid_params": [
    {
      "name": "date_to",
      "reason": "Must be equal to or greater than date_from"
    }
  ]
}
```

---

## 5. Filtering, Sorting & Pagination
- **Pagination (Cursor or Limit/Offset):** Always paginate collection endpoints by default:
  - `GET /api/v1/products?limit=50&offset=100` or `?cursor=eyJpZCI6NDJ9`
  - Response envelope:
    ```json
    {
      "data": [...],
      "pagination": {
        "total": 1250,
        "limit": 50,
        "offset": 100,
        "has_next": true
      }
    }
    ```
- **Sorting:** Use `sort=field` or `sort=-field` (descending): `?sort=-created_at,name`.
- **Filtering:** Use clear query parameters: `?status=active&brand=mobil`.

---

## 6. Idempotency & Concurrency Control
- For critical operations (payments, order processing), accept an `Idempotency-Key` HTTP header. If the same key is received twice, return the cached result instead of executing twice.
- For concurrent updates, use `ETag` and `If-Match` headers (optimistic locking) to prevent lost updates.
