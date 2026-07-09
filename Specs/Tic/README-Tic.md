# TIC — навигация по спецификациям

## Карта набора

Набор устроен двухслойно (конституция — `Algebra.md` §Обзор: чистая тотальная
АЛГЕБРА vs РЕЗОЛЮЦИЯ). Читать сверху вниз: **конституция** — `Algebra.md`
(инвентарь операторов, транспорт осей, инварианты, теоремы) + `TicTypeSystem.md`
(решётка типов, вариантность, постулаты подтипирования); **пооператорные
алгебра-файлы** — `Algebra_LCA.md`, `Algebra_GCD.md`, `Algebra_Merge.md`,
`Algebra_Fit.md`, `Algebra_Concretest.md`, `Algebra_Abstractest.md`,
`Algebra_Unify.md`, `Algebra_CanonicalForms.md` (канонические Optional-формы,
закон refinability); **алгоритм** — `TicGraph.md` (узлы/рёбра),
`TicAlgorithm.md` (Build → Toposort → Pull → Push, циклы, hang triage, границы
ответственности), `TicAlgorithm_Destruction.md` (Destruction + Finalize),
`TicSimplePath.md` (SPS fast-path для чисто примитивных выражений),
`PushReform.md` (изо-рекурсивный вывод именованных структур, F-bounds);
**резолюция** — `TicPreferred.md` (hint-слой) и `TicResolution.md` —
спецификация резолюционного хвоста (Solve*, ↓ₛ, материализация; добавляется
параллельно — ссылаться по имени). Реестр долгов — `TicTechnicalDebt.md`.

## Индекс FU-кодов TIC-путей

Все коды таблицы проверены по call-site'ам: они возникают из вывода типов —
либо TIC бросает `TicException`, а `Errors.TranslateTicError`
(`ParseErrors/Errors.4.Types.cs`) транслирует его в FU по синтаксическому
контексту, либо adapter-слой (`ExpressionBuilderVisitor`, конверторы) валидирует
уже решённые типы. «Фаза» — где искать источник, «Спека» — с чего начинать.

| FU | Типовой симптом | Фаза-производитель | Спека |
|----|-----------------|--------------------|-------|
| 710 | «Unable to cast from X to Y» | adapter: вставка конверсий по решённым типам (`VarTypeConverter`). Исторический симптом dead-island (замороженный snapshot вне рёбер) | `Algebra_CanonicalForms.md` §Закон refinability; `Specs/Types.md` |
| 711 | generic T требует разных структурных глубин (`'h' in 'hello'`) | adapter: `ValidateGenericResolution` (`ExpressionBuilderVisitor`) — принятый дизайн, проверка невозможна в TIC | CLAUDE.md §Accepted Design (FU711) |
| 719 | «error somewhere in the types» — fallback, узлы не замаплены на синтаксис | Pull (`IncompatibleTypes`), Push (`IncompatibleNodes`), ⊓ toposort-слияния (`CannotMerge`), Destruction, wrap-reject (`StagesExtension`) | `TicAlgorithm.md` §Фаза 3/§Фаза 4 (Отказ-ряды); разбор образцового случая — §Паттерн расследования ниже |
| 722 / 725 | «Element '…' has no fields» | Build: `CannotSetState` при field-access-цепочке | `TicAlgorithm.md` §Фаза 1; `TicGraph.md` |
| 728 / 731 / 734 | «Recursive type definition» (± имя, ± цикл) | Toposort (не-контрактивный цикл, `NodeToposort`) и Destruction/Finalize (строгая проверка `ThrowIfRecursiveTypeDefinition`) | `TicAlgorithm.md` §Циклы (двухъярусная контрактивность) |
| 737 | «Invalid functional variable signature» | Build: `SetCall` на functional variable | `TicGraph.md`; `Specs/Rules.md` |
| 738 | «… is not a function or functional variable» | Build | `TicGraph.md` |
| 740 | «Variable 'x' cannot be initialized with type constrains '…'» | Pull/Push/Destruction — Incompatible ancestor, предок — уравнение | `TicAlgorithm.md` §Фаза 3/4 |
| 743 / 746 | «Constant '…' cannot be used here» | то же семейство, потомок — литерал (часто литеральный CS `[U8..Re]I32!`) | `TicAlgorithm.md` §Фаза 3/4; `TicPreferred.md` |
| 749 / 752 | «Variable '…' cannot be used here» | то же семейство, потомок/предок — переменная | `TicAlgorithm.md` §Фаза 3/4 |
| 755–777 | семейство «…cannot be used here» — shape-варианты по виду узла (expression/rule/call/const/struct/array) | то же семейство Incompatible ancestor | `TicAlgorithm.md` §Фаза 3/4 |
| 758 | «'…' cannot be used as an argument of 'f'» | то же семейство, предок — аргумент вызова | `TicAlgorithm.md` §Инстанциация вызовов функций |
| 780 / 783 | «Invalid operator/function call argument: sig. Expected: T, but was: …» | два источника: Build (`CannotSetState`, родитель — вызов) и Incompatible ancestor (потомок — аргумент) | `TicAlgorithm.md` §Фаза 1 + §Фаза 3/4 |
| 798 / 799 | «Types cannot be solved: …» — общий fallback | 798 — нетранслируемый `CannotSetState`; 799 — любой другой `TicException` | начинать с TraceLog + `TicAlgorithm.md` §Фазы |

Резолюционный хвост (Solve*, материализация, output generics) — свои точки
отказа; см. `TicResolution.md`.

## Паттерн расследования

На примере Bug#6 (трейл: `TicPreferred.md` §3.4, `StateExtensions.Lca`):

1. **Симптом** — FU-код + минимальный скрипт (Bug#6: FU719 на поле `{v:float32?}`).
2. **Дифференциал** — что переключает поведение (поле пушит F32, но заранее
   запечённый `opt(Re)` его не принимает).
3. **Фаза** — по индексу выше + TraceLog: где возникла граница (LCA в Pull
   зафиксировал тип до Push-сужения).
4. **Ячейка** — конкретная ветка оператора/Apply (∨-ветка
   `opt(P) ∨ CS[D..A](Pref)`).
5. **Символ + закон** — нарушенный инвариант и fix на уровне ячейки (eager
   `StateOptional.Of(Preferred)` нарушал P3 «hint не создаёт ограничение»; fix —
   join остаётся интервалом с hint'ом; долг #11 закрыт). Тест — на нижнем
   возможном уровне пирамиды.
