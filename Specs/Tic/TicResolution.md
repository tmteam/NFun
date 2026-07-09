# TIC Resolution Tail — конвертация TIC-состояний в FunnyType

Хвост слоя резолюции: всё, что происходит с решённым TIC-графом ПОСЛЕ `Solve`,
на границе TIC → runtime. `SolveCovariant`/`SolveContravariant` выбирают точку
**внутри пространства TIC-состояний** (`TicAlgorithm_Destruction.md` §6b–6d);
этот документ владеет второй половиной — проекцией **TIC-состояние → FunnyType**:
конвертеры `TicTypesConverter`, проекция сигнатур `GenericConstrains.FromTicConstrains`,
контракт идентичности генериков и восстановление именованной идентичности.

Нормативный дом композитного утверждения `SolveCovariant ∘ TicTypesConverter`
(`TicSimplePath.md` §8.2.4) — здесь, §8.

---

## 1. Место в конвейере

```
GraphBuilder.Solve → ITicResults (GenericsStates: список нерешённых CS)
                   → TypeInferenceResultsBuilder.Build → TypeInferenceResults
                   → [этот документ] → FunnyType на каждом syntax-узле,
                     сигнатуры функций, generic-аргументы вызовов
```

Три конвертера (все — `TicTypesConverter`):

| Конвертер | Фабрика | CS ↦ | Потребители |
|---|---|---|---|
| `OnlyConcreteTypesConverter` | синглтон `Concrete` | конкретный FunnyType (правила §7) | тело скрипта (`ApplyTiResultEnterVisitor`), конкретные user-функции, `ExpressionBuilderVisitor` (generic-аргументы вызовов), рендеринг ошибок (`Errors.4.Types`) |
| `ConstrainsConverter` | `GenericSignatureConverter` | `FunnyType.Generic(idx)` | `GenericUserFunction.Create` — внешняя generic-сигнатура функции |
| `GenericMapConverter` | `ReplaceGenericTypesConverter` | `_argTypes[idx]` (подстановка) | `GenericUserFunction.CreateConcrete` — мономорфизация тела на вызов |

Все три конвертера прозрачны для `StateRefTo` (дереференс до применения) и
покомпонентно гомоморфны на композитах (§7.3).

---

## 2. Идентичность генериков

### 2.1 Контракт

**Идентичность generic-переменной = позиция её `ConstraintsState`-объекта в
`constrainsMap`** (`ITicResults.GenericsStates`, у user-функций — расширенный
`extendedGenerics`). `ConstrainsConverter.GetGenericIndexOrThrow`,
`GenericMapConverter` и struct-generic wrapper-lookups вычисляют индекс через
`Helper.IndexOfByReference` — **первое `ReferenceEquals`-совпадение** (закрытие
O1/долга #33, 2026-07; ранее — структурный `ConstraintsState.Equals`,
конфлировавший одноконтентные генерики). Тот же контракт у membership-проверки
`CollectGenericStructs` (`GenericUserFunction`): «поле — генерик карты» =
тот же объект.

Инъективность поиска даётся бесплатно: объекты карты попарно различимы по
ссылке по построению (разные TIC-узлы держат разные CS-инстансы).

Ненайденный CS — исключение «Unknown constrains»: конвертация generic-состояния,
не входящего в карту, запрещена по построению.

### 2.2 Инвариант алиасинга (теперь несущий)

**Инвариант R1**: каждый CS, достижимый из конвертируемых состояний
(TIC-сигнатура функции `GetVariableType(name+"'"+argc)`, запомненные
generic-аргументы вызовов `RememberGenericCallArguments`), — **тот же живой
объект**, что и элемент `GenericsStates`.

