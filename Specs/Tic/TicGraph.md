# TIC Graph — Граф ограничений

## Что это

TIC Graph — направленный граф, в котором:
- **Узлы** — типовые переменные с состоянием (описанием типа)
- **Рёбра** двух видов: constraint edges и component links

Граф строится из выражения и представляет **систему ограничений** на типы.

## Узлы

Каждый узел имеет:
- **Состояние** — описание типа (конкретный, интервал, ссылка, composite)
- **Список ancestors** — constraint edges к узлам-предкам

Три вида узлов (по происхождению, `TicNodeType`):
- **Named** — переменные выражения (входы/выходы)
- **SyntaxNode** — подвыражения AST (литералы, результаты операторов)
- **TypeVariable** — генерики сигнатур и внутренние переменные, созданные в процессе решения

### Правило жёсткости (rigid/flexible identity)

Вид узла — **несущая** характеристика, не только привязка к AST. Он кодирует
обязательство идентичности (identity commitment):

- **SyntaxNode** и **Named** — *pinned*: у литерала фиксированный apparent type,
  у пользовательской переменной — идентичность, заданная декларацией
  (`y:text = x` не может молча расшириться до `opt(text)`).
- **TypeVariable** — *flexible*: идентичность путешествует вместе с состоянием
  и может переехать во внутренний узел при Optional-lift.

Операционально правило видно в `StagesExtension.WrapAncestorInOptional`
(pinned-предикат): узел pinned ⟺
`Type != TypeVariable` ∨ `IsSignatureParam` ∨ (`IsSolved` ∧ состояние — `StatePrimitive`).
Optional-расширение pinned-узла — ошибка типов (`opt(T) ≤ T` невалиден);
flexible TypeVariable с composite-состоянием оборачивается свободно — идентичность
уезжает в inner-узел вместе с состоянием. Аналогия с HM: pinned ≈ rigid/skolem.

### Свойства узлов

- **IsMemberOfAnything** — узел является компонентой composite (element массива,
  поле struct, и т.д.). Пропускается в основных циклах Pull/Push — обрабатывается
  рекурсивно через component links.
- **IsOptionalElement** — узел является element-ом StateOptional. Используется в
  нескольких decision points (предотвращение `opt(opt(T))` при повторном wrap,
  unwrap-ветки цепочек `x?.b?.c`, поглощение Optional-фактора входящих границ —
  см. CLAUDE.md §Accepted Design). Компенсирует отсутствие parent-ссылок на TicNode.
- **IsSignatureParam** — composite-состояние задано сигнатурой функции (shape-rigid).
  Optional wrapping запрещён: форма конструктора фиксирована, компоненты внутри —
  flexible. Устанавливается при инстанциации вызова функции (Build фаза).
- **IsContractiveCycleHead** — witness-флаг: узел сертифицирован как голова
  контрактивного μ-цикла (Cardelli–Mitchell '89 §3: back-edge цикла пересекает
  конструктор типа). Устанавливается в `NodeToposort.Visit` при обнаружении
  composite-member цикла. Потребители (`ThrowIfRecursiveTypeDefinition`,
  `WrapDescendantInOptional`, `HasAnyRecursiveCandidate` в SCC-проходе) трактуют
  такой узел как контрактивную границу — μ-тип живёт в графе, не как отдельный
  AST-объект (Pottier–Rémy).

## BNF — грамматика типов

Формальная грамматика типовых выражений в TIC:

```bnf
State     ::= Type | Constraints | RefTo

Type      ::= Primitive | Composite
Primitive ::= 'Bool' | 'Char' | 'Ip'
            | 'Real' | 'F32'
            | 'I96' | 'I64' | 'I48' | 'I32' | 'I24' | 'I16' | 'I12' | 'I8'
            | 'U64' | 'U48' | 'U32' | 'U24' | 'U16' | 'U12' | 'U8' | 'U4'
            | 'Any' | 'None'

Composite ::= Array | Optional | Fun | Struct | MutStruct
Array     ::= 'Array(' Node ')'
Optional  ::= 'Opt(' Node ')'
Fun       ::= '(' Node* '→' Node ')'
Struct    ::= '{' FieldDef (',' FieldDef)* StructFlags '}'
MutStruct ::= 'mut{' FieldDef (',' FieldDef)* StructFlags '}'
FieldDef  ::= Name ':' Node
StructFlags ::= ('open')? ('frozen')? ('name=' TypeName)? ('optsourced')?

Constraints ::= '[' Desc? '..' Anc? Flags ']'
Desc      ::= State
Anc       ::= Primitive
Flags     ::= ('opt')? ('cmp')? ('pref=' Primitive)? ('S=' Struct)?

RefTo     ::= 'Ref(' Node ')'

Node      ::= <узел графа, содержащий State>
Name      ::= <идентификатор поля>
TypeName  ::= <имя объявленного типа>
```

