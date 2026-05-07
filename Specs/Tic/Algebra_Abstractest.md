# Abstractest — Верхняя граница типа

## Определение

**Abstractest(A)** — наиболее абстрактный (широкий) тип, который описание A допускает.

Унарный оператор: извлекает верхнюю границу из описания типа.

Дуален к Concretest (см. `Algebra_Concretest.md`).

## Интуиция

Для ConstraintsState `[D..A]`: Abstractest → A (верхняя граница).

Для конкретного типа T: Abstractest(T) = T (точечный интервал `[T..T]`).

## Правила

### Примитивы

```
Abstractest(P) = P
```

### ConstraintsState

```
Abstractest([D..A, cmp, S]) =
    A                   если A определён
    [.., cmp]           если A=∅, cmp=true  (comparable нельзя потерять)
    Any                 иначе
```

IsComparable — ограничение, не свойство типа. Abstractest сохраняет его как CS вместо расширения до Any.

**`S` (StructBound) НЕ участвует в Abstractest.** F-bound — отдельное измерение, не projection в primitive-интервал. Если бы Abstractest возвращал `S` (StateStruct) при пустом Ancestor, то операторы вроде `LCA(T_primitive, ↑CS) = LCA(T_primitive, struct) = Any` потеряли бы `S` без следа. F-bound доступен только через `StructBound(CS)` accessor (используется в Fit и call-site dispatch). См. PushReform.md.

### Composites — общее правило

Ковариантно по компонентам, **кроме аргументов функций** (контравариантных — используют дуальный Concretest):

```
Abstractest(A[])                   = Abstractest(A)[]
Abstractest(Opt(A))                = Opt(Abstractest(A))           (Opt(Any) = Any)
Abstractest({f₁:A₁, ...})         = {f₁:A₁, ...}                 (identity)
Abstractest((A₁,...,Aₙ) → R)      = (Concretest(A₁),...) → Abstractest(R)
```

Struct: Abstractest = identity (struct уже описывает конкретную форму).

Функция: наиболее абстрактная функция **принимает** самые узкие аргументы (Concretest) и **возвращает** самый широкий результат (Abstractest).

## Связь с другими операторами

- **Concretest**: дуальный — извлекает нижнюю границу. См. `Algebra_Concretest.md`.
- **GCD**: `CS ∧ T` → использует Ancestor (= Abstractest верхней границы CS).

## Свойства

1. **Identity для concrete**: Abstractest(T) = T
2. **Верхняя граница**: Concretest([D..A]) ≤ Abstractest([D..A])
3. **Opt collapse**: Abstractest(Opt(Any)) = Any
4. **Ref-прозрачность**: Abstractest(Ref(A)) = Abstractest(A)
5. **Fun контравариантность**: аргументы → Concretest, возврат → Abstractest