Держится на том, что `TicResultsWithGenerics.GenericsStates` собирает `node.State`
живых узлов решённого графа (typeVariables ∪ namedNodes ∪ syntaxNodes), а
сигнатурный `StateFun` и запомненные `StateRefTo[]` ссылаются на те же узлы.
`TypeInferenceResultsBuilder.Build` расширяет инвариант на рекурсивные вызовы:
`BuildSharedGenericRefs` оборачивает **те же CS-инстансы** тела в свежие
`StateRefTo` (invisible-узлы), чтобы `GenericMapConverter` на рекурсивном
call-site нашёл их в карте (Amadio–Cardelli: generic-вектор self-вызова равен
вектору объемлющего инстанса).

**Что ломает R1**: любая копия CS между solve и convert (Clone, пересборка
состояния). С reference-поиском ЛЮБАЯ такая копия детектируется явно —
«Unknown constrains» — вместо прежнего тихого попадания одноконтентной копии
в индекс первого структурного двойника. Полная батарея
(Tic/Unit/Syntax/Api) проходит на reference-поиске без единого «Unknown
constrains» — скрытых CS-копий на пути solve→convert нет.

### 2.3 Инъективность — восстановлена (закрытие Equals-конфляции)

Историческая проблема (до 2026-07): `Equals`-поиск конфлировал генерики с
одинаковыми ограничениями (типовой случай — два несвязанных `[∅..∅]`)
first-wins: `f(a,b) = {p = a[0], q = b[0]}` получал сигнатуру
`f(T0[],T0[]):{p:T0;q:T0}` вместо `f(T0[],T1[]):{p:T0;q:T1}`, откуда
`s = f([1.0],[/'x'])` → `{p:Any;q:Any}` (LCA-слияние) и `f([1.0],[2])` →
тихая коэрция `Int32→Real` на `q`.

Пины (RED-подтверждены на Equals-поиске, GREEN на reference):

- юнит (конвертеры напрямую): `TicConverterGenericIdentityTest` —
  `GenericSignatureConverter(new[]{csA,csB})` для двух независимых
  `ConstraintsState.Empty` даёт `Generic(0)`/`Generic(1)`; аналогично на три
  генерика и на подстановку `GenericMapConverter`;
- синтаксис + сигнатура (публичный API): `GenericUserFunctionsTest`, регион
  «Generic identity» — `TwoIndependentGenerics_KeepDistinctTypes`,
  `TwoIndependentGenerics_NoSilentNumericCoercion`,
  `ThreeIndependentGenerics_KeepDistinctTypes`,
  `TwoIndependentGenerics_SameConcreteTypeOnBothArgs_Control`,
  `TwoIndependentGenerics_SignatureHasDistinctGenericArgs`
  (`rt.UserFunctions`: `ArgTypes = (T0[], T1[])`, ids различны).

