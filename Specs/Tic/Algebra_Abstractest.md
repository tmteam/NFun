# Abstractest — Чистая проекция на верхнюю границу

## Определение

**Abstractest(A)**, обозначение **↑A** — наиболее абстрактное (широкое) состояние,
которое описание A допускает.

В целевой системе ↑ — **чистая проекция решётки**: тотальный унарный оператор
алгебры. В отличие от ↓, у ↑ нет и текущих Preferred-веток — он не принимает
резолюционных решений (Preferred переживает только cmp-ветку, где CS возвращается
целиком).

Дуален к Concretest (см. `Algebra_Concretest.md`); на контравариантных позициях они
меняются местами.

## Почему ↑ не рекурсивен

Факт домена (см. `Algebra.md` §Домен): **A ∈ Primitive по построению** — верхняя
граница интервала комбинируется через GCD примитивов (`TryAddAncestor`), композитных
значений в A-слоте не бывает. Композитные верхние ограничения выражаются иначе:
структурной унификацией (CS → Composite) либо осью S (F-bound). Поэтому
`↑[D..A] = A` — прямое чтение слота, без рекурсии; рекурсивен только спуск по
composite-компонентам самого A-аргумента оператора.

## Правила (нормативные, целевая система)

### Примитивы

```
↑P = P
```

### ConstraintsState

Порядок веток существенен — **cmp проверяется первой**, даже при определённом A:

```
↑[D..A, cmp=true, ...] = [D..A, cmp=true, ...]    (сам CS, границы нетронуты)
↑[D..A]                = A                          (A определён)
↑[D..∅]                = Any
```

**cmp-ветка возвращает CS целиком** (это и code truth, и целевое правило):
возврат cmp-CS сохраняет живым обязательство comparable — коллапс в `A` или `Any`
потерял бы ограничение, которое финальный тип обязан удовлетворить, а `Any` сам по
себе не comparable. Тест: `MostAbstractStateTest.Constraint2`.

**`S` (StructBound) НЕ участвует в Abstractest.** F-bound — отдельное измерение, не
projection в primitive-интервал. Если бы Abstractest возвращал `S` (StateStruct) при
пустом Ancestor, то операторы вроде `LCA(T_primitive, ↑CS) = LCA(T_primitive, struct) = Any`
потеряли бы `S` без следа. F-bound доступен только через `StructBound(CS)` accessor
(используется в Fit и call-site dispatch). См. PushReform.md.

### Ось IsOptional — сбрасывается

```
↑[D..A]? = A          (ось opt СБРАСЫВАЕТСЯ)
↑[D..∅]? = Any
```

Это самое сильное (и самое неочевидное) правило ↑. Взятое изолированно, оно
**некорректно**: множество сатисфаеров `[D..A]?` состоит из optional-типов, а
`opt(X) ≰ A` для примитивного A ≠ Any — примитив НЕ является верхней гранью
optional-интервала. Правило корректно только **в паре с Destruction**: когда
descendant остаётся optional, стадия Destruction оборачивает ancestor через
`WrapAncestorInOptional` (implicit lift `T ≤ opt(T)` выполняется стадией, а не
проекцией) — суммарный эффект пары «↑ сбросил opt» + «Destruction вернул opt на
ancestor» даёт корректную верхнюю грань `opt(A)`. Это **парный контракт стадий**:
↑ обязан сбрасывать ось (иначе Pull/Push получили бы несравнимые composite-границы
в примитивных слотах), Destruction обязан её восстанавливать. См.
[TicAlgorithm_Destruction.md](TicAlgorithm_Destruction.md) §Фаза 5. По той же
причине пары закон `↓CS ≤ ↑CS` ложен на optional-оси (контрпример в
`Algebra_Concretest.md` §Законы).

Тесты: `MostAbstractStateTest.CS_IsOptional_NoAnc` (`↑[∅..∅]? = Any`),
`CS_IsOptional_WithAnc` (`↑[D..Real]? = Real`).

Замечание: cmp-ветка старше по приоритету и для optional-cmp CS возвращает CS
целиком **с** opt-флагом — flag form канонична (`Algebra_CanonicalForms.md`), ось
не теряется.

### Composites

Ковариантно по компонентам, **кроме аргументов функций** (контравариантных —
используют дуальный Concretest):

```
↑(A[])              = (↑A)[]
↑Opt(A)             = Opt(↑A)                    (Opt(Any) = Any)
↑((A₁,...,Aₙ) → R)  = (↓A₁,...,↓Aₙ) → ↑R
↑Struct             = identity
↑Ref(N)             = ↑N.State                   (ref-прозрачность)
```

Функция: наиболее абстрактная функция **принимает** самые узкие аргументы
(Concretest) и **возвращает** самый широкий результат (Abstractest).

