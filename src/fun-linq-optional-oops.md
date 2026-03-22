# Спецификация: LINQ + Optional + oops + lazy default

## Мотивация

Множество коллекционных функций могут "не найти" результат: `first`, `last`, `max`, `min`, `single`.
В языках без optional каждая из них порождает 2-3 варианта: `first` / `firstOrNull` / `firstOrDefault`.

NFun может решить это единым паттерном: **одна функция + optional тип + `oops` + lazy default**.

## Ключевые концепции

### 1. `oops` — bottom value (тип `⊥`)

`oops` — ключевое слово, аналог `none` но "громкий": означает "тут ошибка, падай".

```
oops                  # ⊥, generic ошибка
oops('message')       # ⊥, с сообщением
```

Тип `⊥` — подтип всего. Поэтому `oops` подставляется в любой контекст:

```python
y = a ?? oops                    # ≡ a!  (unwrap or throw)
y = if(x > 0) x else oops       # assertion: x must be > 0
y = first(rule, oops)            # find or throw
```

`oops` и `none` — два "дна" языка:
- `none` — тихое дно (нет значения)
- `oops` — громкое дно (ошибка)

`a!` — синтаксический сахар для `a ?? oops`.

### 2. Единый паттерн: `f(..., lazy default)`

Каждая "поисковая" функция имеет одну сигнатуру с optional lazy default:

```
first(predicate?, lazy default?) → T? или T
```

Поведение зависит от наличия default:
- **Без default** → возвращает `T?` (optional ON) или throws (optional OFF)
- **С default** → возвращает `T` (всегда, в обоих режимах)

### 3. `rule` как синтаксический маркер

NFun не поддерживает перегрузки с одинаковой арностью.
Конфликт arity-1: `first(rule it > 0)` vs `first(0)`.

Решение: парсер различает по наличию ключевого слова `rule`:
- Видит `rule` → аргумент заполняет predicate/selector
- Не видит → аргумент заполняет default

Это **синтаксический** routing, не type-based overloading.

### 4. Lazy default — бесплатный

Lazy default не требует lambda/closure для built-in функций.

**Expression Tree** (текущий движок): default — child node, `.Calc()` вызывается
только если элемент не найден. Тот же паттерн что `??`, `if-else`, `and`, `or`.

```csharp
class FirstWithDefaultNode : IExpressionNode {
    IExpressionNode collection, predicate, defaultExpr;

    object Calc() {
        foreach (var item in collection.Calc())
            if (predicate.Calc(item)) return item;
        return defaultExpr.Calc();  // lazy: только если не нашли
    }
}
```

**VM** (будущий движок): conditional jump — skip байткода default-выражения.

```
EVAL collection
EVAL predicate
FIRST_OR_JUMP @default
JUMP @end
@default:
  EVAL default_expr
@end:
```

Стоимость: **ноль** для built-in функций. Lambda нужна только для
user-defined lazy параметров (отдельная фича, потом).

## Синтаксические формы

Для каждой функции — 4 формы вызова:

```python
.first()                       # → T?    без predicate, без default
.first(0)                      # → T     без predicate, default = 0
.first(rule it > 0)            # → T?    с predicate, без default
.first(rule it > 0, 0)         # → T     с predicate, default = 0
```

Будущий сахар (не влияет на семантику):

```python
# tail lambda:
.first(0) rule it > 0          # ≡ .first(rule it > 0, 0)
.first(0) { it > 0 }           # ≡ .first(rule it > 0, 0)

# named params:
.first(default = 0)            # ≡ .first(0)
```

## Функции, попадающие под паттерн

### Возвращают один элемент (могут не найти)

| Функция | Predicate/Selector | Когда "нет результата" |
|---------|-------------------|----------------------|
| `first` | `T → bool` (predicate) | не нашёл / пустая коллекция |
| `last` | `T → bool` (predicate) | не нашёл / пустая коллекция |
| `single` | `T → bool` (predicate) | 0 или 2+ совпадений |
| `max` | `T → K` (selector) | пустая коллекция |
| `min` | `T → K` (selector) | пустая коллекция |
| `reduce` | `(T,T) → T` (aggregator) | пустая коллекция |
| `elementAt` | `int` (index) | index out of bounds |

Примечание: `max`/`min` объединяют `max()`/`maxBy()` — без selector
сравниваются сами элементы, с selector — по ключу. Суффикс `By` не нужен.

`sort` и `distinct` — всегда успешны, default не нужен:
- `sort()`, `sort(rule selector)`
- `distinct()`, `distinct(rule selector)`

### Таблица вызовов (на примере `first`)

```python
# optional ON:
[1,2,3].first()                    # → T?  (первый элемент)
[1,2,3].first(rule it > 10)        # → T?  (none — не нашёл)
[1,2,3].first(rule it > 10, -1)    # → T   (-1 — default)
[1,2,3].first(rule it > 10, oops)  # → T   (throw — oops)
[1,2,3].first(oops)                # → T   (первый элемент или throw)

# optional OFF:
[1,2,3].first()                    # → T   (throw если пусто)
[1,2,3].first(rule it > 10)        # → T   (throw если не нашёл)
[1,2,3].first(rule it > 10, -1)    # → T   (-1 — default)
[1,2,3].first(-1)                  # → T   (первый элемент или -1)
```