**Примечания**:
- Примитивов **23** (включая абстрактные середины I96/I48/I24/I12, U48/U24/U12/U4,
  float-семейство F32/Real и None) — та же решётка, что в `Algebra.md` §Формальный
  статус. Расширение: `StatePrimitiveCustom` — пользовательский тип как изолированный
  примитив (унифицируется только с собой, конвертируется только в Any).
- `Node` — не тип, а узел графа. Composite содержит ссылки на узлы, не на типы напрямую.
- `Anc` (верхняя граница) всегда `Primitive` — по построению: `TryAddAncestor`
  комбинирует границы через GCD примитивов; структурные верхние границы живут
  на отдельной оси `S` (см. ниже и `Algebra.md` §Домен).
- `Desc` (нижняя граница) — **любое State**: примитив, composite (`arr(I32)`),
  вложенный CS. Поэтому проекция ↓ рекурсивна (`Algebra_Concretest.md`).
- `Flags` в Constraints: `opt` = IsOptional, `cmp` = IsComparable,
  `pref` = Preferred (hint слоя резолюции), `S` = StructBound — F-bounded
  структурная верхняя граница `T <: τ(T)`. `S` — третье независимое измерение
  ограничения (peer к cmp/opt); его поля могут содержать `RefTo` назад на
  узел-владелец CS (μ-back-edge). Транспорт S операторами — `Algebra.md`
  §Транспорт осей; контрактивность и владение back-edges — `PushReform.md`.
- `StructFlags`:
  - `open` — row polymorphism: «имеет **минимум** эти поля». Создаётся
    `SetFieldAccess`/`SetSafeFieldAccess`. При комбинировании нижних границ
    (`ConstraintsState.AddDescendant`) open-структуры объединяются по полям
    (union), закрытые — пересекаются (LCA).
  - `frozen` — shape-rigid: набор полей закрыт (сигнатуры функций,
    генерализованные F-bounds). Width-merge не расширяет frozen-структуру
    (`GcdFrozenAndOpen` валидирует кандидата против bound, не поглощая лишние поля).
  - `name=` — `StateStruct.TypeName`: имя объявленного типа (`type node = {...}`);
    null = анонимная структура. Слияние идентичностей — `StateStruct.MergedTypeName`
    (наиболее специфичная непротиворечивая) и `StateStruct.LcaTypeName`
    (только общее имя). Именованные структуры всегда solved.
  - `optsourced` — `StateStruct.IsOptionalSourced`: структура возникла из
    `?.`-ограничения (`SetSafeFieldAccess`). OR-семантика при любых слияниях.
    Гейт cycle-rescue: самозамыкающийся struct-цикл с этим маркером чинится
    восстановлением Optional-разрыва (μX. opt(struct{…X…})), без маркера —
    ошибка рекурсивного типа (`type t = {self:t}`).

## Состояния узлов

### Primitive

Базовый тип, полностью определён: I32, Real, Bool, Char, None, Any, ...

Всегда решён.

### Constraints[D..A, opt, cmp, pref, S]

Нерешённое состояние — интервал допустимых типов плюс оси-обязательства.

- **Desc** (D): нижняя граница. Может быть ∅.
- **Anc** (A): верхняя граница. Может быть ∅.
- **IsOptional**: тип допускает None
- **IsComparable**: тип должен поддерживать сравнение
- **StructBound** (S): F-bounded структурная верхняя граница (может содержать μ-back-edges)
- **Preferred**: hint слоя резолюции; транспортируется операторами по явным
  правилам (`Algebra.md` §Транспорт осей), но не влияет на satisfiability

`[D..A]` означает: тип T такой что `D ≤ T ≤ A`.

Всегда нерешён.

### Composite состояния

Composite — состояние, содержащее **ссылки на другие узлы** графа как компоненты:

| Состояние | Компоненты | Вариантность | Особенности |
|---|---|---|---|
| **Array(elem)** | elem — узел элемента | ковариантен | |
| **Optional(elem)** | elem — узел элемента | ковариантен | не вкладывается: `opt(opt(T))` → `opt(T)` (flatten) |
| **Fun(a₁...aₙ, ret)** | aᵢ — узлы аргументов, ret — узел возврата | args: контравариантны, ret: ковариантен | |
| **Struct{f₁:n₁, ...}** | nᵢ — узлы полей | поля ковариантны | флаги open/frozen/TypeName/IsOptionalSourced — см. BNF выше |
| **MutStruct{f₁:n₁, ...}** | nᵢ — узлы полей | поля **инвариантны** | см. ниже |

