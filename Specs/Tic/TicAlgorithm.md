# TIC Algorithm — Построение и решение графа ограничений

## Фазы

Порядок фаз — `GraphBuilder.SolveCore`:

1. **Build** — построение constraint graph из AST
2. **Toposort** — топологическая сортировка; ref/ancestor-циклы сливаются (⊓),
   composite-member циклы сертифицируются как контрактивные μ-циклы и сохраняются
   (см. §Циклы). Может быть fused с Pull при отсутствии None-узлов.
3. **Pull** — распространение нижних границ (desc → anc), оператор LCA
4. **PropagatePreferred** — broadcast Preferred hint по совместимым CS нодам.
   Выполняется **между Pull и Push**: Push-ячейка `Apply(StatePrimitive, CS)`
   схлопывает литеральный CS `[U8..Re]I32!` до голого примитива, когда ancestor
   пришпиливает его к единственному типу — после Push хинт с CS уже исчез и
   `CollectPreferred` не нашёл бы ничего (комментарий MR2Bug4 в
   `GraphBuilder.SolveCore`; безопасно по TicPreferred P3 — Preferred не влияет
   на Pull/Push).
5. **Push** — распространение верхних границ (anc → desc), оператор GCD
6. **SCC-замыкание** (`ScCClosurePass`, только при `IsRecursion`) — Push до
   фикспойнта на циклических контрактивных SCC (см. §Циклы)
7. **Destruction + Finalize** — см. `TicAlgorithm_Destruction.md`

Pull и Push — ядро алгоритма. Все операции в них выражаются через:
- **Операторы алгебры** — инвентарь конституции (`Algebra.md` §Инвентарь):
  3 бинарных (LCA ∨, GCD ∧, Merge ⊓), 3 предиката (FitsInto ≤, Convert ≤c⁺/≤c⁻),
  3 унарных (Concretest ↓, Abstractest ↑, Simplify)
- **Структурная декомпозиция** — следствие правил подтипирования для composites:
  `F<A₁,...> ≤ F<B₁,...>` раскладывается на компонентные ограничения по вариантности
  (см. `TicTypeSystem.md`)
- **Implicit lift** — постулат подтипирования `T ≤ Opt(T)` (см. `TicTypeSystem.md`)

Независимость результата Pull+Push от выбора конкретного topological order
(confluence через коммутативность/ассоциативность LCA/GCD и лемму Ньюмана,
`Algebra.md` §Теоремы) доказана для **ацикличного rewrite-free фрагмента** —
области действия и каверзы см. §Сходимость.

---

## Соглашения

### Пустые границы (∅)

`∅` в Desc или Anc означает **отсутствие ограничения**:
- `Desc = ∅` — нижняя граница не установлена. Эквивалент: "любой тип допустим снизу."
- `Anc = ∅` — верхняя граница не установлена. Эквивалент: "любой тип допустим сверху."

Операторы с ∅ (∅ — нейтральный элемент при **комбинировании однородных границ**):
```
LCA(∅, T) = T          (комбинирование нижних границ: ∅ не ограничивает)
LCA(∅, ∅) = ∅
GCD(∅, T) = T          (комбинирование верхних границ: ∅ не ограничивает)
GCD(∅, ∅) = ∅
↓[∅..A] = ∅            (Concretest: нет нижней границы)
↑[D..∅] = ∅            (Abstractest: нет верхней границы)
FitsInto(T, ∅) = true  (нет ограничения — всё допустимо)
FitsInto(∅, T) = true
```

**Область применения**: ∅ как нейтральный элемент используется **только** при
комбинировании границ одного вида: Pull комбинирует Desc (нижние) через LCA, Push
комбинирует Anc (верхние) через GCD. Операторы LCA/GCD с ∅ **не вызываются** для
проверки совместимости между Desc и Anc разных интервалов — для этого используется
FitsInto.

`∅` — **не тип**, а отсутствие информации. Семантически: `[∅..∅]` = "неограниченная
переменная", `[D..∅]` = "известна нижняя граница", `[∅..A]` = "известна верхняя граница".

### Флаги ConstraintsState

Помимо интервала `[D..A]`, ConstraintsState содержит оси-обязательства (транспорт
осей операторами — единая таблица в `Algebra.md` §Транспорт осей; здесь — что
делают **стадии**):

- **IsOptional** — тип допускает None. Устанавливается None absorption:
  `ConstraintsState.AddDescendant(None)` ставит флаг вместо записи None в Desc.
  Pull (CS←CS): `A.opt := A.opt ∨ D.opt` (OR — расширяет множество значений).
  Push флаг не транспортирует.
- **IsComparable** — тип должен поддерживать сравнение. Устанавливается из сигнатуры
  функции (операторы `>`, `<` и т.д.). **Pull не транспортирует cmp**: comparable на
  descendant не требует comparable на ancestor (`max(a,b)` возвращает comparable, но
  if-else может объединить comparable и non-comparable ветки). **Push** передаёт вниз:
  `D.cmp := D.cmp ∨ A.cmp` (`PushConstraintsFunctions.Apply(CS,CS)`, независимо от
  наличия A.Anc). **Merge (⊓)**: `cmp₁ ∨ cmp₂`. Валидация: `SimplifyOrNull` отвергает
  интервал, если IsComparable=true, но тип не comparable.
- **StructBound** (`S`, `StateStruct?`) — структурная верхняя граница (F-bound,
  `T <: τ(T)`); может содержать `RefTo` назад на узел-владелец CS (μ-back-edge).
  После закрытия debt #12 ось S **транспортируется самими операторами**:
  `∨ → S₁∩S₂`, `∧/⊓ → S₁∪S₂` (ownerless `GcdBound` — перенос по значению,
  node identity self-referential позиций сохраняется). За **стадиями** остаётся
  перенос владения μ-back-edges: Pull/Push зовут `GcdBound` с owner-узлами и
  делают rewire self-RefTo; одностороннее принятие bound'а — через
  `RewireStructBoundOwnership`; merge-вызыватели (`MergeGroup`, `MergeInplace`)
  алиасят проигравшего (`loser := RefTo(winner)`), так что сохранённые back-edges
  прозрачно разыменовываются в merged-переменную. `Unify(CS,CS) ≡ MergeOrNull`
  (Unify(CS,CS) ≡ Merge), поэтому S идёт через Merge-ядро и при слиянии циклов.
  Контрактивность: каждый back-edge от `S.Fields` к владельцу обязан пересекать
  Optional/Array — строгая проверка в `ThrowIfRecursiveTypeDefinition`
  (CS{S} трактуется как контрактивная граница обхода). Cross-ref: `Algebra.md`
  §Транспорт осей, `PushReform.md`.

Все три измерения (`IsOptional`, `IsComparable`, `StructBound`) **независимы**:
каждое распространяется по своим правилам, не вмешиваясь в `[D..A]`.

**Preferred** — hint слоя резолюции (pref=I32 для целочисленных литералов).
Транспортируется операторами по явным правилам (`Algebra.md` §Транспорт осей),
но **не влияет на satisfiability** — не создаёт ограничение, только выбирает точку
из уже вычисленного интервала при `SolveCovariant`. Полная спецификация:
`TicPreferred.md`.

**Транспорт Preferred в стадиях** (единственное место, где правила стадий
сформулированы — таблицы Pull/Push ссылаются сюда):

1. **Pull(CS←CS)** — двунаправленно (`PullConstraintsFunctions.Apply(CS,CS)`):
   вверх `A.pref := D.pref` если `A.pref = ∅` (литералы → элементы массивов);
   вниз `D.pref := A.pref` если `D.pref = ∅` (struct field chains → результаты
   generic-функций).

2. **Merge (⊓)** — `PreferredHintLcaOrNull`: коммутативный hint-LCA
   (`оба ∅ → ∅; один → он; разные → P₁ ∨ P₂ если ≠ Any, иначе ∅`) плюс
   пост-условие: hint, не проходящий Fit в результат, сбрасывается.

