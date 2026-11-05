---
name: error-handling-patterns
description: Production-grade defensive programming, custom domain exception hierarchies, safe degradation, observability, and clean error propagation patterns.
---

# Error Handling & Resilience Patterns

Use this skill when designing error handling architectures, implementing exception handling, logging errors, or building resilient backend and service integrations.

---

## 1. The Core Principles of Error Handling

1. **Never swallow exceptions silently:**
   - Anti-pattern: `except Exception: pass` or `catch (e) {}` with no logging or recovery.
   - If an error cannot be recovered from, log it or rethrow it with contextual details.
2. **Fail Fast at Boundaries:**
   - Validate inputs, schemas, parameters, and environmental invariants immediately at entry points (API controllers, function signatures, CLI inputs) before processing.
3. **Fail Safe in Non-Critical Paths:**
   - If a non-essential feature fails (e.g., telemetry, recommendations, optional analytics), degrade gracefully without crashing the core user flow.

---

## 2. Domain-Specific Exception Hierarchies

Structure exceptions cleanly into domain categories rather than throwing generic `Exception` or `Error`:

```
AppBaseException
├── ClientError (4xx equivalent: user input issues)
│   ├── ValidationError (malformed parameters, out-of-range dates)
│   ├── NotFoundError (entity not found in database)
│   └── PermissionDeniedError (unauthorized or insufficient privileges)
└── OperationalError (5xx equivalent: infrastructure / network issues)
    ├── ExternalServiceTimeout (third-party gateway timeout)
    ├── DatabaseConnectionError (connection pool exhausted)
    └── ConcurrencyConflictError (optimistic lock failure)
```

### Benefits:
- Upper layers can catch `ClientError` to return clean 4xx responses without alerting on-call engineers.
- Catching `OperationalError` triggers retries, circuit breakers, or critical alerts.

---

## 3. Safe Error Wrapping & Context Preservation

When catching low-level library errors, wrap them in high-level domain exceptions while preserving the original cause:

```python
try:
    response = requests.post(payment_gateway_url, json=payload, timeout=5.0)
    response.raise_for_status()
except requests.Timeout as e:
    raise PaymentGatewayTimeout("Payment gateway timed out after 5.0s", order_id=order.id) from e
except requests.RequestException as e:
    raise PaymentProcessingFailed(f"Gateway returned error: {e}", order_id=order.id) from e
```

---

## 4. Retries, Backoff & Circuit Breakers

- **Safe Retries:** ONLY retry **idempotent** operations (reads, PUTs, or operations backed by an idempotency key). Never blindly retry non-idempotent charges or mutations.
- **Exponential Backoff with Jitter:** Avoid thunderous herds by adding random jitter to delay intervals:
  $$\text{delay} = \text{base} \times 2^{\text{attempt}} + \text{random\_jitter}$$
- **Timeouts:** Every network call (HTTP, RPC, SQL query, cache) **MUST** have an explicit timeout configured. Default infinite timeouts are a major source of production outages.

---

## 5. Observability & Logging Standards

- **Log at the Right Level:**
  - `DEBUG`: Verbose diagnostics, disabled in production.
  - `INFO`: Normal operational events (e.g., "Order #123 created successfully").
  - `WARNING`: Recovered failure or unexpected situation that did not crash the operation.
  - `ERROR`: Failed operation that affected a user request or background job.
  - `CRITICAL`: System-wide failure (database down, disk full).
- **Structured Logs:** Include contextual keys (`user_id`, `tenant_id`, `request_id`, `duration_ms`).
- **Sanitize Data:** NEVER log passwords, API keys, tokens, credit card numbers, or sensitive PII.
