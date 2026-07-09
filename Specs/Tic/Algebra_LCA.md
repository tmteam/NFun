# LCA — Lowest Common Ancestor

## Определение

**LCA(A, B)** — наименьший (самый конкретный) тип, который является супертипом и A, и B.

Обозначение: **A ∨ B** (join в решётке типов).

LCA — оператор слоя **алгебры**: чистый и тотальный. Поведение слоя **резолюции**
(SolveCovariant/SolveContravariant, политика Preferred, материализация) описано отдельно —
см. `TicPreferred.md` и `TicAlgorithm_Destruction.md` §6. Здесь резолюция упоминается только
через явные ссылки.

## Тотальность

LCA определён для **любой** пары состояний. `Any` — top решётки (∀T: T ≤ Any), поэтому A ∨ B всегда существует. В худшем случае A ∨ B = Any.

## Домены законов

Законы квантифицируются явно по двум доменам:

- **T** — решённые типы (`IsSolved`): примитивы, решённые composites.
- **C** — constraints (`ConstraintsState`, интервалы `[D..A]` с осями).

На T выполняется полный набор законов join-полурешётки. На C выживают только коммутативность
и ассоциативность: `CS ∨ CS` **сознательно сбрасывает Ancestor** (join расширяет, не сужает
сверху), поэтому идемпотентность и поглощение на C не выполняются: `[D..A] ∨ [D..A] = [D..∅]`.

## Алгебраические свойства

| Свойство | Формула | Домен | Верификация |
|---|---|---|---|
| Идемпотентность | `A ∨ A = A` | T | `AlgebraInvariantsTest.Lca_Idempotent`. На C — не выполняется (ancestor-drop сознателен) |
| Коммутативность | `A ∨ B = B ∨ A` | T ∪ C | `Lca_Symmetry` |
| Ассоциативность | `(A ∨ B) ∨ C = A ∨ (B ∨ C)` | T ∪ C | `Lca_Associativity_Primitives` (все 23 точки решётки, включая F32/абстрактные середины/None — `LcaTestTools.PrimitiveTypes` расширен), `Lca_Associativity_Composites`, `Lca_Associativity_WithConstraints` |
| Монотонность | `A ≤ B ⟹ A ∨ C ≤ B ∨ C` | T | `AlgebraLatticeLawsTest.Lca_Monotone_FullLattice` (23³) |
| Поглощение top | `A ∨ Any = Any` | T ∪ C | `Lca_AnyAbsorbs_ConcreteTypes`; факторизация `Opt(Any) = Any`: `Lca_OptAnyEqualsAny` |
| Нейтральность низа | `A ∨ ⊥ = A` (⊥ = пустой CS) | T | `Lca_EmptyConstrainsIsBottom` |
| Сэндвич GLB/LUB | `GCD(A,B) ≤ A ≤ LCA(A,B)` | T | `AlgebraInvariantsTest.Lca_Gcd_Duality_ForPrimitives` |
| Поглощение решётки | `A ∨ GCD(A,B) = A` | T | `AlgebraLatticeLawsTest.Absorption_LcaOverGcd_FullLattice` (skip-set = кросс-семейные null-ы, сверен с оракулом); дуально — `Absorption_GcdOverLca_FullLattice` |
| Ref-прозрачность | `Ref(A) ∨ B = A ∨ B` | T ∪ C | `AlgebraLatticeLawsTest.Lca_RefTransparent` |

## Инварианты

1. **Тотальность**: ∀A,B: A ∨ B определён
2. **Верхняя грань**: A ≤ A ∨ B, B ≤ A ∨ B (`Lca_ResultIsAncestorOfBoth_Primitives`)
3. **Наименьшая верхняя грань**: A ≤ C ∧ B ≤ C ⟹ A ∨ B ≤ C (на примитивах таблица = точная LUB независимого оракула порядка — `AlgebraLatticeLawsTest.Lca_PrimitiveTable_IsLeastUpperBound`, 23²; сам порядок сверен в `Order_MatchesIndependentOracle`)
4. **None lift**: None ∨ T = Opt(T) для T ∉ {Any, None}
5. **Связь с Fit**: `A ≤ B` ⟺ `A ∨ B = B` на T (`Lca_FitsInto_Relationship_Primitives`, `_Composites`)

## Правила по категориям типов

### Примитивы

Решётка примитивов — **23 точки**: Any, Char, Bool, Ip, None и числовая семья
`Real → F32 → I96 → { I64→I48→I32→I24→I16→I12→I8 ; U64→U48→U32→U24→U16→U12→U8→U4 }`.
U4 — низ числовой решётки (номинальная ширина 7: общий поднабор I8 и U8).

Кросс-таблица signed × unsigned задаётся формулой замыкания (m, n — битовые ширины):

```
LCA(U_m, I_n) = наименьший I_k,  k ≥ max(n, m+1),  k ∈ {8,12,16,24,32,48,64}, иначе I96
```