Замечание из RED-фазы: маскирующий контрпример `f(a,b) = a[0]` корректен и до
фикса — неиспользуемый bare-`[∅..∅]`-вход резолвится в Any и не доживает до
карты (та же ось, что debt #6).

---

## 3. Проекция сигнатур: `GenericConstrains.FromTicConstrains`

Проекция CS на lang-уровневый `GenericConstrains` (для каждого генерика
user-функции, `GenericUserFunction.Create`):

| Ось CS | Судьба |
|---|---|
| `Ancestor` (примитив) | сохраняется |
| `Descendant`, если примитив | сохраняется (`as StatePrimitive`) |
| `Descendant`, если композит | **МОЛЧА ОТБРАСЫВАЕТСЯ** (каст в null) |
| `IsComparable` | сохраняется |
| `Preferred` | сохраняется |
| `IsOptional` | **ОТБРАСЫВАЕТСЯ** — оси opt в `GenericConstrains` нет |
| `StructBound` (S) | не проходит через проекцию; вызыватель re-attach'ит ветку целиком: `HasStructBound` → `WithStructBound(BuildStructBoundFunnyType(...))` вместо `FromTicConstrains` |
| struct-descendant | re-attach вызывателем: `WithStructDescendant` для распознанных struct-генериков (§4) |

Асимметрия: S восстанавливается вызывателем, **opt и композитный D — нет**.

**Последствия и статус**:

- *opt-drop*: обязательство «T пробегает optional-типы» непредставимо в
  lang-констрейнтах; call-site (`InitializeGenericType` →
  `InitializeVarNode(D, A, cmp)`) её не переустанавливает. На практике
  optional-ность аргументов почти всегда несётся **структурно** — arg-позиция
  `opt(T)` конвертируется в `Optional(Generic(i))`, и «?» живёт в типе, не во
  флаге. Флаг-форма CS в сигнатуре (`f(x) = if(x == none) 0 else 1`) работает:
  тело мономорфизуется по фактическим call-site-состояниям (они несут
  opt/`StateOptional`). Пин: `OptionalTypeTest.GenericFunc_WithCoalesce`;
  CLI-пробы `f(5)`/`f(none)`/`f('abc'[0])` — корректны. Контрпример не найден —
  **proof obligation**, не доказанная дыра (§9/O3).
- *композитный D-drop*: нижняя граница `T ≥ arr(...)` / `T ≥ fun(...)`
  исчезает из lang-констрейнта; переустанавливается только там, где T
  структурно виден в ArgTypes. Для struct-случая есть частичная компенсация
  (§4); для array/fun — нет (§9/O4).

---

## 4. Struct-generics: распознавание и подстановка

### 4.1 Детекция (`DetectStructGenerics`)

Struct-генерик = **не-frozen** `StateStruct` с generic-полями (CS из
`GenericsStates`), встречающийся **и в arg-, и в return-позиции** сигнатуры.
Совпадение arg↔return: reference-равенство ЛИБО `StructFieldNamesMatch` —
одинаковая арность и одинаковое множество ИМЁН полей; **значения полей не
сравниваются**. Каждому найденному struct-генерику создаётся
CS-обёртка `ConstraintsState.Of(argStr)`, дописываемая в `extendedGenerics` —
её позиция и есть generic-индекс struct-генерика (§2-контракт распространяется
на обёртки: lookup обёртки в карте — `IndexOfByReference`, обёртка ищется как
тот самый объект, что был дописан; value-семантика здесь не нужна — асимметрии
с §2.1 нет; по именам полей матчится только ПАТТЕРН `StateStruct`, см. §4.2).

### 4.2 Распознавание при конвертации (`TryGetStructGenericIndex` / `TryGetStructGenericType`)

Нормативное правило: `StateStruct` конвертируется как `Generic(idx)`
(соотв. `_argTypes[idx]`), если он **покрывает по именам** первый подходящий
образец карты: `∀ поле образца ∃ одноимённое поле кандидата`
(superset-match; лишние поля кандидата допустимы — открытая строка).
**First-wins по порядку `structGenericMap`; значения полей игнорируются.**

**Врождённая неоднозначность**: два struct-генерика с вложенными множествами
имён (A = {a}, B = {a, b}) неразличимы для кандидата {a, b, ...} — он всегда
уйдёт в тот, что раньше в карте. Аналогично неразличимы генерики с равными
именами полей, но разными типами полей. Детерминированно, но семантически
произвольно (§9/O5).

### 4.3 Констрейнт struct-генерика

`WithStructDescendant(fieldConverter.Convert(structState))`, где
`fieldConverter` — `GenericSignatureConverter` **без** structGenericMap (иначе
сам struct схлопнулся бы в `Generic(j)`); поля результата могут ссылаться на
другие генерики через `FunnyType.Generic(i)`.

---

## 5. Восстановление именованной идентичности: `BuildNamedTypeFromTicState`

Обход **хребта** состояния (не полей!) до первого именованного struct'а:

| Состояние | Правило |
|---|---|
| `StateRefTo` | дереференс, depth+1 |
| `StateOptional` / `StateArray` | рекурсия в элемент; найден → обернуть `OptionalOf`/`ArrayOf`, иначе null |
| `StateStruct` с `TypeName` | `NamedStructOf(TypeName)` — **первый TypeName на хребте выигрывает**, глубже не идём |
| `StateStruct` без TypeName | null (в поля НЕ спускаемся) |
| CS с `HasDescendant` | рекурсия в D; найден и `IsOptional` → `OptionalOf(inner)` |
| иначе / depth > 128 | null |

Depth-guard 128 — защита от identity-through-none-циклов
(`f(x) = if(x==none) none else x`), где D через RefTo указывает назад в ту же
цепь (Round 6 #84); null = «именованного контента нет» — корректный fallback.

Вызыватели — `GenericUserFunction.Create` (arg-позиции сигнатуры) и
`ExpressionBuilderVisitor.VisitOperator` (generic-аргументы операторов):
результат ПЕРЕКРЫВАЕТ структурную конвертацию, чтобы runtime-матчинг шёл по
именованной идентичности, а не по полному структурному сравнению.

**Асимметрия outermost/nested** (debt #8): `ConvertToFunnyStruct` НАМЕРЕННО
разворачивает самый внешний именованный struct в plain `Struct` (runtime
читает поля через `StructTypeSpecification`), а вложенные (depth ≥ 1) и
циклические вхождения возвращает как `NamedStructOf`. `BuildNamedTypeFromTicState`
— компенсатор этой асимметрии для позиций, где идентичность важнее раскладки.

---

## 6. F-bound → FunnyType: `BuildStructBoundFunnyType`

Генерик с `HasStructBound` минует `FromTicConstrains` (§3): его lang-констрейнт —
`WithStructBound(BuildStructBoundFunnyType(StructBound, ownerCs, i))` — runtime-образ
F-bound `T <: {fᵢ: τᵢ(T)}`, где самоссылки закодированы как `FunnyType.Generic(i)`.

Обход (`BuildStructLayer` / `BuildFieldType`) по полям bound'а:

- **Back-edge → `Generic(i)`**. Самоссылкой считается (при пилинге RefTo-цепи и
  на дереференсированном узле) любая из трёх эквивалентных форм: state ==
  `ownerCs`; state == сам `bound` (пост-lift внутренние узлы несут bound, а не
  внешний CS); state — другой `CS{StructBound}` с тем же (по ссылке) bound'ом
  (параллельные CS одной μ-рекурсии от нескольких вызовов `LiftMuTypes`;
  коиндуктивное равенство Cardelli–Mitchell).
- **Цикл без самоссылки** (повторный вход по visited-набору) → **`Any`** — bail.
- `StateOptional`/`StateArray` → обёртка с рекурсией; именованный struct →
  `NamedStructOf`; анонимный struct → рекурсивный слой; примитив → `ToConcrete`.
- **CS-поле → Ancestor-first, иначе примитивный Descendant, иначе Any**:
  bound — ВЕРХНЯЯ граница кандидата, поэтому для поля берётся широчайший
  приемлемый тип (тело `n.v + 1` даёт полю Anc = Real → любой числовой
  кандидат проходит); Desc — запасная точка, композиты не кодируются.

Потребитель — runtime-Fit на call-site (`FunnyTypeFitsStructBound`,
коиндуктивный width-subtyping); рекурсивный self-вызов с неразрешённым
`concreteTypes[i]` (Empty) подставляет сам bound (μ-unfold `S[μX.S/X]`).

---

## 7. Ветки конвертеров

### 7.1 `OnlyConcreteTypesConverter`: CS-армы

Порядок проверок для `ConstraintsState`:

| # | Условие | Результат |
|---|---|---|
| 1 | `Preferred ≠ null` | `ToConcrete(P)`; **opt-обёртка только при `Descendant is StateOptional`** (флаг-форма `IsOptional` НЕ оборачивается — §9/O3) |
| 2 | `¬HasAncestor ∧ IsComparable` | `Real` (D игнорируется) |
| 3 | `¬HasAncestor ∧ D — абстрактный примитив` | `ConcreteAncestor(D)` (политика 1 §6d Destruction-спеки), opt-обёртка при `IsOptional` |
| 4 | `¬HasAncestor ∧ NoConstrains ∧ внутри именованной конвертации` | `NamedStructOf(текущее имя)` — recursion boundary |
| 5 | `¬HasAncestor` (D конкретный/композитный) | **`Any` — D ОТБРАСЫВАЕТСЯ** (см. §8) |
| 6 | `HasAncestor`, A абстрактный | таблица политики 2 (`TicAlgorithm_Destruction.md` §6d): I96/I48/I24/I12 — вниз с уточнением по `D ≤c⁻`; U48→U32, U24→U16, U12→U8, U4→U8 |
| 7 | `HasAncestor`, A конкретный | `ToConcrete(A)` (`IsOptional` игнорируется — §9/O3) |

`StatePrimitive` → `ToConcrete(Name)`; `ToConcrete` тотальна на решётке, включая
абстрактные точки (политика 3 §6d — округление вверх; голые абстрактные точки
легальны после точечного коллапса ⊓) и `None → FunnyType.None`.

### 7.2 `ConstrainsConverter` / `GenericMapConverter`

Единственное отличие от §7.1 — CS-арм: `Generic(GetGenericIndexOrThrow(cs))`
(соотв. `_argTypes[IndexOf(cs)]`), плюс struct-арм §4.2. Остальные ветки
(примитивы, композиты, custom) идентичны. У этих конвертеров НЕТ интервальных
армов — generic-CS обязан быть в карте, иначе исключение.

### 7.3 Покомпонентные гомоморфизмы и cycle-guards

- `StateArray → ArrayOf(Convert(elem))`; `StateFun → FunOf(Convert(ret), Convert(args))` — по одному правилу, без особых случаев.
- `StateOptional → OptionalOf(Convert(elem))` с guard'ом: повторный вход в тот
  же элемент-узел (VisitMark-сентинел) → **`Any`** (циклические Optional'ы из
  generic `if..else none`).
- `ConvertToFunnyStruct`: per-conversion mark (`SolvingFunctions.NextMark`) +
  стек имён `_convertingNamedTypes`. Именованный struct на глубине ≥ 1 →
  `NamedStructOf`; повторный вход по имени → `NamedStructOf`; цикл по
  field-узлу → `NamedStructOf(TypeName поля)`, иначе имя родителя, иначе
  **`Any`**. Все cycle-fallback'ы в `Any` — потеря информации, легальная
  только потому, что вход — цикл без именованного якоря.

### 7.4 `StatePrimitiveCustom`: точный round-trip

Пользовательский тип входит в TIC через `LangTiHelper.ConvertToTiType`
(`BaseFunnyType.Custom → new StatePrimitiveCustom(name, origin)`), хранит
исходный `FunnyType` в `OriginalFunnyType` и во **всех трёх** конвертерах
возвращается как он сам. Round-trip тождественный по построению: TIC видит
изолированный примитив (LCA только с собой и Any, GCD только с собой),
конвертация не интерпретирует — возвращает провезённый объект.

---

## 8. Эквивалентность с резолюцией (нормативно)

Утверждение паритета (`TicSimplePath.md` §8.2.4; нормативная версия — здесь): резолюция SPS обязана
совпадать с композицией **`SolveCovariant ∘ TicTypesConverter`** полного TIC.
Уточнение, принадлежащее этому документу: фактическая композиция полного TIC —
**Destruction-коллапс ∘ SolveCovariant ∘ TicTypesConverter**, и на интервалах
`[D..∅]` паритет несёт именно первый член:

- SPS rule 8 (`d ≠ OPEN → d`) соответствует не арму конвертера, а
  **точечному коллапсу Destruction** (`CS ← Primitive: A := P`): CLI-пробы
  `x:int16; z:int32; c:bool; y = if(c) x else z` дают `y : Int32` на ОБЕИХ
  трассах (SPS-eligible и форсированный полный TIC через text-константу) —
  интервал `[I32..∅]` не доживает до конвертера.
- Арм §7.1/5 (`[D..∅]` c конкретным/композитным D → `Any`) согласован с
  `SolveCovariant` (ковариантный уклон: нет потолка → широчайший тип = Any для
  конкретного D), но НЕ согласован с `SolveCovariant`-армом «композитный D →
  D» и с SPS rule 8. Сконструировать выражение, где этот арм наблюдаемо
  срабатывает с конкретным D, не удалось (Destruction коллапсирует, LCA
  несовпадающих примитивов абстрактен либо Any, optional-джойны уходят в
  opt-варианты правил): пробы `f(a) = a; y = f('abc'[0])` → `Char`;
  `y = if(c) x else none` → `Int16?`; `y = max(x,z)` → `Int32`. Достижим на
  экзотических рекурсивно-optional формах (`f(x) = if(x==none) none else x;
  y = f(42)` → `Any?`), где атрибуция делится с потерей T в return-позиции
  сигнатуры (task #97) и где SPS всё равно воздерживается.

**Статус**: расхождение армов — латентное; какая семантика нормативна
(`Any` по ковариантному уклону vs `d` по rule 8 vs `D` по SolveCovariant-арму)
— открытый вопрос §9/O2. До его закрытия паритет-инвариант SPS держится на
Destruction-коллапсе, а не на конвертере.

---

## 9. Открытые вопросы (кандидаты в реестр долгов)

- **O1. Equals-идентичность генериков — ЗАКРЫТ** (§2.3, долг #33, 2026-07):
  все identity-точки переведены на `Helper.IndexOfByReference`
  (`GetGenericIndexOrThrow`, CS-арм и wrapper-lookups обоих конвертеров,
  `CollectGenericStructs`). Пины: `TicConverterGenericIdentityTest` (юнит),
  `GenericUserFunctionsTest` §Generic identity (синтаксис + сигнатура через
  `rt.UserFunctions`). `Helper.IndexOf` (value-семантика) сохранён для
  не-identity потребителей (`Errors.4.Types` — позиция syntax-узла).
- **O2. Армы `[D..∅]` конвертера vs rule 8 vs SolveCovariant** (§7.1/5, §8): три
  несовпадающих правила для одного состояния; латентно, маскируется
  Destruction-коллапсом. Требуется нормативное решение и выравнивание
  (минимум: композитный D → `Convert(D)` как в SolveCovariant, либо явное
  доказательство недостижимости арма).
- **O3. Потеря `IsOptional` на границе** (§3, §7.1/1, §7.1/7): проекция
  сигнатур отбрасывает флаг; Preferred-арм оборачивает только композитную
  форму `Descendant is StateOptional`, Ancestor-арм не оборачивает вовсе.
  Контрпример не найден (opt несётся структурно) — оформить как
  proof obligation: «флаг-форма CS с Preferred/Ancestor недостижима на
  конвертере» либо закрыть обёрткой.
- **O4. Композитный Descendant отбрасывается `FromTicConstrains`** (§3):
  array/fun-нижние границы генериков не существуют на lang-уровне; struct
  компенсирован только при arg∧return-детекции (§4.1).
- **O5. Неоднозначность struct-generic-матчинга** (§4.2): superset-по-именам,
  first-wins, значения полей игнорируются — вложенные field-set'ы и
  одноимённые генерики неразличимы.
- **O6. Четвёртое ad-hoc правило резолюции вне слоя**: `RuntimeBuilder`
  (`PrecomputeDefaultValues`, `ApplyTiResultToSubtree`) резолвит CS как
  `Preferred ?? Descendant` в обход и Solve*, и конвертера — ещё одна
  несогласованная точка выбора (для default-значений параметров).
