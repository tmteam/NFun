# TIC Technical Debt

Ограничения текущей реализации, которые отклоняются от идеальной алгебры. Каждый пункт: что не так, почему, что нужно для fix.

---

## 1. ~~IsNonOptional constraint~~ — REMOVED

IsNonOptional infrastructure removed. `??` now uses `SetCoalesce` TIC special form
(see #4), which structurally unwraps the Optional — no constraint flag needed.

## 1a. ~~Single-pass IsNonOptional limitation~~ — N/A (removed with #1)

---

## 2. Struct: отсутствие row polymorphism

**Проблема**: struct generics теряют информацию о полях при covariant resolution.

**Текущее решение**: width propagation в PullConstraintsFunctions.

**Правильный fix**: row polymorphism `T = {a:int | ρ}`.

---

## ~~3. VarTypeConverter `opt(T) → T` passthrough~~ — RESOLVED

**Удалён полностью.** Три корня закрыты:

1. **Type narrowing** — `TypeOverrideNode` + `NarrowIfNeeded()` в ExpressionBuilder.
   Flow-sensitive narrowing bridge (TIC flow-insensitive, narrowing flow-sensitive).
2. **Safe access `?.method()`** — `CachedSourceNode` + TypeOverrideNode unwrap children[0].
3. **IsOptional leak** — `IsolateSharedOptionalElements` в TicSetupVisitor.
   Fresh proxy node при shared generic + Optional. Isolates IsOptionalElement flag.

---

## ~~4. `??` не поддерживает optional right operand~~ — RESOLVED

Replaced generic function `(opt(T), T) → T` with TIC special form `SetCoalesce(left, right, result)`.
Signature: `(opt(U), V) → LCA(U, V)`. Creates fresh element node U, constrains left as opt(U),
U and right are descendants of result. Result = LCA(U, right).
`a:int? ?? b:int?` now works: result = LCA(int, int?) = int?.
VarTypeConverter `opt(T) → T` passthrough removed. FU886 check removed.

---

## Порядок устранения

```
#1 (IsNonOptional) — DONE ✓
#1a (single-pass limitation) — DONE ✓
#3 (passthrough) — DONE ✓ (3 root causes fixed)
#4 (?? optional right) — DONE ✓ (SetCoalesce special form)

#2 (row polymorphism) — независим, высокий effort
```
