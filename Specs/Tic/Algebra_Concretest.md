# Concretest — Нижняя граница типа

## Определение

**Concretest(A)** — наиболее конкретный (узкий) тип, который описание A допускает.

Унарный оператор: извлекает нижнюю границу из описания типа.

Дуален к Abstractest (см. `Algebra_Abstractest.md`).

## Интуиция

Для ConstraintsState `[D..A]`: Concretest → D (нижняя граница).

Для конкретного типа T: Concretest(T) = T (точечный интервал `[T..T]`).

## Правила

### Примитивы

```
Concretest(P) = P
```

### ConstraintsState

```
Concretest([D..A])     = D            (если D определён)
Concretest([∅..A, cmp]) = [.., cmp]   (если D не определён — сохраняем comparable)
```

Если CS.IsOptional и D определён: результат оборачивается в Optional (нижняя граница optional-интервала — optional тип). Opt(Any) коллапсирует в Any.

### Composites — общее правило

Ковариантно по компонентам, **кроме аргументов функций** (контравариантных — используют дуальный Abstractest):

```
Concretest(A[])                   = Concretest(A)[]
Concretest(Opt(A))                = Opt(Concretest(A))           (Opt(Any) = Any)
Concretest({f₁:A₁, ...})         = {f₁: Concretest(A₁), ...}
Concretest((A₁,...,Aₙ) → R)      = (Abstractest(A₁),...) → Concretest(R)
```

Функция: наиболее конкретная функция **принимает** самые широкие аргументы (Abstractest) и **возвращает** самый узкий результат (Concretest).

## Связь с другими операторами

- **Abstractest**: дуальный — извлекает верхнюю границу. См. `Algebra_Abstractest.md`.
- **LCA**: `T ∨ CS = T ∨ Concretest(CS.Desc)` — LCA с constraint использует Concretest нижней границы.
- **Unify**: CS ⊓ CS вычисляет desc через LCA, который может использовать Concretest.

## Свойства

1. **Identity для concrete**: Concretest(T) = T
2. **Нижняя граница**: Concretest([D..A]) ≤ Abstractest([D..A])
3. **Opt collapse**: Concretest(Opt(Any)) = Any
4. **Ref-прозрачность**: Concretest(Ref(A)) = Concretest(A)
5. **Fun контравариантность**: аргументы → Abstractest, возврат → Concretest
