# ADR-0002: Integer minor units

Status: Accepted.

Persist currency values as signed 64-bit minor units plus a currency code. Convert user-entered decimal values only at boundaries. This avoids binary floating-point rounding errors.