(+1 — бит знака). Например: `I32 ∨ I64 = I64`, `I16 ∨ U16 = I24`, `Bool ∨ I32 = Any`.

Реализация: предвычисленная таблица 23×23, O(1). Иерархия — см. `TicTypeSystem.md`.

### None

None — bottom для optional. LCA с None поднимает тип в Optional:

```
None ∨ None   = None
None ∨ T      = Opt(T)      для T ∉ {Any, None}
None ∨ Any    = Any          (Opt(Any) = Any)
None ∨ Opt(T) = Opt(T)       (Opt(Any) → Any)
```

### Optional и канонические формы

Optional ковариантен: `Opt(A) ∨ Opt(B) = Opt(A ∨ B)`, `Opt(A) ∨ B = Opt(A ∨ B)`.
Если внутренний LCA = Any, результат коллапсирует: `Opt(Any) = Any` (факторизация решётки).

**Правило B (каноническая форма)**: `opt(τ)` строится только над решённым τ. Optional-подъём
нерешённой границы остаётся во **флаг-форме** `[D..A]?` с opt-свободным Descendant:
`[opt(X)..]?` ≡ `opt(opt(X))` — неканонично и запрещено. Поддержание канонической формы —
легитимная часть алгебры (см. срез `innerNoOpt` в разделе ConstraintsState).

### Составные (composite) типы — общее правило

Для составного типа `F<T₁, T₂, ...>` LCA вычисляется **покомпонентно** с учётом вариантности:

| Вариантность | Компонента результата | Оператор |
|---|---|---|
| **Ковариантная** (out) | `Tᵢ' = Tᵢ_a ∨ Tᵢ_b` | LCA |
| **Контравариантная** (in) | `Tᵢ' = Tᵢ_a ∧ Tᵢ_b` | GCD |

Если конструкторы различны — результат Any: `F<...> ∨ G<...> = Any` (F ≠ G).

- **Array** `T[]` — ковариантен: `A[] ∨ B[] = (A ∨ B)[]`
- **Function** `(T₁,..)→R` — контравариантна по аргументам, ковариантна по возврату:
  `(A→R₁) ∨ (B→R₂) = (A ∧ B) → (R₁ ∨ R₂)`. Если argsCount различен — Any.
  Если GCD хотя бы одного аргумента **не определён** (null) — весь LCA = Any:
  join обязан существовать, поэтому частичность GCD деградирует к top.

### Struct — width subtyping

LCA = **пересечение имён полей** с ковариантными LCA типов полей (CS-поля — через Unify):

```
{x:A, y:B} ∨ {x:C, z:D} = {x: A ∨ C}
{x:A}       ∨ {y:B}      = {}
```

Поле исключается, если отсутствует в одном из struct или LCA/Unify типа поля не определён.

**TypeName**: результат сохраняет имя ⟺ обе стороны несут **одно и то же** имя; иначе null
(анонимный struct). LCA не изобретает имя, которого нет у одной из сторон — строже, чем
правило GCD (см. `Algebra_GCD.md`).

**IsOptionalSourced**: через LCA **не распространяется** — это односторонний маркер
cycle-rescue, идентичность join'а не должна наследовать метку одной стороны. GCD, напротив,
объединяет его через OR. Асимметрия сознательная.

**Мутабельность**:
- `Mut ∨ Mut` — поля инвариантны: поэлементный Unify; при **первом** несовпадении весь
  результат деградирует в неизменяемый Struct (поля — ковариантный LCA).
- `Mut ∨ Struct` (и симметрично) — неизменяемый Struct с ковариантным LCA полей.

**Рекурсия и терминация** — коиндуктивное правило: `LCA(μX.T, μX.T) = μX.T`.
Для именованных μ-типов цикл срезается по совпадению имени, для анонимных — по идентичности
пары. Гарантирует терминацию на рекурсивных структурах. Механизм — коиндуктивный контекст
(явный параметр `AlgebraCycleContext`, не ambient-состояние; инвариант №13 Algebra.md),
протянутый через все мосты семейства (поля struct → Unify ≝ Merge → AddDescendant → ∨).

### Пользовательские примитивы

`StatePrimitiveCustom` — изолированные точки решётки: `custom ∨ custom = custom` (то же имя),
`custom ∨ X = Any` для любого другого X.

## ConstraintsState

CS — нерешённый тип: интервал `[Desc..Anc]` и оси:
- **Desc / Anc** — нижняя / верхняя граница (каждая может быть null).
- **IsOptional** — флаг-форма Optional-подъёма (см. Правило B).
- **IsComparable** — constraint сравнимости.
- **StructBound (S)** — структурная верхняя граница (F-bounded). См. `PushReform.md`.
- **Preferred** — метаданные резолюции, хранимые на CS. Алгебраический оператор обязан
  задавать **правило переноса** (transport) для Preferred — оно нормативно, см. ниже.