3. **PropagatePreferred pass** (`SolvingFunctions.PropagatePreferred`, между Pull
   и Push — обоснование в §Фазы):
   - **Collect**: обход всех узлов (включая компоненты composites, через
     `GetNonReference`). Берётся **первый найденный** Preferred.
   - **Apply**: для каждого CS с примитивным Desc, конвертируемым в collected hint:
     если своего Preferred нет — установить; если есть, но он reference-equal
     собственному Descendant (авто-инициализация от generic-constraint вроде
     Arithmetical→U24, а не литеральное намерение) — переустановить.
   - Ограничение single-global-hint — debt #7 (Bug Z).

**Инвариант**: Preferred никогда не СОЗДАЁТ constraint — он только ВЫБИРАЕТ из
существующих валидных решений. Любой результат в интервале `[Desc..Anc]` корректен,
Preferred лишь выбирает "лучший".

---

## Фаза 1: Build

**Вход**: AST + реестр функций. **Выход**: TIC Graph (см. `TicGraph.md`).

### Правила построения

Каждая конструкция языка порождает узлы и рёбра:

| Конструкция | Constraint edges | Component links | Состояние |
|-------------|-----------------|-----------------|-----------|
| Литерал `42` | — | — | `[U8..Real, pref=I32]` |
| Переменная `x` | `usage →c x` | — | `[∅..∅]` |
| Определение `y = e` | `e →c y` | — | y: output |
| `if(c) a else b` | `a →c R`, `b →c R` | — | R: `[∅..∅]` |
| `try a catch b` | `a →c R`, `b →c R` | — | R: `[∅..∅]` |
| `try a catch(e) b` | `a →c R`, `b →c R` | — | R: `[∅..∅]`, e: `{message:text, data:any}` (scoped) |
| `[a, b, c]` | `a →c E`, `b →c E`, `c →c E` | `Arr →comp E` | Arr: Array(E) |
| `{x:a, y:b}` | `a →c Nₓ`, `b →c Nᵧ` | `S →comp Nₓ`, `S →comp Nᵧ` | S: Struct (закрытый) |
| `s.field` | — | `s →comp N` | s: Struct{field:N} (**open** — row polymorphism) |
| `s?.field` | — | `res →comp inner` | res: Opt(inner); struct — open + **IsOptionalSourced**, граф помечается `IsRecursion` |
| `a ?? b` | `a →c Opt(U)`, `U →c R`, `b →c R` | `Opt(U) →comp U` | U: fresh `[∅..∅]`, R: `[∅..∅]` |
| `a?[i]` | `a →c Opt(Arr(E))`, `i →c I32` | `res →comp inner` | res: Opt(inner), inner связан с E |
| `x != none` narrowing | `orig →c Opt(T)`, `T` merged с narrowed | — | narrowed: unwrapped тип внутри if-branch |

### Инстанциация вызовов функций

Вызов `f(a₁,...,aₙ)` с generic-сигнатурой `(P₁,...,Pₙ) → R` (где Pᵢ, R могут
содержать типовые переменные T₁,...,Tₘ):

1. Для каждой типовой переменной Tⱼ из сигнатуры создаётся **свежий узел** Tⱼ' с
   ограничениями из сигнатуры. Например, `(T,T)→T` где T:[U24..Real] → узел T' с
   состоянием `[U24..Real]`.
2. Для каждого аргумента: `aᵢ →c Pᵢ'` (constraint edge к инстанциированному параметру).
3. Для composite-параметров (Array, Fun, Struct): создаются component links и
   узлы-компоненты по структуре параметра; узлы помечаются `IsSignatureParam`
   (shape-rigid).
4. Результат: merge узла возврата R' с узлом выражения вызова.