**StateMutableStruct** — отдельный конструктор (mutable-структуры языка), не
взаимозаменяемый со StateStruct: кросс-видовые `UnifyOrNull(StateStruct, StateStruct)`
и `GetMergedStateOrNull` возвращают null («разные конструкторы типов»).
Подтипирование: `mut{a:T} ≤ {a:T}` (read-only view безопасен, но не наоборот —
`StateExtensions.Fit`), width subtyping `mut{a,b} ≤ mut{a}` (скрытие полей безопасно).
Инвариантность полей отражена в Destruction (`DestructionFunctions`: пара
MutStruct×MutStruct обрабатывается invariant-путём).

**Решённость composite**: composite решён тогда и только тогда, когда **все** его
компоненты решены. Исключение: именованная структура (`TypeName != null`) считается
решённой — имя фиксирует тип (μ-циклы не разворачиваются бесконечно).

Примеры:
- `Array(узел с состоянием I32)` — решён (компонент решён)
- `Array(узел с состоянием Constraints[U8..Real])` — **не решён** (компонент — интервал)
- `Struct{x: узел I32, y: узел Constraints[..]}` — **не решён** (поле y не решено)
- `Optional(узел с состоянием Constraints[..])` — **не решён**

### RefTo(N)

Ссылка: "этот узел = узел N". Прозрачна для операторов.

Не решён (разыменовывается до состояния целевого узла).

## Два вида рёбер

### 1. Constraint edges (ancestor)

`D →constraint A` означает: **D ≤ A** (тип D — подтип типа A).

Это **ограничение**, которое должно быть удовлетворено при решении.

Создаются при построении графа:
- **Определение** `y = expr`: expr →constraint y
- **Использование переменной**: usage →constraint x
- **Вызов функции** `f(arg)`: arg →constraint paramType
- **if-else**: branch →constraint result

### 2. Component links (structural)

`Parent →component Child` означает: Child — **часть** composite-состояния Parent.

Это **структурная зависимость**, не ограничение. Тип Parent определяется типами
его компонент.

Создаются при установке composite-состояния:
- `Array(T)`: узел массива →component узел T
- `Optional(T)`: узел optional →component узел T
- `Fun(a₁...aₙ → r)`: узел функции →component каждый aᵢ и r
- `Struct{f₁:n₁}`: узел struct →component каждый nᵢ

### Разница

| | Constraint edge | Component link |
|---|---|---|
| Семантика | D ≤ A (ограничение) | T — часть F (структура) |
| Направление | потомок → предок | parent → child |
| Количество | узел может иметь много ancestors | composite имеет фиксированное число компонент |
| Роль | определяет **что должно быть** | определяет **из чего состоит** |

## Пример

Выражение: `y = x + 1`

```
Узлы:             Состояния:
  x (Named)       Constraints[∅..∅]
  1 (SyntaxNode)  Constraints[U8..Real, pref=I32]
  + (SyntaxNode)  RefTo(T)
  y (Named)       Constraints[∅..∅]
  T (TypeVar)     Constraints[U24..Real]

Constraint edges (≤):
  x →constraint T
  1 →constraint T
  + →constraint y

Component links: нет (нет composite состояний в этом примере)
```

Пример с composite: `y = [x, 1]`

```
Узлы:             Состояния:
  x (Named)       Constraints[∅..∅]
  1 (SyntaxNode)  Constraints[U8..Real, pref=I32]
  y (Named)       Array(E)                ← composite!
  E (TypeVar)     Constraints[∅..∅]       ← элемент массива

Constraint edges:
  x →constraint E
  1 →constraint E
  [x,1] →constraint y

Component links:
  y →component E      (Array содержит узел элемента)
```

## Свойства

1. **Constraint edges ацикличны после Toposort-слияния** — циклы из ref/ancestor-рёбер
   сливаются в один узел (`SolvingFunctions.MergeGroup`), после чего constraint-граф —
   DAG. Циклы через **composite-member рёбра** (μ-типы) НЕ сливаются: они
   сертифицируются как контрактивные (голова получает `IsContractiveCycleHead`)
   и сохраняются by design — см. `TicAlgorithm.md` §Циклы.
2. **Component links формируют деревья с точностью до μ-циклов** — рекурсивные
   типы протягивают циклы через Optional/Array/RefTo (и μ-back-edges внутри оси S);
   каждый такой цикл обязан пересекать контрактивный разрыв (строгая проверка —
   `ThrowIfRecursiveTypeDefinition` на Destruction/Finalize).
3. **Состояния мутабельны**: сужаются по мере решения (Constraints → конкретный тип)
4. **RefTo прозрачен**: разыменовывается, не является ни constraint ни component
5. **Решённость рекурсивна**: composite решён ⟺ все компоненты решены
   (именованные структуры — решены по имени)