**CS ∨ CS:**
```
desc = D₁ ∨ D₂        (если один пуст — ↓ₛ другого: снапшот-проекция ConcretestSnapshot,
                       НЕ чистый ↓ — результат хранится как Descendant join'а, и transport-
                       правило Preferred требует выживания вложенных hint'ов; см. ниже)
anc  = ∅               (ancestor ТЕРЯЕТСЯ — join расширяет, не сужает сверху)
opt  = opt₁ OR opt₂    (optional — расширение; join сохраняет расширения)
cmp  = cmp₁ AND cmp₂   (comparable — ограничение; join ослабляет ограничения)
S    = S₁ ∩ S₂         (пересечение полей, рекурсивный LCA на общих; null поглощает к null)
pref = transport(P₁,P₂)  (см. ниже)
```

Семантика S по множествам satisfier'ов: `CS[D..A,S]` удовлетворяется `{T : D≤T≤A ∧ fields(T) ⊇ S}`;
join — более слабое ограничение, значит пересечение полей; отсутствие bound (null) — самое
слабое ограничение и поглощает. Общие поля джойнятся ковариантно через LCA (satisfier любой
стороны удовлетворяет `T.f ≤ lca(S₁.f, S₂.f)`). **μ-позиции выпадают из пересечения**:
чистый join не может именовать совместную рекурсивную переменную, а выпадение required-поля
только ослабляет bound — sound для ∨ (`IntersectBoundsOrNull`, `StateExtensions.Lca.cs`).
Повторный вход в join той же пары bound'ов срезается коиндуктивным контекстом (явный
параметр `AlgebraCycleContext`) с тем же ответом «drop the bound».
Выживший S блокирует коллапс-к-решённому-типу (bound — всё ещё обязательство узла).
Тесты: `AlgebraStructBoundLawsTest.Lca_BothBounds_DisjointFieldNames_ResultHasNoBound`,
`Lca_BothBounds_CommonField_TypesJoinViaLca`, `Lca_OneSidedBound_AbsorbsToNoBound_BothOrders`,
`Lca_CommutativeOnSAxis`, `Lca_RecursionVariablePosition_DroppedFromIntersection`,
`Lca_SurvivingBound_BlocksSolvedCompositeCollapse`.

**Transport-правило Preferred** (hint обязан пережить join):
```
(∅, ∅)   → ∅
(P, ∅)   → P            (односторонний — сохраняем)
(P, P)   → P            (равны — сохраняем)
(P₁, P₂) → LCA_prim(P₁,P₂), если ≠ Any; иначе ∅
```
Обоснование ветки LCA-of-hints: более широкий hint сохраняет намерение резолюции без потери
точности (любой int поднимается в Real без потерь).
Тест: `CanonicalOptionalFormTest.Lca_ArrayIntPref_vs_ArrayEmptyOptional_KeepsIntervalAndHint`.

**Коллапс к типу**: если флаги пусты, Preferred = ∅ и `D₁ ∨ D₂` — решённый composite или Any,
результат — сам тип, не интервал. Ненулевой Preferred **блокирует** коллапс (hint живёт
только на CS). Коллапс в решённый тип гасит hint — резолюция уже произошла.

**T ∨ CS**: `T ∨ [D..] = T ∨ ↓D` (при пустом D — `↓T`). Дополнительно:
- Если `CS.IsOptional` и результат ≠ Any — результат остаётся во флаг-форме
  `CS[strip(T ∨ ↓D)..]?`, где `strip` снимает внешний `opt` с внутреннего результата
  (поддержание канонической формы: Descendant флаг-формы opt-свободен). Preferred переносится.
- `opt(P) ∨ CS[D..A](Pref)` = `CS[P..A]?(Pref)` для решённого примитивного P при Pref ≠ ∅:
  пока CS несёт hint, join остаётся нерешённым интервалом — досрочная материализация hint'а
  нарушила бы политику Preferred (см. `TicPreferred.md` P3).

Симметричен: CS ∨ T = T ∨ CS.

## Связь с другими операторами

- **GCD** (`∧`) — дуальный, частичный. См. `Algebra_GCD.md`.
- **Fit** — `A ≤ B` ⟺ `A ∨ B = B`. См. `Algebra_Fit.md`.
- **Merge** (`⊓` на constraints) и **Unify** — строже LCA, могут вернуть null.
  Unify используется в struct-LCA для CS-полей. См. `Algebra_Unify.md`.

## Сложность

| Входные типы | Сложность |
|---|---|
| Primitive × Primitive | O(1) |
| Composite × Composite | O(components × depth) |
| Recursive struct | + O(nesting) коиндуктивный контекст (явный параметр) |

## Отклонения текущей реализации (см. TicTechnicalDebt)

Закрыто 2026-07-09 (#20): монотонность, Ref-прозрачность и поглощение решётки покрыты
property-тестами `AlgebraLatticeLawsTest` (полная 23-точечная решётка, LUB-точность —
против независимого оракула порядка); `LcaTestTools.PrimitiveTypes` расширен до полных
23 точек — ассоциативность проверяется на полной таблице (см. таблицу свойств выше).
Открытых отклонений нет.