Struct — identity по тем же двум причинам, что и у ↓ (см.
`Algebra_Concretest.md` §Struct): поля — разделяемые узлы графа (покомпонентный
snapshot нарушил бы refinability), а «самый абстрактный struct» при width subtyping —
пустой struct `{}`, что очевидно не проекция описания. Struct уже описывает свою
форму точно; расширяется он только отбрасыванием полей в LCA, не проекцией.

## Связь с другими операторами

- **Concretest**: дуальный — проекция на нижнюю границу. См. `Algebra_Concretest.md`.
- **GCD**: `[D..A] ∧ T = A ∧ T` при определённом A; но `[D..∅] ∧ T = T`, **не**
  `↑CS ∧ T` (= `Any ∧ T = ↑T`) — meet с неограниченным CS определяется вторым
  операндом целиком (закон 5 умбреллы, `Algebra.md`).
- **Слой резолюции**: `SolveCovariant` — резолюционный аналог ↑ (ancestor +
  Preferred + abstract-midpoint). ↑ проецирует, Solve — выбирает.

## Законы

| Закон | Домен | Верификация |
|---|---|---|
| Identity: `↑T = T` | T (решённые) | `MostAbstractStateTest.Primitive1`, `Array1`, `Struct1` |
| Идемпотентность: `↑↑A = ↑A` | весь | `AlgebraLatticeLawsTest.Abstractest_Idempotent` (примитивы 23, CS-зоопарк с флагами/Preferred, композиты с CS-внутренностями) |
| Fit-совместимость: `↑CS Fit CS` | C non-optional, non-cmp | `AlgebraLatticeLawsTest.Abstractest_FitsBackInto_NonOptionalNonCmpCs` |
| Порядок границ: `↓CS ≤ ↑CS` | C **non-optional** | `AlgebraLatticeLawsTest.BoundsOrder_ConcretestLeqAbstractest_NonOptionalCs`; на optional-оси ЛОЖЕН (см. выше) |
| cmp-сохранение: `↑[.., cmp] = [.., cmp]` | C | `MostAbstractStateTest.Constraint2` |
| opt-drop: `↑[D..A]? = A` | C (корректно в паре с Destruction) | `CS_IsOptional_NoAnc`, `CS_IsOptional_WithAnc` |
| Opt collapse: `↑Opt(Any) = Any` | T | `OptAny_CollapsesToAny` |
| Ref-прозрачность: `↑Ref(A) = ↑A` | всё | косвенно всеми suite |
| Fun-дуальность: args → ↓, return → ↑ | T | `MostAbstractStateTest.Fun1-4` |

Монотонность (`A ≤ B ⟹ ↑A ≤ ↑B`) — закрыта 2026-07-10 (долг #20, последний остаток):
на **T** (решённые типы, где Fit — порядок подтипирования) закон ВЕРИФИЦИРОВАН —
`AlgebraLatticeLawsTest.Abstractest_Monotone_SolvedTypes`. На **C** при ≤ = Fit
ОПРОВЕРГНУТ — [Ignore]-пин `Abstractest_Monotone_CsFragment` (369 контрпримеров на
ProjectionZoo²): дуал ↓-опровержения — `[..] Fit Ch` (неограниченный CS может сузиться
до Ch — satisfaction), но `↑[..] = Any ≰ Ch`: ↑ проецирует C на ВЕРХНЮЮ границу, любая
достижимая цель ниже Any опровергает. Плюс собственный класс optional-оси:
`None Fit [..Re]?`, но ↑ сбрасывает opt-ось (`↑[..Re]? = Re`), а `None ≰ Re` —
корректно только в паре с Destruction (парный контракт, см. `↓CS ≤ ↑CS` выше).
Fit(X, C) — satisfaction-отношение, не информационный порядок; C-версия закона
не формулируема без предиката Sat-включения (см. `Algebra_Concretest.md` §Законы).

## Отклонения текущей реализации (см. TicTechnicalDebt)

1. **Собственных резолюционных примесей нет**: реализация ↑ совпадает с целевыми
   правилами (включая cmp-first и opt-drop — оба нормативны). Единственная связанная
   утечка живёт на стороне ↓ (`ConcretestArrayElement`, `Opt(Preferred)`) — см.
   `Algebra_Concretest.md` §Отклонения.
2. **Ось S не транспортируется** — общесистемное отклонение бинарных операторов
   (`Algebra.md` §Отклонения, п.2); ↑ по нормативу S не касается.
3. ~~**Testing gap (остаток)**: монотонность ↑ не покрыта unit-тестами~~ — закрыт
   2026-07-10 (долг #20 закрыт целиком): на T верифицирована, на C опровергнута при
   ≤ = Fit и [Ignore]-запинена с контрпримерами (см. §Законы). Идемпотентность
   и Fit-совместимость (в заявленном домене non-optional, non-cmp) закрыты
   `AlgebraLatticeLawsTest`.