### Nullable коллекции (T?[])

Проблема: `first(rule) → T??` flatten → `T?`. "Нашёл none" и "не нашёл" неотличимы.

```python
[1, none, 3].first(rule it == none)     # → none : int?  (нашёл? не нашёл?)
[1, none, 3].first(rule it == none)!    # → throw  (BUG: нашли, но throw!)
```

Решение — `default` параметр работает **внутри** функции (до flatten):

```python
[1, none, 3].first(rule it == none, oops)   # → none : int?  ✓ (нашёл none)
[1, none, 3].first(rule it > 10, oops)      # → throw        ✓ (не нашёл)
```

`??` и `!` работают ПОСЛЕ flatten — теряют информацию.
`default` работает ВНУТРИ функции — информация сохранена.

Поэтому `first(rule, default)` — **не дублирование `??`**, а семантически
другая операция.

Для случаев когда `default` не помогает — используй правильный инструмент:

```python
[1, none, 3].any(rule it == none)           # → true (bool)
[1, none, 3].firstIndex(rule it == none)    # → 1 (int?)
[1, none, 3].filterNotNone()                # → [1, 3] : int[]
```

## Дополнительные функции (не lazy-default, но часть LINQ)

### Предикаты

```python
any(rule)  : T[] × (T → bool) → bool    # хотя бы один
all(rule)  : T[] × (T → bool) → bool    # все
```

### Подпоследовательности

```python
take(n)         : T[] × int → T[]
drop(n)         : T[] × int → T[]
takeWhile(rule) : T[] × (T → bool) → T[]
dropWhile(rule) : T[] × (T → bool) → T[]
```

### Трансформации

```python
flatMap(rule) : T[] × (T → K[]) → K[]
zip(other)    : T[] × K[] → {first:T, second:K}[]
```

### Optional-специфичные

```python
filterNotNone() : T?[] → T[]           # убрать none + сузить тип
firstIndex(rule): T[] × (T → bool) → int?
lastIndex(rule) : T[] × (T → bool) → int?
```

### Функция `takeIf` (не коллекция)

```python
takeIf(rule)  : T × (T → bool) → T?    # значение → optional по предикату
```

Мост из обычного значения в optional pipeline:

```python
input
  .takeIf(rule it > 0)       # int → int?
  ?.transform()               # optional chaining
  ?? defaultValue              # unwrap
```

## Режимы компилятора: optional ON / OFF

| | optional ON | optional OFF |
|---|---|---|
| `first()` | `T?` | `T` (throw если пусто) |
| `first(default)` | `T` | `T` |
| `first(rule)` | `T?` | `T` (throw если не нашёл) |
| `first(rule, default)` | `T` | `T` |
| `a ?? b` | есть | нет (не нужен) |
| `a!` | unwrap | нет (не нужен) |
| `oops` | есть | есть (≡ поведение first() без default) |

Функции с default работают **одинаково** в обоих режимах.
Функции без default меняют возвращаемый тип: `T?` → `T` (с throw).

## Зависимости между фичами

```
                 ┌──────────────┐
                 │  optional ON │
                 │  (T? types)  │
                 └──────┬───────┘
                        │ enables
            ┌───────────┼───────────┐
            ▼           ▼           ▼
     ┌──────────┐ ┌──────────┐ ┌──────────┐
     │    ??    │ │    ?.    │ │    !     │
     │ coalesce │ │safe acc. │ │ unwrap   │
     └──────────┘ └──────────┘ └──────────┘
            │                       │
            │ same pattern          │ sugar for
            ▼                       ▼
     ┌──────────────────┐   ┌──────────────┐
     │  lazy default    │   │  ?? oops     │
     │  (branch-based)  │   │              │
     └────────┬─────────┘   └──────┬───────┘
              │                     │
              ▼                     ▼
     ┌──────────────────────────────────────┐
     │  LINQ functions with lazy default    │
     │  first, last, single, max, min,      │
     │  reduce, elementAt                   │
     └──────────────────────────────────────┘
              │
              │ uses
              ▼
     ┌──────────────────┐
     │  oops / oops(msg)│
     │  bottom type ⊥   │
     └──────────────────┘
```

## Порядок реализации

1. **oops** — ключевое слово, тип `⊥`, runtime = throw FunnyRuntimeException
2. **oops('message')** — с lazy сообщением
3. **Lazy default в built-in** — branch-based, как `??` (zero cost)
4. **LINQ функции** — `first`, `last`, `single`, `max`, `min`, `reduce`, `elementAt`
5. **Вспомогательные** — `any`, `all`, `filterNotNone`, `firstIndex`, `takeIf`
6. **sort(rule)**, **distinct(rule)** — By-варианты через перегрузку
7. **(Потом) tail lambda** — синтаксический сахар
8. **(Потом) named/default params** — синтаксический сахар
9. **(Потом) user-defined lazy params** — требует closure/thunk, дорого
