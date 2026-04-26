# TIC Technical Debt

Ограничения текущей реализации, которые отклоняются от идеальной алгебры. 

---


## 5. Stale Pull snapshots (DescendantHasOptionalLift) — WORKAROUND

**Проблема**: Desc-snapshot в CS создаётся в Pull Phase 1. Phase 2 (PullNoneNode) оборачивает элементы в Optional. Snapshot устаревает — не отражает Optional wrapping.

**Текущее решение**: два специальных случая в `DestructionFunctions.Apply(CS, ICompositeState)`:
1. Actual descendant содержит Optional-элементы, snapshot — нет → принять actual через RefTo.
2. Snapshot содержит Optional-элементы, actual — нет → принять snapshot, рекурсивная Destruction.

**Почему это workaround**: Destruction компенсирует temporal gap в Pull. Добавляет complexity (два branch'а + helper `DescendantHasOptionalLift` + `HasAnyOptionalLiftedField`).

**Правильный fix**: Single-pass Pull с immediate Optional propagation (устранить двухфазность), или invalidation/refresh снапшотов после Phase 2.

---

## 6. TwoVariableEquality — unconstrained generics resolve to Any

**Проблема**: `y = a == b` (два unconstrained var). TIC резолвит a,b к Any вместо generic T.

**Причина**: `SolveUselessGenerics` не помечает named input variables как "keep generic" — только output types и explicit inputNodes (из SetFunDef). Free variables без SetFunDef попадают в covariant loop → SolveCovariant → Any.

**Правильный fix**: Named nodes без SetDef = implicit inputs. Их leaf types должны маркироваться как "keep generic" в SolveUselessGenerics. Требует осторожности: нельзя маркировать ВСЕ named nodes (сломает recursive functions где named vars должны резолвиться).

---

## 7. PropagatePreferred — single global value

**Проблема**: PropagatePreferred собирает ОДИН глобальный Preferred (первый найденный) и broadcast'ит всем совместимым CS нодам. При mixed Preferred (e.g., hex P=U16 + int P=I32) результат зависит от порядка toposort.

**Текущее состояние**: не проблема пока все integer литералы имеют одинаковый Preferred из диалекта (I32, I64, или Real). Станет проблемой при per-literal Preferred.

**Правильный fix**: Edge-local PropagatePreferred — пропагация по рёбрам графа вместо global broadcast. Каждый литерал пушит свой Preferred только в connected nodes.

---

## Порядок устранения

```
#5 (stale snapshots) — single-pass Pull refactoring, medium effort
#6 (unconstrained generics) — SolveUselessGenerics fix, small effort but tricky
#7 (PropagatePreferred) — edge-local rewrite, medium effort, not urgent
```