**Пример**: `count(arr)` с сигнатурой `(T[]) → I32`:
- Узел E':[∅..∅] (свежий, для T), узел P':Array(E') (параметр)
- `arr →c P'`, component link `P' →comp E'`
- Результат вызова: I32

**Пример**: `x + 1` с сигнатурой `(T,T) → T` где T — Arithmetical `[U24..Real]`:
- Узел T':[U24..Real] (свежий)
- `x →c T'`, `1 →c T'`
- Результат: RefTo(T')

### Constraint generation rules (формально)

Для каждой синтаксической формы — правило генерации ограничений:

```
[[lit: P]]           = node(lit) : [P..P]
[[lit: N, pref=P]]   = node(lit) : [N..P_anc, pref=P]         (числовой литерал)
[[x]]                = node(usage) →c node(x)                  (использование переменной)
[[y = e]]            = [[e]] →c node(y);  output(y)            (определение)
[[if(c) a else b]]   = [[c]] →c Bool;  [[a]] →c R;  [[b]] →c R;  R : [∅..∅]
[[try a catch b]]    = [[a]] →c R;  [[b]] →c R;  R : [∅..∅]
[[try a catch(e) b]] = [[a]] →c R;  scope(e:{message:text, data:any}){ [[b]] } →c R;  R : [∅..∅]
[[ [a₁,...,aₙ] ]]    = ∀i: [[aᵢ]] →c E;  Arr : Array(E);  Arr →comp E
[[{f₁:a₁,...}]]      = ∀i: [[aᵢ]] →c Nᵢ;  S : Struct{f₁:N₁,...} (закрытый);  S →comp Nᵢ
[[e.f]]              = [[e]] becomes Struct{f:N} (open);  result = N
[[e?.f]]             = [[e]] →c Opt(Struct{f:N}, optsourced);  result = Opt(N)
[[f(a₁,...,aₙ)]]    = instantiate(sig(f));  ∀i: [[aᵢ]] →c Pᵢ';  result = merge(R', call_node)
[[a ?? b]]           = U fresh; [[a]] →c Opt(U); U →c R; [[b]] →c R;  R : [∅..∅]   (SetCoalesce)
```

Instantiate: для каждой типовой переменной T в сигнатуре — свежий узел T'.
Composite-параметры: shape-rigid (IsSignatureParam). Результат: подстановка
`σ = {T₁↦T'₁, ...}`, применённая к сигнатуре.

### Инвариант

После Build:
- Каждое `D →c A` означает `D ≤ A`.
- Каждое `P →comp C` означает C — структурная часть P.
- Constraint edges формируют почти-DAG (возможные циклы обрабатываются в Toposort).

---

## Фаза 2: Toposort

**Вход**: граф из фазы 1. **Выход**: линейный порядок `N₁,...,Nₖ`
(`NodeToposort.OptimizeTopology`).

**Инвариант порядка**: `Nᵢ →c Nⱼ` ⟹ `i < j`.

DFS обходит **оба** вида рёбер (constraint + component). Порядок посещения:
компоненты → ancestors → сам узел.

**Оптимизация: fused Toposort+Pull.** Если в графе нет ни одного None-узла
(`hasNone=false`), Toposort и Pull объединяются в один проход: Pull выполняется
inline для каждого узла в момент его emit-а из toposort. Если `hasNone=true` —
Toposort и Pull выполняются раздельно (двухфазный Pull требует предварительного
полного toposort-порядка). Семантика идентична; это чисто перформансная оптимизация.

### Два вида циклов — два исхода

- **Ref/ancestor-циклы** (взаимные ссылки: `x = f(y); y = g(x)`): все узлы цикла
  **сливаются в один** (`SolvingFunctions.MergeGroup`). Состояния объединяются через
  `GetMergedStateOrNull` (⊓ / Unify; для пары CS — `MergeOrNull`); проигравшие узлы
  становятся `RefTo(winner)`, их ancestors переносятся на winner. Алгебраически:
  `a ≤ b ≤ c ≤ a ⟹ a = b = c`.
- **Composite-member циклы** (back-edge проходит через компоненту composite —
  элемент Optional/Array, поле Struct, компонент Fun): узлы **НЕ сливаются**.
  Инициатор цикла получает витнес `IsContractiveCycleHead`, и toposort продолжается —
  цикл сохраняется в графе как кандидат μ-типа. Критерий здесь **слабый**
  (любое composite-member ребро считается пересечением конструктора); строгая
  проверка «разрыв именно через Optional/Array» отложена до Destruction — см.
  §Циклы (двухъярусная контрактивность). Если composite-member цикл обнаружен вне
  контекста сертификации — ошибка рекурсивного типа.

После слияния ref/ancestor-циклов **constraint-edge граф — DAG**; μ-циклы через
конструкторы остаются и обрабатываются циклической машинерией (§Циклы).

**Member-узлы** (компоненты composites) включены в порядок, но помечены
`IsMemberOfAnything` — в основных циклах Pull/Push пропускаются, обрабатываются
рекурсивно через component links.

---

## Базовые операции

### Структурная декомпозиция

Следствие правил подтипирования composites (см. `TicTypeSystem.md`). Если `D ≤ A`
и оба — composites одного конструктора, ограничение раскладывается на компонентные:

| Конструктор | Декомпозиция `D ≤ A` |
|-------------|---------------------|
| Array | `D.elem ≤ A.elem` |
| Optional | `D.elem ≤ A.elem` |
| Fun | `D.ret ≤ A.ret`, `A.argᵢ ≤ D.argᵢ` |
| Struct | `D.fₖ ≤ A.fₖ` для каждого поля fₖ ∈ A |

Fun: аргументы **контравариантны** — рёбра создаются в обратном направлении
(Pull `Apply(StateFun, StateFun)`: `anc.argᵢ →c desc.argᵢ`). Struct: потомок должен
иметь **все** поля предка (width subtyping) — недостающие поля добавляются потомку;
условия расширения предка — §Width-propagation ниже.

Разные конструкторы несовместимы: `Array(...) ≤ Fun(...)` → ошибка типов.

### Структурная унификация

Если `D ≤ A`, D — ConstraintsState, A — Composite `F<C₁,...,Cₙ>`
(реализация — семейство `SolvingFunctions.TransformToArrayOrNull` /
`TransformToFunOrNull` / `TransformToStructOrNull` / `TransformToOptionalOrNull`):

1. D **принимает форму** A: `D.State := F<C'₁,...,C'ₙ>`
2. Новые компоненты C'ᵢ:
   - Если CS.Desc — composite того же конструктора F: C'ᵢ наследуют соответствующие
     компоненты Desc
   - Если CS.Desc — composite другого конструктора: **ошибка типов**
   - Если CS.Desc = ∅ или Primitive: C'ᵢ := `[∅..∅]` (свежие неограниченные)
3. Исходное ребро `D →c A` **заменяется** на компонентные рёбра (декомпозиция)
4. Новые component links: `D →comp C'ᵢ`
5. **Все остальные** constraint edges D (к другим ancestors) остаются. При их
   обработке D уже имеет composite-форму → срабатывает Composite←Composite или
   Composite←CS по стандартным правилам

**Инвариант**: структурная унификация создаёт рёбра только к **потомкам** A в
toposort (компоненты A расположены раньше A). Поэтому новые рёбра обрабатываются
**в том же проходе** рекурсивно, без повторного обхода. (Исключение из этого
инварианта — Optional-lift-rewire, см. §Циклы (d).)

### Optional wrapping (расширение узла)

Если `D ≤ A`, D — `Opt(F<...>)`, A — `F<...>` (тот же конструктор, но без Optional):

```
A:[F<C₁,...>]  ≤  D:[Opt(F<C'₁,...>)]
⟹  A := Opt(inner),  inner.State := F<C₁,...>,  затем декомпозиция inner ≤ D.elem
```

Это **LCA**: `LCA(F<...>, Opt(F<...>)) = Opt(F<...>)` — ancestor расширяется, чтобы
вместить Optional-descendant. Обе операции — структурная унификация (сужение CS до
Composite) и Optional wrapping (расширение Composite до Opt) — **смена состояния
узла**, не монотонное сужение интервала; обе алгебраически обоснованы
(`CS ≤ Composite`; `LCA(T, Opt(T)) = Opt(T)`).

**Реализация: единый dispatch в `StagesExtension`**, используемый всеми фазами
(Pull, Push, Destruction):

- **`WrapAncestorInOptional`** (non-Opt composite ancestor × Opt descendant):
  1. **Коиндуктивный identity guard**: если `optB.ElementNode.GetNonReference()`
     — это сам ancestor, лифт `T ≤ opt(T)` тривиально выполнен (идентичность
     совпадает) → `true` без действий. Именно этот guard делает утверждение
     «каждый узел оборачивается не более одного раза» истинным: без него wrap
     порождал бы неограниченную цепочку re-wrap `opt(opt(...))`.
  2. **Pinned-identity rule**: wrap отвергается (ошибка типов `opt(T) ≤ T`),
     если у узла есть внешнее обязательство идентичности:
     `Type != TypeVariable` (SyntaxNode-литерал ИЛИ Named-переменная)
     ∨ `IsSignatureParam` (shape-rigid по контракту сигнатуры)
     ∨ (`IsSolved` ∧ состояние — `StatePrimitive`) (анонимные примитивно-решённые
     промежуточные узлы). TypeVariable с composite-состоянием — **не** pinned:
     идентичность (включая TypeName именованных структур) уезжает в inner-узел
     вместе с состоянием.
  3. Иначе: создаётся inner-узел с текущим состоянием ancestor
     (`IsOptionalElement := true`), `A := Opt(inner)`, рекурсивный
     `Apply(Opt, Opt)`.

- **`WrapDescendantInOptional`** (Opt ancestor × non-Opt composite descendant),
  `LCA(Opt(T), T) = Opt(T)`. Три redirect-пути **вместо** оборачивания:
  1. Descendant — SyntaxNode или `IsSolved` → **unwrap**: ограничение
     переписывается на элемент (`Invoke(f, optA.ElementNode, desc)`) — литерал
     не оборачивается, а проверяется как element.
  2. Descendant — `IsContractiveCycleHead` ∧ TypeVariable → **unwrap**:
     сертифицированный μ-цикл уже представляет фикспойнт `μX. opt(...)`;
     дополнительный wrap строил бы башню развёрток вместо единого фикспойнта
     (Pierce TAPL §20.2) с экспоненциальным `opt(opt(...))`-взрывом.
  3. Descendant — не-SyntaxNode с composite-состоянием и `!IsOptionalElement` →
     **implicit lift к элементу**: descendant соединяется с элементом Opt
     напрямую, сохраняя собственную не-Optional идентичность (generic-параметр,
     разделяемый сигнатурой — MBug4; Named-переменная с конкретным composite
     от литерала — MR5Bug5; зеркало pinned-правила WrapAncestor).
  Иначе (гибкий узел): создаётся inner-узел, `D := Opt(inner)`, рекурсивный
  `Apply(Opt, Opt)`.

### Implicit lift (Optional)

Постулат `T ≤ Opt(T)` — правило подтипирования (см. `TicTypeSystem.md`).
Применяется в трёх ситуациях:

1. **Unwrap**: `D:[non-Opt] ≤ A:[Opt(E)]` → ограничение переписывается как `D ≤ E`
2. **Wrap**: `D:[CS, IsOptional] ≤ A:[non-Opt Composite]` → D принимает форму
   `Opt(inner)`, inner получает ограничения CS без IsOptional, `inner ≤ A`.
   **Действует только в Push и Destruction.** В **Pull** та же конфигурация
   **отвергается** (guard MR7Bug3 в `PullConstraintsFunctions.Apply(ICompositeState, CS)`):
   opt-CS-потомок логически представляет `opt(inner)`, а `opt(T) ≤ T` невалиден —
   без guard'а per-shape Transform-ветки молча стирали бы IsOptional и цепочка
   `arr?.method()[idx]` компилировалась бы в unsound bare-index. Асимметрия
   Pull-reject / Push-wrap намеренная: к моменту Push Optional-факт уже
   распространён Pull-ом, и wrap материализует его структурно; в Pull тот же wrap
   маскировал бы ошибку.
3. **None absorption**: `D:None ≤ A:[CS]` → `A.IsOptional := true` (не меняет Desc/Anc)

**Обратное направление запрещено**: `Opt(T) ≤ T` — ошибка типов (implicit unwrap
невалиден).

---

## Фаза 3: Pull

**Цель**: для каждого `D →c A` передать нижнюю границу D в A.

**Порядок**: по toposort (потомки раньше предков). Для каждого узла
(`SolvingFunctions.PullRec`): сначала обработка constraint edges (ancestor edges),
затем рекурсивно Pull компонентов (по component links). Обоснование: Pull может
изменить состояние узла (CS → Composite через Transform*), после чего composite
members нужно рекурсивно обработать.

### Определение

```
Pull(A, D) — обновить A, учитывая D ≤ A
```

### None: двухфазная обработка

Если в графе есть хотя бы один узел с состоянием None, Pull выполняется в два
прохода **по тому же toposort-порядку** (`SolvingFunctions.PullConstraintsTwoPhase`):

**Проход 1** — только None-узлы:
- Для каждого узла D с `D.State = None`: обработать все constraint edges D.
- Основное действие: `A.IsOptional := true` (None absorption через `AddDescendant`).

**Проход 2** — все не-None-узлы:
- Стандартные правила Pull. К этому моменту IsOptional уже установлен на всех
  ancestors None-узлов; Concretest и LCA корректно учитывают optional-природу.

**Зачем**: None absorption устанавливает IsOptional — **флаг**, а не значение в
решётке. Другие правила Pull (LCA, структурная унификация) зависят от этого флага.
Без двухфазности: если не-None потомок обработан до None-потомка, ancestor может
получить Desc без учёта IsOptional, что приводит к ложным конфликтам.

Если None-узлов нет — один проход, все узлы (fused с Toposort).

### Правила

Для каждого constraint edge `D →c A` (реализация — `PullConstraintsFunctions`):

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Проверка `D ≤c⁻ A` (пессимистичный Convert). Ошибка если false. |
| Primitive | CS | Проверка `D ≤c⁺ A` (оптимистичный Convert — отклонять можно только то, что не спасёт никакая резолюция). |
| Primitive | Composite | Проверка `D ≤c⁻ A`. Только `Any` совместим. |
| CS | Primitive | `A.Desc := LCA(A.Desc, D)` (`AddDescendant`; None → `A.opt := true`). Валидация `SimplifyOrNull`. |
| CS | CS | `A.Desc := LCA(A.Desc, D.Desc)`; если A — element Opt (`IsOptionalElement`), Optional-фактор входящей границы поглощается (`opt(X)` входит как `X`). `A.opt := A.opt ∨ D.opt`. **cmp не транспортируется.** Preferred — двунаправленно (§Транспорт Preferred, правило 1). S — `GcdBound` с owner-rewire / `RewireStructBoundOwnership`. |
| CS | Composite(non-Opt, non-Struct) | `A.Desc := LCA(A.Desc, D)`. |
| CS{S} | Composite(Struct) | Struct-потомок — контрибьютор bound'а: `GcdBound(A.S, D)` (meet = поле-union), затем `AddDescendant` + `SimplifyOrNull`. |
| CS | Optional | Если A — `IsOptionalElement`: redirect `D.elem →c A` (Opt-фактор поглощён уровнем выше). Иначе — implicit lift (wrap): probe-копия A проверяет совместимость (`SimplifyOrNull`), A принимает форму `Opt(inner)`; inner получает ограничения A без IsOptional и без Optional-фактора Desc; S мигрирует на inner с rewire self-RefTo; `D.elem →c inner`. |
| Composite | CS(IsOptional) | **Отказ** (guard MR7Bug3): `opt(T) ≤ T` невалиден — см. §Implicit lift, правило 2. |
| Composite | CS | Структурная унификация D → форма A (Transform*), затем декомпозиция. Struct: слияние TypeName (`MergedTypeName`, конфликт → downgrade до анонимного, Bug HH), мердж S потомка в результат (`GcdBound`), отождествление identity общих полей (`UnifyStructFieldIdentities`), width-propagation к предку (см. ниже). |
| Composite | Composite(same) | Структурная декомпозиция → Pull для каждой пары компонент. Struct: двусторонний мердж TypeName/IsOptionalSourced + width-propagation. Array: если элемент потомка уже Opt, а элемент предка — CS, элемент предка оборачивается в Opt (компенсация stale-snapshot после Phase 2). |
| Opt(E) | Primitive(non-None) | `LiftDescendantToOptionalElement`: rewire `D →c E` + **немедленный** Invoke (см. §Циклы (d), debt #10). |
| Opt(E) | None | Detach ребра (None ≤ Opt(T) для любого T). |
| Opt(E) | CS | `TransformToOptionalOrNull`: `D := Opt(inner)`, `inner →c E` (ветки Transform: пустой CS; Desc=None — поглощение; Desc=opt(X); флаг opt с границами — интервал/cmp уходят в inner, Preferred переносится, при его отсутствии примитивный Desc становится Preferred inner-а). Transform не сработал → fallback-веер, см. §Opt(E) ← CS в Pull. |
| non-Opt Comp | Primitive | **Отказ** (`Apply(ICompositeState, StatePrimitive)` → false → `IncompatibleTypes`): примитив — не подтип bare-composite-конструктора. |
| non-Opt Comp(F) | Opt(F) | `WrapAncestorInOptional`: identity guard → true; pinned узел → **ошибка**; гибкий TypeVariable → wrap (см. §Optional wrapping). |
| non-Opt Comp | Opt(другой конструктор) | **Ошибка**: разные конструкторы несовместимы. |
| Composite | Composite(diff, non-Opt) | **Ошибка**: разные конструкторы несовместимы. |

### Width-propagation в Pull

Когда потомок-структура имеет поля, которых нет у предка, Pull добавляет их предку
при условии `!ancestor.IsFrozen` (обе ветки: Struct←Struct и Struct←CS).
Обоснование: `desc ≤ anc`, anc выводится → anc должен быть максимально узким
(специфичным) → впитывает все поля потомка. Frozen-структуры (сигнатуры,
генерализованные F-bounds) не расширяются — их набор полей закрыт контрактом.

### Opt(E) ← CS в Pull: fallback-веер

Когда `TransformToOptionalOrNull` не сработал (у потомка есть не-Optional-границы
без флага opt, либо только оси без интервала), `Apply(StateOptional, CS)` разбирает
случаи **по порядку** (`PullConstraintsFunctions`, Opt-ветка):

1. **Desc — Struct** → материализация `D := Opt(inner)`: inner — копия CS потомка
   (struct-ограничения переносятся), S-rewire self-RefTo
   (`RewireStructBoundOwnership`), `inner →c E`. Возникает из `?.field`-предка на
   lambda-параметре, уже несущем struct-Desc от generic-байндинга.
   **Асимметрия зафиксирована**: guard Pull-ветки — `Desc is StateStruct` (любой,
   включая закрытый литерал); Push-близнец (`Opt(E) | CS`, §Фаза 4) требует
   `Desc.IsOpen` — закрытый литерал `{b=1}` в Push обязан идти через implicit
   lift, а не wrap (MR5Bug5). В Pull аналогичного `IsOpen`-guard'а нет.
2. **Флаг `IsOptional`** (достижим только без интервала — opt-флаг с границами
   забирает сам Transform; остаются cmp/S/Preferred) → материализация
   `D := Opt(inner)`: inner получает границы/cmp/Preferred БЕЗ opt-флага (флаг не
   должен протечь мимо структурного Optional-слоя — SetCoalesce: U не наследует
   IsOptional), `inner.IsOptionalElement := true`, S-rewire, `inner →c E`.
3. **Desc — примитив или ∅** → implicit lift `T ≤ opt(T)`: rewire `D →c E` +
   немедленный Invoke (`LiftDescendantToOptionalElement`, §Циклы (d)).
   Desc=None → detach ребра (защитная ветка: Desc=None поглощает сам Transform,
   в текущем коде сюда не доходит).
4. **Desc — composite не-Struct (Arr/Fun)** → **тихий fall-through**: `return true`
   без каких-либо действий — ребро не переписывается и не удаляется, остаётся до
   Push/Destruction. Зафиксированная дыра ветвления, а не правило.

### Результат Pull

```
∀ узел A:  A.Desc ⊒ LCA{↓Dᵢ | Dᵢ →c A}
```

Каждый ancestor впитал нижние границы всех потомков; `⊒` (а не `=`) — из-за
row-union open-структур и Optional-веток, см. §Теорема.

---

## Фаза 4: Push

**Цель**: для каждого `D →c A` передать верхнюю границу A в D.

**Порядок**: по **обратному** toposort (предки раньше потомков). Для каждого узла
(`SolvingFunctions.PushRec`): сначала рекурсивно Push компонентов (guard по mark для
рекурсивных типов), затем обработка constraint edges. None-узлы пропускаются
целиком (None — литеральное значение, в него нечего проталкивать).

### Определение

```
Push(A, D) — обновить D, учитывая D ≤ A
```

### Правила

Для каждого constraint edge `D →c A` (реализация — `PushConstraintsFunctions`):

| A.State | D.State | Правило |
|---------|---------|---------|
| Primitive | Primitive | Проверка `D ≤c⁻ A`. |
| Primitive | CS | `D.Anc := GCD(D.Anc, A)` (`AddAncestor` + `SimplifyOrNull`). |
| Primitive | Composite | Нет действия (`D ≤ Any` тривиально). |
| CS(opt) | None | **OK, нет действия**: `None ≤ CS?` всегда валидно — None живёт на Optional-оси и в интервале не участвует. |
| CS | Primitive | Если `A.Anc ≠ ∅`: проверка `D ≤c⁻ A.Anc`. Если `A.Anc = ∅`: пропуск. |
| CS | CS | `D.cmp := D.cmp ∨ A.cmp` (независимо от Anc). S — `GcdBound`/`RewireStructBoundOwnership` (независимо от Anc). Если `A.Anc ≠ ∅`: `D.Anc := GCD(D.Anc, A.Anc)` (`TryAddAncestor` + `SimplifyOrNull`). |
| CS{S} | Composite(Struct) | **Проекция F-bound вниз**: недостающие S-обязательные поля добавляются в non-frozen потомка (open-row extension; frozen → ошибка), для общих полей — рекурсивный Push field-состояний. |
| CS{S} | Composite(Array/Fun) | **Ошибка**: структурный конфликт S с не-struct конструктором. |
| CS(Desc=Struct) | Composite(Struct) | Поле-ограничения из **Desc** предка передаются вниз: Opt-поле × CS-поле → element-Push (в обе стороны); CS-поле × не-Any → **`MergeInplace`** (узловая унификация); иначе — Push. |
| CS(Anc≠∅,≠Any) | Composite | **Ошибка**: composite не подтип конкретного примитива. |
| non-Opt Comp | CS(IsOptional) | Implicit lift (wrap, обратная сторона Pull-отказа MR7Bug3): D := Opt(inner), inner наследует интервал/cmp без opt, `inner ≤ A`, рекурсивный Push. Единообразно для Array/Fun/Struct. |
| Composite | CS | Структурная унификация (Transform*) + рекурсивный Push компонент. Struct: `TryMergeStructFields` (см. ниже). |
| Opt(E) | CS | `TransformToOptionalOrNull`; если он не сработал: (a) Desc — **open** struct → wrap D в Opt(inner) с переносом struct-ограничений (guard именно `IsOpen`, не `!IsSolved` — закрытый литерал `{b=1}` должен идти через implicit lift, а не заражать получателя Optional-ом; MR5Bug5); (b) `IsOptional` → материализация D := Opt(inner), element-Push (флаг не должен протечь мимо структурного Optional-слоя); (c) иначе — implicit lift: redirect `D →c E` + рекурсивный Push (None-Desc — просто detach). |
| Composite | Composite(same) | Рекурсивный Push пар компонент. Struct: поля + width-propagation (см. ниже); Opt/Array: element-пары; Fun: `PushFunTypeArgumentsConstraints` — args **контравариантно** `(A.argᵢ ≤ D.argᵢ)`, ret ковариантно `(D.ret ≤ A.ret)` — согласовано с Pull/Transform-рёбрами и Destruction Fun-arm (долг #24 закрыт 2026-07-10: прежнее ковариантное спаривание args было инверсией; вердикт COMPENSATED опровергнут — ячейка достижима на решённой fun-паре через if-else-LCA лямбд + вызов; red-first пины `SolvingFunctionsTest.PushConstraints_SolvedFuns_*`). |
| Opt(E) | Primitive(non-None) | Redirect `D →c E` + рекурсивный Push (implicit lift unwrap). |
| Opt(E) | None | Нет действия. |
| non-Opt Comp | Primitive | **Отказ** (`Apply(ICompositeState, StatePrimitive)` → false → `IncompatibleNodes`): примитив — не подтип bare-composite-конструктора. Зеркало Pull-ряда. |
| non-Opt Comp(F) | Opt(F) | `WrapAncestorInOptional` (тот же dispatch, что в Pull). |
| Composite | Composite(diff, non-Opt) | **Ошибка**. |

### TryMergeStructFields (Struct-ancestor × CS-потомок после Transform)

Для каждой пары полей: None-поле потомка — skip (Optional-слой снаружи); None-поле
предка — Push вниз; Opt-поле предка × CS-поле потомка — Push element-ограничений
(MergeInplace несовместимых форм невозможен — MR2Bug1); оба поля — solved
primitives — Push (ковариантность структур: `{x:I32} ≤ {x:Real}` валиден, equality
не требуется); **всё остальное — `MergeInplace`**: узлы полей отождествляются
целиком (унификация identity, требуется для Optional-propagation).

None-field-skip и OR-слияние `IsOptionalSourced` — не локальная особенность этой
процедуры, а общее правило всех **трёх** точек слияния структурных полей: Pull
Struct×Struct, Push Struct×Struct и TryMergeStructFields (во всех трёх — None-поле
потомка пропускается, Optional-совместимость обрабатывает внешний слой; оба узла
получают `IsOptionalSourced := a ∨ b`).

### Отличие от Pull

Pull вычисляет `A.Desc := LCA(...)`. Push вычисляет `D.Anc := GCD(...)` и
**рекурсивно проталкивает** ограничения в компоненты через декомпозицию. Pull
устанавливает composite-структуру узлов, Push наполняет её верхними границами.

**Push передаёт в основном только верхние границы** — нижние двигаются вверх
(Pull). Два задокументированных исключения, где Push передаёт и Desc:
1. **Поле-проекция `CS(Desc=Struct) → Struct-потомок`** — поле-ограничения из Desc
   предка спускаются вниз (включая `MergeInplace` CS-полей);
2. **`TryMergeStructFields`** — `MergeInplace` отождествляет узлы полей целиком,
   перенося обе границы.
Оба — следствие того, что структура предка и потомка представляет одно значение,
и их поля обязаны сойтись в одной identity, а не только в интервалах.

### Width-propagation в Push

Лишние поля потомка добавляются предку **только если `ancestor.IsOpen`**
(row polymorphism: «минимум эти поля» может стать «минимум эти + ещё»). Закрытые
предки (литералы, LCA-результаты массивов) не расширяются.

**Почему предикаты Pull и Push различаются** (`!IsFrozen` vs `IsOpen`): в Pull
предок — выводимая нижняя граница, его специфичность только уточняется — любой
не-контрактный (non-frozen) узел впитывает поля. К моменту Push предок — уже
вычисленная верхняя граница; расширять её вправе только row-полиморфные
(open) структуры, для которых «ещё поля» — часть семантики ограничения, а не
изменение уже зафиксированного типа.

---

## Результат Pull + Push

### Теорема (минимальный интервал)

**Область действия**: ацикличный фрагмент без edge-rewrites, узлы вне
Preferred-веток и row-union (предусловия ниже). После обеих фаз Pull (None
absorption + основной проход) и одного прохода Push каждый узел N с состоянием
`[D..A]` удовлетворяет:

```
D ⊒ LCA{↓Dᵢ | Dᵢ →c N}     (нижняя граница покрывает всех потомков)
A ⊑ GCD{↑Aⱼ | N →c Aⱼ}     (верхняя граница не шире GCD предков)
D ≤ A                        (непустой интервал, иначе ошибка типов)
```

На чистом фрагменте `⊒`/`⊑` обращаются в равенства. Любой тип T ∈ [D..A] —
валидное решение: все потомки конвертируются в T, T конвертируется во все предки.

**Предусловия точности (где `=` заменяется на `⊒`/`⊑`)**:

1. **Row-polymorphism**: для open-структур `AddDescendant` использует
   **field-union**, а не LCA-пересечение (`ConstraintsState.UnionStructFields`) —
   D для них не решёточный join значений, а meet ограничений «имеет минимум эти
   поля» (принятое правило, `Algebra.md` §Row Polymorphism).
2. **Preferred-ветки в ↓** (debt #19, открыт): `Concretest` содержит резолюционные
   примеси (выбор `Opt(Preferred)`, `ConcretestArrayElement` возвращает
   `CS[D, pref]`), поэтому на Preferred-несущих узлах D может отличаться формой
   от чистой проекции. Интервальная часть не нарушается.
3. **Optional deferral**: узлы с `IsOptional=true` остаются CS после Pull+Push —
   материализация `CS(opt) → StateOptional` и часть Optional-wrap-решений
   происходят в Destruction (`WrapAncestorInOptional`/`WrapDescendantInOptional`
   вызываются и оттуда). Интервал после Push не всегда отражает финальную
   Opt-форму.
4. **Циклический фрагмент** — только после SCC-замыкания, и то в пределах его cap
   (§Циклы (c)).
5. **Edge-rewrites в Pull** компенсируются немедленной ре-инвокацией
   (§Циклы (d), debt #10) — теорема опирается на то, что компенсация полна.

### Идемпотентность

На той же области действия Pull+Push **идемпотентны**: повторный Pull не изменит
ни одного Desc (`LCA(D, D) = D`), повторный Push — ни одного Anc (GCD идемпотентен).
Следствие: один проход оптимален. **На циклическом фрагменте однопроходность
не выполняется** — именно поэтому существует SCC-замыкание (§Циклы (c)): Push
итерируется до фикспойнта на циклических SCC.

### Зачем нужен Destruction

Pull+Push вычисляют **интервал**, не выбирают тип. Destruction+Finalize выполняют
**резолюцию** — см. `TicAlgorithm_Destruction.md`.

**Граница Pull/Push и Destruction**: узлы с `IsOptional=true` остаются
ConstraintsState после Pull+Push (IsOptional — флаг на CS, не composite-состояние).
Materialization `CS(IsOptional) → StateOptional` происходит в Destruction. Optional
wrapping **composites** (Composite → Opt(Composite)) происходит в Pull/Push.

---

## Сходимость (ацикличный rewrite-free фрагмент)

### Утверждение

На фрагменте без μ-циклов и без edge-rewrites Pull и Push завершаются за
**один проход** каждый.

### Мера завершения

Определим потенциал графа:

```
Φ(G) = |E_c| + |E_comp|
```

где `E_c` — множество constraint edges, `E_comp` — множество component links
**в финальном графе** (после всех структурных унификаций).

`Φ(G)` конечен (ограничен размером AST: каждый узел AST порождает O(1) рёбер,
каждая структурная унификация заменяет 1 constraint edge на O(k) компонентных,
k — арность composite).

### Доказательство

**Pull**: обход по toposort. Для каждого узла D:
1. Для каждого ancestor-ребра `D →c A` применяется одно правило таблицы:
   - алгебраическое (LCA/Convert): O(1) работы, ребро обработано;
   - структурная унификация: ребро заменяется на k компонентных, каждое
     обрабатывается рекурсивно — суммарный потенциал Φ не растёт;
   - Optional wrapping: создаётся inner-узел + O(1) рёбер; каждый узел
     оборачивается не более одного раза (коиндуктивный identity guard в
     `WrapAncestorInOptional` + после wrap узел диспатчится как Opt) —
     суммарно O(|composite-узлов|) дополнительных рёбер.
2. Рекурсивный Pull компонентов D — каждый component link обрабатывается один раз
   (mark-based dedup в `PullRec`).

Каждое ребро из Φ(G) обрабатывается **ровно один раз**. Итого: O(Φ(G)).

**Push**: обход в обратном toposort, аналогичный аргумент.

**Монотонность**:
- Pull: Desc только растёт (LCA = join, монотонен).
- Push: Anc только убывает (GCD = meet, монотонен).
- Структурная унификация необратима (CS → Composite).
- Интервал `[D..A]` только **сужается**.

**Сложность**: O(Φ(G)) = O(|AST|).

### Достаточные свойства системы типов

| Свойство | Зачем |
|----------|-------|
| **Конечная высота решётки** (ACC) | LCA/GCD вычисляются за конечное число шагов |
| **Тотальность LCA** (Any — top) | ∨ определён для любой пары типов |
| **Монотонность LCA и GCD** | Нет осцилляций; идемпотентность; однопроходность |
| **Конечная глубина вложенности типов** | Рекурсивная декомпозиция завершается (для μ-типов — коиндуктивно, §Циклы) |
| **DAG constraint graph** (после слияния ref/ancestor-циклов) | Однопроходный обход без циклических зависимостей |

Свойства 1-3 — свойства **системы типов**. Свойства 4-5 — свойства **программ**
на ацикличном фрагменте; за его пределами работает циклическая машинерия.

---

## Циклы: μ-машинерия

Полный язык выводит решатель за ацикличный фрагмент тремя путями: рекурсивные
типы (`type node = {next: node?}`), `?.`-цепочки, замыкающие структуры на себя,
и ко-рекурсивные функции. Четыре механизма обслуживают эти случаи; каждый —
задокументированное ослабление теорем §Сходимость.

### (a) Коиндуктивный visited-pair guard (`StagesExtension.Invoke`)

Каждый вызов `Invoke(f, nodeA, nodeB)` на графе, где возможна рекурсия
(`_isRecursion`), регистрирует пару `(nodeA, nodeB)` в thread-static множестве
допущений на время своего выполнения (снимается в `finally` — множество содержит
ровно пары текущего пути рекурсии, как assumption set Amadio–Cardelli '93 §3).

**Правило discharge**: повторная встреча пары → `return true` — коиндуктивное
допущение «совместимость уже проверяется выше по стеку». Это в точности
коиндуктивное подтипирование эквирекурсивных типов: `μX.F(X) ≤ μY.G(Y)` ⟺
развёртки находятся в отношении ≤ при допущении самой пары.

**Аргумент завершения**: каждый рекурсивный `Invoke` либо спускается в
структурно меньшие компоненты, либо попадает на уже допущенную пару и
разряжается. Создание новых узлов внутри Invoke (Optional-wrap) ограничено
правилом «wrap не более одного раза» (identity guard), поэтому множество
достижимых пар конечно и глубина рекурсии ограничена.

Два fast-path пропускают guard (перф): `!_isRecursion` (граф детерминированно
не может содержать μ-циклов — выставляется до TIC) и пары примитив×примитив
(не ре-входят в Invoke).

### (b) Двухъярусная контрактивность

**Ярус 1 — сертификация (Toposort / TarjanScc), слабый критерий.**
`NodeToposort.Visit`: цикл, замкнувшийся через composite-member ребро, помечает
инициатора `IsContractiveCycleHead` и сохраняется. `TarjanScc.IsContractive` —
тот же критерий для SCC: хотя бы одно ребро внутри SCC пересекает конструктор
типа. Критерий **слабый**: любое composite-member ребро (в т.ч. поле Struct или
компонент Fun) засчитывается как пересечение конструктора — этого достаточно,
чтобы отличить кандидата μ-типа от вырожденного `μX.X` (ref/ancestor-цикл,
который сливается или отвергается).

**Ярус 2 — строгая проверка (Destruction/Finalize).**
`ThrowIfRecursiveTypeDefinition` (вызывается на каждом узле в `DestructionRec` и
`FinalizeRecursive`) проверяет **настоящую** контрактивность: каждый замыкающийся
путь обязан пересекать Optional или Array (struct-поле → array → struct — валидный
именованный рекурсивный тип; struct→struct или standalone arr→arr без разрыва —
ошибка). Для opt-sourced циклов (`?.`-происхождение) вместо ошибки выполняется
починка — восстановление Optional-разрыва (`μX. opt(struct{…X…})`); CS с Desc-структурой,
замыкающейся контрактивно, продвигается в F-bound (Desc → S). CS{S} трактуется
как контрактивная граница обхода (bound контрактивен по построению LiftMuTypes).

Разнесение по ярусам — компромисс: на toposort строгая проверка невозможна
(Optional-обёртки ещё не материализованы — часть возникает в Pull Phase 2 и
Destruction), а слабой сертификации достаточно, чтобы не слить μ-цикл и довести
его до Destruction, где форма уже стабильна.

### (c) SCC-замыкание после Push (`TarjanScc` + `ScCClosurePass`)

Однопроходный Push на циклическом графе неполон: в self-referential
return-позициях ко-рекурсивных функций остаются вырожденные ref-цепочки —
циклу нужно несколько обходов, чтобы протащить F-bound из других веток в
рекурсивный путь возврата.

`ScCClosurePass` (только при `IsRecursion`, с O(n)-прескрином
`HasAnyRecursiveCandidate`): Tarjan-разбиение графа на SCC; для каждой
**циклической контрактивной** SCC — `PushUntilFixpoint(scc, maxIterations: 10)`:
итерации Push по узлам SCC, пока состояние/Desc/Anc хоть одного узла меняется
(Kleene-итерация монотонного оператора на решётке конечной высоты — Cousot '77 /
Kildall '73 — сходится).

**Задокументированный компромисс termination-vs-completeness**: cap в 10 итераций
**молчаливый** — при недостижении фикспойнта проход просто останавливается без
ошибки и без диагностики. Теоретически монотонное сужение обязано сойтись раньше
(высота решётки конечна), так что cap — страховка от незамеченной немонотонности;
но если она случится, решатель продолжит с недо-распространёнными границами —
это проявится как неточный тип, а не как ошибка. Кандидат в TicTechnicalDebt:
диагностика (assert/trace) на срабатывание cap.

### (d) Pull edge-rewire + немедленная ре-инвокация (debt #10)

Implicit-lift-материализация в Pull (`desc →c opt(T)` переписывается в
`desc →c T`) создаёт новые рёбра **после** того, как toposort зафиксировал
порядок — T может быть уже обработан, и однопроходный Pull к нему не вернётся.
Компенсация: общий helper `LiftDescendantToOptionalElement`
(`PullConstraintsFunctions`) выполняет rewire и **немедленно** зовёт
`StagesExtension.Invoke` на новом ребре (eager propagation). Два call-site'а:
Opt-ancestor × Primitive-descendant и Opt-ancestor × CS-descendant с
примитивным Desc. Без компенсации: `true ?? 1.5` → soundness violation
(MR6Bug3), `1 ?? 'hello'` → крах (MR3Bug1).

Это **workaround**, а не решение: однопроходный инвариант Pull нарушен и лишь
немедленно восстанавливается. Правильный fix — worklist Pull.
Cross-ref: `TicTechnicalDebt.md` #10. Теоремы §Сходимость несут эту каверзу
как предусловие 5.

### Hang triage — диагностика зависаний

Зависание или StackOverflow в TIC — всегда пробой одного из четырёх колец
завершения. Диагностика по форме стека, чек-лист по порядку:

**Кольцо 1 — коиндуктивный guard стадий (§(a)).**
- *Сигнатура стека*: повторяющиеся пары кадров
  `StagesExtension.Invoke → InvokeCore → Apply(...)` (Pull/Push/Destruction)
  **без** создания новых узлов.
- *Guard*: thread-static множество допущений `_invokeVisitedPairs` — повторная
  пара разряжается `true`.
- *Пробой*: `_isRecursion` не выставлен при реально μ-способном графе — guard
  пропущен fast-path'ом. Флаг транслируется в `SolveCore` из
  `GraphBuilder.IsRecursion`; источники: setter `NamedTypeRegistry`,
  `SetSafeFieldAccess`/`SetSafeMethodCall`, `RuntimeBuilder` для рекурсивных
  user-функций. Новая μ-способная конструкция, забывшая `IsRecursion = true`, —
  типовая причина.

**Кольцо 2 — identity-guards обёрток (§Optional wrapping).**
- *Сигнатура*: повторяющиеся кадры `WrapAncestorInOptional` /
  `WrapDescendantInOptional` **с созданием узлов** — растущая цепочка
  `ow`-узлов (`owarr` → `owowarr` → …) в trace.
- *Guard*: коиндуктивный identity guard (`ReferenceEquals` элемента Opt и
  оборачиваемого узла) + сертификация toposort — `IsContractiveCycleHead`
  переводит wrap потомка в unwrap (μ-фикспойнт вместо башни развёрток).
- *Пробой*: certification miss — цикл замкнулся ПОСЛЕ toposort (edge-rewire,
  Destruction-обёртки), head не помечен → `WrapDescendantInOptional` строит
  башню `opt(opt(...))`.

**Кольцо 3 — коиндуктивный контекст алгебры (`Algebra.md` инвариант 13).**
- *Сигнатура*: повторяющиеся кадры семейства `Lca` / `Gcd` /
  `UnifyOrNull`/`MergeOrNull` / `FitsInto` / `GcdBound` без кадров стадий между
  ними.
- *Guard*: `AlgebraCycleContext` — явный параметр, лениво создаваемый
  цикло-способными ветками и протянутый через все мосты взаимно-рекурсивного
  семейства; assumption sets раздельны по операторам.
- *Пробой*: не-протянутый ctx-мост — новый вызов внутрь семейства с ctx=null.
  Единственный известный кандидат доказан недостижимым (`Convert`-мост, долг #32
  в `TicTechnicalDebt.md` — там же паттерн fix'а при инвалидации аргумента).

**Кольцо 4 — covers-join walk (Destruction).**
- *Сигнатура*: повторяющиеся кадры `JoinCarriesOptionalBeyond`.
- *Guard*: visited-set пар состояний по reference-identity (`RefPairComparer`)
  + страховочный предел глубины 100.
- *Пробой*: регрессия reference-identity компаратора (переход на структурное
  равенство или пересборка состояний на каждом уровне — пары перестают
  повторяться). Depth-предел тогда ограничивает глубину, но ветвящиеся μ-формы
  дают экспоненциальный обход — практически зависание.

**Исключаются из подозреваемых**: SCC-фикспойнт (`PushUntilFixpoint`, жёсткий cap
10 итераций, §(c)) и depth-предел covers-join (100) — ОГРАНИЧЕНЫ по построению,
зациклиться не могут; их пробой проявляется как неточный тип (тихая остановка),
а не как hang.

---

## Пример

Выражение: `y = if(x > 0) x + 1 else 2`

### Build

Оператор `>` — сигнатура `(T,T) → Bool`, T — Comparable `[∅..∅, cmp]`;
оператор `+` — `(T,T) → T`, T — Arithmetical `[U24..Real]`. Инстанциация создаёт
свежие узлы Tc' и Ta'. Узел `+` — RefTo(Ta'); после toposort-постобработки его
ancestor-ребро переносится на Ta'. Условие if пришпилено к Bool (не трассируем).

```
Узлы и начальные состояния:
  x:   [∅..∅]
  0:   [U8..Real, pref=I32]
  1:   [U8..Real, pref=I32]
  2:   [U8..Real, pref=I32]
  Tc': [∅..∅, cmp]              ← свежий из сигнатуры >
  Ta': [U24..Real]              ← свежий из сигнатуры +
  R:   [∅..∅]                    ← результат if-else
  y:   [∅..∅]

Constraint edges:
  x   →c Tc'   (аргумент >)
  0   →c Tc'   (аргумент >)
  x   →c Ta'   (аргумент +)
  1   →c Ta'   (аргумент +)
  Ta' →c R     (ветка if, через RefTo(+))
  2   →c R     (ветка else)
  R   →c y     (определение)
```

None-узлов нет → Pull fused с Toposort, один проход.

### Pull (порядок: 0, 1, 2, x, Tc', Ta', R, y)

```
Обработка 0: ancestor Tc'. Pull(Tc', 0) — CS ← CS:
  Tc'.Desc := LCA(∅, U8) = U8;  Tc'.pref := I32 (вверх).
  Tc' → [U8..∅, cmp, pref=I32]

Обработка 1: ancestor Ta'.
  Pull(Ta', 1):  Ta'.Desc := LCA(U24, U8) = U24 (без изменений);  Ta'.pref := I32.

Обработка 2: ancestor R.
  Pull(R, 2):    R.Desc := LCA(∅, U8) = U8;  R.pref := I32.   R → [U8..∅, pref=I32]

Обработка x: ancestors Tc', Ta'.
  Pull(Tc', x):  LCA с ∅-Desc — без изменений (∅ нейтрален).
  Pull(Ta', x):  без изменений.

Обработка Tc': ancestors нет.

Обработка Ta': ancestor R. Pull(R, Ta') — CS ← CS:
  R.Desc := LCA(U8, ↓[U24..Real]) = LCA(U8, U24) = U24.   R → [U24..∅, pref=I32]

Обработка R: ancestor y. Pull(y, R):
  y.Desc := LCA(∅, U24) = U24;  y.pref := I32.   y → [U24..∅, pref=I32]
```

### PropagatePreferred

Collect находит I32; Apply ничего не меняет — все CS с примитивным Desc уже несут
I32, у x нет Desc (условие Apply не выполнено).

### Push (порядок: y, R, Ta', Tc', x, 2, 1, 0)

```
Обработка y: ancestors нет → пропуск.

Обработка R: ancestor y. Push(y, R): y.Anc = ∅, cmp нет → пропуск.

Обработка Ta': ancestor R. Push(R, Ta'): R.Anc = ∅ → пропуск.

Обработка Tc': ancestors нет.

Обработка x: ancestors Tc', Ta'.
  Push(Tc', x): Tc'.Anc = ∅ → интервал не меняется, но cmp идёт вниз: x.cmp := true.
  Push(Ta', x): x.Anc := GCD(∅, Real) = Real.    x → [∅..Real, cmp]

Обработка 2: Push(R, 2): R.Anc = ∅ → пропуск.

Обработка 1: Push(Ta', 1): 1.Anc := GCD(Real, Real) = Real (без изменений).

Обработка 0: Push(Tc', 0): Tc'.Anc = ∅ → интервал не меняется; 0.cmp := true.
  0 → [U8..Real, cmp, pref=I32]
```

### Результат (интервалы после Pull+Push)

```
x:   [∅..Real, cmp]
0:   [U8..Real, cmp, pref=I32]
1:   [U8..Real, pref=I32]
2:   [U8..Real, pref=I32]
Tc': [U8..∅, cmp, pref=I32]
Ta': [U24..Real, pref=I32]
R:   [U24..∅, pref=I32]
y:   [U24..∅, pref=I32]
```

Destruction+Finalize (см. `TicAlgorithm_Destruction.md`) выберут конкретные типы;
y разрешится в I32 (Preferred лежит в интервале `[U24..∅]`).

---

## Пример 2: Optional (двухфазный Pull)

Выражение: `y = if(c) 42 else none`

### Build

```
Узлы:
  c:   [∅..∅]
  42:  [U8..Real, pref=I32]
  none: None
  R:   [∅..∅]                ← результат if-else
  y:   [∅..∅]

Constraint edges:
  42   →c R    (ветка if)
  none →c R    (ветка else)
  R    →c y    (определение)
```

None-узел есть → двухфазный Pull.

### Pull Phase 1 (только None)

```
none →c R: None absorption (AddDescendant(None)). R.IsOptional := true.
R → [∅..∅, opt]
```

### Pull Phase 2 (не-None, toposort: c, 42, R, y)

```
42 →c R: правило CS ← CS.
  R.Desc := LCA(∅, U8) = U8;  R.pref := I32.   R → [U8..∅, opt, pref=I32]

R →c y: правило CS ← CS.
  y.Desc := LCA(∅, U8) = U8.
  y.opt := false ∨ true = true.
  y.pref := I32.
  y → [U8..∅, opt, pref=I32]
```

### Push (reverse: y, R, 42, c)

```
Все ancestors без верхних границ — пропуск.
```

### Результат (интервалы после Pull+Push)

```
42:  [U8..Real, pref=I32]
R:   [U8..∅, opt, pref=I32]
y:   [U8..∅, opt, pref=I32]
```

### Destruction (кратко)

Materialization (подфаза 5b): R и y имеют `IsOptional=true`.
`R: [U8..∅, opt] → Opt(inner)`, inner = `[U8..∅, pref=I32]`.
Finalize: inner → I32 (Preferred в интервале). y → `Opt(I32)`.

**Итог: y = int?** (Opt(I32)). Значение: 42 → Opt(42), none → None.

---

## Связь с алгеброй

| Фаза | Оператор | Что вычисляет | Направление |
|------|----------|---------------|-------------|
| Pull | LCA (∨), Concretest (↓) | Desc = LCA нижних границ потомков | desc → anc |
| Push | GCD (∧), Abstractest (↑) | Anc = GCD верхних границ предков | anc → desc |
| Оба | Convert (≤c⁺/≤c⁻), FitsInto (≤) | Проверка совместимости | — |
| Оба | Структурная декомпозиция | Composite ≤ Composite → компоненты | рекурсивно |
| Оба | GcdBound (meet оси S) | S = S₁ ∪ S₂ + перенос владения back-edges | — |
| Toposort | Merge (⊓, `GetMergedStateOrNull`) | Слияние **ref/ancestor**-циклов | — |
| SCC-замыкание | GCD (итерация Push) | Фикспойнт на контрактивных SCC | anc → desc |

---

## Границы ответственности TIC

TIC отвечает ровно за одно: вывод **типов** (constraint graph → решённые
состояния). Смежные вопросы, которые регулярно ошибочно ищут в TIC:

- **VALUE-семантика операторов** (переполнение, деление, wrap-режим) — слой
  Functions: семейство `GenericSwitchFunctions` + T4-шаблон
  `GenericSwitchFunctionsGenerated.tt` (`src/NFun/Functions/`); контракт —
  `Specs/Operators.md` §Integer overflow. TIC выбирает ТИП `+`; поведение `+`
  на этом типе — не его вопрос.
- **CLR-конверсия значений** — `TypeBehaviour` / конверторы (`src/NFun/Types/`:
  `VarTypeConverter`, `FunnyConverter`); контракт — `Specs/Types.md`
  §Type conversion rules. TIC гарантирует лишь, что все требуемые конверсии
  находятся в отношении ≤; корректность каждой — **аксиома реализации**
  (`Algebra.md` §Теоремы, Soundness; `TicAlgorithm_Destruction.md`
  §Ограничение), верифицируется per-conversion тестами, не доказывается в
  TIC-алгебре.
- **Error surface** (FU-коды, тексты, позиции) — слой ParseErrors: TIC бросает
  внутренние `TicException` без привязки к синтаксису; трансляция в FU —
  `Errors.TranslateTicError` (`ParseErrors/Errors.4.Types.cs`). Индекс FU-кодов
  TIC-путей — `README-Tic.md`.
- **Скрипт-семантика** (unbound inputs, anonymous `out`, правила инициализации
  output-переменных) — `Specs/Basics.md`; в TIC эти конструкции приходят уже
  как узлы и рёбра графа.
